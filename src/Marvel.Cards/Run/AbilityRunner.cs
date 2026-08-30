using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>
/// Runs authored card abilities. The one way a card's text enters the engine.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is what replaces a class per card.</b> There was one — a switch on
/// printed id, three cards deep and growing — and it was the "cards as scripts"
/// inversion this port exists to undo (<c>docs/migration.md</c>). A card is now
/// a row in <c>datasets/abilities/abilities.json</c>, and adding one is
/// authoring data rather than compiling code.
/// </para>
/// <para>
/// The vocabulary is small and every gap is loud. A node nothing implements
/// throws naming the node; a card nobody has authored throws naming the card.
/// Growing the engine means adding a case here and growing the game means adding
/// a row there, and the two are different activities on purpose.
/// </para>
/// <para>
/// See <c>docs/card-dsl.md</c> for the design this is the first executable
/// piece of, and <c>docs/enemy-attacks.md</c> for the cards it currently runs.
/// </para>
/// </remarks>
/// <param name="book">The authored cards.</param>
public sealed class AbilityRunner(AbilityBook book) : ICardAbilities
{
    private readonly AbilityBook book = book;

    // An activation is an agenda operation, while the sentence that initiated
    // it is a card operation. The stable agenda id is the join between them.
    // Entries live only until that activation's completion sentinel calls back.
    private readonly Dictionary<int, List<ActivationEffect>> activationEffects = [];

    // Which printed faces carry a constant ability. `Constant` is asked about
    // every card in play every time anything reads the effect list, and all but
    // a handful of cards answer nothing -- so the common answer is a set lookup
    // rather than a walk of the book.
    //
    // **A shortcut and nothing else.** Deleting it is an equivalent mutant: the
    // loop below finds the same abilities, just slower. It is here because
    // reading a stat goes through the effect list, and the digest reads every
    // stat of every card.
    private readonly HashSet<string> constant = new(
        book.Abilities
            .Where(ability => ability.Trigger.Timing == AbilityType.Constant)
            .Select(ability => ability.Card),
        StringComparer.Ordinal);

    /// <summary>The verb an option carries on the wire.</summary>
    public const string ChooseVerb = "Choose_Option";

    private static readonly string[] Branches = ["then", "else"];

    private static readonly DeckType[] Owned = [DeckType.UpgradesArea, DeckType.SupportsArea];

    // A facedown Ultron Drone retains the underlying player-card face id for
    // the state digest, but `rr:in-play-and-out-of-play.5` and `.13` make that
    // facedown card text inactive. Every authored-ability entry point goes
    // through this boundary so no trigger, action, constant, boost, or query
    // can accidentally execute the hidden card.
    private IEnumerable<CardAbility> On(Card card) =>
        FacedownDrones.Is(card) ? [] : book.On(card.FaceId);

    /// <summary>The authored cards, whether or not they do anything.</summary>
    public IReadOnlySet<string> Authored => book.Authored;

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> EntersPlay(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        var events = new List<GameEvent>();
        var occurrence = new Occurrence(
            0, [Steps.CardEntersPlay], Subject: card.ObjectId,
            Player: ControllerOf(world, card));
        foreach (var ability in On(card).Where(ability =>
            ability.Trigger.Timing == AbilityType.WhenRevealed
            && string.Equals(
                ability.Trigger.Event, Steps.CardEntersPlay,
                StringComparison.Ordinal)))
        {
            var cast = new Cast(
                world, card, occurrence, ControllerOf(world, card), events, this)
            {
                Tier = ability.Trigger.Timing,
            };
            TrackResolution(cast, ability);
            Run(ability, cast);
            cast.CompleteResolution();
        }

        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> ActivationCompleted(World world, EnemyActivation result)
    {
        ArgumentNullException.ThrowIfNull(world);

        var events = new List<GameEvent>();
        var enemy = world.Cards[result.Enemy];
        if (result.Made)
        {
            foreach (var ability in On(enemy).Where(ability =>
                ability.Trigger.Timing == AbilityType.ForcedResponse
                && string.Equals(
                    ability.Trigger.Event, "WhenActivationCompleted",
                    StringComparison.Ordinal)))
            {
                var cast = new Cast(
                    world, enemy,
                    new Occurrence(
                        0, ["WhenActivationCompleted"],
                        Actor: enemy.ObjectId, Player: result.Player),
                    result.Player, events, this)
                {
                    Tier = ability.Trigger.Timing,
                };
                TrackResolution(cast, ability);
                Run(ability, cast);
                cast.CompleteResolution();
            }
        }

        if (activationEffects.Remove(result.Id, out var delayed))
        {
            foreach (var effect in delayed)
            {
                var delayedCast = new Cast(
                    world,
                    world.Cards[effect.Source],
                    new Occurrence(
                        0, ["WhenActivationCompleted"],
                        Actor: result.Enemy, Player: effect.Player),
                    effect.Player,
                    events,
                    this)
                {
                    Tier = effect.Tier,
                    AbilityActor = effect.AbilityActor >= 0
                        ? world.Cards[effect.AbilityActor]
                        : null,
                };
                delayedCast.Results["activationDamage"] = result.DamageDealt;
                delayedCast.Results["activationThreat"] = result.ThreatPlaced;
                delayedCast.Results["activationMade"] = result.Made ? 1 : 0;
                if (effect.Altered >= 0)
                {
                    delayedCast.BindAlteration(world.Cards[effect.Altered]);
                }
                Run(effect.Effect, delayedCast);
            }
        }

        if (world.Agenda.CompleteActivationWait(result) is { } continuation)
        {
            events.AddRange(ResumeAbility(world, continuation));
        }

        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> ResumeAbility(World world, PhaseStep continuation)
    {
        var source = continuation.Subject >= 0 && continuation.Subject < world.Cards.Count
            ? world.Cards[continuation.Subject]
            : throw new RulesNotImplementedException(
                $"activation continuation has no card at object id {continuation.Subject}");
        if (continuation.AbilityOrdinal < 0 || continuation.AbilityPath is not { } path)
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' has an incomplete activation continuation");
        }

        var cast = Resuming(
            world, source, continuation.Seat, continuation.Tier, continuation.FinalStep,
            continuation.AbilityOccurrence) with
        {
            EachPlayerFrame = continuation.EachPlayerFrame,
            FinalPlayer = continuation.FinalPlayer,
            AbilityPlayer = continuation.AbilityPlayer,
            EventTrigger = continuation.Trigger,
            GainedKeywords = continuation.SurgeGained
                ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal),
        };
        cast.RestoreAbility(
            continuation.AbilityOrdinal, path, continuation.AbilityFace);
        cast.TrackResolution(continuation.AbilityOrdinal);
        RestorePersisted(cast, continuation);
        if (cast.Results.GetValueOrDefault("activationMade") > 0)
        {
            cast.ResolveEffect();
        }
        RestorePathBindings(cast, path);
        var root = AbilityAt(
            source, continuation.Tier, continuation.AbilityOrdinal,
            continuation.AbilityFace).Effect;
        int eachPlayer = path.ToList().FindIndex(frame =>
            frame.StartsWith("eachPlayer:", StringComparison.Ordinal));
        ResumeAfter(
            root, path, cast,
            stopBefore: continuation.EachPlayerFrame && !continuation.FinalPlayer
                ? eachPlayer
                : -1);
        cast.CompleteResolution();
        DiscardEvent(source, cast);
        return cast.Events;
    }

    /// <inheritdoc/>
    public void ResolveCardAttack(
        World world, CharacterAttack attack, Occurrence occurrence, List<GameEvent> events) =>
        ResolvePower(
            world, attack.Source, attack.Enemy, attack.Player, attack.AbilityIndex,
            attack.PowerOrdinal, attack.ResumeFrom, attack.FinalStep,
            attack.Targets ?? [attack.Enemy], attack.Amount, null,
            attack.Trigger, attack.SurgeGained, occurrence, events,
            BasicPowers.AttackVerb, attack.AbilityPath, attack.AbilityFace,
            attack.AbilityResults, attack.AbilityOccurrence, attack.Discarded,
            attack.EachPlayerFrame, attack.FinalPlayer, attack.AbilityPlayer,
            attack.AbilityHasContinuation, attack.AbilityActor);

    /// <inheritdoc/>
    public void ResolveCardThwart(
        World world, CharacterThwart thwart, Occurrence occurrence, List<GameEvent> events) =>
        ResolvePower(
            world, thwart.Source, thwart.Scheme, thwart.Player, thwart.AbilityIndex,
            thwart.PowerOrdinal, thwart.ResumeFrom, thwart.FinalStep,
            thwart.Targets ?? [thwart.Scheme], thwart.Amount, thwart.ImminentThreat,
            thwart.Trigger, thwart.SurgeGained, occurrence, events,
            BasicPowers.ThwartVerb, thwart.AbilityPath, thwart.AbilityFace,
            thwart.AbilityResults, thwart.AbilityOccurrence, thwart.Discarded,
            thwart.EachPlayerFrame, thwart.FinalPlayer, thwart.AbilityPlayer,
            thwart.AbilityHasContinuation, thwart.AbilityActor);

    private void ResolvePower(
        World world, int sourceId, int targetId, int player, int abilityIndex,
        int powerOrdinal, int resumeFrom, bool finalStep, IReadOnlyList<int> targets,
        long powerAmount, ThreatPlacement? imminentThreat, string eventTrigger,
        bool surgeGained,
        Occurrence occurrence,
        List<GameEvent> events, string power, IReadOnlyList<string>? abilityPath = null,
        string abilityFace = "", IReadOnlyDictionary<string, long>? abilityResults = null,
        Occurrence? abilityOccurrence = null, IReadOnlyList<int>? discarded = null,
        bool eachPlayerFrame = false, bool finalPlayer = false, int abilityPlayer = -1,
        bool abilityHasContinuation = false, int abilityActor = -1)
    {
        if (sourceId < 0 || sourceId >= world.Cards.Count)
        {
            throw new RulesNotImplementedException(
                $"card {power.ToLowerInvariant()} has no reconstructable source");
        }

        var source = world.Cards[sourceId];
        var abilities = AbilitiesOn(source, abilityFace).ToList();
        var ability = abilities.ElementAtOrDefault(abilityIndex)
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no ability {abilityIndex} for its "
                + power.ToLowerInvariant());
        var wrappers = PowerNodes(ability.Effect, power).ToList();
        var wrapper = wrappers.ElementAtOrDefault(powerOrdinal)
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' ability {abilityIndex} has no {power.ToLowerInvariant()} "
                + $"wrapper {powerOrdinal}");
        var effect = Tree(wrapper.Require("effect"));
        var cast = new Cast(
            world, source, abilityOccurrence ?? occurrence, player, events, this)
        {
            Tier = ability.Trigger.Timing,
            FinalStep = finalStep,
            Power = power,
            PowerAmount = powerAmount,
            ImminentThreat = imminentThreat,
            EventTrigger = eventTrigger,
            PowerTargets = [.. targets.Select(id => world.Cards[id])],
            PowerActor = occurrence.Actor >= 0 ? world.Cards[occurrence.Actor] : null,
            EachPlayerFrame = eachPlayerFrame,
            FinalPlayer = finalPlayer,
            AbilityPlayer = abilityPlayer,
            AbilityActor = abilityActor >= 0 ? world.Cards[abilityActor] : null,
            GainedKeywords = surgeGained
                ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal),
        };
        cast.Choose(world.Cards[targetId]);
        RestorePersisted(cast, discarded, abilityResults);
        if (abilityPath is not null)
        {
            RestorePathBindings(cast, abilityPath);
        }
        if (SuspendsPowerEffect(effect, cast))
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' suspends inside a {power.ToLowerInvariant()}, "
                + "which is not implemented");
        }
        int ordinal = abilities
            .Where(candidate => candidate.Trigger.Timing == ability.Trigger.Timing)
            .ToList()
            .IndexOf(ability);
        cast.RestoreAbility(ordinal, abilityPath ?? [], abilityFace);
        cast.TrackResolution(ordinal);
        var attackModifiers = power == BasicPowers.AttackVerb
            ? EventModifierEffects(cast, "attackDamage")
            : [];
        Run(effect, cast);

        // A modifier to "an attack" lasts through every damage node belonging
        // to that attack, then is consumed once. This is deliberately at the
        // wrapper boundary rather than in generic dealDamage: one attack may
        // damage several characters, while a later wrapper is a later attack.
        foreach (var modifier in attackModifiers)
        {
            world.Effects.Use(modifier);
        }

        if (power == BasicPowers.AttackVerb)
        {
            var attacker = cast.PowerActor ?? world.Seats[player].IdentityCard;
            if (!Keywords.Has(world, attacker, Keywords.Ranged, world.Facts))
            {
                foreach (var target in cast.Attacked.DistinctBy(card => card.ObjectId))
                {
                    Damage.Retaliate(world, world.Facts, target, attacker, cast.Trigger, events);
                }
            }
        }

        // The labelled power owns `chosen` while its effect runs. The outer
        // ability's earlier selection is a different binding and becomes
        // current again only when that outer continuation resumes.
        RestorePersistedChosen(cast, abilityResults, overwrite: true);

        if (!cast.Suspended && abilityPath is not null)
        {
            int eachPlayer = abilityPath.ToList().FindIndex(frame =>
                frame.StartsWith("eachPlayer:", StringComparison.Ordinal));
            ResumeAfter(
                ability.Effect, abilityPath, cast,
                stopBefore: eachPlayerFrame && !finalPlayer ? eachPlayer : -1);
        }
        else if (!cast.Suspended && resumeFrom >= 0)
        {
            if (ability.Effect.Kind != "seq")
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' resumes a {power.ToLowerInvariant()} outside a sequence");
            }
            Sequence(ability.Effect, cast, resumeFrom);
        }
        cast.CompleteResolution();
        DiscardEvent(source, cast);
    }

    /// <inheritdoc/>
    public string ResourcesGeneratedBy(World world, Card source, Card? payingFor)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);

        string printed = Resources.GeneratedBy(source.FaceId, world.Facts);
        if (payingFor is null)
        {
            return printed;
        }

        string classes = world.Facts.Attributes(payingFor.FaceId)
            .GetValueOrDefault("Class", string.Empty);
        bool doubles = On(source).Any(ability =>
            ability.Trigger.Timing == AbilityType.Constant
            && ability.Effect.Kind == "doubleResourceFor"
            && classes.Split(';').Contains(
                Word(ability.Effect.Argument), StringComparer.Ordinal));

        return doubles ? printed + printed : printed;
    }

    /// <inheritdoc/>
    public DefenderChoice Defenders(
        World world, EnemyAttack attack, IReadOnlyList<Card> candidates)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(candidates);

        var enemy = world.Cards[attack.Enemy];
        bool requiresControlledAlly = On(enemy).Any(ability =>
            ability.Trigger.Timing == AbilityType.Constant
            && ability.Effect.Kind == "requireAllyDefender");
        if (!requiresControlledAlly)
        {
            return new DefenderChoice(candidates, Required: false);
        }

        var allies = candidates.Where(card =>
            card.Ready
            && card.Area.PlayArea == PlayArea.Of(attack.Player)
            && world.Facts.Kind(card.FaceId) == CardKind.Ally).ToList();
        return allies.Count > 0
            ? new DefenderChoice(allies, Required: true)
            : new DefenderChoice(candidates, Required: false);
    }

    /// <inheritdoc/>
    public bool CanRemoveThreat(World world, Card scheme, int ignoredSource = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(scheme);

        foreach (var card in world.Cards.Where(card =>
                     card.ObjectId != ignoredSource && DeckTypes.IsInPlay(card.Area.Type)))
        {
            foreach (var ability in On(card).Where(ability =>
                ability.Trigger.Timing == AbilityType.Constant))
            {
                if (ProhibitsThreatRemoval(
                        ability.Effect,
                        new Cast(world, card, new Occurrence(0, []),
                            ControllerOf(world, card), [], this),
                        scheme))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyList<PendingAbility> Waiting(
        World world, Occurrence occurrence, WindowKind window)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);

        var waiting = new List<PendingAbility>();
        foreach (var card in world.Cards)
        {
            // `rr:ability.1` -- a card's ability functions while the card is in
            // play. Being attached to something is not the same as being in
            // play: the recorded Tough hangs off Rhino from a zone that is not.
            bool eventInHand = world.Facts.Kind(card.FaceId) == CardKind.Event
                && card.Owner >= 0
                && card.Area == world.Seats[card.Owner].Hand;
            if (!DeckTypes.IsInPlay(card.Area.Type) && !eventInHand)
            {
                continue;
            }

            var written = On(card).ToList();
            for (int index = 0; index < written.Count; index++)
            {
                var ability = written[index];
                IEnumerable<int> players;
                if (!ability.AnyPlayer)
                {
                    players = [Controller(world, ability, card, occurrence)];
                }
                else if (ability.Trigger.Player == AbilityPlayers.TriggerPlayer
                    && occurrence.Player >= 0)
                {
                    // The permission is broad, but the trigger is not:
                    // trigger.player is the seat the occurrence happened to.
                    players = [occurrence.Player];
                }
                else
                {
                    players = world.PlayerOrder;
                }
                foreach (int controller in players)
                {
                    if (!Answers(
                            world, ability, card, occurrence, window,
                            ability.AnyPlayer ? controller : null))
                    {
                        continue;
                    }

                    // `rr:initiating-abilities.step.2` -- "if the card or ability
                    // has a form requirement (for example, 'Hero form only' or
                    // 'Hero Action'), the form of the player playing that card or
                    // initiating that ability is checked now." Step 2 is about any
                    // ability, not only an action: Prelate Armor prints a *Hero
                    // Response*, and an alter-ego cannot initiate one.
                    if (!InForm(world, controller, ability.Trigger.Form, card))
                    {
                        continue;
                    }

                    var eligibility = new Cast(
                        world, card, occurrence, controller, [], this);
                    eligibility.SetPaymentMayMutate(
                        ability.Cost is not null
                        || world.Facts.Kind(card.FaceId) == CardKind.Event);
                    if ((ability.When is not null && !Test(ability.When, eligibility))
                        || (controller >= 0 && !CanInitiate(ability, eligibility)))
                    {
                        continue;
                    }

                    // `rr:initiating-abilities.step.3` -- the cost and "the
                    // player's ability to pay them" are one step, and only "if both
                    // conditions are met" do the later steps happen. So an ability
                    // nobody can pay for is not an offer that fails at step 5; it
                    // never reaches the window at all.
                    if (!Payable(world, card, controller, ability.Cost)
                        || !EventPayable(world, card, controller, ability)
                        || !Available(world, card, ability, occurrence))
                    {
                        continue;
                    }

                    int ordinal = written.Take(index).Count(candidate =>
                        candidate.Trigger.Timing == ability.Trigger.Timing);
                    waiting.Add(new PendingAbility(
                        card.ObjectId, ability.Trigger.Timing, controller, ordinal));
                }
            }
        }

        return waiting;
    }

    /// <inheritdoc/>
    public Affordance Describe(World world, PendingAbility ability)
    {
        ArgumentNullException.ThrowIfNull(world);

        var card = world.Cards[ability.Card];
        var found = Pending(card, ability);

        // The ability's own name is the verb: an affordance for Foresight is
        // offered as `Foresight`, so a client has something to render without
        // knowing what the ability does. One string does for both fields
        // because the engine carries one -- see the remarks on `Affordance.Id`.
        var price = CombinedPrice(world, card, ability.Player, found);
        return new Affordance(
            Id: ability.Card,
            Verb: found.Name,
            AnchorId: ability.Card,
            AnchorPlayer: ability.Player,
            Label: found.Name,
            Targets: Asking(world, ability.Player, found.Cost),
            Costs: price is null ? null : [price]);
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen) =>
        Resolve(world, occurrence, ability, paying, chosen, values: null, allocations: null);

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);
        ArgumentNullException.ThrowIfNull(paying);
        ArgumentNullException.ThrowIfNull(chosen);

        var card = world.Cards[ability.Card];
        var found = Pending(card, ability);

        var events = new List<GameEvent>();

        // **Who "you" is, which is not who may trigger it.**
        // `PendingAbility.Player` is control -- `rr:ability.8` lets any player
        // use an optional ability on an encounter card, so an encounter card's
        // is the scenario. That is the right answer to "whose opportunity is
        // this" and the wrong one to "who does the card mean by *you*".
        //
        // `rr:you-your.7` is explicit for the case this arrived on: "for
        // abilities that trigger 'after [enemy] attacks you,' 'you' refers to
        // the attacked player, even if that player defended with an ally." The
        // attacked player is the occurrence's, so an ability on a card nobody
        // owns resolves as the player the occurrence happened to. `.16` is not
        // in the way -- it says an encounter card's ability is not performed by
        // that player's identity, which is about who acts, not about who the
        // word points at.
        int resolving = ability.Player >= 0 ? ability.Player : occurrence.Player;
        var cast = new Cast(world, card, occurrence, resolving, events, this)
        {
            Tier = found.Trigger.Timing,
        };
        cast.SetPaymentMayMutate(
            found.Cost is not null || world.Facts.Kind(card.FaceId) == CardKind.Event);

        if (!Available(world, card, found, occurrence))
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' has reached its printed maximum for this "
                + "ability's period");
        }

        // **A forced ability is resolved, never offered, and so never priced.**
        // `rr:forced.1` makes it resolve when its condition is met, which is
        // why `Offering.Work` runs it without asking anybody anything -- and a
        // payment is an answer to a question. `rr:initiating-abilities.step.5`
        // would still have to be paid, out of a hand nobody chose from. No card
        // in the pool prints one; the day one does, the window has to ask.
        if (AbilityTypes.IsMandatory(found.Trigger.Timing) && found.Cost is not null)
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' has a mandatory ability with a cost, and a mandatory ability "
                + "resolves without any player being asked to pay one");
        }

        if (resolving >= 0 && !CanInitiate(found, cast))
        {
            // A mandatory ability with no valid target does not become a
            // question and cannot initiate. The window has still reached it,
            // so resolving it means doing nothing rather than stopping the
            // timing sequence on an impossible instruction.
            if (AbilityTypes.IsMandatory(found.Trigger.Timing))
            {
                return events;
            }
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' cannot initiate this ability in the current state");
        }

        // `rr:initiating-abilities` keeps the steps apart, and step 5 pays
        // before step 6 resolves. Nothing here can abort for want of resources,
        // because step 3 -- `Payable`, when the ability was offered -- already
        // asked whether the cost could be paid at all. What it cannot check is
        // that the player named a payment that works, and `CardPlay.Spend`
        // refuses one that does not.
        ValidatePayment(found.Cost, paying, chosen, values, cast);
        PayEvent(card, paying, cast, found.Effect, allocations, found.Cost);
        if (world.Facts.Kind(card.FaceId) == CardKind.Event
            && ResourceRequirement(found.Cost, card).Length > 0)
        {
            PayNonResourceCosts(found.Cost, paying, chosen, values, cast);
        }
        else
        {
            Pay(found.Cost, paying, chosen, values, cast);
        }
        Use(world, card, found, occurrence);
        if (world.Facts.Kind(card.FaceId) == CardKind.Event)
        {
            occurrence.BeginCard(card.ObjectId, [ability]);
        }
        cast.RestoreAbility(ability.Ordinal, []);
        cast.TrackResolution(ability.Ordinal);
        Run(found, cast);
        cast.CompleteResolution();
        DiscardEvent(card, cast);
        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player) =>
        WhenRevealed(
            world, card, player,
            new Occurrence(0, [Steps.CardRevealed], Subject: card.ObjectId, Player: player));

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenRevealed(
        World world, Card card, int player, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(occurrence);

        if (!book.Authored.Contains(card.FaceId))
        {
            // Authored-and-does-nothing is a different thing from nobody having
            // read the card, and only one of them is safe to treat as silence.
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was revealed and no ability data is written for it; "
                + $"this engine has {book.Authored.Count} authored card(s)");
        }

        var reveals = On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenRevealed)
            .Select((ability, ordinal) => (Ability: ability, Ordinal: ordinal))
            .Where(entry => string.Equals(
                entry.Ability.Trigger.Event, Steps.CardRevealed,
                StringComparison.Ordinal))
            .ToList();
        var addresses = reveals.Select(entry => new PendingAbility(
            card.ObjectId, AbilityType.WhenRevealed, player, entry.Ordinal)).ToList();
        if (world.Facts.Kind(card.FaceId) == CardKind.Treachery)
        {
            occurrence.BeginCard(card.ObjectId, addresses);
        }

        var events = new List<GameEvent>();
        if (CancelWhenRevealed(world, card, player, occurrence))
        {
            return events;
        }

        // One reveal can contain several authored abilities. A non-numeric
        // keyword gained by more than one of them is still one keyword, so the
        // casts share which keyword grants have already resolved.
        var gainedKeywords = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (ability, ordinal) in reveals)
        {
            // `rr:ability.step.3` -- "When Revealed" *is* the occurrence, not a
            // window around it. An interrupt or a response to a card being
            // revealed is a different ability and reaches the board through
            // `Waiting`, so matching on the condition alone would run it twice.
            var cast = new Cast(world, card, occurrence, player, events, this)
            {
                Tier = ability.Trigger.Timing,
                GainedKeywords = gainedKeywords,
            };
            cast.RestoreAbility(ordinal, []);
            cast.TrackResolution(ordinal);
            Run(ability, cast);
            cast.CompleteResolution();
        }

        return events;
    }

    /// <inheritdoc/>
    public bool CancelWhenRevealed(
        World world, Card card, int player, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(occurrence);

        var authored = On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenRevealed)
            .Select((ability, ordinal) => (ability, ordinal))
            .Where(entry => string.Equals(
                entry.ability.Trigger.Event, Steps.CardRevealed,
                StringComparison.Ordinal))
            .Select(entry => new PendingAbility(
                card.ObjectId, AbilityType.WhenRevealed, player, entry.ordinal));
        var addresses = authored
            .Concat(Reveal.KeywordAbilities(world, world.Facts, card, player))
            .ToList();
        var cancellation = world.Effects.Active().FirstOrDefault(effect =>
            string.Equals(effect.Kind, "cancelWhenRevealed", StringComparison.Ordinal)
            && effect.Affects == card.ObjectId);
        bool mayBeCanceled = world.Facts.Kind(card.FaceId)
            is not (CardKind.EncounterVillain or CardKind.MainScheme);
        if (!mayBeCanceled || cancellation is null || !world.Effects.Use(cancellation))
        {
            return false;
        }

        if (world.Facts.Kind(card.FaceId) == CardKind.Treachery)
        {
            occurrence.BeginCard(card.ObjectId, addresses);
        }
        foreach (var address in addresses)
        {
            occurrence.Cancel(address);
        }
        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Boost(World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        // **Not "is the card authored" but "is this half of it".** A card with
        // two abilities at two tiers -- `01168` Sweeping Swoop has a "When
        // Revealed" and a "Boost" -- would otherwise pass on the strength of
        // the half somebody had written, and the other half would go back to
        // being silent.
        var boosts = On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.Boost)
            .ToList();

        if (boosts.Count == 0)
        {
            // **The star gates the complaint, not the run.** The printed
            // `Boost` attribute counts icons and `rr:boost-boost-icon.1` says a
            // star is not one, so a card with an ability and a card without
            // carry the same number and only the text box can tell them apart.
            // Asked here rather than first, so that the text box cannot veto
            // authored data.
            return world.Facts.HasBoostAbility(card.FaceId)
                ? throw new RulesNotImplementedException(
                    $"card '{card.FaceId}' was turned faceup as a boost card and prints a "
                    + "'Boost' ability that no ability data is written for")
                : [];
        }

        var events = new List<GameEvent>();
        var occurrence = new Occurrence(
            0, [Steps.CardRevealed], Subject: card.ObjectId, Player: player);

        foreach (var (ability, ordinal) in boosts.Select((ability, ordinal) =>
                     (ability, ordinal)))
        {
            // `rr:ability` puts a "Boost" ability at the occurrence tier, like
            // "When Revealed": it is the thing happening rather than a window
            // around it, so there is nothing to offer and nothing to decline.
            var cast = new Cast(world, card, occurrence, player, events, this)
            {
                Tier = ability.Trigger.Timing,
            };
            cast.RestoreAbility(ordinal, []);
            cast.TrackResolution(ordinal);
            Run(ability, cast);
            cast.CompleteResolution();
        }

        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> ResolveSpecial(
        World world, Card card, int player, bool finalStep)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        var ability = On(card).SingleOrDefault(candidate =>
            candidate.Trigger.Timing == AbilityType.Special)
            ?? throw new RulesNotImplementedException(
                $"card '{card.FaceId}' has no authored Special ability");
        var events = new List<GameEvent>();
        var cast = new Cast(
            world, card,
            new Occurrence(0, [Steps.ResolveSpecial], Subject: card.ObjectId, Player: player),
            player, events, this)
        {
            Tier = AbilityType.Special,
            FinalStep = finalStep,
        };
        if (CanInitiate(ability, cast))
        {
            cast.RestoreAbility(0, []);
            cast.TrackResolution(0);
            Run(ability, cast);
            cast.CompleteResolution();
        }
        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);

        var sources = new List<ResourceSource>();
        foreach (var card in Triggerable(world, player).ToList())
        {
            foreach (var ability in On(card))
            {
                var eligibility = new Cast(
                    world,
                    card,
                    new Occurrence(
                        0, [Steps.TurnAction], Subject: card.ObjectId, Player: player),
                    player,
                    [],
                    this);
                if (ability.Trigger.Timing != AbilityType.Resource
                    || !MayInitiate(world, ability, card, player)
                    || !Available(world, card, ability)
                    || !InForm(world, player, ability.Trigger.Form)
                    || !Payable(world, card, player, ability.Cost)
                    || (ability.When is not null && !Test(ability.When, eligibility)))
                {
                    continue;
                }

                // The letters this makes, read off the effect rather than the
                // printed `RES` field: `RES` is what discarding the card
                // generates, and an ability is a different way to make one.
                string generated = Generated(ability.Effect, world, player);
                if (generated.Length > 0)
                {
                    sources.Add(new ResourceSource(card.ObjectId, generated));
                }
            }
        }

        return sources;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ResourceSource> PrintedResourceAbilities(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);

        var available = ResourceAbilities(world, player);
        return
        [
            .. available.Where(source => On(world.Cards[source.Effect]).Any(ability =>
                ability.Trigger.Timing == AbilityType.Resource
                && string.Equals(
                    ability.PrintedResources, source.Generates, StringComparison.Ordinal))),
        ];
    }

    /// <summary>
    /// Whether an ability has uses left this round — <c>rr:limit</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Each copy of an ability with such a limit may be used X times per the
    /// specified period, <b>per instance of that ability</b>", so the count is
    /// kept against the card in play rather than the printed id: two Peter
    /// Parkers at one table have one use each.
    /// </para>
    /// <para>
    /// <b>Kept as a lasting effect and not a token.</b> A card's tokens are on
    /// the wire — they are the digest's <c>fields</c> — so counting uses there
    /// would put a number in every recorded board that the recording does not
    /// have. A lasting effect is not digested, and it expires at the end of the
    /// round without anything having to remember to clear it.
    /// </para>
    /// </remarks>
    private bool Available(
        World world, Card card, CardAbility ability, Occurrence? occurrence = null)
    {
        if (ability.Limit is { } limit
            && world.Effects.Active().Count(effect =>
                effect.Card == card.ObjectId
                && string.Equals(
                    effect.Kind, Spent(card, ability), StringComparison.Ordinal)) >= limit)
        {
            return false;
        }

        if (ability.Maximum is not { } maximum)
        {
            return true;
        }
        if (maximum.Period == MaximumPeriod.Instance && occurrence is null)
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' has a per-instance maximum outside an occurrence window");
        }

        string key = MaximumSpent(world, card, maximum.Period, occurrence);
        return world.Effects.Active().Count(effect =>
            string.Equals(effect.Kind, key, StringComparison.Ordinal)) < maximum.Uses;
    }

    /// <summary>Records one use of a limited ability, until the round ends.</summary>
    private void Use(
        World world, Card card, CardAbility ability, Occurrence? occurrence = null)
    {
        if (ability.Limit is not null)
        {
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: Spent(card, ability),
                Card: card.ObjectId,
                Affects: card.ObjectId,
                Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
        }

        if (ability.Maximum is { } maximum)
        {
            Duration lasts = maximum.Period switch
            {
                MaximumPeriod.Round => Duration.UntilEndOf(TimingPoints.EndOfRound),
                MaximumPeriod.Phase => Duration.UntilEndOf(TimingPoints.EndOfPhase),
                MaximumPeriod.Game or MaximumPeriod.Instance => Duration.WhileInPlay,
                _ => throw new ArgumentOutOfRangeException(nameof(ability)),
            };
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: MaximumSpent(world, card, maximum.Period, occurrence),
                Card: card.ObjectId,
                Lasts: lasts));
        }
    }

    private static string MaximumSpent(
        World world, Card card, MaximumPeriod period, Occurrence? occurrence)
    {
        string title = world.Facts.Title(card.FaceId);
        string instance = period == MaximumPeriod.Instance
            ? ":" + (occurrence?.Id
                ?? throw new RulesNotImplementedException(
                    $"'{card.FaceId}' has a per-instance maximum without an occurrence"))
                .ToString(System.Globalization.CultureInfo.InvariantCulture)
            : string.Empty;
        return $"maximum:{period}:{title}{instance}";
    }

    /// <summary>The effect kind that stands for one use of this instance of an ability.</summary>
    private string Spent(Card card, CardAbility ability)
    {
        var written = On(card).ToList();
        int ordinal = written.FindIndex(candidate => ReferenceEquals(candidate, ability));
        if (ordinal < 0)
        {
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' used an ability that is not on its current face");
        }

        // `rr:limit` counts "per instance of that ability". The engine chooses
        // the printed face plus its ordinal as the stable ability identity;
        // names are display text and two unnamed abilities default to the same
        // card name. Face matters because two sides can each have ordinal zero.
        // `rr:leaves-play.1` makes a returning card a new copy with no memory,
        // so its in-play incarnation is part of the key as well.
        return "spent:"
            + card.Incarnation.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ":"
            + ability.Card
            + ":"
            + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>What letters an effect generates, if it only generates.</summary>
    private static string Generated(AbilityNode effect, World world, int player)
    {
        if (effect.Kind == "generate")
        {
            return Word(effect.Argument);
        }

        if (effect.Kind == "generateTopDiscard")
        {
            var cards = world.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player).Cards;
            return cards.Count > 0
                ? Resources.GeneratedBy(cards[^1].FaceId, world.Facts)
                : string.Empty;
        }

        throw new RulesNotImplementedException(
            $"a resource ability whose effect is '{effect.Kind}' generates nothing this "
            + "engine can read");
    }

    /// <inheritdoc/>
    public string UseResource(World world, int player, int card, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);

        var holder = world.Cards[card];
        var ability = On(holder).FirstOrDefault(candidate =>
        {
            var eligibility = new Cast(
                world,
                holder,
                new Occurrence(
                    0, [Steps.TurnAction], Subject: holder.ObjectId, Player: player),
                player,
                [],
                this);
            return candidate.Trigger.Timing == AbilityType.Resource
                && MayInitiate(world, candidate, holder, player)
                && Available(world, holder, candidate)
                && InForm(world, player, candidate.Trigger.Form)
                && Payable(world, holder, player, candidate.Cost)
                && (candidate.When is null || Test(candidate.When, eligibility));
        })
            ?? throw new RulesNotImplementedException(
                $"card {card} has no resource ability left to use this round");

        var cast = new Cast(
            world, holder,
            new Occurrence(0, [Steps.TurnAction], Subject: holder.ObjectId, Player: player),
            player, events, this)
        {
            Tier = ability.Trigger.Timing,
        };
        Pay(ability.Cost, [], [], cast);
        Use(world, holder, ability);
        return Generated(ability.Effect, world, player);
    }

    /// <inheritdoc/>
    public bool CanTakeDamage(World world, Card target, Card source)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        if (!DeckTypes.IsInPlay(target.Area.Type))
        {
            return true;
        }

        foreach (var ability in On(target).Where(ability =>
            ability.Trigger.Timing == AbilityType.Constant))
        {
            var cast = new Cast(
                world, target, new Occurrence(0, []), ControllerOf(world, target), [], this);
            if (ProhibitsDamage(ability.Effect, cast, source))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public bool CanReady(World world, Card target, Card source)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        foreach (var card in world.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards))
        {
            foreach (var ability in On(card).Where(ability =>
                ability.Trigger.Timing == AbilityType.Constant))
            {
                var cast = new Cast(
                    world, card, new Occurrence(0, []), ControllerOf(world, card), [], this);
                if (ProhibitsReady(ability.Effect, cast, target))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ProhibitsReady(AbilityNode node, Cast cast, Card target) =>
        node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument).Any(step =>
                ProhibitsReady(step, cast, target)),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch && ProhibitsReady(Tree(branch), cast, target),
            "preventReady" => Find(node.Argument, cast)?.ObjectId == target.ObjectId,
            _ => false,
        };

    private static bool ProhibitsDamage(AbilityNode node, Cast cast, Card source) =>
        node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument)
                .Any(step => ProhibitsDamage(step, cast, source)),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch && ProhibitsDamage(Tree(branch), cast, source),
            "preventDamageFrom" => cast.World.Facts.Kind(source.FaceId)
                    == Kind(Word(node.Require("sourceKind")))
                && Rules.State.Traits.Has(
                    cast.World, source, Word(node.Require("sourceTrait")), cast.World.Facts),
            "preventDamageWhile" => Test(Tree(node.Require("condition")), cast),
            _ => false,
        };

    /// <inheritdoc/>
    public long WouldBeDealt(
        World world, Card target, Card source, long amount, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        if (amount <= 0)
        {
            return amount;
        }

        var prevention = world.Effects.Active().FirstOrDefault(effect =>
            string.Equals(effect.Kind, "preventDamage", StringComparison.Ordinal)
            && effect.Affects == target.ObjectId);
        if (prevention is not null && world.Effects.Use(prevention))
        {
            long prevented = prevention.Amount <= 0 ? amount : prevention.Amount;
            return Math.Max(0, amount - prevented);
        }

        var occurrence = new Occurrence(
            0, [Steps.DamageWouldBeDealt], Subject: target.ObjectId, Player: target.Owner);

        long left = amount;
        foreach (var (card, ability) in Waiting(world, occurrence))
        {
            // **Forced only.** `rr:ability.11` makes everything optional unless
            // prefaced by "Forced", and an optional interrupt is a question --
            // which needs a window, which dealing damage has not got. A card
            // that would ask here is refused by name rather than resolved
            // without asking.
            if (ability.Trigger.Timing != AbilityType.ForcedInterrupt)
            {
                // Optional interrupts are offered by the agenda before attack
                // damage is applied. A direct damage call has no window, so it
                // cannot trigger one and must not resolve it on the player's
                // behalf.
                continue;
            }

            var cast = new Cast(world, card, occurrence, target.Owner, events, this)
            {
                Incoming = left,
                Tier = ability.Trigger.Timing,
            };

            TrackResolution(cast, ability);
            Run(ability, cast);
            cast.CompleteResolution();

            // An ability that touched the damage says so; one that did nothing
            // to it leaves it alone. `rr:damage.step.1` holds abilities that
            // *may* replace the damage, not ones that must.
            left = cast.Remaining < 0 ? left : cast.Remaining;
            if (left <= 0)
            {
                // `rr:replacement-effect.1` -- "when an effect is replaced, it
                // is no longer considered imminent and no further interrupts or
                // responses to that effect can be triggered."
                return 0;
            }
        }

        return left;
    }

    /// <inheritdoc/>
    public void WouldBeDefeated(World world, Card target, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        var occurrence = new Occurrence(
            0, [Steps.CardWouldBeDefeated], Subject: target.ObjectId, Player: target.Owner);
        var spent = world.Agenda.Occurrence;

        while (AbilityWindow.Tiers(
            Waiting(world, occurrence, WindowKind.Interrupt)
                .Where(pending => spent?.MayTrigger(WindowKind.Interrupt, pending.Card) ?? true),
            WindowKind.Interrupt,
            occurrence) is { Count: > 0 } tiers)
        {
            var (mandatory, optional) = AbilityWindow.Split(tiers[0]);
            if (mandatory.Count == 0)
            {
                throw new RulesNotImplementedException(
                    $"'{world.Cards[optional[0].Card].FaceId}' offers an optional interrupt "
                    + "at rr:damage.step.6, and dealing damage has no suspended window in "
                    + "which to ask whether to use it");
            }

            if (mandatory.Count > 1)
            {
                throw new RulesNotImplementedException(
                    $"{mandatory.Count} forced interrupts answer the imminent defeat of card "
                    + $"{target.ObjectId}. rr:forced.5 gives their order to the first player, "
                    + "and rr:damage.step.6 has no ordering prompt yet");
            }

            occurrence.Trigger(WindowKind.Interrupt, mandatory[0].Card);
            spent?.Trigger(WindowKind.Interrupt, mandatory[0].Card);
            events.AddRange(Resolve(world, occurrence, mandatory[0], [], []));

            // `rr:would.1`: once the interrupt changes the imminent defeat,
            // no later interrupt to that original condition may be used.
            if (Damage.Health(world, world.Facts, target) - target.Damage > 0)
            {
                return;
            }
        }
    }

    /// <summary>Every authored ability answering one occurrence, with its card.</summary>
    /// <remarks>
    /// <b>Gathered before any of it runs.</b> An ability can make an area —
    /// giving a status card creates one to hold it — and walking
    /// <c>World.Areas</c> lazily while resolving would be modifying the
    /// collection being read.
    /// </remarks>
    private List<(Card Card, CardAbility Ability)> Waiting(World world, Occurrence what) =>
    [
        .. world.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .ToList()
            .SelectMany(card => On(card)
                .Where(ability => Answers(world, ability, card, what))
                .Select(ability => (Card: card, Ability: ability)))
            .ToList(),
    ];

    /// <summary>Whether one ability answers this occurrence at all.</summary>
    private bool Answers(
        World world, CardAbility ability, Card card, Occurrence what)
    {
        int? restricted = RestrictedPlayer(world, ability, card);
        return ability.Trigger.Event is { } condition
            && what.Conditions.Contains(condition, StringComparer.Ordinal)
            && Subject(world, ability.Trigger.Subject, card, what, restricted)
            && Role(world, ability.Trigger.Actor, card, what.ActorFacts, restricted)
            && Role(world, ability.Trigger.Target, card, what.TargetFacts, restricted)
            && Player(world, ability.Trigger.Player, card, what, restricted);
    }

    /// <inheritdoc/>
    public int? AttachesTo(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        if (book.Attaches(card.FaceId) is not { } element)
        {
            return null;
        }

        // No occurrence and no events, for the reason `Constant` has none:
        // this is asked while a card is being placed, so it answers a question
        // and does not act on the answer. `Find` reads the board and nothing
        // else.
        var candidates = Every(
            element,
            new Cast(world, card, new Occurrence(0, []), card.Owner, [], this));

        return candidates.Count switch
        {
            0 => null,
            1 => candidates[0].ObjectId,
            _ => throw new RulesNotImplementedException(
                $"'{card.FaceId}' can attach to {candidates.Count} equally eligible cards. "
                + "rr:first-player.1 gives that choice to the first player, and attaching "
                + "during a reveal has no target prompt yet"),
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<int>? AttachmentTargets(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        if (book.Attaches(card.FaceId) is not { } element)
        {
            return null;
        }

        return [.. Every(element, new Cast(
            world, card, new Occurrence(0, []), card.Owner, [], this))
            .Select(candidate => candidate.ObjectId)];
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Setup(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        if (!book.Authored.Contains(card.FaceId))
        {
            // The same distinction `WhenRevealed` makes, and setup is where it
            // matters most: a scenario whose main scheme nobody has read would
            // otherwise deal a board that is quietly missing whatever its first
            // card said, and every later assertion would be about the wrong
            // game.
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' is being set up and no ability data is written for it; "
                + $"this engine has {book.Authored.Count} authored card(s)");
        }

        var events = new List<GameEvent>();

        // `rr:setup-triggered-ability.2` times these to a step of setup rather
        // than to anything happening, so `Steps.Setup` is the step's name and
        // not a triggering condition -- no card can name it, because the reader
        // refuses an `event` on a Setup ability. What it is for is the events:
        // a board built during setup is told apart in the stream from one built
        // during a round.
        //
        // There is no player whose turn it is either. The card's owner resolves
        // it, which for an encounter card is the scenario.
        var occurrence = new Occurrence(
            0, [Steps.Setup], Subject: card.ObjectId, Player: card.Owner);

        foreach (var ability in On(card))
        {
            if (ability.Trigger.Timing == AbilityType.Setup)
            {
                var cast = new Cast(world, card, occurrence, card.Owner, events, this)
                {
                    Tier = ability.Trigger.Timing,
                };
                TrackResolution(cast, ability);
                Run(ability, cast);
                cast.CompleteResolution();
            }
        }

        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Card> PlayerSetupCards(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(player, world.Players);

        return
        [
            .. world.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type)
                    && area.PlayArea == PlayArea.Of(player))
                .SelectMany(area => area.Cards)
                .Where(card => ControllerOf(world, card) == player
                    && On(card).Any(
                        ability => ability.Trigger.Timing == AbilityType.Setup)),
        ];
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> ResolveEachPlayer(
        World world, Card source, int player, int stoppedAt,
        AbilityType? tier, bool finalStep, bool finalPlayer)
    {
        var step = world.Agenda.Current;
        var written = AbilitiesOn(source, step?.AbilityFace)
            .Where(ability => tier is null || ability.Trigger.Timing == tier)
            .ToList();
        int ordinal = step is { What: Steps.ResolveEachPlayer, AbilityOrdinal: >= 0 }
            ? step.Value.AbilityOrdinal
            : written.FindIndex(ability => EachPlayers(ability.Effect).Any());
        var outer = written.ElementAtOrDefault(ordinal)?.Effect
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no reconstructable each-player ability");
        var parentPath = step is { What: Steps.ResolveEachPlayer, AbilityPath: { } path }
            ? path
            : outer.Kind == "seq" ? [$"seq:{stoppedAt - 1}"] : [];
        var each = NodeAtPath(outer, parentPath);
        if (each.Kind != "eachPlayer")
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no each-player frame at step {stoppedAt - 1}");
        }

        var cast = Resolving(
            world, source, player, tier, finalStep, step?.AbilityOccurrence) with
        {
            EachPlayerFrame = true,
            FinalPlayer = finalPlayer,
            AbilityPlayer = step?.AbilityPlayer ?? player,
            GainedKeywords = world.Agenda.Current is
                { What: Steps.ResolveEachPlayer, SurgeGained: true }
                    ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal),
        };
        RestorePersisted(cast, step);
        cast.RestoreAbility(
            ordinal, [.. parentPath, "eachPlayer:effect"], step?.AbilityFace);
        cast.TrackResolution(ordinal);
        RestorePathBindings(cast, parentPath);
        cast.At(stoppedAt - 1);
        cast.SetContinuation(finalPlayer && (step?.AbilityHasContinuation
            ?? (outer.Kind == "seq" && stoppedAt < Nodes(outer.Argument).Count())));
        Run(Tree(each.Require("effect")), cast);
        if (!cast.Suspended && finalPlayer)
        {
            cast.SetAbilityPath(parentPath);
            cast.RestorePlayer(cast.AbilityPlayer);
            cast.SetContinuation(false);
            ResumeAfter(outer, parentPath, cast);
        }
        cast.CompleteResolution();
        return cast.Events;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>A reader, not a runner.</b> Everything else on this class resolves an
    /// ability once, at a moment, and records what it did. A constant ability
    /// has no moment: <c>rr:ability</c> makes it active "as soon as its card
    /// enters play" and keeps it active "while the card is in play", so the
    /// question is never "what did it do" but "what is it doing". The answer is
    /// worked out afresh whenever anything reads the effect list, which is what
    /// <c>rr:modifiers</c> describes the game as doing continuously.
    /// </para>
    /// <para>
    /// So this shares the interpreter's tests and amounts and none of its
    /// verbs. <c>Grants</c> walks <c>seq</c> and <c>if</c> and stops at
    /// <c>grant</c>; there is no route from here to anything that moves a card
    /// or deals damage, which is what makes it safe to call from inside the
    /// rules rather than between them.
    /// </para>
    /// </remarks>
    public IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        // A player's printed card is blank while Ultron uses it facedown as a
        // Drone minion. Its face id remains underneath for the digest, so the
        // interpreter must use the runtime card kind rather than mistake that
        // id for active player-card text.
        if (!constant.Contains(card.FaceId))
        {
            return [];
        }

        var found = new List<ContinuousEffect>();
        foreach (var ability in On(card))
        {
            if (ability.Trigger.Timing != AbilityType.Constant)
            {
                continue;
            }

            // No occurrence, because there is none: a constant ability is not
            // timed to anything. The empty event list is never written to --
            // nothing `Grants` reaches records anything -- and the card's
            // current controller stands in for the resolving player, which for
            // an encounter card is the scenario.
            Grants(
                ability.Effect,
                new Cast(
                    world, card, new Occurrence(0, []), ControllerOf(world, card), [], this),
                found);
        }

        return found;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenCardDefeated(World world, Card card, Defeated defeated)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(defeated);

        var written = On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenDefeated)
            .ToList();

        // **The printed check gates the complaint, not the run.** Nothing in
        // the printed attributes records a "When Defeated", so an unwritten one
        // and a card that has none look identical from here -- but that is only
        // a question when there is nothing written. Asking it first would let
        // the text box veto authored data, which is the wrong way round: the
        // data is what the engine runs.
        if (written.Count == 0 && world.Facts.HasWhenDefeated(card.FaceId))
        {
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was defeated and prints a 'When Defeated' "
                + "ability that no ability data is written for");
        }

        // **Two occurrences, and each is asked what only it can answer.**
        //
        // This one is built here because the matching needs the defeated card:
        // "when **attached minion** is defeated" is a claim about which card
        // died, while the occurrence the defeat joined keeps the cause. An
        // attack carries its actor and target separately. This occurrence also
        // carries the provenance because "the player who defeated this scheme"
        // is on the card and not on the board.
        //
        // What it cannot answer is `rr:triggering-condition.1`, "each
        // **Interrupt** ability can only be triggered once per occurrence of
        // its triggering condition". The occurrence there is the one on the
        // agenda: it is what lasts, it is what a still-open interrupt window is
        // polling, and it is where a second defeat in the same moment would
        // find an ability already spent. This one is made fresh on every call
        // and would forget all of that.
        var occurrence = new Occurrence(
            0, [Steps.CardDefeated], Subject: card.ObjectId, Player: card.Owner);
        occurrence.Also(defeated);

        var spent = world.Agenda.Occurrence;
        var elsewhere = Answering(world, card, occurrence, spent);
        Alone(card, written.Count, elsewhere);

        var events = new List<GameEvent>();

        // `rr:when-defeated-abilities.2` -- "**all** When Defeated abilities on
        // the card resolve", so this is every one of them rather than the
        // single one a window would take.
        foreach (var ability in written)
        {
            var cast = new Cast(world, card, occurrence, card.Owner, events, this)
            {
                Tier = ability.Trigger.Timing,
            };
            TrackResolution(cast, ability);
            Run(ability, cast);
            cast.CompleteResolution();
        }

        // `rr:forced.6` -- "each forced ability must resolve as completely as
        // possible before the next forced ability being triggered by the same
        // triggering condition may initiate", so the board is re-read between
        // them rather than this walking a list gathered once. The occurrence
        // remembers what has fired, which is `rr:triggering-condition.1`, and
        // is what stops the re-read offering the same card twice.
        while (Answering(world, card, occurrence, spent) is { Count: > 0 } waiting)
        {
            Alone(card, own: 0, waiting);
            occurrence.Trigger(WindowKind.Interrupt, waiting[0].Card);
            spent?.Trigger(WindowKind.Interrupt, waiting[0].Card);
            events.AddRange(Resolve(world, occurrence, waiting[0], [], []));
        }

        return events;
    }

    /// <summary>
    /// The forced interrupts on <i>other</i> cards that answer this defeat —
    /// <c>rr:damage.step.7</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The earliest tier with anything in it, because <c>rr:forced.4</c> orders
    /// the tiers and a later one does not initiate while an earlier one is
    /// still waiting. A status card's forced interrupt is its own tier ahead of
    /// the rest — <c>rr:ability.step.2.a</c>.
    /// </para>
    /// <para>
    /// <b>A non-forced interrupt is refused rather than dropped.</b> Step 7 is
    /// reached from inside the damage, after <c>.step.5</c> has placed it, and
    /// an optional ability there has nobody to offer it to. A card carrying one
    /// would otherwise sit in the dataset looking implemented and never fire,
    /// which is the failure the whole of this file is arranged to avoid.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="card">The card that was defeated.</param>
    /// <param name="occurrence">The defeat, which is what an ability matches against.</param>
    /// <param name="spent">
    /// The occurrence on the agenda, which is what remembers what has already
    /// fired — <c>rr:triggering-condition.1</c>. Null when a caller reached
    /// this without anything happening on the agenda.
    /// </param>
    private IReadOnlyList<PendingAbility> Answering(
        World world, Card card, Occurrence occurrence, Occurrence? spent)
    {
        var tiers = AbilityWindow.Tiers(
            Waiting(world, occurrence, WindowKind.Interrupt)
                .Where(pending => pending.Card != card.ObjectId)
                .Where(pending => spent?.MayTrigger(WindowKind.Interrupt, pending.Card) ?? true),
            WindowKind.Interrupt,
            occurrence);

        foreach (var tier in tiers)
        {
            var (mandatory, optional) = AbilityWindow.Split(tier);
            if (optional.Count > 0)
            {
                throw new RulesNotImplementedException(
                    $"card '{world.Cards[optional[0].Card].FaceId}' offers a non-forced "
                    + $"interrupt when card {card.ObjectId} is defeated. rr:damage.step.7 "
                    + "puts it after the damage has been placed, where the occurrence's "
                    + "interrupt window has long closed and there is nobody left to ask");
            }

            if (mandatory.Count > 0)
            {
                return mandatory;
            }
        }

        return [];
    }

    /// <summary>
    /// Refuses a defeat that two cards answer at once — <c>rr:forced.5</c>.
    /// </summary>
    /// <remarks>
    /// "If two or more forced abilities would initiate at the same moment, the
    /// <b>first player determines the order</b> in which the abilities
    /// initiate, regardless of who controls the cards bearing those abilities."
    /// A question, and <c>rr:damage.step.7</c> is reached from inside the damage
    /// with nobody to put it to — <see cref="Offering"/> asks it in a window and
    /// this is not one.
    /// <para>
    /// So it refuses rather than picks. Two effects at one moment in an order
    /// the engine chose is a board that is plausible and wrong, and the
    /// alternative costs nothing today: <c>rr:when-defeated-abilities.2</c>
    /// decides the one-card case outright — "all When Defeated abilities
    /// <b>on the card</b> resolve" — and it is only across cards that nothing
    /// does. Nothing in the pool the engine reaches puts two there: MARVEL-254.
    /// </para>
    /// </remarks>
    /// <param name="card">The card that was defeated.</param>
    /// <param name="own">How many of its own abilities are waiting.</param>
    /// <param name="elsewhere">What other cards have waiting.</param>
    private static void Alone(Card card, int own, IReadOnlyList<PendingAbility> elsewhere)
    {
        int cards = (own > 0 ? 1 : 0) + elsewhere.Select(pending => pending.Card).Distinct().Count();
        if (cards > 1)
        {
            throw new RulesNotImplementedException(
                $"{cards} cards have a forced interrupt when card {card.ObjectId} is defeated. "
                + "rr:forced.5 gives the order to the first player, and rr:damage.step.7 is "
                + "reached from inside the damage with nobody to ask");
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Act(
        World world, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null)
        => Act(
            world, ability, paying, chosen,
            new Occurrence(
                0, [Steps.TurnAction], Subject: ability.Card, Player: ability.Player),
            values, allocations);

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Act(
        World world, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen, Occurrence occurrence,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(paying);
        ArgumentNullException.ThrowIfNull(chosen);
        ArgumentNullException.ThrowIfNull(occurrence);

        var card = world.Cards[ability.Card];
        var found = Pending(card, ability);

        var events = new List<GameEvent>();
        var cast = new Cast(
            world,
            card,
            occurrence,
            ability.Player,
            events,
            this)
        {
            Tier = found.Trigger.Timing,
        };

        // The wire only names an affordance; it is not authority to use one
        // after its legality has changed. Re-run the same initiation checks
        // that produced Actions before any cost moves a card or exhausts a
        // game element. `rr:cost.6` and `rr:event.3` make target availability
        // part of whether the cost may be paid at all.
        if (!ActionAvailable(world, card, found, ability.Player, cast))
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' cannot initiate this action in the current state");
        }

        // `rr:initiating-abilities` keeps the steps apart, and step 5 pays
        // before step 6 resolves.
        ValidatePayment(found.Cost, paying, chosen, values, cast);
        PayEvent(card, paying, cast, found.Effect, allocations, found.Cost);
        if (world.Facts.Kind(card.FaceId) == CardKind.Event
            && ResourceRequirement(found.Cost, card).Length > 0)
        {
            PayNonResourceCosts(found.Cost, paying, chosen, values, cast);
        }
        else
        {
            Pay(found.Cost, paying, chosen, values, cast);
        }
        Use(world, card, found, occurrence);
        if (world.Facts.Kind(card.FaceId) == CardKind.Event)
        {
            occurrence.BeginCard(card.ObjectId, [ability]);
        }
        cast.RestoreAbility(ability.Ordinal, []);
        cast.TrackResolution(ability.Ordinal);
        Run(found, cast);
        cast.CompleteResolution();
        DiscardEvent(card, cast);
        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<PendingAbility> Actions(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);

        var found = new List<PendingAbility>();
        foreach (var card in Triggerable(world, player).ToList())
        {
            var written = On(card).ToList();
            for (int index = 0; index < written.Count; index++)
            {
                var ability = written[index];
                if (ability.Trigger.Timing is not (AbilityType.Action or AbilityType.ForcedAction)
                    || !MayInitiate(world, ability, card, player))
                {
                    continue;
                }

                var eligibility = new Cast(world, card, new Occurrence(
                    0, [Steps.TurnAction], Subject: card.ObjectId, Player: player),
                    player, [], this);
                if (ActionAvailable(world, card, ability, player, eligibility)
                    && Payable(world, card, player, ability.Cost)
                    && EventPayable(world, card, player, ability))
                {
                    int ordinal = written.Take(index).Count(candidate =>
                        candidate.Trigger.Timing == ability.Trigger.Timing);
                    found.Add(new PendingAbility(
                        card.ObjectId, ability.Trigger.Timing, player, ordinal));
                }
            }
        }

        return found;
    }

    /// <summary>Whether an action can pass every initiation check right now.</summary>
    private bool ActionAvailable(
        World world, Card card, CardAbility ability, int player, Cast eligibility)
    {
        eligibility.SetPaymentMayMutate(
            ability.Cost is not null || world.Facts.Kind(card.FaceId) == CardKind.Event);
        return MayInitiate(world, ability, card, player)
        && Available(world, card, ability)
        && InForm(world, player, ability.Trigger.Form)
        && (ability.When is null || Test(ability.When, eligibility))
        && CanInitiate(ability, eligibility);
    }

    /// <summary>The exact same-timing ability named by a pending ordinal.</summary>
    private CardAbility Pending(Card card, PendingAbility pending) =>
        On(card)
            .Where(candidate => candidate.Trigger.Timing == pending.Type)
            .ElementAtOrDefault(pending.Ordinal)
        ?? throw new AbilityException(
            $"card '{card.FaceId}' has no '{pending.Type}' ability at ordinal "
            + pending.Ordinal);

    /// <summary>
    /// The cards one player may trigger an action on —
    /// <c>rr:player-turn.5</c>'s four places.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>.a</c> "a card in play they control", <c>.b</c> "an encounter card in
    /// play", <c>.d</c> "an event card in their hand <i>(by playing that
    /// event)</i>". <c>.c</c> — "any card in play with text that allows that
    /// player to trigger its action ability" — is a card's own text and belongs
    /// to whichever card says it, so there is nothing general to write here.
    /// </para>
    /// <para>
    /// <b>An event is reached from the hand and nowhere else.</b> That is why
    /// <c>CardPlay.Price</c> refuses to offer one: an event is not
    /// <c>rr:player-turn.2</c>'s "ally, upgrade, support, or player side
    /// scheme", it is played by triggering its action.
    /// </para>
    /// </remarks>
    private IEnumerable<Card> Triggerable(World world, int player)
    {
        foreach (var area in world.Areas)
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            foreach (var card in area.Cards)
            {
                // `.a` and `.b`: yours, or nobody's. A card another player
                // controls is theirs to trigger -- `rr:player-turn.6` is how
                // you ask them.
                if (ControllerOf(world, card) == player
                    || card.Owner == World.Scenario
                    || On(card).Any(ability => ability.AnyPlayer))
                {
                    yield return card;
                }
            }
        }

        // `.d`, and only events: an ally in hand is played rather than
        // triggered, and `rr:player-turn.2` is where that happens.
        foreach (var card in world.Seats[player].Hand.Cards)
        {
            if (world.Facts.Kind(card.FaceId) == CardKind.Event)
            {
                yield return card;
            }
        }
    }

    /// <summary>
    /// Whether the player is in the form an ability requires —
    /// <c>rr:player-turn.5.1</c>.
    /// </summary>
    private static bool InForm(World world, int player, string? form) =>
        form is null || Forms.In(world, world.Seats[player], world.Facts, form);

    /// <summary>
    /// The same question asked of an ability in a window, where the seat may be
    /// nobody's.
    /// </summary>
    /// <remarks>
    /// A form is a property of an identity, and an ability offered to every
    /// player at once is not offered to an identity. A card that means one seat
    /// says so on its trigger; one that names a form without naming a seat has
    /// asked a question about nobody.
    /// </remarks>
    private static bool InForm(World world, int player, string? form, Card card)
    {
        if (form is null)
        {
            return true;
        }

        return player >= 0
            ? InForm(world, player, form)
            : throw new RulesNotImplementedException(
                $"'{card.FaceId}' requires '{form}' form and is offered to every player, "
                + "so there is no identity whose form to read");
    }

    /// <summary>
    /// Whether an ability's cost can be paid — <c>rr:initiating-abilities.step.3</c>.
    /// </summary>
    /// <remarks>
    /// Asked before the ability is offered, because "the player's ability to pay
    /// them" is step 3 and step 5 aborts "without paying any costs" — so an
    /// ability that would abort is not an offer, it is a trap. An exhausted card
    /// cannot pay a cost of exhausting itself: <c>rr:exhausted.2</c>.
    /// </remarks>
    private static bool Payable(World world, Card card, int player, AbilityNode? cost) =>
        cost switch
        {
            null => true,
            { Kind: "seq" } => SequencePayable(world, card, player, cost),
            { Kind: "exhaust" } => CostTarget(world, card, player, cost.Argument)?.Ready == true,
            { Kind: "discard" } => CostTarget(world, card, player,
                cost.Field("card") ?? cost.Argument) is not null,
            { Kind: "removeCounters" } =>
                CounterKeyForRemoval(card, Word(cost.Argument)) is not null,

            // Every other cost is somebody's, and an ability offered to every
            // seat at once has not said whose. `AbilityTrigger.Player` is where
            // a card that means one seat says so.
            _ when player < 0 => throw new RulesNotImplementedException(
                $"'{card.FaceId}' has a cost of '{cost.Kind}' and is offered to every player, "
                + "so there is no hand to price it against"),

            // Asked of the whole hand, which is the right question rather than
            // an approximation: `rr:cost.4` permits generating beyond the cost,
            // so if everything together cannot pay then no choice among them
            // can, and if it can then spending it all is a payment.
            { Kind: "spend" } => Resources.Pays(
                string.Concat(CardPlay.Generators(world, world.Facts, world.Seats[player])
                    .SelectMany(source => source.Generates)),
                Word(cost.Argument).Length,
                Word(cost.Argument)),
            { Kind: "spendPrinted" } => Resources.PaysPrinted(
                string.Concat(PrintedGenerators(world, player)
                    .SelectMany(source => source.Generates)),
                Word(cost.Argument).Length,
                Word(cost.Argument)),
            { Kind: "spendEnergyX" } => Resources.Pays(
                string.Concat(CardPlay.Generators(world, world.Facts, world.Seats[player])
                    .SelectMany(source => source.Generates)),
                1,
                "Y"),

            // "Discard **a card** from your hand" -- `rr:cost.3` spends
            // resources by discarding cards, and this is the other thing a
            // discard can be: the card is the cost and what it would have
            // generated is not read at all. So the question is a count and not
            // a sum, and a card with no printed `RES` pays it.
            { Kind: "discardFromHand" } =>
                world.Seats[player].Hand.Cards.Count >= Number(cost.Argument),
            { Kind: "discardUpToFromHand" } =>
                Number(cost.Argument) > 0 && world.Seats[player].Hand.Cards.Count > 0,
            { Kind: "discardAnyFromHand" } =>
                world.Seats[player].Hand.Cards.Count > 0,
            { Kind: "exhaustChosen" } => CostChoices(world, player, cost)
                .Count(card => card.Ready) >= CostRange(cost, available: int.MaxValue).Min,

            { Kind: "heal" } => CostTarget(
                    world, card, player, cost.Require("card")) is { Damage: > 0 }
                && Number(cost.Require("amount")) > 0,

            { Kind: "dealDamage" } => CostTarget(
                    world, card, player, cost.Require("cards")) is { } damageTarget
                && Number(cost.Require("amount")) > 0
                && world.Abilities.CanTakeDamage(world, damageTarget, card),
            { Kind: "takeDamage" } => CostTarget(
                    world, card, player, cost.Require("cards")) is { } takingTarget
                && Number(cost.Require("amount")) > 0
                && world.Abilities.CanTakeDamage(world, takingTarget, card),

            _ => throw new RulesNotImplementedException(
                $"'{card.FaceId}' has a cost of '{cost.Kind}', which is not implemented"),
        };

    private static bool SequencePayable(World world, Card card, int player, AbilityNode cost)
    {
        var steps = Nodes(cost.Argument).ToList();
        var spends = steps.Where(step => step.Kind is "spend" or "spendPrinted").ToList();
        if (spends.Count > 0)
        {
            if (player < 0)
            {
                throw new RulesNotImplementedException(
                    $"'{card.FaceId}' has simultaneous resource costs and is offered to "
                    + "every player, so there is no hand to price them against");
            }

            bool printed = spends.All(step => step.Kind == "spendPrinted");
            if (!printed && spends.Any(step => step.Kind == "spendPrinted"))
            {
                throw new RulesNotImplementedException(
                    $"'{card.FaceId}' mixes printed and ordinary simultaneous "
                    + "resource costs, whose allocation is not implemented");
            }

            string required = string.Concat(spends.Select(step => Word(step.Argument)));
            string pool = string.Concat((printed
                    ? PrintedGenerators(world, player)
                    : CardPlay.Generators(world, world.Facts, world.Seats[player]))
                .SelectMany(source => source.Generates));
            bool pays = printed
                ? Resources.PaysPrinted(pool, required.Length, required)
                : Resources.Pays(pool, required.Length, required);
            if (!pays)
            {
                return false;
            }
        }

        return steps.Where(step => step.Kind is not ("spend" or "spendPrinted"))
            .All(step => Payable(world, card, player, step));
    }

    private static Card? CostTarget(World world, Card source, int player, AbilityValue value) =>
        Word(value) switch
        {
            "this" => source,
            "you" => player >= 0 ? world.Seats[player].IdentityCard : null,
            _ => null,
        };

    private static bool EventPayable(
        World world, Card card, int player, CardAbility ability)
    {
        if (world.Facts.Kind(card.FaceId) != CardKind.Event)
        {
            return true;
        }

        if (!Resources.HasPlayableCost(card.FaceId, world.Facts))
        {
            return false;
        }

        long cost = CardPlay.CostOf(
            world, world.Facts, world.Seats[player], card).Amount;
        string required = Resources.Required(world, card, world.Facts)
            + ResourceRequirement(ability.Cost, card);
        cost += required.Length
            - Resources.Required(world, card, world.Facts).Length;
        string pool = string.Concat(EventGenerators(world, card, player, ability.Effect)
            .SelectMany(source => source.Generates));
        return Resources.Pays(pool, cost, required);
    }

    private static string ResourceRequirement(AbilityNode? cost, Card card) => cost switch
    {
        null => string.Empty,
        { Kind: "spend" } => Word(cost.Argument),
        { Kind: "spendPrinted" } => throw new RulesNotImplementedException(
            $"event '{card.FaceId}' combines its printed card cost with a printed-resource "
            + "arrow cost, whose allocation is not implemented"),
        { Kind: "seq" } => string.Concat(Nodes(cost.Argument)
            .Select(step => step.Kind switch
            {
                "spend" => Word(step.Argument),
                "spendPrinted" => throw new RulesNotImplementedException(
                    $"event '{card.FaceId}' combines its printed card cost with a "
                    + "printed-resource arrow cost, whose allocation is not implemented"),
                _ => string.Empty,
            })),
        { Kind: "spendEnergyX" } => throw new RulesNotImplementedException(
            $"event '{card.FaceId}' combines a printed cost with a variable X cost"),
        _ => string.Empty,
    };

    /// <summary>
    /// What a cost still has to be told, or null.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:initiating-abilities</c> keeps choosing and paying in different
    /// steps, and this is the choosing half of a cost that has one. A resource
    /// cost has none: <see cref="CostOption.Sources"/> is the menu and which
    /// subset pays is the <i>payment</i>, which travels in
    /// <c>Decision.Resources</c>.
    /// </para>
    /// <para>
    /// <b>The whole hand, and not the hand minus this card.</b> Hunted is an
    /// obligation in the player's play area rather than a card in hand, so
    /// there is nothing here for <c>CardPlay.Spend</c>'s "a card being played
    /// cannot also pay for itself" to guard against — and a card that could
    /// would be a different rule, checked where that one is.
    /// </para>
    /// </remarks>
    private static TargetRequest? Asking(World world, int player, AbilityNode? cost)
    {
        if (cost is { Kind: "seq" })
        {
            return Nodes(cost.Argument)
                .Select(step => Asking(world, player, step))
                .SingleOrDefault(request => request is not null);
        }

        if (cost is { Kind: "discardFromHand" })
        {
            long many = Number(cost.Argument);
            return new TargetRequest(
                [.. world.Seats[player].Hand.Cards.Select(card => card.ObjectId)],
                (int)many,
                (int)many);
        }

        if (cost is { Kind: "discardUpToFromHand" })
        {
            int maximum = Math.Min(
                checked((int)Number(cost.Argument)),
                world.Seats[player].Hand.Cards.Count);
            return new TargetRequest(
                [.. world.Seats[player].Hand.Cards.Select(card => card.ObjectId)],
                Min: 1,
                Max: maximum);
        }

        if (cost is { Kind: "discardAnyFromHand" })
        {
            return new TargetRequest(
                [.. world.Seats[player].Hand.Cards.Select(card => card.ObjectId)],
                Min: 1,
                Max: world.Seats[player].Hand.Cards.Count);
        }

        if (cost is not { Kind: "exhaustChosen" })
        {
            return null;
        }

        var legal = CostChoices(world, player, cost).Where(card => card.Ready).ToList();
        var range = CostRange(cost, legal.Count);
        return new TargetRequest(
            [.. legal.Select(card => card.ObjectId)],
            range.Min,
            range.Max);
    }

    private static IReadOnlyList<Card> CostChoices(
        World world, int player, AbilityNode cost)
    {
        var query = Tree(cost.Require("from"));
        string name = Word(query.Argument);
        return (query.Kind, name) switch
        {
            ("query", "heroesAndAllies") =>
            [
                .. world.PlayerOrder
                    .Where(seat => Forms.In(world, world.Seats[seat], world.Facts, "hero"))
                    .Select(seat => world.Seats[seat].IdentityCard),
                .. world.Areas.Where(area => area.Type == DeckType.AlliesArea)
                    .SelectMany(area => area.Cards),
            ],
            ("query", "charactersYouControl") =>
            [
                world.Seats[player].IdentityCard,
                .. world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player)).Cards,
            ],
            ("query", "alliesYouControl") =>
                [.. world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player)).Cards],
            _ => throw new RulesNotImplementedException(
                $"cost choice query '{query.Kind}:{name}' is not implemented"),
        };
    }

    private static (int Min, int Max) CostRange(AbilityNode cost, int available)
    {
        if (cost.Field("count") is { } exact)
        {
            int count = checked((int)Number(exact));
            return (count, count);
        }
        if (cost.Field("upTo") is { } upTo)
        {
            return (1, Math.Min(checked((int)Number(upTo)), available));
        }
        if (cost.Field("anyNumber") is not null)
        {
            return (1, available);
        }
        return (1, 1);
    }

    /// <summary>What an action's cost looks like on a prompt, or null.</summary>
    /// <remarks>
    /// Only a resource cost reaches the wire, because only a resource cost is a
    /// <i>choice</i>. Exhausting the card the ability is on has one way to be
    /// paid, so there is nothing to ask and nothing to carry.
    /// </remarks>
    private static CostOption? Price(World world, Card card, int player, AbilityNode? cost)
    {
        if (cost is { Kind: "seq" })
        {
            var prices = Nodes(cost.Argument)
                .Select(step => Price(world, card, player, step))
                .Where(price => price is not null)
                .Cast<CostOption>()
                .ToList();
            if (prices.Count == 0)
            {
                return null;
            }
            if (prices.Count == 1)
            {
                return prices[0];
            }

            if (prices.Any(price => price.HasAlternative)
                || prices.Any(price => !long.TryParse(
                    price.Cost, System.Globalization.CultureInfo.InvariantCulture,
                    out _)))
            {
                throw new RulesNotImplementedException(
                    $"'{card.FaceId}' has multiple resource costs whose combined price "
                    + "cannot be represented");
            }

            var components = prices.SelectMany(price => price.ResourceCosts).ToList();
            bool hasPrinted = components.Any(component => component.Printed);
            if (hasPrinted && components.Any(component => !component.Printed))
            {
                throw new RulesNotImplementedException(
                    $"'{card.FaceId}' mixes printed and ordinary simultaneous "
                    + "resource costs, whose allocation is not implemented");
            }

            long total = prices.Sum(price => long.Parse(
                price.Cost, System.Globalization.CultureInfo.InvariantCulture));
            return new CostOption(
                card.ObjectId,
                total.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Rule: [string.Concat(prices.SelectMany(price => price.Rule ?? []))],
                Sources: prices
                    .SelectMany(price => price.Generators)
                    .GroupBy(source => source.Effect)
                    .Select(group => group.First())
                    .ToList(),
                Components: components);
        }

        if (cost is { Kind: "spendEnergyX" })
        {
            long maximum = CardPlay.Generators(
                    world, world.Facts, world.Seats[player])
                .Sum(source => source.Generates.LongCount(resource =>
                    resource is Resources.Energy or Resources.Wild));
            return new CostOption(
                card.ObjectId, "X", ["Y"],
                Sources: CardPlay.Generators(world, world.Facts, world.Seats[player]),
                Variables: [new VariableRequest("X", 1, maximum)]);
        }

        if (cost is { Kind: "spendPrinted" })
        {
            string printedLetters = Word(cost.Argument);
            return new CostOption(
                Target: card.ObjectId,
                Cost: printedLetters.Length.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                Rule: [printedLetters],
                Sources: PrintedGenerators(world, player),
                Components: [new ResourceCost(
                    printedLetters.Length.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                    [printedLetters],
                    Printed: true)]);
        }

        if (cost is not { Kind: "spend" })
        {
            return null;
        }

        string letters = Word(cost.Argument);
        return new CostOption(
            Target: card.ObjectId,
            Cost: letters.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Rule: [letters],
            Sources: CardPlay.Generators(world, world.Facts, world.Seats[player]));
    }

    private static CostOption? CombinedPrice(
        World world, Card card, int player, CardAbility ability)
    {
        var printed = EventPrice(world, card, player, ability.Effect);
        var arrow = Price(world, card, player, ability.Cost);
        if (printed is null || arrow is null)
        {
            return printed ?? arrow;
        }

        if (!long.TryParse(
                printed.Cost, System.Globalization.CultureInfo.InvariantCulture,
                out long printedAmount)
            || !long.TryParse(
                arrow.Cost, System.Globalization.CultureInfo.InvariantCulture,
                out long arrowAmount)
            || printed.HasAlternative || arrow.HasAlternative
            || printed.VariableRequests.Count > 0 || arrow.VariableRequests.Count > 0)
        {
            throw new RulesNotImplementedException(
                $"event '{card.FaceId}' has combined resource costs whose price "
                + "cannot be represented");
        }

        return new CostOption(
            Target: card.ObjectId,
            Cost: checked(printedAmount + arrowAmount).ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            Rule:
            [
                string.Concat(printed.Rule ?? []),
                string.Concat(arrow.Rule ?? []),
            ],
            Sources: printed.Generators
                .Concat(arrow.Generators)
                .GroupBy(source => source.Effect)
                .Select(group => group.First())
                .ToList(),
            Components:
            [
                new ResourceCost(printed.Cost, printed.Rule),
                new ResourceCost(arrow.Cost, arrow.Rule),
            ]);
    }

    private static CostOption? EventPrice(
        World world, Card card, int player, AbilityNode effect)
    {
        if (world.Facts.Kind(card.FaceId) != CardKind.Event)
        {
            return null;
        }

        long cost = CardPlay.CostOf(
            world, world.Facts, world.Seats[player], card).Amount;
        return new CostOption(
            Target: card.ObjectId,
            Cost: cost.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Rule: Resources.Required(world, card, world.Facts) is { Length: > 0 } required
                ? [required]
                : null,
            Sources: EventGenerators(world, card, player, effect));
    }

    private static List<ResourceSource> EventGenerators(
        World world, Card card, int player, AbilityNode effect)
    {
        return CardPlay.Paying(world, world.Facts, world.Seats[player], card)
            .SelectMany(seat => CardPlay.Generators(world, world.Facts, seat, card))
            .Where(source => source.Effect != card.ObjectId)
            .GroupBy(source => source.Effect)
            .Select(group => group.First())
            .ToList();
    }

    private static List<ResourceSource> PrintedGenerators(World world, int player)
    {
        var hand = world.Seats[player].Hand.Cards
            .Select(card => new ResourceSource(
                card.ObjectId,
                Resources.GeneratedBy(card.FaceId, world.Facts)))
            .Where(source => source.Generates.Length > 0);
        return hand
            .Concat(world.Abilities.PrintedResourceAbilities(world, player))
            .GroupBy(source => source.Effect)
            .Select(group => group.First())
            .ToList();
    }

    private static void PayEvent(
        Card card, IReadOnlyList<int> paying, Cast cast, AbilityNode effect,
        IReadOnlyList<ResourceAllocation>? allocations = null,
        AbilityNode? additionalCost = null)
    {
        if (cast.World.Facts.Kind(card.FaceId) != CardKind.Event)
        {
            return;
        }

        if (!Resources.HasPlayableCost(card.FaceId, cast.World.Facts))
        {
            throw new RulesNotImplementedException(
                $"event '{card.FaceId}' has no payable printed cost");
        }

        var adjusted = CardPlay.CostOf(
            cast.World, cast.World.Facts, cast.World.Seats[cast.Player], card);
        var payingSeats = CardPlay.Paying(
            cast.World, cast.World.Facts, cast.World.Seats[cast.Player], card);
        var generators = payingSeats
            .SelectMany(seat => CardPlay.Generators(
                cast.World, cast.World.Facts, seat, card))
            .Where(source => source.Effect != card.ObjectId)
            .GroupBy(source => source.Effect)
            .Select(group => group.First())
            .ToList();
        var resourcePayers = payingSeats
            .SelectMany(seat => cast.World.Abilities.ResourceAbilities(
                    cast.World, seat.Index)
                .Select(source => (source.Effect, seat.Index)))
            .GroupBy(entry => entry.Effect)
            .ToDictionary(group => group.Key, group => group.First().Index);
        var selected = paying.ToHashSet();
        if (selected.Count != paying.Count
            || paying.Any(id => generators.All(source => source.Effect != id)))
        {
            throw new RulesNotImplementedException(
                $"the payment for event {card.ObjectId} names a source that is not available");
        }

        string generated = string.Concat(generators
            .Where(source => selected.Contains(source.Effect))
            .Select(source => source.Generates));
        string printedRequired = Resources.Required(
            cast.World, card, cast.World.Facts);
        string additionalRequired = ResourceRequirement(additionalCost, card);
        string required = printedRequired + additionalRequired;
        long total = checked(adjusted.Amount + additionalRequired.Length);
        var components = new List<ResourceCost>
        {
            new(
                adjusted.Amount.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                printedRequired.Length > 0 ? [printedRequired] : null),
        };
        if (additionalRequired.Length > 0)
        {
            components.Add(new ResourceCost(
                additionalRequired.Length.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                [additionalRequired]));
        }

        if (!Resources.Pays(generated, total, required))
        {
            throw new RulesNotImplementedException(
                $"the cost is {total}"
                + (required.Length > 0 ? $" requiring '{required}'" : string.Empty)
                + $" and the payment generates '{generated}'; "
                + "rr:initiating-abilities.step.5 aborts without paying");
        }

        var assigned = allocations ?? [];
        if (components.Count > 1 && assigned.Count == 0)
        {
            throw new RulesNotImplementedException(
                $"event {card.ObjectId} has simultaneous printed and arrow resource "
                + "costs whose icon allocation was not supplied");
        }
        if (assigned.Count == 0 && HasAmbiguousPaidResourceAllocation(
                effect, generated, total, required))
        {
            throw new RulesNotImplementedException(
                $"event {card.ObjectId} overpays with unlike resource types, whose paid "
                + "allocation is not represented");
        }

        // Validate the player's complete icon allocation before step 1 moves
        // the event. A malformed allocation is an invalid payment answer and
        // `rr:initiating-abilities.step.5` must reject it without changing the
        // board.
        bool declarationSensitive = PaidResourceQueries(effect.Argument).Any()
            || effect.Kind == "paidWithResource";
        string paid = assigned.Count > 0
            ? AllocatedResources(generators, paying, assigned, components, card)
            : declarationSensitive
                ? DeclaredPaidResources(generated, total, required)
                : Resources.Paid(generated, total, required);

        // `rr:initiating-abilities.step.1` and `rr:event`: the event leaves the
        // hand faceup and out of play before costs are paid, and remains there
        // while a choice suspends its resolution. RevealingArea already has
        // exactly those state semantics; the player's play area distinguishes
        // this event from encounter cards being revealed elsewhere.
        var from = card.Area;
        var resolving = cast.World.AreaOf(
            DeckType.RevealingArea, PlayArea.Of(cast.Player), cardOwner: card.Owner);
        World.MoveToTop(card, resolving);
        cast.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(resolving),
            [new Landing(card.ObjectId, resolving.Cards.Count - 1)])
        {
            Trigger = CardPlay.Verb,
            Verb = CardPlay.Verb,
        });

        cast.PaidWith(paid);
        foreach (char resource in paid.Distinct())
        {
            cast.World.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: "paid:" + resource,
                Card: card.ObjectId,
                Affects: card.ObjectId,
                Lasts: new Duration(Uses: 1)));
        }

        CardPlay.Spend(
            cast.World, cast.World.Facts, [.. payingSeats.Select(seat => seat.Hand)], paying,
            total,
            required, card.ObjectId,
            cast.Player, cast.Events, payingFor: card,
            resourcePayers: resourcePayers);
        CardPlay.UseCostModifiers(cast.World, adjusted);

        // `rr:initiating-abilities.step.6`: after its costs are paid, the
        // event is played and its effect resolves. The action's persistent
        // occurrence owns the response window after that effect, so add the
        // condition here rather than creating an earlier separate window.
        if (cast.Occurrence.Is(Steps.TurnAction))
        {
            cast.Occurrence.Also(Steps.CardPlayed);
        }
    }

    private static string AllocatedResources(
        IReadOnlyList<ResourceSource> generators,
        IReadOnlyList<int> paying,
        IReadOnlyList<ResourceAllocation> allocations,
        List<ResourceCost> components,
        Card card)
    {
        var selected = paying.ToHashSet();
        var remaining = generators
            .Where(source => selected.Contains(source.Effect))
            .ToDictionary(
                source => source.Effect,
                source => source.Generates.ToList());
        var paid = Enumerable.Range(0, components.Count)
            .Select(_ => new System.Text.StringBuilder())
            .ToList();

        foreach (var allocation in allocations)
        {
            if (!remaining.TryGetValue(allocation.Source, out var available)
                || allocation.Cost < 0 || allocation.Cost >= components.Count
                || allocation.PaidAs.Length == 0)
            {
                throw new RulesNotImplementedException(
                    $"event {card.ObjectId} carries an invalid resource allocation");
            }

            foreach (char declared in allocation.PaidAs)
            {
                if (!Resources.Types.Contains(declared))
                {
                    throw new RulesNotImplementedException(
                        $"event {card.ObjectId} declares unknown resource '{declared}'");
                }

                int icon = available.IndexOf(declared);
                if (icon < 0
                    && !components[allocation.Cost].Printed
                    && declared != Resources.Wild)
                {
                    icon = available.IndexOf(Resources.Wild);
                }
                if (icon < 0)
                {
                    throw new RulesNotImplementedException(
                        $"event {card.ObjectId} allocates '{declared}' from generator "
                        + $"{allocation.Source}, which cannot produce it");
                }

                available.RemoveAt(icon);
                paid[allocation.Cost].Append(declared);
            }
        }

        for (int index = 0; index < components.Count; index++)
        {
            if (!long.TryParse(
                    components[index].Cost,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out long amount))
            {
                throw new RulesNotImplementedException(
                    $"event {card.ObjectId} has a non-numeric allocation component");
            }
            string assigned = paid[index].ToString();
            string required = string.Concat(components[index].Rule ?? []);
            if (assigned.Length != amount
                || !Resources.Pays(assigned, amount, required))
            {
                throw new RulesNotImplementedException(
                    $"event {card.ObjectId} assigns '{assigned}' to cost {index}, "
                    + $"which costs {amount}"
                    + (required.Length > 0 ? $" requiring '{required}'" : string.Empty));
            }
        }

        return string.Concat(paid.Select(component => component.ToString()));
    }

    private static bool HasAmbiguousPaidResourceAllocation(
        AbilityNode effect, string generated, long cost, string required)
    {
        var queried = PaidResourceQueries(effect.Argument)
            .Concat(effect.Kind == "paidWithResource"
                ? [Word(effect.Argument)[0]]
                : [])
            .Distinct()
            .ToList();
        if (queried.Count == 0)
        {
            return false;
        }

        var outcomes = queried.ToDictionary(
            resource => resource,
            _ => (Paid: false, NotPaid: false));
        var selected = new char[(int)cost];
        return Search(start: 0, chosen: 0);

        bool Search(int start, int chosen)
        {
            if (chosen == selected.Length)
            {
                string payment = new(selected);
                var declared = payment.ToCharArray();
                return DeclareWild(index: 0);

                bool DeclareWild(int index)
                {
                    while (index < declared.Length && declared[index] != Resources.Wild)
                    {
                        index++;
                    }
                    if (index < declared.Length)
                    {
                        foreach (char declaration in Resources.Types)
                        {
                            declared[index] = declaration;
                            if (DeclareWild(index + 1))
                            {
                                return true;
                            }
                        }
                        declared[index] = Resources.Wild;
                        return false;
                    }

                    var pool = declared.ToList();
                    foreach (char requiredType in required)
                    {
                        int found = pool.IndexOf(requiredType);
                        if (found < 0)
                        {
                            return false;
                        }
                        pool.RemoveAt(found);
                    }

                    foreach (char resource in queried)
                    {
                        bool paid = declared.Contains(resource);
                        var seen = outcomes[resource];
                        outcomes[resource] = paid
                            ? (true, seen.NotPaid)
                            : (seen.Paid, true);
                        if (outcomes[resource] is (true, true))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }

            int left = selected.Length - chosen;
            for (int index = start; index <= generated.Length - left; index++)
            {
                selected[chosen] = generated[index];
                if (Search(index + 1, chosen + 1))
                {
                    return true;
                }
            }
            return false;
        }
    }

    private static IEnumerable<char> PaidResourceQueries(AbilityValue value)
    {
        if (value is AbilityValue.List list)
        {
            foreach (char resource in list.Values.SelectMany(PaidResourceQueries))
            {
                yield return resource;
            }
            yield break;
        }

        if (value is not AbilityValue.Map map)
        {
            yield break;
        }

        foreach (var (kind, argument) in map.Entries)
        {
            if (kind == "paidWithResource")
            {
                yield return Word(argument)[0];
            }
            foreach (char resource in PaidResourceQueries(argument))
            {
                yield return resource;
            }
        }
    }

    private static string DeclaredPaidResources(
        string generated, long cost, string required)
    {
        var selected = new char[(int)cost];
        string? declaredPayment = null;
        Search(start: 0, chosen: 0);
        return declaredPayment
            ?? throw new RulesNotImplementedException(
                "the generated resources have no legal declared payment");

        bool Search(int start, int chosen)
        {
            if (chosen == selected.Length)
            {
                var declared = selected.ToArray();
                return DeclareWild(index: 0);

                bool DeclareWild(int index)
                {
                    while (index < declared.Length && declared[index] != Resources.Wild)
                    {
                        index++;
                    }
                    if (index < declared.Length)
                    {
                        foreach (char declaration in Resources.Types)
                        {
                            declared[index] = declaration;
                            if (DeclareWild(index + 1))
                            {
                                return true;
                            }
                        }
                        declared[index] = Resources.Wild;
                        return false;
                    }

                    var pool = declared.ToList();
                    foreach (char requiredType in required)
                    {
                        int found = pool.IndexOf(requiredType);
                        if (found < 0)
                        {
                            return false;
                        }
                        pool.RemoveAt(found);
                    }

                    declaredPayment = new string(declared);
                    return true;
                }
            }

            int left = selected.Length - chosen;
            for (int index = start; index <= generated.Length - left; index++)
            {
                selected[chosen] = generated[index];
                if (Search(index + 1, chosen + 1))
                {
                    return true;
                }
            }
            return false;
        }
    }

    private static void DiscardEvent(Card card, Cast cast)
    {
        bool playedInWindow = !cast.Suspended
            && cast.World.Facts.Kind(card.FaceId) == CardKind.Event
            && card.Area.Type == DeckType.RevealingArea
            && card.Area.PlayArea == PlayArea.Of(card.Owner)
            && !cast.Occurrence.Is(Steps.TurnAction);
        if (!cast.Suspended
            && cast.World.Facts.Kind(card.FaceId) == CardKind.Event
            && card.Area.Type == DeckType.RevealingArea
            && card.Area.PlayArea == PlayArea.Of(card.Owner))
        {
            Rules.Play.Discard.Card(cast.World, card, CardPlay.Verb, cast.Events);
            foreach (var payment in cast.World.Effects.Active().Where(effect =>
                effect.Card == card.ObjectId
                && effect.Kind.StartsWith("paid:", StringComparison.Ordinal)).ToList())
            {
                cast.World.Effects.Use(payment);
            }

            if (playedInWindow)
            {
                cast.World.Agenda.NowEventPlayed(
                    cast.World.Agenda.Current?.Round ?? 0,
                    card.ObjectId,
                    cast.Player);
            }
        }
    }

    /// <summary>Pays an ability's cost — <c>rr:initiating-abilities.step.5</c>.</summary>
    private static void PayNonResourceCosts(
        AbilityNode? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values, Cast cast)
    {
        if (cost is null || cost.Kind is "spend" or "spendPrinted" or "spendEnergyX")
        {
            return;
        }
        if (cost.Kind == "seq")
        {
            foreach (var step in Nodes(cost.Argument))
            {
                PayNonResourceCosts(step, paying, chosen, values, cast);
            }
            return;
        }

        Pay(cost, paying, chosen, values, cast);
    }

    /// <summary>Pays an ability's cost — <c>rr:initiating-abilities.step.5</c>.</summary>
    private static void Pay(
        AbilityNode? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen, Cast cast)
        => Pay(cost, paying, chosen, values: null, cast);

    private static void Pay(
        AbilityNode? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values, Cast cast)
    {
        if (cost is null)
        {
            return;
        }

        if (cost.Kind == "seq")
        {
            var steps = Nodes(cost.Argument).ToList();
            // A take-damage cost is not paid unless all of the damage is
            // taken. Resolve that uncertain component before any payment that
            // cannot fail after validation, so a prevention never leaves an
            // otherwise simultaneous card or resource cost paid by itself.
            foreach (var step in steps.Where(step => step.Kind == "takeDamage"))
            {
                Pay(step, paying, chosen, values, cast);
            }
            var spends = steps.Where(step => step.Kind is "spend" or "spendPrinted").ToList();
            if (spends.Count > 0)
            {
                bool printed = spends.All(step => step.Kind == "spendPrinted");
                if (!printed && spends.Any(step => step.Kind == "spendPrinted"))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' mixes printed and ordinary simultaneous "
                        + "resource costs, whose allocation is not implemented");
                }
                string required = string.Concat(spends.Select(step => Word(step.Argument)));
                SpendAbilityResources(required, paying, cast, printed);
            }

            foreach (var step in steps.Where(
                         step => step.Kind is not ("spend" or "spendPrinted" or "takeDamage")))
            {
                Pay(step, paying, chosen, values, cast);
            }
            return;
        }

        if (cost.Kind is "discardFromHand" or "discardUpToFromHand" or "discardAnyFromHand")
        {
            DiscardToPay(cost, chosen, cast);
            return;
        }

        if (cost.Kind == "exhaustChosen")
        {
            foreach (int id in chosen)
            {
                var target = cast.World.Cards[id];
                target.Exhaust();
                cast.Events.Add(new FieldSet(target.ObjectId, "is_exhaust", 0, 1)
                {
                    Trigger = cast.Trigger,
                    Verb = "Exhaust",
                });
            }
            return;
        }

        if (cost.Kind == "spend")
        {
            SpendAbilityResources(Word(cost.Argument), paying, cast);
            return;
        }

        if (cost.Kind == "spendPrinted")
        {
            SpendAbilityResources(Word(cost.Argument), paying, cast, printed: true);
            return;
        }

        if (cost.Kind == "spendEnergyX")
        {
            if (paying.Count == 0 || paying.Distinct().Count() != paying.Count)
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' requires one or more distinct generators for X");
            }

            var selected = paying.ToHashSet();
            string generated = string.Concat(CardPlay.Generators(
                    cast.World, cast.World.Facts, cast.World.Seats[cast.Player])
                .Where(source => selected.Contains(source.Effect))
                .Select(source => source.Generates));
            long x = DefinedVariable(values, "X", cast.Source);
            CardPlay.Spend(
                cast.World, cast.World.Facts, [cast.World.Seats[cast.Player].Hand], paying,
                x, new string(Resources.Energy, checked((int)x)), itself: -1,
                cast.Player, cast.Events);
            cast.Results["energy"] = x;
            return;
        }

        if (cost.Kind == "takeDamage")
        {
            var target = CostTarget(
                    cast.World, cast.Source, cast.Player, cost.Require("cards"))
                ?? throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' cannot find its take-damage cost target");
            long amount = Number(cost.Require("amount"));
            long before = target.Damage;
            Damage.Deal(
                cast.World, cast.World.Facts, cast.Source, target, amount,
                cast.Trigger, CardPlay.Verb, cast.Events);
            long taken = target.Damage - before;
            if (taken != amount)
            {
                // `rr:cost.12`: "If any of the damage is prevented, then the
                // cost has not been paid." The prevention itself has happened,
                // but neither another cost nor the post-arrow effect has.
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' requires {amount} damage to be taken as a "
                    + $"cost, but only {taken} was taken; rr:cost.12 leaves it unpaid");
            }
            return;
        }

        Run(cost, cast);
    }

    private static void SpendAbilityResources(
        string required, IReadOnlyList<int> paying, Cast cast, bool printed = false) =>
        CardPlay.Spend(
            cast.World,
            cast.World.Facts,
            [cast.World.Seats[cast.Player].Hand],
            paying,
            required.Length,
            required,
            itself: -1,
            cast.Player,
            cast.Events);

    /// <summary>Validates every selected cost before any simultaneous cost is paid.</summary>
    private static void ValidatePayment(
        AbilityNode? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen, Cast cast)
        => ValidatePayment(cost, paying, chosen, values: null, cast);

    private static void ValidatePayment(
        AbilityNode? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values, Cast cast)
    {
        if (cost is null)
        {
            return;
        }

        var steps = cost.Kind == "seq" ? Nodes(cost.Argument).ToList() : [cost];
        if (cast.World.Facts.Kind(cast.Source.FaceId) == CardKind.Event
            && steps.Any(step => step.Kind == "takeDamage"))
        {
            // Playing an event pays its printed resource price through
            // PayEvent. Rolling that payment back after damage prevention is a
            // transaction the engine does not yet represent, so refuse it
            // before the event or a generator moves.
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' is an event with a take-damage cost; "
                + "atomic printed and damage payment is not implemented");
        }
        var spends = steps.Where(step => step.Kind is "spend" or "spendPrinted").ToList();
        if (spends.Count > 0)
        {
            if (paying.Distinct().Count() != paying.Count || paying.Intersect(chosen).Any())
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' names a generator more than once across its costs");
            }

            bool printed = spends.All(step => step.Kind == "spendPrinted");
            if (!printed && spends.Any(step => step.Kind == "spendPrinted"))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' mixes printed and ordinary simultaneous "
                    + "resource costs, whose allocation is not implemented");
            }
            var generators = printed
                ? PrintedGenerators(cast.World, cast.Player)
                : CardPlay.Generators(
                    cast.World, cast.World.Facts, cast.World.Seats[cast.Player]).ToList();
            var selected = paying.ToHashSet();
            if (paying.Any(id => generators.All(source => source.Effect != id)))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' names a resource source that is not available");
            }

            string generated = string.Concat(generators
                .Where(source => selected.Contains(source.Effect))
                .Select(source => source.Generates));
            string required = string.Concat(spends.Select(step => Word(step.Argument)));
            bool pays = printed
                ? Resources.PaysPrinted(generated, required.Length, required)
                : Resources.Pays(generated, required.Length, required);
            if (!pays)
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has simultaneous resource costs requiring "
                    + $"'{required}' and the payment generates '{generated}'");
            }
        }

        foreach (var step in steps.Where(
                     step => step.Kind is not ("spend" or "spendPrinted")))
        {
            if (step.Kind == "spendEnergyX")
            {
                long x = DefinedVariable(values, "X", cast.Source);
                var request = Price(cast.World, cast.Source, cast.Player, step)!
                    .VariableRequests.Single(variable =>
                        string.Equals(variable.Name, "X", StringComparison.Ordinal));
                if (!request.Allows(x))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' defines X as {x}, outside the offered "
                        + $"range {request.Min}..{request.Max}");
                }

                var generators = CardPlay.Generators(
                    cast.World, cast.World.Facts, cast.World.Seats[cast.Player]).ToList();
                var selected = paying.ToHashSet();
                if (paying.Count == 0
                    || paying.Distinct().Count() != paying.Count
                    || paying.Any(id => generators.All(source => source.Effect != id)))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' names an invalid generator for X");
                }

                string generated = string.Concat(generators
                    .Where(source => selected.Contains(source.Effect))
                    .Select(source => source.Generates));
                if (!Resources.Pays(
                        generated, x,
                        new string(Resources.Energy, checked((int)x))))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' defines X as {x}, but the payment "
                        + $"generates '{generated}'");
                }
            }
            else if (step.Kind is "discardFromHand" or "discardUpToFromHand"
                     or "discardAnyFromHand")
            {
                var hand = cast.World.Seats[cast.Player].Hand;
                int minimum = step.Kind == "discardFromHand"
                    ? checked((int)Number(step.Argument))
                    : 1;
                int maximum = step.Kind switch
                {
                    "discardFromHand" => minimum,
                    "discardUpToFromHand" => checked((int)Number(step.Argument)),
                    _ => hand.Cards.Count,
                };
                if (chosen.Count < minimum || chosen.Count > maximum
                    || chosen.Distinct().Count() != chosen.Count)
                {
                    string required = minimum == maximum
                        ? $"{minimum}"
                        : $"{minimum}..{maximum}";
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' costs {required} card(s) from "
                        + $"hand and {chosen.Count} were chosen; "
                        + "rr:initiating-abilities.step.5 aborts without paying");
                }

                foreach (int id in chosen)
                {
                    if (cast.World.Cards[id].Area != hand)
                    {
                        throw new RulesNotImplementedException(
                            $"card {id} is not in {cast.World.Seats[cast.Player].Name}'s hand "
                            + "and cannot be discarded from it");
                    }
                }
            }
            else if (step.Kind == "exhaustChosen")
            {
                var legal = CostChoices(cast.World, cast.Player, step)
                    .Where(card => card.Ready)
                    .Select(card => card.ObjectId)
                    .ToHashSet();
                var range = CostRange(step, legal.Count);
                if (chosen.Count < range.Min || chosen.Count > range.Max
                    || chosen.Distinct().Count() != chosen.Count
                    || chosen.Any(id => !legal.Contains(id)))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' requires {range.Min}..{range.Max} legal "
                        + $"cards to exhaust and {chosen.Count} were supplied");
                }
            }
            else if (!Payable(cast.World, cast.Source, cast.Player, step))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' cannot pay its '{step.Kind}' cost");
            }
        }
    }

    private static long DefinedVariable(
        IReadOnlyDictionary<string, long>? values, string name, Card source) =>
        values is not null && values.TryGetValue(name, out long value)
            ? value
            : throw new RulesNotImplementedException(
                $"'{source.FaceId}' requires an explicit value for {name}");

    /// <summary>
    /// "Discard a card from your hand" — a cost whose payment is a card and not
    /// a number of resources.
    /// </summary>
    /// <remarks>
    /// Refused rather than corrected when the answer does not match the
    /// request. <c>rr:initiating-abilities.step.5</c> aborts "without paying
    /// any costs" if the cost cannot be paid, and an engine that picked a card
    /// for the player would be making a decision the player was asked to make.
    /// </remarks>
    private static void DiscardToPay(AbilityNode cost, IReadOnlyList<int> chosen, Cast cast)
    {
        var hand = cast.World.Seats[cast.Player].Hand;
        int minimum = cost.Kind == "discardFromHand"
            ? checked((int)Number(cost.Argument))
            : 1;
        int maximum = cost.Kind switch
        {
            "discardFromHand" => minimum,
            "discardUpToFromHand" => checked((int)Number(cost.Argument)),
            _ => hand.Cards.Count,
        };

        if (chosen.Count < minimum || chosen.Count > maximum)
        {
            string required = minimum == maximum
                ? $"{minimum}"
                : $"{minimum}..{maximum}";
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' costs {required} card(s) from hand and "
                + $"{chosen.Count} "
                + "were chosen; rr:initiating-abilities.step.5 aborts without paying");
        }

        foreach (int id in chosen)
        {
            var card = cast.World.Cards[id];
            if (card.Area != hand)
            {
                throw new RulesNotImplementedException(
                    $"card {id} is not in {cast.World.Seats[cast.Player].Name}'s hand "
                    + "and cannot be discarded from it");
            }

            Rules.Play.Discard.Card(cast.World, card, CardPlay.Verb, cast.Events);
        }
    }

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
            offers.Add(new Affordance(
                1, ChooseVerb, source.ObjectId, World.Scenario, "exhaust"));
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

        return new Prompt(
            Player: player,
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
                        Rule: "rr:indirect-damage.1")),
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
            cast.Choose(selected[0]);
            SchedulePower(
                Tree(choice.Require("power")), cast, BasicPowers.ThwartVerb,
                selected[0], selected, -1);
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

            Resolve(cast, share);
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

        var prior = cast.Chosen;
        var priorSelection = cast.PlayerSelection;
        try
        {
            ResolutionOutcome? pendingOutcome = cast.HasPendingDependency
                ? ResolutionOf(option, cast)
                : null;
            var before = new BindingCandidateState(
                prior is null ? [] : [prior], prior is null);
            var outcomes = BindingCandidatesAfter(option, cast, before);
            return ContinuationCanResolve(outcomes, cast, pendingOutcome);
        }
        finally
        {
            cast.Choose(prior);
            cast.RestorePlayerSelection(priorSelection);
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
        var prior = cast.Chosen;
        var priorSelection = cast.PlayerSelection;
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
            cast.Choose(prior);
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
        var prior = cast.Chosen;
        var priorSelection = cast.PlayerSelection;
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
                return ContinuationCanResolve(outcomes, cast, pendingOutcome);
            }).ToList();
        }
        finally
        {
            cast.Choose(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
    }

    private static bool ContinuationCanResolve(
        BindingCandidateState outcomes, Cast cast,
        ResolutionOutcome? pendingOutcome)
    {
        var outerCandidates = cast.PriorBindingCandidates;
        bool outerMayBeEmpty = cast.PriorBindingMayBeEmpty;
        bool outerBindingMayChange = cast.PriorBindingMayChange;
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
            return outcomes.Cards.Any(CanResolve)
                || outcomes.MayBeEmpty && CanResolve(null);
        }
        finally
        {
            cast.SetPriorBindingCandidates(outerCandidates);
            cast.SetPriorBindingMayBeEmpty(outerMayBeEmpty);
            cast.SetPriorBindingMayChange(outerBindingMayChange);
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

    /// <summary>Whether every card target required by an effect exists.</summary>
    private static bool HasRequiredTargets(AbilityNode node, Cast cast) => node.Kind switch
    {
        "seq" or "and" => Nodes(node.Argument).All(step => HasRequiredTargets(step, cast)),
        "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
            is not { } branch || HasRequiredTargets(Tree(branch), cast),
        "then" => HasRequiredTargets(Tree(node.Require("effect")), cast)
            && (ResolutionOf(Tree(node.Require("effect")), cast) != ResolutionOutcome.Full
                || HasRequiredTargets(Tree(node.Require("then")), cast)),
        "otherwise" => ResolutionOf(Tree(node.Require("effect")), cast) switch
        {
            ResolutionOutcome.None => HasRequiredTargets(
                Tree(node.Require("otherwise")), cast),
            _ => HasRequiredTargets(Tree(node.Require("effect")), cast),
        },
        "choose" => Nodes(node.Require("options")).Any(option => OptionIsLegal(option, cast)),
        "chooseCard" => LegalCardChoices(node, cast).Count > 0,
        "forEach" => Amount(node.Require("count"), cast) <= 0
            || HasRequiredTargets(Tree(node.Require("effect")), cast),
        "eachTime" => true,
        "removeFromGame" or "exhaust" or "reveal" or "returnToHand" =>
            Every(node.Argument, cast).Count > 0,
        "ready" => Every(node.Argument, cast).Any(target =>
            !target.Ready && cast.Abilities.CanReady(cast.World, target, cast.Source)),
        "soakDamage" => Find(node.Require("onto"), cast) is not null,
        "giveStatus" => StatusTargets(node, cast).Count > 0,
        "attachTo" => Find(node.Argument, cast) is not null,
        "grantUntil" => Find(node.Require("card"), cast) is not null,
        // The delayed effect's game element is supplied by its future
        // occurrence, so rr:target.5 requires no target at initiation.
        "delayUntil" => true,
        "defense" => HasRequiredTargets(Tree(node.Require("effect")), cast),
        "discard" or "dealEncounterCard" =>
            Find(node.Field("card") ?? node.Argument, cast) is not null,
        "heal" => Find(node.Require("card"), cast) is not null,
        "indirectDamage" => Amount(node.Require("amount"), cast) <= 0
            || Assignable(node.Require("among"), cast).Count > 0,
        "dealDamage" => DamageTargets(node.Require("cards"), cast).Count > 0,
        "dealAttackDamage" => DamageTargets(node.Require("cards"), cast).Count > 0,
        "placeThreat" => Every(node.Require("scheme"), cast).Count > 0,
        "placeAccelerationToken" => cast.World.TheCardIn(DeckType.MainSchemesArea) is not null,
        "advanceMainScheme" => CanAdvanceMainScheme(node, cast),
        "removeThreat" => Find(node.Require("scheme"), cast) is not null,
        "enemyAttacks" or "enemySchemes" => Every(node.Require("enemies"), cast).Count > 0,
        "putIntoPlay" => Find(node.Require("card"), cast) is not null,
        "placeAtRandom" => Find(node.Require("on"), cast) is not null,
        "createDrones" => CanCreateDrones(node, cast),
        "draw" => CanDraw(node, cast),
        "search" => HasSearchableArea(node, cast),

        // These effects need no separate card-target existence check in this
        // legacy option-reachability pass. TargetLegalityOf is the authority
        // for the complete rr:target initiation rule, including players.
        "generate" or "changeForm" or "removeCounters" or "preventDamage"
            or "cancelWhenRevealed" or "dealEncounterCards" or "revealTop"
            or "discardAtRandom" or "discardUntil" or "discardTop"
            or "recoverDiscardedByResource" or "shuffleInto"
            or "gainSurge" or "shuffle" or "drawToHandSize"
            or "drawToPrintedHandSize" or "preventThreat"
            or "replaceThreatWithDamage" or "grantCharactersControlledBy"
            or "reduceNextCardCost" => true,
        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' uses '{node.Kind}' in an option whose target "
            + "legality is not implemented"),
    };

    /// <summary>Whether one authored ability can begin before any cost is paid.</summary>
    private static bool CanInitiate(CardAbility ability, Cast cast)
    {
        if (!CanInitiateLabels(ability, cast))
        {
            return false;
        }
        cast.LabelsPreflighted = true;
        return CanInitiate(ability.Effect, cast)
            && TargetLegalityOf(ability.Effect, cast) != TargetLegality.Invalid;
    }

    /// <summary>Whether an ability envelope can establish every labeled lifecycle.</summary>
    private static bool CanInitiateLabels(CardAbility ability, Cast cast)
    {
        var labels = ability.Labels ?? [];
        if (labels.Count == 0)
        {
            return true;
        }

        bool cancelled = LabeledAbilities.WouldBeCancelled(
            cast.World, cast.World.Facts, Resolver(cast), cast.Source, labels);

        foreach (string power in LabeledAbilities.Known)
        {
            if (!labels.Contains(power, StringComparer.Ordinal)
                && PowerNodes(ability.Effect, power).Any())
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' contains a {power.ToLowerInvariant()} power "
                    + "that is absent from its ability labels");
            }
        }

        if (!cancelled
            && labels.Contains(Attack.DefenseVerb, StringComparer.Ordinal)
            && !Attack.CanUseDefenseAbility(cast.World, Resolver(cast)))
        {
            return false;
        }

        if (!cancelled)
        {
            bool attack = labels.Contains(BasicPowers.AttackVerb, StringComparer.Ordinal);
            bool thwart = labels.Contains(BasicPowers.ThwartVerb, StringComparer.Ordinal);

            if (attack && thwart)
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has one ability labeled as both attack and "
                    + "thwart, whose single combined power occurrence is not implemented");
            }
            if (attack && !GuaranteesOneLabeledPower(
                    ability.Effect, BasicPowers.AttackVerb))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has an attack label without exactly one "
                    + "saveable attack power");
            }
            if (thwart && !GuaranteesOneLabeledPower(
                    ability.Effect, BasicPowers.ThwartVerb))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has a thwart label without exactly one "
                    + "saveable thwart power");
            }
        }

        return true;
    }

    /// <summary>Whether every executable path enters one matching saveable power.</summary>
    private static bool GuaranteesOneLabeledPower(AbilityNode node, string power)
    {
        if (string.Equals(
            node.Kind, power.ToLowerInvariant(), StringComparison.Ordinal))
        {
            return PowerNodes(node, power).Count() == 1;
        }

        if (node.Kind == "chooseCard")
        {
            return GuaranteesOneLabeledPower(Tree(node.Require("effect")), power);
        }

        if (node.Kind == "choose")
        {
            var options = Nodes(node.Require("options")).ToList();
            return options.Count >= 2
                && options.All(option => GuaranteesOneLabeledPower(option, power));
        }

        if (node.Kind == "if")
        {
            return node.Field("then") is { } then
                && node.Field("else") is { } otherwise
                && GuaranteesOneLabeledPower(Tree(then), power)
                && GuaranteesOneLabeledPower(Tree(otherwise), power);
        }

        if (node.Kind == "seq")
        {
            var steps = Nodes(node.Argument).ToList();
            return steps.Count > 0
                && GuaranteesOneLabeledPower(steps[0], power)
                && steps.Skip(1).All(step => !PowerNodes(step, power).Any());
        }

        return false;
    }

    /// <summary>Whether every choice required to initiate this effect has an answer.</summary>
    private static bool CanInitiate(AbilityNode node, Cast cast)
    {
        if (HasNestedEachPlayer(
            node, cast, bindingMayChange: cast.PriorBindingMayChange))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' nests one each-player frame inside another, "
                + "which is not implemented");
        }
        if (ContainsUnsupportedPower(
            node, cast, bindingMayChange: cast.PriorBindingMayChange))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside a labelled power, "
                + "which is not implemented");
        }
        return node.Kind switch
        {
            "seq" => CanInitiateSequence(node, cast),
            "and" => CanInitiateAnd(node, cast),
            "if" => CanInitiateIf(node, cast),
            "forEach" => CanInitiateForEach(node, cast),
            "eachTime" => CanInitiateEachTime(node, cast),
            "then" => CanInitiateDependent(
                node, cast, ResolutionOutcome.Full, "then"),
            "otherwise" => CanInitiateDependent(
                node, cast, ResolutionOutcome.None, "otherwise"),
            _ => CanInitiateLeaf(node, cast),
        };
    }

    private static bool CanInitiateSequence(AbilityNode node, Cast cast)
    {
        var steps = Nodes(node.Argument).ToList();
        bool outerContinuation = cast.HasContinuation;
        bool outerPriorMutation = cast.PriorStepMayMutate;
        ulong outerPriorFormChanges = cast.PriorFormsMayChange;
        bool outerPriorBinding = cast.PriorBindingMayChange;
        var outerPriorCandidates = cast.PriorBindingCandidates;
        bool outerPriorBindingMayBeEmpty = cast.PriorBindingMayBeEmpty;
        ulong priorFormChanges = outerPriorFormChanges;
        bool priorBinding = outerPriorBinding;
        var priorCandidates = new BindingCandidateState(
            outerPriorCandidates,
            outerPriorBindingMayBeEmpty
                || outerPriorCandidates.Count == 0 && cast.Chosen is null);
        try
        {
            for (int step = 0; step < steps.Count; step++)
            {
                cast.SetContinuation(outerContinuation || step < steps.Count - 1);
                cast.SetPriorStepMayMutate(outerPriorMutation || step > 0);
                cast.SetPriorFormsMayChange(priorFormChanges);
                cast.SetPriorBindingMayChange(priorBinding);
                cast.SetPriorBindingCandidates(priorCandidates.Cards);
                cast.SetPriorBindingMayBeEmpty(priorCandidates.MayBeEmpty);
                if (step > 0)
                {
                    PreflightDependentOutcomesAfterMutation(steps[step], cast);
                }
                if (!CanInitiate(steps[step], cast))
                {
                    return false;
                }
                priorFormChanges = FormsMayDifferAfter(
                    steps[step], cast, priorFormChanges, priorBinding);
                priorBinding = BindingMayChangeAfter(
                    steps[step], cast, priorBinding);
                priorCandidates = steps[step].Kind == "choose"
                    && step + 1 < steps.Count
                        ? ChoiceBindingCandidatesAfter(
                            steps[step], cast, priorCandidates,
                            steps.Skip(step + 1).ToList())
                        : BindingCandidatesAfter(
                            steps[step], cast, priorCandidates);
            }

            return true;
        }
        finally
        {
            cast.SetContinuation(outerContinuation);
            cast.SetPriorStepMayMutate(outerPriorMutation);
            cast.SetPriorFormsMayChange(outerPriorFormChanges);
            cast.SetPriorBindingMayChange(outerPriorBinding);
            cast.SetPriorBindingCandidates(outerPriorCandidates);
            cast.SetPriorBindingMayBeEmpty(outerPriorBindingMayBeEmpty);
        }
    }

    private static void PreflightDependentOutcomesAfterMutation(
        AbilityNode node, Cast cast)
    {
        if (node.Kind == "forEach" && SkipForEachPreflight(node, cast))
        {
            return;
        }

        if (node.Kind is "then" or "otherwise")
        {
            PreflightResolutionBranches(
                Tree(node.Require("effect")), cast, allBranches: true);
        }

        var children = node.Kind switch
        {
            "choose" => Nodes(node.Require("options")),
            "eachPlayer" => [Tree(node.Require("effect"))],
            _ => StructuralChildren(node),
        };
        foreach (var child in children)
        {
            PreflightDependentOutcomesAfterMutation(child, cast);
        }
    }

    private static bool CanInitiateIf(AbilityNode node, Cast cast)
    {
        // Payment happens after an action is offered and can change the facts
        // tested by the branch. Validate every structurally reachable
        // continuation boundary now, while no cost has been paid, then use
        // only the currently active branch for ordinary target eligibility.
        var test = Tree(node.Require("test"));
        bool paymentCanSwitch = cast.PaymentMayMutate && PaymentCanChange(test);
        bool bindingCanSwitch = cast.PriorBindingMayChange
            && BindingCanChange(test.Argument);
        bool stateCanSwitch = bindingCanSwitch
            || PriorStepCanChange(test, cast) || paymentCanSwitch;
        bool reachableLabelledTargetsAreValid = true;
        foreach (var branch in Branches.Select(node.Field).Where(value => value is not null))
        {
            var effect = Tree(branch!);
            PreflightContinuationBoundaries(effect, cast);
            if (stateCanSwitch)
            {
                PreflightDependentOutcomesAfterMutation(effect, cast);
                PreflightInitiationConstraints(
                    effect, cast, requireCurrentTargets: paymentCanSwitch);
                if (HasLabelledPower(effect) && !CanInitiate(effect, cast))
                {
                    // A prior step or payment can select this branch after the
                    // ability has begun. Refuse now if its labelled target is
                    // not valid on the initiation board; discovering that only
                    // after the mutation would leave a partial action behind.
                    reachableLabelledTargetsAreValid = false;
                }
            }
        }

        if (!reachableLabelledTargetsAreValid)
        {
            return false;
        }
        if (bindingCanSwitch)
        {
            return Branches.Select(node.Field)
                .Where(value => value is not null)
                .All(value => CanInitiate(Tree(value!), cast));
        }
        return node.Field(Test(test, cast) ? "then" : "else")
            is not { } active || CanInitiate(Tree(active), cast);
    }

    private static bool PriorStepCanChange(AbilityNode test, Cast cast) => test.Kind switch
    {
        "and" or "or" => Nodes(test.Argument).Any(child =>
            PriorStepCanChange(child, cast)),
        "not" => PriorStepCanChange(Tree(test.Argument), cast),
        "inForm" => cast.PriorBindingMayChange
                && BindingCanChange(test.Argument)
            || FormSeats(test.Require("player"), cast)
                .Any(seat => SeatMayChange(cast.PriorFormsMayChange, seat)),
        _ => cast.PriorStepMayMutate,
    };

    /// <summary>Seats whose final form may differ after a reachable effect.</summary>
    private static ulong FormsMayDifferAfter(
        AbilityNode node, Cast cast, ulong before,
        bool bindingMayChange = false)
    {
        if (node.Kind == "forEach" && StableZeroForEach(node, cast))
        {
            return before;
        }
        if (node.Kind == "changeForm")
        {
            string destination = Word(node.Require("to"));
            var player = node.Require("player");
            var seats = FormSeats(player, cast, bindingMayChange).ToList();
            ulong ChangeOne(ulong state, int seat)
            {
                ulong bit = PlayerSeat(seat);
                return Forms.In(
                        cast.World, cast.World.Seats[seat], cast.World.Facts,
                        destination)
                    ? state & ~bit
                    : state | bit;
            }
            if (bindingMayChange && BindingCanChange(player))
            {
                return seats.Aggregate(0UL, (possible, seat) =>
                    possible | ChangeOne(before, seat));
            }
            ulong after = before;
            foreach (int seat in seats)
            {
                after = ChangeOne(after, seat);
            }
            return after;
        }

        ulong outer = cast.PriorFormsMayChange;
        try
        {
            cast.SetPriorFormsMayChange(before);
            if (node.Kind == "if")
            {
                var test = Tree(node.Require("test"));
                bool canSwitch = bindingMayChange
                        && BindingCanChange(test.Argument)
                    || PriorStepCanChange(test, cast)
                    || cast.PaymentMayMutate && PaymentCanChange(test);
                var branches = canSwitch
                    ? Branches.Select(node.Field).Where(value => value is not null)
                    : node.Field(Test(test, cast) ? "then" : "else") is { } active
                        ? [active]
                        : [];
                return branches.Select(branch =>
                        FormsMayDifferAfter(
                            Tree(branch!), cast, before, bindingMayChange))
                    .DefaultIfEmpty(before)
                    .Aggregate((left, right) => left | right);
            }
            if (node.Kind == "choose")
            {
                return Nodes(node.Require("options")).Select(option =>
                        FormsMayDifferAfter(
                            option, cast, before, bindingMayChange))
                    .DefaultIfEmpty(before)
                    .Aggregate((left, right) => left | right);
            }
            if (node.Kind == "eachPlayer")
            {
                int original = cast.Player;
                try
                {
                    ulong possible = 0;
                    foreach (var order in PlayerPermutations(
                        cast.World.PlayerOrder.ToList()))
                    {
                        ulong after = before;
                        foreach (int player in order)
                        {
                            cast.RestorePlayer(player);
                            cast.SetPriorFormsMayChange(after);
                            after = FormsMayDifferAfter(
                                Tree(node.Require("effect")), cast, after,
                                bindingMayChange);
                        }
                        possible |= after;
                    }
                    return possible;
                }
                finally
                {
                    cast.RestorePlayer(original);
                }
            }

            ulong state = before;
            bool childBindingMayChange = bindingMayChange
                || node.Kind is "chooseCard" or "thwartSchemes"
                    or "thwartDifferentSchemes" or "legalPractice";
            foreach (var child in MutationChildren(node))
            {
                cast.SetPriorFormsMayChange(state);
                state = FormsMayDifferAfter(
                    child, cast, state, childBindingMayChange);
            }
            return state;
        }
        finally
        {
            cast.SetPriorFormsMayChange(outer);
        }
    }

    private static IEnumerable<int> FormSeats(
        AbilityValue value, Cast cast, bool bindingMayChange = false) =>
        bindingMayChange && BindingCanChange(value)
            ? cast.World.PlayerOrder
            : Seats(value, cast);

    private static IEnumerable<IReadOnlyList<int>> PlayerPermutations(
        List<int> players)
    {
        if (players.Count == 0)
        {
            yield return [];
            yield break;
        }
        for (int index = 0; index < players.Count; index++)
        {
            int player = players[index];
            var rest = players.Where((_, candidate) => candidate != index).ToList();
            foreach (var tail in PlayerPermutations(rest))
            {
                yield return [player, .. tail];
            }
        }
    }

    private static bool BindingMayChangeAfter(
        AbilityNode node, Cast cast, bool before)
    {
        if (node.Kind is "chooseCard" or "thwartSchemes"
            or "thwartDifferentSchemes" or "legalPractice")
        {
            return true;
        }
        if (node.Kind == "forEach" && StableZeroForEach(node, cast))
        {
            return before;
        }
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            bool canSwitch = before && BindingCanChange(test.Argument)
                || PriorStepCanChange(test, cast)
                || cast.PaymentMayMutate && PaymentCanChange(test);
            var branches = canSwitch
                ? Branches.Select(node.Field).Where(value => value is not null)
                : node.Field(Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            return branches.Any(branch =>
                BindingMayChangeAfter(Tree(branch!), cast, before));
        }
        bool after = before;
        foreach (var child in MutationChildren(node))
        {
            after = BindingMayChangeAfter(child, cast, after);
        }
        return after;
    }

    private sealed record BindingCandidateState(
        IReadOnlyList<Card> Cards, bool MayBeEmpty);

    private static BindingCandidateState BindingCandidatesAfter(
        AbilityNode node, Cast cast, BindingCandidateState before)
    {
        if (node.Kind is "attack" or "thwart")
        {
            // A labelled power owns `chosen` only inside its wrapper. Its
            // effect cannot suspend, and the outer binding resumes afterwards.
            return before;
        }
        if (node.Kind == "chooseCard")
        {
            return ChooseCardBindingCandidatesAfter(node, cast, before);
        }
        if (node.Kind == "forEach" && StableZeroForEach(node, cast))
        {
            return before;
        }
        if (node.Kind == "eachPlayer")
        {
            int original = cast.Player;
            try
            {
                var possible = new List<Card>();
                bool mayBeEmpty = false;
                foreach (var order in PlayerPermutations(
                    cast.World.PlayerOrder.ToList()))
                {
                    // Each scheduled frame restores the same persisted outer
                    // binding. Only the final frame supplies the binding seen
                    // by the continuation; earlier frames do not feed it.
                    cast.RestorePlayer(order[^1]);
                    var finalFrame = BindingCandidatesAfter(
                        Tree(node.Require("effect")), cast, before);
                    possible.AddRange(finalFrame.Cards);
                    mayBeEmpty |= finalFrame.MayBeEmpty;
                }
                return new BindingCandidateState(
                    possible.DistinctBy(card => card.ObjectId).ToList(),
                    mayBeEmpty);
            }
            finally
            {
                cast.RestorePlayer(original);
            }
        }
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            bool canSwitch = before.Cards.Count > 0
                    && BindingCanChange(test.Argument)
                || PriorStepCanChange(test, cast)
                || cast.PaymentMayMutate && PaymentCanChange(test);
            var branches = canSwitch
                ? Branches.Select(node.Field).Where(value => value is not null)
                : node.Field(Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            var outcomes = branches.Select(branch => BindingCandidatesAfter(
                    Tree(branch!), cast, before))
                .ToList();
            return new BindingCandidateState(
                outcomes.SelectMany(outcome => outcome.Cards)
                    .DistinctBy(card => card.ObjectId)
                    .ToList(),
                outcomes.Count == 0 || outcomes.Any(outcome => outcome.MayBeEmpty));
        }
        if (node.Kind == "choose")
        {
            return ChoiceBindingCandidatesAfter(node, cast, before);
        }
        var candidates = before;
        foreach (var child in MutationChildren(node))
        {
            candidates = BindingCandidatesAfter(child, cast, candidates);
        }
        return candidates;
    }

    private static BindingCandidateState ChooseCardBindingCandidatesAfter(
        AbilityNode node, Cast cast, BindingCandidateState before)
    {
        var prior = cast.Chosen;
        var priorSelection = cast.PlayerSelection;
        var possible = new List<Card>();
        bool mayBeEmpty = false;
        try
        {
            void AddOutcome(Card? binding)
            {
                cast.ChooseSelection(binding);
                var legal = LegalCardChoices(node, cast);
                if (legal.Count > 0)
                {
                    foreach (var chosen in legal)
                    {
                        cast.ChooseSelection(chosen);
                        var afterEffect = BindingCandidatesAfter(
                            Tree(node.Require("effect")), cast,
                            new BindingCandidateState([chosen], MayBeEmpty: false));
                        possible.AddRange(afterEffect.Cards);
                        mayBeEmpty |= afterEffect.MayBeEmpty;
                    }
                }
                else if (binding is not null)
                {
                    // Runtime choice resolution is a no-op when this frame has
                    // no legal target, so an earlier binding survives it.
                    possible.Add(binding);
                }
                else
                {
                    mayBeEmpty = true;
                }
            }

            foreach (var candidate in before.Cards)
            {
                AddOutcome(candidate);
            }
            if (before.MayBeEmpty)
            {
                AddOutcome(null);
            }
            if (before.Cards.Count == 0 && !before.MayBeEmpty)
            {
                AddOutcome(prior);
            }
        }
        finally
        {
            cast.Choose(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
        return new BindingCandidateState(
            possible.DistinctBy(card => card.ObjectId).ToList(), mayBeEmpty);
    }

    private static BindingCandidateState ChoiceBindingCandidatesAfter(
        AbilityNode node, Cast cast, BindingCandidateState before,
        IReadOnlyList<AbilityNode>? continuation = null)
    {
        var prior = cast.Chosen;
        var priorSelection = cast.PlayerSelection;
        var outcomes = new List<BindingCandidateState>();
        try
        {
            void AddOutcomes(Card? binding)
            {
                cast.ChooseSelection(binding);
                var incoming = new BindingCandidateState(
                    binding is null ? [] : [binding], binding is null);
                foreach (var option in Nodes(node.Require("options"))
                    .Where(option => OptionIsLegal(option, cast)))
                {
                    var outcome = BindingCandidatesAfter(option, cast, incoming);
                    outcomes.Add(continuation is null
                        ? outcome
                        : FilterCandidatesForContinuation(
                            outcome, continuation, cast));
                }
            }

            foreach (var candidate in before.Cards)
            {
                AddOutcomes(candidate);
            }
            if (before.MayBeEmpty)
            {
                AddOutcomes(null);
            }
            if (before.Cards.Count == 0 && !before.MayBeEmpty)
            {
                AddOutcomes(prior);
            }
        }
        finally
        {
            cast.Choose(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
        return new BindingCandidateState(
            outcomes.SelectMany(outcome => outcome.Cards)
                .DistinctBy(card => card.ObjectId).ToList(),
            outcomes.Any(outcome => outcome.MayBeEmpty));
    }

    private static BindingCandidateState FilterCandidatesForContinuation(
        BindingCandidateState candidates,
        IReadOnlyList<AbilityNode> continuation,
        Cast cast)
    {
        var suffix = new AbilityNode(
            "seq",
            new AbilityValue.List(continuation.Select(NodeValue).ToList()));
        var prior = cast.Chosen;
        var priorSelection = cast.PlayerSelection;
        var outerCandidates = cast.PriorBindingCandidates;
        bool outerMayBeEmpty = cast.PriorBindingMayBeEmpty;
        bool outerBindingMayChange = cast.PriorBindingMayChange;
        try
        {
            var legal = candidates.Cards.Where(candidate =>
            {
                cast.ChooseSelection(candidate);
                cast.SetPriorBindingCandidates([candidate]);
                cast.SetPriorBindingMayBeEmpty(false);
                cast.SetPriorBindingMayChange(false);
                return CanInitiateSequence(suffix, cast)
                    && TargetLegalityOf(suffix, cast) != TargetLegality.Invalid;
            }).ToList();
            // An explicit empty option is the authored decline branch for
            // “may.” It remains reachable rather than being silently removed;
            // the enclosing sequence will reject it if the suffix needs a
            // binding. Card-bearing alternatives can be filtered individually.
            return new BindingCandidateState(legal, candidates.MayBeEmpty);
        }
        finally
        {
            cast.Choose(prior);
            cast.RestorePlayerSelection(priorSelection);
            cast.SetPriorBindingCandidates(outerCandidates);
            cast.SetPriorBindingMayBeEmpty(outerMayBeEmpty);
            cast.SetPriorBindingMayChange(outerBindingMayChange);
        }
    }

    private static void PreflightContinuationBoundaries(AbilityNode node, Cast cast)
    {
        if (node.Kind == "forEach" && SkipForEachPreflight(node, cast))
        {
            return;
        }

        if (node.Kind == "seq")
        {
            var steps = Nodes(node.Argument).ToList();
            bool outerContinuation = cast.HasContinuation;
            try
            {
                for (int step = 0; step < steps.Count; step++)
                {
                    cast.SetContinuation(outerContinuation || step < steps.Count - 1);
                    PreflightContinuationBoundaries(steps[step], cast);
                }
            }
            finally
            {
                cast.SetContinuation(outerContinuation);
            }
            return;
        }

        if (node.Kind == "and")
        {
            _ = CanInitiateAnd(node, cast);
            return;
        }

        var children = node.Kind switch
        {
            "choose" => Nodes(node.Require("options")),
            "eachPlayer" => [Tree(node.Require("effect"))],
            _ => StructuralChildren(node),
        };
        foreach (var child in children)
        {
            PreflightContinuationBoundaries(child, cast);
        }
    }

    private static void PreflightInitiationConstraints(
        AbilityNode node, Cast cast, bool requireCurrentTargets)
    {
        if (node.Kind == "forEach" && SkipForEachPreflight(node, cast))
        {
            return;
        }

        if (node.Kind == "grantUntil" && !LastingPeriodIsOpen(node, cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a lasting effect outside its named period");
        }
        if (node.Kind == "grantUntil"
            && requireCurrentTargets
            && Find(node.Require("card"), cast) is null)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' may reach a lasting effect with no target after payment");
        }

        var children = node.Kind switch
        {
            "choose" => Nodes(node.Require("options")),
            "eachPlayer" => [Tree(node.Require("effect"))],
            _ => StructuralChildren(node),
        };
        foreach (var child in children)
        {
            PreflightInitiationConstraints(child, cast, requireCurrentTargets);
        }
    }

    private static bool CanInitiateAnd(AbilityNode node, Cast cast)
    {
        var effects = Nodes(node.Argument).ToList();
        if (effects.Count > 1 && effects.Any(effect => SuspendsInsideAnd(effect, cast)))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' orders simultaneous effects around a threat "
                + "placement continuation, which is not implemented");
        }
        bool outerContinuation = cast.HasContinuation;
        try
        {
            foreach (var effect in effects)
            {
                cast.SetContinuation(outerContinuation || effects.Count > 1);
                if (!CanInitiate(effect, cast))
                {
                    return false;
                }
            }
            return true;
        }
        finally
        {
            cast.SetContinuation(outerContinuation);
        }
    }

    private static bool CanInitiateDependent(
        AbilityNode node, Cast cast, ResolutionOutcome required, string branch)
    {
        var effect = Tree(node.Require("effect"));
        if (ActiveChoices(effect, cast).Any())
        {
            var choices = ActiveChoices(effect, cast).ToList();
            if (effect.Kind is not ("choose" or "chooseCard")
                || choices.Any(ChoiceHasNestedChoice))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has multiple-stage player choices before "
                    + $"'{node.Kind}', whose combined resolution outcome is not implemented");
            }
            PreflightAnsweredOutcome(effect, cast);
            return CanInitiate(effect, cast);
        }
        var outcome = EnsureDependentSupported(
            node, cast, effect, Tree(node.Require(branch)), required);
        return outcome == required
            ? CanInitiate(Tree(node.Require(branch)), cast)
            : CanInitiate(effect, cast);
    }

    private static bool ChoiceHasNestedChoice(AbilityNode choice) => choice.Kind switch
    {
        "chooseCard" => Choices(Tree(choice.Require("effect"))).Any(),
        "choose" => Nodes(choice.Require("options")).Any(option => Choices(option).Any()),
        _ => false,
    };

    private static bool CanInitiateLeaf(AbilityNode node, Cast cast) => node.Kind switch
    {
        "resolveSpecials" when cast.HasContinuation =>
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' continues after ordered Special abilities, "
                + "which is not implemented"),
        "chooseCard" => CanInitiateChooseCard(node, cast),
        "choose" => CanInitiateChoice(node, cast),
        "draw" => CanInitiateDraw(node, cast),
        "thwartDifferentSchemes" => Every(node.Require("schemes"), cast).Count > 0,
        "legalPractice" => cast.World.Seats[cast.Player].Hand.Cards.Any(card =>
                card.ObjectId != cast.Source.ObjectId)
            && Every(node.Require("schemes"), cast).Count > 0,
        "thwartSchemes" when SuspendsPowerEffect(
            Tree(Tree(node.Require("power")).Require("effect")), cast) =>
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside a labelled power, "
                + "which is not implemented"),
        "thwartSchemes" => Every(node.Require("schemes"), cast).Count > 0,
        "attack" or "thwart" when SuspendsPowerEffect(
            Tree(node.Require("effect")), cast) =>
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside a labelled power, "
                + "which is not implemented"),
        "attack" => CanTargetAttack(node, cast),
        "thwart" => CanTargetThwart(node, cast),
        "enemyAttacks" or "enemySchemes" => CanInitiateActivation(node, cast),
        "defense" => Attack.CanUseDefenseAbility(cast.World, cast.Player)
            && CanInitiate(Tree(node.Require("effect")), cast),
        // A missing dynamic target gets the resolver's specific exception
        // (for example, no activating enemy). When the target exists, the
        // lasting period itself is an initiation constraint.
        "grantUntil" => Find(node.Require("card"), cast) is not null
            ? LastingPeriodIsOpen(node, cast)
            : !IsPlayerCard(cast)
                && !cast.PaymentMayMutate
                && !cast.PriorStepMayMutate,
        _ => true,
    };

    private static bool CanInitiateChooseCard(AbilityNode node, Cast cast)
    {
        bool CanChooseFromCurrentBinding() =>
            (!RequiresChosenPlayer(node.Require("from"))
                || (cast.PlayerSelection ?? cast.Chosen) is { Owner: >= 0 })
            && LegalCardChoices(node, cast).Count > 0;

        if (cast.PriorBindingCandidates.Count == 0
            && !cast.PriorBindingMayBeEmpty)
        {
            return CanChooseFromCurrentBinding();
        }

        var prior = cast.Chosen;
        var priorSelection = cast.PlayerSelection;
        try
        {
            bool any = false;
            foreach (var candidate in cast.PriorBindingCandidates)
            {
                cast.ChooseSelection(candidate);
                any |= CanChooseFromCurrentBinding();
            }
            if (cast.PriorBindingMayBeEmpty)
            {
                cast.ChooseSelection(null);
                any |= CanChooseFromCurrentBinding();
            }
            return any;
        }
        finally
        {
            cast.Choose(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
    }

    private static bool CanInitiateDraw(AbilityNode node, Cast cast)
    {
        bool CanDrawFromBinding() =>
            !BindingCanChange(node.Require("player"))
            || (cast.PlayerSelection ?? cast.Chosen) is { Owner: >= 0 }
                && CanDraw(node, cast);

        if (cast.Chosen is null && BindingCanChange(node.Require("player"))
            && cast.PriorBindingCandidates.Count > 0)
        {
            if (cast.PriorBindingMayBeEmpty)
            {
                return false;
            }
            var prior = cast.Chosen;
            var priorSelection = cast.PlayerSelection;
            try
            {
                bool any = cast.PriorBindingCandidates.Any(candidate =>
                {
                    cast.ChooseSelection(candidate);
                    return CanDrawFromBinding();
                });
                return any;
            }
            finally
            {
                cast.Choose(prior);
                cast.RestorePlayerSelection(priorSelection);
            }
        }
        return CanDrawFromBinding();
    }

    private static bool CanTargetAttack(AbilityNode node, Cast cast)
    {
        var target = node.Require("target");
        if (cast.Chosen is null && BindingCanChange(target)
            && cast.PriorBindingCandidates.Count > 0)
        {
            return EveryCandidateCan(cast, () => CanTargetAttack(node, cast));
        }
        return Find(target, cast) is { } enemy
            && BasicPowers.Attackable(cast.World, cast.World.Facts, Resolver(cast))
                .Any(candidate => candidate.ObjectId == enemy.ObjectId)
            && cast.World.Abilities.CanTakeDamage(cast.World, enemy, cast.Source);
    }

    private static bool CanTargetThwart(AbilityNode node, Cast cast)
    {
        var target = node.Require("target");
        if (cast.Chosen is null && BindingCanChange(target)
            && cast.PriorBindingCandidates.Count > 0)
        {
            return EveryCandidateCan(cast, () => CanTargetThwart(node, cast));
        }
        if (Find(target, cast) is not { } scheme)
        {
            return false;
        }

        if (node.Field("automaticTarget") is not null)
        {
            return BasicPowers.CanAutomaticallyThwart(
                cast.World, cast.World.Facts, Resolver(cast), scheme);
        }

        if (BasicPowers.Thwartable(cast.World, cast.World.Facts, Resolver(cast))
            .Any(candidate => candidate.ObjectId == scheme.ObjectId))
        {
            return true;
        }

        // rr:cannot.3 lets an explicit exception win. Crisis normally removes
        // the main scheme from Thwartable, but a scoped ignoresCrisis removal
        // can still make that declared thwart target valid. Automatic thwart
        // checks the remaining scheme-level prohibition (notably Patrol).
        bool crisisException = BasicPowers.CanAutomaticallyThwart(
                cast.World, cast.World.Facts, Resolver(cast), scheme)
            && CrisisIgnoringRemovalCanAffect(
                Tree(node.Require("effect")), cast, scheme);
        if (crisisException)
        {
            cast.ValidateCrisisIgnoringThwart(node);
        }
        return crisisException;
    }

    private static bool EveryCandidateCan(Cast cast, Func<bool> test)
    {
        if (cast.PriorBindingMayBeEmpty)
        {
            return false;
        }
        var prior = cast.Chosen;
        var priorSelection = cast.PlayerSelection;
        try
        {
            return cast.PriorBindingCandidates.All(candidate =>
            {
                cast.ChooseSelection(candidate);
                return test();
            });
        }
        finally
        {
            cast.Choose(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
    }

    private static bool CrisisIgnoringRemovalCanAffect(
        AbilityNode node, Cast cast, Card scheme)
    {
        var prior = cast.Chosen;
        try
        {
            // SchedulePower binds the declared power target before it runs the
            // nested effect. Offer-time legality must expose the same binding.
            cast.Choose(scheme);
            return CrisisIgnoringRemovalCanAffectBound(node, cast, scheme);
        }
        finally
        {
            cast.Choose(prior);
        }
    }

    private static bool CrisisIgnoringRemovalCanAffectBound(
        AbilityNode node, Cast cast, Card scheme)
    {
        if (node.Kind == "removeThreat")
        {
            return IgnoresCrisis(node)
                && Every(node.Require("scheme"), cast).Any(candidate =>
                    candidate.ObjectId == scheme.ObjectId)
                && scheme.Tokens.GetValueOrDefault("k_threat") > 0
                && Amount(node.Require("amount"), cast) > 0
                && CanRemoveThreatFrom(node, cast, scheme);
        }

        return node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument).Any(child =>
                CrisisIgnoringRemovalCanAffectBound(child, cast, scheme)),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch
                && CrisisIgnoringRemovalCanAffectBound(Tree(branch), cast, scheme),
            "forEach" => Amount(node.Require("count"), cast) > 0
                && CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("effect")), cast, scheme),
            "choose" => Nodes(node.Require("options")).Any(option =>
                CrisisIgnoringRemovalCanAffectBound(option, cast, scheme)),
            "then" when ActiveChoices(Tree(node.Require("effect")), cast).Any() =>
                CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("effect")), cast, scheme),
            "then" => CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("effect")), cast, scheme)
                || ResolutionOf(Tree(node.Require("effect")), cast)
                    == ResolutionOutcome.Full
                && CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("then")), cast, scheme),
            "otherwise" when ActiveChoices(Tree(node.Require("effect")), cast).Any() =>
                CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("effect")), cast, scheme),
            "otherwise" => CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("effect")), cast, scheme)
                || ResolutionOf(Tree(node.Require("effect")), cast)
                    == ResolutionOutcome.None
                && CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("otherwise")), cast, scheme),
            "eachPlayer" or "defense" => CrisisIgnoringRemovalCanAffectBound(
                Tree(node.Require("effect")), cast, scheme),
            _ => false,
        };
    }

    /// <summary>Whether a tree has no current target, an invalid one, or a valid one.</summary>
    /// <remarks>
    /// <c>rr:target.2</c> asks only for “at least one valid target,” and
    /// <c>rr:target.3.4</c> says one effect on that target is enough. Keeping
    /// <see cref="TargetLegality.None"/> separate from
    /// <see cref="TargetLegality.Invalid"/> prevents an
    /// untargeted sibling from manufacturing a target while still allowing a
    /// valid sibling to make a multi-effect ability initiable.
    /// </remarks>
    private static TargetLegality TargetLegalityOf(
        AbilityNode node, Cast cast, bool bindingMayChange = false)
    {
        TargetLegality Cards(IEnumerable<Card> candidates) =>
            candidates.Any() ? TargetLegality.Valid : TargetLegality.Invalid;

        if (cast.Chosen is null
            && cast.PriorBindingCandidates.Count > 0
            && BindingCanChange(node.Argument)
            && node.Kind is not ("seq" or "and" or "if" or "then" or "otherwise"
                or "forEach" or "defense" or "choose" or "eachTime"
                or "delayUntil" or "chooseCard"))
        {
            return CandidateTargetLegality(node, cast);
        }

        return node.Kind switch
        {
            "seq" => SequenceTargetLegality(node, cast, bindingMayChange),
            "and" => CombineTargetLegality(
                Nodes(node.Argument).Select(child =>
                    TargetLegalityOf(child, cast, bindingMayChange))),
            "if" when bindingMayChange
                    && BindingCanChange(Tree(node.Require("test")).Argument) =>
                CombineTargetLegality(Branches.Select(node.Field)
                    .Where(value => value is not null)
                    .Select(value => TargetLegalityOf(
                        Tree(value!), cast, bindingMayChange))),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch
                    ? TargetLegalityOf(Tree(branch), cast, bindingMayChange)
                    : TargetLegality.None,
            "then" when ActiveChoices(Tree(node.Require("effect")), cast).Any() =>
                TargetLegalityOf(
                    Tree(node.Require("effect")), cast, bindingMayChange),
            "then" => ResolutionOf(Tree(node.Require("effect")), cast)
                == ResolutionOutcome.Full
                    ? CombineTargetLegality(
                    [
                        TargetLegalityOf(
                            Tree(node.Require("effect")), cast, bindingMayChange),
                        TargetLegalityOf(
                            Tree(node.Require("then")), cast, bindingMayChange),
                    ])
                    : TargetLegalityOf(
                        Tree(node.Require("effect")), cast, bindingMayChange),
            "otherwise" when ActiveChoices(Tree(node.Require("effect")), cast).Any() =>
                TargetLegalityOf(
                    Tree(node.Require("effect")), cast, bindingMayChange),
            "otherwise" => ResolutionOf(Tree(node.Require("effect")), cast)
                == ResolutionOutcome.None
                    ? TargetLegalityOf(
                        Tree(node.Require("otherwise")), cast, bindingMayChange)
                    : TargetLegalityOf(
                        Tree(node.Require("effect")), cast, bindingMayChange),
            "forEach" => Amount(node.Require("count"), cast) <= 0
                ? TargetLegality.None
                : TargetLegalityOf(
                    Tree(node.Require("effect")), cast, bindingMayChange),
            "attack" => CanTargetAttack(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "thwart" => CanTargetThwart(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "defense" => TargetLegalityOf(
                Tree(node.Require("effect")), cast, bindingMayChange),

            // The choice itself is validated by CanInitiateChoice and
            // OptionIsLegal. Future-target lasting effects have no target node
            // in this tree, so they fall through to None.
            "choose" or "eachTime" => TargetLegality.None,
            "delayUntil" => TargetLegality.None,
            "chooseCard" => CanInitiateChooseCard(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,

            "removeFromGame" or "reveal" or "returnToHand" =>
                Cards(Every(node.Argument, cast)),
            "exhaust" => Cards(Every(node.Argument, cast).Where(card => card.Ready)),
            "ready" => Cards(Every(node.Argument, cast).Where(card =>
                !card.Ready && cast.Abilities.CanReady(cast.World, card, cast.Source))),
            "giveStatus" => Cards(StatusTargets(node, cast)),
            "attachTo" => Find(node.Argument, cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "grantUntil" => Find(node.Require("card"), cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "discard" or "dealEncounterCard" =>
                Find(node.Field("card") ?? node.Argument, cast) is null
                    ? TargetLegality.Invalid : TargetLegality.Valid,
            "heal" => Find(node.Require("card"), cast) is { Damage: > 0 }
                && Amount(node.Require("amount"), cast) > 0
                    ? TargetLegality.Valid : TargetLegality.Invalid,
            "dealDamage" or "dealAttackDamage" =>
                Amount(node.Require("amount"), cast) > 0
                    ? Cards(DamageTargets(node.Require("cards"), cast))
                    : TargetLegality.Invalid,
            "indirectDamage" => Amount(node.Require("amount"), cast) <= 0
                ? TargetLegality.Invalid
                : Cards(Assignable(node.Require("among"), cast)),
            "placeThreat" => Amount(node.Require("amount"), cast) <= 0
                ? TargetLegality.Invalid
                : Cards(Every(node.Require("scheme"), cast)),
            "removeThreat" => Every(node.Require("scheme"), cast).Any(scheme =>
                scheme.Tokens.GetValueOrDefault("k_threat") > 0
                && Amount(node.Require("amount"), cast) > 0
                && CanRemoveThreatFrom(node, cast, scheme))
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "enemyAttacks" or "enemySchemes" =>
                Cards(Every(node.Require("enemies"), cast)),
            "putIntoPlay" => Find(node.Require("card"), cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "placeAtRandom" => Find(node.Require("on"), cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "draw" when cast.Chosen is null
                    && bindingMayChange
                    && BindingCanChange(node.Require("player")) =>
                CanInitiateDraw(node, cast)
                    ? TargetLegality.Valid : TargetLegality.Invalid,
            "draw" when BindingCanChange(node.Require("player"))
                    && (cast.PlayerSelection ?? cast.Chosen) is { Owner: < 0 } =>
                TargetLegality.Invalid,
            "draw" => CanDraw(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "search" => HasSearchableArea(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            _ => TargetLegality.None,
        };
    }

    private static TargetLegality CandidateTargetLegality(
        AbilityNode node, Cast cast)
    {
        if (cast.PriorBindingMayBeEmpty)
        {
            return TargetLegality.Invalid;
        }
        var prior = cast.Chosen;
        var priorSelection = cast.PlayerSelection;
        try
        {
            var outcomes = new List<TargetLegality>();
            foreach (var candidate in cast.PriorBindingCandidates)
            {
                cast.ChooseSelection(candidate);
                outcomes.Add(TargetLegalityOf(node, cast));
            }
            if (outcomes.Contains(TargetLegality.Invalid))
            {
                return TargetLegality.Invalid;
            }
            return outcomes.Count > 0
                ? TargetLegality.Valid
                : TargetLegality.None;
        }
        finally
        {
            cast.Choose(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
    }

    private static TargetLegality ChosenPlayerTargetLegality(
        AbilityNode node, Cast cast)
    {
        var outerCandidates = cast.PriorBindingCandidates;
        try
        {
            if (outerCandidates.Count == 0)
            {
                cast.SetPriorBindingCandidates(
                    cast.World.PlayerOrder.Select(player =>
                        cast.World.Seats[player].IdentityCard).ToList());
            }
            return CandidateTargetLegality(node, cast);
        }
        finally
        {
            cast.SetPriorBindingCandidates(outerCandidates);
        }
    }

    private static TargetLegality SequenceTargetLegality(
        AbilityNode node, Cast cast, bool bindingMayChange)
    {
        var found = new List<TargetLegality>();
        bool binding = bindingMayChange;
        var outerCandidates = cast.PriorBindingCandidates;
        bool outerMayBeEmpty = cast.PriorBindingMayBeEmpty;
        var candidates = new BindingCandidateState(
            outerCandidates,
            outerMayBeEmpty
                || outerCandidates.Count == 0 && cast.Chosen is null);
        try
        {
            var children = Nodes(node.Argument).ToList();
            for (int index = 0; index < children.Count; index++)
            {
                var child = children[index];
                cast.SetPriorBindingCandidates(candidates.Cards);
                cast.SetPriorBindingMayBeEmpty(candidates.MayBeEmpty);
                found.Add(TargetLegalityOf(child, cast, binding));
                binding = BindingMayChangeAfter(child, cast, binding);
                candidates = child.Kind == "choose" && index + 1 < children.Count
                    ? ChoiceBindingCandidatesAfter(
                        child, cast, candidates,
                        children.Skip(index + 1).ToList())
                    : BindingCandidatesAfter(child, cast, candidates);
                binding |= candidates.Cards.Count > 0
                    || ContainsNode(child, "chooseCard", cast);
            }
            return CombineTargetLegality(found);
        }
        finally
        {
            cast.SetPriorBindingCandidates(outerCandidates);
            cast.SetPriorBindingMayBeEmpty(outerMayBeEmpty);
        }
    }

    private static TargetLegality CombineTargetLegality(
        IEnumerable<TargetLegality> children)
    {
        var found = children.ToList();
        if (found.Contains(TargetLegality.Valid))
        {
            return TargetLegality.Valid;
        }
        return found.Contains(TargetLegality.Invalid)
            ? TargetLegality.Invalid
            : TargetLegality.None;
    }

    private static IReadOnlyList<Card> StatusTargets(AbilityNode node, Cast cast)
    {
        string status = Word(node.Require("status"));
        return [.. Every(node.Require("card"), cast).Where(card =>
            Statuses.Count(cast.World, card, status)
                < Statuses.Limit(cast.World, cast.World.Facts, card, status))];
    }

    private enum TargetLegality
    {
        None,
        Invalid,
        Valid,
    }

    private static bool CanInitiateChoice(AbilityNode node, Cast cast)
    {
        var options = Nodes(node.Require("options")).ToList();
        foreach (var option in options)
        {
            _ = CanInitiate(option, cast);
        }

        return options.Any(option => OptionIsLegal(option, cast));
    }

    private static bool CanInitiateForEach(AbilityNode node, Cast cast)
    {
        bool stateMayChange = cast.PaymentMayMutate || cast.PriorStepMayMutate;
        if (stateMayChange && AmountMayChange(node.Require("count")))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a for-each count after state may change");
        }
        long count = ForEachCount(node, cast);
        if (count == 0)
        {
            return true;
        }

        var effect = Tree(node.Require("effect"));
        if (!Choices(effect).Any())
        {
            if (effect.Kind == "dealDamage")
            {
                if (DamageTargets(effect.Require("cards"), cast).Count != 1)
                {
                    return false;
                }
                if (stateMayChange && !StableForEachTarget(effect.Require("cards"), "villain"))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' reaches an unbound targeted for-each effect "
                        + "after state may change");
                }
                return CanInitiate(effect, cast);
            }
            if (effect.Kind == "removeThreat")
            {
                if (Every(effect.Require("scheme"), cast).Count != 1)
                {
                    return false;
                }
                if (stateMayChange
                    && !StableForEachTarget(effect.Require("scheme"), "mainScheme"))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' reaches an unbound targeted for-each effect "
                        + "after state may change");
                }
                return CanInitiate(effect, cast);
            }
            if (ContainsForEachTarget(effect))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has a targeted for-each effect without choose "
                    + "whose one target cannot be persisted");
            }
        }
        return CanInitiate(effect, cast);
    }

    private static bool CanInitiateEachTime(AbilityNode node, Cast cast)
    {
        var preceding = EachTimePreceding(node, cast);
        var authoredCount = preceding.Require("count");
        if ((cast.PaymentMayMutate || cast.PriorStepMayMutate)
            && AmountMayChange(authoredCount))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches an each-time count after state may change");
        }
        long requested = Amount(authoredCount, cast);
        if (requested < 0)
        {
            throw new AbilityException("'eachTime' needs a non-negative discard count");
        }
        if (requested > 0)
        {
            ValidateEachTimeBody(node, cast);
        }
        return true;
    }

    private static bool StableForEachTarget(AbilityValue value, string query) =>
        value is AbilityValue.Word word
            && string.Equals(word.Value, query, StringComparison.Ordinal)
        || value is AbilityValue.Map
            && Tree(value) is { Kind: "query", Argument: AbilityValue.Word named }
            && string.Equals(named.Value, query, StringComparison.Ordinal);

    private static bool AmountMayChange(AbilityValue value)
    {
        if (value is not AbilityValue.Map)
        {
            return false;
        }
        var amount = Tree(value);
        return amount.Kind switch
        {
            "perPlayer" or "powerAmount" => false,
            "min" or "add" or "mul" => Values(amount.Argument).Any(AmountMayChange),
            _ => true,
        };
    }

    private static bool ContainsPowerAmount(AbilityValue value) => value switch
    {
        AbilityValue.List list => list.Values.Any(ContainsPowerAmount),
        AbilityValue.Map map => map.Entries.Any(entry =>
            entry.Key == "powerAmount" || ContainsPowerAmount(entry.Value)),
        _ => false,
    };

    private static bool HasUnboundPowerAmount(AbilityNode node, Cast cast) =>
        cast.PowerAmount < 0 && ContainsPowerAmount(node.Require("count"));

    private static bool ContainsMutableAmount(AbilityNode node) =>
        (node.Field("amount") is { } amount && AmountMayChange(amount))
        || (node.Kind == "forEach" && AmountMayChange(node.Require("count")))
        || ContinuationChildren(node).Any(ContainsMutableAmount);

    private static long ForEachCount(AbilityNode node, Cast cast)
    {
        long count = Amount(node.Require("count"), cast);
        if (count < 0)
        {
            throw new AbilityException("'forEach' needs a non-negative 'count'");
        }
        return count;
    }

    private static bool SkipForEachPreflight(AbilityNode node, Cast cast)
    {
        if ((cast.PaymentMayMutate || cast.PriorStepMayMutate)
            && AmountMayChange(node.Require("count")))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a for-each count after state may change");
        }
        return StableZeroForEach(node, cast);
    }

    private static bool StableZeroForEach(AbilityNode node, Cast cast)
    {
        if (node.Kind != "forEach")
        {
            return false;
        }

        // A labelled choice such as Legal Practice binds this sentinel only
        // when its power is scheduled. Its body remains reachable during
        // preflight, but the count cannot be validated or pruned yet.
        if (HasUnboundPowerAmount(node, cast))
        {
            return false;
        }

        long count = ForEachCount(node, cast);
        return !AmountMayChange(node.Require("count")) && count == 0;
    }

    private static bool CurrentlyZeroForEach(AbilityNode node, Cast cast)
    {
        if (node.Kind != "forEach" || HasUnboundPowerAmount(node, cast))
        {
            return false;
        }
        if ((cast.PaymentMayMutate || cast.PriorStepMayMutate)
            && AmountMayChange(node.Require("count")))
        {
            return false;
        }
        return ForEachCount(node, cast) == 0;
    }

    private static bool CanInitiateActivation(AbilityNode node, Cast cast)
    {
        return true;
    }

    private static bool LastingPeriodIsOpen(AbilityNode node, Cast cast) =>
        Word(node.Require("until")) switch
        {
            TimingPoints.EndOfAttack => cast.World.Attack is not null
                || cast.World.CharacterAttack is not null
                || cast.Occurrence.Is(Steps.AttackInitiated),
            TimingPoints.EndOfActivation => cast.World.Activation is not null,
            _ => true,
        };

    private static bool HasLabelledPower(AbilityNode node) =>
        PowerNodes(node, BasicPowers.AttackVerb).Any()
        || PowerNodes(node, BasicPowers.ThwartVerb).Any()
        || PowerNodes(node, Attack.DefenseVerb).Any();

    private static bool HasInitiationConstraint(AbilityNode node) =>
        HasLabelledPower(node)
        || node.Kind == "grantUntil"
        || StructuralChildren(node).Any(HasInitiationConstraint);

    private static IEnumerable<AbilityNode> StructuralChildren(AbilityNode node) =>
        node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument),
            "if" => Branches.Select(node.Field).Where(value => value is not null)
                .Select(value => Tree(value!)),
            "then" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("then")),
            ],
            "otherwise" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("otherwise")),
            ],
            "defense" or "delayUntil" or "forEach" =>
                [Tree(node.Require("effect"))],
            "eachTime" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("then")),
            ],
            _ => [],
        };

    /// <summary>Whether a player-card option can change the current state.</summary>
    private static bool CanPartiallyResolve(AbilityNode node, Cast cast)
    {
        return node.Kind switch
        {
            "seq" or "and" => !Nodes(node.Argument).Any()
                || Nodes(node.Argument).Any(step => CanPartiallyResolve(step, cast)),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch && CanPartiallyResolve(Tree(branch), cast),
            "then" => ResolutionOf(Tree(node.Require("effect")), cast)
                is not ResolutionOutcome.None,
            "otherwise" => ResolutionOf(Tree(node.Require("effect")), cast) switch
            {
                ResolutionOutcome.None => CanPartiallyResolve(
                    Tree(node.Require("otherwise")), cast),
                _ => true,
            },
            "defense" => CanPartiallyResolve(Tree(node.Require("effect")), cast),
            "forEach" => Amount(node.Require("count"), cast) > 0
                && CanPartiallyResolve(Tree(node.Require("effect")), cast),
            "choose" => Nodes(node.Require("options")).Any(option => OptionIsLegal(option, cast)),
            "chooseCard" => LegalCardChoices(node, cast).Count > 0,
            "changeForm" => !Forms.In(
                cast.World,
                cast.World.Seats[Seat(node.Require("player"), cast)],
                cast.World.Facts,
                Word(node.Require("to"))),
            "removeFromGame" => Find(node.Argument, cast) is { } card
                && card.Area.Type != DeckType.RemovedArea,
            "exhaust" => Find(node.Argument, cast)?.Ready == true,
            "ready" => Every(node.Argument, cast).Any(card =>
                !card.Ready && cast.Abilities.CanReady(cast.World, card, cast.Source)),
            "removeCounters" =>
                CounterKeyForRemoval(cast.Source, Word(node.Argument)) is not null,
            "advanceMainScheme" => CanAdvanceMainScheme(node, cast),
            "discardAtRandom" => Amount(node.Require("count"), cast) > 0
                && Seats(node.Require("player"), cast)
                    .Any(seat => cast.World.Seats[seat].Hand.Cards.Count > 0),
            "discardTop" => Amount(node.Require("count"), cast) > 0
                && Area(Word(node.Require("from")), cast).Cards.Count > 0,
            "heal" => Find(node.Require("card"), cast) is { Damage: > 0 }
                && Amount(node.Require("amount"), cast) > 0,
            "indirectDamage" => HasRequiredTargets(node, cast)
                && Amount(node.Require("amount"), cast) > 0,
            "dealDamage" => HasRequiredTargets(node, cast)
                && Amount(node.Require("amount"), cast) > 0,
            "dealAttackDamage" => HasRequiredTargets(node, cast)
                && Amount(node.Require("amount"), cast) > 0,
            "placeThreat" => HasRequiredTargets(node, cast)
                && Amount(node.Require("amount"), cast) > 0,
            "removeThreat" => CanRemoveThreat(node, cast),
            "gainSurge" => Number(node.Argument) > 0,
            "draw" => CanDraw(node, cast),
            "drawToHandSize" => cast.World.Seats[Seat(node.Argument, cast)].Hand.Cards.Count
                < PhaseEnd.HandSize(
                    cast.World, cast.World.Seats[Seat(node.Argument, cast)], cast.World.Facts),
            "drawToPrintedHandSize" => CanDrawToPrintedHandSize(node, cast),
            "createDrones" => CanCreateDrones(node, cast),
            "placeAccelerationToken" => HasRequiredTargets(node, cast),
            "preventThreat" => cast.Occurrence.Threat is { Remaining: > 0 }
                && Amount(node.Argument, cast) > 0,
            "replaceThreatWithDamage" => cast.Occurrence.Threat is { Remaining: > 0 },
            "grantCharactersControlledBy" or "reduceNextCardCost" => true,

            // Target availability is the only state-dependent precondition
            // these currently expressible effects carry. Their own resolver
            // performs any further rule-specific work.
            "generate" or "soakDamage" or "preventDamage" or "cancelWhenRevealed"
                or "dealEncounterCards" or "dealEncounterCard"
                or "revealTop" or "reveal" or "placeAtRandom"
                or "returnToHand" or "discardUntil" or "recoverDiscardedByResource"
                or "shuffleInto" or "search" or "giveStatus" or "attachTo"
                or "grantUntil" or "delayUntil" or "discard" or "enemyAttacks"
                or "enemySchemes" or "putIntoPlay" or "shuffle" =>
                    HasRequiredTargets(node, cast),
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses '{node.Kind}' in an option whose partial "
                + "resolution is not implemented"),
        };
    }

    /// <summary>How completely one effect can resolve on the current board.</summary>
    /// <remarks>
    /// <para>
    /// This is the distinction the printed dependency words need:
    /// <c>rr:then</c> requires <see cref="ResolutionOutcome.Full"/>, while
    /// <c>rr:otherwise.1.2</c> permits its branch only for
    /// <see cref="ResolutionOutcome.None"/>. A partial effect takes neither.
    /// </para>
    /// <para>
    /// It is deliberately a closed vocabulary. A node whose outcome has not
    /// been made explicit raises before the preceding effect mutates the board;
    /// guessing “full” would silently resolve dependent text that should not
    /// happen.
    /// </para>
    /// </remarks>
    private static ResolutionOutcome ResolutionOf(AbilityNode node, Cast cast)
    {
        if (node.Kind is "choose" or "chooseCard" or "indirectDamage"
            or "resolveSpecials" or "payOrExhaust" or "chooseTopForHand"
            or "chooseDiscardToShuffle" or "thwartDifferentSchemes" or "makeTheCall"
            or "legalPractice" or "payOrEffect")
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses '{node.Kind}' before dependent text and it "
                + "suspends for a player choice");
        }

        return node.Kind switch
        {
            "seq" or "and" => CombinedOutcomes(
                Nodes(node.Argument).Select(effect => ResolutionOf(effect, cast))),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch
                    ? ResolutionOf(Tree(branch), cast)
                    : ResolutionOutcome.None,
            "forEach" when ForEachCount(node, cast) == 0 => ResolutionOutcome.None,
            "changeForm" => Forms.In(
                    cast.World,
                    cast.World.Seats[Seat(node.Require("player"), cast)],
                    cast.World.Facts,
                    Word(node.Require("to")))
                ? ResolutionOutcome.None
                : ResolutionOutcome.Full,
            "exhaust" => ResolutionOfCards(
                Every(node.Argument, cast), card => card.Ready),
            "ready" => ResolutionOfCards(
                Every(node.Argument, cast), card => !card.Ready
                    && cast.Abilities.CanReady(cast.World, card, cast.Source)),
            "discard" => Find(node.Field("card") ?? node.Argument, cast) is not null
                ? ResolutionOutcome.Full
                : ResolutionOutcome.None,
            "draw" => CombinedOutcomes(Seats(node.Require("player"), cast).Select(player =>
                ResolutionOfAmount(
                    cast.World.Seats[player].Deck.Cards.Count
                    + cast.World.AreaOf(
                        DeckType.DiscardPile, PlayArea.Of(player)).Cards.Count,
                    Number(node.Require("count"))))),
            "heal" => ResolutionOfAmount(
                Find(node.Require("card"), cast)?.Damage ?? 0,
                Amount(node.Require("amount"), cast)),
            "removeThreat" => ResolutionOfThreat(node, cast),
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses '{node.Kind}' before dependent text, whose "
                + "none/partial/full resolution is not implemented"),
        };
    }

    private static ResolutionOutcome CombinedOutcomes(
        IEnumerable<ResolutionOutcome> values)
    {
        var outcomes = values.ToList();
        if (outcomes.Count == 0 || outcomes.All(outcome => outcome == ResolutionOutcome.None))
        {
            return ResolutionOutcome.None;
        }

        return outcomes.All(outcome => outcome == ResolutionOutcome.Full)
            ? ResolutionOutcome.Full
            : ResolutionOutcome.Partial;
    }

    private static ResolutionOutcome ResolutionOfCards(
        IReadOnlyList<Card> cards, Func<Card, bool> affected)
    {
        // `rr:target.4.1`: a multi-target effect does not resolve against
        // invalid elements. Completeness is therefore measured across the
        // targets the effect can affect, not every element named by "each".
        return cards.Any(affected)
            ? ResolutionOutcome.Full
            : ResolutionOutcome.None;
    }

    private static ResolutionOutcome ResolutionOfAmount(long available, long wanted)
    {
        if (available <= 0 || wanted <= 0)
        {
            return ResolutionOutcome.None;
        }

        return available >= wanted
            ? ResolutionOutcome.Full
            : ResolutionOutcome.Partial;
    }

    private static ResolutionOutcome ResolutionOfThreat(AbilityNode node, Cast cast)
    {
        var schemes = Every(node.Require("scheme"), cast);
        long wanted = Amount(node.Require("amount"), cast);
        if (schemes.Count == 0 || wanted <= 0)
        {
            return ResolutionOutcome.None;
        }

        var valid = schemes.Where(scheme =>
            scheme.Tokens.GetValueOrDefault("k_threat") > 0
            && CanRemoveThreatFrom(node, cast, scheme));
        return CombinedOutcomes(valid.Select(scheme => ResolutionOfAmount(
            scheme.Tokens.GetValueOrDefault("k_threat"), wanted)));
    }

    private static void ResolveDependent(
        AbilityNode node, Cast cast, ResolutionOutcome required, string branch)
    {
        var effect = Tree(node.Require("effect"));
        var dependent = Tree(node.Require(branch));
        if (ActiveChoices(effect, cast).Any())
        {
            PreflightAnsweredOutcome(effect, cast);
            PreflightContinuationBoundaries(dependent, cast);
            RunChild(effect, $"{node.Kind}:effect:Pending", cast);
            return;
        }
        var outcome = EnsureDependentSupported(node, cast, effect, dependent, required);

        // A supported predecessor classified as `None` changes no state. Some
        // low-level resolvers deliberately reject a missing target when used
        // alone; dependency words make that absence an expected outcome, so
        // do not turn an advertised `otherwise` fallback into an exception.
        if (outcome != ResolutionOutcome.None)
        {
            RunChild(effect, $"{node.Kind}:effect:{outcome}", cast);
        }
        if (outcome == required)
        {
            RunChild(dependent, $"{node.Kind}:{branch}", cast);
        }
    }

    private static ResolutionOutcome EnsureDependentSupported(
        AbilityNode node,
        Cast cast,
        AbilityNode effect,
        AbilityNode dependent,
        ResolutionOutcome required)
    {
        PreflightResolutionBranches(effect, cast);

        var outcome = ResolutionOf(effect, cast);
        bool stateMayChange = cast.PaymentMayMutate || cast.PriorStepMayMutate;
        if ((outcome == required || stateMayChange)
            && ContainsNode(dependent, "placeThreat", cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses '{node.Kind}' before dependent text that "
                + "needs a nested continuation");
        }

        return outcome;
    }

    private static void PreflightAnsweredOutcome(AbilityNode node, Cast cast)
    {
        void PreflightEffect(AbilityNode effect)
        {
            if (ActiveChoices(effect, cast).Any())
            {
                PreflightAnsweredOutcome(effect, cast);
            }
            else
            {
                _ = ResolutionOf(effect, cast);
            }
        }

        if (node.Kind == "choose")
        {
            foreach (var option in Nodes(node.Require("options")))
            {
                PreflightEffect(option);
            }
            return;
        }
        if (node.Kind == "chooseCard")
        {
            PreflightEffect(Tree(node.Require("effect")));
            return;
        }
        var choices = ActiveChoices(node, cast).ToList();
        if (choices.Count > 0)
        {
            foreach (var choice in choices)
            {
                PreflightAnsweredOutcome(choice, cast);
            }
            return;
        }
        throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' uses '{node.Kind}' before dependent text, whose "
            + "answered resolution outcome is not implemented");
    }

    private static void PreflightResolutionBranches(
        AbilityNode node, Cast cast, bool allBranches = false)
    {
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            var branches = allBranches || cast.PriorStepMayMutate || PaymentCanChange(test)
                ? Branches.Select(node.Field).Where(value => value is not null)
                : node.Field(Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            foreach (var branch in branches)
            {
                PreflightResolutionBranches(Tree(branch!), cast, allBranches);
            }
            return;
        }

        _ = ResolutionOf(node, cast);
    }

    private static bool PaymentCanChange(AbilityNode test) => test.Kind switch
    {
        "and" or "or" => Nodes(test.Argument).Any(PaymentCanChange),
        "not" => PaymentCanChange(Tree(test.Argument)),

        // Paying an ability cannot change identity form. Other predicates may
        // read the chosen resources, the source's in-play status, or another
        // fact changed by an authored cost, so their branches are preflighted
        // conservatively.
        "inForm" => false,
        _ => true,
    };

    private static bool ContainsNode(AbilityNode node, string kind, Cast cast) =>
        node.Kind == kind
        || !StableZeroForEach(node, cast)
            && StructuralChildren(node).Any(child => ContainsNode(child, kind, cast));

    private static bool HasNestedEachPlayer(
        AbilityNode node, Cast cast, bool inside = false, bool stateMayChange = false,
        bool bindingMayChange = false, AbilityNode? repeatedEffect = null)
    {
        if (inside && node.Kind == "eachPlayer")
        {
            return true;
        }
        if (node.Kind == "eachPlayer")
        {
            int original = cast.Player;
            try
            {
                var players = cast.World.PlayerOrder.ToList();
                foreach (int player in players)
                {
                    cast.RestorePlayer(player);
                    if (HasNestedEachPlayer(
                        Tree(node.Require("effect")), cast, inside: true,
                        stateMayChange, bindingMayChange,
                        players.Count > 1 ? Tree(node.Require("effect")) : repeatedEffect))
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                cast.RestorePlayer(original);
            }
        }
        bool within = inside || node.Kind == "eachPlayer";
        return GuardChildren(
            node, cast, stateMayChange, bindingMayChange, repeatedEffect).Any(child =>
            HasNestedEachPlayer(
                child.Node, cast, within, child.StateMayChange,
                child.BindingMayChange, repeatedEffect));
    }

    private static bool ContainsUnsupportedPower(
        AbilityNode node, Cast cast, bool stateMayChange = false,
        bool bindingMayChange = false, AbilityNode? repeatedEffect = null)
    {
        if (node.Kind == "eachPlayer")
        {
            int original = cast.Player;
            try
            {
                var players = cast.World.PlayerOrder.ToList();
                foreach (int player in players)
                {
                    cast.RestorePlayer(player);
                    if (ContainsUnsupportedPower(
                        Tree(node.Require("effect")), cast,
                        stateMayChange, bindingMayChange,
                        players.Count > 1 ? Tree(node.Require("effect")) : repeatedEffect))
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                cast.RestorePlayer(original);
            }
        }
        if (node.Kind is "attack" or "thwart")
        {
            var prior = cast.Chosen;
            try
            {
                var target = Find(node.Require("target"), cast);
                bool targetWillBind = target is null;
                if (target is not null)
                {
                    cast.Choose(target);
                }
                if (SuspendsPowerEffect(
                    Tree(node.Require("effect")), cast, stateMayChange,
                    bindingMayChange || targetWillBind))
                {
                    return true;
                }
            }
            finally
            {
                cast.Choose(prior);
            }
        }
        if (node.Kind == "thwartSchemes")
        {
            var power = Tree(node.Require("power"));
            if (SuspendsPowerEffect(
                Tree(power.Require("effect")), cast, stateMayChange, bindingMayChange))
            {
                return true;
            }
        }
        return GuardChildren(
            node, cast, stateMayChange, bindingMayChange, repeatedEffect).Any(child =>
            ContainsUnsupportedPower(
                child.Node, cast, child.StateMayChange,
                child.BindingMayChange, repeatedEffect));
    }

    /// <summary>Executable children that can be reached after an ability is offered.</summary>
    private static IEnumerable<(
        AbilityNode Node, bool StateMayChange, bool BindingMayChange)> GuardChildren(
        AbilityNode node, Cast cast, bool stateMayChange, bool bindingMayChange,
        AbilityNode? repeatedEffect)
    {
        if (node.Kind == "forEach")
        {
            bool countWillBind = bindingMayChange && HasUnboundPowerAmount(node, cast);
            long? count = countWillBind ? null : ForEachCount(node, cast);
            if (!countWillBind
                && (stateMayChange || bindingMayChange || cast.PaymentMayMutate)
                && AmountMayChange(node.Require("count")))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' reaches a for-each count after state may change");
            }
            if (count == 0)
            {
                return [];
            }
        }

        if (node.Kind == "seq")
        {
            return Nodes(node.Argument).Select((child, index) =>
                (child, stateMayChange || index > 0, bindingMayChange));
        }
        if (node.Kind == "and")
        {
            var children = Nodes(node.Argument).ToList();
            return children.Select(child =>
                (child, stateMayChange || children.Count > 1, bindingMayChange));
        }
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            bool canSwitch = stateMayChange
                || cast.PaymentMayMutate && PaymentCanChange(test)
                || bindingMayChange && BindingCanChange(test.Argument)
                || repeatedEffect is not null
                    && RepeatedEffectCanChange(test, repeatedEffect, cast);
            var branches = canSwitch
                ? Branches.Select(node.Field).Where(value => value is not null)
                : node.Field(Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            return branches.Select(value =>
                (Tree(value!), stateMayChange, bindingMayChange));
        }
        if (node.Kind is "then" or "otherwise")
        {
            var effect = Tree(node.Require("effect"));
            var dependent = Tree(node.Require(node.Kind));
            var required = node.Kind == "then"
                ? ResolutionOutcome.Full
                : ResolutionOutcome.None;
            bool answered = ActiveChoices(effect, cast).Any();
            bool dependentCanRun = stateMayChange
                || cast.PaymentMayMutate
                || bindingMayChange
                || answered
                || ResolutionOf(effect, cast) == required;
            bool predecessorMayMutate = stateMayChange
                || cast.PaymentMayMutate
                || node.Kind == "then"
                || answered;
            return dependentCanRun
                ? [
                    (effect, stateMayChange, bindingMayChange),
                    (dependent, predecessorMayMutate, bindingMayChange),
                ]
                : [(effect, stateMayChange, bindingMayChange)];
        }
        if (node.Kind is "chooseCard" or "thwartSchemes"
            or "thwartDifferentSchemes" or "legalPractice")
        {
            return ContinuationChildren(node).Select(child =>
                (child, stateMayChange, true));
        }
        if (node.Kind is "afterActivation" or "delayUntil" or "defense"
            or "payOrEffect" or "payOrExhaust")
        {
            return ContinuationChildren(node).Select(child =>
                (child, true, bindingMayChange));
        }
        return ContinuationChildren(node).Select(child =>
            (child, stateMayChange, bindingMayChange));
    }

    private static bool BindingCanChange(AbilityValue value) => value switch
    {
        AbilityValue.Word word => word.Value is "chosen" or "chosenPlayer"
            or "powerTargets" or "enemiesEngagedWithChosenPlayer"
            or "topmostTechInChosenDiscard" or "that",
        AbilityValue.List list => list.Values.Any(BindingCanChange),
        AbilityValue.Map map => map.Entries.Keys.Any(key => key == "powerAmount")
            || map.Entries.Values.Any(BindingCanChange),
        _ => false,
    };

    private static bool RequiresChosenPlayer(AbilityValue value) => value switch
    {
        AbilityValue.Word word => word.Value is "chosenPlayer"
            or "enemiesEngagedWithChosenPlayer"
            or "topmostTechInChosenDiscard",
        AbilityValue.List list => list.Values.Any(RequiresChosenPlayer),
        AbilityValue.Map map => map.Entries.Values.Any(RequiresChosenPlayer),
        _ => false,
    };

    private static bool RepeatedEffectCanChange(
        AbilityNode test, AbilityNode effect, Cast cast)
    {
        int original = cast.Player;
        try
        {
            var assumed = RepeatedChange.None;
            int priorFrames = Math.Max(0, cast.World.PlayerOrder.Count() - 1);
            for (int frame = 0; frame < priorFrames; frame++)
            {
                var observed = RepeatedChange.None;
                foreach (int player in cast.World.PlayerOrder)
                {
                    cast.RestorePlayer(player);
                    observed |= RepeatedChanges(
                        effect, cast, assumed, binding: false, priorFrames,
                        effect);
                }
                assumed |= observed;
            }
            return RepeatedTestCanChange(test, assumed);
        }
        finally
        {
            cast.RestorePlayer(original);
        }
    }

    private static RepeatedChange RepeatedChanges(
        AbilityNode node, Cast cast, RepeatedChange assumed, bool binding,
        int priorFrames, AbilityNode repeatedEffect)
    {
        if (node.Kind == "changeForm")
        {
            return RepeatedChange.Form | RepeatedChange.CardsInPlay;
        }
        if (node.Kind is "dealDamage" or "indirectDamage" or "moveDamage"
            or "replaceThreatWithDamage")
        {
            return RepeatedChange.CardsInPlay
                | (DamageCanChangePlayerOrder(
                        node, cast, binding, repeatedEffect, assumed,
                        priorFrames)
                    ? RepeatedChange.PlayerOrder
                    : RepeatedChange.None);
        }
        if (node.Kind is "dealAttackDamage" or "moveAttackDamage")
        {
            return RepeatedChange.CardsInPlay;
        }
        if (node.Kind == "enemyAttacks")
        {
            return RepeatedChange.CardsInPlay | RepeatedChange.PlayerOrder;
        }
        if (StableForCardsInPlay(
            node, cast, priorFrames, repeatedEffect, assumed, binding))
        {
            return RepeatedChange.None;
        }
        if (node.Kind is "seq" or "then" or "otherwise")
        {
            var changes = RepeatedChange.None;
            foreach (var child in MutationChildren(node))
            {
                var next = RepeatedChanges(
                    child, cast, assumed | changes, binding, priorFrames,
                    repeatedEffect);
                changes |= next;
            }
            return changes;
        }
        if (node.Kind == "and")
        {
            var ordered = MutationChildren(node).ToList();
            var changes = RepeatedChange.None;
            for (int pass = 0; pass < ordered.Count; pass++)
            {
                var before = changes;
                foreach (var child in ordered)
                {
                    changes |= RepeatedChanges(
                        child, cast, assumed | changes, binding, priorFrames,
                        repeatedEffect);
                }
                if (changes == before)
                {
                    break;
                }
            }
            return changes;
        }
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            var branches = RepeatedTestCanChange(test, assumed)
                    || binding && BindingCanChange(test.Argument)
                ? Branches.Select(node.Field).Where(value => value is not null)
                : node.Field(Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            return branches.Aggregate(
                RepeatedChange.None,
                (changes, branch) => changes
                    | RepeatedChanges(
                        Tree(branch!), cast, assumed, binding, priorFrames,
                        repeatedEffect));
        }
        if (node.Kind is "chooseCard" or "thwartSchemes"
            or "thwartDifferentSchemes" or "legalPractice")
        {
            binding = true;
        }
        var children = MutationChildren(node).ToList();
        if (children.Count == 0)
        {
            return RepeatedChange.CardsInPlay;
        }
        return children.Aggregate(
            RepeatedChange.None,
            (changes, child) => changes
                | RepeatedChanges(
                    child, cast, assumed, binding, priorFrames,
                    repeatedEffect));
    }

    private static bool RepeatedTestCanChange(
        AbilityNode test, RepeatedChange changes) => test.Kind switch
        {
            "and" or "or" => Nodes(test.Argument).Any(child =>
                RepeatedTestCanChange(child, changes)),
            "not" => RepeatedTestCanChange(Tree(test.Argument), changes),
            "inForm" => changes.HasFlag(RepeatedChange.Form)
                || changes.HasFlag(RepeatedChange.PlayerOrder)
                    && Word(test.Require("player")) != AbilityPlayers.You,
            "titleInPlay" => changes.HasFlag(RepeatedChange.CardsInPlay),
            "finalStep" or "paidWithResource" or "threatCause" => false,
            _ => true,
        };

    [Flags]
    private enum RepeatedChange
    {
        None = 0,
        Form = 1,
        CardsInPlay = 2,
        PlayerOrder = 4,
    }

    private static bool StableForCardsInPlay(
        AbilityNode node, Cast cast, int priorFrames,
        AbilityNode repeatedEffect, RepeatedChange assumed,
        bool binding) =>
        node.Kind is "draw" or "drawToHandSize" or "drawToPrintedHandSize"
            or "exhaust" or "ready" or "heal" or "generate" or "giveStatus"
            or "gainSurge" or "preventDamage" or "preventThreat"
            or "cancelWhenRevealed" or "grantUntil"
            or "grantCharactersControlledBy" or "reduceNextCardCost"
        || node.Kind == "removeThreat"
            && Every(node.Require("scheme"), cast) is { Count: > 0 } schemes
            && schemes.All(scheme => scheme.Area.Type == DeckType.MainSchemesArea
                || !CanExhaust(
                    // An earlier ordered mutation can switch a branch before
                    // this leaf is reached in the same repeated frame.
                    TotalThreatRemoved(
                        scheme, repeatedEffect, cast, assumed, binding),
                    priorFrames,
                    scheme.Tokens.GetValueOrDefault("k_threat")));

    private static bool DamageCanChangePlayerOrder(
        AbilityNode node, Cast cast, bool binding,
        AbilityNode repeatedEffect, RepeatedChange assumed,
        int priorFrames)
    {
        AbilityValue targets = node.Kind switch
        {
            "dealDamage" => node.Require("cards"),
            "indirectDamage" => node.Require("among"),
            "moveDamage" => node.Require("to"),
            "replaceThreatWithDamage" => node.Require("card"),
            _ => throw new InvalidOperationException(
                $"'{node.Kind}' is not a direct damage node"),
        };
        int damagingFrames = RebindsToEachPlayer(targets)
            ? 1
            : priorFrames;
        var cards = Every(targets, cast);
        return cards.Any(card => cast.World.PlayerOrder.Any(player =>
                cast.World.Seats[player].IdentityCard == card)
            && TotalRepeatedDamageTo(
                card, repeatedEffect, cast, assumed, binding,
                damagingFrames)
                >= Damage.Health(cast.World, cast.World.Facts, card) - card.Damage)
            || cards.Count == 0 && binding && BindingCanChange(targets);
    }

    private static long TotalRepeatedDamageTo(
        Card target, AbilityNode repeatedEffect, Cast cast,
        RepeatedChange assumed, bool binding, int frames)
        => PeakRepeatedDamageOn(
            target, repeatedEffect, cast, assumed, binding, frames)
            - target.Damage;

    private static bool RebindsToEachPlayer(AbilityValue targets) => targets switch
    {
        AbilityValue.Word word => word.Value is "you" or "yourHero" or "yourAlterEgo",
        AbilityValue.Map => RebindsToEachPlayer(Tree(targets)),
        _ => false,
    };

    private static bool RebindsToEachPlayer(AbilityNode targets) => targets.Kind switch
    {
        "query" => targets.Argument is AbilityValue.Word
            { Value: "charactersYouControl" },
        "withTrait" => RebindsToEachPlayer(targets.Require("cards")),
        "minBy" or "maxBy" => RebindsToEachPlayer(targets.Require("of")),
        "withoutAnotherCopyAttached" => RebindsToEachPlayer(targets.Argument),
        _ => false,
    };

    private static long TotalThreatRemoved(
        Card scheme, AbilityNode node, Cast cast,
        RepeatedChange assumed = RepeatedChange.None, bool binding = false)
    {
        long own = node.Kind == "removeThreat"
            && Every(node.Require("scheme"), cast).Any(candidate =>
                candidate.ObjectId == scheme.ObjectId)
                ? Amount(node.Require("amount"), cast)
                : 0;
        return MutationTotal(
            node, cast, assumed, binding, own,
            child => TotalThreatRemoved(
                scheme, child, cast, assumed, binding));
    }

    private readonly record struct DamageTransfer(
        int From, int To, long Amount,
        bool GrantsHealth = false, bool DealsDamage = false,
        bool Discards = false, bool EntersPlay = false,
        bool RemovesThreat = false,
        bool PlacesThreat = false, string? GrantsTrait = null,
        string? GrantsField = null,
        AbilityValue? FromVillain = null, AbilityValue? ToVillain = null,
        int ChangesForm = -1, string? Form = null,
        string? GrantsStatus = null);

    private readonly record struct TraceCard(
        Card Card, AbilityValue? VillainSelector);

    private sealed record DamageTraceState(
        Dictionary<int, long> Damage, long PeakTargetPressure,
        HashSet<int> Players, Dictionary<int, int> Tough,
        HashSet<(int Card, string Status)> StatusChanges,
        Dictionary<(int Card, string Status), int> StatusCounts,
        Dictionary<int, long> Health, HashSet<int> Discarded,
        Dictionary<int, long> Threat, Dictionary<int, HashSet<string>> Traits,
        Dictionary<(int Card, string Field), long> Modifiers,
        Dictionary<int, int> Engagement,
        ulong FormsMayChange,
        int FirstPlayer,
        int CurrentVillain,
        int VillainStagesDrawn, bool Finished);

    private static long PeakRepeatedDamageOn(
        Card target, AbilityNode repeatedEffect, Cast cast,
        RepeatedChange assumed, bool binding, int frames)
    {
        int original = cast.Player;
        try
        {
            int villain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
            IReadOnlyList<DamageTraceState> states =
                [new(new Dictionary<int, long>(), target.Damage, [], new(), new(),
                    new(), new(), TraceUnavailableMinions(cast), new(),
                    new(), new(), new(), 0, cast.World.FirstPlayer,
                    villain, 0, false)];
            for (int frame = 0; frame < frames; frame++)
            {
                var next = new List<DamageTraceState>();
                foreach (var state in states)
                {
                    foreach (int player in cast.World.PlayerOrder.Where(player =>
                        !state.Players.Contains(player)))
                    {
                        cast.RestorePlayer(player);
                        var traces = DamageTraces(
                            repeatedEffect, cast, assumed, binding);
                        foreach (var trace in traces)
                        {
                            var advanced = ApplyDamageTrace(
                                state, trace, target, cast);
                            next.Add(advanced with
                            {
                                Players = [.. state.Players, player],
                            });
                        }
                    }
                }
                states = next;
            }
            return states.Select(state => state.PeakTargetPressure)
                .DefaultIfEmpty(target.Damage)
                .Max();
        }
        finally
        {
            cast.RestorePlayer(original);
        }
    }

    private static HashSet<int> TraceUnavailableMinions(Cast cast) =>
        cast.World.Areas
            .SelectMany(area => area.Cards)
            .Where(card => card.Area.Type != DeckType.EngagedEnemiesArea
                && FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion)
            .Select(card => card.ObjectId)
            .ToHashSet();

    private static DamageTraceState ApplyDamageTrace(
        DamageTraceState state, IReadOnlyList<DamageTransfer> trace,
        Card target, Cast cast)
    {
        var damage = new Dictionary<int, long>(state.Damage);
        var tough = new Dictionary<int, int>(state.Tough);
        var statusChanges = new HashSet<(int Card, string Status)>(
            state.StatusChanges);
        var statusCounts = new Dictionary<(int Card, string Status), int>(
            state.StatusCounts);
        var health = new Dictionary<int, long>(state.Health);
        var discarded = new HashSet<int>(state.Discarded);
        var threat = new Dictionary<int, long>(state.Threat);
        var traits = state.Traits.ToDictionary(
            pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.Ordinal));
        var modifiers = new Dictionary<(int Card, string Field), long>(state.Modifiers);
        var engagement = new Dictionary<int, int>(state.Engagement);
        ulong formsMayChange = state.FormsMayChange;
        int firstPlayer = state.FirstPlayer;
        int currentVillain = state.CurrentVillain;
        int villainStagesDrawn = state.VillainStagesDrawn;
        bool finished = state.Finished;
        long peak = state.PeakTargetPressure;
        long Current(int card) => damage.TryGetValue(card, out long amount)
            ? amount
            : cast.World.Cards[card].Damage;
        int CurrentTough(int card) => tough.TryGetValue(card, out int count)
            ? count
            : Statuses.Count(cast.World, cast.World.Cards[card], Statuses.Tough);
        long HealthBonus(int card) => health.GetValueOrDefault(card);
        void ObserveTarget() => peak = Math.Max(
            peak, Current(target.ObjectId) - HealthBonus(target.ObjectId));
        long CurrentThreat(int card) => threat.TryGetValue(card, out long amount)
            ? amount
            : cast.World.Cards[card].Tokens.GetValueOrDefault("k_threat");
        void LeavePlay(int cardId)
        {
            var leaving = new List<int> { cardId };
            var pending = new Stack<Card>(cast.World.Areas
                .Where(area => area.Host == cardId)
                .SelectMany(area => area.Cards)
                .Reverse());
            var seen = new HashSet<int> { cardId };
            while (pending.TryPop(out var hosted))
            {
                if (!seen.Add(hosted.ObjectId))
                {
                    throw new RulesNotImplementedException(
                        $"attachment {hosted.ObjectId} forms a hosting cycle");
                }
                if (StateFields.Modified(
                        cast.World, hosted, "permanent",
                        cast.World.Facts, cast.World.Players) > 0)
                {
                    // Match Discard.Attachments' complete-tree preflight so
                    // eligibility refuses before an action cost can mutate.
                    throw new RulesNotImplementedException(
                        $"permanent attachment {hosted.ObjectId} lost host "
                        + $"{cardId}, and rr:permanent.5 is not implemented");
                }

                leaving.Add(hosted.ObjectId);
                foreach (var child in cast.World.Areas
                    .Where(area => area.Host == hosted.ObjectId)
                    .SelectMany(area => area.Cards)
                    .Reverse())
                {
                    pending.Push(child);
                }
            }

            foreach (int leavingId in leaving)
            {
                discarded.Add(leavingId);
                if (Statuses.Count(
                    cast.World, cast.World.Cards[leavingId], Statuses.Tough) > 0)
                {
                    tough[leavingId] = 0;
                }
                else
                {
                    tough.Remove(leavingId);
                }
                TraceStatusesLeave(
                    leavingId, cast, statusCounts, statusChanges);
                engagement.Remove(leavingId);
            }
        }
        void ResolveCharacterDefeat(int cardId)
        {
            var card = cast.World.Cards[cardId];
            long tracedHealth = SaturatingAdd(
                TraceHealth(card, discarded, threat, cast),
                HealthBonus(cardId));
            if (cardId == currentVillain
                && Current(cardId) >= tracedHealth)
            {
                var deck = cast.World.AreaOf(DeckType.VillainDeck);
                int nextIndex = deck.Cards.Count - 1 - villainStagesDrawn;
                if (nextIndex < 0)
                {
                    LeavePlay(cardId);
                    finished = true;
                    return;
                }

                var next = deck.Cards[nextIndex];
                bool carriesAttachments = string.Equals(
                    cast.World.Facts.Title(card.FaceId),
                    cast.World.Facts.Title(next.FaceId),
                    StringComparison.Ordinal);
                if (carriesAttachments)
                {
                    discarded.Add(cardId);
                }
                else
                {
                    LeavePlay(cardId);
                }
                if (cast.Abilities is AbilityRunner runner
                    && runner.On(next).Any(ability =>
                        ability.Trigger.Timing == AbilityType.Constant))
                {
                    throw new RulesNotImplementedException(
                        $"villain stage '{next.FaceId}' enters play before a "
                        + "repeated continuation reads its constant abilities, "
                        + "which is not implemented");
                }
                if (HasLiveVillainRetargetingConstant(
                    discarded, card, next, cast,
                    threatChanges: threat,
                    damageChanges: damage,
                    modifierChanges: modifiers,
                    traitChanges: traits,
                    statusChanges:
                    [
                        .. statusChanges,
                        .. tough.Keys.Select(card => (card, Statuses.Tough)),
                    ],
                    engagementChanges: engagement,
                    formsMayChange: formsMayChange,
                    traceFirstPlayer: firstPlayer))
                {
                    throw new RulesNotImplementedException(
                        $"villain stage '{next.FaceId}' enters play before a "
                        + "repeated continuation reads retargeting constant "
                        + "abilities, which is not implemented");
                }
                villainStagesDrawn++;
                int carriedTough = carriesAttachments
                    ? CurrentTough(cardId)
                    : 0;
                currentVillain = next.ObjectId;
                damage[next.ObjectId] = 0;
                tough[next.ObjectId] = Math.Max(
                    carriedTough,
                    StateFields.Modified(
                        cast.World, next, "toughness",
                        cast.World.Facts, cast.World.Players) > 0 ? 1 : 0);
                return;
            }
            if ((card.Area.Type is DeckType.EngagedEnemiesArea
                    or DeckType.AlliesArea
                    || engagement.ContainsKey(cardId))
                && Current(cardId) >= tracedHealth)
            {
                LeavePlay(cardId);
            }
            int eliminated = cast.World.Seats
                .Select((seat, player) => (seat, player))
                .Where(pair => pair.seat.IdentityCard.ObjectId == cardId)
                .Select(pair => pair.player)
                .DefaultIfEmpty(-1)
                .First();
            if (eliminated >= 0 && Current(cardId) >= tracedHealth)
            {
                var plan = PlanTracePlayerElimination(
                    eliminated, cast, discarded, engagement);
                foreach (int relocated in plan.RelocatedCards)
                {
                    engagement[relocated] = plan.NextPlayer!.Value;
                }
                foreach (int leaving in plan.Leaving)
                {
                    discarded.Add(leaving);
                    if (Statuses.Count(
                            cast.World, cast.World.Cards[leaving],
                            Statuses.Tough) > 0)
                    {
                        tough[leaving] = 0;
                    }
                    else
                    {
                        tough.Remove(leaving);
                    }
                    TraceStatusesLeave(
                        leaving, cast, statusCounts, statusChanges);
                    engagement.Remove(leaving);
                }
            }
            if (eliminated == firstPlayer
                && Current(cardId) >= tracedHealth)
            {
                for (int offset = 1; offset < cast.World.Seats.Count; offset++)
                {
                    int candidate = (firstPlayer + offset) % cast.World.Seats.Count;
                    var identity = cast.World.Seats[candidate].IdentityCard;
                    long candidateHealth = SaturatingAdd(
                        TraceHealth(identity, discarded, threat, cast),
                        HealthBonus(identity.ObjectId));
                    if (Current(identity.ObjectId) < candidateHealth)
                    {
                        firstPlayer = candidate;
                        break;
                    }
                }
            }
        }

        foreach (var transfer in trace)
        {
            if (finished)
            {
                break;
            }

            int from = transfer.FromVillain is { } fromSelector
                ? TraceSelectorIncludesCard(
                    fromSelector, transfer.From, currentVillain,
                    cast, discarded, traits, modifiers, engagement)
                    is int tracedFrom ? tracedFrom
                    : int.MinValue
                : transfer.From;
            int to = transfer.ToVillain is { } toSelector
                ? TraceSelectorIncludesCard(
                    toSelector, transfer.To, currentVillain,
                    cast, discarded, traits, modifiers, engagement)
                    is int tracedTo ? tracedTo
                    : int.MinValue
                : transfer.To;
            if (from == int.MinValue || to == int.MinValue)
            {
                continue;
            }
            if (transfer.ChangesForm >= 0)
            {
                int seat = transfer.ChangesForm;
                ulong bit = PlayerSeat(seat);
                bool destinationIsCurrent = Forms.In(
                    cast.World, cast.World.Seats[seat], cast.World.Facts,
                    transfer.Form!);
                formsMayChange = destinationIsCurrent
                    ? formsMayChange & ~bit
                    : formsMayChange | bit;
                continue;
            }
            if (transfer.EntersPlay)
            {
                if (!discarded.Remove(to))
                {
                    // A repeated frame is reconstructed from the unchanged
                    // World, so its branch may still contain the same entry.
                    // Once trace-local play has made the card available, that
                    // later transfer is a no-op and must not migrate its
                    // engagement or restore its printed statuses.
                    continue;
                }
                engagement[to] = Resolver(cast);
                int enteredTough = Math.Max(
                    CurrentTough(to),
                    StateFields.Modified(
                        cast.World, cast.World.Cards[to], "toughness",
                        cast.World.Facts, cast.World.Players) > 0 ? 1 : 0);
                int liveTough = Statuses.Count(
                    cast.World, cast.World.Cards[to], Statuses.Tough);
                if (enteredTough == liveTough)
                {
                    tough.Remove(to);
                }
                else
                {
                    tough[to] = enteredTough;
                }
                TraceSetStatusCount(
                    cast.World.Cards[to], Statuses.Tough, enteredTough, cast,
                    statusCounts, statusChanges);
                continue;
            }
            if (transfer.RemovesThreat || transfer.PlacesThreat)
            {
                long current = CurrentThreat(to);
                long changed = transfer.PlacesThreat
                    ? SaturatingSum(current, [transfer.Amount])
                    : Math.Max(0, current - transfer.Amount);
                threat[to] = changed;
                if (transfer.RemovesThreat && changed == 0
                    && cast.World.Cards[to].Area.Type
                        == DeckType.SideSchemesArea)
                {
                    discarded.Add(to);
                }
                continue;
            }
            if (transfer.Discards)
            {
                LeavePlay(to);
                continue;
            }
            if (transfer.GrantsHealth)
            {
                health[to] = SaturatingSum(
                    HealthBonus(to), [transfer.Amount]);
                ObserveTarget();
                continue;
            }
            if (transfer.GrantsTrait is { } gainedTrait)
            {
                if (!traits.TryGetValue(to, out var gained))
                {
                    gained = new HashSet<string>(StringComparer.Ordinal);
                    traits[to] = gained;
                }
                gained.Add(gainedTrait);
                continue;
            }
            if (transfer.GrantsField is { } gainedField)
            {
                var key = (to, gainedField);
                long changed = SaturatingSum(
                    modifiers.GetValueOrDefault(key), [transfer.Amount]);
                if (changed == 0)
                {
                    modifiers.Remove(key);
                }
                else
                {
                    modifiers[key] = changed;
                }
                continue;
            }
            if (transfer.GrantsStatus is { } grantedStatus)
            {
                if (discarded.Contains(to))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' would give a status to a card "
                        + "that is not there");
                }
                var statusTarget = cast.World.Cards[to];
                if (grantedStatus == Statuses.Tough)
                {
                    int toughCurrent = CurrentTough(to);
                    if (toughCurrent >= 1)
                    {
                        continue;
                    }
                    int toughLive = Statuses.Count(
                        cast.World, statusTarget, Statuses.Tough);
                    if (toughLive == 1)
                    {
                        tough.Remove(to);
                    }
                    else
                    {
                        tough[to] = 1;
                    }
                    TraceSetStatusCount(
                        statusTarget, Statuses.Tough, 1, cast,
                        statusCounts, statusChanges);
                    continue;
                }
                var key = (to, grantedStatus);
                int live = Statuses.Count(
                    cast.World, statusTarget, grantedStatus);
                int current = statusCounts.GetValueOrDefault(key, live);
                int limit = TraceStatusLimit(
                    statusTarget, grantedStatus, cast, discarded, modifiers);
                if (current >= limit)
                {
                    continue;
                }
                int changed = current + 1;
                TraceSetStatusCount(
                    statusTarget, grantedStatus, changed, cast,
                    statusCounts, statusChanges);
                if (TraceStatusMakesVulnerable(
                    statusTarget, grantedStatus, changed, limit,
                    cast, discarded, modifiers))
                {
                    LeavePlay(to);
                }
                continue;
            }
            if (from < 0)
            {
                if (transfer.DealsDamage
                    && !CanTakeDamageInTrace(
                        cast, cast.World.Cards[to], discarded))
                {
                    continue;
                }
                long incoming = transfer.DealsDamage
                    ? AfterForcedDamageReplacements(
                        cast, to, transfer.Amount, damage, discarded,
                        currentVillain)
                    : transfer.Amount;
                if (incoming > 0 && CurrentTough(to) > 0)
                {
                    tough[to] = CurrentTough(to) - 1;
                    continue;
                }
                damage[to] = SaturatingSum(Current(to), [incoming]);
                ResolveCharacterDefeat(to);
                ObserveTarget();
                continue;
            }

            long available = Current(from);
            long amount = Math.Min(available, transfer.Amount);
            if (to >= 0 && transfer.DealsDamage
                && !CanTakeDamageInTrace(
                    cast, cast.World.Cards[to], discarded))
            {
                continue;
            }
            damage[from] = available - amount;
            long landed = to >= 0 && transfer.DealsDamage
                ? AfterForcedDamageReplacements(
                    cast, to, amount, damage, discarded, currentVillain)
                : amount;
            if (to >= 0)
            {
                if (landed > 0 && CurrentTough(to) > 0)
                {
                    tough[to] = CurrentTough(to) - 1;
                }
                else
                {
                    damage[to] = SaturatingSum(Current(to), [landed]);
                    ResolveCharacterDefeat(to);
                }
            }
            ObserveTarget();
        }
        return new DamageTraceState(
            damage, peak, state.Players, tough, statusChanges,
            statusCounts, health, discarded, threat,
            traits, modifiers, engagement,
            formsMayChange, firstPlayer,
            currentVillain, villainStagesDrawn, finished);
    }

    private static long AfterForcedDamageReplacements(
        Cast cast, int target, long amount, Dictionary<int, long> damage,
        HashSet<int> discarded, int currentVillain)
    {
        if (amount <= 0 || cast.Abilities is not AbilityRunner runner)
        {
            return amount;
        }

        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        foreach (var area in cast.World.Areas.Where(area =>
            area.Host == target
            || target == currentVillain && area.Host == boardVillain
                && currentVillain >= 0 && boardVillain >= 0
                && string.Equals(
                    cast.World.Facts.Title(cast.World.Cards[currentVillain].FaceId),
                    cast.World.Facts.Title(cast.World.Cards[boardVillain].FaceId),
                    StringComparison.Ordinal)))
        {
            foreach (var attachment in area.Cards.Where(card =>
                !discarded.Contains(card.ObjectId)))
            {
                var replacement = runner.On(attachment).FirstOrDefault(ability =>
                    ability.Trigger.Timing == AbilityType.ForcedInterrupt
                    && ContainsEffect(ability.Effect, "soakDamage"));
                if (replacement is null)
                {
                    continue;
                }

                long placed = SaturatingSum(
                    damage.TryGetValue(attachment.ObjectId, out long traced)
                        ? traced
                        : attachment.Damage,
                    [amount]);
                damage[attachment.ObjectId] = placed;
                long threshold = SoakDiscardThreshold(replacement.Effect);
                if (threshold > 0 && placed >= threshold)
                {
                    discarded.Add(attachment.ObjectId);
                }
                return 0;
            }
        }
        return amount;
    }

    private static bool ContainsEffect(AbilityNode node, string kind) =>
        node.Kind == kind || MutationChildren(node).Any(child =>
            ContainsEffect(child, kind));

    private static long SoakDiscardThreshold(AbilityNode node)
    {
        if (node.Kind == "if"
            && Tree(node.Require("test")) is { Kind: "atLeast" } test
            && Tree(test.Require("value")) is { Kind: "damageOn" }
            && node.Field("then") is { } then
            && ContainsEffect(Tree(then), "discard"))
        {
            return Number(test.Require("count"));
        }
        return MutationChildren(node)
            .Select(SoakDiscardThreshold)
            .FirstOrDefault(threshold => threshold > 0);
    }

    private static bool CanTakeDamageInTrace(
        Cast cast, Card target, HashSet<int> discarded)
    {
        if (discarded.Contains(target.ObjectId))
        {
            return false;
        }
        if (cast.Abilities is not AbilityRunner runner)
        {
            return cast.Abilities.CanTakeDamage(
                cast.World, target, cast.Source);
        }

        foreach (var ability in runner.On(target).Where(ability =>
            ability.Trigger.Timing == AbilityType.Constant))
        {
            var constant = new Cast(
                cast.World, target, new Occurrence(0, []),
                ControllerOf(cast.World, target), [], runner);
            if (ProhibitsDamageInTrace(
                ability.Effect, constant, cast.Source, discarded))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ProhibitsDamageInTrace(
        AbilityNode node, Cast cast, Card source,
        HashSet<int> discarded) => node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument).Any(step =>
                ProhibitsDamageInTrace(step, cast, source, discarded)),
            "if" => node.Field(
                    TraceTest(Tree(node.Require("test")), cast, discarded)
                        ? "then"
                        : "else")
                is { } branch && ProhibitsDamageInTrace(
                    Tree(branch), cast, source, discarded),
            "preventDamageFrom" => cast.World.Facts.Kind(source.FaceId)
                    == Kind(Word(node.Require("sourceKind")))
                && Rules.State.Traits.Has(
                    cast.World, source, Word(node.Require("sourceTrait")),
                    cast.World.Facts),
            "preventDamageWhile" => TraceTest(
                Tree(node.Require("condition")), cast, discarded),
            _ => false,
        };

    private static bool TraceTest(
        AbilityNode node, Cast cast, HashSet<int> discarded) => node.Kind switch
        {
            "and" => Nodes(node.Argument).All(test =>
                TraceTest(test, cast, discarded)),
            "or" => Nodes(node.Argument).Any(test =>
                TraceTest(test, cast, discarded)),
            "not" => !TraceTest(Tree(node.Argument), cast, discarded),
            "exists" => Every(node.Argument, cast).Any(card =>
                !discarded.Contains(card.ObjectId)),
            "titleInPlay" => cast.World.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards)
                .Any(card => !discarded.Contains(card.ObjectId)
                    && string.Equals(
                        cast.World.Facts.Title(card.FaceId),
                        Word(node.Argument), StringComparison.Ordinal)),
            _ => Test(node, cast),
        };

    private static IReadOnlyList<IReadOnlyList<DamageTransfer>> DamageTraces(
        AbilityNode node, Cast cast, RepeatedChange assumed, bool binding)
    {
        if (node.Kind == "forEach")
        {
            long count = ForEachCount(node, cast);
            if (AmountMayChange(node.Require("count")))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has a for-each count that can change "
                    + "between traced iterations");
            }
            if (count == 0)
            {
                return [[]];
            }
            if (ContainsMutableAmount(Tree(node.Require("effect"))))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has a for-each amount that can change "
                    + "between traced iterations");
            }

            var effect = Tree(node.Require("effect"));
            var iteration = DamageTraces(effect, cast, assumed, binding);
            if (!Choices(effect).Any()
                && effect.Kind is "dealDamage" or "removeThreat")
            {
                return
                [
                    .. iteration.Select(trace =>
                        (IReadOnlyList<DamageTransfer>)[
                            .. trace.Select(transfer => transfer with
                            {
                                Amount = SaturatingMultiply(transfer.Amount, count),
                            }),
                        ]),
                ];
            }

            IReadOnlyList<IReadOnlyList<DamageTransfer>> repeated = [[]];
            for (long frame = 0; frame < count; frame++)
            {
                repeated =
                [
                    .. repeated.SelectMany(prefix => iteration.Select(suffix =>
                        (IReadOnlyList<DamageTransfer>)[.. prefix, .. suffix])),
                ];
            }
            return repeated;
        }

        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            var branches = RepeatedTestCanChange(test, assumed)
                    || binding && BindingCanChange(test.Argument)
                ? Branches.Select(node.Field).Where(value => value is not null)
                : node.Field(Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            return
            [
                .. branches.SelectMany(branch => DamageTraces(
                    Tree(branch!), cast, assumed, binding)),
            ];
        }

        if (node.Kind == "choose")
        {
            return
            [
                .. MutationChildren(node).SelectMany(child =>
                    DamageTraces(child, cast, assumed, binding)),
            ];
        }

        var own = DamageTransfers(node, cast);
        var children = MutationChildren(node).ToList();
        IReadOnlyList<IReadOnlyList<DamageTransfer>> traces = [own];
        foreach (var child in children)
        {
            var next = DamageTraces(child, cast, assumed, binding);
            traces =
            [
                .. traces.SelectMany(prefix => next.Select(suffix =>
                    (IReadOnlyList<DamageTransfer>)[.. prefix, .. suffix])),
            ];
        }
        return traces;
    }

    private static IReadOnlyList<DamageTransfer> DamageTransfers(
        AbilityNode node, Cast cast)
    {
        if (node.Kind == "changeForm")
        {
            return [new DamageTransfer(
                0, 0, 0,
                ChangesForm: Seat(node.Require("player"), cast),
                Form: Word(node.Require("to")))];
        }
        if (node.Kind is "dealDamage" or "indirectDamage")
        {
            string field = node.Kind == "dealDamage" ? "cards" : "among";
            return
            [
                .. TraceCards(node.Require(field), cast).Select(target =>
                    new DamageTransfer(
                        -1, target.Card.ObjectId,
                        Amount(node.Require("amount"), cast),
                        DealsDamage: true,
                        ToVillain: target.VillainSelector)),
            ];
        }
        if (node.Kind == "replaceThreatWithDamage"
            && TraceCardNamed(node.Require("card"), cast) is { } replaced)
        {
            return [new DamageTransfer(
                -1, replaced.Card.ObjectId,
                cast.Occurrence.Threat?.Remaining ?? long.MaxValue,
                DealsDamage: true,
                ToVillain: replaced.VillainSelector)];
        }
        if (node.Kind == "heal"
            && TraceCardNamed(node.Require("card"), cast) is { } healed)
        {
            return [new DamageTransfer(
                healed.Card.ObjectId, -1, Amount(node.Require("amount"), cast),
                FromVillain: healed.VillainSelector)];
        }
        if (node.Kind == "moveDamage"
            && TraceCardNamed(node.Require("from"), cast) is { } from
            && TraceCardNamed(node.Require("to"), cast) is { } to)
        {
            return [new DamageTransfer(
                from.Card.ObjectId, to.Card.ObjectId,
                Amount(node.Require("amount"), cast),
                DealsDamage: true,
                FromVillain: from.VillainSelector,
                ToVillain: to.VillainSelector)];
        }
        if (node.Kind == "giveStatus")
        {
            return
            [
                .. TraceCards(node.Require("card"), cast).Select(target =>
                    new DamageTransfer(
                        0, target.Card.ObjectId, 0,
                        GrantsStatus: Word(node.Require("status")),
                        ToVillain: target.VillainSelector)),
            ];
        }
        if (node.Kind == "grantUntil"
            && node.Field("trait") is { } gainedTrait)
        {
            return
            [
                .. TraceCards(node.Require("card"), cast).Select(target =>
                    new DamageTransfer(
                        0, target.Card.ObjectId, 0,
                        GrantsTrait: Word(gainedTrait),
                        ToVillain: target.VillainSelector)),
            ];
        }
        if (node.Kind == "grantUntil"
            && node.Field("keyword") is { } grantedField
            && Word(grantedField) != "health"
            && TraceCards(node.Require("card"), cast) is { Count: > 0 } targets)
        {
            return
            [
                .. targets.Select(target => new DamageTransfer(
                    0, target.Card.ObjectId,
                    node.Field("amount") is { } amount ? Amount(amount, cast) : 1,
                    GrantsField: Word(grantedField),
                    ToVillain: target.VillainSelector)),
            ];
        }
        if (node.Kind == "grantUntil"
            && node.Field("keyword") is { } granted
            && Word(granted) == "health"
            && TraceCardNamed(node.Require("card"), cast) is { } healthier)
        {
            return [new DamageTransfer(
                0, healthier.Card.ObjectId,
                node.Field("amount") is { } amount ? Amount(amount, cast) : 0,
                GrantsHealth: true,
                ToVillain: healthier.VillainSelector)];
        }
        if (node.Kind == "discard")
        {
            AbilityValue cards = node.Field("card") ?? node.Argument;
            return
            [
                .. TraceCards(cards, cast).Select(target =>
                    new DamageTransfer(
                        0, target.Card.ObjectId, 0, Discards: true,
                        ToVillain: target.VillainSelector)),
            ];
        }
        if (node.Kind == "putIntoPlay"
            && TraceCardNamed(node.Require("card"), cast) is { } entering)
        {
            if (cast.Abilities is AbilityRunner runner
                && runner.On(entering.Card).Any(ability =>
                    ability.Trigger.Timing == AbilityType.Constant))
            {
                // The card is intentionally not moved while eligibility is
                // traced, so its constant abilities are not active in the
                // unchanged World. Refuse that continuation before mutation
                // rather than rank later selectors against a plausible lie.
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' puts '{entering.Card.FaceId}' into play "
                    + "before a repeated continuation reads its constant abilities, "
                    + "which is not implemented");
            }
            return [new DamageTransfer(
                0, entering.Card.ObjectId, 0, EntersPlay: true)];
        }
        if (node.Kind is "removeThreat" or "placeThreat")
        {
            bool removes = node.Kind == "removeThreat";
            return
            [
                .. Every(node.Require("scheme"), cast).Select(scheme =>
                    new DamageTransfer(
                        0, scheme.ObjectId,
                        Amount(node.Require("amount"), cast),
                        RemovesThreat: removes,
                        PlacesThreat: !removes)),
            ];
        }
        return [];
    }

    private static List<TraceCard> TraceCards(
        AbilityValue value, Cast cast)
    {
        bool dynamic = SelectorMembershipCanChange(value)
            || PotentialVillainSelector(value, cast);
        var selected = dynamic
            ? TraceCandidateCards(value, cast)
            : Every(value, cast).ToList();
        var current = cast.World.TheCardIn(DeckType.VillainArea);
        var traced = selected.Select(card => new TraceCard(
            card, dynamic ? value : VillainSelector(value, card, cast))).ToList();
        if (current is not null
            && selected.All(card => card.ObjectId != current.ObjectId)
            && dynamic)
        {
            traced.Insert(0, new TraceCard(current, value));
        }
        return traced;
    }

    private static List<Card> TraceCandidateCards(
        AbilityValue value, Cast cast)
    {
        if (value is AbilityValue.Map)
        {
            var node = Tree(value);
            if (node.Kind is "minBy" or "maxBy")
            {
                return TraceCandidateCards(node.Require("of"), cast);
            }
            if (node.Kind == "withTrait")
            {
                return TraceCandidateCards(node.Require("cards"), cast);
            }
            if (node.Kind == "withoutAnotherCopyAttached")
            {
                return TraceCandidateCards(node.Argument, cast);
            }
            if (node.Kind == "enemiesWithTrait"
                || node.Kind == "query" && node.Argument is AbilityValue.Word
                    { Value: "enemies" or "attackableEnemies"
                        or "minionsEngagedWithYou" or "dronesEngagedWithYou"
                        or "enemiesEngagedWithChosenPlayer" })
            {
                return
                [
                    .. cast.World.Areas
                        .SelectMany(area => area.Cards)
                        .Where(card => FacedownDrones.Kind(
                            card, cast.World.Facts) is
                            CardKind.EncounterVillain or CardKind.Minion),
                ];
            }
            if (node.Kind == "query" && node.Argument is AbilityValue.Word
                { Value: "upgradesYouControl" or "supportsYouControl"
                    or "upgradesAndSupportsYouControl" })
            {
                return
                [
                    .. cast.World.Areas
                        .Where(area => area.Type is DeckType.UpgradesArea
                            or DeckType.SupportsArea)
                        .SelectMany(area => area.Cards),
                ];
            }
        }
        return [.. Every(value, cast)];
    }

    private static TraceCard? TraceCardNamed(
        AbilityValue value, Cast cast)
    {
        if (Find(value, cast) is { } found)
        {
            return new TraceCard(found, VillainSelector(value, found, cast));
        }
        var current = cast.World.TheCardIn(DeckType.VillainArea);
        return current is not null && PotentialVillainSelector(value, cast)
            ? new TraceCard(current, value)
            : null;
    }

    private static bool PotentialVillainSelector(
        AbilityValue value, Cast cast)
    {
        if (cast.World.TheCardIn(DeckType.VillainArea) is null
            || value is not AbilityValue.Map)
        {
            return false;
        }
        var node = Tree(value);
        return node.Kind switch
        {
            "query" => node.Argument is AbilityValue.Word
                { Value: "villain" or "enemies" or "attackableEnemies" or "characters" },
            "titled" => cast.World.AreaOf(DeckType.VillainDeck).Cards
                    .Prepend(cast.World.TheCardIn(DeckType.VillainArea)!)
                    .Any(stage => string.Equals(
                        Word(node.Argument), cast.World.Facts.Title(stage.FaceId),
                        StringComparison.Ordinal)),
            "enemiesWithTrait" => true,
            "withTrait" => PotentialVillainSelector(node.Require("cards"), cast),
            "withoutAnotherCopyAttached" => PotentialVillainSelector(node.Argument, cast),
            "minBy" or "maxBy" => PotentialVillainSelector(node.Require("of"), cast),
            _ => false,
        };
    }

    private static bool SelectorMembershipCanChange(AbilityValue value)
    {
        if (value is not AbilityValue.Map)
        {
            return false;
        }
        var node = Tree(value);
        return node.Kind switch
        {
            "withTrait" or "enemiesWithTrait" or "minBy" or "maxBy"
                or "withoutAnotherCopyAttached" => true,
            "query" => node.Argument is AbilityValue.Word
                { Value: "attackableEnemies" or "minionsEngagedWithYou"
                    or "dronesEngagedWithYou"
                    or "enemiesEngagedWithChosenPlayer"
                    or "upgradesYouControl" or "supportsYouControl"
                    or "upgradesAndSupportsYouControl" },
            _ => false,
        };
    }

    private static AbilityValue? VillainSelector(
        AbilityValue value, Card resolved, Cast cast)
    {
        var current = cast.World.TheCardIn(DeckType.VillainArea);
        if (current is null || resolved.ObjectId != current.ObjectId
            || value is not AbilityValue.Map)
        {
            return null;
        }
        return SelectorCanTrackVillain(value, current, cast) ? value : null;
    }

    private static bool SelectorCanTrackVillain(
        AbilityValue value, Card current, Cast cast)
    {
        if (value is not AbilityValue.Map)
        {
            return false;
        }
        var node = Tree(value);
        return node.Kind switch
        {
            "query" => node.Argument is AbilityValue.Word
                { Value: "villain" or "enemies" or "attackableEnemies" or "characters" },
            "titled" => string.Equals(
                Word(node.Argument), cast.World.Facts.Title(current.FaceId),
                StringComparison.Ordinal),
            "enemiesWithTrait" => TraceHasTrait(
                current, Word(node.Argument), cast, []),
            "withTrait" => TraceHasTrait(
                    current, Word(node.Require("trait")), cast, [])
                && SelectorCanTrackVillain(
                    node.Require("cards"), current, cast),
            "withoutAnotherCopyAttached" => SelectorCanTrackVillain(
                node.Argument, current, cast),
            "minBy" or "maxBy" => SelectorCanTrackVillain(
                node.Require("of"), current, cast),
            _ => false,
        };
    }

    private static int? TraceSelectorIncludesCard(
        AbilityValue value, int bound, int currentVillain, Cast cast,
        HashSet<int> discarded, Dictionary<int, HashSet<string>> traits,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement)
    {
        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        int candidateId = bound == boardVillain ? currentVillain : bound;
        if (candidateId < 0 || discarded.Contains(candidateId))
        {
            return null;
        }
        var candidate = cast.World.Cards[candidateId];
        return TraceSelectorMatches(
            value, candidate, currentVillain, cast, discarded, traits, modifiers,
            engagement)
            ? candidateId
            : null;
    }

    private static bool TraceSelectorMatches(
        AbilityValue value, Card candidate, int currentVillain, Cast cast,
        HashSet<int> discarded, Dictionary<int, HashSet<string>> traits,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement)
    {
        if (value is not AbilityValue.Map)
        {
            return false;
        }
        var node = Tree(value);
        bool villain = candidate.ObjectId == currentVillain;
        var kind = FacedownDrones.Kind(candidate, cast.World.Facts);
        return node.Kind switch
        {
            "query" => node.Argument switch
            {
                AbilityValue.Word { Value: "villain" } => villain,
                AbilityValue.Word { Value: "enemies" } => villain
                    || kind == CardKind.Minion,
                AbilityValue.Word { Value: "minions" } => kind == CardKind.Minion,
                AbilityValue.Word { Value: "characters" } => villain
                    || kind is CardKind.Minion or CardKind.Hero
                        or CardKind.AlterEgo or CardKind.Ally,
                AbilityValue.Word { Value: "attackableEnemies" } => villain
                    ? VillainIsAttackableInTrace(
                        cast, candidate, discarded, modifiers, engagement)
                    : kind == CardKind.Minion
                        && CanTakeDamageInTrace(cast, candidate, discarded),
                AbilityValue.Word { Value: "minionsEngagedWithYou" } =>
                    kind == CardKind.Minion
                    && TraceEngagedWith(candidate, cast.Player, engagement),
                AbilityValue.Word { Value: "dronesEngagedWithYou" } =>
                    FacedownDrones.Is(candidate)
                    && TraceEngagedWith(candidate, Resolver(cast), engagement),
                AbilityValue.Word { Value: "enemiesEngagedWithChosenPlayer" } =>
                    kind == CardKind.Minion
                    && cast.Chosen is { Owner: >= 0 } chosen
                    && TraceEngagedWith(candidate, chosen.Owner, engagement),
                AbilityValue.Word { Value: "upgradesYouControl" } =>
                    candidate.Area.Type == DeckType.UpgradesArea
                    && TracePlayAreaPlayer(candidate, engagement) == cast.Player,
                AbilityValue.Word { Value: "supportsYouControl" } =>
                    candidate.Area.Type == DeckType.SupportsArea
                    && TracePlayAreaPlayer(candidate, engagement) == cast.Player,
                AbilityValue.Word { Value: "upgradesAndSupportsYouControl" } =>
                    candidate.Area.Type is DeckType.UpgradesArea
                        or DeckType.SupportsArea
                    && TracePlayAreaPlayer(candidate, engagement) == cast.Player,
                _ => Every(value, cast).Any(card =>
                    card.ObjectId == candidate.ObjectId),
            },
            "titled" => string.Equals(
                Word(node.Argument), cast.World.Facts.Title(candidate.FaceId),
                StringComparison.Ordinal),
            "enemiesWithTrait" => TraceHasTrait(
                candidate, Word(node.Argument), cast, discarded, traits),
            "withTrait" => TraceSelectorMatches(
                    node.Require("cards"), candidate, currentVillain,
                    cast, discarded, traits, modifiers, engagement)
                && TraceHasTrait(
                    candidate, Word(node.Require("trait")), cast, discarded, traits),
            "withoutAnotherCopyAttached" => TraceSelectorMatches(
                    node.Argument, candidate, currentVillain,
                    cast, discarded, traits, modifiers, engagement)
                && !AnotherCopyAttachedInTrace(candidate, cast, discarded),
            "minBy" or "maxBy" => TraceRankedSelectorIncludesCard(
                node, candidate, currentVillain, cast, discarded, traits,
                modifiers, engagement),
            _ => false,
        };
    }

    private static bool TraceEngagedWith(
        Card card, int player, Dictionary<int, int> engagement) =>
        engagement.TryGetValue(card.ObjectId, out int traced)
            ? traced == player
            : card.Area.Type == DeckType.EngagedEnemiesArea
                && card.Area.PlayArea == PlayArea.Of(player);

    private static int TracePlayAreaPlayer(
        Card card, Dictionary<int, int> placement) =>
        placement.TryGetValue(card.ObjectId, out int traced)
            ? traced
            : card.Area.PlayArea.Player;

    private static bool VillainIsAttackableInTrace(
        Cast cast, Card current, HashSet<int> discarded,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement)
    {
        int player = Resolver(cast);
        bool guarded = cast.World
            .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player))
            .Cards.Any(enemy => !discarded.Contains(enemy.ObjectId)
                && !engagement.ContainsKey(enemy.ObjectId)
                && FacedownDrones.Kind(enemy, cast.World.Facts) == CardKind.Minion
                && TraceModified(
                    enemy, "guard", cast, discarded, modifiers) > 0)
            || engagement.Any(pair => pair.Value == player
                && !discarded.Contains(pair.Key)
                && TraceModified(
                    cast.World.Cards[pair.Key], "guard",
                    cast, discarded, modifiers) > 0);
        return !guarded && CanTakeDamageInTrace(cast, current, discarded);
    }

    private static bool TraceHasTrait(
        Card current, string trait, Cast cast, HashSet<int> discarded,
        Dictionary<int, HashSet<string>>? traits = null)
    {
        if (traits?.TryGetValue(current.ObjectId, out var gained) == true
            && gained.Contains(trait))
        {
            return true;
        }
        if (FacedownDrones.InherentTraits(current, cast.World.Facts)
            .Contains(trait, StringComparer.Ordinal))
        {
            return true;
        }

        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        string grantedKind = Rules.State.Traits.Granted + trait;
        if (cast.World.Effects.Active().Any(effect =>
            effect.Affects == current.ObjectId
            && (effect.Source != EffectSource.ConstantAbility
                || effect.Card is not int source || !discarded.Contains(source))
            && string.Equals(effect.Kind, grantedKind, StringComparison.Ordinal)))
        {
            return true;
        }

        var carried = TraceCarriedAttachments(current, cast, discarded);
        var carriedIds = carried.Select(card => card.ObjectId).ToHashSet();
        return cast.World.Effects.Active().Any(effect =>
            effect.Affects == boardVillain
            && effect.Card is int source && carriedIds.Contains(source)
            && string.Equals(effect.Kind, grantedKind, StringComparison.Ordinal));
    }

    private static List<Card> TraceCarriedAttachments(
        Card current, Cast cast, HashSet<int> discarded)
    {
        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        if (boardVillain < 0 || !string.Equals(
                cast.World.Facts.Title(cast.World.Cards[boardVillain].FaceId),
                cast.World.Facts.Title(current.FaceId),
                StringComparison.Ordinal))
        {
            return [];
        }
        return
        [
            .. cast.World.Areas
                .Where(area => area.Host == boardVillain
                    || area.Host == current.ObjectId)
                .SelectMany(area => area.Cards)
                .Where(card => !discarded.Contains(card.ObjectId)),
        ];
    }

    private static long TraceModified(
        Card current, string field, Cast cast, HashSet<int> discarded,
        Dictionary<(int Card, string Field), long>? modifiers = null)
    {
        if (StateFields.FilledFrom.TryGetValue(field, out string? attribute)
            && attribute is "ATK" or "THW" or "DEF" or "REC" or "SCH"
            && !StateFields.HasUsablePrintedPower(
                cast.World.Facts, current.FaceId, attribute))
        {
            // `rr:dash-value.3`: a referenced dash is an unmodifiable zero.
            // Match StateFields.Modified's early return before applying either
            // trace-local or carried modifiers.
            return 0;
        }

        string? printed = field switch
        {
            "attack" => "ATK+",
            "scheme" => "SCH+",
            "thwart" => "THW+",
            _ => null,
        };
        long value = StateFields.Modified(
                cast.World, current, field, cast.World.Facts, cast.World.Players)
            + (modifiers?.GetValueOrDefault((current.ObjectId, field)) ?? 0);
        if (printed is not null)
        {
            value -= cast.World.Areas
                .Where(area => area.Host == current.ObjectId)
                .SelectMany(area => area.Cards)
                .Where(card => discarded.Contains(card.ObjectId))
                .Sum(card => cast.World.Facts.PrintedValue(
                    card.FaceId, printed, cast.World.Players));
        }
        value -= cast.World.Effects.Active()
            .Where(effect => string.Equals(
                    effect.Kind, field, StringComparison.Ordinal)
                && effect.AppliesTo(cast.World, current)
                && effect.Source == EffectSource.ConstantAbility
                && effect.Card is int source && discarded.Contains(source))
            .Sum(effect => effect.Amount);

        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        if (current.ObjectId == boardVillain)
        {
            return value;
        }

        var carried = TraceCarriedAttachments(current, cast, discarded);
        if (printed is not null)
        {
            value += carried.Sum(card => cast.World.Facts.PrintedValue(
                card.FaceId, printed, cast.World.Players));
        }

        var carriedIds = carried.Select(card => card.ObjectId).ToHashSet();
        value += cast.World.Effects.Active()
            .Where(effect => effect.Affects == boardVillain
                && effect.Card is int source && carriedIds.Contains(source)
                && string.Equals(effect.Kind, field, StringComparison.Ordinal))
            .Sum(effect => effect.Amount);
        return value;
    }

    private static int TraceStatusLimit(
        Card card, string status, Cast cast, HashSet<int> discarded,
        Dictionary<(int Card, string Field), long> modifiers)
    {
        if (status is not (Statuses.Stunned or Statuses.Confused))
        {
            return 1;
        }
        if (TraceModified(card, "stalwart", cast, discarded, modifiers) > 0)
        {
            return 0;
        }
        return TraceModified(card, "steady", cast, discarded, modifiers) > 0
            ? 2
            : 1;
    }

    private static void TraceStatusesLeave(
        int cardId, Cast cast,
        Dictionary<(int Card, string Status), int> statusCounts,
        HashSet<(int Card, string Status)> statusChanges)
    {
        var statuses = statusCounts.Keys
            .Where(key => key.Card == cardId)
            .Select(key => key.Status)
            .Concat(cast.World.Areas
                .Where(area => area.Type == DeckType.StatusArea
                    && area.Host == cardId)
                .SelectMany(area => area.Cards)
                .Select(card => card.FaceId))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        foreach (string status in statuses)
        {
            TraceSetStatusCount(
                cast.World.Cards[cardId], status, 0, cast,
                statusCounts, statusChanges);
        }
    }

    private static void TraceSetStatusCount(
        Card card, string status, int count, Cast cast,
        Dictionary<(int Card, string Status), int> statusCounts,
        HashSet<(int Card, string Status)> statusChanges)
    {
        var key = (card.ObjectId, status);
        int live = Statuses.Count(cast.World, card, status);
        if (count == live)
        {
            statusCounts.Remove(key);
        }
        else
        {
            statusCounts[key] = count;
        }
        if ((count > 0) == (live > 0))
        {
            statusChanges.Remove(key);
        }
        else
        {
            statusChanges.Add(key);
        }
    }

    private static bool TraceStatusMakesVulnerable(
        Card card, string status, int count, int limit, Cast cast,
        HashSet<int> discarded,
        Dictionary<(int Card, string Field), long> modifiers) =>
        status is Statuses.Stunned or Statuses.Confused
        && limit > 0
        && count >= limit
        && TraceModified(
            card, "vulnerable", cast, discarded, modifiers) > 0;

    private static bool AnotherCopyAttachedInTrace(
        Card current, Cast cast, HashSet<int> discarded)
    {
        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        string sourceTitle = cast.World.Facts.Title(cast.Source.FaceId);
        bool carried = boardVillain >= 0 && string.Equals(
            cast.World.Facts.Title(cast.World.Cards[boardVillain].FaceId),
            cast.World.Facts.Title(current.FaceId), StringComparison.Ordinal);
        return cast.World.Areas
            .Where(area => area.Host == current.ObjectId
                || carried && area.Host == boardVillain)
            .SelectMany(area => area.Cards)
            .Any(attached => attached.ObjectId != cast.Source.ObjectId
                && !discarded.Contains(attached.ObjectId)
                && string.Equals(
                    cast.World.Facts.Title(attached.FaceId), sourceTitle,
                    StringComparison.Ordinal));
    }

    private static bool TraceRankedSelectorIncludesCard(
        AbilityNode node, Card candidate, int currentVillain, Cast cast,
        HashSet<int> discarded, Dictionary<int, HashSet<string>> traits,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement)
    {
        if (!TraceSelectorMatches(
                node.Require("of"), candidate, currentVillain,
                cast, discarded, traits, modifiers, engagement)
            || TraceModified(candidate, "permanent", cast, discarded) > 0)
        {
            return false;
        }

        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        var candidates = TraceCandidateCards(node.Require("of"), cast)
            .Select(card => card.ObjectId == boardVillain
                ? currentVillain >= 0
                    ? cast.World.Cards[currentVillain]
                    : null
                : card)
            .Where(card => card is not null)
            .Cast<Card>()
            .DistinctBy(card => card.ObjectId)
            .Where(card => !discarded.Contains(card.ObjectId)
                && TraceSelectorMatches(
                    node.Require("of"), card, currentVillain,
                    cast, discarded, traits, modifiers, engagement)
                && TraceModified(card, "permanent", cast, discarded) <= 0)
            .ToList();
        string key = Word(node.Require("by"));
        long Rank(Card card) => key switch
        {
            "cost" => cast.World.Facts.PrintedValue(
                card.FaceId, "Cost", cast.World.Players),
            "attack" => TraceModified(
                card, "attack", cast, discarded, modifiers),
            "printedHealth" => cast.World.Facts.PrintedValue(
                card.FaceId, "HP", cast.World.Players),
            _ => throw new AbilityException(
                $"'{key}' is not a value cards can be ranked by"),
        };
        long extreme = node.Kind == "minBy"
            ? candidates.Min(Rank)
            : candidates.Max(Rank);
        return Rank(candidate) == extreme;
    }

    private static long MutationTotal(
        AbilityNode node, Cast cast, RepeatedChange assumed, bool binding,
        long own,
        Func<AbilityNode, long> childAmount)
    {
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            if (RepeatedTestCanChange(test, assumed)
                || binding && BindingCanChange(test.Argument))
            {
                long possible = Branches.Select(node.Field)
                    .Where(value => value is not null)
                    .Select(value => childAmount(Tree(value!)))
                    .DefaultIfEmpty(0)
                    .Max();
                return SaturatingSum(own, [possible]);
            }

            bool passes = Test(test, cast);
            long active = node.Field(passes ? "then" : "else") is { } branch
                ? childAmount(Tree(branch))
                : 0;
            return SaturatingSum(own, [active]);
        }

        var amounts = MutationChildren(node).Select(childAmount).ToList();

        // The engine chooses one option. Ordered and simultaneous children all
        // resolve, so only those amounts combine.
        long descendants = node.Kind switch
        {
            "choose" => amounts.DefaultIfEmpty(0).Max(),
            "forEach" => SaturatingMultiply(
                amounts.SingleOrDefault(), Amount(node.Require("count"), cast)),
            _ => SaturatingSum(0, amounts),
        };
        return SaturatingSum(own, [descendants]);
    }

    private static long SaturatingSum(long own, IEnumerable<long> rest)
    {
        foreach (long amount in rest)
        {
            own = amount > long.MaxValue - own ? long.MaxValue : own + amount;
        }
        return own;
    }

    private static long SaturatingMultiply(long amount, long multiplier)
    {
        if (amount <= 0 || multiplier <= 0)
        {
            return 0;
        }
        return amount > long.MaxValue / multiplier
            ? long.MaxValue
            : amount * multiplier;
    }

    private static bool CanExhaust(
        long amountPerFrame, int frames, long remaining) =>
        amountPerFrame > 0 && frames > 0
        && amountPerFrame >= (remaining + frames - 1) / frames;

    private static IEnumerable<AbilityNode> MutationChildren(AbilityNode node) =>
        node.Kind is "attack" or "thwart"
            ? [Tree(node.Require("effect"))]
            : ContinuationChildren(node);

    private static IEnumerable<AbilityNode> ContinuationChildren(AbilityNode node) =>
        node.Kind switch
        {
            "choose" => Nodes(node.Require("options")),
            "chooseCard" or "eachPlayer" or "forEach" =>
                [Tree(node.Require("effect"))],
            "eachTime" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("then")),
            ],
            "afterActivation" => [Tree(node.Require("effect"))],
            "payOrEffect" or "payOrExhaust" => [Tree(node.Require("otherwise"))],
            "thwartSchemes" or "thwartDifferentSchemes" or "legalPractice" =>
                [Tree(node.Require("power"))],
            _ => StructuralChildren(node),
        };

    private static bool ContainsFirstActivation(AbilityNode node) =>
        (node.Kind is "enemyAttacks" or "enemySchemes"
            && node.Field("first") is AbilityValue.Word { Value: "true" })
        || StructuralChildren(node).Any(ContainsFirstActivation);

    /// <summary>Whether this player-card effect can remove any threat.</summary>
    private static bool CanRemoveThreat(AbilityNode node, Cast cast)
    {
        var scheme = Find(node.Require("scheme"), cast);
        return scheme is not null
            && scheme.Tokens.GetValueOrDefault("k_threat") > 0
            && Amount(node.Require("amount"), cast) > 0
            && CanRemoveThreatFrom(node, cast, scheme);
    }

    private static bool CanRemoveThreatFrom(AbilityNode node, Cast cast, Card scheme) =>
        cast.Abilities.CanRemoveThreat(
            cast.World, scheme, OverriddenThreatRemovalSource(node, cast))
        && (IgnoresCrisis(node)
            || scheme.Area.Type != DeckType.MainSchemesArea
            || !IsPlayerCard(cast)
            || !MainScheme.Crisis(cast.World, cast.World.Facts));

    private static int OverriddenThreatRemovalSource(AbilityNode node, Cast cast) =>
        node.Field("overridesCannotFrom") is { } source
            ? Find(source, cast)?.ObjectId ?? -1
            : -1;

    private static bool IgnoresCrisis(AbilityNode node) =>
        node.Field("ignoresCrisis") is AbilityValue.Word { Value: "true" };

    /// <summary>Whether at least one named player can draw a card.</summary>
    private static bool CanDraw(AbilityNode node, Cast cast) =>
        Number(node.Require("count")) > 0
        && Seats(node.Require("player"), cast).Any(player =>
            CanDraw(cast.World, player));

    private static bool CanDraw(World world, int player) =>
        world.Seats[player].Deck.Cards.Count > 0;

    /// <summary>Whether a search names at least one searchable game area.</summary>
    private static bool HasSearchableArea(AbilityNode node, Cast cast)
    {
        if (node.Field("in") is not AbilityValue.List areas || areas.Values.Count == 0)
        {
            return false;
        }

        foreach (var value in areas.Values)
        {
            _ = Area(Tree(value).Kind, cast);
        }
        return true;
    }

    /// <summary>
    /// Runs what is left of the ability after the answered choice.
    /// </summary>
    /// <remarks>
    /// The chosen option has already run; this is the rest of the sequence it
    /// was a step of. If the rest holds another choice, it suspends again and
    /// the step it schedules says where to pick up next.
    /// </remarks>
    private List<GameEvent> Continue(Card source, Cast cast, int from)
    {
        if (cast.AbilityOrdinal >= 0 && cast.AbilityPath.Count > 0)
        {
            var root = AbilityAt(
                source, cast.Tier, cast.AbilityOrdinal, cast.AbilityFace).Effect;
            int eachPlayer = cast.AbilityPath.FindIndex(frame =>
                frame.StartsWith("eachPlayer:", StringComparison.Ordinal));
            ResumeAfter(
                root, cast.AbilityPath, cast,
                stopBefore: cast.EachPlayerFrame && !cast.FinalPlayer ? eachPlayer : -1);
            cast.CompleteResolution();
            DiscardEvent(source, cast);
            return cast.Events;
        }

        var effect = On(source)
            .Select(ability => ability.Effect)
            .FirstOrDefault(tree => Choices(tree).Any());

        // Agenda steps created before structural continuation paths existed
        // still resume from their top-level sequence index.
        if (cast.EachPlayerFrame)
        {
            if (cast.FinalPlayer && effect is { Kind: "seq" } && !cast.Suspended)
            {
                Sequence(effect, cast, from);
            }
            DiscardEvent(source, cast);
            cast.CompleteResolution();
            return cast.Events;
        }

        if (effect is { Kind: "seq" } && !cast.Suspended)
        {
            Sequence(effect, cast, from);
        }

        DiscardEvent(source, cast);
        cast.CompleteResolution();

        return cast.Events;
    }

    /// <summary>A fresh resolution of one card's ability, by one player.</summary>
    private Cast Resolving(
        World world, Card source, int player, AbilityType? tier, bool finalStep = false,
        Occurrence? continuation = null) =>
        new(world,
            source,
            continuation ?? new Occurrence(
                0, [Steps.CardRevealed], Subject: source.ObjectId, Player: player),
            player,
            [],
            this)
        {
            Tier = tier,
            FinalStep = finalStep,
        };

    /// <summary>A suspended resolution with its persisted card bindings restored.</summary>
    private Cast Resuming(
        World world, Card source, int player, AbilityType? tier, bool finalStep = false,
        Occurrence? continuation = null)
    {
        var cast = Resolving(world, source, player, tier, finalStep, continuation);
        if (world.Agenda.Current?.Discarded is { } discarded)
        {
            cast.Discarded.AddRange(discarded.Select(id => world.Cards[id]));
        }
        return cast;
    }

    /// <summary>The one choice a card offers, found again from the card.</summary>
    /// <remarks>
    /// A step cannot carry an effect tree, so it carries the card, same-timing
    /// ability ordinal, and structural path used to find the node again.
    /// </remarks>
    private AbilityNode Choice(
        World world, Card source, int player, int stoppedAt, AbilityType? tier)
    {
        // **Which ability, when a card has a choice in more than one.** The
        // step carries the tier that suspended, because the card and the
        // position do not say: Infinite Hunter's "When Revealed" chooses an ally
        // and its "Boost" chooses between two effects, and picking the first
        // ability with a choice in it would resume the wrong one -- silently,
        // and with a legal-looking question about the wrong cards.
        var persisted = ContinuationStep(world, source, stoppedAt, tier);
        var written = AbilitiesOn(source, persisted?.AbilityFace)
            .Where(ability => tier is null || ability.Trigger.Timing == tier)
            .ToList();
        var cast = Resuming(
            world, source, player, tier,
            continuation: persisted?.AbilityOccurrence);
        RestorePersisted(cast, persisted);
        if (persisted is
            { AbilityOrdinal: >= 0, AbilityPath: { } path } step)
        {
            var ability = written.ElementAtOrDefault(step.AbilityOrdinal)
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' has no '{tier}' ability {step.AbilityOrdinal}");
            cast.RestoreAbility(step.AbilityOrdinal, path, step.AbilityFace);
            RestorePathBindings(cast, path);
            var exact = NodeAtPath(ability.Effect, path);
            return ActiveChoices(exact, cast).SingleOrDefault()
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' has no choice at its persisted ability path");
        }

        if (written.Count > 1
            && written.Count(a => ActiveChoices(a.Effect, cast).Any()) > 1)
        {
            // Two choices at one tier on one card. The tier is as fine as the
            // step gets, so this is the next thing to carry rather than
            // something to guess at.
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' has a choice in more than one '{tier}' ability, and a "
                + "suspended ability is found again from its card and its tier");
        }

        var effect = written
            .Select(ability => ability.Effect)
            .FirstOrDefault(tree => ActiveChoices(tree, cast).Any())
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no choice waiting on an answer");

        if (effect.Kind != "seq")
        {
            return ActiveChoices(effect, cast).Single();
        }

        var steps = Nodes(effect.Argument).ToList();
        if (stoppedAt >= 1 && stoppedAt <= steps.Count)
        {
            var nested = ActiveChoices(steps[stoppedAt - 1], cast).ToList();
            if (nested.Count == 1)
            {
                return nested[0];
            }
        }

        throw new RulesNotImplementedException(
            $"'{source.FaceId}' has no single choice at step {stoppedAt - 1} of its sequence");
    }

    /// <summary>Every <c>choose</c> node in one effect tree.</summary>
    private static IEnumerable<AbilityNode> Choices(AbilityNode node)
    {
        if ((node.Kind == "and" && Nodes(node.Argument).Skip(1).Any())
            || IsChoice(node))
        {
            yield return node;
            yield break;
        }

        var children = node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument),
            "if" => Branches
                .Select(node.Field)
                .Where(branch => branch is not null)
                .Select(branch => Tree(branch!)),
            "then" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("then")),
            ],
            "otherwise" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("otherwise")),
            ],
            "eachPlayer" or "forEach" => [Tree(node.Require("effect"))],
            "defense" => [Tree(node.Require("effect"))],
            _ => [],
        };

        foreach (var found in children.SelectMany(Choices))
        {
            yield return found;
        }
    }

    private static bool IsChoice(AbilityNode node) =>
        node.Kind is "choose" or "chooseCard" or "indirectDamage"
            or "resolveSpecials" or "payOrExhaust" or "chooseTopForHand"
            or "chooseDiscardToShuffle" or "thwartDifferentSchemes" or "makeTheCall"
            or "legalPractice" or "payOrEffect";

    /// <summary>Choice nodes on the control-flow path that can execute now.</summary>
    private static IEnumerable<AbilityNode> ActiveChoices(AbilityNode node, Cast cast)
    {
        if (CurrentlyZeroForEach(node, cast))
        {
            yield break;
        }

        if (node.Kind == "and" && Nodes(node.Argument).Skip(1).Any())
        {
            yield return node;
            yield break;
        }

        if (IsChoice(node))
        {
            if (node.Kind != "indirectDamage"
                || Assignable(node.Require("among"), cast).Count > 1)
            {
                yield return node;
            }
            yield break;
        }

        if (node.Kind is "then" or "otherwise")
        {
            var preceding = Tree(node.Require("effect"));
            var precedingChoices = ActiveChoices(preceding, cast).ToList();
            foreach (var found in precedingChoices)
            {
                yield return found;
            }
            if (precedingChoices.Count > 0)
            {
                yield break;
            }

            var required = node.Kind == "then"
                ? ResolutionOutcome.Full
                : ResolutionOutcome.None;
            if (ResolutionOf(preceding, cast) == required)
            {
                foreach (var found in ActiveChoices(
                    Tree(node.Require(node.Kind)), cast))
                {
                    yield return found;
                }
            }
            yield break;
        }

        var children = node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch ? [Tree(branch)] : [],
            "eachPlayer" or "forEach" => [Tree(node.Require("effect"))],
            "defense" => [Tree(node.Require("effect"))],
            _ => [],
        };

        foreach (var found in children.SelectMany(child => ActiveChoices(child, cast)))
        {
            yield return found;
        }
    }

    private static bool SuspendsInsideAnd(
        AbilityNode node, Cast cast, bool stateMayChange = false,
        bool bindingMayChange = false) =>
        node.Kind == "placeThreat"
        || GuardChildren(node, cast, stateMayChange, bindingMayChange, null).Any(child =>
            SuspendsInsideAnd(
                child.Node, cast, child.StateMayChange, child.BindingMayChange));

    [Flags]
    private enum PowerReadiness
    {
        Ready = 1,
        Exhausted = 2,
    }

    private readonly record struct PowerReachability(
        ulong FormsMayChange, int FirstPlayer,
        long FirstPlayerDamage, bool FirstPlayerTough,
        Dictionary<int, long> CardDamage, Dictionary<int, bool> CardTough,
        HashSet<(int Card, string Status)> StatusChanges,
        Dictionary<(int Card, string Status), int> StatusCounts,
        Dictionary<int, PowerReadiness> CardReadiness, HashSet<int> Discarded,
        Dictionary<int, long> SchemeThreat,
        Dictionary<int, long> PlayerCardsAvailable,
        Dictionary<(int Card, string Field), long> Modifiers,
        Dictionary<int, HashSet<string>> Traits,
        Dictionary<int, int> Engagement,
        int CurrentVillain, int VillainStagesDrawn, bool Finished,
        PowerReachability[]? Alternatives = null);

    private static bool SuspendsPowerEffect(
        AbilityNode node, Cast cast, bool stateMayChange = false,
        bool bindingMayChange = false, PowerReachability? reachability = null)
    {
        var state = reachability ?? InitialPowerReachability(cast);
        if (state.Alternatives is { } alternatives)
        {
            return alternatives.Any(alternative => SuspendsPowerEffect(
                node, cast, stateMayChange, bindingMayChange, alternative));
        }
        return (node.Kind == "and" && Nodes(node.Argument).Skip(1).Any())
            || IsChoice(node)
            || node.Kind is "eachPlayer" or "attack" or "thwart" or "thwartSchemes"
                or "placeThreat" or "enemyAttacks" or "enemySchemes"
            || PowerSuspensionChildren(
                node, cast, stateMayChange, bindingMayChange, state).Any(child =>
                SuspendsPowerEffect(
                    child.Node, cast, child.StateMayChange, child.BindingMayChange,
                    child.Reachability));
    }

    private static IEnumerable<(
        AbilityNode Node, bool StateMayChange, bool BindingMayChange,
        PowerReachability Reachability)> PowerSuspensionChildren(
        AbilityNode node, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        if (node.Kind == "seq")
        {
            var children = Nodes(node.Argument).ToList();
            var result = new List<(
                AbilityNode, bool, bool, PowerReachability)>(children.Count);
            var state = reachability;
            for (int index = 0; index < children.Count; index++)
            {
                bool mayChange = stateMayChange || index > 0;
                result.Add((children[index], mayChange, bindingMayChange, state));
                if (index + 1 < children.Count)
                {
                    state = PowerStateAfter(
                        children[index], cast, mayChange, bindingMayChange, state);
                }
            }
            return result;
        }
        if (node.Kind == "and")
        {
            var children = Nodes(node.Argument).ToList();
            return children.Select(child =>
                (child,
                    stateMayChange || children.Count > 1,
                    bindingMayChange,
                    reachability));
        }
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            bool canSwitch = PowerTestCanChange(
                test, cast, stateMayChange, bindingMayChange, reachability);
            var branches = canSwitch
                ? Branches.Select(node.Field).Where(value => value is not null)
                : node.Field(Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            return branches.Select(value =>
                (Tree(value!), stateMayChange, bindingMayChange, reachability));
        }
        return GuardChildren(node, cast, stateMayChange, bindingMayChange, null)
            .Select(child =>
                (child.Node, child.StateMayChange, child.BindingMayChange,
                    reachability));
    }

    private static bool PowerTestCanChange(
        AbilityNode test, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability) => test.Kind switch
        {
            "and" or "or" => Nodes(test.Argument).Any(child =>
                PowerTestCanChange(
                    child, cast, stateMayChange, bindingMayChange, reachability)),
            "not" => PowerTestCanChange(
                Tree(test.Argument), cast, stateMayChange,
                bindingMayChange, reachability),
            "inForm" => bindingMayChange && BindingCanChange(test.Argument)
                || FirstPlayerMayRebind(PowerForms(reachability))
                    && test.Require("player") is AbilityValue.Word
                        { Value: "firstPlayer" }
                || SeatMayChange(
                    PowerForms(reachability), Seat(test.Require("player"), cast)),
            _ => stateMayChange
                || cast.PaymentMayMutate && PaymentCanChange(test)
                || bindingMayChange && BindingCanChange(test.Argument),
        };

    private static PowerReachability InitialPowerReachability(Cast cast)
    {
        var identity = cast.World.Seats[cast.World.FirstPlayer].IdentityCard;
        int villain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        return new PowerReachability(
            0, cast.World.FirstPlayer, identity.Damage,
            Statuses.Has(cast.World, identity, Statuses.Tough),
            new Dictionary<int, long>(), new Dictionary<int, bool>(),
            [], [], new Dictionary<int, PowerReadiness>(), TraceUnavailableMinions(cast),
            new Dictionary<int, long>(), new Dictionary<int, long>(), [], [], [],
            villain, 0, false, null);
    }

    private static Card? PowerFind(
        AbilityValue value, Cast cast, PowerReachability reachability)
    {
        if (value is AbilityValue.Map
            && (SelectorMembershipCanChange(value)
                || PotentialVillainSelector(value, cast)))
        {
            return PowerEvery(value, cast, reachability).FirstOrDefault();
        }
        var found = Find(value, cast);
        int liveVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        if (found?.ObjectId == liveVillain
            && reachability.CurrentVillain != liveVillain)
        {
            found = reachability.Finished || reachability.CurrentVillain < 0
                ? null
                : cast.World.Cards[reachability.CurrentVillain];
        }
        return found is not null
            && !reachability.Discarded.Contains(found.ObjectId)
                ? found
                : null;
    }

    private static List<Card> PowerEvery(
        AbilityValue value, Cast cast, PowerReachability reachability)
    {
        int liveVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        bool dynamic = value is AbilityValue.Map
            && (SelectorMembershipCanChange(value)
                || PotentialVillainSelector(value, cast));
        if (dynamic)
        {
            var candidates = TraceCandidateCards(value, cast);
            if (liveVillain >= 0
                && PotentialVillainSelector(value, cast)
                && candidates.All(card => card.ObjectId != liveVillain))
            {
                candidates.Insert(0, cast.World.Cards[liveVillain]);
            }
            return
            [
                .. candidates.Select(card => card.ObjectId == liveVillain
                        && reachability.CurrentVillain != liveVillain
                    ? reachability.Finished || reachability.CurrentVillain < 0
                        ? null
                        : cast.World.Cards[reachability.CurrentVillain]
                    : card)
                    .Where(card => card is not null)
                    .Cast<Card>()
                    .DistinctBy(card => card.ObjectId)
                    .Where(card => !reachability.Discarded.Contains(card.ObjectId)
                        && TraceSelectorMatches(
                            value, card, reachability.CurrentVillain,
                            cast, reachability.Discarded,
                            reachability.Traits, reachability.Modifiers,
                            reachability.Engagement)),
            ];
        }
        var cards = new List<Card>();
        foreach (var found in Every(value, cast))
        {
            Card? card = found.ObjectId == liveVillain
                && reachability.CurrentVillain != liveVillain
                    ? reachability.Finished || reachability.CurrentVillain < 0
                        ? null
                        : cast.World.Cards[reachability.CurrentVillain]
                    : found;
            if (card is not null
                && !reachability.Discarded.Contains(card.ObjectId)
                && cards.All(existing => existing.ObjectId != card.ObjectId))
            {
                cards.Add(card);
            }
        }
        return cards;
    }

    private static PowerReachability PowerStateAfter(
        AbilityNode node, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        if (reachability.Alternatives is { } alternatives)
        {
            return MergePowerAlternatives(alternatives.Select(alternative =>
                PowerStateAfter(
                    node, cast, stateMayChange, bindingMayChange, alternative)));
        }
        // SuspendsPowerEffect rejects this simultaneous choice by shape. Do
        // not eagerly replay its children while computing a later sibling's
        // abstract state; each replay would invent another damage instance.
        if (node.Kind == "and" && Nodes(node.Argument).Skip(1).Any())
        {
            return reachability;
        }
        if (node.Kind == "changeForm")
        {
            return ChangeFormState(node, cast, bindingMayChange, reachability);
        }
        if (node.Kind == "forEach")
        {
            if (HasUnboundPowerAmount(node, cast))
            {
                return MergePowerStates(
                    reachability,
                    PowerStateAfter(
                        Tree(node.Require("effect")), cast,
                        stateMayChange, bindingMayChange, reachability),
                    cast);
            }
            long count = ForEachCount(node, cast);
            var effect = Tree(node.Require("effect"));
            if (!Choices(effect).Any() && effect.Kind == "dealDamage")
            {
                return ApplyPowerLeafState(
                    effect, cast, bindingMayChange, reachability, count);
            }
            var repeated = reachability;
            for (long iteration = 0; iteration < count; iteration++)
            {
                var next = PowerStateAfter(
                    effect, cast,
                    stateMayChange || iteration > 0, bindingMayChange, repeated);
                if (SamePowerState(next, repeated))
                {
                    break;
                }
                repeated = next;
            }
            return repeated;
        }
        if (node.Kind is "then" or "otherwise")
        {
            return PowerDependentStateAfter(
                node, cast, stateMayChange, bindingMayChange, reachability);
        }

        var advanced = ApplyPowerLeafState(node, cast, bindingMayChange, reachability);
        var children = PowerSuspensionChildren(
            node, cast, stateMayChange, bindingMayChange, advanced).ToList();
        if (children.Count == 0)
        {
            return advanced;
        }
        if (node.Kind == "seq"
            || (node.Kind == "and" && children.Count == 1))
        {
            var ordered = advanced;
            foreach (var child in children)
            {
                ordered = PowerStateAfter(
                    child.Node, cast, child.StateMayChange,
                    child.BindingMayChange, child.Reachability);
            }
            return ordered;
        }

        bool includeBaseline = node.Kind != "if"
            || ConditionalCanSkipBranch(
                node, cast, stateMayChange, bindingMayChange, advanced);
        PowerReachability? merged = includeBaseline ? advanced : null;
        foreach (var child in children)
        {
            var branch = PowerStateAfter(
                child.Node, cast, child.StateMayChange,
                child.BindingMayChange, child.Reachability);
            merged = merged is { } prior
                ? MergePowerStates(prior, branch, cast)
                : branch;
        }
        return merged ?? advanced;
    }

    private static PowerReachability PowerDependentStateAfter(
        AbilityNode node, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        var effect = Tree(node.Require("effect"));
        var dependent = Tree(node.Require(node.Kind));
        var required = node.Kind == "then"
            ? ResolutionOutcome.Full
            : ResolutionOutcome.None;
        bool answered = ActiveChoices(effect, cast).Any();
        var outcomes = PowerOutcomeStates(
            effect, cast, stateMayChange, bindingMayChange, reachability);
        PowerReachability? merged = null;
        foreach (var outcome in outcomes)
        {
            var branch = outcome.Outcome == required
                ? PowerStateAfter(
                    dependent, cast,
                    node.Kind == "then" || answered || outcomes.Count > 1,
                    bindingMayChange, outcome.State)
                : outcome.State;
            merged = merged is { } prior
                ? MergePowerStates(prior, branch, cast)
                : branch;
        }
        return merged ?? reachability;
    }

    private readonly record struct PowerOutcomeState(
        ResolutionOutcome Outcome, PowerReachability State);

    private static List<PowerOutcomeState> PowerOutcomeStates(
        AbilityNode node, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        if (reachability.Alternatives is { } alternatives)
        {
            return [.. alternatives.SelectMany(alternative => PowerOutcomeStates(
                node, cast, stateMayChange, bindingMayChange, alternative))];
        }
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            bool canSwitch = PowerTestCanChange(
                test, cast, stateMayChange, bindingMayChange, reachability);
            IEnumerable<AbilityValue?> branches = canSwitch
                ? Branches.Select(node.Field)
                : [node.Field(Test(test, cast) ? "then" : "else")];
            return [.. branches.SelectMany(branch => branch is null
                ? [new PowerOutcomeState(ResolutionOutcome.None, reachability)]
                : PowerOutcomeStates(
                    Tree(branch), cast, stateMayChange,
                    bindingMayChange, reachability))];
        }
        if (node.Kind == "seq")
        {
            var states = new List<PowerOutcomeState>
            {
                new(ResolutionOutcome.None, reachability),
            };
            int index = 0;
            foreach (var child in Nodes(node.Argument))
            {
                int childIndex = index++;
                states = [.. states.SelectMany(prior => PowerOutcomeStates(
                    child, cast, stateMayChange || childIndex > 0,
                    bindingMayChange, prior.State).Select(next => new PowerOutcomeState(
                        childIndex == 0
                            ? next.Outcome
                            : CombinePowerOutcomes(prior.Outcome, next.Outcome),
                        next.State)))];
            }
            return states;
        }

        var after = PowerStateAfter(
            node, cast, stateMayChange, bindingMayChange, reachability);
        var outcomes = PowerOutcomes(
            node, cast, stateMayChange, bindingMayChange, reachability);
        return [.. PowerPaths(after).SelectMany(state => outcomes.Select(outcome =>
            new PowerOutcomeState(outcome, state)))];
    }

    private static ResolutionOutcome CombinePowerOutcomes(
        ResolutionOutcome left, ResolutionOutcome right) =>
        left == right ? left : ResolutionOutcome.Partial;

    private static HashSet<ResolutionOutcome> PowerOutcomes(
        AbilityNode node, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        if (node.Kind == "draw")
        {
            long count = Number(node.Require("count"));
            return [CombinedOutcomes(Seats(node.Require("player"), cast).Select(
                player => ResolutionOfAmount(
                    PowerCardsAvailable(reachability, player, cast), count)))];
        }
        if (node.Kind == "heal")
        {
            var card = PowerFind(node.Require("card"), cast, reachability);
            if (card is not null)
            {
                return [ResolutionOfAmount(
                    PowerDamage(reachability, card),
                    Amount(node.Require("amount"), cast))];
            }
        }
        if (node.Kind == "discard")
        {
            AbilityValue target = node.Field("card") ?? node.Argument;
            var card = PowerFind(target, cast, reachability);
            if (card is not null)
            {
                return [reachability.Discarded.Contains(card.ObjectId)
                    ? ResolutionOutcome.None
                    : ResolutionOutcome.Full];
            }
            var unchanged = Find(target, cast);
            if (unchanged is not null
                && reachability.Discarded.Contains(unchanged.ObjectId)
                && !(bindingMayChange && BindingCanChange(target)))
            {
                return [ResolutionOutcome.None];
            }
        }
        if (node.Kind == "removeThreat")
        {
            long wanted = Amount(node.Require("amount"), cast);
            var schemes = PowerEvery(node.Require("scheme"), cast, reachability);
            var valid = schemes.Where(scheme =>
                PowerThreat(reachability, scheme) > 0
                && cast.Abilities.CanRemoveThreat(
                    cast.World, scheme, OverriddenThreatRemovalSource(node, cast))
                && (IgnoresCrisis(node)
                    || !(scheme.Area.Type == DeckType.MainSchemesArea
                    && IsPlayerCard(cast)
                    && PowerCrisis(reachability, cast))));
            return [CombinedOutcomes(valid.Select(scheme => ResolutionOfAmount(
                PowerThreat(reachability, scheme), wanted)))];
        }
        var currentTargets = node.Kind is "exhaust" or "ready"
            ? PowerEvery(node.Argument, cast, reachability)
            : [];
        bool fixedTarget = !stateMayChange
            || node.Argument is AbilityValue.Word { Value: "this" or "you" }
            || currentTargets.Count > 0
                && currentTargets.All(card => reachability.Discarded.Contains(card.ObjectId));
        if (node.Kind is "exhaust" or "ready"
            && fixedTarget
            && !(bindingMayChange && BindingCanChange(node.Argument)))
        {
            var possibilities = new HashSet<(bool Changed, bool Unchanged)>
            {
                (false, false),
            };
            foreach (var card in currentTargets.Where(card =>
                !reachability.Discarded.Contains(card.ObjectId)))
            {
                var readiness = PowerReady(card, reachability);
                bool canChange = node.Kind == "exhaust"
                    ? readiness.HasFlag(PowerReadiness.Ready)
                    : readiness.HasFlag(PowerReadiness.Exhausted);
                bool canStay = node.Kind == "exhaust"
                    ? readiness.HasFlag(PowerReadiness.Exhausted)
                    : readiness.HasFlag(PowerReadiness.Ready);
                var next = new HashSet<(bool Changed, bool Unchanged)>();
                foreach (var prior in possibilities)
                {
                    if (canChange)
                    {
                        next.Add((true, prior.Unchanged));
                    }
                    if (canStay)
                    {
                        next.Add((prior.Changed, true));
                    }
                }
                possibilities = next;
            }
            return [.. possibilities.Select(possibility => possibility switch
            {
                (false, _) => ResolutionOutcome.None,
                (true, false) => ResolutionOutcome.Full,
                _ => ResolutionOutcome.Partial,
            })];
        }
        if (node.Kind == "changeForm"
            && !(bindingMayChange && BindingCanChange(node.Require("player"))))
        {
            int seat = Seat(node.Require("player"), cast);
            bool destinationIsLive = Forms.In(
                cast.World, cast.World.Seats[seat], cast.World.Facts,
                Word(node.Require("to")));
            bool destinationIsCurrent = SeatMayChange(
                    reachability.FormsMayChange, seat)
                ? !destinationIsLive
                : destinationIsLive;
            return [destinationIsCurrent
                ? ResolutionOutcome.None
                : ResolutionOutcome.Full];
        }

        bool outcomeMayChange = stateMayChange
            || cast.PaymentMayMutate
            || bindingMayChange
            || ActiveChoices(node, cast).Any();
        return outcomeMayChange
            ? [ResolutionOutcome.None, ResolutionOutcome.Partial, ResolutionOutcome.Full]
            : [ResolutionOf(node, cast)];
    }

    private static bool ConditionalCanSkipBranch(
        AbilityNode node, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        var test = Tree(node.Require("test"));
        bool canSwitch = PowerTestCanChange(
            test, cast, stateMayChange, bindingMayChange, reachability);
        if (!canSwitch)
        {
            string active = Test(test, cast) ? "then" : "else";
            return node.Field(active) is null;
        }
        return node.Field("then") is null || node.Field("else") is null;
    }

    private static PowerReachability ChangeFormState(
        AbilityNode node, Cast cast, bool bindingMayChange,
        PowerReachability reachability)
    {
        var player = node.Require("player");
        if (bindingMayChange && BindingCanChange(player))
        {
            return reachability with
            {
                FormsMayChange = reachability.FormsMayChange | AllPlayerSeats(cast),
            };
        }
        int seat = Seat(player, cast);
        ulong bit = PlayerSeat(seat);
        bool destinationIsCurrent = Forms.In(
            cast.World, cast.World.Seats[seat], cast.World.Facts,
            Word(node.Require("to")));
        return reachability with
        {
            FormsMayChange = destinationIsCurrent
                ? reachability.FormsMayChange & ~bit
                : reachability.FormsMayChange | bit,
        };
    }

    private const ulong FirstPlayerRebinding = 1UL << 63;

    private static bool FirstPlayerMayRebind(ulong state) =>
        (state & FirstPlayerRebinding) != 0;

    private static PowerReachability ApplyPowerLeafState(
        AbilityNode node, Cast cast, bool bindingMayChange,
        PowerReachability reachability, long multiplier = 1)
    {
        if (reachability.Alternatives is { } alternatives)
        {
            return MergePowerAlternatives(alternatives.Select(alternative =>
                ApplyPowerLeafState(
                    node, cast, bindingMayChange, alternative, multiplier)));
        }
        if (node.Field("amount") is { } authoredAmount
            && (reachability.CardDamage.Count > 0
                || reachability.SchemeThreat.Count > 0)
            && AmountMayChange(authoredAmount))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reads a mutable power amount after damage changed");
        }
        if (node.Kind is "exhaust" or "ready")
        {
            var state = reachability;
            var readiness = node.Kind == "exhaust"
                ? PowerReadiness.Exhausted
                : PowerReadiness.Ready;
            foreach (var card in PowerEvery(node.Argument, cast, reachability))
            {
                state = SetPowerReady(state, card, readiness);
            }
            return state;
        }
        if (node.Kind == "grantUntil")
        {
            var target = PowerFind(node.Require("card"), cast, reachability);
            if (target is null)
            {
                return reachability;
            }
            if (node.Field("trait") is { } gained)
            {
                var traits = reachability.Traits.ToDictionary(
                    pair => pair.Key,
                    pair => new HashSet<string>(pair.Value, StringComparer.Ordinal));
                if (!traits.TryGetValue(target.ObjectId, out var values))
                {
                    values = new HashSet<string>(StringComparer.Ordinal);
                    traits[target.ObjectId] = values;
                }
                values.Add(Word(gained));
                return reachability with { Traits = traits };
            }

            string field = Word(node.Require("keyword"));
            long grantedAmount = node.Field("amount") is { } granted
                ? Amount(granted, cast)
                : 1;
            var modifiers = new Dictionary<(int Card, string Field), long>(
                reachability.Modifiers);
            var key = (target.ObjectId, field);
            long changed = SaturatingAdd(
                modifiers.GetValueOrDefault(key), grantedAmount);
            if (changed == 0)
            {
                modifiers.Remove(key);
            }
            else
            {
                modifiers[key] = changed;
            }
            return reachability with { Modifiers = modifiers };
        }
        if (node.Kind == "putIntoPlay")
        {
            var card = Find(node.Require("card"), cast);
            if (card is null)
            {
                return reachability;
            }
            if (cast.Abilities is AbilityRunner runner
                && runner.On(card).Any(ability =>
                    ability.Trigger.Timing == AbilityType.Constant))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' puts '{card.FaceId}' into play before "
                    + "a labelled-power continuation reads its constant abilities, "
                    + "which is not implemented");
            }
            var discarded = new HashSet<int>(reachability.Discarded);
            if (!discarded.Remove(card.ObjectId))
            {
                return reachability;
            }
            var engagement = new Dictionary<int, int>(reachability.Engagement);
            if (node.Field("where") is { } where
                && Word(where) == "engagedWithYou")
            {
                engagement[card.ObjectId] = Resolver(cast);
            }
            var state = reachability with
            {
                Discarded = discarded,
                Engagement = engagement,
            };
            if (StateFields.Modified(
                    cast.World, card, "toughness",
                    cast.World.Facts, cast.World.Players) > 0)
            {
                var firstIdentity = cast.World.Seats[cast.World.FirstPlayer].IdentityCard;
                state = SetPowerTough(state, card, true, firstIdentity, cast);
            }
            return state;
        }
        if (node.Kind == "draw")
        {
            long count = Number(node.Require("count"));
            var state = reachability;
            foreach (int player in Seats(node.Require("player"), cast))
            {
                long available = PowerCardsAvailable(state, player, cast);
                state = SetPowerCardsAvailable(
                    state, player, Math.Max(0, available - count), cast);
            }
            return state;
        }
        if (node.Kind == "discard")
        {
            var card = PowerFind(
                node.Field("card") ?? node.Argument, cast, reachability);
            if (card is null)
            {
                return reachability;
            }
            var discarded = new HashSet<int>(reachability.Discarded);
            var engagement = new Dictionary<int, int>(reachability.Engagement);
            var statusCounts = new Dictionary<(int Card, string Status), int>(
                reachability.StatusCounts);
            var statusChanges = new HashSet<(int Card, string Status)>(
                reachability.StatusChanges);
            foreach (int leaving in PowerLeavingTree(card, cast))
            {
                discarded.Add(leaving);
                TraceStatusesLeave(
                    leaving, cast, statusCounts, statusChanges);
                engagement.Remove(leaving);
            }
            return reachability with
            {
                Discarded = discarded,
                Engagement = engagement,
                StatusCounts = statusCounts,
                StatusChanges = statusChanges,
            };
        }
        if (node.Kind == "removeThreat")
        {
            long removedAmount = SaturatingMultiply(
                Amount(node.Require("amount"), cast), multiplier);
            var state = reachability;
            foreach (var scheme in PowerEvery(
                node.Require("scheme"), cast, reachability))
            {
                if (!cast.Abilities.CanRemoveThreat(
                        cast.World, scheme, OverriddenThreatRemovalSource(node, cast))
                    || !IgnoresCrisis(node)
                        && scheme.Area.Type == DeckType.MainSchemesArea
                        && IsPlayerCard(cast)
                        && PowerCrisis(state, cast))
                {
                    continue;
                }
                long current = PowerThreat(state, scheme);
                long changed = Math.Max(0, current - removedAmount);
                state = SetPowerThreat(state, scheme, changed);
                if (current <= 0 || changed > 0
                    || scheme.Area.Type != DeckType.SideSchemesArea)
                {
                    continue;
                }
                if (PowerDefeatHasTriggeredWork(state, scheme, cast))
                {
                    throw new RulesNotImplementedException(
                        $"side scheme '{scheme.FaceId}' is defeated before a "
                        + "labelled-power continuation reads a defeat-triggered ability, "
                        + "which is not implemented");
                }
                var discarded = new HashSet<int>(state.Discarded);
                var engagement = new Dictionary<int, int>(state.Engagement);
                foreach (int leaving in PowerLeavingTree(scheme, cast))
                {
                    discarded.Add(leaving);
                    engagement.Remove(leaving);
                }
                state = state with
                {
                    Discarded = discarded,
                    Engagement = engagement,
                };
            }
            return state;
        }
        AbilityValue? targets = node.Kind switch
        {
            "dealDamage" or "dealAttackDamage" => node.Require("cards"),
            "indirectDamage" => node.Require("among"),
            "moveDamage" or "moveAttackDamage" => node.Require("to"),
            "replaceThreatWithDamage" => node.Require("card"),
            "heal" => node.Require("card"),
            "giveStatus" => node.Require("card"),
            _ => null,
        };
        if (targets is null)
        {
            return reachability;
        }
        var first = cast.World.Seats[cast.World.FirstPlayer].IdentityCard;
        List<Card> cards = node.Kind switch
        {
            "dealDamage" or "dealAttackDamage" =>
                [.. PowerEvery(targets, cast, reachability).Where(target =>
                    CanTakeDamageInTrace(cast, target, reachability.Discarded))],
            "moveDamage" or "moveAttackDamage" =>
                PowerFind(targets, cast, reachability) is { } destination
                    && CanTakeDamageInTrace(cast, destination, reachability.Discarded)
                        ? [destination]
                        : [],
            _ => PowerEvery(targets, cast, reachability),
        };
        if (cards.Count == 0)
        {
            return bindingMayChange && BindingCanChange(targets)
                ? reachability with
                {
                    FormsMayChange = reachability.FormsMayChange
                        | FirstPlayerRebinding,
                }
                : reachability;
        }
        if (node.Kind == "heal")
        {
            long healed = Amount(node.Require("amount"), cast);
            var state = reachability;
            foreach (var card in cards)
            {
                state = SetPowerDamage(
                    state, card, Math.Max(0, PowerDamage(state, card) - healed),
                    first, cast);
            }
            return state;
        }
        if (node.Kind == "giveStatus")
        {
            string status = Word(node.Require("status"));
            if (status != Statuses.Tough)
            {
                var changes = new HashSet<(int Card, string Status)>(
                    reachability.StatusChanges);
                var counts = new Dictionary<(int Card, string Status), int>(
                    reachability.StatusCounts);
                var discarded = new HashSet<int>(reachability.Discarded);
                var engagement = new Dictionary<int, int>(reachability.Engagement);
                foreach (var card in cards)
                {
                    var key = (card.ObjectId, status);
                    int live = Statuses.Count(cast.World, card, status);
                    int current = counts.GetValueOrDefault(key, live);
                    int limit = TraceStatusLimit(
                        card, status, cast, discarded, reachability.Modifiers);
                    if (current >= limit)
                    {
                        continue;
                    }
                    int changed = current + 1;
                    TraceSetStatusCount(
                        card, status, changed, cast, counts, changes);
                    if (!TraceStatusMakesVulnerable(
                        card, status, changed, limit, cast,
                        discarded, reachability.Modifiers))
                    {
                        continue;
                    }
                    foreach (int leaving in PowerLeavingTree(card, cast))
                    {
                        discarded.Add(leaving);
                        TraceStatusesLeave(
                            leaving, cast, counts, changes);
                        engagement.Remove(leaving);
                    }
                }
                return reachability with
                {
                    StatusChanges = changes,
                    StatusCounts = counts,
                    Discarded = discarded,
                    Engagement = engagement,
                };
            }
            var state = reachability;
            foreach (var card in cards)
            {
                state = SetPowerTough(state, card, true, first, cast);
            }
            return state;
        }

        if (node.Kind is "moveDamage" or "moveAttackDamage")
        {
            var from = PowerFind(node.Require("from"), cast, reachability);
            if (from is null)
            {
                return reachability;
            }
            long moved = Math.Min(
                PowerDamage(reachability, from),
                Amount(node.Require("amount"), cast));
            var state = SetPowerDamage(
                reachability, from, PowerDamage(reachability, from) - moved,
                first, cast);
            return ApplyPowerDamage(state, cards, moved, first, cast);
        }

        long amount = SaturatingMultiply(node.Kind switch
        {
            "indirectDamage" => Amount(node.Require("amount"), cast),
            "replaceThreatWithDamage" => cast.Occurrence.Threat?.Remaining ?? 0,
            _ => Amount(node.Require("amount"), cast),
        }, multiplier);
        return ApplyPowerDamage(reachability, cards, amount, first, cast);
    }

    private static PowerReachability ApplyPowerDamage(
        PowerReachability reachability, IReadOnlyList<Card> cards,
        long amount, Card first, Cast cast)
    {
        if (amount <= 0)
        {
            return reachability;
        }
        var state = reachability;
        foreach (var card in cards)
        {
            var damage = new Dictionary<int, long>(state.CardDamage);
            var discarded = new HashSet<int>(state.Discarded);
            long landed = AfterForcedDamageReplacements(
                cast, card.ObjectId, amount, damage, discarded,
                state.CurrentVillain);
            state = state with
            {
                CardDamage = damage,
                Discarded = discarded,
            };
            if (landed <= 0)
            {
                continue;
            }
            if (PowerTough(state, card, cast))
            {
                state = SetPowerTough(state, card, false, first, cast);
                continue;
            }
            state = SetPowerDamage(
                state, card, SaturatingAdd(PowerDamage(state, card), landed),
                first, cast);
            state = ResolvePowerCharacterDefeat(state, card, first, cast);
        }
        return state;
    }

    private static PowerReachability ResolvePowerCharacterDefeat(
        PowerReachability state, Card damaged, Card first, Cast cast)
    {
        long health = PowerHealth(state, damaged, cast);
        if (PowerDamage(state, damaged) < health)
        {
            return state;
        }
        if (PowerWouldBeDefeatedHasTriggeredWork(state, damaged, cast))
        {
            throw new RulesNotImplementedException(
                $"character '{damaged.FaceId}' would be defeated before a "
                + "labelled-power continuation reads a step-6 interrupt, "
                + "which is not implemented");
        }
        if (PowerDefeatHasTriggeredWork(state, damaged, cast))
        {
            throw new RulesNotImplementedException(
                $"character '{damaged.FaceId}' is defeated before a "
                + "labelled-power continuation reads a defeat-triggered ability, "
                + "which is not implemented");
        }
        if (damaged.ObjectId == state.CurrentVillain)
        {
            return AdvancePowerVillain(state, damaged, first, cast);
        }
        if (FacedownDrones.Kind(damaged, cast.World.Facts)
            is not (CardKind.Minion or CardKind.Ally))
        {
            if (!cast.World.Seats.Any(seat => seat.IdentityCard == damaged))
            {
                return state;
            }
            int eliminatedPlayer = cast.World.Seats
                .Select((seat, player) => (seat, player))
                .Single(pair => pair.seat.IdentityCard == damaged)
                .player;
            var plan = PlanTracePlayerElimination(
                eliminatedPlayer, cast, state.Discarded, state.Engagement);
            var eliminated = new HashSet<int>(state.Discarded);
            var eliminatedEngagement = new Dictionary<int, int>(state.Engagement);
            var eliminatedStatusCounts = new Dictionary<(int Card, string Status), int>(
                state.StatusCounts);
            var eliminatedStatusChanges = new HashSet<(int Card, string Status)>(
                state.StatusChanges);
            var eliminatedTough = new Dictionary<int, bool>(state.CardTough);
            foreach (int relocated in plan.RelocatedCards)
            {
                eliminatedEngagement[relocated] = plan.NextPlayer!.Value;
            }
            foreach (int eliminatedCard in plan.Leaving)
            {
                eliminated.Add(eliminatedCard);
                eliminatedEngagement.Remove(eliminatedCard);
                eliminatedTough.Remove(eliminatedCard);
                TraceStatusesLeave(
                    eliminatedCard, cast,
                    eliminatedStatusCounts, eliminatedStatusChanges);
            }
            return state with
            {
                Discarded = eliminated,
                Engagement = eliminatedEngagement,
                CardTough = eliminatedTough,
                StatusCounts = eliminatedStatusCounts,
                StatusChanges = eliminatedStatusChanges,
            };
        }

        var leaving = PowerLeavingTree(damaged, cast);
        var discarded = new HashSet<int>(state.Discarded);
        var engagement = new Dictionary<int, int>(state.Engagement);
        var statusCounts = new Dictionary<(int Card, string Status), int>(
            state.StatusCounts);
        var statusChanges = new HashSet<(int Card, string Status)>(
            state.StatusChanges);
        foreach (int cardId in leaving)
        {
            discarded.Add(cardId);
            TraceStatusesLeave(
                cardId, cast, statusCounts, statusChanges);
            engagement.Remove(cardId);
        }
        return state with
        {
            Discarded = discarded,
            Engagement = engagement,
            StatusCounts = statusCounts,
            StatusChanges = statusChanges,
        };
    }

    private static long PowerHealth(
        PowerReachability state, Card character, Cast cast)
        => SaturatingAdd(
            TraceHealth(
                character, state.Discarded, state.SchemeThreat, cast),
            state.Modifiers.GetValueOrDefault((character.ObjectId, "health")));

    private static long TraceHealth(
        Card character, HashSet<int> discarded,
        Dictionary<int, long> schemeThreat, Cast cast)
    {
        long health = Damage.Health(cast.World, cast.World.Facts, character);
        var active = cast.World.Effects.Active()
            .Where(effect => effect.Source == EffectSource.ConstantAbility
                && string.Equals(effect.Kind, "health", StringComparison.Ordinal)
                && effect.Card is not null
                && effect.AppliesTo(cast.World, character))
            .ToList();
        var sources = active.Select(effect => effect.Card!.Value).ToHashSet();
        if (schemeThreat.Count > 0 && cast.Abilities is AbilityRunner authored)
        {
            foreach (var source in cast.World.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards)
                .Where(card => authored.On(card).Any(ability =>
                    ability.Trigger.Timing == AbilityType.Constant)))
            {
                sources.Add(source.ObjectId);
            }
        }

        foreach (int sourceId in sources)
        {
            long live = 0;
            foreach (var effect in active.Where(effect => effect.Card == sourceId))
            {
                live = SaturatingAdd(live, effect.Amount);
            }
            long traced = live;
            if (discarded.Contains(sourceId))
            {
                traced = 0;
            }
            else if (schemeThreat.Count > 0
                && cast.Abilities is AbilityRunner runner)
            {
                var source = cast.World.Cards[sourceId];
                var constantCast = new Cast(
                    cast.World, source, new Occurrence(0, []),
                    ControllerOf(cast.World, source), [], runner);
                traced = 0;
                foreach (var ability in runner.On(source).Where(ability =>
                    ability.Trigger.Timing == AbilityType.Constant))
                {
                    if (!TryTraceConstantHealth(
                        ability.Effect, character, schemeThreat,
                        constantCast, out long amount))
                    {
                        throw new RulesNotImplementedException(
                            $"character '{character.FaceId}' has a conditional health "
                            + "constant whose traced predicate is not implemented");
                    }
                    traced = SaturatingAdd(traced, amount);
                }
            }
            health = SaturatingAdd(
                SaturatingSubtract(health, live), traced);
        }
        return health;
    }

    private static bool TryTraceConstantHealth(
        AbilityNode node, Card character, Dictionary<int, long> schemeThreat,
        Cast cast, out long amount)
    {
        if (node.Kind is "seq" or "and")
        {
            amount = 0;
            foreach (var child in Nodes(node.Argument))
            {
                if (!TryTraceConstantHealth(
                    child, character, schemeThreat, cast, out long childAmount))
                {
                    return false;
                }
                amount = SaturatingAdd(amount, childAmount);
            }
            return true;
        }
        if (node.Kind == "if")
        {
            if (!TryPowerTest(
                Tree(node.Require("test")), schemeThreat, cast, out bool branch))
            {
                amount = 0;
                return false;
            }
            if (node.Field(branch ? "then" : "else") is not { } chosen)
            {
                amount = 0;
                return true;
            }
            return TryTraceConstantHealth(
                Tree(chosen), character, schemeThreat, cast, out amount);
        }
        if (node.Kind == "grant"
            && node.Field("keyword") is { } keyword
            && string.Equals(Word(keyword), "health", StringComparison.Ordinal))
        {
            if (Find(node.Require("card"), cast)?.ObjectId != character.ObjectId)
            {
                amount = 0;
                return true;
            }
            if (node.Field("amount") is { } granted)
            {
                return TryPowerAmount(granted, schemeThreat, cast, out amount);
            }
            amount = 1;
            return true;
        }
        if (node.Kind == "grantEach"
            && node.Field("keyword") is { } eachKeyword
            && string.Equals(Word(eachKeyword), "health", StringComparison.Ordinal)
            && Every(node.Require("cards"), cast).Any(card =>
                card.ObjectId == character.ObjectId))
        {
            if (node.Field("amount") is { } granted)
            {
                return TryPowerAmount(granted, schemeThreat, cast, out amount);
            }
            amount = 1;
            return true;
        }
        amount = 0;
        return true;
    }

    private static bool TryPowerTest(
        AbilityNode test, Dictionary<int, long> schemeThreat,
        Cast cast, out bool result)
    {
        if (test.Kind is "and" or "or")
        {
            var values = new List<bool>();
            foreach (var child in Nodes(test.Argument))
            {
                if (!TryPowerTest(child, schemeThreat, cast, out bool value))
                {
                    result = false;
                    return false;
                }
                values.Add(value);
            }
            result = test.Kind == "and"
                ? values.All(value => value)
                : values.Any(value => value);
            return true;
        }
        if (test.Kind == "not")
        {
            if (!TryPowerTest(
                Tree(test.Argument), schemeThreat, cast, out bool value))
            {
                result = false;
                return false;
            }
            result = !value;
            return true;
        }
        if (test.Kind == "atLeast"
            && TryPowerAmount(
                test.Require("value"), schemeThreat, cast, out long valueAt)
            && TryPowerAmount(
                test.Require("count"), schemeThreat, cast, out long count))
        {
            result = valueAt >= count;
            return true;
        }
        if (!ReadsChangedThreat(test.Argument, schemeThreat, cast))
        {
            result = Test(test, cast);
            return true;
        }
        result = false;
        return false;
    }

    private static bool TryPowerAmount(
        AbilityValue value, Dictionary<int, long> schemeThreat,
        Cast cast, out long amount)
    {
        if (!ReadsChangedThreat(value, schemeThreat, cast))
        {
            amount = Amount(value, cast);
            return true;
        }
        if (value is AbilityValue.Map)
        {
            var node = Tree(value);
            if (node.Kind == "tokensOn"
                && Find(node.Argument, cast) is { } scheme)
            {
                amount = TraceThreat(schemeThreat, scheme);
                return true;
            }
        }
        amount = 0;
        return false;
    }

    private static bool ReadsChangedThreat(
        AbilityValue value, Dictionary<int, long> schemeThreat,
        Cast cast) => value switch
        {
            AbilityValue.Map map => map.Entries.Any(pair =>
                string.Equals(pair.Key, "tokensOn", StringComparison.Ordinal)
                    && Find(pair.Value, cast) is { } scheme
                    && schemeThreat.ContainsKey(scheme.ObjectId)
                || ReadsChangedThreat(pair.Value, schemeThreat, cast)),
            AbilityValue.List list => list.Values.Any(item =>
                ReadsChangedThreat(item, schemeThreat, cast)),
            _ => false,
        };

    private static bool PowerWouldBeDefeatedHasTriggeredWork(
        PowerReachability state, Card defeated, Cast cast) =>
        PowerHasMatchingInterrupt(
            state, defeated, cast, Steps.CardWouldBeDefeated);

    private static bool PowerDefeatHasTriggeredWork(
        PowerReachability state, Card defeated, Cast cast) =>
        PowerHasMatchingInterrupt(state, defeated, cast, Steps.CardDefeated);

    private static bool PowerHasMatchingInterrupt(
        PowerReachability state, Card subject, Cast cast, string condition)
    {
        if (cast.Abilities is not AbilityRunner runner)
        {
            return false;
        }
        if (string.Equals(condition, Steps.CardDefeated, StringComparison.Ordinal)
            && cast.World.Facts.HasWhenDefeated(subject.FaceId)
            && !runner.On(subject).Any(ability => string.Equals(
                ability.Trigger.Event, Steps.CardDefeated,
                StringComparison.Ordinal)))
        {
            // Runtime refuses printed defeat text with no authored behavior.
            // Eligibility must make the same refusal before a labelled cost.
            return true;
        }

        // Damage steps 6 and 7 re-read every card for interrupts that answer
        // the imminent or actual defeat. Use that same board-wide matcher, but
        // omit sources the trace has already discarded.
        var occurrence = new Occurrence(
            0, [condition], Subject: subject.ObjectId, Player: subject.Owner);
        return runner.Waiting(
                cast.World, occurrence, WindowKind.Interrupt)
            .Any(pending => !state.Discarded.Contains(pending.Card));
    }

    private static List<int> PowerLeavingTree(Card host, Cast cast)
    {
        var leaving = new List<int> { host.ObjectId };
        var pending = new Stack<Card>(cast.World.Areas
            .Where(area => area.Host == host.ObjectId)
            .SelectMany(area => area.Cards)
            .Reverse());
        var seen = new HashSet<int> { host.ObjectId };
        while (pending.TryPop(out var hosted))
        {
            if (!seen.Add(hosted.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"attachment {hosted.ObjectId} forms a hosting cycle");
            }
                if (StateFields.Modified(
                        cast.World, hosted, "permanent",
                        cast.World.Facts, cast.World.Players) > 0)
            {
                throw new RulesNotImplementedException(
                    $"permanent attachment {hosted.ObjectId} lost host "
                    + $"{host.ObjectId}, and rr:permanent.5 is not implemented");
            }
            leaving.Add(hosted.ObjectId);
            foreach (var child in cast.World.Areas
                .Where(area => area.Host == hosted.ObjectId)
                .SelectMany(area => area.Cards)
                .Reverse())
            {
                pending.Push(child);
            }
        }
        return leaving;
    }

    private sealed record TracePlayerElimination(
        int? NextPlayer,
        HashSet<int> Leaving,
        HashSet<int> RelocatedCards);

    private static TracePlayerElimination PlanTracePlayerElimination(
        int player, Cast cast, HashSet<int> discarded,
        Dictionary<int, int> engagement)
    {
        int? next = null;
        for (int offset = 1; offset < cast.World.Seats.Count; offset++)
        {
            int candidate = (player + offset) % cast.World.Seats.Count;
            var identity = cast.World.Seats[candidate].IdentityCard;
            if (!cast.World.Seats[candidate].Eliminated
                && !discarded.Contains(identity.ObjectId))
            {
                next = candidate;
                break;
            }
        }

        var retained = new HashSet<int>();
        var relocated = new HashSet<int>();
        foreach (var minion in cast.World.Cards.Where(card =>
            !discarded.Contains(card.ObjectId)
            && FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion))
        {
            int engagedPlayer = engagement.TryGetValue(
                    minion.ObjectId, out int traced)
                ? traced
                : minion.Area.Type == DeckType.EngagedEnemiesArea
                    ? minion.Area.PlayArea.Player
                    : -1;
            if (engagedPlayer == player && next is not null)
            {
                foreach (int relocatedCard in TraceHostedTree(minion, cast))
                {
                    relocated.Add(relocatedCard);
                }
            }
            if (engagedPlayer != player || next is not null)
            {
                foreach (int retainedCard in TraceHostedTree(minion, cast))
                {
                    retained.Add(retainedCard);
                }
            }
        }

        var leaving = cast.World.Cards
            .Where(card => card.Area.PlayArea == PlayArea.Of(player)
                && !retained.Contains(card.ObjectId))
            .Select(card => card.ObjectId)
            .ToHashSet();
        leaving.Add(cast.World.Seats[player].IdentityCard.ObjectId);
        foreach (int cardId in leaving)
        {
            var card = cast.World.Cards[cardId];
            if (DeckTypes.IsInPlay(card.Area.Type)
                && cast.World.Facts.Kind(card.FaceId) == CardKind.Attachment
                && StateFields.Modified(
                    cast.World, card, "permanent",
                    cast.World.Facts, cast.World.Players) > 0)
            {
                throw new RulesNotImplementedException(
                    $"card {cardId} is a permanent attachment on an eliminated "
                    + "player's board, and rr:player-elimination.1 resolves its "
                    + "'attach to' text, which is not modelled");
            }
        }
        return new TracePlayerElimination(next, leaving, relocated);
    }

    private static List<int> TraceHostedTree(Card root, Cast cast)
    {
        var tree = new List<int> { root.ObjectId };
        var pending = new Stack<Card>(cast.World.Areas
            .Where(area => area.Host == root.ObjectId)
            .SelectMany(area => area.Cards)
            .Reverse());
        var seen = new HashSet<int> { root.ObjectId };
        while (pending.TryPop(out var card))
        {
            if (!seen.Add(card.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"attachment {card.ObjectId} forms a hosting cycle");
            }
            tree.Add(card.ObjectId);
            foreach (var child in cast.World.Areas
                .Where(area => area.Host == card.ObjectId)
                .SelectMany(area => area.Cards)
                .Reverse())
            {
                pending.Push(child);
            }
        }
        return tree;
    }

    private static PowerReachability AdvancePowerVillain(
        PowerReachability state, Card damaged, Card first, Cast cast)
    {
        if (damaged.ObjectId != state.CurrentVillain
            || PowerDamage(state, damaged) < PowerHealth(state, damaged, cast))
        {
            return state;
        }

        var deck = cast.World.AreaOf(DeckType.VillainDeck);
        int nextIndex = deck.Cards.Count - 1 - state.VillainStagesDrawn;
        Card? next = nextIndex >= 0 ? deck.Cards[nextIndex] : null;
        bool carriesAttachments = next is not null && string.Equals(
            cast.World.Facts.Title(damaged.FaceId),
            cast.World.Facts.Title(next.FaceId),
            StringComparison.Ordinal);
        var discarded = new HashSet<int>(state.Discarded) { damaged.ObjectId };
        if (!carriesAttachments)
        {
            foreach (int leaving in PowerLeavingTree(damaged, cast).Skip(1))
            {
                discarded.Add(leaving);
            }
        }
        if (next is not null
            && cast.Abilities is AbilityRunner runner
            && runner.On(next).Any(ability =>
                ability.Trigger.Timing == AbilityType.Constant))
        {
            // The stage is intentionally not moved while eligibility is
            // traced, so its constant abilities are absent from the unchanged
            // World. Refuse the labelled continuation before its cost mutates.
            throw new RulesNotImplementedException(
                $"villain stage '{next.FaceId}' enters play before a "
                + "labelled-power continuation reads its constant abilities, "
                + "which is not implemented");
        }
        if (next is not null
            && HasLiveVillainRetargetingConstant(
                discarded, damaged, next, cast,
                threatChanges: state.SchemeThreat,
                damageChanges: state.CardDamage,
                modifierChanges: state.Modifiers,
                traitChanges: state.Traits,
                statusChanges:
                [
                    .. state.StatusChanges,
                    .. state.CardTough.Keys.Select(card => (card, Statuses.Tough)),
                ],
                engagementChanges: state.Engagement,
                formsMayChange: state.FormsMayChange,
                traceFirstPlayer: state.FirstPlayer))
        {
            // A constant selector such as { query: villain } follows the new
            // stage as soon as it enters play. The unchanged World still binds
            // that effect to the defeated stage, so refuse before the labelled
            // cost rather than project the continuation from stale modifiers.
            throw new RulesNotImplementedException(
                $"villain stage '{next.FaceId}' enters play before a "
                + "labelled-power continuation reads retargeting constant "
                + "abilities, which is not implemented");
        }
        var engagement = new Dictionary<int, int>(state.Engagement);
        if (!carriesAttachments)
        {
            foreach (int leaving in discarded.Where(cardId =>
                cardId != damaged.ObjectId
                && !state.Discarded.Contains(cardId)))
            {
                engagement.Remove(leaving);
            }
        }
        if (nextIndex < 0)
        {
            return state with
            {
                Discarded = discarded,
                Engagement = engagement,
                CurrentVillain = -1,
                Finished = true,
            };
        }

        next = deck.Cards[nextIndex];
        bool carriesTough = string.Equals(
                cast.World.Facts.Title(damaged.FaceId),
                cast.World.Facts.Title(next.FaceId),
                StringComparison.Ordinal)
            && PowerTough(state, damaged, cast);
        var advanced = state with
        {
            Discarded = discarded,
            Engagement = engagement,
            CurrentVillain = next.ObjectId,
            VillainStagesDrawn = state.VillainStagesDrawn + 1,
        };
        advanced = SetPowerDamage(advanced, next, 0, first, cast);
        bool printedTough = StateFields.Modified(
            cast.World, next, "toughness",
            cast.World.Facts, cast.World.Players) > 0;
        return SetPowerTough(
            advanced, next, carriesTough || printedTough, first, cast);
    }

    private static bool ConstantCanRetargetVillain(
        AbilityNode node, Card current, Card next, Cast cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer)
    {
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            if (TryTraceConstantTest(
                test, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer,
                out bool tracedTest))
            {
                return node.Field(tracedTest ? "then" : "else") is { } traced
                    && ConstantCanRetargetVillain(
                        Tree(traced), current, next, cast, discarded,
                        threatChanges, damageChanges, modifierChanges,
                        traitChanges, statusChanges, engagementChanges,
                        formsMayChange,
                        traceFirstPlayer);
            }
            if (TestCanChangeOnVillainAdvance(
                test, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer))
            {
                return StructuralChildren(node).Any(child =>
                    ConstantCanRetargetVillain(
                        child, current, next, cast, discarded,
                        threatChanges, damageChanges, modifierChanges,
                        traitChanges, statusChanges, engagementChanges,
                        formsMayChange, traceFirstPlayer));
            }
            return node.Field(Test(test, cast) ? "then" : "else") is { } active
                && ConstantCanRetargetVillain(
                    Tree(active), current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer);
        }
        if (node.Kind == "grant"
            && PotentialVillainSelector(node.Require("card"), cast))
        {
            return true;
        }
        if (node.Kind == "grantEach"
            && PotentialVillainSelector(node.Require("cards"), cast))
        {
            return true;
        }
        return StructuralChildren(node).Any(child =>
            ConstantCanRetargetVillain(
                child, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer));
    }

    private static bool HasLiveVillainRetargetingConstant(
        HashSet<int> discarded, Card current, Card next, Cast cast,
        IReadOnlyDictionary<int, long>? threatChanges = null,
        IReadOnlyDictionary<int, long>? damageChanges = null,
        IReadOnlyDictionary<(int Card, string Field), long>? modifierChanges = null,
        IReadOnlyDictionary<int, HashSet<string>>? traitChanges = null,
        HashSet<(int Card, string Status)>? statusChanges = null,
        IReadOnlyDictionary<int, int>? engagementChanges = null,
        ulong formsMayChange = 0, int traceFirstPlayer = -1) =>
        cast.Abilities is AbilityRunner runner
        && cast.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .ToList()
            .Where(card => !discarded.Contains(card.ObjectId))
            .Any(card =>
            {
                var placement = engagementChanges ?? new Dictionary<int, int>();
                var constantCast = TraceConstantCast(cast, card, runner, placement);
                return runner.On(card).Any(ability =>
                    ability.Trigger.Timing == AbilityType.Constant
                    && ConstantCanRetargetVillain(
                        ability.Effect, current, next, constantCast, discarded,
                        threatChanges ?? new Dictionary<int, long>(),
                        damageChanges ?? new Dictionary<int, long>(),
                        modifierChanges
                            ?? new Dictionary<(int Card, string Field), long>(),
                        traitChanges ?? new Dictionary<int, HashSet<string>>(),
                        statusChanges ?? [],
                        placement,
                        formsMayChange,
                        traceFirstPlayer < 0
                            ? cast.World.FirstPlayer
                            : traceFirstPlayer));
            });

    private static Cast TraceConstantCast(
        Cast cast, Card source, AbilityRunner runner,
        IReadOnlyDictionary<int, int> placement)
    {
        int controller = ControllerOf(cast.World, source);
        if (IsPlayerCard(cast.World.Facts, source)
            && DeckTypes.IsInPlay(source.Area.Type)
            && placement.TryGetValue(source.ObjectId, out int projectedPlayer))
        {
            controller = projectedPlayer;
        }
        return new Cast(
            cast.World, source, new Occurrence(0, []), controller, [], runner)
        {
            ProjectedPlayAreaPlayer = placement.TryGetValue(
                source.ObjectId, out int projected)
                    ? projected
                    : null,
        };
    }

    private static bool TryTraceConstantTest(
        AbilityNode test, Card current, Card next, Cast cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        out bool result)
    {
        if (test.Kind is "and" or "or")
        {
            bool unknown = false;
            foreach (var child in Nodes(test.Argument))
            {
                if (!TryTraceConstantTest(
                    child, current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer,
                    out bool value))
                {
                    unknown = true;
                    continue;
                }
                if (test.Kind == "and" && !value
                    || test.Kind == "or" && value)
                {
                    result = value;
                    return true;
                }
            }
            result = test.Kind == "and";
            return !unknown;
        }
        if (test.Kind == "not")
        {
            bool known = TryTraceConstantTest(
                Tree(test.Argument), current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer,
                out bool inner);
            result = !inner;
            return known;
        }
        if (test.Kind == "inForm")
        {
            int seat = test.Require("player") is AbilityValue.Word
                    { Value: "firstPlayer" }
                ? traceFirstPlayer
                : Seat(test.Require("player"), cast);
            bool live = Forms.In(
                cast.World, cast.World.Seats[seat], cast.World.Facts,
                Word(test.Require("form")));
            result = SeatMayChange(formsMayChange, seat) ? !live : live;
            return true;
        }
        if (test.Kind == "titleInPlay")
        {
            string title = Word(test.Argument);
            result = string.Equals(
                    cast.World.Facts.Title(next.FaceId), title,
                    StringComparison.Ordinal)
                || cast.World.Cards.Any(card => string.Equals(
                    cast.World.Facts.Title(card.FaceId), title,
                    StringComparison.Ordinal)
                && (DeckTypes.IsInPlay(card.Area.Type)
                    ? !discarded.Contains(card.ObjectId)
                    : FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
                        && !discarded.Contains(card.ObjectId)));
            return true;
        }
        if (test.Kind == "exists"
            && TraceVillainExists(test.Argument, next, cast, discarded)
                is { } exists)
        {
            result = exists;
            return true;
        }
        if (test.Kind == "exists"
            && TryTraceCount(
                test.Argument, next, cast, discarded,
                traitChanges, modifierChanges, engagementChanges,
                formsMayChange, out long existing))
        {
            result = existing > 0;
            return true;
        }
        if (test.Kind == "atLeast"
            && TryTraceCountAmount(
                test.Require("value"), next, cast, discarded,
                traitChanges, modifierChanges, engagementChanges,
                formsMayChange, out long tracedValue)
            && TryTraceCountAmount(
                test.Require("count"), next, cast, discarded,
                traitChanges, modifierChanges, engagementChanges,
                formsMayChange, out long tracedCount))
        {
            result = tracedValue >= tracedCount;
            return true;
        }
        if (test.Kind is "isTitle" or "isKind"
            && IsProjectedVillainSelector(test.Require("card")))
        {
            result = test.Kind == "isTitle"
                ? string.Equals(
                    cast.World.Facts.Title(next.FaceId),
                    Word(test.Require("title")),
                    StringComparison.Ordinal)
                : cast.World.Facts.Kind(next.FaceId)
                    == Kind(Word(test.Require("kind")));
            return true;
        }
        if (TryTraceEnteredCardTest(
            test, cast, discarded, traitChanges, statusChanges, out result))
        {
            return true;
        }
        if (test.Kind is "hasStatus" or "hasTrait" or "isTitle" or "isKind"
            && Find(test.Require("card"), cast) is { } absentTarget
            && discarded.Contains(absentTarget.ObjectId))
        {
            result = false;
            return true;
        }
        if (!TestCanChangeOnVillainAdvance(
            test, current, next, cast, discarded,
            threatChanges, damageChanges, modifierChanges,
            traitChanges, statusChanges, engagementChanges,
            formsMayChange, traceFirstPlayer))
        {
            result = Test(test, cast);
            return true;
        }
        result = false;
        return false;
    }

    private static bool IsProjectedVillainSelector(AbilityValue selector) =>
        selector is AbilityValue.Map { Entries.Count: 1 }
        && Tree(selector) is { Kind: "query", Argument: AbilityValue.Word
            { Value: "villain" } };

    private static bool? TraceVillainExists(
        AbilityValue selector, Card next, Cast cast,
        HashSet<int> discarded)
    {
        if (selector is not AbilityValue.Map { Entries.Count: 1 })
        {
            return null;
        }
        var node = Tree(selector);
        if (node.Kind == "query"
            && node.Argument is AbilityValue.Word
                { Value: "villain" or "enemies" or "characters" })
        {
            return true;
        }
        if (node.Kind == "titled")
        {
            string title = Word(node.Argument);
            return string.Equals(
                    cast.World.Facts.Title(next.FaceId), title,
                    StringComparison.Ordinal)
                || cast.World.Areas
                    .Where(area => DeckTypes.IsInPlay(area.Type))
                    .SelectMany(area => area.Cards)
                    .Any(card => !discarded.Contains(card.ObjectId)
                        && string.Equals(
                            cast.World.Facts.Title(card.FaceId), title,
                            StringComparison.Ordinal));
        }
        return null;
    }

    private static Card? TraceEnteredCard(
        AbilityValue selector, HashSet<int> discarded, Cast cast)
    {
        if (selector is not AbilityValue.Map { Entries.Count: 1 }
            || Tree(selector) is not { Kind: "titled" } titled)
        {
            return null;
        }
        string title = Word(titled.Argument);
        return cast.World.Cards.FirstOrDefault(card =>
            !DeckTypes.IsInPlay(card.Area.Type)
            && !discarded.Contains(card.ObjectId)
            && FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
            && string.Equals(
                cast.World.Facts.Title(card.FaceId), title,
                StringComparison.Ordinal));
    }

    private static bool TryTraceEnteredCardTest(
        AbilityNode test, Cast cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        out bool result)
    {
        result = false;
        if (test.Kind is not ("hasStatus" or "hasTrait" or "isTitle" or "isKind")
            || TraceEnteredCard(
                test.Require("card"), discarded, cast) is not { } enteredTarget)
        {
            return false;
        }
        result = test.Kind switch
        {
            "hasStatus" => statusChanges.Contains((
                enteredTarget.ObjectId,
                Word(test.Require("status")))),
            "hasTrait" => Rules.State.Traits.Has(
                    cast.World, enteredTarget,
                    Word(test.Require("trait")), cast.World.Facts)
                || traitChanges.TryGetValue(
                    enteredTarget.ObjectId, out var enteredTraits)
                    && enteredTraits.Contains(Word(test.Require("trait"))),
            "isTitle" => string.Equals(
                cast.World.Facts.Title(enteredTarget.FaceId),
                Word(test.Require("title")), StringComparison.Ordinal),
            _ => cast.World.Facts.Kind(enteredTarget.FaceId)
                == Kind(Word(test.Require("kind"))),
        };
        return true;
    }

    private static bool TraceEnteredCardTestCanChange(
        AbilityNode test, Cast cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges) =>
        TryTraceEnteredCardTest(
            test, cast, discarded, traitChanges, statusChanges,
            out bool traced)
        && traced != Test(test, cast);

    private static bool TestCanChangeOnVillainAdvance(
        AbilityNode test, Card current, Card next, Cast cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        int dependencyDepth = 16) => test.Kind switch
        {
            "and" or "or" => Nodes(test.Argument).Any(child =>
                TestCanChangeOnVillainAdvance(
                    child, current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth)),
            "not" => TestCanChangeOnVillainAdvance(
                Tree(test.Argument), current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer, dependencyDepth),
            "inForm" => test.Require("player") is AbilityValue.Word
                    { Value: "firstPlayer" }
                ? FirstPlayerMayRebind(formsMayChange)
                    || traceFirstPlayer != cast.World.FirstPlayer
                    || SeatMayChange(formsMayChange, traceFirstPlayer)
                : SeatMayChange(
                    formsMayChange, Seat(test.Require("player"), cast)),
            "titleInPlay" => !string.Equals(
                    cast.World.Facts.Title(current.FaceId),
                    cast.World.Facts.Title(next.FaceId),
                    StringComparison.Ordinal)
                && (string.Equals(
                        Word(test.Argument),
                        cast.World.Facts.Title(current.FaceId),
                        StringComparison.Ordinal)
                    || string.Equals(
                        Word(test.Argument),
                        cast.World.Facts.Title(next.FaceId),
                        StringComparison.Ordinal))
                || TraceTitlePresenceMayDiffer(
                    Word(test.Argument), discarded, cast),
            "exists" => TraceCardsInPlayMayDiffer(discarded, cast)
                || PotentialVillainSelector(test.Argument, cast),
            "hasStatus" or "hasTrait" or "isTitle" or "isKind"
                when TraceEnteredCardTestCanChange(
                    test, cast, discarded, traitChanges, statusChanges) => true,
            "hasStatus" => PotentialVillainSelector(
                    test.Require("card"), cast)
                || Find(test.Require("card"), cast) is { } statusTarget
                    && (discarded.Contains(statusTarget.ObjectId)
                        || statusChanges.Contains((
                            statusTarget.ObjectId,
                            Word(test.Require("status"))))),
            "hasTrait" => PotentialVillainSelector(
                    test.Require("card"), cast)
                || Find(test.Require("card"), cast) is { } traitTarget
                    && (discarded.Contains(traitTarget.ObjectId)
                        || traitChanges.TryGetValue(
                            traitTarget.ObjectId, out var gainedTraits)
                            && gainedTraits.Contains(
                                Word(test.Require("trait")))),
            "isTitle" or "isKind" => PotentialVillainSelector(
                    test.Require("card"), cast)
                || Find(test.Require("card"), cast) is { } identityTarget
                    && discarded.Contains(identityTarget.ObjectId),
            "atLeast" => ValueReadsVillain(
                    test.Require("value"), cast)
                || ValueReadsVillain(test.Require("count"), cast)
                || AmountCanDifferInVillainTrace(
                    test.Require("value"), current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth)
                || AmountCanDifferInVillainTrace(
                    test.Require("count"), current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth),
            _ => false,
        };

    private static bool AmountCanDifferInVillainTrace(
        AbilityValue value, Card current, Card next, Cast cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        int dependencyDepth = 16)
    {
        if (value is AbilityValue.Number or AbilityValue.Word)
        {
            return false;
        }
        if (value is AbilityValue.List list)
        {
            return list.Values.Any(item => AmountCanDifferInVillainTrace(
                item, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer, dependencyDepth));
        }
        if (value is not AbilityValue.Map { Entries.Count: 1 })
        {
            return false;
        }

        var amount = Tree(value);
        return amount.Kind switch
        {
            "tokensOn" or "countersOn" =>
                PotentialVillainSelector(amount.Argument, cast)
                || TraceEnteredCard(amount.Argument, discarded, cast) is not null
                || Find(amount.Argument, cast) is { } tokenTarget
                    && (discarded.Contains(tokenTarget.ObjectId)
                        || threatChanges.ContainsKey(tokenTarget.ObjectId)),
            "damageOn" =>
                PotentialVillainSelector(amount.Argument, cast)
                || (Find(amount.Argument, cast)
                        ?? TraceEnteredCard(amount.Argument, discarded, cast))
                    is { } damageTarget
                    && (discarded.Contains(damageTarget.ObjectId)
                        || damageChanges.ContainsKey(damageTarget.ObjectId)),
            "remainingHealth" =>
                PotentialVillainSelector(amount.Argument, cast)
                || TraceEnteredCard(amount.Argument, discarded, cast) is not null
                || Find(amount.Argument, cast) is { } healthTarget
                    && (discarded.Contains(healthTarget.ObjectId)
                        || damageChanges.ContainsKey(healthTarget.ObjectId)
                        || modifierChanges.ContainsKey((
                            healthTarget.ObjectId, "health"))
                        || ConditionalModifierCanDiffer(
                            healthTarget, "health", current, next,
                            cast, discarded, threatChanges, damageChanges,
                            modifierChanges, traitChanges, statusChanges,
                            engagementChanges,
                            formsMayChange, traceFirstPlayer,
                            dependencyDepth)),
            "count" => TraceCountMayDiffer(
                amount.Argument, next, cast, discarded,
                traitChanges, modifierChanges, engagementChanges,
                formsMayChange),
            "modified" => PotentialVillainSelector(
                    amount.Require("card"), cast)
                || TraceEnteredCard(
                    amount.Require("card"), discarded, cast) is not null
                || Find(amount.Require("card"), cast) is { } modifiedTarget
                    && (discarded.Contains(modifiedTarget.ObjectId)
                        || modifierChanges.ContainsKey((
                            modifiedTarget.ObjectId,
                            Word(amount.Require("field"))))
                        || damageChanges.ContainsKey(modifiedTarget.ObjectId)
                        || traitChanges.ContainsKey(modifiedTarget.ObjectId)
                        || statusChanges.Any(change =>
                            change.Card == modifiedTarget.ObjectId)
                        || ConditionalModifierCanDiffer(
                            modifiedTarget,
                            Word(amount.Require("field")), current, next,
                            cast, discarded, threatChanges, damageChanges,
                            modifierChanges, traitChanges, statusChanges,
                            engagementChanges,
                            formsMayChange, traceFirstPlayer,
                            dependencyDepth)),
            "min" or "add" or "mul" => Values(amount.Argument).Any(item =>
                AmountCanDifferInVillainTrace(
                    item, current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth)),
            "if" => TestCanChangeOnVillainAdvance(
                    Tree(amount.Require("test")), current, next, cast,
                    discarded, threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth)
                || AmountCanDifferInVillainTrace(
                    amount.Require("then"), current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth)
                || amount.Field("else") is { } otherwise
                    && AmountCanDifferInVillainTrace(
                        otherwise, current, next, cast, discarded,
                        threatChanges, damageChanges, modifierChanges,
                        traitChanges, statusChanges, engagementChanges,
                        formsMayChange, traceFirstPlayer, dependencyDepth),
            _ => false,
        };
    }

    private static bool ConditionalModifierCanDiffer(
        Card target, string field, Card current, Card next, Cast cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        int dependencyDepth)
    {
        if (dependencyDepth <= 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a cyclic conditional modifier "
                + "before a labelled-power continuation, which is not implemented");
        }
        return cast.Abilities is AbilityRunner runner
            && cast.World.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards)
                .Where(source => !discarded.Contains(source.ObjectId))
                .Any(source =>
                {
                    var constantCast = TraceConstantCast(
                        cast, source, runner, engagementChanges);
                    return runner.On(source).Any(ability =>
                        ability.Trigger.Timing == AbilityType.Constant
                        && ConstantFieldCanDiffer(
                            ability.Effect, source, target, field,
                            current, next, constantCast, discarded,
                            threatChanges, damageChanges, modifierChanges,
                            traitChanges, statusChanges, engagementChanges,
                            formsMayChange, traceFirstPlayer, dependencyDepth));
                });
    }

    private static bool ConstantFieldCanDiffer(
        AbilityNode node, Card source, Card target, string field,
        Card current, Card next, Cast cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        int dependencyDepth)
    {
        if (!ContainsFieldGrant(node, source, target, field, cast))
        {
            return false;
        }
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            if (TryTraceConstantTest(
                test, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer,
                out bool tracedTest))
            {
                bool liveTest = Test(test, cast);
                if (tracedTest != liveTest)
                {
                    return node.Field(liveTest ? "then" : "else") is { } live
                            && ContainsFieldGrant(
                                Tree(live), source, target, field, cast)
                        || node.Field(tracedTest ? "then" : "else") is { } traced
                            && ContainsFieldGrant(
                                Tree(traced), source, target, field, cast);
                }
                return node.Field(tracedTest ? "then" : "else") is { } stable
                    && ConstantFieldCanDiffer(
                        Tree(stable), source, target, field,
                        current, next, cast, discarded,
                        threatChanges, damageChanges, modifierChanges,
                        traitChanges, statusChanges, engagementChanges,
                        formsMayChange, traceFirstPlayer, dependencyDepth);
            }
            if (TestCanChangeOnVillainAdvance(
                test, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer, dependencyDepth - 1))
            {
                return StructuralChildren(node).Any(child =>
                    ContainsFieldGrant(child, source, target, field, cast));
            }
            return node.Field(Test(test, cast) ? "then" : "else") is { } active
                && ConstantFieldCanDiffer(
                    Tree(active), source, target, field,
                    current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth);
        }
        return StructuralChildren(node).Any(child => ConstantFieldCanDiffer(
            child, source, target, field, current, next, cast, discarded,
            threatChanges, damageChanges, modifierChanges,
            traitChanges, statusChanges, engagementChanges,
            formsMayChange, traceFirstPlayer, dependencyDepth));
    }

    private static bool ContainsFieldGrant(
        AbilityNode node, Card source, Card target, string field, Cast cast)
    {
        if (node.Kind == "grant"
            && node.Field("keyword") is { } keyword
            && string.Equals(Word(keyword), field, StringComparison.Ordinal)
            && ConstantSelectorAffects(
                node.Require("card"), source, target, cast))
        {
            return true;
        }
        if (node.Kind == "grantEach"
            && node.Field("keyword") is { } eachKeyword
            && string.Equals(Word(eachKeyword), field, StringComparison.Ordinal)
            && ConstantSelectorAffects(
                node.Require("cards"), source, target, cast))
        {
            return true;
        }
        return StructuralChildren(node).Any(child =>
            ContainsFieldGrant(child, source, target, field, cast));
    }

    private static bool ConstantSelectorAffects(
        AbilityValue selector, Card source, Card target, Cast cast) =>
        selector is AbilityValue.Word { Value: "this" }
            ? source.ObjectId == target.ObjectId
            : Every(selector, cast).Any(card => card.ObjectId == target.ObjectId);

    private static bool TraceCardsInPlayMayDiffer(
        HashSet<int> discarded, Cast cast) => cast.World.Cards.Any(card =>
            DeckTypes.IsInPlay(card.Area.Type)
                ? discarded.Contains(card.ObjectId)
                : FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
                    && !discarded.Contains(card.ObjectId));

    private static bool TraceCountMayDiffer(
        AbilityValue selector, Card next, Cast cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange)
    {
        return !TryTraceCount(
                selector, next, cast, discarded,
                traitChanges, modifierChanges, engagementChanges,
                formsMayChange, out long traced)
            || traced != Every(selector, cast).Count;
    }

    private static bool TryTraceCountAmount(
        AbilityValue value, Card next, Cast cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        out long amount)
    {
        if (value is AbilityValue.Number number)
        {
            amount = number.Value;
            return true;
        }
        if (value is not AbilityValue.Map { Entries.Count: 1 })
        {
            amount = 0;
            return false;
        }
        var node = Tree(value);
        if (node.Kind == "count")
        {
            return TryTraceCount(
                node.Argument, next, cast, discarded,
                traitChanges, modifierChanges, engagementChanges,
                formsMayChange, out amount);
        }
        AbilityValue? target = node.Kind is "countersOn" or "modified"
            ? node.Require("card")
            : node.Kind is "tokensOn" or "damageOn" or "remainingHealth"
                ? node.Argument
                : null;
        if (target is not null
            && !PotentialVillainSelector(target, cast)
            && Find(target, cast) is { } removed
            && discarded.Contains(removed.ObjectId)
            && cast.World.Seats.Any(seat =>
                seat.IdentityCard.ObjectId == removed.ObjectId))
        {
            // Amount() returns zero when its selector no longer finds a card.
            // The trace keeps the physical World unchanged, so make that
            // runtime absence explicit after projected removal.
            amount = 0;
            return true;
        }
        if (node.Kind is "min" or "add" or "mul")
        {
            var values = new List<long>();
            foreach (var item in Values(node.Argument))
            {
                if (!TryTraceCountAmount(
                    item, next, cast, discarded,
                    traitChanges, modifierChanges, engagementChanges,
                    formsMayChange, out long traced))
                {
                    amount = 0;
                    return false;
                }
                values.Add(traced);
            }
            amount = node.Kind switch
            {
                "min" => values.Min(),
                "add" => values.Sum(),
                _ => values.Aggregate(1L, (product, item) => product * item),
            };
            return true;
        }
        amount = 0;
        return false;
    }

    private static bool TryTraceCount(
        AbilityValue selector, Card next, Cast cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        out long count)
    {
        if (selector is AbilityValue.Word { Value: "yourHero" or "yourAlterEgo" } formSelector)
        {
            int seat = Resolver(cast);
            var identity = cast.World.Seats[seat].IdentityCard;
            string form = formSelector.Value == "yourHero" ? Forms.Hero : Forms.AlterEgo;
            bool liveForm = Forms.In(
                cast.World, cast.World.Seats[seat], cast.World.Facts, form);
            bool tracedForm = SeatMayChange(formsMayChange, seat) ? !liveForm : liveForm;
            count = tracedForm && !discarded.Contains(identity.ObjectId) ? 1 : 0;
            return true;
        }
        if (selector is AbilityValue.Map { Entries.Count: 1 }
            && Tree(selector) is { Kind: "query", Argument: AbilityValue.Word
                { Value: "heroes" } })
        {
            count = cast.World.Seats
                .Select((seat, player) => (seat, player))
                .Count(pair => !discarded.Contains(
                        pair.seat.IdentityCard.ObjectId)
                    && (SeatMayChange(formsMayChange, pair.player)
                        != Forms.In(
                            cast.World, pair.seat, cast.World.Facts,
                            Forms.Hero)));
            return true;
        }
        if (CountSelectorFormsMayChange(selector, cast, formsMayChange))
        {
            count = 0;
            return false;
        }
        var traits = traitChanges.ToDictionary(
            pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.Ordinal));
        var modifiers = new Dictionary<(int Card, string Field), long>(
            modifierChanges);
        var engagement = new Dictionary<int, int>(engagementChanges);
        count = 0;
        foreach (var card in cast.World.Cards)
        {
            bool projectedInPlay = card.ObjectId == next.ObjectId
                || DeckTypes.IsInPlay(card.Area.Type)
                    && !discarded.Contains(card.ObjectId)
                || !DeckTypes.IsInPlay(card.Area.Type)
                    && FacedownDrones.Kind(card, cast.World.Facts)
                        == CardKind.Minion
                    && !discarded.Contains(card.ObjectId);
            bool projected = projectedInPlay
                && TraceSelectorMatches(
                    selector, card, next.ObjectId, cast, discarded,
                    traits, modifiers, engagement);
            if (projected)
            {
                count++;
            }
        }
        return true;
    }

    private static bool CountSelectorFormsMayChange(
        AbilityValue selector, Cast cast, ulong formsMayChange)
    {
        if (selector is AbilityValue.Word { Value: "yourHero" or "yourAlterEgo" })
        {
            return SeatMayChange(formsMayChange, Resolver(cast));
        }
        if (selector is not AbilityValue.Map)
        {
            return false;
        }
        var node = Tree(selector);
        return node.Kind == "query"
                && node.Argument is AbilityValue.Word { Value: "heroes" }
                && Enumerable.Range(0, cast.World.Seats.Count)
                    .Any(seat => SeatMayChange(formsMayChange, seat))
            || node.Kind == "withTrait"
                && CountSelectorFormsMayChange(
                    node.Require("cards"), cast, formsMayChange)
            || node.Kind is "minBy" or "maxBy"
                && CountSelectorFormsMayChange(
                    node.Require("of"), cast, formsMayChange)
            || node.Kind == "withoutAnotherCopyAttached"
                && CountSelectorFormsMayChange(
                    node.Argument, cast, formsMayChange);
    }

    private static bool TraceTitlePresenceMayDiffer(
        string title, HashSet<int> discarded, Cast cast) =>
        cast.World.Cards.Any(card => string.Equals(
                cast.World.Facts.Title(card.FaceId), title,
                StringComparison.Ordinal)
            && (DeckTypes.IsInPlay(card.Area.Type)
                ? discarded.Contains(card.ObjectId)
                : FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
                    && !discarded.Contains(card.ObjectId)));

    private static bool ValueReadsVillain(AbilityValue value, Cast cast) =>
        value is AbilityValue.Map { Entries.Count: 1 }
            && PotentialVillainSelector(value, cast)
        || value switch
        {
            AbilityValue.List list => list.Values.Any(item =>
                ValueReadsVillain(item, cast)),
            AbilityValue.Map map => map.Entries.Values.Any(item =>
                ValueReadsVillain(item, cast)),
            _ => false,
        };

    private static long PowerDamage(PowerReachability state, Card card) =>
        state.CardDamage.TryGetValue(card.ObjectId, out long damage)
            ? damage
            : card.Damage;

    private static long PowerThreat(PowerReachability state, Card scheme) =>
        TraceThreat(state.SchemeThreat, scheme);

    private static long TraceThreat(
        Dictionary<int, long> schemeThreat, Card scheme) =>
        schemeThreat.TryGetValue(scheme.ObjectId, out long threat)
            ? threat
            : scheme.Tokens.GetValueOrDefault("k_threat");

    private static bool PowerCrisis(PowerReachability state, Cast cast) =>
        cast.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Where(card => !state.Discarded.Contains(card.ObjectId))
            .Any(card => TraceModified(
                card, "crisis", cast, state.Discarded, state.Modifiers) > 0);

    private static PowerReachability SetPowerThreat(
        PowerReachability state, Card scheme, long threat)
    {
        var values = new Dictionary<int, long>(state.SchemeThreat);
        long live = scheme.Tokens.GetValueOrDefault("k_threat");
        if (threat == live)
        {
            values.Remove(scheme.ObjectId);
        }
        else
        {
            values[scheme.ObjectId] = threat;
        }
        return state with { SchemeThreat = values };
    }

    private static bool PowerTough(
        PowerReachability state, Card card, Cast cast) =>
        state.CardTough.TryGetValue(card.ObjectId, out bool tough)
            ? tough
            : Statuses.Has(cast.World, card, Statuses.Tough);

    private static PowerReadiness PowerReady(
        Card card, PowerReachability state) =>
        state.CardReadiness.TryGetValue(card.ObjectId, out var readiness)
            ? readiness
            : card.Ready
                ? PowerReadiness.Ready
                : PowerReadiness.Exhausted;

    private static PowerReachability SetPowerReady(
        PowerReachability state, Card card, PowerReadiness readiness)
    {
        var cards = new Dictionary<int, PowerReadiness>(state.CardReadiness);
        var live = card.Ready
            ? PowerReadiness.Ready
            : PowerReadiness.Exhausted;
        if (readiness == live)
        {
            cards.Remove(card.ObjectId);
        }
        else
        {
            cards[card.ObjectId] = readiness;
        }
        return state with { CardReadiness = cards };
    }

    private static long PowerCardsAvailable(
        PowerReachability state, int player, Cast cast) =>
        state.PlayerCardsAvailable.TryGetValue(player, out long available)
            ? available
            : cast.World.Seats[player].Deck.Cards.Count
                + cast.World.AreaOf(
                    DeckType.DiscardPile, PlayArea.Of(player)).Cards.Count;

    private static PowerReachability SetPowerCardsAvailable(
        PowerReachability state, int player, long available, Cast cast)
    {
        var cards = new Dictionary<int, long>(state.PlayerCardsAvailable);
        long live = cast.World.Seats[player].Deck.Cards.Count
            + cast.World.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player)).Cards.Count;
        if (available == live)
        {
            cards.Remove(player);
        }
        else
        {
            cards[player] = available;
        }
        return state with { PlayerCardsAvailable = cards };
    }

    private static PowerReachability SetPowerDamage(
        PowerReachability state, Card card, long damage, Card first, Cast cast)
    {
        if (damage == PowerDamage(state, card))
        {
            return state;
        }
        var inventory = new Dictionary<int, long>(state.CardDamage);
        if (damage == card.Damage)
        {
            inventory.Remove(card.ObjectId);
        }
        else
        {
            inventory[card.ObjectId] = damage;
        }
        ulong forms = state.FormsMayChange;
        int firstPlayer = state.FirstPlayer;
        var tracedFirst = cast.World.Seats[firstPlayer].IdentityCard;
        if (card == tracedFirst
            && damage >= PowerHealth(state, tracedFirst, cast))
        {
            forms |= FirstPlayerRebinding;
            var changed = state with { CardDamage = inventory };
            for (int offset = 1; offset < cast.World.Seats.Count; offset++)
            {
                int candidate = (firstPlayer + offset) % cast.World.Seats.Count;
                var identity = cast.World.Seats[candidate].IdentityCard;
                if (PowerDamage(changed, identity) < PowerHealth(changed, identity, cast))
                {
                    firstPlayer = candidate;
                    break;
                }
            }
        }
        return state with
        {
            FormsMayChange = forms,
            FirstPlayer = firstPlayer,
            FirstPlayerDamage = card == first ? damage : state.FirstPlayerDamage,
            CardDamage = inventory,
        };
    }

    private static PowerReachability SetPowerTough(
        PowerReachability state, Card card, bool tough, Card first, Cast cast)
    {
        if (tough == PowerTough(state, card, cast))
        {
            return state;
        }
        var statuses = new Dictionary<int, bool>(state.CardTough);
        bool live = Statuses.Has(cast.World, card, Statuses.Tough);
        if (tough == live)
        {
            statuses.Remove(card.ObjectId);
        }
        else
        {
            statuses[card.ObjectId] = tough;
        }
        return state with
        {
            FirstPlayerTough = card == first ? tough : state.FirstPlayerTough,
            CardTough = statuses,
        };
    }

    private static PowerReachability MergePowerStates(
        PowerReachability left, PowerReachability right, Cast cast) =>
        MergePowerAlternatives([left, right]);

    private static PowerReachability MergePowerAlternatives(
        IEnumerable<PowerReachability> states)
    {
        var alternatives = new List<PowerReachability>();
        foreach (var state in states.SelectMany(PowerPaths))
        {
            if (!alternatives.Any(existing => SameConcretePowerState(existing, state)))
            {
                alternatives.Add(state);
            }
        }
        if (alternatives.Count == 0)
        {
            throw new InvalidOperationException("A power state must have a reachable path.");
        }
        if (alternatives.Count == 1)
        {
            return alternatives[0];
        }
        return alternatives[0] with { Alternatives = [.. alternatives] };
    }

    private static IEnumerable<PowerReachability> PowerPaths(
        PowerReachability state) => state.Alternatives ?? [state];

    private static ulong PowerForms(PowerReachability state) =>
        PowerPaths(state).Aggregate(0UL, (forms, path) => forms | path.FormsMayChange);

    private static bool SamePowerState(
        PowerReachability left, PowerReachability right)
    {
        var leftPaths = PowerPaths(left).ToList();
        var rightPaths = PowerPaths(right).ToList();
        return leftPaths.Count == rightPaths.Count
            && leftPaths.All(path => rightPaths.Any(other =>
                SameConcretePowerState(path, other)));
    }

    private static bool SameConcretePowerState(
        PowerReachability left, PowerReachability right) =>
        left.FormsMayChange == right.FormsMayChange
        && left.FirstPlayer == right.FirstPlayer
        && left.FirstPlayerDamage == right.FirstPlayerDamage
        && left.FirstPlayerTough == right.FirstPlayerTough
        && left.CardDamage.Count == right.CardDamage.Count
        && left.CardDamage.All(pair =>
            right.CardDamage.GetValueOrDefault(pair.Key) == pair.Value)
        && left.CardTough.Count == right.CardTough.Count
        && left.CardTough.All(pair =>
            right.CardTough.TryGetValue(pair.Key, out bool value)
                && value == pair.Value)
        && left.StatusChanges.SetEquals(right.StatusChanges)
        && left.StatusCounts.Count == right.StatusCounts.Count
        && left.StatusCounts.All(pair =>
            right.StatusCounts.GetValueOrDefault(pair.Key, -1) == pair.Value)
        && left.CardReadiness.Count == right.CardReadiness.Count
        && left.CardReadiness.All(pair =>
            right.CardReadiness.TryGetValue(pair.Key, out var value)
                && value == pair.Value)
        && left.Discarded.SetEquals(right.Discarded)
        && left.SchemeThreat.Count == right.SchemeThreat.Count
        && left.SchemeThreat.All(pair =>
            right.SchemeThreat.GetValueOrDefault(pair.Key) == pair.Value)
        && left.PlayerCardsAvailable.Count == right.PlayerCardsAvailable.Count
        && left.PlayerCardsAvailable.All(pair =>
            right.PlayerCardsAvailable.GetValueOrDefault(pair.Key) == pair.Value)
        && left.Modifiers.Count == right.Modifiers.Count
        && left.Modifiers.All(pair =>
            right.Modifiers.GetValueOrDefault(pair.Key) == pair.Value)
        && left.Traits.Count == right.Traits.Count
        && left.Traits.All(pair =>
            right.Traits.TryGetValue(pair.Key, out var traits)
                && pair.Value.SetEquals(traits))
        && left.Engagement.Count == right.Engagement.Count
        && left.Engagement.All(pair =>
            right.Engagement.GetValueOrDefault(pair.Key, -1) == pair.Value)
        && left.CurrentVillain == right.CurrentVillain
        && left.VillainStagesDrawn == right.VillainStagesDrawn
        && left.Finished == right.Finished;

    private static long SaturatingAdd(long left, long right) =>
        right > 0 && left > long.MaxValue - right ? long.MaxValue : left + right;

    private static long SaturatingSubtract(long left, long right)
    {
        if (right > 0 && left < long.MinValue + right)
        {
            return long.MinValue;
        }
        if (right < 0 && left > long.MaxValue + right)
        {
            return long.MaxValue;
        }
        return left - right;
    }

    private static ulong AllPlayerSeats(Cast cast) =>
        cast.World.Seats.Count >= 63
            ? FirstPlayerRebinding - 1
            : (1UL << cast.World.Seats.Count) - 1;

    private static ulong PlayerSeat(int seat) => 1UL << seat;

    private static bool SeatMayChange(ulong seats, int seat) =>
        (seats & PlayerSeat(seat)) != 0;

    private static IEnumerable<AbilityNode> PowerNodes(AbilityNode node, string power)
    {
        if (string.Equals(node.Kind, power.ToLowerInvariant(), StringComparison.Ordinal))
        {
            yield return node;
        }

        foreach (var found in PowerValues(node.Argument, power))
        {
            yield return found;
        }
    }

    private static IEnumerable<AbilityNode> PowerValues(AbilityValue value, string power)
    {
        if (value is AbilityValue.List list)
        {
            foreach (var found in list.Values.SelectMany(item => PowerValues(item, power)))
            {
                yield return found;
            }
            yield break;
        }

        if (value is not AbilityValue.Map map)
        {
            yield break;
        }

        if (map.Entries.Count == 1)
        {
            var node = AbilityNode.Of(value);
            foreach (var found in PowerNodes(node, power))
            {
                yield return found;
            }
            yield break;
        }

        foreach (var found in map.Entries.Values.SelectMany(item => PowerValues(item, power)))
        {
            yield return found;
        }
    }

    private static IEnumerable<AbilityNode> EachPlayers(AbilityNode node)
    {
        if (node.Kind == "eachPlayer")
        {
            yield return node;
            yield break;
        }
        IEnumerable<AbilityNode> children = node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument),
            "if" => Branches.Select(node.Field).Where(value => value is not null)
                .Select(value => Tree(value!)),
            "then" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("then")),
            ],
            "otherwise" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("otherwise")),
            ],
            "forEach" => [Tree(node.Require("effect"))],
            _ => [],
        };
        foreach (var found in children.SelectMany(EachPlayers))
        {
            yield return found;
        }
    }

    /// <summary>Whether one ability answers this occurrence, in this window.</summary>
    private bool Answers(
        World world, CardAbility ability, Card card, Occurrence occurrence, WindowKind window,
        int? initiatingPlayer = null)
    {
        // A constant ability names no condition at all -- `rr:ability.5` -- so
        // it answers no occurrence and appears in no window. What it does is
        // read off the board by `Constant` instead.
        if (ability.Trigger.Event is not { } condition || !occurrence.Is(condition))
        {
            return false;
        }

        // **The rest of the sentence, when the rest of the sentence is about
        // the occurrence.** `rr:triggering-condition.2` gives one window to an
        // occurrence that created several conditions, so a card whose triggering
        // condition is "Unus **attacks and** defeats an ally" is answering a
        // moment that carries both -- and an occurrence carrying only the defeat
        // has not met it. `rr:forced.1` is why this gates the trigger rather
        // than guarding the effect: a forced ability must resolve when its
        // condition is met, so an ability that initiates and does nothing has
        // already been the wrong answer -- it takes a place in `rr:forced.5`'s
        // ordering question that it should never have been offered.
        if (ability.Trigger.Also is { } also && !occurrence.Is(also))
        {
            return false;
        }

        bool belongs = window switch
        {
            WindowKind.Interrupt => AbilityTypes.IsInterrupt(ability.Trigger.Timing),
            WindowKind.Response => AbilityTypes.IsResponse(ability.Trigger.Timing),
            _ => false,
        };

        int? restricted = initiatingPlayer ?? RestrictedPlayer(world, ability, card);
        return belongs
            && Subject(world, ability.Trigger.Subject, card, occurrence, restricted)
            && Role(world, ability.Trigger.Actor, card, occurrence.ActorFacts, restricted)
            && Role(world, ability.Trigger.Target, card, occurrence.TargetFacts, restricted)
            && Player(world, ability.Trigger.Player, card, occurrence, restricted);
    }

    /// <summary>
    /// Whose opportunity an ability in a window is, or <c>-1</c> for every
    /// seat's — <c>rr:ability.8</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The card's controller, unless the trigger names somebody. "Players can only
    /// trigger interrupt / response abilities on cards they control or on
    /// encounter cards", and an encounter card is one the scenario owns, so
    /// <c>-1</c> here means <i>anyone</i> rather than nobody.
    /// </para>
    /// <para>
    /// <b>A card that says "you" may name the occurrence's player rather than
    /// its controller.</b> <c>rr:you-your.7</c> — "for abilities that trigger
    /// 'after [enemy] attacks you,' 'you' refers to the attacked player, even
    /// if that player defended with an ally." Prelate Armor's "after
    /// <i>you</i> make a basic attack against Unus" is no opportunity at all
    /// for a player who did not attack, and it is that player's hand the cost
    /// would otherwise be priced against.
    /// </para>
    /// <para>
    /// Which is why this is written on the trigger rather than inferred from
    /// the card having no owner: "any player may" and "the player it happened
    /// to" are both things an encounter card can say, and only the card knows
    /// which it said.
    /// </para>
    /// </remarks>
    private int Controller(
        World world, CardAbility ability, Card card, Occurrence occurrence) =>
        RestrictedPlayer(world, ability, card) is { } restricted
            ? restricted
            : ability.Trigger.Player is not null
            ? occurrence.Player
            : ability.Trigger.Actor == AbilityRoles.You
                ? occurrence.ActorFacts?.Controller ?? ControllerOf(world, card)
                : ControllerOf(world, card);

    /// <summary>The one player allowed to use this encounter-card ability, if any.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:ability.8.1</c>: only the controller of a player card bearing an
    /// attachment may trigger or pay for that attachment's abilities that use
    /// “you” or “your”. The host supplies that controller; the attachment is
    /// still owned by the scenario.
    /// </para>
    /// <para>
    /// <c>rr:ability.8.2</c>: only the player whose play area holds an
    /// obligation may trigger or pay for it. This is a permission distinct
    /// from control, because an obligation remains an encounter card.
    /// </para>
    /// </remarks>
    private int? RestrictedPlayer(World world, CardAbility ability, Card card)
    {
        // The Golden Rules give explicit card text precedence. Obedience
        // Potion-shaped attachments say “Any player can do this,” so that
        // permission overrides the otherwise card-wide “your identity” binding.
        if (ability.AnyPlayer)
        {
            return null;
        }

        if (world.Facts.Kind(card.FaceId) == CardKind.Obligation
            && card.Area.PlayArea.IsPlayers)
        {
            return card.Area.PlayArea.Player;
        }

        if (world.Facts.Kind(card.FaceId) == CardKind.Attachment
            && card.Area.Host >= 0
            && card.Area.Host < world.Cards.Count
            && UsesYouOrYour(ability, card))
        {
            int controller = ControllerOf(world, world.Cards[card.Area.Host]);
            return controller >= 0 ? controller : null;
        }

        return null;
    }

    /// <summary>Whether the authored ability contains the printed “you/your” binding.</summary>
    private bool UsesYouOrYour(CardAbility ability, Card card) =>
        ability.Trigger.Subject == AbilitySubjects.You
        || ability.Trigger.Actor == AbilityRoles.You
        || ability.Trigger.Target == AbilityRoles.You
        || ability.Trigger.Player == AbilityPlayers.You
        || ContainsYouOrYour(ability.Effect)
        || (ability.Cost is { } cost && ContainsYouOrYour(cost))
        || (ability.When is { } when && ContainsYouOrYour(when))
        || (book.Attaches(card.FaceId) is { } attachment
            && ContainsYouOrYour(attachment));

    private static bool ContainsYouOrYour(AbilityNode node) =>
        IsYouOrYourBinding(node.Kind) || ContainsYouOrYour(node.Argument);

    private static bool ContainsYouOrYour(AbilityValue value) => value switch
    {
        AbilityValue.Word word => IsYouOrYourBinding(word.Value),
        AbilityValue.List list => list.Values.Any(ContainsYouOrYour),
        AbilityValue.Map map => map.Entries.Any(entry =>
            IsYouOrYourBinding(entry.Key) || ContainsYouOrYour(entry.Value)),
        _ => false,
    };

    /// <summary>Whether one DSL identifier is relative to the resolving player.</summary>
    private static bool IsYouOrYourBinding(string value)
    {
        if (string.Equals(value, "you", StringComparison.Ordinal)
            || value.StartsWith("your", StringComparison.Ordinal))
        {
            return true;
        }

        // Compound DSL identifiers use camel case: alliesYouControl,
        // isYourIdentity, identitySpecificInYourHand. A printed title can also
        // contain the word “You”, so only identifier-shaped values count.
        return value.All(character => char.IsLetterOrDigit(character) || character == '.')
            && (value.Contains("You", StringComparison.Ordinal)
                || value.Contains("Your", StringComparison.Ordinal));
    }

    /// <summary>Whether this player is permitted to initiate the ability.</summary>
    private bool MayInitiate(World world, CardAbility ability, Card card, int player)
    {
        // `rr:player-turn.5.a-c` grants permission per ability: a player may
        // use their card, an encounter card, or the particular ability whose
        // text allows them. One AnyPlayer ability must not expose its card's
        // other controller-only actions or resource abilities.
        bool cardPermits = ControllerOf(world, card) == player
            || card.Owner == World.Scenario
            || ability.AnyPlayer;
        return cardPermits
            && (RestrictedPlayer(world, ability, card) is not { } restricted
                || restricted == player);
    }

    private static bool Subject(
        World world, string? subject, Card card, Occurrence occurrence,
        int? restricted = null) => subject switch
    {
        null => true,
        AbilitySubjects.This => occurrence.Subject == card.ObjectId,
        AbilitySubjects.AttachedTo => card.Area.Host >= 0 && occurrence.Subject == card.Area.Host,
        AbilitySubjects.You => occurrence.Player >= 0
            && occurrence.Player == (restricted ?? ControllerOf(world, card)),

        // Nothing to match: the condition alone decides. `Waiting` has already
        // checked that the card is in play and that the occurrence carries the
        // condition, which is the whole of what such a card asks for.
        AbilitySubjects.Game => true,
        _ => throw new AbilityException($"'{subject}' is not a subject anything matches"),
    };

    /// <summary>Whether a captured card fills one named occurrence role.</summary>
    private static bool Role(
        World world, string? match, Card card, OccurrenceCard? role,
        int? restricted = null) => match switch
    {
        null => true,
        _ when role is null => false,
        AbilityRoles.This => role.Card == card.ObjectId,
        AbilityRoles.AttachedTo => card.Area.Host >= 0 && role.Card == card.Area.Host,
        AbilityRoles.You => role.Controller >= 0
            && (restricted is { } player
                ? role.Controller == player
                : card.Owner == World.Scenario || role.Controller == ControllerOf(world, card)),
        AbilityRoles.Villain => role.IsVillain,
        AbilityRoles.Minion => role.IsMinion,
        AbilityRoles.Hero => role.IsHero,
        AbilityRoles.Ally => role.IsAlly,
        AbilityRoles.Friendly => role.IsFriendly,
        AbilityRoles.Enemy => role.IsEnemy,
        _ => throw new AbilityException($"'{match}' is not an occurrence role matcher"),
    };

    /// <summary>Whether the occurrence's player fills the trigger's player role.</summary>
    private static bool Player(
        World world, string? match, Card card, Occurrence occurrence,
        int? restricted = null) => match switch
    {
        null or AbilityPlayers.TriggerPlayer => true,
        AbilityPlayers.You => occurrence.Player >= 0
            && occurrence.Player == (restricted ?? ControllerOf(world, card)),
        _ => throw new AbilityException($"'{match}' is not an occurrence player matcher"),
    };

    // ---- the effect tree ---------------------------------------------------

    private static void Run(CardAbility ability, Cast cast)
    {
        var labels = ability.Labels ?? [];
        if (labels.Count > 0)
        {
            if (!cast.LabelsPreflighted)
            {
                if (!CanInitiateLabels(ability, cast))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' cannot initiate its labeled ability "
                        + "in the current state");
                }
                cast.LabelsPreflighted = true;
            }

            var performer = LabeledAbilities.Begin(
                cast.World, cast.World.Facts, Resolver(cast), cast.Source,
                labels, cast.Events);
            if (performer is null)
            {
                return;
            }

            cast.AbilityActor = performer;
            if (labels.Contains(Attack.DefenseVerb, StringComparer.Ordinal))
            {
                Attack.BeginDefenseAbility(cast.World, Resolver(cast), performer);
            }
        }

        Run(ability.Effect, cast);
    }

    private static void Run(AbilityNode node, Cast cast)
    {
        int eventsBefore = cast.Events.Count;
        switch (node.Kind)
        {
            case "seq":
                Sequence(node, cast, from: 0);
                break;

            case "and":
                // `rr:and` makes the effects simultaneous and independent;
                // `rr:first-player.3` gives their order to the first player.
                var simultaneous = Nodes(node.Argument).ToList();
                if (simultaneous.Count <= 1)
                {
                    foreach (var effect in simultaneous)
                    {
                        RunChild(effect, $"and:{simultaneous.IndexOf(effect)}::", cast);
                    }
                    break;
                }
                SuspendForChoice(node, cast);
                break;

            case "then":
                ResolveDependent(node, cast, ResolutionOutcome.Full, "then");
                break;

            case "otherwise":
                ResolveDependent(node, cast, ResolutionOutcome.None, "otherwise");
                break;

            case "eachPlayer":
                if (HasNestedEachPlayer(node, cast))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' nests one each-player frame inside another, "
                        + "which is not implemented");
                }
                int eachPlayerAbility = AbilityOrdinal(node, cast);
                EachPlayerEffects.Schedule(
                    cast.World, cast.Source, cast.Position + 1, cast.Tier, cast.FinalStep,
                    cast.GainedKeywords.Contains("surge"), eachPlayerAbility,
                    [.. cast.AbilityPath], cast.AbilityFace, cast.Player,
                    ContinuationResults(cast, eachPlayerAbility),
                    cast.Occurrence, [.. cast.Discarded.Select(card => card.ObjectId)],
                    cast.HasContinuation, cast.AbilityActor?.ObjectId ?? -1);
                cast.Suspend();
                break;

            case "generate":
                // `rr:resource-ability` -- a resource ability is *read* while a
                // cost is being paid rather than run like an effect, so nothing
                // happens here. `ResourceAbilities` takes its letters and
                // `UseResource` counts the use; running it would be a second
                // way to generate the same resource.
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' generates a resource, which is read while a "
                    + "cost is paid rather than resolved as an effect");

            case "changeForm":
                ChangeForm(node, cast);
                break;

            case "removeFromGame":
                RemoveFromGame(node, cast);
                break;

            case "soakDamage":
                Soak(node, cast);
                break;

            case "exhaust":
                Exhaust(node, cast);
                break;

            case "ready":
                Ready(node, cast);
                break;

            case "drawToHandSize":
                DrawToHandSize(node, cast);
                break;

            case "drawToPrintedHandSize":
                DrawToPrintedHandSize(node, cast);
                break;

            case "removeCounters":
                RemoveCounters(node, cast);
                break;

            case "placeCounters":
                var counter = Word(node.Require("counter"));
                var counterCard = Find(node.Require("card"), cast)
                    ?? throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' cannot find the card receiving counters");
                long beforeCounters = counterCard.Tokens.GetValueOrDefault("c_" + counter);
                long placedCounters = Amount(node.Require("count"), cast);
                if (placedCounters < 0)
                {
                    throw new AbilityException("'placeCounters' needs a non-negative 'count'");
                }
                if (placedCounters == 0)
                {
                    break;
                }
                counterCard.PlaceTokens("c_" + counter, placedCounters);
                cast.Events.Add(new FieldSet(
                    counterCard.ObjectId, "c_" + counter,
                    beforeCounters, beforeCounters + placedCounters)
                {
                    Trigger = cast.Trigger, Verb = "Place_Counters",
                });
                break;

            case "advanceMainScheme":
                AdvanceMainScheme(node, cast);
                break;

            case "preventDamage":
                PreventDamage(node, cast);
                cast.ResolveEffect();
                break;

            case "cancelWhenRevealed":
                CancelWhenRevealed(cast);
                cast.ResolveEffect();
                break;

            case "dealEncounterCards":
                DealEncounterCards(node, cast);
                break;

            case "dealEncounterCard":
                Rules.Play.Deal.EncounterCard(
                    cast.World,
                    Find(node.Require("card"), cast)
                        ?? throw new RulesNotImplementedException(
                            $"'{cast.Source.FaceId}' cannot find the encounter card to deal"),
                    Seat(node.Require("player"), cast),
                    cast.Trigger,
                    cast.Events);
                break;

            case "revealTop":
                RevealCard(TopOfTheEncounterDeck(cast), cast);
                break;

            case "reveal":
                RevealCard(Find(node.Argument, cast), cast);
                break;

            case "placeAtRandom":
                PlaceAtRandom(node, cast);
                break;

            case "returnToHand":
                ReturnToHand(node, cast);
                break;

            case "addToHand":
            {
                var added = Find(node.Argument, cast)
                    ?? throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' cannot find the card added to hand");
                var oldArea = added.Area;
                var newHand = cast.World.Seats[cast.Player].Hand;
                var addedConstantsEnding = cast.World.Effects.PreflightConstantsEnding(added);
                using var addedDeparture = addedConstantsEnding.Begin();
                if (DeckTypes.IsInPlay(oldArea.Type))
                {
                    Rules.Play.Discard.Attachments(
                        cast.World, added, cast.Trigger, cast.Events);
                }
                if (!Characteristics.IsLost(cast.World, added, "linked")
                    && cast.World.Facts.Attributes(added.FaceId).ContainsKey("Linked"))
                {
                    // rr:linked-card-title.4 changes ownership at the moment
                    // the player takes control. A linked ally added from the
                    // set-aside area reaches their hand before it enters play.
                    added.TransferLinkedOwnership(cast.Player);
                }
                World.MoveToTop(added, newHand);
                cast.Events.Add(new CardsMoved(
                    Places.Reference(oldArea), Places.Reference(newHand),
                    [new Landing(added.ObjectId, newHand.Cards.Count - 1)])
                {
                    Trigger = cast.Trigger, Verb = "Add_To_Hand",
                });
                addedConstantsEnding.Complete(cast.Trigger, cast.Events);
                break;
            }

            case "returnOwnedToHand":
            {
                var returned = Find(node.Argument, cast)
                    ?? throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' cannot find the card returned to hand");
                if (returned.Owner < 0)
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' returns a card with no owning player");
                }
                var returnedFrom = returned.Area;
                var ownersHand = cast.World.Seats[returned.Owner].Hand;
                var returnedConstantsEnding =
                    cast.World.Effects.PreflightConstantsEnding(returned);
                using var returnedDeparture = returnedConstantsEnding.Begin();
                if (DeckTypes.IsInPlay(returnedFrom.Type))
                {
                    Rules.Play.Discard.Attachments(
                        cast.World, returned, cast.Trigger, cast.Events);
                }
                World.MoveToTop(returned, ownersHand);
                cast.Events.Add(new CardsMoved(
                    Places.Reference(returnedFrom), Places.Reference(ownersHand),
                    [new Landing(returned.ObjectId, ownersHand.Cards.Count - 1)])
                {
                    Trigger = cast.Trigger, Verb = "Return",
                });
                returnedConstantsEnding.Complete(cast.Trigger, cast.Events);
                break;
            }

            case "discardAtRandom":
                DiscardAtRandom(node, cast);
                break;

            case "discardHandWithResource":
                string wantedResource = Word(node.Argument);
                foreach (var card in cast.World.Seats[cast.Player].Hand.Cards
                             .Where(card => Resources.GeneratedBy(
                                 card.FaceId, cast.World.Facts).Contains(
                                     wantedResource, StringComparison.Ordinal))
                             .ToList())
                {
                    Rules.Play.Discard.Card(cast.World, card, cast.Trigger, cast.Events);
                    cast.Discarded.Add(card);
                }
                cast.Results["discarded"] = cast.Discarded.Count;
                break;

            case "discardUntil":
                DiscardUntil(node, cast);
                break;

            case "discardTop":
                DiscardTop(node, cast);
                break;

            case "recoverDiscardedByResource":
                RecoverDiscardedByResource(node, cast);
                break;

            case "shuffleInto":
                ShuffleInto(node, cast);
                break;

            case "search":
                Search(node, cast);
                break;

            case "choose":
            case "chooseCard":
                Choose(node, cast);
                break;

            case "resolveSpecials":
                if (Every(node.Require("cards"), cast).Count > 0)
                {
                    SuspendForChoice(node, cast);
                }

                break;

            case "payOrExhaust":
            case "payOrEffect":
                SuspendForChoice(node, cast);
                break;

            case "chooseTopForHand":
                if (TopCards(
                    cast.World.Seats[cast.Player].Deck,
                    (int)Number(node.Require("count"))).Count == 0)
                {
                    break;
                }
                SuspendForChoice(node, cast);
                break;

            case "chooseDiscardToShuffle":
            case "thwartDifferentSchemes":
            case "makeTheCall":
            case "legalPractice":
                SuspendForChoice(node, cast);
                break;

            case "afterActivation":
                if (cast.World.Activation is not { } current)
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' delays an effect and no enemy is activating");
                }

                if (!((AbilityRunner)cast.Abilities).activationEffects.TryGetValue(
                    current.Id, out var waiting))
                {
                    ((AbilityRunner)cast.Abilities).activationEffects[current.Id] = waiting = [];
                }
                waiting.Add(new ActivationEffect(
                    cast.Source.ObjectId, cast.Player, cast.Tier,
                    Tree(node.Require("effect")),
                    cast.Altered?.ObjectId ?? -1,
                    cast.AbilityActor?.ObjectId ?? -1));
                cast.ResolveEffect();
                break;

            case "if":
                var branch = Test(Tree(node.Require("test")), cast) ? "then" : "else";
                if (node.Field(branch) is { } taken)
                {
                    RunChild(Tree(taken), $"if:{branch}", cast);
                }

                break;

            case "forEach":
                ForEach(node, cast);
                break;

            case "eachTime":
                EachTime(node, cast);
                break;

            case "giveStatus":
                GiveStatus(node, cast);
                break;

            case "giveAdditionalBoost":
                Attack.GiveAdditionalBoostCard(
                    cast.World,
                    Find(node.Require("enemy"), cast)
                        ?? throw new AbilityException(
                            $"'{cast.Source.FaceId}' cannot find the enemy receiving an additional boost card"),
                    cast.Trigger,
                    cast.Events);
                break;

            case "alsoAttackEachOtherHero":
                Attack.AlsoResolveAgainstEachOtherHero(cast.World);
                cast.ResolveEffect();
                break;

            case "attachTo":
                AttachTo(node, cast);
                break;

            case "grantUntil":
                GrantUntil(node, cast);
                cast.ResolveEffect();
                break;

            case "grantCharactersControlledBy":
                foreach (string field in Values(node.Require("fields")).Select(Word))
                {
                    cast.World.Effects.GrantToCharactersControlledBy(
                        cast.Source, Seat(node.Require("player"), cast), field,
                        Amount(node.Require("amount"), cast),
                        Word(node.Require("until")));
                }
                cast.ResolveEffect();
                break;

            case "reduceNextCardCost":
                CardPlay.ReduceNextCardCost(
                    cast.World, cast.Source, Seat(node.Require("player"), cast),
                    Amount(node.Require("amount"), cast));
                cast.ResolveEffect();
                break;

            case "delayUntil":
                DelayUntil(node, cast);
                cast.ResolveEffect();
                break;

            case "discard":
                Discard(node, cast);
                break;

            case "gainSurge":
                // `rr:surge`: "the player resolving the card deals themself a
                // facedown encounter card from the top of the encounter deck",
                // and `.1` writes it as "**When Revealed**: deal yourself 1
                // facedown encounter card". A card that *gains* surge does the
                // same thing the keyword would have.
                //
                // `rr:keywords.1` makes every additional non-numeric instance
                // inert. Printed and continuously granted Surge already ran in
                // `Reveal.Keywords`; multiple nodes and a value greater than one
                // are multiple gained instances inside this reveal. All four
                // shapes therefore produce at most one deal between them.
                if (Number(node.Argument) > 0
                    && StateFields.Modified(
                        cast.World, cast.Source, "surge", cast.World.Facts,
                        cast.World.Players) <= 0
                    && cast.GainedKeywords.Add("surge"))
                {
                    RememberGainedSurge(cast.World, cast.Source.ObjectId);
                    Deal.EncounterCard(
                        cast.World, cast.Player, cast.Trigger, cast.Events);
                }

                // `.2` finishes the original card first, which the villain
                // phase's reveal queue does without anything else here.
                break;

            case "heal":
                Heal(node, cast);
                break;

            case "indirectDamage":
                Indirect(node, cast);
                break;

            case "dealDamage":
                DealDamage(node, cast);
                break;

            case "moveDamage":
                MoveDamage(node, cast);
                break;

            case "dealAttackDamage":
                DealAttackDamage(node, cast);
                break;

            case "moveAttackDamage":
                MoveAttackDamage(node, cast);
                break;

            case "attack":
                ((AbilityRunner)cast.Abilities).SchedulePower(node, cast, BasicPowers.AttackVerb);
                break;

            case "defense":
                var defender = cast.AbilityActor ?? LabeledAbilities.Begin(
                    cast.World, cast.World.Facts, Resolver(cast), cast.Source,
                    [Attack.DefenseVerb], cast.Events);
                if (defender is not null)
                {
                    if (cast.AbilityActor is null)
                    {
                        Attack.BeginDefenseAbility(cast.World, Resolver(cast), defender);
                    }
                    RunChild(Tree(node.Require("effect")), "defense:effect", cast);
                }
                break;

            case "thwart":
                ((AbilityRunner)cast.Abilities).SchedulePower(node, cast, BasicPowers.ThwartVerb);
                break;

            case "thwartSchemes":
                var schemes = Every(node.Require("schemes"), cast);
                if (schemes.Count > 0)
                {
                    cast.Choose(schemes[0]);
                    ((AbilityRunner)cast.Abilities).SchedulePower(
                        Tree(node.Require("power")), cast, BasicPowers.ThwartVerb,
                        schemes[0], schemes, -1);
                }
                break;

            case "placeThreat":
                PlaceThreat(node, cast);
                break;

            case "placeAccelerationToken":
                EncounterDeck.PlaceAccelerationToken(cast.World, cast.Trigger, cast.Events);
                break;

            case "preventThreat":
                PreventThreat(node, cast);
                cast.ResolveEffect();
                break;

            case "replaceThreatWithDamage":
                ReplaceThreatWithDamage(node, cast);
                break;

            case "removeThreat":
                RemoveThreat(node, cast);
                break;

            case "enemyAttacks":
                Activate(node, cast, Steps.Attack);
                break;

            case "enemySchemes":
                Activate(node, cast, Steps.Scheme);
                break;

            case "putIntoPlay":
                PutIntoPlay(node, cast);
                break;

            case "createDrones":
                CreateDrones(node, cast);
                break;

            case "shuffle":
                // `rr:search.3` -- "if any portion of a deck is searched, upon
                // completion of that game step, game function, or card ability,
                // shuffle that entire deck." A step of the card rather than
                // part of the search, because "upon completion" is after the
                // player has answered which card they took.
                if (cast.World.Shuffle(Area(Word(node.Argument), cast)))
                {
                    cast.ResolveEffect();
                }
                break;

            case "draw":
                foreach (int player in Seats(node.Require("player"), cast))
                {
                    if (CanDraw(cast.World, player))
                    {
                        Draw.Cards(
                            cast.World, player,
                            (int)Number(node.Require("count")),
                            cast.Trigger, cast.Events);
                    }
                }
                break;

            default:
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' uses the effect node '{node.Kind}', "
                    + "which is not implemented");
        }
        if (cast.Events.Count > eventsBefore && EventMeansEffectApplied(node.Kind))
        {
            cast.ResolveEffect();
        }
    }

    private static bool EventMeansEffectApplied(string kind) => kind is not (
        "seq" or "and" or "then" or "otherwise" or "eachPlayer" or "if"
        or "forEach" or "eachTime" or "choose" or "chooseCard"
        or "resolveSpecials" or "payOrExhaust" or "payOrEffect"
        or "chooseTopForHand" or "chooseDiscardToShuffle"
        or "thwartDifferentSchemes" or "makeTheCall" or "legalPractice"
        or "attack" or "defense" or "thwart" or "thwartSchemes"
        or "placeThreat" or "enemyAttacks" or "enemySchemes");

    private static bool Test(AbilityNode node, Cast cast) => node.Kind switch
    {
        "and" => Nodes(node.Argument).All(each => Test(each, cast)),
        "or" => Nodes(node.Argument).Any(each => Test(each, cast)),
        "not" => !Test(Tree(node.Argument), cast),
        "finalStep" => cast.FinalStep,
        "paidWithResource" => PaidWith(cast, Word(node.Argument)),
        "threatCause" => cast.Occurrence.Threat?.Cause == (Word(node.Argument) switch
            {
                "villainPhase" => ThreatCause.VillainPhase,
                "enemyScheme" => ThreatCause.EnemyScheme,
                "incite" => ThreatCause.Incite,
                "cardAbility" => ThreatCause.CardAbility,
                var cause => throw new AbilityException($"'{cause}' is not a threat cause"),
            }),
        // Through `Every` and not `Find`: "is there one" is a question about a
        // set, and a query that names many -- "an upgrade or support you
        // control" -- has to be answerable by it. `Every` falls back to `Find`
        // for the queries that name one, so both shapes go through here.
        "exists" => Every(node.Argument, cast).Count > 0,
        "canMakeTheCall" => CanMakeTheCall(cast),
        "canLegalPractice" => cast.World.Seats[cast.Player].Hand.Cards.Any(
                card => card.ObjectId != cast.Source.ObjectId)
            && Every(node.Argument, cast).Any(
                scheme => scheme.Tokens.GetValueOrDefault("k_threat") > 0),
        "canAutomaticThwart" => Find(node.Argument, cast) is { } scheme
            && BasicPowers.CanAutomaticallyThwart(
                cast.World, cast.World.Facts, cast.Player, scheme),

        // "If Vulture is in play". `rr:identity.2` makes a title name one
        // card -- "if a card refers to a hero or alter-ego by title, it refers
        // only to the identity with that title" -- so this compares titles and
        // not printed ids, and asks only of the places `rr:in-play-and-out-of-play`
        // calls in play.
        "titleInPlay" => cast.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Any(card => string.Equals(
                cast.World.Facts.Title(card.FaceId), Word(node.Argument),
                StringComparison.Ordinal)),

        // "If no damage was healed this way" and its family: a comparison
        // against what an earlier action in this ability actually did.
        "atLeast" => Amount(node.Require("value"), cast) >= Amount(node.Require("count"), cast),

        // `rr:form` -- "(Hero)" and "(Alter-Ego)" on a card gate the ability by
        // which form the player is in. Not a boolean: `Forms.Of` answers with a
        // set, because a hero can print more than two faces.
        "inForm" => Forms.In(
            cast.World,
            cast.World.Seats[Seat(node.Require("player"), cast)],
            cast.World.Facts,
            Word(node.Require("form"))),

        "activationIs" => cast.World.Activation is { } activation
            && activation.Attacking == (Word(node.Argument) switch
            {
                "attack" => true,
                "scheme" => false,
                var kind => throw new AbilityException(
                    $"'{kind}' is not an enemy activation kind"),
            }),

        // "After [enemy] attacks **and damages** you". Two facts, and
        // `rr:attack-enemy-activation.step.6.a` lists them as one trigger
        // shape -- but the abilities it lists all run in the window *after* the
        // attack, by which time the damage is on a dial that had damage on it
        // before. So the attack carries what it did, and this reads it.
        //
        // A test rather than a triggering condition of its own, because the two
        // are indistinguishable for a forced ability: it is in the same window
        // either way, and does nothing when the attack did not land. A card
        // whose trigger is optional would be able to tell them apart -- the
        // prompt would appear -- and that is the case to change this for.
        "attackDamaged" => cast.World.FinishedAttack is { Damaged: true } landed
            && landed.Enemy == cast.Source.ObjectId,

        // `rr:modes-of-play` -- "in expert mode" on a card, which 86 cards in
        // the pool print. Not a property of the card or of anything on the
        // board: it is how the game was set up, so the board carries it.
        //
        // The argument is the mode's own name and is checked, so that a card
        // reaching for one of the other three -- heroic, skirmish, campaign --
        // is a card that says so rather than one that quietly reads "expert".
        "inExpertMode" => Word(node.Argument) == "expert"
            ? cast.World.Expert
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' asks about the '{Word(node.Argument)}' mode, and "
                + "rr:modes-of-play names four of which only 'expert' is modelled"),

        "hasStatus" => Find(node.Require("card"), cast) is { } host
            && Statuses.Has(cast.World, host, Word(node.Require("status"))),

        "hasTrait" => Find(node.Require("card"), cast) is { } traitHolder
            && Rules.State.Traits.Has(
                cast.World, traitHolder, Word(node.Require("trait")), cast.World.Facts),

        "cardSet" => Find(node.Require("card"), cast) is { } setCard
            && string.Equals(
                cast.World.Facts.EncounterSet(setCard.FaceId), Word(node.Require("set")),
                StringComparison.Ordinal),

        "isTitle" => Find(node.Require("card"), cast) is { } titled
            && string.Equals(
                cast.World.Facts.Title(titled.FaceId), Word(node.Require("title")),
                StringComparison.Ordinal),

        "discardedWithResource" => cast.Discarded.Any(card =>
            Resources.GeneratedBy(card.FaceId, cast.World.Facts).Contains(
                Word(node.Argument), StringComparison.Ordinal)),

        "defeatedByYou" => cast.Occurrence.Defeat is { By: >= 0 } defeatedByYou
            && defeatedByYou.By < cast.World.Cards.Count
            && ControllerOf(cast.World, cast.World.Cards[defeatedByYou.By]) == Resolver(cast),

        "wasDefeated" => Find(node.Argument, cast) is { } defeatedCard
            && cast.Occurrence.Defeats.Any(defeat => defeat.Card == defeatedCard.ObjectId),

        "heroDefended" => cast.World.FinishedAttack is { BasicDefense: true } defended
            && defended.Defender == cast.World.Seats[Resolver(cast)].IdentityCard.ObjectId,

        "undefendedAttack" => cast.World.Attack is { IsDefended: false },

        // "After **an ally** is defeated". A card type, asked of a card the
        // ability has already named -- `rr:defeat` is one rule for every kind
        // of card and the cards are the ones that narrow it.
        "isKind" => Find(node.Require("card"), cast) is { } subject
            && cast.World.Facts.Kind(subject.FaceId) == Kind(Word(node.Require("kind"))),

        "isYourIdentity" => (cast.Occurrence.Is(Steps.DamageWouldBeDealt)
                && cast.World.Attack is { } attack
                    ? attack.Target
                    : Find(node.Argument, cast)?.ObjectId)
            == cast.World.Seats[Resolver(cast)].IdentityCard.ObjectId,

        // "Defeated **by anything other than consequential damage**." What did
        // it is carried on the occurrence's record of the defeat, and the word
        // here names the rule rather than the engine's spelling of it -- see
        // `Cause`.
        "defeatedBy" => cast.Occurrence.Defeat is { } defeat
            && string.Equals(defeat.How, Cause(Word(node.Argument), cast), StringComparison.Ordinal),


        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' uses the test node '{node.Kind}', "
            + "which is not implemented"),
    };

    private static bool PaidWith(Cast cast, string resource) =>
        cast.Payment.Contains(resource[0])
        || cast.World.Effects.Active().Any(effect =>
            effect.Card == cast.Source.ObjectId
            && string.Equals(effect.Kind, "paid:" + resource, StringComparison.Ordinal));

    /// <summary>
    /// "Deal each player a facedown encounter card" — <c>rr:deal</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Dealt is not revealed.</b> <c>rr:deal</c> puts the card facedown in
    /// front of the player and leaves it there; step 4 of the villain phase is
    /// what turns it over, and it drains the whole queue however the cards got
    /// into it. So a card dealt during setup waits for the first villain phase,
    /// which is what it does at a table.
    /// </para>
    /// <para>
    /// In player order, because <c>rr:in-player-order</c> is what "each player"
    /// means whenever the order is observable — and it is here, since the deck
    /// can empty part-way round.
    /// </para>
    /// </remarks>
    private static void DealEncounterCards(AbilityNode node, Cast cast)
    {
        long each = node.Field("count") is { } count ? Number(count) : 1;
        IReadOnlyList<int> players = node.Field("player") is { } player
            ? [Seat(player, cast)]
            : [.. cast.World.PlayerOrder];
        for (long dealt = 0; dealt < each; dealt++)
        {
            foreach (int seat in players)
            {
                if (Rules.Play.Deal.EncounterCard(
                        cast.World, seat, cast.Trigger, cast.Events) is null)
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// "Put the top card of your deck into play facedown … as a Drone minion."
    /// </summary>
    private static void CreateDrones(AbilityNode node, Cast cast)
    {
        long count = node.Field("count") is { } amount ? Number(amount) : 1;
        foreach (int player in Seats(node.Require("player"), cast))
        {
            for (long created = 0; created < count; created++)
            {
                FacedownDrones.EngageTop(
                    cast.World, player, cast.Trigger, "Create_Drone", cast.Events);
            }
        }
    }

    private static bool CanCreateDrones(AbilityNode node, Cast cast) =>
        (node.Field("count") is not { } count || Number(count) > 0)
        && Seats(node.Require("player"), cast).Any(player =>
            cast.World.Seats[player].Deck.Cards.Count > 0
            || cast.World.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player).Cards.Count > 0);

    /// <summary>
    /// "Put it into play engaged with you" — <c>rr:play-put-into-play</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Put into play is not revealed.</b>
    /// <c>rr:when-revealed-abilities.2</c>: "if an encounter card with a
    /// '<b>When Revealed</b>' ability is put into play without being revealed,
    /// the '<b>When Revealed</b>' ability does not trigger." So this moves the
    /// card and stops, where <c>Steps.RevealEncounterCard</c> would have run
    /// the card's own text — and the difference is the whole reason the two are
    /// separate words here.
    /// </para>
    /// <para>
    /// <b>The keywords still fire.</b> <c>rr:enters-play</c> is "any time when
    /// a card transitions from an out-of-play area into play", which a card put
    /// into play does — so toughness and uses X apply, and only the "When
    /// Revealed" is skipped.
    /// </para>
    /// <para>
    /// "Engaged with you" is the only destination any authored card asks for.
    /// <c>rr:engage.1</c> makes it a place: "when a minion engages a player, it
    /// is placed in that player's play area", so engagement is where the card
    /// sits rather than a flag on it.
    /// </para>
    /// </remarks>
    private static void PutIntoPlay(AbilityNode node, Cast cast)
    {
        var card = Find(node.Require("card"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would put a card into play that is not there");

        string where = Word(node.Require("where"));
        if (!string.Equals(where, "engagedWithYou", StringComparison.Ordinal))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' puts a card into play '{where}', "
                + "which is not implemented");
        }

        PutIntoPlay(card, cast.Player, cast);
    }

    /// <summary>Puts one exact minion into play engaged with a named player.</summary>
    private static void PutIntoPlay(Card card, int player, Cast cast)
    {
        var into = cast.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player));
        var from = card.Area;

        // Moving into an in-play area turns the card faceup in `Card.MovedTo`;
        // a second `TurnFaceUp` here would be an equivalent state write.
        World.MoveToTop(card, into);
        cast.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(into),
            [new Landing(card.ObjectId, into.Cards.Count - 1)])
        {
            Trigger = cast.Trigger, Verb = "Put_Into_Play",
        });

        Reveal.EnterPlay(cast.World, cast.World.Facts, card, cast.Events);
    }

    private static void GiveStatus(AbilityNode node, Cast cast)
    {
        // "Stun **each hero**" and "stun your hero" are the same node with a
        // different query, the way `placeThreat` names one scheme or all of
        // them: `Every` answers both.
        foreach (var host in Every(node.Require("card"), cast))
        {
            GiveStatus(node, cast, host);
        }
    }

    private static void GiveStatus(AbilityNode node, Cast cast, Card host)
    {
        string what = Word(node.Require("status"));

        // Through the rules rather than straight at `Statuses.Give`:
        // `rr:status-cards.1` caps how many a character can hold,
        // `rr:stalwart` makes that cap zero, and `rr:vulnerable` discards the
        // character. A card giving a status does not get to skip any of them.
        var status = Reveal.Afflict(
            cast.World, cast.World.Facts, host, what, cast.Trigger, cast.Events);
        if (status is null)
        {
            return;
        }

        cast.Events.Add(new CardAttached(status.ObjectId, host.ObjectId)
        {
            Trigger = cast.Trigger, Verb = "Give_Status",
        });
    }

    // `rr:attachment` -- "when an attachment enters play, it attaches to another
    // card or game element".
    private static void AttachTo(AbilityNode node, Cast cast)
    {
        var host = Find(node.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' attaches to a card that is not there");

        var onto = cast.World.AreaOf(
            DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);
        var from = cast.Source.Area;
        World.MoveToTop(cast.Source, onto);

        cast.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(onto),
            [new Landing(cast.Source.ObjectId, onto.Cards.Count - 1)])
        {
            Trigger = cast.Trigger, Verb = "Attach",
        });
        cast.Events.Add(new CardAttached(cast.Source.ObjectId, host.ObjectId)
        {
            Trigger = cast.Trigger, Verb = "Attach",
        });
    }

    /// <summary>
    /// What one constant ability grants, as continuous effects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A deliberately tiny vocabulary: a sequence, a condition, and a grant.
    /// <c>rr:ability.9</c> is why the condition is here rather than resolved
    /// once — "some constant abilities continuously seek a specific condition
    /// <i>(denoted by words such as 'during', 'if', or 'while')</i>. The effects
    /// of such abilities are active anytime the specific condition is met." So
    /// the test is re-read on every ask, and Unus stops retaliating the moment
    /// Gene Pool is thwarted below three threat.
    /// </para>
    /// <para>
    /// Everything else throws. A constant ability that moves a card or deals
    /// damage is a different shape from this one — it would have to happen at a
    /// moment, and a constant ability has no moment — so the card that needs it
    /// needs a design rather than a case.
    /// </para>
    /// </remarks>
    private static void Grants(AbilityNode node, Cast cast, List<ContinuousEffect> found)
    {
        switch (node.Kind)
        {
            case "seq":
            case "and":
                foreach (var step in Nodes(node.Argument))
                {
                    Grants(step, cast, found);
                }

                break;

            case "if":
                string branch = Test(Tree(node.Require("test")), cast) ? "then" : "else";
                if (node.Field(branch) is { } taken)
                {
                    Grants(Tree(taken), cast, found);
                }

                break;

            case "grant":
                if (Find(node.Require("card"), cast) is { } grantTarget)
                {
                    found.Add(Grant(node, cast, grantTarget));
                }
                else if (Word(node.Require("card")) is not ("yourHero" or "yourAlterEgo"))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' card {cast.Source.ObjectId} in "
                        + $"{cast.Source.Area.Type} hosted by {cast.Source.Area.Host} would grant "
                        + "to a card that is not there");
                }
                break;

            case "grantEach":
                foreach (var target in Every(node.Require("cards"), cast))
                {
                    found.Add(Grant(node, cast, target));
                }
                break;

            case "preventThreatRemoval":
                // A prohibition is answered by `CanRemoveThreat`; it is not a
                // numeric modifier and therefore contributes no effect here.
                break;

            case "doubleResourceFor":
                // This constant acts while its resource card is spent from
                // hand. `ResourcesGeneratedBy` reads it with the payment's
                // target card, which is context this general effect list does
                // not carry.
                break;

            case "requireAllyDefender":
                // Defender declaration carries the attack and its engaged
                // player; `Defenders` reads this constraint in that context.
                break;

            case "preventDamageFrom":
            case "preventDamageWhile":
                // Damage carries both source and target. `CanTakeDamage`
                // evaluates these prohibitions in that complete context.
                break;

            case "preventReady":
                // The card to be readied and the source of that instruction
                // are available only when `CanReady` asks the question.
                break;

            default:
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has a constant ability using the node "
                    + $"'{node.Kind}', which a constant ability cannot be written with");
        }
    }

    private static bool ProhibitsThreatRemoval(AbilityNode node, Cast cast, Card scheme)
    {
        return node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument).Any(step =>
                ProhibitsThreatRemoval(step, cast, scheme)),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch && ProhibitsThreatRemoval(Tree(branch), cast, scheme),
            "preventThreatRemoval" => Find(node.Argument, cast)?.ObjectId == scheme.ObjectId,
            _ => false,
        };
    }

    /// <summary>One keyword a constant ability gives something.</summary>
    /// <remarks>
    /// <para>
    /// <b>The amount defaults to one, not to zero.</b> "Unus gains retaliate 1"
    /// states its number and "Unus also gains stalwart" does not, because a
    /// keyword without one is simply present — and the engine asks whether a
    /// card is stalwart by reading the field and comparing it to zero
    /// (<c>Statuses</c>, <c>rr:stalwart.1</c>). A grant defaulting to zero would
    /// parse, register, and mean the opposite of what the card says.
    /// </para>
    /// <para>
    /// The keyword is held against the fields the engine actually reads, for
    /// the reason the whole dataset is: <c>stallwart</c> would otherwise sit in
    /// the file looking implemented and grant nothing for ever.
    /// </para>
    /// </remarks>
    private static ContinuousEffect Grant(AbilityNode node, Cast cast)
    {
        var target = Find(node.Require("card"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would grant to a card that is not there");

        return Grant(node, cast, target);
    }

    private static ContinuousEffect Grant(AbilityNode node, Cast cast, Card target)
    {

        // `rr:traits.1` -- "traits have no inherent effects on the game.
        // Instead, some card abilities reference cards that possess or lack
        // specific traits." So a granted trait carries no amount and is not
        // held against the printed fields: it is a name other cards ask about,
        // and the pool spells one in capitals.
        if (node.Field("trait") is { } gained)
        {
            return new ContinuousEffect(
                EffectSource.ConstantAbility,
                Kind: Rules.State.Traits.Granted + Word(gained),
                Card: cast.Source.ObjectId,
                Affects: target.ObjectId,
                Lasts: Duration.WhileInPlay);
        }

        string keyword = Word(node.Require("keyword"));
        if (!StateFields.IsModifiable(keyword) && !Keywords.Granted.Contains(keyword))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' grants '{keyword}', which is not a keyword or "
                + "field the engine reads modifiers into");
        }

        return new ContinuousEffect(
            EffectSource.ConstantAbility,
            Kind: keyword,
            Amount: node.Field("amount") is { } amount ? Amount(amount, cast) : 1,
            Card: cast.Source.ObjectId,
            Affects: target.ObjectId,
            Lasts: Duration.WhileInPlay);
    }

    // `rr:lasting-effects` -- an effect "for a specified duration (such as
    // [...] 'until the end of this attack')".
    private static void GrantUntil(AbilityNode node, Cast cast)
    {
        var target = Find(node.Require("card"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would grant to a card that is not there");

        if (node.Field("trait") is { } gained)
        {
            EnsureLastingPeriodOpen(node, cast);
            cast.World.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: Rules.State.Traits.Granted + Word(gained),
                Amount: 1,
                Card: cast.Source.ObjectId,
                Affects: target.ObjectId,
                Lasts: Duration.UntilEndOf(Word(node.Require("until")))));
            return;
        }

        // Held against the fields the engine actually reads, exactly as a
        // constant ability's grant is: an unrecognised name would register
        // happily, expire on time, and modify nothing in between. "+2 SCH" is
        // the same mechanism as "gains overkill" and reaches it through the
        // same door, which is why `scheme` sits in this vocabulary beside
        // `overkill`.
        string keyword = Word(node.Require("keyword"));
        if (!StateFields.IsModifiable(keyword) && !Keywords.Granted.Contains(keyword))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' grants '{keyword}' for a duration, which is not a "
                + "keyword or field the engine reads modifiers into");
        }

        EnsureLastingPeriodOpen(node, cast);

        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: keyword,
            Amount: node.Field("amount") is { } amount ? Amount(amount, cast) : 0,
            Card: cast.Source.ObjectId,
            Affects: target.ObjectId,
            Lasts: Duration.UntilEndOf(Word(node.Require("until")))));
    }

    private static void EnsureLastingPeriodOpen(AbilityNode node, Cast cast)
    {
        if (!LastingPeriodIsOpen(node, cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' begins a lasting effect outside its named period");
        }
    }

    // `rr:delayed-effect.1` -- an effect that resolves "after their specified
    // timing point or future condition occurs or becomes true".
    private static void DelayUntil(AbilityNode node, Cast cast)
    {
        var effect = Tree(node.Require("effect"));

        // "If a character is damaged by this attack, that character is
        // stunned." **The card it acts on does not exist yet** -- the attack
        // has not happened, so there is nobody to name. `Affects` stays null
        // and the occurrence names the card when the effect comes due.
        if (effect.Kind == "giveStatus"
            && Word(effect.Require("card")) == "damaged"
            && Word(effect.Require("status")) == Statuses.Stunned)
        {
            // **Bounded by the attack as well as by the condition.** "If a
            // character is damaged by **this attack**" is false once the attack
            // is over, so an attack that damaged nobody -- `rr:tough.3`, a
            // tough status card ate it -- must not leave the effect waiting for
            // somebody else's. `Duration` carries both: the next time damage is
            // dealt, and not past the end of this attack.
            cast.World.Effects.Register(new ContinuousEffect(
                EffectSource.DelayedEffect,
                Kind: DelayedEffects.StunTheSubject,
                Card: cast.Source.ObjectId,
                Affects: null,
                Lasts: new Duration(
                    Until: node.Field("within") is { } bound ? Word(bound) : null,
                    OnCondition: Word(node.Require("condition")),
                    Uses: 1)));
            return;
        }

        if (effect.Kind != "discard")
        {
            // A delayed effect is data on the board, not a closure, so what it
            // will do has to be a `Kind` the engine can read back after a save.
            // `DelayedEffects` knows one; the rest is the vocabulary that grows.
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' delays '{effect.Kind}', and only 'discard' can be "
                + "written down as a delayed effect");
        }

        var target = Find(effect.Field("card") ?? effect.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would delay a discard of a card that is not there");

        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect,
            Kind: DelayedEffects.DiscardFromPlay,
            Card: cast.Source.ObjectId,
            Affects: target.ObjectId,
            Lasts: Duration.NextTime(Word(node.Require("condition")))));
    }

    private static void Discard(AbilityNode node, Cast cast)
    {
        if (Find(node.Field("card") ?? node.Argument, cast) is { } target)
        {
            // rr:target.2 lets a multi-target ability initiate when at least
            // one target is valid. A different target can therefore be absent
            // by resolution, and this effect simply has nothing to discard.
            Rules.Play.Discard.Card(cast.World, target, cast.Trigger, cast.Events);
        }
    }

    // ---- reading a value ---------------------------------------------------

    /// <summary>
    /// "Flip to alter-ego form" — <c>rr:form-change-form</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It does not use up the turn's flip.</b> <c>rr:form-change-form.3</c>:
    /// "if a card ability causes a player to change forms, it does not count
    /// against the one voluntary form change the player is permitted during
    /// their turn that round." So this goes through <c>Forms.Change</c>, which
    /// turns the card, and leaves <c>Seat.FormChangedInRound</c> alone —
    /// <c>Game</c> sets that when the player takes the turn option.
    /// </para>
    /// <para>
    /// A player already in the named form does nothing. "Flip <b>to</b>
    /// alter-ego form" names a destination, and flipping an alter-ego would
    /// arrive at the wrong one.
    /// </para>
    /// </remarks>
    private static void ChangeForm(AbilityNode node, Cast cast)
    {
        var seat = cast.World.Seats[Seat(node.Require("player"), cast)];
        string form = Word(node.Require("to"));
        if (Forms.In(cast.World, seat, cast.World.Facts, form))
        {
            return;
        }

        string was = seat.IdentityCard.FaceId;
        Forms.Change(seat, cast.World.Facts);
        cast.Events.Add(new CardsFlipped([seat.IdentityCard.ObjectId], true)
        {
            Trigger = cast.Trigger, Verb = "Change_Form",
        });

        if (!Forms.In(cast.World, seat, cast.World.Facts, form))
        {
            throw new RulesNotImplementedException(
                $"flipping '{was}' did not reach {form}");
        }
    }

    /// <summary>"Remove … from the game" — <c>rr:removed-from-the-game</c>.</summary>
    /// <remarks>
    /// Removed and not discarded: <c>rr:defeat.2</c> keeps the two apart, and a
    /// card in the discard pile can come back where one out of the game cannot.
    /// </remarks>
    private static void RemoveFromGame(AbilityNode node, Cast cast)
    {
        var card = Find(node.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would remove a card that is not there");

        var from = card.Area;
        var removed = cast.World.AreaOf(DeckType.RemovedArea);
        var constantsEnding = cast.World.Effects.PreflightConstantsEnding(card);
        using var departure = constantsEnding.Begin();
        if (DeckTypes.IsInPlay(from.Type))
        {
            Rules.Play.Discard.Attachments(
                cast.World, card, cast.Trigger, cast.Events);
        }
        World.MoveToTop(card, removed);
        cast.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(removed),
            [new Landing(card.ObjectId, removed.Cards.Count - 1)])
        {
            Trigger = cast.Trigger, Verb = "Remove_From_Game",
        });
        constantsEnding.Complete(cast.Trigger, cast.Events);
    }

    /// <summary>
    /// "Place it here instead" — <c>rr:replacement-effect</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The damage does not happen to the character at all: it is <i>placed</i>
    /// on this card as damage tokens, which is why it goes on with
    /// <c>Card.TakeDamage</c> rather than through <c>Damage.Deal</c>. Dealing it
    /// would start the nine steps of <c>rr:damage</c> again, on a card that is
    /// not a character.
    /// </para>
    /// <para>
    /// What is left afterwards is zero, and <c>rr:replacement-effect.1</c> then
    /// holds for free: the damage is no longer imminent, so nothing later in
    /// the order can respond to it.
    /// </para>
    /// </remarks>
    private static void Soak(AbilityNode node, Cast cast)
    {
        var onto = Find(node.Require("onto"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would soak damage onto a card that is not there");

        long before = onto.Damage;
        onto.TakeDamage(cast.Incoming);
        cast.Events.Add(new FieldSet(onto.ObjectId, "k_damage", before, onto.Damage)
        {
            Trigger = cast.Trigger, Verb = "Place_Damage",
        });

        cast.Replace(0);
    }

    /// <summary>"Exhaust …" — <c>rr:exhausted</c>.</summary>
    /// <remarks>
    /// A card already exhausted stays exhausted and reports nothing:
    /// <c>rr:exhausted</c> is a state and not a counter, so exhausting
    /// twice is not two exhaustions and must not be two events on the wire.
    /// </remarks>
    private static void Exhaust(AbilityNode node, Cast cast)
    {
        foreach (var target in Every(node.Argument, cast).Where(target => target.Ready))
        {
            target.Exhaust();
            cast.Events.Add(new FieldSet(target.ObjectId, "is_exhaust", 0, 1)
            {
                Trigger = cast.Trigger, Verb = "Exhaust",
            });
        }
    }

    private static void Ready(AbilityNode node, Cast cast)
    {
        foreach (var target in Every(node.Argument, cast).Where(target =>
            !target.Ready
            && cast.Abilities.CanReady(cast.World, target, cast.Source)))
        {
            target.Refresh();
            cast.Events.Add(new FieldSet(target.ObjectId, "is_exhaust", 1, 0)
            {
                Trigger = cast.Trigger, Verb = "Ready",
            });
        }
    }

    private static void DrawToHandSize(AbilityNode node, Cast cast)
    {
        int player = Seat(node.Argument, cast);
        var seat = cast.World.Seats[player];
        int count = (int)Math.Max(
            0, PhaseEnd.HandSize(cast.World, seat, cast.World.Facts)
                - HandCountDuringEvent(cast, seat));
        Draw.Cards(cast.World, player, count, cast.Trigger, cast.Events);
    }

    /// <summary>"Draw up to your printed hand size" — <c>rr:printed</c>.</summary>
    private static void DrawToPrintedHandSize(AbilityNode node, Cast cast)
    {
        int player = Seat(node.Argument, cast);
        var seat = cast.World.Seats[player];
        long printed = cast.World.Facts.PrintedValue(
            seat.IdentityCard.FaceId, "HS", cast.World.Players);
        int count = (int)Math.Max(0, printed - HandCountDuringEvent(cast, seat));
        Draw.Cards(cast.World, player, count, cast.Trigger, cast.Events);
    }

    private static bool CanDrawToPrintedHandSize(AbilityNode node, Cast cast)
    {
        int player = Seat(node.Argument, cast);
        var seat = cast.World.Seats[player];
        return HandCountDuringEvent(cast, seat) < cast.World.Facts.PrintedValue(
            seat.IdentityCard.FaceId, "HS", cast.World.Players);
    }

    private static int HandCountDuringEvent(Cast cast, Seat seat) =>
        seat.Hand.Cards.Count - (cast.Source.Area == seat.Hand
            && cast.World.Facts.Kind(cast.Source.FaceId) == CardKind.Event ? 1 : 0);

    private static void RemoveCounters(AbilityNode node, Cast cast)
    {
        string type = Word(node.Argument);
        string key = CounterKeyForRemoval(cast.Source, type)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' has no {type} counter to remove");
        long before = cast.Source.Tokens.GetValueOrDefault(key);

        cast.Source.PlaceTokens(key, -1);
        cast.Events.Add(new FieldSet(cast.Source.ObjectId, key, before, before - 1)
        {
            Trigger = cast.Trigger, Verb = "Remove_Counter",
        });

        if (CounterCount(cast.Source, "allPurpose") == 0
            && !Characteristics.IsLost(cast.World, cast.Source, "uses")
            && Reveal.Uses(cast.World.Facts.Attributes(cast.Source.FaceId)).Count > 0)
        {
            Rules.Play.Discard.Card(cast.World, cast.Source, cast.Trigger, cast.Events);
        }
    }

    /// <summary>
    /// Advances because a card effect says to —
    /// <c>rr:main-scheme-main-scheme-deck.2.2</c>.
    /// </summary>
    /// <remarks>
    /// "If the main scheme advances other than through having threat on it
    /// equal to or greater than its target threat value, that main scheme is
    /// not considered completed." This calls the deck transition directly and
    /// never writes <c>is_completed</c>. The DSL word <c>next</c> is the
    /// engine's choice; stage-addressed advancement needs a separate
    /// implementation.
    /// </remarks>
    private static void AdvanceMainScheme(AbilityNode node, Cast cast)
    {
        if (!string.Equals(Word(node.Argument), "next", StringComparison.Ordinal))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' advances to an unsupported main scheme stage");
        }

        var scheme = cast.World.TheCardIn(DeckType.MainSchemesArea)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' advances a main scheme that is not in play");
        MainScheme.Advance(
            cast.World, cast.World.Facts, cast.Abilities, scheme,
            cast.Trigger, cast.Events);
    }

    private static bool CanAdvanceMainScheme(AbilityNode node, Cast cast)
    {
        if (!string.Equals(Word(node.Argument), "next", StringComparison.Ordinal))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' advances to an unsupported main scheme stage");
        }

        return cast.World.TheCardIn(DeckType.MainSchemesArea) is not null
            && cast.World.AreaOf(DeckType.MainSchemesDeck).Cards.Count > 0;
    }

    /// <summary>
    /// Reads a named counter pool, or every typed pool when the card says
    /// "all-purpose counter" — <c>rr:all-purpose-counter.1</c> and
    /// <c>rr:all-purpose-counter.2</c>.
    /// </summary>
    /// <remarks>
    /// Counters use the same token inventory as threat, damage, and status
    /// markers because the rules consider them tokens for every game purpose.
    /// The DSL spelling <c>allPurpose</c> is the engine's choice. A reference
    /// to it can see every <c>c_*</c> pool regardless of the type a card gave
    /// that physical counter.
    /// </remarks>
    private static long CounterCount(Card card, string type) =>
        string.Equals(type, "allPurpose", StringComparison.Ordinal)
            ? card.Tokens
                .Where(pair => pair.Key.StartsWith("c_", StringComparison.Ordinal))
                .Sum(pair => pair.Value)
            : card.Tokens.GetValueOrDefault("c_" + type);

    /// <summary>Resolves the physical counter removed by a cost.</summary>
    /// <remarks>
    /// If more than one typed pool is present, the rule permits the player to
    /// choose either one. The current action protocol has no counter-choice
    /// affordance, so resolution raises before changing state rather than
    /// choosing an outcome on the player's behalf.
    /// </remarks>
    private static string? CounterKeyForRemoval(Card card, string type)
    {
        if (!string.Equals(type, "allPurpose", StringComparison.Ordinal))
        {
            string typed = "c_" + type;
            return card.Tokens.GetValueOrDefault(typed) > 0 ? typed : null;
        }

        string[] pools = [.. card.Tokens
            .Where(pair => pair.Value > 0
                && pair.Key.StartsWith("c_", StringComparison.Ordinal))
            .Select(pair => pair.Key)
            .Order(StringComparer.Ordinal)];
        return pools.Length switch
        {
            0 => null,
            1 => pools[0],
            _ => throw new RulesNotImplementedException(
                $"'{card.FaceId}' must choose which all-purpose counter to remove"),
        };
    }

    private static void PreventDamage(AbilityNode node, Cast cast)
    {
        int target = cast.Occurrence.Target >= 0
            ? cast.Occurrence.Target
            : cast.Occurrence.Subject;
        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: "preventDamage",
            Amount: node.Field("amount") is { } amount ? Amount(amount, cast) : long.MaxValue,
            Card: cast.Source.ObjectId,
            Affects: target,
            Lasts: new Duration(Uses: 1)));
    }

    private static void CancelWhenRevealed(Cast cast)
    {
        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: "cancelWhenRevealed",
            Card: cast.Source.ObjectId,
            Affects: cast.Occurrence.Subject,
            Lasts: new Duration(Uses: 1)));
    }

    /// <summary>
    /// "Reveal the top card of the encounter deck" — <c>rr:reveal</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Revealed, not dealt.</b> <c>rr:deal-deal-an-encounter-card</c> puts a
    /// card facedown in a queue to be resolved later; this one is turned over
    /// now. The difference is a whole villain phase, and Under Fire says
    /// "reveal".
    /// </para>
    /// <para>
    /// Scheduled, for the same reason <c>search</c> schedules: revealing an
    /// encounter card is a step with an interrupt window and a response window
    /// around it, and the card revealed may itself ask a player something.
    /// </para>
    /// <para>
    /// <c>EncounterDeck.TakeTop</c> is what draws it, so an empty deck
    /// reshuffles its discard pile first — <c>rr:encounter-deck.3</c> — rather
    /// than this quietly doing nothing.
    /// </para>
    /// </remarks>
    private static Card? TopOfTheEncounterDeck(Cast cast) =>
        EncounterDeck.TakeTop(cast.World, cast.Trigger, cast.Events);

    /// <summary>Reveals one card, wherever it was.</summary>
    /// <remarks>
    /// <b>The card moves now and resolves later.</b> It goes to the revealing
    /// area at once, so a later step of the same ability cannot find it where
    /// it was — Shadow of the Past reveals two cards out of a pile and then
    /// shuffles "the rest" of that pile away, and a reveal that only scheduled
    /// would shuffle the two it had just chosen.
    /// </remarks>
    private static void RevealCard(Card? card, Cast cast)
    {
        if (card is null)
        {
            return;
        }

        World.MoveToTop(card, cast.World.AreaOf(DeckType.RevealingArea));
        cast.World.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCard,
            cast.World.Agenda.Current?.Round ?? 0,
            4,
            Index: cast.Player,
            Subject: card.ObjectId,
            Seat: cast.Player));
        cast.ResolveEffect();
    }

    /// <summary>
    /// "Shuffle the rest of … into the encounter deck" — <c>rr:shuffle</c>.
    /// </summary>
    /// <remarks>
    /// The cards move in the order the query answers and the deck is shuffled
    /// once afterwards, not once per card. The shuffle draws from the game's
    /// single random stream, so how many times it happens is a wire fact and
    /// not a detail.
    /// </remarks>
    private static void ShuffleInto(AbilityNode node, Cast cast)
    {
        var deck = Area(Word(node.Require("deck")), cast);
        bool applied = false;
        foreach (var card in Every(node.Require("cards"), cast))
        {
            var from = card.Area;
            World.MoveToTop(card, deck);
            cast.Events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(deck),
                [new Landing(card.ObjectId, deck.Cards.Count - 1)])
            {
                Trigger = cast.Trigger, Verb = "Shuffle_Into",
            });
            applied = true;
        }

        applied |= cast.World.Shuffle(deck);
        if (applied)
        {
            cast.ResolveEffect();
        }
    }

    /// <summary>
    /// "Search the encounter deck and discard pile for … and reveal it" —
    /// <c>rr:search</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:search.2</c> — "cards being searched are not considered to leave
    /// the searched area" — so looking costs nothing and only the card found
    /// moves.
    /// </para>
    /// <para>
    /// <b>The reveal is scheduled, not done here.</b> Revealing an encounter
    /// card is a step with an interrupt window and a response window around it,
    /// and a reveal called inline would have neither. The step is the same one
    /// the villain phase uses, so the card found goes through
    /// <c>rr:reveal</c>'s four steps exactly as a dealt card does.
    /// </para>
    /// <para>
    /// <c>rr:search.3</c> — "if any portion of a deck is searched, upon
    /// completion of that game step, game function, or card ability, shuffle
    /// that entire deck." Taken as the ability completing, which is this method
    /// returning; the reveal it scheduled happens afterwards. Nothing in the
    /// pool that is reached this way reads the encounter deck, so the two
    /// readings agree on every board that exists — but this is the one written
    /// down.
    /// </para>
    /// <para>
    /// <c>rr:search.1</c> gives the player the choice when several cards match.
    /// That is a second suspension inside an ability that may already have one,
    /// so it is refused by name until a card needs it.
    /// </para>
    /// </remarks>
    private static void Search(AbilityNode node, Cast cast)
    {
        string wanted = Word(node.Require("for"));
        var searched = Nodes(node.Require("in")).Select(where => where.Kind).ToList();
        var areas = searched.Select(where => Area(where, cast)).ToList();

        var found = areas
            .SelectMany(area => area.Cards)
            .Where(card => string.Equals(card.FaceId, wanted, StringComparison.Ordinal))
            .ToList();

        if (found.Count > 1)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' searched and found {found.Count} copies of "
                + $"'{wanted}'; rr:search.1 gives the player that choice and asking is "
                + "not implemented");
        }

        bool applied = found.Count == 1;
        if (applied)
        {
            cast.World.Agenda.Then(new PhaseStep(
                Steps.RevealEncounterCard,
                cast.World.Agenda.Current?.Round ?? 0,
                4,
                Index: cast.Player,
                Subject: found[0].ObjectId,
                Seat: cast.Player));
        }

        cast.Results["found"] = found.Count;

        // `rr:search.3`. The discard pile is not a deck and is not shuffled --
        // and shuffling one would consume from the game's single random stream,
        // which is a wire format.
        foreach (var deck in areas.Where(area => area.Type == DeckType.EncounterDeck))
        {
            applied |= cast.World.Shuffle(deck);
        }
        if (applied)
        {
            cast.ResolveEffect();
        }
    }

    /// <summary>Which place on the board a word names.</summary>
    private static Area Area(string where, Cast cast) => where switch
    {
        "encounterDeck" => cast.World.AreaOf(DeckType.EncounterDeck),
        "encounterDiscardPile" => cast.World.AreaOf(DeckType.EncounterDiscardPile),
        "yourDeck" => cast.World.Seats[cast.Player].Deck,
        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' searches '{where}', which is not implemented"),
    };

    /// <summary>
    /// "Choose to either … or …" — <c>rr:choose-option</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The ability stops here.</b> An interpreter that returns a list of
    /// events has nowhere to ask a question, so the choice becomes a step on
    /// the agenda and what resumes the ability is the answer to it. The step
    /// carries the source card and the seat; <see cref="Choice"/> finds the
    /// node again from the card, which is why an ability may hold only one.
    /// </para>
    /// <para>
    /// <c>rr:choose-game-element.1</c> settles who is asked, and it is the
    /// player resolving the ability — not the first player, and not the card's
    /// owner, which an encounter card has not got.
    /// </para>
    /// </remarks>
    /// <summary>
    /// The steps of a <c>seq</c>, from wherever the ability left off.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An ability can ask more than once.</b> Eviction Notice says "you may
    /// flip to alter-ego form" and then "choose:", which is two questions in a
    /// row; 36 cards in the pool pair a "may" with a listed choice, and every
    /// "may" is itself a question.
    /// </para>
    /// <para>
    /// A suspended ability stores its exact authored ability and structural
    /// path in <see cref="PhaseStep"/>. Unwinding that path resumes nested
    /// sequences and branches without rerunning completed effects.
    /// </para>
    /// </remarks>
    private static void Sequence(AbilityNode node, Cast cast, int from)
    {
        if (from == 0)
        {
            _ = CanInitiateSequence(node, cast);
        }

        var steps = Nodes(node.Argument).ToList();
        bool outerContinuation = cast.HasContinuation;
        for (int step = from; step < steps.Count; step++)
        {
            cast.At(step);
            cast.SetContinuation(outerContinuation || step < steps.Count - 1);
            RunChild(steps[step], $"seq:{step}", cast);
            if (cast.Suspended)
            {
                return;
            }
        }
        cast.SetContinuation(outerContinuation);
    }

    /// <summary>Repeats one count-based “for each” effect.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:for-each.1-.2</c> makes damage and threat removal without a
    /// “choose” instruction one combined instance against one target. Those
    /// effects therefore multiply before entering the ordinary resolver; a
    /// loop would incorrectly spend Tough on the first point and deal the
    /// remaining points as later instances.
    /// </para>
    /// <para>
    /// <c>rr:for-each.3</c> makes an explicit choice a new decision every
    /// iteration. Each frame is persisted in the ability path so an answer can
    /// finish its iteration, update the board, and then ask the next question
    /// from the board as it now stands. Evaluating the child afresh also makes
    /// an ability modifier part of every instance as required by
    /// <c>rr:for-each.4</c>.
    /// </para>
    /// </remarks>
    private static void ForEach(AbilityNode node, Cast cast)
    {
        long count = Amount(node.Require("count"), cast);
        if (count < 0)
        {
            throw new AbilityException("'forEach' needs a non-negative 'count'");
        }
        if (count == 0)
        {
            return;
        }

        var effect = Tree(node.Require("effect"));
        if (!Choices(effect).Any())
        {
            switch (effect.Kind)
            {
                case "dealDamage":
                    if (DamageTargets(effect.Require("cards"), cast).Count != 1)
                    {
                        throw new RulesNotImplementedException(
                            $"'{cast.Source.FaceId}' has a for-each damage effect without "
                            + "choose and does not resolve to one target");
                    }
                    DealDamage(effect, cast, count);
                    return;

                case "removeThreat":
                    if (Every(effect.Require("scheme"), cast).Count != 1)
                    {
                        throw new RulesNotImplementedException(
                            $"'{cast.Source.FaceId}' has a for-each threat-removal effect "
                            + "without choose and does not resolve to one target");
                    }
                    RemoveThreat(effect, cast, count);
                    return;
            }

            if (ContainsForEachTarget(effect))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has a targeted for-each effect without choose "
                    + "whose one target cannot be persisted");
            }
        }

        bool outerContinuation = cast.HasContinuation;
        for (long iteration = 0; iteration < count; iteration++)
        {
            cast.SetContinuation(outerContinuation || iteration < count - 1);
            RunChild(effect, $"forEach:{iteration}:{count}", cast);
            if (cast.Suspended)
            {
                return;
            }
        }
        cast.SetContinuation(outerContinuation);
    }

    /// <summary>Interrupts a discard effect once for every matching card.</summary>
    /// <remarks>
    /// <c>rr:alteration-effect</c> says an “each time” effect halts the
    /// preceding ability, resolves in its entirety, and only then lets that
    /// ability continue. Discarding one card per frame makes that ordering
    /// observable: its alteration finishes before the next card is discarded.
    /// The exact-card binding survives an immediate encounter-deck reset.
    /// </remarks>
    private static void EachTime(AbilityNode node, Cast cast)
    {
        var preceding = EachTimePreceding(node, cast);
        long requested = Amount(preceding.Require("count"), cast);
        if (requested < 0)
        {
            throw new AbilityException("'eachTime' needs a non-negative discard count");
        }
        if (requested == 0)
        {
            return;
        }
        ValidateEachTimeBody(node, cast);

        var deck = cast.World.AreaOf(DeckType.EncounterDeck);
        var discard = cast.World.AreaOf(DeckType.EncounterDiscardPile);
        long available = deck.Cards.Count > 0 ? deck.Cards.Count : discard.Cards.Count;
        ContinueEachTime(node, cast, from: 0, Math.Min(requested, available));
    }

    private static AbilityNode EachTimePreceding(AbilityNode node, Cast cast)
    {
        var preceding = Tree(node.Require("effect"));
        if (preceding.Kind != "discardTop"
            || preceding.Field("player") is not null
            || !string.Equals(
                Word(preceding.Require("from")), "encounterDeck",
                StringComparison.Ordinal))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses each-time around an unsupported preceding effect");
        }
        return preceding;
    }

    private static void ValidateEachTimeBody(AbilityNode node, Cast cast)
    {
        if (ContainsUnreconstructibleAfterActivation(
            Tree(node.Require("then")), cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside an after-activation effect, "
                + "which cannot be reconstructed");
        }
    }

    private static bool ContainsUnreconstructibleAfterActivation(
        AbilityNode node, Cast cast)
    {
        if (node.Kind == "afterActivation")
        {
            return DelayedNeedsContinuationAddress(
                Tree(node.Require("effect")), cast, hasContinuation: false);
        }
        return ContinuationChildren(node).Any(child =>
            ContainsUnreconstructibleAfterActivation(child, cast));
    }

    private static bool DelayedNeedsContinuationAddress(
        AbilityNode node, Cast cast, bool hasContinuation)
    {
        if (node.Kind == "afterActivation"
            || node.Kind == "and" && Nodes(node.Argument).Skip(1).Any()
            || IsChoice(node)
            || node.Kind is "eachPlayer" or "attack" or "thwart" or "thwartSchemes")
        {
            return true;
        }
        if (node.Kind is "placeThreat" or "enemyAttacks" or "enemySchemes")
        {
            return hasContinuation;
        }
        if (node.Kind is "seq" or "and")
        {
            var children = Nodes(node.Argument).ToList();
            return children.Select((child, index) => (child, index)).Any(entry =>
                DelayedNeedsContinuationAddress(
                    entry.child, cast,
                    hasContinuation || entry.index < children.Count - 1));
        }
        if (node.Kind == "if")
        {
            return Branches.Select(node.Field)
                .Where(branch => branch is not null)
                .Any(branch => DelayedNeedsContinuationAddress(
                    Tree(branch!), cast, hasContinuation));
        }
        if (node.Kind is "then" or "otherwise")
        {
            return DelayedNeedsContinuationAddress(
                    Tree(node.Require("effect")), cast, hasContinuation: true)
                || DelayedNeedsContinuationAddress(
                    Tree(node.Require(node.Kind)), cast, hasContinuation);
        }
        if (node.Kind == "forEach")
        {
            if (AmountMayChange(node.Require("count")))
            {
                return DelayedNeedsContinuationAddress(
                    Tree(node.Require("effect")), cast, hasContinuation: true);
            }
            long count = ForEachCount(node, cast);
            return count > 0 && DelayedNeedsContinuationAddress(
                Tree(node.Require("effect")), cast,
                hasContinuation || count > 1);
        }
        if (node.Kind == "eachTime")
        {
            var preceding = Tree(node.Require("effect"));
            if (preceding.Kind != "discardTop"
                || preceding.Field("player") is not null
                || !string.Equals(
                    Word(preceding.Require("from")), "encounterDeck",
                    StringComparison.Ordinal))
            {
                return true;
            }

            var requested = preceding.Require("count");
            if (AmountMayChange(requested))
            {
                return true;
            }
            long count = Amount(requested, cast);
            if (count < 0)
            {
                throw new AbilityException("'eachTime' needs a non-negative discard count");
            }
            if (count == 0)
            {
                return false;
            }
            return DelayedNeedsContinuationAddress(
                Tree(node.Require("then")), cast,
                hasContinuation || count > 1);
        }
        return ContinuationChildren(node).Any(child =>
            DelayedNeedsContinuationAddress(child, cast, hasContinuation));
    }

    private static void ContinueEachTime(
        AbilityNode node, Cast cast, long from, long count)
    {
        bool outerContinuation = cast.HasContinuation;
        for (long iteration = from; iteration < count; iteration++)
        {
            var discarded = EncounterDeck.DiscardTop(
                cast.World, 1, cast.Trigger, cast.Events).SingleOrDefault();
            if (discarded is null)
            {
                break;
            }
            cast.Discarded.Add(discarded);
            cast.BindAlteration(discarded);

            if (!Test(Tree(node.Require("when")), cast))
            {
                continue;
            }

            cast.SetContinuation(outerContinuation || iteration < count - 1);
            RunChild(
                Tree(node.Require("then")),
                $"eachTime:{iteration}:{count}:{discarded.ObjectId}",
                cast);
            if (cast.Suspended)
            {
                return;
            }
        }
        cast.SetContinuation(outerContinuation);
    }

    /// <summary>Whether a repeated effect names a game element it can affect.</summary>
    /// <remarks>
    /// The rulebook decides that a no-choice repetition keeps one target, but
    /// it does not supply a binding for the DSL. Direct damage and threat
    /// removal capture their single target by resolving once above. Other
    /// targeted shapes fail closed until their target can be persisted instead
    /// of running a fresh selector against a changed board.
    /// </remarks>
    private static bool ContainsForEachTarget(AbilityNode node) =>
        node.Kind is "removeFromGame" or "exhaust" or "ready" or "reveal"
            or "returnToHand" or "returnOwnedToHand" or "soakDamage"
            or "addToHand" or "giveStatus" or "attachTo" or "grantUntil"
            or "discard" or "heal" or "placeCounters" or "shuffleInto" or "search"
            or "indirectDamage" or "dealDamage" or "moveDamage"
            or "dealAttackDamage" or "moveAttackDamage" or "placeThreat"
            or "removeThreat" or "replaceThreatWithDamage" or "enemyAttacks"
            or "enemySchemes" or "putIntoPlay" or "placeAtRandom" or "thwartSchemes"
            or "thwartDifferentSchemes" or "legalPractice"
        || ContinuationChildren(node).Any(ContainsForEachTarget);

    private static void RunChild(AbilityNode node, string frame, Cast cast)
    {
        cast.AbilityPath.Add(frame);
        try
        {
            Run(node, cast);
        }
        finally
        {
            cast.AbilityPath.RemoveAt(cast.AbilityPath.Count - 1);
        }
    }

    private static int AbilityOrdinal(AbilityNode node, Cast cast)
    {
        if (cast.AbilityOrdinal >= 0)
        {
            return cast.AbilityOrdinal;
        }

        var runner = (AbilityRunner)cast.Abilities;
        var written = runner.AbilitiesOn(cast.Source, cast.AbilityFace)
            .Where(ability => cast.Tier is null || ability.Trigger.Timing == cast.Tier)
            .ToList();
        var matches = written
            .Select((ability, ordinal) => (Node: TryNodeAtPath(
                ability.Effect, cast.AbilityPath), ordinal))
            .Where(candidate => candidate.Node == node)
            .Select(candidate => candidate.ordinal)
            .ToList();
        return matches.Count == 1
            ? matches[0]
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot identify the exact ability that suspended");
    }

    private IEnumerable<CardAbility> AbilitiesOn(Card source, string? face) =>
        string.IsNullOrEmpty(face) ? On(source) : book.On(face);

    private void TrackResolution(Cast cast, CardAbility ability)
    {
        var sameTier = AbilitiesOn(cast.Source, cast.AbilityFace)
            .Where(candidate => candidate.Trigger.Timing == ability.Trigger.Timing)
            .ToList();
        int ordinal = sameTier.FindIndex(candidate => ReferenceEquals(candidate, ability));
        if (ordinal < 0)
        {
            ordinal = sameTier.IndexOf(ability);
        }
        if (ordinal < 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot identify the ability whose resolution is tracked");
        }
        cast.RestoreAbility(ordinal, []);
        cast.TrackResolution(ordinal);
    }

    private CardAbility AbilityAt(
        Card source, AbilityType? tier, int ordinal, string? face = null) =>
        AbilitiesOn(source, face)
            .Where(ability => tier is null || ability.Trigger.Timing == tier)
            .ElementAtOrDefault(ordinal)
        ?? throw new RulesNotImplementedException(
            $"'{source.FaceId}' has no '{tier}' ability {ordinal}");

    private static void RestorePersisted(Cast cast, PhaseStep? continuation)
    {
        if (continuation is not { } step)
        {
            return;
        }
        RestorePersisted(cast, step.Discarded, step.AbilityResults);
        cast.AbilityActor = step.AbilityActor >= 0
            ? cast.World.Cards[step.AbilityActor]
            : null;
    }

    private static void RestorePersisted(
        Cast cast, IReadOnlyList<int>? discarded,
        IReadOnlyDictionary<string, long>? results)
    {
        cast.Discarded.Clear();
        if (discarded is not null)
        {
            cast.Discarded.AddRange(discarded.Select(id => cast.World.Cards[id]));
        }
        foreach (var (name, value) in results
            ?? new Dictionary<string, long>(StringComparer.Ordinal))
        {
            if (name == PersistedChosen)
            {
                RestorePersistedChosen(cast, value, overwrite: false);
                continue;
            }
            if (cast.RestoreCrisisIgnoringThwart(name, value))
            {
                continue;
            }
            cast.Results[name] = value;
        }
    }

    private static void RestorePersistedChosen(
        Cast cast, IReadOnlyDictionary<string, long>? results, bool overwrite)
    {
        if (results?.TryGetValue(PersistedChosen, out long chosen) == true)
        {
            RestorePersistedChosen(cast, chosen, overwrite);
        }
    }

    private static void RestorePersistedChosen(
        Cast cast, long chosen, bool overwrite)
    {
        if (chosen < 0 || chosen >= cast.World.Cards.Count)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' has invalid persisted chosen-card metadata");
        }
        var card = cast.World.Cards[(int)chosen];
        cast.RestorePlayerSelection(card);
        if (overwrite || cast.Chosen is null)
        {
            cast.Choose(card);
        }
    }

    private static void RestorePathBindings(Cast cast, IReadOnlyList<string> path)
    {
        var frame = path.LastOrDefault(candidate =>
            candidate.StartsWith("eachTime:", StringComparison.Ordinal));
        if (frame is null)
        {
            return;
        }
        var parts = frame.Split(':');
        cast.BindAlteration(cast.World.Cards[ParseEachTimeCard(parts, frame)]);
    }

    private static AbilityNode? TryNodeAtPath(
        AbilityNode root, IReadOnlyList<string> path)
    {
        try
        {
            return NodeAtPath(root, path);
        }
        catch (Exception error) when (error is AbilityException
            or ArgumentOutOfRangeException or InvalidOperationException
            or RulesNotImplementedException)
        {
            return null;
        }
    }

    private static PhaseStep? ContinuationStep(
        World world, Card source, int stoppedAt, AbilityType? tier)
    {
        bool Matches(PhaseStep step) => step.What == Steps.ChooseOption
            && step.Subject == source.ObjectId
            && step.Index == stoppedAt
            && step.Tier == tier;
        if (world.Agenda.Current is { } current && Matches(current))
        {
            return current;
        }
        for (int index = world.Agenda.Outstanding.Count - 1; index >= 0; index--)
        {
            if (Matches(world.Agenda.Outstanding[index]))
            {
                return world.Agenda.Outstanding[index];
            }
        }
        return null;
    }

    private static AbilityNode NodeAtPath(
        AbilityNode root, IReadOnlyList<string> path)
    {
        try
        {
            return NodeAtPathCore(root, path);
        }
        catch (Exception error) when (error is AbilityException
            or ArgumentOutOfRangeException or IndexOutOfRangeException
            or InvalidOperationException or FormatException)
        {
            throw new RulesNotImplementedException(
                $"ability continuation path '{string.Join("/", path)}' is invalid");
        }
    }

    private static AbilityNode NodeAtPathCore(
        AbilityNode root, IReadOnlyList<string> path, int offset = 0)
    {
        var node = root;
        for (int index = offset; index < path.Count; index++)
        {
            var parts = path[index].Split(':');
            node = parts[0] switch
            {
                "seq" => Nodes(node.Argument).ElementAt(ParseIndex(parts, path[index])),
                "if" => Tree(node.Require(parts[1])),
                "then" or "otherwise" => Tree(node.Require(parts[1])),
                "defense" or "eachPlayer" or "forEach" =>
                    Tree(node.Require("effect")),
                "eachTime" => Tree(node.Require("then")),
                "choice" when parts[1] == "option" =>
                    Nodes(node.Require("options")).ElementAt(ParseIndex(parts, path[index], 2)),
                "choice" when parts[1] == "effect" => Tree(node.Require("effect")),
                "choice" when parts[1] == "otherwise" => Tree(node.Require("otherwise")),
                "and" => Nodes(node.Argument).ElementAt(ParseIndex(parts, path[index])),
                _ => throw new RulesNotImplementedException(
                    $"ability continuation frame '{path[index]}' is not implemented"),
            };
        }
        return node;
    }

    private static void ResumeAfter(
        AbilityNode node, IReadOnlyList<string> path, Cast cast, int depth = 0,
        int stopBefore = -1)
    {
        try
        {
            ResumeAfterCore(node, path, cast, depth, stopBefore);
        }
        catch (Exception error) when (error is AbilityException
            or ArgumentOutOfRangeException or IndexOutOfRangeException
            or InvalidOperationException or FormatException)
        {
            throw new RulesNotImplementedException(
                $"ability continuation path '{string.Join("/", path)}' is invalid");
        }
    }

    private static void ResumeAfterCore(
        AbilityNode node, IReadOnlyList<string> path, Cast cast, int depth = 0,
        int stopBefore = -1)
    {
        if (depth >= path.Count)
        {
            return;
        }

        string frame = path[depth];
        var parts = frame.Split(':');
        if (parts[0] == "eachTime")
        {
            cast.BindAlteration(cast.World.Cards[ParseEachTimeCard(parts, frame)]);
        }
        AbilityNode child = parts[0] switch
        {
            "seq" => Nodes(node.Argument).ElementAt(ParseIndex(parts, frame)),
            "if" => Tree(node.Require(parts[1])),
            "then" or "otherwise" => Tree(node.Require(parts[1])),
            "defense" or "eachPlayer" or "forEach" =>
                Tree(node.Require("effect")),
            "eachTime" => Tree(node.Require("then")),
            "choice" when parts[1] == "option" =>
                Nodes(node.Require("options")).ElementAt(ParseIndex(parts, frame, 2)),
            "choice" when parts[1] == "effect" => Tree(node.Require("effect")),
            "choice" when parts[1] == "otherwise" => Tree(node.Require("otherwise")),
            "and" => Nodes(node.Argument).ElementAt(ParseIndex(parts, frame)),
            _ => throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' is not implemented"),
        };

        bool inheritedContinuation = cast.HasContinuation;
        cast.SetContinuation(
            inheritedContinuation || HasRemainingAtFrame(node, parts, frame));
        ResumeAfterCore(child, path, cast, depth + 1, stopBefore);
        if (cast.Suspended || depth <= stopBefore)
        {
            return;
        }

        cast.SetContinuation(inheritedContinuation);
        cast.SetAbilityPath(path.Take(depth));
        switch (parts[0])
        {
            case "seq":
                var steps = Nodes(node.Argument).ToList();
                bool outerContinuation = cast.HasContinuation;
                for (int index = ParseIndex(parts, frame) + 1; index < steps.Count; index++)
                {
                    cast.At(index);
                    cast.SetContinuation(outerContinuation || index < steps.Count - 1);
                    RunChild(steps[index], $"seq:{index}", cast);
                    if (cast.Suspended)
                    {
                        return;
                    }
                }
                cast.SetContinuation(outerContinuation);
                break;

            case "then" when parts[1] == "effect":
            case "otherwise" when parts[1] == "effect":
                if (parts.Length < 3
                    || !Enum.TryParse(parts[2], out ResolutionOutcome outcome))
                {
                    throw new RulesNotImplementedException(
                        $"ability continuation frame '{frame}' has no resolution outcome");
                }
                var required = parts[0] == "then"
                    ? ResolutionOutcome.Full
                    : ResolutionOutcome.None;
                if (outcome == required)
                {
                    RunChild(Tree(node.Require(parts[0])), $"{parts[0]}:{parts[0]}", cast);
                }
                break;

            case "and":
                var effects = Nodes(node.Argument).ToList();
                var remaining = ValidRemaining(node, parts, frame);
                var completed = Completed(parts, frame);
                completed.Add(ParseIndex(parts, frame));
                bool outerAndContinuation = cast.HasContinuation;
                for (int position = 0; position < remaining.Count; position++)
                {
                    int index = remaining[position];
                    string after = string.Join(',', remaining.Skip(position + 1));
                    string before = string.Join(',', completed.Concat(remaining.Take(position)));
                    cast.SetContinuation(
                        outerAndContinuation || position < remaining.Count - 1);
                    RunChild(effects[index], $"and:{index}:{after}:{before}", cast);
                    if (cast.Suspended)
                    {
                        return;
                    }
                }
                cast.SetContinuation(outerAndContinuation);
                break;

            case "eachPlayer":
                if (cast.AbilityPlayer >= 0)
                {
                    cast.RestorePlayer(cast.AbilityPlayer);
                }
                break;

            case "forEach":
                long count = ParseForEachCount(parts, frame);
                long completedIteration = ParseIndex(parts, frame);
                var repeated = Tree(node.Require("effect"));
                bool outerForEachContinuation = cast.HasContinuation;
                for (long iteration = completedIteration + 1; iteration < count; iteration++)
                {
                    cast.SetContinuation(
                        outerForEachContinuation || iteration < count - 1);
                    RunChild(repeated, $"forEach:{iteration}:{count}", cast);
                    if (cast.Suspended)
                    {
                        return;
                    }
                }
                cast.SetContinuation(outerForEachContinuation);
                break;

            case "eachTime":
                ContinueEachTime(
                    node, cast,
                    from: ParseIndex(parts, frame) + 1,
                    count: ParseForEachCount(parts, frame));
                break;
        }
    }

    private static bool HasRemainingAtFrame(
        AbilityNode node, string[] parts, string frame)
    {
        return parts[0] switch
        {
            "seq" => ParseIndex(parts, frame) < Nodes(node.Argument).Count() - 1,
            "and" => ValidRemaining(node, parts, frame).Count > 0,
            "forEach" => ParseIndex(parts, frame) + 1
                < ParseForEachCount(parts, frame),
            "eachTime" => ParseIndex(parts, frame) + 1
                < ParseForEachCount(parts, frame),
            "then" when parts[1] == "effect" => DependentContinues(parts, frame, true),
            "otherwise" when parts[1] == "effect" =>
                DependentContinues(parts, frame, false),
            _ => false,
        };
    }

    private static long ParseForEachCount(string[] parts, string frame)
    {
        if (parts.Length < 3
            || !long.TryParse(
                parts[2], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out long count)
            || count < 0)
        {
            throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has no iteration count");
        }
        return count;
    }

    private static int ParseEachTimeCard(string[] parts, string frame)
    {
        if (parts.Length < 4
            || !int.TryParse(
                parts[3], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out int card)
            || card < 0)
        {
            throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has no bound card");
        }
        return card;
    }

    private static bool DependentContinues(string[] parts, string frame, bool onFull)
    {
        if (parts.Length < 3
            || !Enum.TryParse(parts[2], out ResolutionOutcome outcome))
        {
            throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has no resolution outcome");
        }
        return outcome == (onFull ? ResolutionOutcome.Full : ResolutionOutcome.None);
    }

    private static List<int> ValidRemaining(
        AbilityNode node, string[] parts, string frame)
    {
        var effects = Nodes(node.Argument).ToList();
        var remaining = Remaining(parts, frame);
        var completed = Completed(parts, frame);
        var completeOrder = completed
            .Append(ParseIndex(parts, frame))
            .Concat(remaining)
            .ToList();
        if (completeOrder.Count != effects.Count
            || completeOrder.Distinct().Count() != effects.Count
            || completeOrder.Any(index => index < 0 || index >= effects.Count))
        {
            throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has an invalid remaining order");
        }
        return remaining;
    }

    private static List<int> Remaining(string[] parts, string frame)
        => OrderPart(parts, 2, frame);

    private static List<int> Completed(string[] parts, string frame)
    {
        if (parts.Length < 4)
        {
            throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has no completed order");
        }
        return OrderPart(parts, 3, frame);
    }

    private static List<int> OrderPart(string[] parts, int position, string frame)
    {
        if (parts.Length <= position || string.IsNullOrEmpty(parts[position]))
        {
            return [];
        }
        try
        {
            return parts[position].Split(',').Select(value => int.Parse(
                value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        }
        catch (Exception error) when (error is FormatException or OverflowException)
        {
            throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has an invalid remaining order");
        }
    }

    private static int ParseIndex(string[] parts, string frame, int position = 1) =>
        parts.Length > position
        && int.TryParse(
            parts[position], System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out int value)
            ? value
            : throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has no valid index");

    private static void Choose(AbilityNode node, Cast cast)
    {
        if (node.Kind == "choose" && Nodes(node.Require("options")).Count() < 2)
        {
            throw new AbilityException(
                $"'{cast.Source.FaceId}' offers a choice of one, which is not a choice");
        }

        if (node.Kind == "chooseCard"
            && LegalCardChoicesForContinuation(node, cast).Count == 0)
        {
            // A mandatory ability with no valid chosen target cannot initiate;
            // reaching it directly from reveal or boost resolution is a no-op.
            // Optional and action paths reject it during their preflight.
            return;
        }

        SuspendForChoice(node, cast);
    }

    /// <summary>Suspend an ability for one persisted player choice.</summary>
    private static void SuspendForChoice(AbilityNode node, Cast cast)
    {
        // `Index` remains the legacy top-level resume point. New continuations
        // use AbilityOrdinal and AbilityPath below.
        int abilityOrdinal = AbilityOrdinal(node, cast);
        var abilityResults = ContinuationResults(cast, abilityOrdinal);
        var continuation = new PhaseStep(
            Steps.ChooseOption,
            cast.World.Agenda.Current?.Round ?? 0,
            2,
            Index: cast.Position + 1,
            Subject: cast.Source.ObjectId,
            Seat: cast.Player,

            // Which ability stopped. A card can have a choice in two of them,
            // and the card and the position do not say which -- see `Choice`.
            Tier: cast.Tier,
            FinalStep: cast.FinalStep,
            FinalPlayer: cast.FinalPlayer,
            EachPlayerFrame: cast.EachPlayerFrame,
            Trigger: cast.Trigger,
            SurgeGained: cast.GainedKeywords.Contains("surge"),
            Discarded: [.. cast.Discarded.Select(card => card.ObjectId)],
            AbilityOrdinal: abilityOrdinal,
            AbilityPath: [.. cast.AbilityPath],
            AbilityResults: abilityResults,
            AbilityOccurrence: cast.Occurrence,
            AbilityFace: cast.AbilityFace,
            AbilityPlayer: cast.AbilityPlayer,
            AbilityActor: cast.AbilityActor?.ObjectId ?? -1,
            AbilityHasContinuation: cast.HasContinuation);
        if (cast.Occurrence.Is(Steps.TurnAction))
        {
            cast.World.Agenda.ThenContinuation(continuation, cast.Occurrence);
        }
        else
        {
            cast.World.Agenda.Then(continuation);
        }

        cast.Suspend();
    }

    /// <summary>"… heals N damage" — <c>rr:heal</c>.</summary>
    /// <remarks>
    /// <para>
    /// What it records is the point. <c>rr:heal</c> heals up to the amount, and
    /// a character at full health or damaged by less heals less than it was
    /// told to — so <c>result.healed</c> is what actually moved, and a card
    /// reading "if no damage was healed this way" reads that rather than
    /// checking the character's health first. The check <i>before</i> is
    /// silently wrong: it reads a number the heal may never reach.
    /// </para>
    /// <para>
    /// A target that is not on the board heals nothing rather than throwing.
    /// "Rhino heals 4 damage. If no damage was healed this way, this card gains
    /// surge" is a sentence with an answer for the absent villain, and it is
    /// the surge.
    /// </para>
    /// </remarks>
    private static void Heal(AbilityNode node, Cast cast)
    {
        long healed = Find(node.Require("card"), cast) is { } target
            ? Damage.Heal(
                cast.World, cast.World.Facts, target, Amount(node.Require("amount"), cast),
                cast.Trigger, "Heal", cast.Events)
            : 0;

        cast.Results["healed"] = healed;
    }

    /// <summary>
    /// "Assign N damage among …" — <c>rr:indirect-damage</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>.1</c>: "indirect damage dealt to a player can be divided as that
    /// player chooses among characters under their control." <c>.2</c> is the
    /// group form, "among friendly characters in play", which is what "assign X
    /// damage among heroes and allies" means.
    /// </para>
    /// <para>
    /// <b>Only asked when there is something to ask.</b> A player with no ally
    /// has one character, so every point goes to their identity and there is no
    /// division to choose — which is most of the 101 cards in the pool that
    /// deal indirect damage. It suspends only when the eligible characters can
    /// hold the damage more than one way.
    /// </para>
    /// <para>
    /// <c>.3.1</c> caps each character at its remaining hit points: "a
    /// character cannot be assigned more indirect damage than would cause it to
    /// be defeated", assessed "without accounting for interactions with other
    /// abilities". <c>.3.2</c> keeps a tough character eligible up to that same
    /// cap even though the tough card will prevent all of it, and <c>.3</c>
    /// assigns everything before resolving any of it.
    /// </para>
    /// </remarks>
    private static void Indirect(AbilityNode node, Cast cast)
    {
        long amount = Amount(node.Require("amount"), cast);
        var eligible = Assignable(node.Require("among"), cast);

        if (amount <= 0 || eligible.Count == 0)
        {
            return;
        }

        if (eligible.Count == 1)
        {
            // No division to choose. `.3.1`'s cap still applies -- a character
            // cannot be assigned more than would defeat it -- so what is over
            // the cap is simply not assigned.
            Assign(cast, [eligible[0]], amount);
            return;
        }

        SuspendForChoice(node, cast);
    }

    /// <summary>The characters indirect damage may be assigned to.</summary>
    /// <remarks>
    /// <c>rr:indirect-damage.4</c>: "characters that cannot take damage cannot
    /// be assigned indirect damage", and <c>.3.1</c> makes a character with no
    /// hit points left ineligible for the same reason — there is no amount that
    /// would not defeat it.
    /// </remarks>
    private static List<Card> Assignable(AbilityValue among, Cast cast) =>
    [
        .. Every(among, cast).Where(card =>
            Room(cast, card) > 0
            && cast.Abilities.CanTakeDamage(cast.World, card, cast.Source)),
    ];

    private static IReadOnlyList<Card> DamageTargets(AbilityValue targets, Cast cast) =>
        [.. Every(targets, cast).Where(target =>
            cast.Abilities.CanTakeDamage(cast.World, target, cast.Source))];

    /// <summary>How much indirect damage one character may be assigned.</summary>
    private static long Room(Cast cast, Card card) =>
        Damage.Health(cast.World, cast.World.Facts, card) - card.Damage;

    /// <summary>Assigns the damage, then resolves it — <c>rr:indirect-damage.3</c>.</summary>
    /// <remarks>
    /// "All indirect damage from a single source is <b>first assigned and then
    /// resolved simultaneously</b>." So the whole assignment is worked out
    /// before any of it is dealt, which is what stops the first point defeating
    /// a character and making the rest illegal.
    /// </remarks>
    private static void Assign(Cast cast, IReadOnlyList<Card> among, long amount)
    {
        var assigned = new Dictionary<int, long>();
        long left = amount;

        foreach (var card in among)
        {
            if (left <= 0)
            {
                break;
            }

            long take = Math.Min(Room(cast, card), left);
            if (take <= 0)
            {
                continue;
            }

            assigned[card.ObjectId] = take;
            left -= take;
        }

        Resolve(cast, assigned);
    }

    /// <summary>Deals an assignment that is already worked out.</summary>
    /// <remarks>
    /// In object-id order, because <c>rr:indirect-damage.3</c> resolves it
    /// "simultaneously" and simultaneous still has to reach the event stream in
    /// some order — one the board cannot see and the wire can.
    /// </remarks>
    private static void Resolve(Cast cast, Dictionary<int, long> assigned)
    {
        foreach (var (card, damage) in assigned.OrderBy(each => each.Key))
        {
            Damage.Deal(
                cast.World, cast.World.Facts, cast.Source, cast.World.Cards[card], damage,
                cast.Trigger, "Indirect_Damage", cast.Events);
        }
    }

    /// <summary>"Deal N damage to …" — <c>rr:damage</c>.</summary>
    /// <remarks>
    /// Through <see cref="Damage.Deal"/> and not at the token, because damage
    /// is one rule however it arrived: <c>rr:tough.2</c> prevents all of it and
    /// discards a status card instead, and <c>rr:defeat</c> is the other half
    /// of the same moment. A card that wrote to <c>k_damage</c> would skip
    /// both and leave a defeated character standing.
    /// </remarks>
    private static void DealDamage(AbilityNode node, Cast cast, long multiplier = 1)
    {
        long amount = SaturatingMultiply(
            Amount(node.Require("amount"), cast), multiplier);
        amount = SaturatingSum(amount, [EventModifier(cast, "eventDamage")]);
        if (cast.Power == BasicPowers.AttackVerb)
        {
            amount = SaturatingSum(amount, [EventModifier(cast, "attackDamage")]);
        }
        string verb = node.Field("attack") is null ? "Deal_Damage" : "Attack";
        foreach (var target in Every(node.Require("cards"), cast))
        {
            long before = target.Damage;
            Damage.Deal(
                cast.World, cast.World.Facts, cast.Source, target, amount, cast.Trigger, verb,
                cast.Events);
            if (cast.Power == BasicPowers.AttackVerb && target.Damage > before)
            {
                cast.Occurrence.Also(Steps.DamageDealt);
            }
        }
    }

    private static void MoveDamage(AbilityNode node, Cast cast)
    {
        var from = Find(node.Require("from"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot find the character damage moves from");
        var to = Find(node.Require("to"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot find the enemy damage moves to");
        long amount = Math.Min(from.Damage, Amount(node.Require("amount"), cast));
        if (amount <= 0 || !cast.Abilities.CanTakeDamage(cast.World, to, cast.Source))
        {
            return;
        }

        Damage.Heal(
            cast.World, cast.World.Facts, from, amount,
            cast.Trigger, "Move_Damage", cast.Events);
        Damage.Deal(
            cast.World, cast.World.Facts, cast.Source, to, amount,
            cast.Trigger, "Attack", cast.Events);
    }

    /// <summary>Damage from an attack event performed by the resolving identity.</summary>
    private static void DealAttackDamage(AbilityNode node, Cast cast)
    {
        var attacker = cast.PowerActor
            ?? cast.AbilityActor
            ?? cast.World.Seats[Resolver(cast)].IdentityCard;
        ContinuousEffect? temporaryOverkill = null;
        if (node.Field("overkill") is not null)
        {
            temporaryOverkill = new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: Keywords.Overkill,
                Amount: 1,
                Card: cast.Source.ObjectId,
                Affects: attacker.ObjectId,
                Lasts: new Duration(Uses: 1));
            cast.World.Effects.Register(temporaryOverkill);
        }

        var attackModifiers = EventModifierEffects(cast, "attackDamage");
        long amount = SaturatingSum(
            Amount(node.Require("amount"), cast),
            [EventModifier(cast, "eventDamage"),
             SaturatingSum(0, attackModifiers.Select(effect => effect.Amount))]);
        foreach (var target in DamageTargets(node.Require("cards"), cast))
        {
            var damaged = Damage.Attack(
                cast.World, cast.World.Facts, attacker, cast.Source, target,
                amount, cast.Trigger, "Attack", cast.Events,
                retaliate: false);
            cast.Attacked.Add(target);
            if (damaged.Characters.Count > 0)
            {
                cast.Occurrence.Also(Steps.DamageDealt);
            }
        }
        // Inside an attack wrapper, every damage instance belongs to the same
        // attack and the wrapper consumes these after its whole effect. A
        // direct dealAttackDamage node is itself the attack and consumes here.
        foreach (var modifier in cast.Power == BasicPowers.AttackVerb
                     ? []
                     : attackModifiers)
        {
            cast.World.Effects.Use(modifier);
        }

        if (temporaryOverkill is not null)
        {
            cast.World.Effects.Use(temporaryOverkill);
        }
    }

    private static void MoveAttackDamage(AbilityNode node, Cast cast)
    {
        var from = Find(node.Require("from"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot find the character damage moves from");
        var to = Find(node.Require("to"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot find the enemy damage moves to");
        cast.Attacked.Add(to);
        long amount = Math.Min(from.Damage, Amount(node.Require("amount"), cast));
        if (amount <= 0 || !cast.Abilities.CanTakeDamage(cast.World, to, cast.Source))
        {
            return;
        }

        Damage.Heal(
            cast.World, cast.World.Facts, from, amount,
            cast.Trigger, "Move_Damage", cast.Events);
        var damaged = Damage.Attack(
            cast.World,
            cast.World.Facts,
            cast.PowerActor
                ?? cast.AbilityActor
                ?? cast.World.Seats[Resolver(cast)].IdentityCard,
            cast.Source,
            to,
            amount,
            cast.Trigger,
            BasicPowers.AttackVerb,
            cast.Events,
            retaliate: false);
        if (damaged.Characters.Count > 0)
        {
            cast.Occurrence.Also(Steps.DamageDealt);
        }
    }

    private void SchedulePower(AbilityNode node, Cast cast, string power)
    {
        var target = Find(node.Require("target"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot find the target of its {power}");
        SchedulePower(node, cast, power, target, [target], -1);
    }

    private void SchedulePower(
        AbilityNode node, Cast cast, string power, Card target,
        IReadOnlyList<Card> targets, long powerAmount)
    {
        var effect = Tree(node.Require("effect"));
        var continuationChosen = cast.PlayerSelection ?? cast.Chosen;
        cast.Choose(target);
        if (SuspendsPowerEffect(
            effect, cast, bindingMayChange: powerAmount >= 0))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside a {power.ToLowerInvariant()}, "
                + "which is not implemented");
        }

        var abilities = AbilitiesOn(cast.Source, cast.AbilityFace).ToList();
        var addresses = abilities
            .Select((ability, index) => (Ability: ability, Index: index))
            .Where(candidate => cast.Tier is null
                || candidate.Ability.Trigger.Timing == cast.Tier)
            .SelectMany(candidate => PowerNodes(candidate.Ability.Effect, power)
                .Select((wrapper, ordinal) =>
                    (candidate.Index, Ordinal: ordinal, Wrapper: wrapper)))
            .Where(candidate => candidate.Wrapper == node)
            .ToList();
        if (addresses.Count != 1)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' {power.ToLowerInvariant()} has {addresses.Count} "
                + "reconstructable authored locations");
        }

        var address = addresses[0];
        int resumeFrom = cast.HasContinuation ? cast.Position + 1 : -1;
        IReadOnlyList<string> abilityPath = [.. cast.AbilityPath];
        var abilityResults = ContinuationResults(cast, abilities[address.Index]);
        if (continuationChosen is null)
        {
            abilityResults.Remove(PersistedChosen);
        }
        else
        {
            abilityResults[PersistedChosen] = continuationChosen.ObjectId;
        }
        var discarded = cast.Discarded.Select(card => card.ObjectId).ToList();
        bool automaticThwartTarget = node.Field("automaticTarget") is not null
            || cast.CrisisIgnoringThwartWasValidated(node, address.Ordinal);
        bool scheduled = power == BasicPowers.AttackVerb
            ? BasicPowers.CardAttack(
                cast.World, cast.World.Facts, Resolver(cast), cast.Source, target, powerAmount,
                cast.Trigger, cast.Events, abilityIndex: address.Index,
                powerOrdinal: address.Ordinal, resumeFrom: resumeFrom,
                finalStep: cast.FinalStep,
                targets: [.. targets.Select(card => card.ObjectId)], nested: true,
                surgeGained: cast.GainedKeywords.Contains("surge"),
                abilityPath: abilityPath, abilityFace: cast.AbilityFace,
                abilityResults: abilityResults, abilityOccurrence: cast.Occurrence,
                discarded: discarded, eachPlayerFrame: cast.EachPlayerFrame,
                finalPlayer: cast.FinalPlayer, abilityPlayer: cast.AbilityPlayer,
                abilityHasContinuation: cast.HasContinuation,
                performer: cast.AbilityActor)
            : BasicPowers.CardThwart(
                cast.World, cast.World.Facts, Resolver(cast), cast.Source, target, powerAmount,
                cast.Trigger, cast.Events, abilityIndex: address.Index,
                powerOrdinal: address.Ordinal, resumeFrom: resumeFrom,
                finalStep: cast.FinalStep,
                targets: [.. targets.Select(card => card.ObjectId)],
                imminentThreat: cast.Occurrence.Threat,
                automaticTarget: automaticThwartTarget,
                nested: true,
                surgeGained: cast.GainedKeywords.Contains("surge"),
                abilityPath: abilityPath, abilityFace: cast.AbilityFace,
                abilityResults: abilityResults, abilityOccurrence: cast.Occurrence,
                discarded: discarded, eachPlayerFrame: cast.EachPlayerFrame,
                finalPlayer: cast.FinalPlayer, abilityPlayer: cast.AbilityPlayer,
                abilityHasContinuation: cast.HasContinuation,
                performer: cast.AbilityActor);
        if (!scheduled)
        {
            return;
        }

        cast.Suspend();
    }

    /// <summary>"Place N threat on …" — <c>rr:threat</c>.</summary>
    /// <remarks>
    /// Through <see cref="Threat.Place"/>, which checks
    /// <c>rr:main-scheme-main-scheme-deck.2</c> afterwards: threat that reaches
    /// a main scheme's target completes it whatever put it there, and a card
    /// placing threat is one of the things that can.
    /// </remarks>
    private static void PlaceThreat(AbilityNode node, Cast cast)
    {
        // "On each side scheme" and "here" are the same node with a different
        // query: `Every` answers one card or many, so a card that names one
        // scheme and a card that names all of them read alike.
        var schemes = Every(node.Require("scheme"), cast);
        if (schemes.Count == 0)
        {
            // The ability has initiated, but its named game element can leave
            // before resolution. `rr:resolve-as-much-as-possible` resolves the
            // remaining effect with no target rather than recreating the card
            // or treating an absent target as an engine gap.
            return;
        }

        long amount = Amount(node.Require("amount"), cast);
        if (amount <= 0)
        {
            return;
        }

        if (cast.HasContinuation)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' places threat before its ability has finished; "
                + "the continuation must be preserved across the threat interrupt window");
        }

        Threat.Schedule(
            cast.World, schemes, cast.Source, amount,
            ThreatCause.CardAbility, cast.Trigger, cast.Player,
            cast.ResolutionAbility, cast.Occurrence);
        cast.Suspend();
    }

    private static void PreventThreat(AbilityNode node, Cast cast)
    {
        var placement = cast.ImminentThreat ?? cast.Occurrence.Threat
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would prevent threat that is not imminent");
        placement.Prevent(Amount(node.Argument, cast));
    }

    private static void ReplaceThreatWithDamage(AbilityNode node, Cast cast)
    {
        var placement = cast.Occurrence.Threat
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would replace threat that is not imminent");
        long damage = placement.Remaining;
        var target = Find(node.Require("card"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' replaces threat with damage to a card that is not there");
        placement.Replace();
        cast.ResolveEffect();
        Damage.Deal(
            cast.World, cast.World.Facts, cast.Source, target, damage,
            cast.Trigger, "Deal_Damage", cast.Events);
    }

    private static void RemoveThreat(AbilityNode node, Cast cast, long multiplier = 1)
    {
        var schemes = Every(node.Require("scheme"), cast);
        if (schemes.Count == 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would remove threat from a scheme that is not there");
        }

        foreach (var scheme in schemes)
        {
            // `rr:crisis-icon.1`: player cards cannot remove threat from the main
            // scheme while a crisis icon is in play. Encounter effects are not
            // player cards and remain able to do so.
            if (!IgnoresCrisis(node)
                && scheme.Area.Type == DeckType.MainSchemesArea
                && IsPlayerCard(cast)
                && MainScheme.Crisis(cast.World, cast.World.Facts))
            {
                continue;
            }

            Threat.Remove(
                cast.World,
                cast.World.Facts,
                cast.Abilities,
                scheme,
                SaturatingSum(
                    SaturatingMultiply(Amount(node.Require("amount"), cast), multiplier),
                    [EventModifier(cast, "eventThreatRemoval")]),
                cast.Trigger,
                "Remove_Threat",
                cast.Events,
                by: Resolver(cast),
                overridesCannotFrom: OverriddenThreatRemovalSource(node, cast));
        }
    }

    private static long EventModifier(Cast cast, string kind) =>
        SaturatingSum(0, EventModifierEffects(cast, kind).Select(effect => effect.Amount));

    private static IReadOnlyList<ContinuousEffect> EventModifierEffects(
        Cast cast, string kind)
    {
        if (cast.World.Facts.Kind(cast.Source.FaceId) != CardKind.Event)
        {
            return [];
        }

        return [.. cast.World.Effects.Active().Where(effect =>
            string.Equals(effect.Kind, kind, StringComparison.Ordinal)
            && effect.Affects == cast.Source.ObjectId)];
    }

    /// <summary>
    /// "Each player places a random card from their hand facedown here."
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Placed, not discarded.</b> The card is still a card and comes back —
    /// Highway Robbery's "When Defeated" returns each one to its owner's hand.
    /// So it goes onto the host as an attachment, which is what
    /// <c>rr:attachment</c> makes "here" mean, and it goes <b>facedown</b>:
    /// nobody may look at it while it is there.
    /// </para>
    /// <para>
    /// One draw from the game's single random stream per card taken, in player
    /// order, for the same reason <c>discardAtRandom</c> takes them that way —
    /// the order is what the stream sees.
    /// </para>
    /// </remarks>
    private static void PlaceAtRandom(AbilityNode node, Cast cast)
    {
        var host = Find(node.Require("on"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' places cards on a card that is not there");

        var onto = cast.World.AreaOf(
            DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);
        long count = Amount(node.Require("count"), cast);

        foreach (int seat in Seats(node.Require("player"), cast))
        {
            var hand = cast.World.Seats[seat].Hand;
            for (long placed = 0; placed < count && hand.Cards.Count > 0; placed++)
            {
                var card = cast.World.Random.Choice(hand.Cards);
                var from = card.Area;
                World.MoveToTop(card, onto);
                card.TurnFaceDown();

                cast.Events.Add(new CardsMoved(
                    Places.Reference(from), Places.Reference(onto),
                    [new Landing(card.ObjectId, onto.Cards.Count - 1)])
                {
                    Trigger = cast.Trigger, Verb = "Place",
                });
                cast.Events.Add(new CardAttached(card.ObjectId, host.ObjectId)
                {
                    Trigger = cast.Trigger, Verb = "Place",
                });
            }
        }
    }

    /// <summary>"Return each … to its owner's hand."</summary>
    /// <remarks>
    /// To <b>its owner's</b> hand and not the resolving player's: a card placed
    /// by each player comes back to each player. Ownership is the card's, which
    /// is why <c>Card.Owner</c> decides rather than whoever defeated the
    /// scheme.
    /// </remarks>
    private static void ReturnToHand(AbilityNode node, Cast cast)
    {
        foreach (var card in Every(node.Argument, cast))
        {
            var from = card.Area;
            var hand = cast.World.Seats[card.Owner].Hand;
            var constantsEnding = cast.World.Effects.PreflightConstantsEnding(card);
            using var departure = constantsEnding.Begin();
            if (DeckTypes.IsInPlay(from.Type))
            {
                Rules.Play.Discard.Attachments(
                    cast.World, card, cast.Trigger, cast.Events);
            }
            World.MoveToTop(card, hand);
            card.TurnFaceUp();

            cast.Events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(hand),
                [new Landing(card.ObjectId, hand.Cards.Count - 1)])
            {
                Trigger = cast.Trigger, Verb = "Return",
            });
            constantsEnding.Complete(cast.Trigger, cast.Events);
            cast.Events.Add(new CardDetached(card.ObjectId, from.Host)
            {
                Trigger = cast.Trigger, Verb = "Return",
            });
        }
    }

    /// <summary>
    /// "Discard N cards at random from … hand".
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The draw is a wire format.</b> One MT19937 stream runs the whole
    /// game, so how many numbers this takes and in what order decides every
    /// later shuffle and every later random card. <c>EngineRandom.Choice</c> is
    /// the ported primitive and is pinned against recorded RNG vectors; this
    /// takes one draw per card discarded, from the hand as it stands after the
    /// previous one.
    /// </para>
    /// <para>
    /// "From <b>each</b> player's hand" goes in player order —
    /// <c>rr:in-player-order</c> — because the order is what the stream sees.
    /// A player with an empty hand discards nothing and takes no draw.
    /// </para>
    /// <para>
    /// What it records is <c>result.resourceTypes</c>: how many <i>different</i>
    /// resource types went, which is what "for each different resource type
    /// discarded this way" counts. A card printing two of one letter is one
    /// type, and a card printing none is none.
    /// </para>
    /// </remarks>
    private static void DiscardAtRandom(AbilityNode node, Cast cast)
    {
        long count = Amount(node.Require("count"), cast);
        var types = new SortedSet<char>();
        long discarded = 0;

        foreach (int seat in Seats(node.Require("player"), cast))
        {
            var hand = cast.World.Seats[seat].Hand;
            for (long gone = 0; gone < count && hand.Cards.Count > 0; gone++)
            {
                var card = cast.World.Random.Choice(hand.Cards);
                types.UnionWith(Resources.GeneratedBy(card.FaceId, cast.World.Facts));
                Marvel.Rules.Play.Discard.Card(cast.World, card, cast.Trigger, cast.Events);
                discarded += 1;
            }
        }

        cast.Results["discarded"] = discarded;
        cast.Results["resourceTypes"] = types.Count;
    }

    /// <summary>Which seats a word names.</summary>
    /// <remarks>
    /// <c>rr:each-player.1</c> resolves "each player" in player order when the
    /// effect does not say otherwise, and <c>rr:player-elimination.6</c> is why
    /// that is <c>PlayerOrder</c>: "effects that refer to the players in the
    /// game ignore eliminated players".
    /// </remarks>
    private static IEnumerable<int> Seats(AbilityValue value, Cast cast) =>
        Word(value) switch
        {
            "each" => cast.World.PlayerOrder,
            _ => [Seat(value, cast)],
        };

    /// <summary>
    /// "Discard cards from the top of the encounter deck until a … is
    /// discarded" — <c>rr:discard.4</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "If multiple cards are discarded from a deck by a singular effect, place
    /// those cards in the appropriate discard pile <b>one at a time (without
    /// changing the order)</b>", and <c>.4.1</c> makes them simultaneous all
    /// the same. So this takes the top card each time rather than counting
    /// ahead — and through <see cref="EncounterDeck.TakeTop"/>, so a deck that
    /// empties mid-search reshuffles instead of ending the search.
    /// </para>
    /// <para>
    /// <b>Bounded, and the bound is a rule and not a fear.</b> A search for a
    /// card that is in neither the deck nor the discard pile would otherwise
    /// reshuffle for ever. The bound is how many cards there are, so a card
    /// that exists is always found and one that does not ends the search
    /// instead of the game.
    /// </para>
    /// </remarks>
    private static void DiscardUntil(AbilityNode node, Cast cast)
    {
        if (!string.Equals(
            Word(node.Require("from")), "encounterDeck", StringComparison.Ordinal))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' discards until a match from an unsupported area");
        }

        var wanted = Kind(Word(node.Require("kind")));
        string? trait = node.Field("trait") is { } requiredTrait
            ? Word(requiredTrait)
            : null;
        var found = EncounterDeck.DiscardUntil(
            cast.World, cast.World.Facts, wanted, cast.Trigger, cast.Events, trait);
        if (found is null)
        {
            return;
        }

        switch (Word(node.Require("then")))
        {
            case "reveal":
                RevealCard(found, cast);
                break;
            case "putIntoPlayFirstPlayer":
                PutIntoPlay(found, cast.World.FirstPlayer, cast);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has an unsupported discard-until result");
        }
    }

    private static void DiscardTop(AbilityNode node, Cast cast)
    {
        long count = Amount(node.Require("count"), cast);
        if (node.Field("player") is null
            && string.Equals(Word(node.Require("from")), "encounterDeck", StringComparison.Ordinal))
        {
            cast.Discarded.AddRange(EncounterDeck.DiscardTop(
                cast.World, count, cast.Trigger, cast.Events));
            return;
        }
        IEnumerable<Area> decks = node.Field("player") is { } players
            ? Seats(players, cast).Select(player => cast.World.Seats[player].Deck)
            : [Area(Word(node.Require("from")), cast)];
        foreach (var deck in decks)
        {
            for (long discarded = 0; discarded < count && deck.Cards.Count > 0; discarded++)
            {
                var card = deck.Cards[^1];
                Rules.Play.Discard.Card(cast.World, card, cast.Trigger, cast.Events);
                cast.Discarded.Add(card);
            }
        }
    }

    private static void RecoverDiscardedByResource(AbilityNode node, Cast cast)
    {
        string resource = Word(node.Argument);
        var hand = cast.World.Seats[cast.Player].Hand;
        foreach (var card in cast.Discarded.Where(card =>
            Resources.GeneratedBy(card.FaceId, cast.World.Facts).Contains(
                resource, StringComparison.Ordinal)).ToList())
        {
            var from = card.Area;
            World.MoveToTop(card, hand);
            cast.Events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(hand),
                [new Landing(card.ObjectId, hand.Cards.Count - 1)])
            {
                Trigger = cast.Trigger, Verb = "Add_To_Hand",
            });
        }
    }

    /// <summary>Which card type a word names.</summary>
    private static CardKind Kind(string named) => named switch
    {
        "sideScheme" => CardKind.EncounterSideScheme,
        "minion" => CardKind.Minion,
        "ally" => CardKind.Ally,
        "upgrade" => CardKind.Upgrade,
        "treachery" => CardKind.Treachery,
        _ => throw new RulesNotImplementedException(
            $"'{named}' is not a card type this engine can name"),
    };

    /// <summary>
    /// Which kind of thing a card means by how something was defeated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The word in the data names <b>the rule</b>; what comes back is the verb
    /// the event stream records damage under, which is what
    /// <c>Defeated.How</c> holds. Keeping the two apart is the point: a card
    /// says "consequential damage" because <c>rr:consequential-damage</c> is
    /// what it means, and the day the stream spells that verb differently the
    /// card does not have to change.
    /// </para>
    /// <para>
    /// <b>One word, because one card asks.</b> Gene Pool's "by anything other
    /// than consequential damage" is the whole of it. Anything else is refused
    /// by name rather than guessed at, so a card reaching for a cause this
    /// engine cannot tell apart says so instead of quietly answering false.
    /// </para>
    /// </remarks>
    private static string Cause(string named, Cast cast) => named switch
    {
        "consequentialDamage" => "Consequential_Damage",
        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' asks whether a card was defeated by '{named}', and this "
            + "engine can only tell consequential damage from everything else"),
    };

    /// <summary>
    /// "The villain attacks you", "the villain schemes" — an enemy activation
    /// a card asked for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Scheduled, not called.</b> <c>rr:attack-enemy-activation</c> is six
    /// steps and one of them asks a player who is defending, so an activation
    /// cannot resolve inside an ability that has to return. It goes on the
    /// agenda, and <c>Agenda.Then</c> puts it after the step that is running —
    /// which is what <c>rr:surge.2</c> wants anyway: finish resolving the card
    /// before what it caused happens.
    /// </para>
    /// <para>
    /// <b>Which activation is the card's to say.</b> <c>rr:activation.1</c>
    /// reads it off the player's form — attack in hero form, scheme in
    /// alter-ego form — but that rule is about the activation the villain phase
    /// schedules. A card that says "the villain attacks you" has already
    /// chosen, and reading the form here would make Assault do nothing to a
    /// hero who had flipped since the card was dealt.
    /// </para>
    /// <para>
    /// One step per enemy, in the order <see cref="Every"/> returns them.
    /// <c>rr:minion.3</c> makes that order the player's choice; it is taken
    /// here as the order the minions sit in the play area, deterministically
    /// and stated, exactly as the villain phase's own step 2 takes it.
    /// </para>
    /// </remarks>
    private static void Activate(AbilityNode node, Cast cast, string what)
    {
        // The round the activation belongs to is the round the card was
        // revealed in. Nothing else on the agenda can tell it.
        int round = cast.World.Agenda.Current?.Round ?? 0;

        // "Speed Demon attacks **that character**." Absent on every card that
        // simply says "the villain attacks you", which is the case
        // `rr:attack-enemy-activation.1.1` calls normal: "the attacked
        // character is the player's hero". An ability naming one instead is
        // the exception the same clause allows.
        AbilityValue? namedTarget = node.Field("against");
        bool engagedHero = namedTarget is AbilityValue.Word { Value: "engagedHero" };
        int against = namedTarget is { } named && !engagedHero
            ? Find(named, cast)?.ObjectId ?? -1
            : -1;

        // An ordinary "attacks you" activation belongs to the player
        // resolving the card. An attack against a named occurrence role gets
        // its attacked player from that role's snapshot instead. Speed Demon's
        // target can move or change control during this interrupt, but that
        // must not rewrite who was behind the character that attacked it.
        int seat = namedTarget switch
        {
            AbilityValue.Word { Value: "trigger.actor" } =>
                cast.Occurrence.ActorFacts?.Controller ?? World.Scenario,
            AbilityValue.Word { Value: "trigger.target" } =>
                cast.Occurrence.TargetFacts?.Controller ?? World.Scenario,
            null => cast.Player,
            _ => cast.Player,
        };

        if (seat < 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' initiates an enemy attack against a character "
                + "with no attacked player");
        }

        // "**(Resolve Speed Demon's attack first.)**" -- the card prints the
        // instruction, so the data records it. Absent, an activation a card
        // causes goes after whatever is happening, which is `rr:activation.8`:
        // "an activation initiated during another resolves after the current
        // activation has finished resolving." An interrupt that means to get
        // in front of the thing it answers has to say so, and Speed Demon's
        // parenthesis is the card saying it.
        bool first = node.Field("first") is AbilityValue.Word { Value: "true" };

        var activationIds = new List<int>();
        foreach (var enemy in Every(node.Require("enemies"), cast))
        {
            int activationSeat = engagedHero ? enemy.Area.PlayArea.Player : seat;
            if (activationSeat < 0
                || (engagedHero && !Forms.In(
                    cast.World,
                    cast.World.Seats[activationSeat],
                    cast.World.Facts,
                    Forms.Hero)))
            {
                continue;
            }

            var activation = new PhaseStep(
                what, round, 2, Index: activationSeat, Subject: enemy.ObjectId,
                Seat: activationSeat,
                Character: against);

            if (first)
            {
                activationIds.Add(cast.World.Agenda.NowActivation(activation));
            }
            else
            {
                activationIds.Add(cast.World.Agenda.ThenActivation(activation));
            }
        }

        if (activationIds.Count > 0)
        {
            int abilityOrdinal = AbilityOrdinal(node, cast);
            cast.World.Agenda.AfterActivations(activationIds, new PhaseStep(
                Steps.ResumeAbility,
                round,
                2,
                Index: cast.Position + 1,
                Subject: cast.Source.ObjectId,
                Seat: cast.Player,
                Tier: cast.Tier,
                FinalStep: cast.FinalStep,
                FinalPlayer: cast.FinalPlayer,
                EachPlayerFrame: cast.EachPlayerFrame,
                Trigger: cast.Trigger,
                SurgeGained: cast.GainedKeywords.Contains("surge"),
                Discarded: [.. cast.Discarded.Select(card => card.ObjectId)],
                AbilityOrdinal: abilityOrdinal,
                AbilityPath: [.. cast.AbilityPath],
                AbilityResults: ActivationResults(cast, abilityOrdinal),
                AbilityOccurrence: cast.Occurrence,
                AbilityFace: cast.AbilityFace,
                AbilityPlayer: cast.AbilityPlayer,
                AbilityActor: cast.AbilityActor?.ObjectId ?? -1,
                AbilityHasContinuation: cast.HasContinuation));
            cast.WaitFor(activationIds);
            cast.Suspend();
        }
    }

    private static Dictionary<string, long> ActivationResults(
        Cast cast, int abilityOrdinal)
    {
        var results = ContinuationResults(cast, abilityOrdinal);
        results.Remove("activationMade");
        results.Remove("activationDamage");
        results.Remove("activationThreat");
        return results;
    }

    /// <summary>Gameplay results plus engine-owned state needed after suspension.</summary>
    private static Dictionary<string, long> ContinuationResults(
        Cast cast, int abilityOrdinal)
    {
        var results = new Dictionary<string, long>(cast.Results, StringComparer.Ordinal);
        if (cast.Abilities is AbilityRunner runner)
        {
            cast.PersistCrisisIgnoringThwarts(
                runner.AbilityAt(
                    cast.Source, cast.Tier, abilityOrdinal, cast.AbilityFace),
                results);
        }
        PersistChosen(cast, results);
        return results;
    }

    private static Dictionary<string, long> ContinuationResults(
        Cast cast, CardAbility ability)
    {
        var results = new Dictionary<string, long>(cast.Results, StringComparer.Ordinal);
        cast.PersistCrisisIgnoringThwarts(ability, results);
        PersistChosen(cast, results);
        return results;
    }

    private const string PersistedChosen = "__continuation.chosen";

    private static void PersistChosen(Cast cast, Dictionary<string, long> results)
    {
        if ((cast.PlayerSelection ?? cast.Chosen) is { } chosen)
        {
            results[PersistedChosen] = chosen.ObjectId;
        }
    }

    /// <summary>Propagate one reveal-scoped Surge gain to work already suspended.</summary>
    private static void RememberGainedSurge(World world, int source)
    {
        // Choice and each-player continuations are saveable agenda data. An
        // earlier ability can already have scheduled one when a later sibling
        // ability gains Surge, so its original snapshot must be advanced too.
        // The rulebook determines the shared non-numeric keyword instance; the
        // propagation mechanism is the engine's choice.
        world.Agenda.MarkSurgeGained(source);
        if (world.CharacterAttack is { Source: var attackSource } attack
            && attackSource == source)
        {
            world.CharacterAttack = attack with { SurgeGained = true };
        }
        if (world.CharacterThwart is { Source: var thwartSource } thwart
            && thwartSource == source)
        {
            world.CharacterThwart = thwart with { SurgeGained = true };
        }
    }

    /// <summary>All allies in player discard piles, in player and pile order.</summary>
    private static IReadOnlyList<Card> AlliesInPlayerDiscards(World world) =>
    [
        .. world.PlayerOrder.SelectMany(player => world.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player).Cards)
            .Where(card => world.Facts.Kind(card.FaceId) == CardKind.Ally),
    ];

    private static bool CanMakeTheCall(Cast cast)
        => AlliesInPlayerDiscards(cast.World).Any(ally => Resources.Pays(
            string.Concat(MakeTheCallSources(
                    cast.World, cast.Player, cast.Source, ally)
                .Select(source => source.Generates)),
            Resources.Cost(ally.FaceId, cast.World.Facts, cast.World.Players) ?? 0,
            Resources.Required(cast.World, ally, cast.World.Facts)));

    /// <summary>The resources available while paying one Make the Call candidate's cost.</summary>
    private static IReadOnlyList<ResourceSource> MakeTheCallSources(
        World world, int player, Card source, Card ally) =>
    [
        .. CardPlay.Generators(world, world.Facts, world.Seats[player], payingFor: ally)
            .Where(generator => generator.Effect != source.ObjectId),
    ];

    /// <summary>Every card a value names, which may be none.</summary>
    /// <remarks>
    /// A value that names one card answers with that one, so a card reading
    /// "the villain attacks you" and one reading "each minion engaged with you
    /// attacks you" are the same node with a different argument.
    /// </remarks>
    private static IReadOnlyList<Card> Every(AbilityValue value, Cast cast)
    {
        if (value is AbilityValue.Map && Tree(value) is { Kind: "titled" } titled)
        {
            return ReferencedByTitle(Word(titled.Argument), cast);
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } query
            && query.Argument is AbilityValue.Word { Value: "minionsEngagedWithYou" })
        {
            // `rr:engage.1` -- "when a minion engages a player, it is placed in
            // that player's play area". Engagement *is* which area the minion
            // sits in, so this is a read of the board and not of a flag; and
            // "you" is the player resolving the card, so a minion engaged with
            // somebody else is not in this list however close it is on the
            // table.
            return [.. cast.World
                .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(cast.Player))
                .Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } eligibleIdentities
            && eligibleIdentities.Argument is AbilityValue.Word
                { Value: "identitiesWithinPerPlayerLimit" })
        {
            long maximum = cast.World.Facts.PrintedValue(
                cast.Source.FaceId, "MaxPerUnit", cast.World.Players);
            string title = cast.World.Facts.Title(cast.Source.FaceId);
            return
            [
                .. cast.World.PlayerOrder
                    .Where(player => maximum <= 0 || cast.World.Areas
                        .Where(area => area.PlayArea == PlayArea.Of(player))
                        .SelectMany(area => area.Cards)
                        .Count(card => DeckTypes.IsInPlay(card.Area.Type)
                            && string.Equals(
                                cast.World.Facts.Title(card.FaceId),
                                title,
                                StringComparison.Ordinal)) < maximum)
                    .Select(player => cast.World.Seats[player].IdentityCard),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } attached
            && attached.Argument is AbilityValue.Word { Value: "attachedToThis" })
        {
            // What is sitting on this card. `rr:attachment` puts an attachment
            // in an area hosted by the card it is attached to, so this is a
            // read of the board.
            return
            [
                .. cast.World.Areas
                    .Where(area => area.Host == cast.Source.ObjectId)
                    .SelectMany(area => area.Cards),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } friendly
            && friendly.Argument is AbilityValue.Word { Value: "heroesAndAllies" })
        {
            // `rr:indirect-damage.2`'s "friendly characters in play", which
            // `rr:friendly` makes every player's rather than one player's: "a
            // blanket term that refers to cards **the players** control".
            //
            // **Every identity, not only those in hero form.** "Heroes and
            // allies" is what the card says, but `rr:you-your.3` divides
            // indirect damage "among characters in play under their control",
            // and a player in alter-ego form is still a character with hit
            // points. A reading that skipped them would leave damage
            // unassignable at a table where everyone had flipped down.
            return
            [
                .. cast.World.PlayerOrder.Select(seat => cast.World.Seats[seat].IdentityCard),
                .. cast.World.Areas
                    .Where(area => area.Type == DeckType.AlliesArea)
                    .SelectMany(area => area.Cards),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } schemes
            && schemes.Argument is AbilityValue.Word { Value: "sideSchemes" })
        {
            // "Each side scheme", which reaches the players' as well as the
            // scenario's: `rr:player-side-scheme` calls them "the player card
            // equivalent of the side schemes found in the encounter deck" and
            // `.1` puts them in the same place, next to the main scheme.
            return [.. cast.World.AreaOf(DeckType.SideSchemesArea).Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } minions
            && minions.Argument is AbilityValue.Word { Value: "minions" })
        {
            // `rr:minion.3`: minions in play are engaged with players, so the
            // engaged-enemy areas across every play area are the complete set.
            return
            [
                .. cast.World.Areas
                    .Where(area => area.Type == DeckType.EngagedEnemiesArea)
                    .SelectMany(area => area.Cards)
                    .Where(card => cast.World.Facts.Kind(card.FaceId) == CardKind.Minion),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } enemies
            && enemies.Argument is AbilityValue.Word { Value: "enemies" })
        {
            return
            [
                .. cast.World.Areas
                    .Where(area => area.Type is DeckType.VillainArea
                        or DeckType.EngagedEnemiesArea)
                    .SelectMany(area => area.Cards)
                    .Where(card => cast.World.Facts.Kind(card.FaceId) is
                        CardKind.EncounterVillain or CardKind.Minion),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } attackable
            && attackable.Argument is AbilityValue.Word { Value: "attackableEnemies" })
        {
            return
            [
                .. BasicPowers.Attackable(cast.World, cast.World.Facts, Resolver(cast))
                    .Where(enemy => cast.World.Abilities.CanTakeDamage(
                        cast.World, enemy, cast.Source)),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } attackableMinions
            && attackableMinions.Argument is AbilityValue.Word { Value: "attackableMinions" })
        {
            return
            [
                .. BasicPowers.Attackable(cast.World, cast.World.Facts, Resolver(cast))
                    .Where(enemy => cast.World.Facts.Kind(enemy.FaceId) == CardKind.Minion)
                    .Where(enemy => cast.World.Abilities.CanTakeDamage(
                        cast.World, enemy, cast.Source)),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } allSchemes
            && allSchemes.Argument is AbilityValue.Word { Value: "schemes" })
        {
            return
            [
                .. cast.World.AreaOf(DeckType.MainSchemesArea).Cards,
                .. cast.World.AreaOf(DeckType.SideSchemesArea).Cards,
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } thwartable
            && thwartable.Argument is AbilityValue.Word { Value: "thwartableSchemes" })
        {
            return BasicPowers.Thwartable(cast.World, cast.World.Facts, Resolver(cast));
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } powerTargets
            && powerTargets.Argument is AbilityValue.Word { Value: "powerTargets" })
        {
            return cast.PowerTargets;
        }

        if (value is AbilityValue.Map
            && Tree(value) is { Kind: "withoutAnotherCopyAttached" } unoccupied)
        {
            string title = cast.World.Facts.Title(cast.Source.FaceId);
            return
            [
                .. Every(unoccupied.Argument, cast).Where(candidate =>
                    !cast.World.Areas
                        .Where(area => area.Host == candidate.ObjectId)
                        .SelectMany(area => area.Cards)
                        .Any(attached => attached.ObjectId != cast.Source.ObjectId
                            && string.Equals(
                                cast.World.Facts.Title(attached.FaceId), title,
                                StringComparison.Ordinal))),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } pile
            && pile.Argument is AbilityValue.Word { Value: "yourAsidePile" })
        {
            // "The rest of your set-aside nemesis encounter set" -- whatever is
            // still in the pile once the cards this ability took out of it have
            // gone. The obligation is not among them: setup shuffles it into
            // the encounter deck long before this resolves.
            return [.. cast.World.Seats[cast.Player].Nemesis.Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } yours
            && yours.Argument is AbilityValue.Word { Value: "upgradesAndSupportsYouControl" })
        {
            // "An upgrade or support **you control**." A player's upgrades and
            // supports sit in their own play area, so control is where the card
            // is -- the same reading `rr:engage.1` gets for a minion.
            return
            [
                .. Owned.SelectMany(where =>
                    cast.World.AreaOf(where, PlayArea.Of(cast.Player)).Cards),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "minBy" or "maxBy" } ranked)
        {
            return Ranked(ranked, cast);
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "cardsIn" } search)
        {
            return CardsIn(search, cast);
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "enemiesWithTrait" } trait)
        {
            // "Each **[[Criminal]]** enemy in play." A query with an argument
            // rather than one of the bare words, the way `titled` is -- the
            // trait is the whole of what varies, and dozens of cards in the
            // pool print this shape with a different one.
            //
            // **Spelled as the engine spells it** -- `CRIMINAL`, upper case,
            // spaces underscored -- for the reason `AbilityTrigger.Event` gives
            // for conditions: a translation table between the printed trait and
            // the stored one is a second vocabulary, and a second vocabulary
            // drifts. `ICardFacts.Traits` answers in that spelling.
            //
            // `rr:enemy`: "an enemy is a minion or villain", so this is the
            // villain's own area and every player's engaged minions --
            // `rr:minion.3` is why engagement is which play area a minion sits
            // in. Every player's, not the resolving one's: the card says "in
            // play" and says nothing about whose.
            string wanted = Word(trait.Argument);
            return
            [
                .. cast.World.Areas
                    .Where(area => area.Type is DeckType.VillainArea
                        or DeckType.EngagedEnemiesArea)
                    .SelectMany(area => area.Cards)
                    .Where(card => Rules.State.Traits.Of(cast.World, card, cast.World.Facts)
                        .Contains(wanted, StringComparer.Ordinal)),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } hand
            && hand.Argument is AbilityValue.Word { Value: "identitySpecificInYourHand" })
        {
            // "1 identity-specific card from your hand."
            // `rr:identity-specific-card` calls it a classification -- "cards
            // that belong to an identity's set of accompanying cards" -- and
            // `.3` says it is "designated by the identity icon printed in the
            // bottom right corner of the card". The extract records that corner
            // as the `Class` attribute, where an aspect card carries its aspect
            // and an identity-specific one carries `Hero`.
            //
            // A contains rather than an equals: `rr:classifications` lets a
            // card hold more than one, and three cards in the pool are printed
            // both identity-specific and aspect.
            return
            [
                .. cast.World.Seats[cast.Player].Hand.Cards
                    .Where(card => cast.World.Facts
                        .Attributes(card.FaceId)
                        .GetValueOrDefault("Class", string.Empty)
                        .Split(';')
                        .Contains("Hero", StringComparer.Ordinal)),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } supports
            && supports.Argument is AbilityValue.Word { Value: "supportsYouControl" })
        {
            // The support half of `upgradesAndSupportsYouControl`, on its own,
            // because Speed Demon's boost says "support" and an upgrade is not
            // one. `rr:play-area.1` again for what "you control" reads as.
            return [.. cast.World.AreaOf(DeckType.SupportsArea, PlayArea.Of(cast.Player)).Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } characters
            && characters.Argument is AbilityValue.Word { Value: "charactersYouControl" })
        {
            // "The character you control with the highest ATK value." Every
            // character, not only those in hero form: `rr:you-your.10` reads
            // "you control" as the cards in that player's play area, and an
            // alter-ego is a character with a hit point dial. An alter-ego
            // prints no ATK, and `rr:dash-value.3` makes that "an unmodifiable
            // 0" rather than a card that cannot be compared.
            return
            [
                cast.World.Seats[cast.Player].IdentityCard,
                .. cast.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(cast.Player)).Cards,
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } upgrades
            && upgrades.Argument is AbilityValue.Word { Value: "upgradesYouControl" })
        {
            // The upgrade half of `upgradesAndSupportsYouControl`, on its own,
            // because Beetle's two abilities both say "upgrade" and a support
            // is not one. Same reading of control: `rr:play-area.1` puts "any
            // cards in play under their control" in a player's own play area.
            return [.. cast.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(cast.Player)).Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } panther
            && panther.Argument is AbilityValue.Word { Value: "blackPantherUpgrades" })
        {
            return
            [
                .. cast.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(cast.Player)).Cards
                    .Where(card => Rules.State.Traits.Has(
                        cast.World, card, "BLACK_PANTHER", cast.World.Facts)),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } engaged
            && engaged.Argument is AbilityValue.Word { Value: "enemiesEngagedWithChosenPlayer" })
        {
            int player = ChosenPlayer(cast).Owner;
            return
            [
                .. cast.World.AreaOf(
                    DeckType.EngagedEnemiesArea, PlayArea.Of(player)).Cards,
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } allies
            && allies.Argument is AbilityValue.Word { Value: "alliesYouControl" })
        {
            // "Each ally **you control**", which is where the card is:
            // `rr:play-area.1` puts "any cards in play under their control" in
            // a player's own play area, so control is a read of the board
            // rather than a field -- the same reading `rr:engage.1` gets for a
            // minion. Not `heroesAndAllies`, which is every player's: Boomerang
            // hits the allies of the player it attacked and nobody else's.
            return [.. cast.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(cast.Player)).Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } heroes
            && heroes.Argument is AbilityValue.Word { Value: "heroes" })
        {
            // **Not every identity.** `rr:form-change-form.5`: "while a player
            // is in alter-ego form, card abilities that interact with their
            // hero do not interact with their identity." So "each hero" passes
            // over a player who has flipped down, and Shocker's one damage is
            // one damage to whoever is standing up.
            return [.. cast.World.PlayerOrder
                .Select(seat => cast.World.Seats[seat])
                .Where(seat => Forms.In(cast.World, seat, cast.World.Facts, Forms.Hero))
                .Select(seat => seat.IdentityCard)];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } identities
            && identities.Argument is AbilityValue.Word { Value: "identities" })
        {
            return [.. cast.World.PlayerOrder.Select(player =>
                cast.World.Seats[player].IdentityCard)];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } eligiblePlayers
            && eligiblePlayers.Argument is AbilityValue.Word
                { Value: "identitiesWithTechInDiscard" })
        {
            return
            [
                .. cast.World.PlayerOrder
                    .Where(player => cast.World.AreaOf(
                            DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player)
                        .Cards.Any(card => Rules.State.Traits.Has(
                            cast.World, card, "TECH", cast.World.Facts)))
                    .Select(player => cast.World.Seats[player].IdentityCard),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } topmost
            && topmost.Argument is AbilityValue.Word
                { Value: "topmostTechInChosenDiscard" })
        {
            int player = ChosenPlayer(cast).Owner;
            var card = cast.World.AreaOf(
                    DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player)
                .Cards.LastOrDefault(candidate => Rules.State.Traits.Has(
                    cast.World, candidate, "TECH", cast.World.Facts));
            return card is null ? [] : [card];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } allCharacters
            && allCharacters.Argument is AbilityValue.Word { Value: "characters" })
        {
            return
            [
                .. cast.World.PlayerOrder.Select(player =>
                    cast.World.Seats[player].IdentityCard),
                .. cast.World.Areas
                    .Where(area => area.Type is DeckType.AlliesArea
                        or DeckType.VillainArea or DeckType.EngagedEnemiesArea)
                    .SelectMany(area => area.Cards),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } drones
            && drones.Argument is AbilityValue.Word { Value: "drones" })
        {
            return FacedownDrones.InPlay(cast.World);
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } engagedDrones
            && engagedDrones.Argument is AbilityValue.Word { Value: "dronesEngagedWithYou" })
        {
            return FacedownDrones.EngagedWith(cast.World, Resolver(cast));
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "withTrait" } withTrait)
        {
            string wanted = Word(withTrait.Require("trait"));
            return [.. Every(withTrait.Require("cards"), cast).Where(card =>
                Rules.State.Traits.Has(cast.World, card, wanted, cast.World.Facts))];
        }

        return Find(value, cast) is { } one ? [one] : [];
    }

    /// <summary>The top cards of a deck, in top-to-bottom order.</summary>
    private static IReadOnlyList<Card> TopCards(Area deck, int count) =>
        [.. deck.Cards.TakeLast(count).Reverse()];

    /// <summary>
    /// The cards in named areas that match a search's criteria — <c>rr:search</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Search the encounter deck for a <b>[[Criminal]] minion</b>." Three
    /// named facets: which area, which card type, which trait. The Doomsday
    /// Chair adds the other printed shape in the core set: two areas and one
    /// card named by title.
    /// <c>docs/card-dsl.md</c> is explicit that selection must be "a fixed
    /// vocabulary of relations, <b>not</b> as a general 'run this predicate'
    /// hook" — so this grows a facet when a card prints one, and never a
    /// filter expression.
    /// </para>
    /// <para>
    /// <b>Nothing leaves the area here.</b> <c>rr:search.2</c>: "cards being
    /// searched are not considered to leave the searched area." This answers
    /// which cards a player may pick; the picking is a <c>chooseCard</c>, which
    /// is where <c>rr:search.1</c> puts the choice — "if a player finds
    /// multiple cards that satisfy the criteria of a search, the player chooses
    /// among those options."
    /// </para>
    /// <para>
    /// <b>The shuffle is not here either.</b> <c>rr:search.3</c> shuffles "upon
    /// completion of that game step, game function, or card ability", which is
    /// after the choice has been answered — so the card carries it as a step of
    /// its own, in both branches, because the deck was searched whether or not
    /// anything was found.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<Card> CardsIn(AbilityNode node, Cast cast)
    {
        var areas = node.Field("areas") is AbilityValue.List several
            ? several.Values.Select(named => Area(Word(named), cast)).ToList()
            : [Area(Word(node.Require("area")), cast)];
        string? kind = node.Field("kind") is { } named ? Word(named) : null;
        string? trait = node.Field("trait") is { } carried ? Word(carried) : null;
        string? title = node.Field("title") is { } titled ? Word(titled) : null;

        return
        [
            .. areas.SelectMany(area => area.Cards)
                .Where(card => kind is null || string.Equals(
                    cast.World.Facts.Kind(card.FaceId).ToString(), kind, StringComparison.Ordinal))
                .Where(card => trait is null
                    || Rules.State.Traits.Has(cast.World, card, trait, cast.World.Facts))
                .Where(card => title is null || string.Equals(
                    cast.World.Facts.Title(card.FaceId), title, StringComparison.Ordinal)),
        ];
    }

    /// <summary>
    /// "The lowest-cost upgrade you control" — <c>minBy</c> and <c>maxBy</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ties are kept.</b> The Rules Reference gives no tie-break for "the
    /// lowest-cost X", and collapsing one here would be the interpreter
    /// deciding something the rules leave to the table. So this answers with
    /// every card that shares the extreme value, and the card that wants one
    /// wraps it in a <c>chooseCard</c> — which is where
    /// <c>rr:choose-game-element.1</c> puts the question, to the player
    /// resolving.
    /// </para>
    /// <para>
    /// <b>Permanents are not among the candidates.</b>
    /// <c>rr:permanent.4.1</c> names this exact shape: "if a permanent card
    /// would be targeted by such an effect <i>(for example, 'discard the
    /// lowest-cost support you control')</i>, that effect instead targets the
    /// <b>non-permanent</b> card that fits its criteria." So a permanent is
    /// dropped before the comparison rather than after it, or a cheap
    /// permanent would shield a dearer card that the effect should have taken.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<Card> Ranked(AbilityNode node, Cast cast)
    {
        // Through `StateFields` rather than straight at the printed field:
        // `rr:permanent.1` makes the keyword "equivalent to the following
        // constant ability", and a constant ability is something a card can
        // grant. Reading print alone would miss a permanence handed out in
        // play.
        var among = Every(node.Require("of"), cast)
            .Where(card => StateFields.Modified(
                cast.World, card, "permanent", cast.World.Facts, cast.World.Players) <= 0)
            .ToList();

        if (among.Count == 0)
        {
            return [];
        }

        string key = Word(node.Require("by"));
        long Rank(Card card) => key switch
        {
            // `rr:dash-value.3` -- a printed dash "is treated as an
            // unmodifiable 0", which is what `PrintedValue` answers for a field
            // that is not a number, so nothing extra is needed for it here.
            "cost" => cast.World.Facts.PrintedValue(card.FaceId, "Cost", cast.World.Players),
            "attack" => StateFields.Modified(
                cast.World, card, "attack", cast.World.Facts, cast.World.Players),
            "printedHealth" => cast.World.Facts.PrintedValue(
                card.FaceId, "HP", cast.World.Players),
            _ => throw new AbilityException($"'{key}' is not a value cards can be ranked by"),
        };

        long extreme = node.Kind == "minBy" ? among.Min(Rank) : among.Max(Rank);
        return [.. among.Where(card => Rank(card) == extreme)];
    }

    /// <summary>Which card a value names, or null when it names none.</summary>
    private static Card? Find(AbilityValue value, Cast cast) => value switch
    {
        AbilityValue.Word word => Named(word.Value, cast),
        AbilityValue.Map => Find(Tree(value), cast),
        _ => throw new AbilityException($"{AbilityNode.Describe(value)} does not name a card"),
    };

    /// <summary>Which one card a query names, refusing a player choice.</summary>
    private static Card? Find(AbilityNode node, Cast cast)
    {
        if (node.Kind != "cardsIn")
        {
            return Query(node, cast);
        }

        var found = CardsIn(node, cast);
        return found.Count switch
        {
            0 => null,
            1 => found[0],
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' searched and found {found.Count} matching cards; "
                + "rr:search.1 gives the player that choice and asking is not implemented"),
        };
    }

    private static Card? Named(string name, Cast cast) => name switch
    {
        "this" => cast.Source,
        "that" => cast.Altered,

        // "Stun **the attacking character**." Not the attacking player:
        // `rr:ally.2` lets a player attack with an ally, and `rr:you-your.15`
        // is emphatic that an ally's attack is *not* performed by that player's
        // identity -- so Shocker stuns whichever character swung, and the
        // player standing behind it is untouched.
        "trigger.actor" => cast.Occurrence.Actor >= 0
            ? cast.World.Cards[cast.Occurrence.Actor]
            : null,

        "trigger.target" => cast.Occurrence.Target >= 0
            ? cast.World.Cards[cast.Occurrence.Target]
            : null,

        // The card a `chooseCard` was answered with. Null while the ability is
        // still asking, which is why nothing before the answer can read it.
        "chosen" => cast.Chosen,

        // "Your hero" and not "you". `rr:form-change-form.5`: "while a player
        // is in alter-ego form, card abilities that interact with their hero do
        // not interact with their identity" -- so this names nothing at all
        // when the player has flipped down, and a card that has something to
        // say about that says it with `exists`.
        "yourHero" => Forms.In(
            cast.World, cast.World.Seats[Resolver(cast)], cast.World.Facts, Forms.Hero)
            ? cast.World.Seats[Resolver(cast)].IdentityCard
            : null,

        // The other half of the form-specific reference. `rr:form-change-form.4`
        // says a hero-form identity is not its alter-ego for card abilities,
        // just as `.5` says an alter-ego-form identity is not its hero.
        "yourAlterEgo" => Forms.In(
            cast.World, cast.World.Seats[Resolver(cast)], cast.World.Facts, Forms.AlterEgo)
            ? cast.World.Seats[Resolver(cast)].IdentityCard
            : null,

        // `rr:you-your.5`: "if a card ability places a status card on 'you'
        // (such as 'you are stunned'), the player resolving that card ability
        // places that status card on their identity." `rr:you-your` opens with
        // the general form -- "if the word 'you' **can** be resolved as
        // referring to the player's identity, it **must** be resolved as such"
        // -- so "you" is a card here whenever a card is what is wanted.
        // "The player who defeated this scheme confuses their identity."
        // `rr:you-your.5` is why this answers an identity rather than a seat:
        // a status card placed on a player goes on their identity.
        "defeater" => cast.Occurrence.Defeat is { By: >= 0 } defeated
            ? cast.World.Seats[defeated.By].IdentityCard
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' names the player who defeated a card, and no player "
                + "did"),

        // "The **activating enemy** gets +2 SCH and +2 ATK for this
        // activation." A boost card is turned faceup in the middle of an
        // activation and its own occurrence is about the boost card, so the
        // enemy is read off the board rather than off the moment --
        // `rr:activation` is what makes one answer serve an attack and a scheme
        // alike.
        "activatingEnemy" => cast.World.Activation is { } activating
            ? cast.World.Cards[activating.Enemy]
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' names the activating enemy, and no enemy is "
                + "activating"),

        // "After **an ally** is defeated by anything other than consequential
        // damage." The card the occurrence defeated, which is not its subject:
        // an attack keeps its participants in actor and target roles, and the
        // ally that died is a second thing the same moment did.
        "defeated" => cast.Occurrence.Defeat is { } killed
            ? cast.World.Cards[killed.Card]
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' names the defeated card, and nothing was defeated"),

        "you" => cast.World.Seats[Resolver(cast)].IdentityCard,
        "attachedTo" => cast.Source.Area.Host >= 0 ? cast.World.Cards[cast.Source.Area.Host] : null,
        "trigger.subject" => cast.Occurrence.Subject >= 0
            ? cast.World.Cards[cast.Occurrence.Subject]
            : null,
        _ => throw new AbilityException($"'{name}' does not name a card"),
    };

    private static Card? Query(AbilityNode node, Cast cast)
    {
        // "Bomb Scare", "Vulture" -- a card in play named by its title, which
        // is a query with an argument rather than one of the bare words below.
        // `rr:identity.2` makes a title name one card, so this compares titles
        // and not printed ids.
        if (node.Kind == "titled")
        {
            var referenced = ReferencedByTitle(Word(node.Argument), cast);
            return referenced.Count switch
            {
                0 => null,
                1 => referenced[0],
                _ => throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' refers to {referenced.Count} cards titled "
                    + $"'{Word(node.Argument)}' where one card is required"),
            };
        }

        if (node.Kind != "query")
        {
            throw new AbilityException($"'{node.Kind}' does not name a card");
        }

        string what = Word(node.Argument);
        if (what == "topmostTechInChosenDiscard")
        {
            int player = ChosenPlayer(cast).Owner;
            return cast.World.AreaOf(
                    DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player)
                .Cards.LastOrDefault(candidate => Rules.State.Traits.Has(
                    cast.World, candidate, "TECH", cast.World.Facts));
        }

        return what switch
        {
            // `rr:villain-villain-deck` -- one villain is in the villain area.
            "villain" => cast.World.TheCardIn(DeckType.VillainArea),
            "mainScheme" => cast.World.TheCardIn(DeckType.MainSchemesArea),

            // "Your set-aside nemesis minion" and "your set-aside nemesis side
            // scheme". A nemesis set holds one of each, so naming the kind
            // names the card -- and answering null when it has already been
            // taken is what Shadow of the Past's surge branch reads.
            "yourAsideMinion" => Aside(cast, CardKind.Minion),
            "yourAsideSideScheme" => Aside(cast, CardKind.EncounterSideScheme),
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' queries '{what}', which is not implemented"),
        };
    }

    /// <summary>Cards a printed title reference denotes — <c>rr:referential-ability</c>.</summary>
    /// <remarks>
    /// The Rules Reference supplies the precedence; the set-name normalization
    /// is the engine's mapping from the vendored dataset to “associated with
    /// the same identity.” A unique title needs no tie-break. When the title is
    /// shared, self wins, then the identity family, then cards on the same side
    /// of the encounter/player boundary as the source.
    /// </remarks>
    private static List<Card> ReferencedByTitle(string title, Cast cast)
    {
        bool HasPrintedTitle(Card card) => card.Faces.Any(face => string.Equals(
            cast.World.Facts.Title(face), title, StringComparison.Ordinal));

        bool ShowsTitle(Card card) => card.FaceUp
            && !FacedownDrones.Is(card)
            && string.Equals(
                cast.World.Facts.Title(card.FaceId), title, StringComparison.Ordinal);

        if (HasPrintedTitle(cast.Source))
        {
            // A self-reference continues to name its source after a cost moves
            // that card out of play; rr:initiating-abilities.3 lets it finish.
            return [cast.Source];
        }

        var matches = cast.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Where(card => !FacedownDrones.Is(card) && HasPrintedTitle(card))
            .ToList();
        if (matches.Count <= 1)
        {
            return [.. matches.Where(ShowsTitle)];
        }

        string associated = IdentityAssociation(
            cast.World.Facts.EncounterSet(cast.Source.FaceId));
        if (associated.Length > 0)
        {
            var sameIdentity = matches.Where(card => string.Equals(
                    IdentityAssociation(cast.World.Facts.EncounterSet(card.FaceId)),
                    associated,
                    StringComparison.Ordinal))
                .ToList();
            if (sameIdentity.Count > 0)
            {
                // The higher tier remains the reference even when its title
                // is on an inactive identity face. Form legality can make the
                // result empty; it cannot fall through to an unrelated card.
                return [.. sameIdentity.Where(ShowsTitle)];
            }
        }

        bool playerCard = IsPlayerCard(cast.World.Facts, cast.Source);
        return [.. matches.Where(card =>
            IsPlayerCard(cast.World.Facts, card) == playerCard
            && ShowsTitle(card))];
    }

    private static string IdentityAssociation(string set)
    {
        string[] suffixes =
        [
            "_nemesis",
            "_sense_deck",
            "_invocation_deck",
            "_gift_deck",
            "_labor_deck",
            "_weather_deck",
        ];
        foreach (string suffix in suffixes)
        {
            if (set.EndsWith(suffix, StringComparison.Ordinal))
            {
                return set[..^suffix.Length];
            }
        }
        return set;
    }

    /// <summary>The one card of a kind in the player's set-aside pile.</summary>
    private static Card? Aside(Cast cast, CardKind kind) =>
        cast.World.Seats[cast.Player].Nemesis.Cards
            .FirstOrDefault(card => cast.World.Facts.Kind(card.FaceId) == kind);

    /// <summary>
    /// Which player is resolving this ability, or a refusal.
    /// </summary>
    /// <remarks>
    /// <b>An encounter card's ability does not always have one.</b> A "When
    /// Defeated" on a minion belongs to nobody until somebody defeats it, and
    /// the cards say whose it is themselves — "the player who defeated Fabian
    /// Cortez". Until <c>Defeat</c> carries that, a card that asks for a player
    /// it has not got is refused by name rather than reaching for the first
    /// one.
    /// </remarks>
    private static int Resolver(Cast cast) => cast.Player >= 0
        ? cast.Player
        : throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' asks who is resolving it, and an encounter card's "
            + "ability has no player unless the card says which");

    private static int Seat(AbilityValue value, Cast cast) =>
        value is AbilityValue.Word word
            ? word.Value switch
            {
                AbilityPlayers.TriggerPlayer => cast.Occurrence.Player,
                AbilityPlayers.You => Resolver(cast),
                AbilityPlayers.Controller => cast.ProjectedPlayAreaPlayer
                    ?? ControllerOf(cast.World, cast.Source),
                "chosenPlayer" => ChosenPlayer(cast).Owner,
                "engagedPlayer" => cast.ProjectedPlayAreaPlayer
                    ?? (cast.Source.Area.PlayArea.Player >= 0
                    ? cast.Source.Area.PlayArea.Player
                    : throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' asks for its engaged player outside a "
                        + "player's engaged area")),
                "firstPlayer" => cast.World.FirstPlayer,
                _ => throw new AbilityException($"'{word.Value}' does not name a player"),
            }
            : throw new AbilityException(
                $"{AbilityNode.Describe(value)} does not name a player");

    private static Card ChosenPlayer(Cast cast) =>
        (cast.PlayerSelection ?? cast.Chosen) is { Owner: >= 0 } chosen
            ? chosen
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' asks for the chosen player before one was chosen");

    private static IEnumerable<AbilityNode> Nodes(AbilityValue value) =>
        value is AbilityValue.List list
            ? list.Values.Select(Tree)
            : throw new AbilityException(
                $"{AbilityNode.Describe(value)} is not a list of nodes");

    private static AbilityNode Tree(AbilityValue value) => AbilityNode.Of(value);

    private static string Word(AbilityValue value) =>
        value is AbilityValue.Word word
            ? word.Value
            : throw new AbilityException($"{AbilityNode.Describe(value)} is not a word");

    /// <summary>How much, which may be printed per player.</summary>
    /// <remarks>
    /// <c>rr:per-player-icon</c> multiplies by the number of players, and
    /// <c>rr:player-elimination.6</c> is the exception that keeps this
    /// <c>World.Players</c> rather than the number still playing: "effects that
    /// refer to the players in the game ignore eliminated players, <b>except
    /// for the per player icon</b>."
    /// </remarks>
    private static long Amount(AbilityValue value, Cast cast)
    {
        if (value is not AbilityValue.Map)
        {
            return Number(value);
        }

        var node = Tree(value);
        return node.Kind switch
        {
            "perPlayer" => Number(node.Argument) * cast.World.Players,

            // "X is the amount of threat on Bomb Scare" -- a number read off
            // the board rather than printed. `rr:threat` counts tokens, so this
            // is the token pool and not a printed field.
            "tokensOn" => Find(node.Argument, cast) is { } holder
                ? holder.Tokens.GetValueOrDefault("k_threat")
                : 0,

            // `result.*` -- what an action earlier in this ability actually
            // did, which is not what it was asked to do. Zero when nothing has
            // written it, so a card reading a result it never produced reads a
            // number rather than throwing: "no damage was healed" is exactly
            // the case where nothing ran.
            "result" => cast.Results.GetValueOrDefault(Word(node.Argument)),

            // "If there is at least 5 damage here" -- damage tokens on a card,
            // which `rr:damage.2` puts on an ally or minion and which an
            // attachment can hold when a card puts them there.
            "damageOn" => Find(node.Argument, cast)?.Damage ?? 0,
            "powerAmount" => cast.PowerAmount,
            "countersOn" => Find(node.Require("card"), cast) is { } counterHolder
                ? CounterCount(counterHolder, Word(node.Require("counter")))
                : 0,
            "printedResourceCountDiscarded" => Resources.PrintedCount(
                cast.Discarded, Word(node.Argument)[0], cast.World.Facts),
            "printedBoostIconsDiscarded" => cast.Discarded.Sum(card =>
                cast.World.Facts.PrintedValue(card.FaceId, "Boost", cast.World.Players)),
            // The binding's spelling is the engine's choice. The printed card
            // names what was "discarded this way," whose identity survives an
            // immediate encounter-deck reset even when the discard pile does not.
            "topEncounterDiscardBoostPlusOne" => 1 + (cast.Discarded.LastOrDefault() is { } card
                ? cast.World.Facts.PrintedValue(card.FaceId, "Boost", cast.World.Players)
                : 0),
            "remainingHealth" => Find(node.Argument, cast) is { } remaining
                ? Math.Max(
                    0,
                    Damage.Health(cast.World, cast.World.Facts, remaining) - remaining.Damage)
                : 0,
            "if" => Test(Tree(node.Require("test")), cast)
                ? Amount(node.Require("then"), cast)
                : node.Field("else") is { } otherwise
                    ? Amount(otherwise, cast)
                    : 0,
            "count" => Every(node.Argument, cast).Count,
            "discardedWithResource" => cast.Discarded.Count(card =>
                Resources.GeneratedBy(card.FaceId, cast.World.Facts).Contains(
                    Word(node.Argument), StringComparison.Ordinal)),
            "modified" => Find(node.Require("card"), cast) is { } modified
                ? StateFields.Modified(
                    cast.World, modified, Word(node.Require("field")),
                    cast.World.Facts, cast.World.Players)
                : 0,
            "min" => Values(node.Argument).Select(each => Amount(each, cast)).Min(),
            "add" => Values(node.Argument).Sum(each => Amount(each, cast)),
            "mul" => Values(node.Argument).Aggregate(1L, (product, each) =>
                product * Amount(each, cast)),
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' asks for the amount '{node.Kind}', "
                + "which is not implemented"),
        };
    }

    private static long Number(AbilityValue value) =>
        value is AbilityValue.Number number
            ? number.Value
            : throw new AbilityException($"{AbilityNode.Describe(value)} is not a number");

    private static IReadOnlyList<AbilityValue> Values(AbilityValue value) =>
        value is AbilityValue.List list
            ? list.Values
            : throw new AbilityException($"{AbilityNode.Describe(value)} is not a list");

    private enum ResolutionOutcome
    {
        None,
        Partial,
        Full,
    }

    /// <summary>What one ability is resolving against.</summary>
    /// <param name="World">The board.</param>
    /// <param name="Source">The card whose text this is.</param>
    /// <param name="Occurrence">What it is timed to.</param>
    /// <param name="InitialPlayer">The seat resolving its first structural frame.</param>
    /// <param name="Events">Where to record what it did.</param>
    /// <param name="Abilities">
    /// The runner itself, for the rules that run more cards. A main scheme this
    /// ability completes advances, and <c>rr:villain-defeat</c> resolves the
    /// new stage's own "When Revealed" — so an ability can reach back into the
    /// interpreter that is running it.
    /// </param>
    private sealed record Cast(
        World World, Card Source, Occurrence Occurrence, int InitialPlayer,
        List<GameEvent> Events,
        ICardAbilities Abilities)
    {
        /// <summary>The seat whose perspective the current structural frame uses.</summary>
        public int Player { get; private set; } = InitialPlayer;

        /// <summary>The resolver to restore after leaving an each-player frame.</summary>
        public int AbilityPlayer { get; init; } = InitialPlayer;

        /// <summary>A trace-local player area after a projected card move.</summary>
        public int? ProjectedPlayAreaPlayer { get; init; }

        public void RestorePlayer(int player) => Player = player;

        /// <summary>The trigger string this ability's events carry.</summary>
        /// <remarks>
        /// A constant ability resolves against no occurrence and so has none.
        /// Nothing reachable from <c>Grants</c> asks — every use of this is in
        /// a verb, and <c>Grants</c> refuses every verb by name — so the guard
        /// belongs there rather than being restated here, where only one of the
        /// two could stay right after an edit.
        /// </remarks>
        public string Trigger => string.IsNullOrEmpty(EventTrigger)
            ? Occurrence.Conditions[0]
            : EventTrigger;

        /// <summary>Event-stream provenance carried across a scheduled power.</summary>
        public string? EventTrigger { get; init; }

        /// <summary>
        /// What the actions in this ability actually did — the <c>result.*</c>
        /// namespace.
        /// </summary>
        /// <remarks>
        /// Scoped to one resolution of one ability, because that is the scope
        /// the cards use: "if no damage was healed <b>this way</b>" is about
        /// this sentence and not about the game.
        /// </remarks>
        public Dictionary<string, long> Results { get; } = new(StringComparer.Ordinal);

        /// <summary>Non-numeric keywords gained during this resolution scope.</summary>
        /// <remarks>
        /// A reveal shares this set across each of the card's When Revealed
        /// abilities. Other entry points keep the per-cast default.
        /// </remarks>
        public HashSet<string> GainedKeywords { get; init; } =
            new(StringComparer.Ordinal);

        /// <summary>The resource letters generated to pay for this event.</summary>
        public string Payment { get; private set; } = string.Empty;

        public void PaidWith(string resources) => Payment = resources;

        /// <summary>Whether initiation payment may change outcome-relevant state.</summary>
        public bool PaymentMayMutate { get; private set; }

        public void SetPaymentMayMutate(bool value) => PaymentMayMutate = value;

        /// <summary>Whether an earlier sequence step may have changed the board.</summary>
        public bool PriorStepMayMutate { get; private set; }

        public void SetPriorStepMayMutate(bool value) => PriorStepMayMutate = value;

        /// <summary>Seats whose form an earlier sequence step may change.</summary>
        public ulong PriorFormsMayChange { get; private set; }

        public void SetPriorFormsMayChange(ulong value) =>
            PriorFormsMayChange = value;

        /// <summary>Whether an earlier step may bind a selected game element.</summary>
        public bool PriorBindingMayChange { get; private set; }

        public void SetPriorBindingMayChange(bool value) =>
            PriorBindingMayChange = value;

        /// <summary>Cards an earlier unanswered selector may bind.</summary>
        public IReadOnlyList<Card> PriorBindingCandidates { get; private set; } = [];

        public void SetPriorBindingCandidates(IReadOnlyList<Card> value) =>
            PriorBindingCandidates = value;

        /// <summary>Whether a reachable binding path supplies no selected card.</summary>
        public bool PriorBindingMayBeEmpty { get; private set; }

        public void SetPriorBindingMayBeEmpty(bool value) =>
            PriorBindingMayBeEmpty = value;

        /// <summary>Cards discarded earlier in this resolution, in order.</summary>
        public List<Card> Discarded { get; } = [];

        /// <summary>The game element bound by the current alteration frame.</summary>
        public Card? Altered { get; private set; }

        public void BindAlteration(Card card) => Altered = card;

        /// <summary>Whether this ability has stopped to ask a question.</summary>
        public bool Suspended { get; private set; }

        /// <summary>Stops the ability here — <c>rr:choose-option</c>.</summary>
        public void Suspend() => Suspended = true;

        /// <summary>The scheduled activations this sentence must wait for.</summary>
        public List<int> ActivationIds { get; } = [];

        public void WaitFor(IEnumerable<int> ids) => ActivationIds.AddRange(ids);

        /// <summary>Whether text after the current node still has to resolve.</summary>
        public bool HasContinuation { get; private set; }

        public void SetContinuation(bool value) => HasContinuation = value;

        /// <summary>Which step of the top-level sequence is running.</summary>
        public int Position { get; private set; }

        /// <summary>Records which step of the sequence this is.</summary>
        /// <param name="step">Its index.</param>
        public void At(int step) => Position = step;

        /// <summary>The exact authored ability and structural route being resolved.</summary>
        public int AbilityOrdinal { get; private set; } = -1;

        public string AbilityFace { get; private set; } = Source.FaceId;

        public List<string> AbilityPath { get; } = [];

        public void RestoreAbility(
            int ordinal, IReadOnlyList<string> path, string? face = null)
        {
            AbilityOrdinal = ordinal;
            if (face is not null)
            {
                AbilityFace = face;
            }
            AbilityPath.Clear();
            AbilityPath.AddRange(path);
        }

        private PendingAbility? resolutionAbility;

        /// <summary>The exact ability whose status this cast updates.</summary>
        public PendingAbility? ResolutionAbility => resolutionAbility;

        /// <summary>Begin tracking the exact ability whose tree this cast runs.</summary>
        public void TrackResolution(int ordinal)
        {
            if (Tier is not { } tier)
            {
                return;
            }
            resolutionAbility = new PendingAbility(Source.ObjectId, tier, Player, ordinal);
            Occurrence.Begin(resolutionAbility.Value);
        }

        /// <summary>Record that one effect in the tracked ability applied.</summary>
        public void ResolveEffect()
        {
            if (resolutionAbility is { } ability)
            {
                Occurrence.Resolve(ability);
            }
        }

        /// <summary>Finish a tracked ability unless it remains suspended.</summary>
        public void CompleteResolution()
        {
            if (!Suspended && resolutionAbility is { } ability)
            {
                Occurrence.Complete(ability);
            }
        }

        public void SetAbilityPath(IEnumerable<string> path)
        {
            var copy = path.ToList();
            AbilityPath.Clear();
            AbilityPath.AddRange(copy);
        }

        public void CompletePendingDependency(ResolutionOutcome outcome)
        {
            int pending = AbilityPath.FindLastIndex(frame =>
                frame.EndsWith(":Pending", StringComparison.Ordinal));
            if (pending >= 0)
            {
                AbilityPath[pending] = AbilityPath[pending][..^"Pending".Length]
                    + outcome;
            }
        }

        public bool HasPendingDependency => AbilityPath.Any(frame =>
            frame.EndsWith(":Pending", StringComparison.Ordinal));

        /// <summary>The card the player picked, once they have.</summary>
        public Card? Chosen { get; private set; }

        /// <summary>The outer card selection used by chosen-player references.</summary>
        public Card? PlayerSelection { get; private set; }

        /// <summary>Records the card a <c>chooseCard</c> was answered with.</summary>
        /// <param name="card">What they picked.</param>
        public void Choose(Card? card) => Chosen = card;

        /// <summary>Records a player answer in both chosen namespaces.</summary>
        public void ChooseSelection(Card? card)
        {
            Chosen = card;
            PlayerSelection = card;
        }

        public void RestorePlayerSelection(Card? card) => PlayerSelection = card;

        /// <summary>
        /// Which of the card's abilities is running, or null.
        /// </summary>
        /// <remarks>
        /// Only a suspended choice reads it, and it reads it to find its way
        /// back: a card with a choice in two of its abilities cannot be resumed
        /// from the card and a position alone. See <c>Choice</c>.
        /// </remarks>
        public AbilityType? Tier { get; init; }

        /// <summary>Whether this Special is the final step in its parent sequence.</summary>
        public bool FinalStep { get; init; }

        public bool EachPlayerFrame { get; init; }

        public bool FinalPlayer { get; init; }

        /// <summary>The labelled player power whose occurrence is resolving.</summary>
        public string? Power { get; init; }

        /// <summary>Every game element selected for this labelled power.</summary>
        public IReadOnlyList<Card> PowerTargets { get; init; } = [];

        /// <summary>The card attributed as performer of the labelled power.</summary>
        public Card? PowerActor { get; init; }

        /// <summary>The performer attributed by an ability-envelope label.</summary>
        public Card? AbilityActor { get; set; }

        /// <summary>Whether this fresh cast passed envelope legality before resolution.</summary>
        public bool LabelsPreflighted { get; set; }

        private const string CrisisIgnoringThwartPrefix =
            "__preflight.crisisIgnoringThwart.";

        private HashSet<AbilityNode> CrisisIgnoringThwarts { get; } = [];

        private HashSet<int> PersistedCrisisIgnoringThwarts { get; } = [];

        /// <summary>Persists a pre-payment exception to this power's target limit.</summary>
        public void ValidateCrisisIgnoringThwart(AbilityNode node) =>
            CrisisIgnoringThwarts.Add(node);

        /// <summary>Whether initiation established this scoped target exception.</summary>
        public bool CrisisIgnoringThwartWasValidated(AbilityNode node, int ordinal) =>
            CrisisIgnoringThwarts.Contains(node)
            || PersistedCrisisIgnoringThwarts.Contains(ordinal);

        /// <summary>Writes validated power addresses into continuation metadata.</summary>
        public void PersistCrisisIgnoringThwarts(
            CardAbility ability, Dictionary<string, long> results)
        {
            var nodes = PowerNodes(ability.Effect, BasicPowers.ThwartVerb).ToList();
            for (int ordinal = 0; ordinal < nodes.Count; ordinal++)
            {
                if (CrisisIgnoringThwarts.Contains(nodes[ordinal])
                    || PersistedCrisisIgnoringThwarts.Contains(ordinal))
                {
                    results[$"{CrisisIgnoringThwartPrefix}{ordinal}"] = 1;
                }
            }
        }

        /// <summary>Consumes engine-owned target metadata without exposing a result.</summary>
        public bool RestoreCrisisIgnoringThwart(string name, long value)
        {
            if (!name.StartsWith(CrisisIgnoringThwartPrefix, StringComparison.Ordinal))
            {
                return false;
            }
            string encoded = name[CrisisIgnoringThwartPrefix.Length..];
            if (value != 1 || !int.TryParse(
                    encoded,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out int ordinal)
                || ordinal < 0)
            {
                throw new RulesNotImplementedException(
                    $"'{Source.FaceId}' has invalid persisted thwart target metadata");
            }
            PersistedCrisisIgnoringThwarts.Add(ordinal);
            return true;
        }

        /// <summary>A numeric result carried into this labelled power.</summary>
        public long PowerAmount { get; init; } = -1;

        /// <summary>The outer threat assignment this interrupt can prevent.</summary>
        public ThreatPlacement? ImminentThreat { get; init; }

        /// <summary>Targets attacked by damage nodes, for one deferred retaliation each.</summary>
        public List<Card> Attacked { get; } = [];

        /// <summary>How much damage is about to be dealt — <c>rr:damage.step.1</c>.</summary>
        public long Incoming { get; init; }

        /// <summary>How much is left after this ability, defaulting to all of it.</summary>
        public long Remaining { get; private set; } = -1;

        /// <summary>Replaces the damage with this much.</summary>
        /// <param name="amount">What is left.</param>
        public void Replace(long amount) => Remaining = amount;
    }

    private sealed record ActivationEffect(
        int Source, int Player, AbilityType? Tier, AbilityNode Effect, int Altered,
        int AbilityActor);
}
