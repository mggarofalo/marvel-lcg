using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    /// <inheritdoc/>
    public Prompt? Choosing(
        World world, Card source, int player, int stoppedAt, AbilityType? tier = null)
        => Choosing(world, source, player, stoppedAt, tier, finalStep: false);

    /// <inheritdoc/>
    public Prompt? Choosing(
        World world, Card source, int player, int stoppedAt, AbilityType? tier, bool finalStep)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);

        var choice = Choice(world, source, player, stoppedAt, tier);

        var persisted = ContinuationStep(world, source, stoppedAt, tier);
        var cast = Resuming(
            world, source, player, tier, finalStep,
            persisted?.AbilityOccurrence) with
        {
            EachPlayerFrame = persisted?.EachPlayerFrame ?? false,
            FinalPlayer = persisted?.FinalPlayer ?? false,
            AbilityPlayer = persisted?.AbilityPlayer ?? player,
        };
        RestorePersisted(cast, persisted);
        if (persisted is
            { AbilityOrdinal: >= 0, AbilityPath: { } choicePath } current)
        {
            cast.RestoreAbility(
                current.AbilityOrdinal, choicePath, current.AbilityFace);
            RestorePathBindings(cast, choicePath);
        }

        if (choice.OperationName() == "indirectDamage")
        {
            return Sharing(source, player, (AbilityEffect.IndirectDamage)choice, cast);
        }

        if (choice.OperationName() == "and")
        {
            int count = EffectOf<AbilityEffect.Simultaneous>(choice, cast).Effects.Length;
            return new Prompt(
                Player: world.FirstPlayer,
                Asking: Question.Order,
                When: TimingPriority.Untimed,
                Trigger: Steps.CardRevealed,
                Label: $"{source.FaceId}: order simultaneous effects",
                Cancellable: false,
                Affordances:
                [
                    new Affordance(
                        source.ObjectId,
                        "Order",
                        source.ObjectId,
                        world.FirstPlayer,
                        "simultaneous effects",
                        new TargetRequest(
                            Enumerable.Range(0, count).ToList(),
                            count,
                            count,
                            Rule: "rr:first-player.3")),
                ]);
        }

        if (choice.OperationName() is "enemyAttacks" or "enemySchemes")
        {
            var enemies = ActivationCandidates(ActivationOf(choice, cast), cast);
            var ids = enemies.Select(enemy => enemy.ObjectId).ToList();
            return new Prompt(
                Player: world.FirstPlayer,
                Asking: Question.Order,
                When: TimingPriority.Untimed,
                Trigger: Steps.CardRevealed,
                Label: $"{source.FaceId}: order enemy activations",
                Cancellable: false,
                Affordances:
                [
                    new Affordance(
                        source.ObjectId,
                        "Order",
                        source.ObjectId,
                        world.FirstPlayer,
                        "enemy activations",
                        new TargetRequest(ids, ids.Count, ids.Count, Rule: "rr:activation.5")),
                ]);
        }

        bool cards = choice.OperationName() == "chooseCard";

        // `rr:choose-option` and `rr:choose-game-element` are two questions and
        // not one: an option is a branch the card lists, an element is a card
        // on the board.
        if (choice.OperationName() == "resolveSpecials")
        {
            var upgrades = Every(EffectOf<AbilityEffect.CardAction>(choice, cast).Selection, cast);
            return new Prompt(
                Player: player,
                Asking: Question.Element,
                When: TimingPriority.Untimed,
                Trigger: Steps.ResolveSpecial,
                Label: $"{source.FaceId}: order Special abilities",
                Cancellable: false,
                Affordances:
                [
                    new Affordance(
                        Id: source.ObjectId,
                        Verb: ChooseVerb,
                        AnchorId: source.ObjectId,
                        AnchorPlayer: player,
                        Label: choice.OperationName(),
                        Targets: new TargetRequest(
                            [.. upgrades.Select(card => card.ObjectId)],
                            upgrades.Count,
                            upgrades.Count)),
                ]);
        }
        if (choice.OperationName() == "payOrExhaust")
        {
            string required = EffectOf<AbilityEffect.PayOrEffect>(choice, cast).Resources;
            var sources = CardPlay.Generators(world, world.Facts, world.Seats[player]);
            string pool = string.Concat(sources.SelectMany(source => source.Generates));
            var offers = new List<Affordance>();
            if (Resources.Pays(pool, required.Length, required))
            {
                offers.Add(new Affordance(
                    0, ChooseVerb, source.ObjectId, World.Scenario, "spend",
                    Costs:
                    [
                        new CostOption(
                            source.ObjectId,
                            required.Length.ToString(
                                System.Globalization.CultureInfo.InvariantCulture),
                            [required],
                            Sources: sources),
                    ]));
            }
            var exhaust = (AbilityEffect.CardAction)EffectOf<AbilityEffect.PayOrEffect>(choice, cast).Otherwise;
            if (Every(exhaust.Selection, cast).Any(card => card.Ready))
            {
                offers.Add(new Affordance(
                    1, ChooseVerb, source.ObjectId, World.Scenario, "exhaust"));
            }
            return new Prompt(
                player, Question.Option, TimingPriority.Untimed,
                Steps.CardRevealed, $"{source.FaceId}: spend or exhaust",
                Cancellable: false, offers);
        }
        if (choice.OperationName() == "payOrEffect")
        {
            string required = EffectOf<AbilityEffect.PayOrEffect>(choice, cast).Resources;
            var sources = CardPlay.Generators(world, world.Facts, world.Seats[player]);
            var offers = new List<Affordance>();
            if (Resources.Pays(string.Concat(sources.SelectMany(s => s.Generates)),
                required.Length, required))
            {
                offers.Add(new Affordance(0, ChooseVerb, source.ObjectId, World.Scenario,
                    "spend", Costs: [new CostOption(source.ObjectId,
                        required.Length.ToString(), [required], Sources: sources)]));
            }
            offers.Add(new Affordance(1, ChooseVerb, source.ObjectId, World.Scenario, "effect"));
            return new Prompt(player, Question.Option, TimingPriority.Untimed,
                Steps.CardRevealed, $"{source.FaceId}: spend or resolve", false, offers);
        }
        if (choice.OperationName() == "chooseTopForHand")
        {
            var top = TopCards(
                world.Seats[player].Deck,
                EffectOf<AbilityEffect.ChooseTopForHand>(choice, cast).Count);
            return new Prompt(
                player, Question.Element, TimingPriority.Untimed,
                Steps.TurnAction, $"{source.FaceId}: choose a top card",
                Cancellable: false,
                top.Select(card => new Affordance(
                    card.ObjectId, ChooseVerb, card.ObjectId, player, card.FaceId)).ToList())
            {
                ExposesConcealedCandidates = true,
            };
        }
        if (choice.OperationName() == "chooseDiscardToShuffle")
        {
            var discard = world.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player);
            int max = Math.Min(
                EffectOf<AbilityEffect.ChooseDiscardToShuffle>(choice, cast).Maximum,
                discard.Cards.Select(card => world.Facts.Title(card.FaceId)).Distinct().Count());
            return new Prompt(
                player, Question.Element, TimingPriority.Untimed,
                Steps.TurnAction, $"{source.FaceId}: choose cards to shuffle",
                Cancellable: false,
                [new Affordance(
                    source.ObjectId, ChooseVerb, source.ObjectId, player, choice.OperationName(),
                    new TargetRequest(
                        [.. discard.Cards.Select(card => card.ObjectId)], 1, max))]);
        }
        if (choice.OperationName() == "thwartDifferentSchemes")
        {
            var schemes = Every(EffectOf<AbilityEffect.ThwartGroup>(choice, cast).Schemes, cast);
            bool aerial = Rules.State.Traits.Has(
                world, world.Seats[player].IdentityCard, "AERIAL", world.Facts);
            int count = aerial && schemes.Count > 1 ? 2 : 1;
            return new Prompt(
                player, Question.Element, TimingPriority.Untimed,
                Steps.TurnAction, $"{source.FaceId}: choose scheme{(count == 1 ? "" : "s")}",
                Cancellable: false,
                [new Affordance(
                    source.ObjectId, ChooseVerb, source.ObjectId, player, choice.OperationName(),
                    new TargetRequest(
                        [.. schemes.Select(card => card.ObjectId)], count, count))]);
        }
        if (choice.OperationName() == "makeTheCall")
        {
            var offers = AlliesInPlayerDiscards(world)
                .Select(ally => (Ally: ally, Sources: MakeTheCallSources(
                    world, player, source, ally)))
                .Where(candidate => Resources.Pays(
                    string.Concat(candidate.Sources.Select(generator => generator.Generates)),
                    Resources.Cost(candidate.Ally.FaceId, world.Facts, world.Players) ?? 0,
                    Resources.Required(world, candidate.Ally, world.Facts)))
                .Select(candidate => new Affordance(
                    candidate.Ally.ObjectId,
                    ChooseVerb,
                    candidate.Ally.ObjectId,
                    candidate.Ally.Owner,
                    candidate.Ally.FaceId,
                    Costs:
                    [
                        new CostOption(
                            candidate.Ally.ObjectId,
                            (Resources.Cost(
                                candidate.Ally.FaceId, world.Facts, world.Players) ?? 0).ToString(
                                System.Globalization.CultureInfo.InvariantCulture),
                            Resources.Required(world, candidate.Ally, world.Facts)
                                is { Length: > 0 } rule
                                ? [rule]
                                : null,
                            Sources: candidate.Sources),
                    ]))
                .ToList();
            return new Prompt(
                player, Question.Element, TimingPriority.Untimed,
                Steps.TurnAction, $"{source.FaceId}: choose an ally",
                Cancellable: false, offers);
        }
        if (choice.OperationName() == "legalPractice")
        {
            var hand = world.Seats[player].Hand.Cards
                .Where(card => card.ObjectId != source.ObjectId).ToList();
            var schemes = Every(EffectOf<AbilityEffect.ThwartGroup>(choice, cast).Schemes, cast)
                .Where(card => card.Tokens.GetValueOrDefault("k_threat") > 0).ToList();
            return new Prompt(player, Question.Element, TimingPriority.Untimed,
                Steps.TurnAction, $"{source.FaceId}: choose cards and a scheme", false,
                schemes.Select(scheme => new Affordance(
                    scheme.ObjectId, ChooseVerb, scheme.ObjectId, World.Scenario, scheme.FaceId,
                    new TargetRequest([.. hand.Select(card => card.ObjectId)], 1,
                        Math.Min(5, hand.Count)))).ToList());
        }
        var descriptions = cards ? default : EffectOf<AbilityEffect.Choose>(choice, cast).Descriptions;
        bool optionalTransition = !cards
            && ((AbilityEffect.Choose)choice).Options.Any(IsExplicitDecline);
        var affordances = cards
            ? LegalCardChoicesForContinuation(choice, cast)
                .Select(card => new Affordance(
                    Id: card.ObjectId,
                    Verb: ChooseVerb,
                    AnchorId: card.ObjectId,
                    AnchorPlayer: card.Owner,
                    Label: card.FaceId,
                    Description: ChoiceCardDescription(choice, card, cast)))
            : ((AbilityEffect.Choose)choice).Options
                .Select((option, index) => (Option: option, Index: index))
                .Where(candidate => OptionIsLegalForContinuation(
                    candidate.Option, cast, optionalTransition))
                .Select(candidate => new Affordance(
                    Id: candidate.Index,
                    Verb: ChooseVerb,
                    AnchorId: source.ObjectId,
                    AnchorPlayer: World.Scenario,
                    Label: candidate.Option.OperationName(),
                    Description: descriptions.IsDefaultOrEmpty ? null : descriptions[candidate.Index]));

        var offered = affordances.ToList();
        if (offered.Count == 0)
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' requires a choice and has no legal option");
        }

        return new Prompt(
            Player: player,
            Asking: cards ? Question.Element : Question.Option,
            When: TimingPriority.Untimed,
            Trigger: Steps.CardRevealed,
            Label: $"{source.FaceId}: choose {(cards ? "a card" : "an option")}",

            // Neither rule gives a way out. The ability is resolving, and one
            // of the things it offers is going to happen.
            Cancellable: false,
            Affordances: offered)
        {
            ExposesConcealedCandidates = cards
                && InspectsConcealedPile(EffectOf<AbilityEffect.ChooseCard>(choice, cast).From),
        };
    }

    private static string ChoiceCardDescription(
        AbilityEffect choice, Card card, Cast cast)
    {
        string title = cast.World.Facts.Title(card.FaceId);
        if (cast.World.Facts.Kind(card.FaceId) is CardKind.Hero or CardKind.AlterEgo)
        {
            return $"Select {cast.World.Seats[card.Owner].Name} → {title}";
        }

        AbilityEffect effect = EffectOf<AbilityEffect.ChooseCard>(choice, cast).Effect;
        if (ProjectedDamage(effect, cast) is { } projection)
        {
            Card attacker = cast.AbilityActor
                ?? cast.World.Seats[Resolver(cast)].IdentityCard;
            if (projection.IsAttack && Statuses.Afflicted(
                cast.World, cast.World.Facts, attacker, Statuses.Stunned))
            {
                return $"{title} · Stunned cancels this attack; no damage will be dealt";
            }

            long amount = ProjectedDamageAmount(projection.Amount, projection.IsAttack, cast);
            string consequence = projection.IsAttack
                ? Damage.PreviewAttack(
                    cast.World, cast.World.Facts, attacker, cast.Source, card, amount,
                    grantsOverkill: projection.Overkill)
                : Damage.PreviewDamage(
                    cast.World, cast.World.Facts, cast.Source, card, amount);
            return $"{title} · {consequence}";
        }

        if (effect is AbilityEffect.RemoveThreat threat)
        {
            long current = card.Tokens.GetValueOrDefault("k_threat");
            long removed = Math.Min(current, Amount(threat.Amount, cast));
            long result = current - removed;
            long threshold = cast.World.Facts.PrintedValue(
                card.FaceId, "TargetThreat", cast.World.Players);
            return threshold > 0
                ? $"{title} · {current}/{threshold} → {result}/{threshold} threat"
                : $"{title} · {current} → {result} threat";
        }

        return title;
    }

    private static (AbilityNumber Amount, bool IsAttack, bool Overkill)? ProjectedDamage(
        AbilityEffect? effect, Cast cast, bool isAttack = false)
    {
        if (effect is AbilityEffect.Power { Kind: AbilityPowerKind.Attack } power)
        {
            return ProjectedDamage(power.Effect, cast, isAttack: true);
        }
        if (effect is AbilityEffect.Conditional conditional)
        {
            return ProjectedDamage(Test(conditional.Test, cast) ? conditional.Then : conditional.Else, cast, isAttack);
        }
        if (effect is AbilityEffect.Sequence sequence)
        {
            // A later amount can depend on what an earlier effect discovers
            // (Repulsor Blast is the Core example). Only the leading effect is
            // already knowable at this decision; do not skip over state changes.
            return ProjectedDamage(sequence.Effects.FirstOrDefault(), cast, isAttack);
        }
        return effect switch
        {
            AbilityEffect.AttackDamage damage => (damage.Amount, true, damage.Overkill),
            AbilityEffect.Damage damage => (damage.Amount, isAttack, false),
            _ => null,
        };
    }

    private static long ProjectedDamageAmount(
        AbilityNumber damage, bool isAttack, Cast cast)
    {
        long amount = SaturatingSum(
            Amount(damage, cast),
            [EventModifier(cast, "eventDamage")]);
        if (!isAttack)
        {
            return amount;
        }
        return SaturatingSum(amount, [EventModifier(cast, "attackDamage")]);
    }

    /// <inheritdoc/>
    public Prompt? Choosing(
        World world, Card source, int player, int stoppedAt, AbilityType? tier,
        bool finalStep, bool eachPlayerFrame, bool finalPlayer) =>
        Choosing(world, source, player, stoppedAt, tier, finalStep);

    /// <summary>
    /// The question an assignment asks — <c>rr:indirect-damage.1</c>.
    /// </summary>
    /// <remarks>
    /// One answer naming a character per point, so the same character may
    /// appear more than once: assigning three damage to one hero is three
    /// entries. <c>rr:choose-game-element.3.1</c>'s "the same target cannot be
    /// chosen multiple times" is about <i>targets</i>, and this is a division
    /// rather than a target list.
    /// </remarks>
    private static Prompt Sharing(
        Card source, int player, AbilityEffect.IndirectDamage choice, Cast cast)
    {
        long amount = Amount(choice.Amount, cast);
        var eligible = Assignable(choice.Among, cast);

        // `rr:indirect-damage.3.1` -- never more than would defeat a character,
        // so an assignment can be short of the amount when the table has less
        // room than the card has damage.
        long share = Math.Min(amount, eligible.Sum(card => Room(cast, card)));

        return new Prompt(
            Player: player,
            Asking: Question.Element,
            When: TimingPriority.Untimed,
            Trigger: Steps.CardRevealed,
            Label: $"{source.FaceId}: assign {share} damage",
            Cancellable: false,
            Affordances:
            [
                new Affordance(
                    Id: source.ObjectId,
                    Verb: ChooseVerb,
                    AnchorId: source.ObjectId,
                    AnchorPlayer: World.Scenario,
                    Label: "indirectDamage",
                    Targets: new TargetRequest(
                        Legal: [.. eligible.Select(card => card.ObjectId)],
                        Min: (int)share,
                        Max: (int)share,
                        Rule: "rr:indirect-damage.1",
                        AllowRepeated: true,
                        MaximumOccurrences: eligible.ToDictionary(
                            card => card.ObjectId,
                            card => checked((int)Room(cast, card))))),
            ]);
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier = null)
        => Chose(world, source, player, stoppedAt, input, tier, finalStep: false);

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep)
        => ChoseCore(
            world, source, player, stoppedAt, input, tier, finalStep,
            eachPlayerFrame: false, finalPlayer: false);

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer)
        => ChoseCore(
            world, source, player, stoppedAt, input, tier, finalStep,
            eachPlayerFrame, finalPlayer, eventTrigger: null);

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer,
        string trigger)
        => ChoseCore(
            world, source, player, stoppedAt, input, tier, finalStep,
            eachPlayerFrame, finalPlayer, trigger);

    private List<GameEvent> ChoseCore(
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer,
        string? eventTrigger = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(input);

        var choice = Choice(world, source, player, stoppedAt, tier);
        var persisted = ContinuationStep(world, source, stoppedAt, tier);
        var continuation = world.Agenda.Current is { What: Steps.ChooseOption, Plan: true }
            && world.Agenda.Occurrence is { } live
            && live.Is(Steps.TurnAction)
                ? live
                : null;
        var cast = Resuming(
            world, source, player, tier, finalStep,
            persisted?.AbilityOccurrence ?? continuation) with
        {
            EachPlayerFrame = eachPlayerFrame,
            FinalPlayer = finalPlayer,
            AbilityPlayer = persisted?.AbilityPlayer ?? player,
            EventTrigger = eventTrigger,
            GainedKeywords = world.Agenda.Current is
                { What: Steps.ChooseOption, SurgeGained: true }
                    ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal),
        };
        RestorePersisted(cast, persisted);
        if (persisted is
            { AbilityOrdinal: >= 0, AbilityPath: { } path } current)
        {
            cast.RestoreAbility(current.AbilityOrdinal, path, current.AbilityFace);
            RestorePathBindings(cast, path);
        }
        if (cast.AbilityOrdinal >= 0)
        {
            cast.TrackResolution(cast.AbilityOrdinal);
        }
        cast.At(Math.Max(0, stoppedAt - 1));
        cast.SetContinuation(persisted?.AbilityHasContinuation ?? On(source).Any(ability =>
            (tier is null || ability.Trigger.Timing == tier)
            && ability.Effect.OperationName() == "seq"
            && OrderedEffects(ability.Effect).Length > stoppedAt));

        if (choice.OperationName() == "and")
        {
            var effects = OrderedEffects(choice).ToList();
            var legal = Enumerable.Range(0, effects.Count).ToHashSet();
            if (input.IsDecline
                || input.Affordance != source.ObjectId
                || input.Targets.Count != effects.Count
                || input.Targets.Distinct().Count() != effects.Count
                || input.Targets.Any(index => !legal.Contains(index)))
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' requires one permutation of all "
                    + $"{effects.Count} simultaneous effects");
            }

            bool outerContinuation = cast.HasContinuation;
            for (int position = 0; position < input.Targets.Count; position++)
            {
                int index = input.Targets[position];
                string remaining = string.Join(',', input.Targets.Skip(position + 1));
                string completed = string.Join(',', input.Targets.Take(position));
                cast.SetContinuation(
                    outerContinuation || position < input.Targets.Count - 1);
                RunChild(effects[index], $"and:{index}:{remaining}:{completed}", cast);
                if (cast.Suspended)
                {
                    return cast.Events;
                }
            }
            cast.SetContinuation(outerContinuation);

            return Continue(source, cast, stoppedAt);
        }

        if (choice.OperationName() is "enemyAttacks" or "enemySchemes")
        {
            var activation = ActivationOf(choice, cast);
            var legal = ActivationCandidates(activation, cast)
                .Select(enemy => enemy.ObjectId)
                .ToList();
            if (input.IsDecline
                || input.Affordance != source.ObjectId
                || input.Targets.Count != legal.Count
                || input.Targets.Distinct().Count() != legal.Count
                || input.Targets.Any(id => !legal.Contains(id)))
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' requires one permutation of all "
                    + $"{legal.Count} enemy activations");
            }

            cast.Results["dynamicActivationOrderSet"] = 1;
            for (int index = 0; index < input.Targets.Count; index++)
            {
                cast.Results[$"dynamicActivationOrder:{input.Targets[index]}"] = index;
            }
            Activate(activation, choice, cast);
            if (cast.Suspended)
            {
                return cast.Events;
            }
            return Continue(source, cast, stoppedAt);
        }

        if (choice.OperationName() == "resolveSpecials")
        {
            var legal = Every(EffectOf<AbilityEffect.CardAction>(choice, cast).Selection, cast)
                .Select(card => card.ObjectId)
                .ToHashSet();
            if (input.Targets.Count != legal.Count
                || input.Targets.Distinct().Count() != legal.Count
                || input.Targets.Any(id => !legal.Contains(id)))
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' requires one permutation of all {legal.Count} Special abilities");
            }

            int round = world.Agenda.Current?.Round ?? 0;
            foreach (var (id, index) in input.Targets.Select((id, index) => (id, index)))
            {
                world.Agenda.Then(new PhaseStep(
                    Steps.ResolveSpecial, round, index + 1, Subject: id, Seat: player,
                    Plan: true, FinalStep: index == input.Targets.Count - 1));
            }
            if (input.Targets.Count > 0)
            {
                cast.ResolveEffect();
            }

            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "payOrEffect")
        {
            if (input.Affordance == 0)
            {
                string required = EffectOf<AbilityEffect.PayOrEffect>(choice, cast).Resources;
                CardPlay.Spend(world, world.Facts, [world.Seats[player].Hand], input.Spent,
                    required.Length, required, -1, player, cast.Events);
                cast.ResolveEffect();
            }
            else if (input.Affordance == 1)
            {
                RunChild(EffectFollowing(choice), "choice:otherwise", cast);
                if (cast.Suspended)
                {
                    return cast.Events;
                }
            }
            else
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' did not offer option {input.Affordance}");
            }
            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "payOrExhaust")
        {
            if (input.Affordance == 0)
            {
                string required = EffectOf<AbilityEffect.PayOrEffect>(choice, cast).Resources;
                CardPlay.Spend(
                    world, world.Facts, [world.Seats[player].Hand], input.Spent,
                    required.Length, required, itself: -1, player, cast.Events);
                cast.ResolveEffect();
            }
            else if (input.Affordance == 1)
            {
                RunChild(EffectFollowing(choice), "choice:otherwise", cast);
                if (cast.Suspended)
                {
                    return cast.Events;
                }
            }
            else
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' offers spend or exhaust, not option {input.Affordance}");
            }

            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "chooseTopForHand")
        {
            var deck = world.Seats[player].Deck;
            var top = TopCards(deck, EffectOf<AbilityEffect.ChooseTopForHand>(choice, cast).Count);
            var selected = top.FirstOrDefault(card => card.ObjectId == input.Affordance)
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' did not offer card {input.Affordance} among its top cards");
            var hand = world.Seats[player].Hand;
            foreach (var card in top)
            {
                if (card == selected)
                {
                    World.MoveToTop(card, hand);
                    cast.Events.Add(new CardsMoved(
                        Places.Reference(deck), Places.Reference(hand),
                        [new Landing(card.ObjectId, hand.Cards.Count - 1)])
                    {
                        Trigger = cast.Trigger, Verb = "Add_To_Hand",
                    });
                }
                else
                {
                    Rules.Play.Discard.Card(world, card, cast.Trigger, cast.Events);
                }
            }
            cast.ResolveEffect();

            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "chooseDiscardToShuffle")
        {
            var discard = world.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player);
            var selected = input.Targets.Select(id =>
                discard.Cards.FirstOrDefault(card => card.ObjectId == id)
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' cannot shuffle card {id} from that discard pile"))
                .ToList();
            int max = EffectOf<AbilityEffect.ChooseDiscardToShuffle>(choice, cast).Maximum;
            if (selected.Count < 1
                || selected.Count > max
                || selected.Select(card => world.Facts.Title(card.FaceId)).Distinct().Count()
                    != selected.Count)
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' requires one to {max} cards with different titles");
            }
            foreach (var card in selected)
            {
                World.MoveToTop(card, world.Seats[player].Deck);
            }
            world.Shuffle(world.Seats[player].Deck);
            cast.ResolveEffect();
            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "thwartDifferentSchemes")
        {
            var legal = Every(EffectOf<AbilityEffect.ThwartGroup>(choice, cast).Schemes, cast);
            var selected = input.Targets.Select(id =>
                legal.FirstOrDefault(card => card.ObjectId == id)
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' cannot thwart scheme {id}"))
                .ToList();
            bool aerial = Rules.State.Traits.Has(
                world, world.Seats[player].IdentityCard, "AERIAL", world.Facts);
            int expected = aerial && legal.Count > 1 ? 2 : 1;
            if (selected.Count != expected || selected.Distinct().Count() != selected.Count)
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' requires {expected} different scheme target(s)");
            }

            // rr:then: the second Crisis Interdiction removal is dependent on
            // the first removal fully resolving. The choice is simultaneous,
            // but only the first selected scheme belongs to the pre-then
            // effect, so determine that outcome in isolation before the power
            // receives the targets it will actually resolve against.
            cast.Choose(selected[0]);
            var priorTargets = cast.PowerTargets;
            cast.SetPowerTargets([selected[0]]);
            var power = ((AbilityEffect.ThwartGroup)choice).Thwart;
            bool firstFullyResolves = ResolutionOf(
                EffectBody(power), cast) == ResolutionOutcome.Full;
            cast.SetPowerTargets(priorTargets);
            IReadOnlyList<Card> resolving = firstFullyResolves
                ? selected
                : [selected[0]];
            SchedulePower(
                power, cast, BasicPowers.ThwartVerb,
                selected[0], resolving, -1);
            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "makeTheCall")
        {
            var ally = AlliesInPlayerDiscards(world)
                .FirstOrDefault(card => card.ObjectId == input.Affordance)
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' did not offer ally {input.Affordance}");
            long cost = Resources.Cost(ally.FaceId, world.Facts, world.Players) ?? 0;
            CardPlay.Spend(
                world, world.Facts, [world.Seats[player].Hand], input.Spent,
                cost, Resources.Required(world, ally, world.Facts),
                source.ObjectId, player, cast.Events, payingFor: ally);
            CardPlay.PutAllyIntoPlay(
                world, world.Facts, cast.Abilities, ally, player, cast.Trigger, cast.Events);
            cast.ResolveEffect();
            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "legalPractice")
        {
            var scheme = Every(EffectOf<AbilityEffect.ThwartGroup>(choice, cast).Schemes, cast)
                .FirstOrDefault(card => card.ObjectId == input.Affordance)
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' did not offer scheme {input.Affordance}");
            var hand = world.Seats[player].Hand;
            if (input.Targets.Count is < 1 or > 5
                || input.Targets.Distinct().Count() != input.Targets.Count)
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' requires one to five distinct hand cards");
            }
            foreach (int id in input.Targets)
            {
                var card = world.Cards[id];
                if (card.Area != hand || card.ObjectId == source.ObjectId)
                {
                    throw new RulesNotImplementedException(
                        $"card {id} cannot be discarded for '{source.FaceId}'");
                }
                Rules.Play.Discard.Card(world, card, CardPlay.Verb, cast.Events);
            }
            cast.ResolveEffect();
            cast.Choose(scheme);
            SchedulePower(
                ((AbilityEffect.ThwartGroup)choice).Thwart, cast, BasicPowers.ThwartVerb,
                scheme, [scheme], input.Targets.Count);
            return Continue(source, cast, stoppedAt);
        }

        if (choice.OperationName() == "indirectDamage")
        {
            var damage = (AbilityEffect.IndirectDamage)choice;
            var eligible = Assignable(damage.Among, cast);
            long amount = Amount(damage.Amount, cast);
            long expected = Math.Min(amount, eligible.Sum(card => Room(cast, card)));
            if (input.Targets.Count != expected)
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' requires {expected} indirect damage assignment(s) "
                    + $"and {input.Targets.Count} were chosen");
            }
            // One point per entry, so a character named three times takes
            // three. `rr:indirect-damage.3` resolves the whole assignment at
            // once, which is why the counts are gathered before any of it is
            // dealt.
            var share = new Dictionary<int, long>();
            foreach (int id in input.Targets)
            {
                var card = eligible.FirstOrDefault(card => card.ObjectId == id)
                    ?? throw new RulesNotImplementedException(
                        $"card {id} cannot be assigned indirect damage from "
                        + $"'{source.FaceId}'");
                long assigned = share.GetValueOrDefault(card.ObjectId) + 1;
                if (assigned > Room(cast, card))
                {
                    throw new RulesNotImplementedException(
                        $"card {id} has room for {Room(cast, card)} indirect damage "
                        + $"and was assigned {assigned} from '{source.FaceId}'");
                }
                share[card.ObjectId] = assigned;
            }

            Resolve(choice, cast, share);
            return Continue(source, cast, stoppedAt);
        }


        if (choice.OperationName() == "chooseCard")
        {
            cast.ChooseSelection(
                LegalCardChoicesForContinuation(choice, cast)
                    .FirstOrDefault(card => card.ObjectId == input.Affordance)
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' did not offer card {input.Affordance} to choose"));

            if (cast.HasPendingDependency)
            {
                var effect = EffectBody(choice);
                if (!ActiveChoices(effect, cast).Any())
                {
                    cast.CompletePendingDependency(ResolutionOf(effect, cast));
                }
            }
            RunChild(EffectBody(choice), "choice:effect", cast);
            if (cast.Suspended)
            {
                return cast.Events;
            }
            return Continue(source, cast, stoppedAt);
        }

        var options = ((AbilityEffect.Choose)choice).Options.ToList();
        if (input.IsDecline || input.Affordance < 0 || input.Affordance >= options.Count)
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' offers {options.Count} options and none of them is "
                + $"number {input.Affordance}");
        }

        bool optionalTransition = options.Any(IsExplicitDecline);
        if (!OptionIsLegalForContinuation(
                options[input.Affordance], cast, optionalTransition))
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' cannot choose illegal option {input.Affordance}");
        }

        if (cast.HasPendingDependency)
        {
            cast.CompletePendingDependency(ResolutionOf(options[input.Affordance], cast));
        }
        RunChild(
            options[input.Affordance], $"choice:option:{input.Affordance}", cast);
        if (cast.Suspended)
        {
            return cast.Events;
        }
        return Continue(source, cast, stoppedAt);
    }

    /// <summary>Whether one listed option may be chosen right now.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:choose-option.1</c>: an encounter-card option that requires a
    /// target is unavailable when it has no valid target. Printed card kind
    /// tells which half of the rule applies; ownership cannot, because a
    /// scenario-specific player card can begin the game owned by the scenario.
    /// </para>
    /// <para>
    /// <c>rr:choose-option.2</c>: a player-card option must be able to resolve
    /// at least partially. A sequence therefore needs one resolvable effect;
    /// an encounter-card sequence needs every target it requires. The empty
    /// sequence is the explicit decline branch used to express “may”.
    /// </para>
    /// </remarks>
    private static bool OptionIsLegal(AbilityEffect option, Cast cast)
    {
        // Eligibility and support are separate questions. Preserve the
        // printed-option legality rules below, but make an unsupported option
        // raise while the enclosing ability is still being offered.
        bool canInitiate = CanInitiate(option, cast);
        return canInitiate
            && TargetLegalityOf(option, cast) != TargetLegality.Invalid
            && (!IsPlayerCard(cast) || CanPartiallyResolve(option, cast));
    }

    private static bool OptionIsLegalForContinuation(
        AbilityEffect option, Cast cast, bool requireStateChange = false)
    {
        bool locallyLegal = OptionIsLegal(option, cast)
            && (!requireStateChange
                || IsExplicitDecline(option)
                || CanPartiallyResolve(option, cast));
        if (!locallyLegal || cast.AbilityPath.Count == 0)
        {
            return locallyLegal;
        }
        cast = cast.ForReachability(cast.Reachability);
        var prior = cast.CaptureChosen();
        ResolutionOutcome? pendingOutcome = cast.HasPendingDependency
            ? ResolutionOf(option, cast)
            : null;
        var before = new BindingCandidateState(
            prior is null ? [] : [prior.Card], prior is null);
        var outcomes = BindingCandidatesAfter(option, cast, before);
        var scope = cast.ForReachability(cast.Reachability with
        {
            PriorSteps = cast.Reachability.PriorSteps.Add(option),
            FilteringContinuationOption = true,
        });
        return ContinuationCanResolve(outcomes, scope, pendingOutcome);
    }

    private static bool IsExplicitDecline(AbilityEffect option) =>
        option.OperationName() == "seq" && !OrderedEffects(option).Any();

    /// <summary>Cards that meet both a choice's selector and its nested effect.</summary>
    /// <remarks>
    /// <c>rr:target.2.2</c> makes “choose” a target selection, so the selector
    /// is only the first half of legality. Binding each candidate before
    /// asking about the nested effect keeps offering, prompting, and answer
    /// validation on the same decision.
    /// </remarks>
    private static List<Card> LegalCardChoices(AbilityEffect choice, Cast cast)
    {
        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        var legal = new List<Card>();
        try
        {
            foreach (var card in Every(EffectOf<AbilityEffect.ChooseCard>(choice, cast).From, cast))
            {
                cast.ChooseSelection(card);
                var effect = EffectBody(choice);
                if (TargetLegalityOf(effect, cast) != TargetLegality.Invalid)
                {
                    legal.Add(card);
                }
            }
        }
        finally
        {
            cast.RestoreChosen(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
        return legal;
    }

    /// <summary>Legal targets that also leave the persisted sequence resumable.</summary>
    private static List<Card> LegalCardChoicesForContinuation(
        AbilityEffect choice, Cast cast)
    {
        var legal = LegalCardChoices(choice, cast);
        if (cast.AbilityPath.Count == 0)
        {
            return legal;
        }
        return legal.Where(candidate =>
        {
            var scope = cast.ForReachability(cast.Reachability);
            scope.ChooseSelection(candidate);
            var effect = EffectBody(choice);
            ResolutionOutcome? pendingOutcome = scope.HasPendingDependency
                && !ActiveChoices(effect, scope).Any()
                    ? ResolutionOf(effect, scope)
                    : null;
            var outcomes = BindingCandidatesAfter(
                effect, scope,
                new BindingCandidateState([candidate], MayBeEmpty: false));
            scope = scope.ForReachability(scope.Reachability with
            {
                PriorSteps = cast.Reachability.PriorSteps.Add(effect),
                FilteringContinuationOption = true,
            });
            return ContinuationCanResolve(outcomes, scope, pendingOutcome);
        }).ToList();
    }

    private static bool ContinuationCanResolve(
        BindingCandidateState outcomes, Cast cast,
        ResolutionOutcome? pendingOutcome)
    {
        bool CanResolve(Card? binding)
        {
            var scope = cast.ForReachability(cast.Reachability with
            {
                CheckingInitiation = true,
                PriorBindingCandidates = binding is null ? [] : [binding],
                PriorBindingMayBeEmpty = binding is null,
                PriorBindingMayChange = false,
            });
            scope.ChooseSelection(binding);
            var remaining = RemainingContinuationSteps(scope, pendingOutcome);
            if (remaining.Count == 0)
            {
                return true;
            }
            // An option cannot invalidate a later singular lookup, even inside
            // a condition whose false result would otherwise be a legal no-op.
            var sensitiveAreas = new HashSet<DeckType>();
            foreach (var step in remaining)
                CollectSingularAreaDependencies(step, scope, sensitiveAreas);
            if (sensitiveAreas.Count > 0
                && EffectsMayChangeAnyArea(scope.Reachability.PriorSteps, sensitiveAreas, scope))
            {
                return false;
            }
            var continuation = new AbilityEffect.Sequence([.. remaining]);
            return CanInitiateSequence(continuation, scope)
                && TargetLegalityOf(continuation, scope) != TargetLegality.Invalid;
        }
        return outcomes.Cards.Any(CanResolve)
            || outcomes.MayBeEmpty && CanResolve(null);
    }

    /// <summary>Sequence siblings reached after the currently persisted choice.</summary>
    private static List<AbilityEffect> RemainingContinuationSteps(
        Cast cast, ResolutionOutcome? pendingOutcome)
    {
        if (cast.AbilityOrdinal < 0 || cast.AbilityPath.Count == 0
            || cast.Abilities is not AbilityRunner runner)
        {
            return [];
        }
        var root = runner.AbilitiesOn(cast.Source, cast.AbilityFace)
            .Where(ability => cast.Tier is null || ability.Trigger.Timing == cast.Tier)
            .ElementAtOrDefault(cast.AbilityOrdinal)?.Effect;
        if (root is null)
        {
            return [];
        }

        var remaining = new List<AbilityEffect>();
        for (int position = cast.AbilityPath.Count - 1; position >= 0; position--)
        {
            string frame = cast.AbilityPath[position];
            var parts = frame.Split(':');
            if (parts[0] == "eachPlayer" && cast.EachPlayerFrame
                && !cast.FinalPlayer)
            {
                break;
            }
            var prefix = cast.AbilityPath.Take(position).ToList();
            var parent = prefix.Count == 0 ? root : NodeAtPath(root, prefix);
            if (!frame.StartsWith("seq:", StringComparison.Ordinal)
                || !int.TryParse(
                    frame.AsSpan(4),
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out int index))
            {
                if (parts[0] is "then" or "otherwise" && parts.Length >= 2
                    && parts[1] == "effect")
                {
                    ResolutionOutcome? outcome = parts.Length >= 3
                        && Enum.TryParse(parts[2], out ResolutionOutcome recorded)
                            ? recorded
                            : pendingOutcome;
                    var required = parts[0] == "then"
                        ? ResolutionOutcome.Full : ResolutionOutcome.None;
                    if (outcome == required)
                    {
                        remaining.Add(ContinuationChild(parent, parts[0]));
                    }
                }
                else if (parts[0] == "and")
                {
                    var effects = OrderedEffects(parent).ToList();
                    remaining.AddRange(
                        ValidRemaining(parent, parts, frame).Select(index => effects[index]));
                }
                else if (parts[0] == "forEach" && parts.Length >= 3
                    && long.TryParse(
                        parts[1], System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out long iteration)
                    && long.TryParse(
                        parts[2], System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out long count))
                {
                    var repeated = EffectBody(parent);
                    for (long next = iteration + 1; next < count; next++)
                    {
                        remaining.Add(repeated);
                    }
                }
                else if (parts[0] == "eachTime" && parts.Length >= 3
                    && long.TryParse(
                        parts[1], System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out long eachTimeIteration)
                    && long.TryParse(
                        parts[2], System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out long eachTimeCount)
                    && eachTimeIteration + 1 < eachTimeCount
                    && LaterEachTimePromptIsGuaranteed(
                        parent, cast, eachTimeIteration, eachTimeCount))
                {
                    // A later matching discard is already visible on top of the
                    // deterministic encounter deck and must ask another legal
                    // question. That later answer replaces this binding before
                    // the outer continuation resumes.
                    break;
                }
                continue;
            }
            if (parent.OperationName() == "seq")
            {
                remaining.AddRange(OrderedEffects(parent).Skip(index + 1));
            }
        }
        return remaining;
    }

    private static bool LaterEachTimePromptIsGuaranteed(
        AbilityEffect eachTime, Cast cast, long iteration, long count)
    {
        long remaining = count - iteration - 1;
        var future = cast.World.AreaOf(DeckType.EncounterDeck).Cards
            .Reverse()
            .Take((int)Math.Min(remaining, int.MaxValue))
            .ToList();
        if (future.Count < remaining)
        {
            // A reset would shuffle the discard pile. Its result is
            // deterministic at runtime but projecting it here would consume
            // the game's wire-format RNG, so it cannot guarantee a prompt.
            return false;
        }

        var prior = cast.Altered;
        try
        {
            foreach (var card in future)
            {
                cast.BindAlteration(card);
                var body = EffectFollowing(eachTime);
                if (Test(EachTimeOf(eachTime, cast).When, cast)
                    && ActiveChoices(body, cast).Any()
                    && CanInitiate(body, cast))
                {
                    return true;
                }
            }
            return false;
        }
        finally
        {
            if (prior is not null)
            {
                cast.BindAlteration(prior);
            }
        }
    }

    /// <summary>Whether the source has a player-card face.</summary>
    private static bool IsPlayerCard(Cast cast) =>
        IsPlayerCard(cast.World.Facts, cast.Source);

    /// <summary>Whether a card face belongs to a player rather than the scenario.</summary>
    private static bool IsPlayerCard(ICardFacts facts, Card card) => AbilityCardQueries.IsPlayerCard(facts, card);

    private static int ControllerOf(World world, Card card) => AbilityCardQueries.ControllerOf(world, card);

}
