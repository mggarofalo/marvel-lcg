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

        if (choice.Kind == "indirectDamage")
        {
            return Sharing(source, player, choice, cast);
        }

        if (choice.Kind == "and")
        {
            int count = Nodes(choice.Argument).Count();
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

        if (choice.Kind is "enemyAttacks" or "enemySchemes")
        {
            var enemies = ActivationCandidates(choice, cast);
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

        bool cards = choice.Kind == "chooseCard";

        // `rr:choose-option` and `rr:choose-game-element` are two questions and
        // not one: an option is a branch the card lists, an element is a card
        // on the board. `Question` has told them apart since before anything
        // asked either.
        if (choice.Kind == "resolveSpecials")
        {
            var upgrades = Every(choice.Require("cards"), cast);
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
                        Label: choice.Kind,
                        Targets: new TargetRequest(
                            [.. upgrades.Select(card => card.ObjectId)],
                            upgrades.Count,
                            upgrades.Count)),
                ]);
        }
        if (choice.Kind == "payOrExhaust")
        {
            string required = Word(choice.Require("resources"));
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
            AbilityNode otherwise = Tree(choice.Require("otherwise"));
            if (otherwise.Kind != "exhaust"
                || Every(otherwise.Argument, cast).Any(card => card.Ready))
            {
                offers.Add(new Affordance(
                    1, ChooseVerb, source.ObjectId, World.Scenario, "exhaust"));
            }
            return new Prompt(
                player, Question.Option, TimingPriority.Untimed,
                Steps.CardRevealed, $"{source.FaceId}: spend or exhaust",
                Cancellable: false, offers);
        }
        if (choice.Kind == "payOrEffect")
        {
            string required = Word(choice.Require("resources"));
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
        if (choice.Kind == "chooseTopForHand")
        {
            var top = TopCards(
                world.Seats[player].Deck,
                (int)Number(choice.Require("count")));
            return new Prompt(
                player, Question.Element, TimingPriority.Untimed,
                Steps.TurnAction, $"{source.FaceId}: choose a top card",
                Cancellable: false,
                top.Select(card => new Affordance(
                    card.ObjectId, ChooseVerb, card.ObjectId, player, card.FaceId)).ToList());
        }
        if (choice.Kind == "chooseDiscardToShuffle")
        {
            var discard = world.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player);
            int max = Math.Min(
                (int)Number(choice.Require("max")),
                discard.Cards.Select(card => world.Facts.Title(card.FaceId)).Distinct().Count());
            return new Prompt(
                player, Question.Element, TimingPriority.Untimed,
                Steps.TurnAction, $"{source.FaceId}: choose cards to shuffle",
                Cancellable: false,
                [new Affordance(
                    source.ObjectId, ChooseVerb, source.ObjectId, player, choice.Kind,
                    new TargetRequest(
                        [.. discard.Cards.Select(card => card.ObjectId)], 1, max))]);
        }
        if (choice.Kind == "thwartDifferentSchemes")
        {
            var schemes = Every(choice.Require("schemes"), cast);
            bool aerial = Rules.State.Traits.Has(
                world, world.Seats[player].IdentityCard, "AERIAL", world.Facts);
            int count = aerial && schemes.Count > 1 ? 2 : 1;
            return new Prompt(
                player, Question.Element, TimingPriority.Untimed,
                Steps.TurnAction, $"{source.FaceId}: choose scheme{(count == 1 ? "" : "s")}",
                Cancellable: false,
                [new Affordance(
                    source.ObjectId, ChooseVerb, source.ObjectId, player, choice.Kind,
                    new TargetRequest(
                        [.. schemes.Select(card => card.ObjectId)], count, count))]);
        }
        if (choice.Kind == "makeTheCall")
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
        if (choice.Kind == "legalPractice")
        {
            var hand = world.Seats[player].Hand.Cards
                .Where(card => card.ObjectId != source.ObjectId).ToList();
            var schemes = Every(choice.Require("schemes"), cast)
                .Where(card => card.Tokens.GetValueOrDefault("k_threat") > 0).ToList();
            return new Prompt(player, Question.Element, TimingPriority.Untimed,
                Steps.TurnAction, $"{source.FaceId}: choose cards and a scheme", false,
                schemes.Select(scheme => new Affordance(
                    scheme.ObjectId, ChooseVerb, scheme.ObjectId, World.Scenario, scheme.FaceId,
                    new TargetRequest([.. hand.Select(card => card.ObjectId)], 1,
                        Math.Min(5, hand.Count)))).ToList());
        }
        var affordances = cards
            ? LegalCardChoicesForContinuation(choice, cast)
                .Select(card => new Affordance(
                    Id: card.ObjectId,
                    Verb: ChooseVerb,
                    AnchorId: card.ObjectId,
                    AnchorPlayer: card.Owner,
                    Label: card.FaceId))
            : Nodes(choice.Require("options"))
                .Select((option, index) => (Option: option, Index: index))
                .Where(candidate => OptionIsLegalForContinuation(
                    candidate.Option, cast))
                .Select(candidate => new Affordance(
                    Id: candidate.Index,
                    Verb: ChooseVerb,
                    AnchorId: source.ObjectId,
                    AnchorPlayer: World.Scenario,
                    Label: candidate.Option.Kind));

        var offered = affordances.ToList();
        if (offered.Count == 0)
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' requires a choice and has no legal option");
        }

        // Most questions belong to the player resolving the ability. A card
        // can instead name who makes this choice; the DSL chooses the
        // `chooser` spelling because prompt ownership is an engine wire choice,
        // not Rules Reference terminology. The resolving player remains on the
        // continuation, so "you" inside the chosen effect does not change.
        int chooser = choice.Field("chooser") is { } namedChooser
            ? Seat(namedChooser, cast)
            : player;

        return new Prompt(
            Player: chooser,
            Asking: cards ? Question.Element : Question.Option,
            When: TimingPriority.Untimed,
            Trigger: Steps.CardRevealed,
            Label: $"{source.FaceId}: choose {(cards ? "a card" : "an option")}",

            // Neither rule gives a way out. The ability is resolving, and one
            // of the things it offers is going to happen.
            Cancellable: false,
            Affordances: offered);
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
        Card source, int player, AbilityNode choice, Cast cast)
    {
        long amount = Amount(choice.Require("amount"), cast);
        var eligible = Assignable(choice.Require("among"), cast);

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
                    Label: choice.Kind,
                    Targets: new TargetRequest(
                        Legal: [.. eligible.Select(card => card.ObjectId)],
                        Min: (int)share,
                        Max: (int)share,
                        Rule: "rr:indirect-damage.1",
                        AllowRepeated: true)),
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
            && ability.Effect.Kind == "seq"
            && Nodes(ability.Effect.Argument).Count() > stoppedAt));

        if (choice.Kind == "and")
        {
            var effects = Nodes(choice.Argument).ToList();
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

        if (choice.Kind is "enemyAttacks" or "enemySchemes")
        {
            var legal = ActivationCandidates(choice, cast)
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
            Activate(
                choice,
                cast,
                choice.Kind == "enemyAttacks" ? Steps.Attack : Steps.Scheme);
            if (cast.Suspended)
            {
                return cast.Events;
            }
            return Continue(source, cast, stoppedAt);
        }

        if (choice.Kind == "resolveSpecials")
        {
            var legal = Every(choice.Require("cards"), cast)
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
        if (choice.Kind == "payOrEffect")
        {
            if (input.Affordance == 0)
            {
                string required = Word(choice.Require("resources"));
                CardPlay.Spend(world, world.Facts, [world.Seats[player].Hand], input.Spent,
                    required.Length, required, -1, player, cast.Events);
                cast.ResolveEffect();
            }
            else if (input.Affordance == 1)
            {
                RunChild(Tree(choice.Require("otherwise")), "choice:otherwise", cast);
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
        if (choice.Kind == "payOrExhaust")
        {
            if (input.Affordance == 0)
            {
                string required = Word(choice.Require("resources"));
                CardPlay.Spend(
                    world, world.Facts, [world.Seats[player].Hand], input.Spent,
                    required.Length, required, itself: -1, player, cast.Events);
                cast.ResolveEffect();
            }
            else if (input.Affordance == 1)
            {
                RunChild(Tree(choice.Require("otherwise")), "choice:otherwise", cast);
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
        if (choice.Kind == "chooseTopForHand")
        {
            var deck = world.Seats[player].Deck;
            var top = TopCards(deck, (int)Number(choice.Require("count")));
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
        if (choice.Kind == "chooseDiscardToShuffle")
        {
            var discard = world.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player);
            var selected = input.Targets.Select(id =>
                discard.Cards.FirstOrDefault(card => card.ObjectId == id)
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' cannot shuffle card {id} from that discard pile"))
                .ToList();
            int max = (int)Number(choice.Require("max"));
            if (selected.Count is < 1 || selected.Count > 3
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
        if (choice.Kind == "thwartDifferentSchemes")
        {
            var legal = Every(choice.Require("schemes"), cast);
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
            var power = Tree(choice.Require("power"));
            bool firstFullyResolves = ResolutionOf(
                Tree(power.Require("effect")), cast) == ResolutionOutcome.Full;
            cast.SetPowerTargets(priorTargets);
            IReadOnlyList<Card> resolving = firstFullyResolves
                ? selected
                : [selected[0]];
            SchedulePower(
                power, cast, BasicPowers.ThwartVerb,
                selected[0], resolving, -1);
            return Continue(source, cast, stoppedAt);
        }
        if (choice.Kind == "makeTheCall")
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
        if (choice.Kind == "legalPractice")
        {
            var scheme = Every(choice.Require("schemes"), cast)
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
                Tree(choice.Require("power")), cast, BasicPowers.ThwartVerb,
                scheme, [scheme], input.Targets.Count);
            return Continue(source, cast, stoppedAt);
        }

        if (choice.Kind == "indirectDamage")
        {
            var eligible = Assignable(choice.Require("among"), cast);
            long amount = Amount(choice.Require("amount"), cast);
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


        if (choice.Kind == "chooseCard")
        {
            cast.ChooseSelection(
                LegalCardChoicesForContinuation(choice, cast)
                    .FirstOrDefault(card => card.ObjectId == input.Affordance)
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' did not offer card {input.Affordance} to choose"));

            if (cast.HasPendingDependency)
            {
                var effect = Tree(choice.Require("effect"));
                if (!ActiveChoices(effect, cast).Any())
                {
                    cast.CompletePendingDependency(ResolutionOf(effect, cast));
                }
            }
            RunChild(Tree(choice.Require("effect")), "choice:effect", cast);
            if (cast.Suspended)
            {
                return cast.Events;
            }
            return Continue(source, cast, stoppedAt);
        }

        var options = Nodes(choice.Require("options")).ToList();
        if (input.IsDecline || input.Affordance < 0 || input.Affordance >= options.Count)
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' offers {options.Count} options and none of them is "
                + $"number {input.Affordance}");
        }

        if (!OptionIsLegalForContinuation(options[input.Affordance], cast))
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
    private static bool OptionIsLegal(AbilityNode option, Cast cast)
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
        AbilityNode option, Cast cast)
    {
        bool locallyLegal = OptionIsLegal(option, cast);
        if (!locallyLegal || cast.AbilityPath.Count == 0)
        {
            return locallyLegal;
        }

        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        var priorSteps = cast.PriorSteps;
        bool priorFiltering = cast.FilteringContinuationOption;
        try
        {
            ResolutionOutcome? pendingOutcome = cast.HasPendingDependency
                ? ResolutionOf(option, cast)
                : null;
            var before = new BindingCandidateState(
                prior is null ? [] : [prior.Card], prior is null);
            var outcomes = BindingCandidatesAfter(option, cast, before);
            cast.SetPriorSteps([.. priorSteps, option]);
            cast.SetFilteringContinuationOption(true);
            return ContinuationCanResolve(outcomes, cast, pendingOutcome);
        }
        finally
        {
            cast.RestoreChosen(prior);
            cast.RestorePlayerSelection(priorSelection);
            cast.SetPriorSteps(priorSteps);
            cast.SetFilteringContinuationOption(priorFiltering);
        }
    }

    /// <summary>Cards that meet both a choice's selector and its nested effect.</summary>
    /// <remarks>
    /// <c>rr:target.2.2</c> makes “choose” a target selection, so the selector
    /// is only the first half of legality. Binding each candidate before
    /// asking about the nested effect keeps offering, prompting, and answer
    /// validation on the same decision.
    /// </remarks>
    private static List<Card> LegalCardChoices(AbilityNode choice, Cast cast)
    {
        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        var legal = new List<Card>();
        try
        {
            foreach (var card in Every(choice.Require("from"), cast))
            {
                cast.ChooseSelection(card);
                var effect = Tree(choice.Require("effect"));
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
        AbilityNode choice, Cast cast)
    {
        var legal = LegalCardChoices(choice, cast);
        if (cast.AbilityPath.Count == 0)
        {
            return legal;
        }
        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        var priorSteps = cast.PriorSteps;
        bool priorFiltering = cast.FilteringContinuationOption;
        try
        {
            return legal.Where(candidate =>
            {
                cast.ChooseSelection(candidate);
                var effect = Tree(choice.Require("effect"));
                ResolutionOutcome? pendingOutcome = cast.HasPendingDependency
                    && !ActiveChoices(effect, cast).Any()
                        ? ResolutionOf(effect, cast)
                        : null;
                var outcomes = BindingCandidatesAfter(
                    effect, cast,
                    new BindingCandidateState([candidate], MayBeEmpty: false));
                cast.SetPriorSteps([.. priorSteps, effect]);
                cast.SetFilteringContinuationOption(true);
                return ContinuationCanResolve(outcomes, cast, pendingOutcome);
            }).ToList();
        }
        finally
        {
            cast.RestoreChosen(prior);
            cast.RestorePlayerSelection(priorSelection);
            cast.SetPriorSteps(priorSteps);
            cast.SetFilteringContinuationOption(priorFiltering);
        }
    }

    private static bool ContinuationCanResolve(
        BindingCandidateState outcomes, Cast cast,
        ResolutionOutcome? pendingOutcome)
    {
        var outerCandidates = cast.PriorBindingCandidates;
        bool outerMayBeEmpty = cast.PriorBindingMayBeEmpty;
        bool outerBindingMayChange = cast.PriorBindingMayChange;
        bool outerChecking = cast.CheckingInitiation;
        bool CanResolve(Card? binding)
        {
            cast.ChooseSelection(binding);
            cast.SetPriorBindingCandidates(binding is null ? [] : [binding]);
            cast.SetPriorBindingMayBeEmpty(binding is null);
            cast.SetPriorBindingMayChange(false);
            var remaining = RemainingContinuationSteps(cast, pendingOutcome);
            if (remaining.Count == 0)
            {
                return true;
            }
            var continuation = new AbilityNode(
                "seq",
                new AbilityValue.List(remaining.Select(NodeValue).ToList()));
            return CanInitiateSequence(continuation, cast)
                && TargetLegalityOf(continuation, cast) != TargetLegality.Invalid;
        }
        try
        {
            cast.SetCheckingInitiation(true);
            return outcomes.Cards.Any(CanResolve)
                || outcomes.MayBeEmpty && CanResolve(null);
        }
        finally
        {
            cast.SetPriorBindingCandidates(outerCandidates);
            cast.SetPriorBindingMayBeEmpty(outerMayBeEmpty);
            cast.SetPriorBindingMayChange(outerBindingMayChange);
            cast.SetCheckingInitiation(outerChecking);
        }
    }

    private static AbilityValue NodeValue(AbilityNode node) =>
        new AbilityValue.Map(new Dictionary<string, AbilityValue>(
            StringComparer.Ordinal)
        {
            [node.Kind] = node.Argument,
        });

    /// <summary>Sequence siblings reached after the currently persisted choice.</summary>
    private static List<AbilityNode> RemainingContinuationSteps(
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

        var remaining = new List<AbilityNode>();
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
                        remaining.Add(Tree(parent.Require(parts[0])));
                    }
                }
                else if (parts[0] == "and")
                {
                    var effects = Nodes(parent.Argument).ToList();
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
                    var repeated = Tree(parent.Require("effect"));
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
            if (parent.Kind == "seq")
            {
                remaining.AddRange(Nodes(parent.Argument).Skip(index + 1));
            }
        }
        return remaining;
    }

    private static bool LaterEachTimePromptIsGuaranteed(
        AbilityNode eachTime, Cast cast, long iteration, long count)
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
                var body = Tree(eachTime.Require("then"));
                if (Test(Tree(eachTime.Require("when")), cast)
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
    private static bool IsPlayerCard(ICardFacts facts, Card card)
    {
        var kind = facts.Kind(card.FaceId);

        // Player side schemes are not yet a modelled kind and answer Unknown.
        // Unlike an unknown encounter card, one created in a player's deck has
        // that player as its owner, which preserves the rule's distinction.
        return kind is CardKind.AlterEgo
                or CardKind.Hero
                or CardKind.Ally
                or CardKind.Event
                or CardKind.Resource
                or CardKind.Support
                or CardKind.Upgrade
            || (kind == CardKind.Unknown && card.Owner != World.Scenario);
    }

    /// <summary>The card's current controller, falling back to its owner out of play.</summary>
    /// <remarks>
    /// <c>rr:ownership-and-control.5</c> moves a changed-control player card to
    /// its controller's play area. Ownership remains on <see cref="Card.Owner"/>,
    /// so the two facts must not be read from the same field.
    /// </remarks>
    private static int ControllerOf(World world, Card card) =>
        IsPlayerCard(world.Facts, card)
        && DeckTypes.IsInPlay(card.Area.Type)
        && card.Area.PlayArea.IsPlayers
            ? card.Area.PlayArea.Player
            : card.Owner;

}
