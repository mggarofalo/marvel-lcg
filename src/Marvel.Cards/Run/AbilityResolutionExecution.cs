using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityPaymentRules;
using System.Collections.Immutable;
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
/// Cards compose the supported operations as inert JSON. Construction validates
/// that data and lowers it to an immutable program; gameplay reads the checked
/// instructions rather than the supplied syntax maps.
/// </para>
/// <para>
/// Adding an operation requires engine behavior and tests. Authoring another
/// card combines those operations without introducing a card-specific class.
/// Unknown data fails validation; an unsupported rule situation raises during
/// resolution rather than inventing an outcome.
/// </para>
/// <para>
/// See <c>docs/card-dsl.md</c> for the language and <c>docs/timing.md</c> for
/// scheduling and continuation contracts.
/// </para>
/// </remarks>
internal sealed partial class AbilityResolutionExecution
{
    private readonly AbilityProgram program;
    private readonly AbilityOfferQueries offerQueries;
    private readonly AbilityGameRuntimes runtimes;
    private readonly IEncounterCardAbilities encounterAbilities;
    private readonly ICardPlayAbilities cardPlayAbilities;
    private readonly ICardReadinessAbilities readinessAbilities;
    private readonly IResourceCardAbilities resourceAbilities;
    private readonly IThreatCardAbilities threatAbilities;

    internal AbilityResolutionExecution(
        AbilityProgram program,
        AbilityGameRuntimes runtimes,
        IEncounterCardAbilities encounterAbilities,
        ICardPlayAbilities cardPlayAbilities,
        ICardReadinessAbilities readinessAbilities,
        IResourceCardAbilities resourceAbilities,
        IThreatCardAbilities threatAbilities,
        AbilityOfferQueries offerQueries)
    {
        ArgumentNullException.ThrowIfNull(program);
        this.program = program;
        this.runtimes = runtimes;
        this.encounterAbilities = encounterAbilities;
        this.cardPlayAbilities = cardPlayAbilities;
        this.readinessAbilities = readinessAbilities;
        this.resourceAbilities = resourceAbilities;
        this.threatAbilities = threatAbilities;
        this.offerQueries = offerQueries;
    }

    /// <summary>The verb an option carries on the wire.</summary>
    public const string ChooseVerb = AbilityStructuralExecution.ChooseVerb;

    private static readonly string[] Branches = ["then", "else"];

    private static readonly DeckType[] Owned = [DeckType.UpgradesArea, DeckType.SupportsArea];

    // A facedown Ultron Drone retains the underlying player-card face id for
    // the state digest, but `rr:in-play-and-out-of-play.5` and `.13` make that
    // facedown card text inactive. Every authored-ability entry point goes
    // through this boundary so no trigger, action, constant, boost, or query
    // can accidentally execute the hidden card.
    private ImmutableArray<CompiledCardAbility> On(Card card) =>
        FacedownDrones.Is(card) ? [] : program.On(card.FaceId);

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
            var cast = new AbilityResolutionState(
                world, card, occurrence, ControllerOf(world, card), events)
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
                var cast = new AbilityResolutionState(
                    world, enemy,
                    new Occurrence(
                        0, ["WhenActivationCompleted"],
                        Actor: enemy.ObjectId, Player: result.Player),
                    result.Player, events)
                {
                    Tier = ability.Trigger.Timing,
                };
                TrackResolution(cast, ability);
                Run(ability, cast);
                cast.CompleteResolution();
            }
        }

        foreach (var effect in runtimes.CompleteActivation(world, result.Id))
        {
                var delayedCast = new AbilityResolutionState(
                    world,
                    world.Cards[effect.Source],
                    new Occurrence(
                        0, ["WhenActivationCompleted"],
                        Actor: result.Enemy, Player: effect.Player),
                    effect.Player,
                    events)
                {
                    Tier = effect.Tier,
                    AbilityActor = effect.AbilityActor >= 0
                        ? world.Cards[effect.AbilityActor]
                        : null,
                };
                AbilityContinuationCodec.RecordImmediateActivationResult(
                    delayedCast.Results, result);
                if (effect.Altered >= 0)
                {
                    delayedCast.BindAlteration(world.Cards[effect.Altered]);
                }
                Run(effect.Effect, delayedCast);
        }

        if (world.Agenda.ActivationWait(result.Id) is { } waiting)
        {
            var updated = AbilityContinuationCodec.RecordActivationResult(waiting, result);
            if (updated.Complete)
            {
                _ = world.Agenda.TakeActivationWait(result.Id);
                events.AddRange(ResumeAbility(world, updated.Step));
            }
            else
            {
                world.Agenda.ReplaceActivationWait(result.Id, updated.Step);
            }
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
        var transition = AbilityContinuationCodec.BeginResume(program, source, continuation);
        var resumed = transition switch
        {
            RestartAfterPaidCost paid => paid.State,
            RunResumedNode node => node.State,
            ContinueAfterResumedNode completed => completed.State,
            ResumeComplete complete => complete.State,
            ResumeRejected rejected => throw new RulesNotImplementedException(rejected.Reason),
            _ => throw new InvalidOperationException("Unknown continuation transition"),
        };
        continuation = AbilityContinuationCodec.WithResumedResults(continuation, resumed);

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
        RestoreContinuationCursor(cast, resumed);
        cast.TrackResolution(continuation.AbilityOrdinal);
        RestorePersisted(cast, continuation);
        if (transition is RestartAfterPaidCost paidCost)
        {
            // The cost has settled. Do not persist its resume marker into a
            // later suspension inside the effect, or that continuation would
            // restart the whole effect instead of resuming its own path.
            var ability = paidCost.Ability;
            Use(world, source, ability, cast.Occurrence);
            if (world.Facts.Kind(source.FaceId) == CardKind.Event)
            {
                cast.Occurrence.BeginCard(
                    source.ObjectId,
                    [new PendingAbility(
                        source.ObjectId,
                        ability.Trigger.Timing,
                        continuation.Seat,
                        continuation.AbilityOrdinal)]);
            }
            Run(ability, cast);
            cast.CompleteResolution();
            DiscardEvent(source, cast);
            return cast.Events;
        }
        return ResumeContinuation(cast, source, transition);
    }

    private List<GameEvent> ResumeContinuation(
        AbilityResolutionState cast, Card source, AbilityContinuationTransition transition)
    {
        while (true)
        {
            switch (transition)
            {
                case ContinueAfterResumedNode completed:
                    RestoreContinuationCursor(cast, completed.State);
                    if (completed.EffectApplied)
                        cast.ResolveEffect();
                    transition = AbilityContinuationCodec.Advance(
                        StructuralContext(cast), completed.Ability, completed.State,
                        new AbilityStructuralObservation(false));
                    break;

                case RunResumedNode run:
                    RestoreContinuationCursor(cast, run.State);
                    if (run.EffectApplied)
                        cast.ResolveEffect();
                    RestoreAlteredFromFrames(cast, run.State.Frames);
                    Run(run.Effect, cast);
                    if (cast.Suspended)
                    {
                        cast.CompleteResolution();
                        return cast.Events;
                    }
                    transition = AbilityContinuationCodec.Advance(
                        StructuralContext(cast), run.Ability, run.State,
                        new AbilityStructuralObservation(false));
                    break;

                case DiscardForResumedEachTime discard:
                    RestoreContinuationCursor(cast, discard.State);
                    int before = cast.Discarded.Count;
                    var one = new AbilityEffect.DiscardTop(
                        AbilitySearchArea.EncounterDeck, Players: null,
                        new AbilityNumber.Constant(1));
                    if (!TryRunCardState(one, cast))
                        throw new InvalidOperationException(
                            "The card-state owner refused discardTop");
                    var discarded = cast.Discarded.Skip(before).SingleOrDefault();
                    if (discarded is not null)
                        cast.BindAlteration(discarded);
                    transition = AbilityContinuationCodec.AfterEachTimeDiscard(
                        StructuralContext(cast), discard, discarded);
                    break;

                case ResumeComplete complete:
                    RestoreContinuationCursor(cast, complete.State);
                    cast.CompleteResolution();
                    DiscardEvent(source, cast);
                    return cast.Events;

                case ResumeRejected rejected:
                    throw new RulesNotImplementedException(rejected.Reason);

                default:
                    throw new InvalidOperationException(
                        $"Unknown continuation transition {transition.GetType().Name}");
            }
        }
    }

    private static void RestoreContinuationCursor(
        AbilityResolutionState cast, AbilityContinuationState state)
    {
        cast.RestoreAbility(
            state.Address.Ordinal,
            state.Frames,
            state.Address.Face);
        cast.At(state.Position);
        cast.SetContinuation(state.HasContinuation);
        cast.RestorePlayer(state.Player);
    }

    private static void RestoreAlteredFromFrames(
        AbilityResolutionState cast, ImmutableArray<AbilityStructuralFrame> frames)
    {
        var card = frames.OfType<EachTimeFrame>()
            .LastOrDefault()?.DiscardedCard;
        if (card is { } id)
            cast.BindAlteration(cast.World.Cards[id]);
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
        var restored = AbilityContinuationCodec.DecodePower(
            program, source, abilityIndex, powerOrdinal, power, resumeFrom, abilityPath,
            abilityFace, eachPlayerFrame, finalPlayer);
        var ability = restored.Ability;
        var effect = restored.Body;
        var cast = new AbilityResolutionState(
            world, source, abilityOccurrence ?? occurrence, player, events)
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
        if (SuspendsPowerEffect(effect, cast))
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' suspends inside a {power.ToLowerInvariant()}, "
                + "which is not implemented");
        }
        int ordinal = restored.Ordinal;
        var continuationFrames = restored.Frames;
        cast.RestoreAbility(ordinal, continuationFrames, abilityFace);
        RestoreAlteredFromFrames(cast, continuationFrames);
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
        if (AbilityContinuationCodec.ChosenBinding(
            world.Cards, abilityResults, source.FaceId) is { } outerChosen)
        {
            cast.RestorePersistedSelection(
                world.Cards[outerChosen.ObjectId], outerChosen.AreaId,
                outerChosen.Incarnation, overwriteChosen: true);
        }

        var next = AbilityContinuationCodec.AfterPower(
            program, source, restored, Capture(cast, ordinal),
            world.Agenda.Current?.Round ?? 0, cast.Suspended);
        if (next is not null)
        {
            _ = ResumeContinuation(cast, source, next);
            return;
        }
        cast.CompleteResolution();
        DiscardEvent(source, cast);
    }

    /// <inheritdoc/>
    public IReadOnlyList<PendingAbility> Waiting(
        World world, Occurrence occurrence, WindowKind window)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);
        return [.. AbilityWindowAdmission.Waiting(
                program, world, occurrence, window, resourceAbilities)
            .Select(candidate => new PendingAbility(
                candidate.Card.ObjectId, candidate.Ability.Trigger.Timing,
                candidate.Controller, candidate.Ordinal))];
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
        var price = CombinedPrice(
            world, card, ability.Player, found, resourceAbilities);
        return new Affordance(
            Id: ability.Card,
            Verb: found.Name,
            AnchorId: ability.Card,
            AnchorPlayer: ability.Player,
            Label: found.Name,
            Targets: AbilityCostSelection.Ask(world, ability.Player, found.Cost),
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
        var cast = new AbilityResolutionState(world, card, occurrence, resolving, events)
        {
            Tier = found.Trigger.Timing,
        };
        cast = cast.ForReachability(cast.Reachability with
        {
            PaymentMayMutate = found.Cost is not null || world.Facts.Kind(card.FaceId) == CardKind.Event,
            PaymentCost = found.Cost,
        });

        if (!AbilityAvailability.Available(
                world, card, found,
                AbilityAvailability.IndexOf(program, card, found), occurrence))
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' has reached its printed maximum for this "
                + "ability's period");
        }

        // A forced ability is resolved rather than offered, but its printed
        // arrow cost is still paid at `rr:initiating-abilities.step.5`.
        // Superhuman Strength's “discard this card” names the whole payment,
        // so no player decision is needed. A mandatory cost that does require
        // a selection needs a prompt carried by the timing window; refuse that
        // state instead of choosing on the player's behalf.
        if (AbilityTypes.IsMandatory(found.Trigger.Timing) && found.Cost is not null)
        {
            if (!MandatoryCostIsAutomatic(found.Cost!))
            {
                throw new RulesNotImplementedException(
                    $"'{card.FaceId}' has a mandatory ability whose '{found.Cost.OperationName()}' "
                    + "cost requires a player decision");
            }
            if (!Payable(
                    world, card, resolving, found.Cost, program, resourceAbilities))
            {
                return events;
            }
        }
        else if (!CounterCostsPayable(world, card, resolving, found.Cost))
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' can no longer pay this ability's cost");
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
        var costOwner = world.Agenda.Current;
        var costOccurrence = world.Agenda.Occurrence;
        var arrowPayment = AbilityCostPayment.Prepare(
            world, card, cast.Player, found.Cost, paying, chosen,
            program, resourceAbilities, values,
            resourcesPaidByEvent: world.Facts.Kind(card.FaceId) == CardKind.Event
                && ResourceRequirement(found.Cost, card).Length > 0);
        var eventPayment = AbilityEventPayment.Prepare(
            world, card, cast.Player, paying, found.Effect, resourceAbilities,
            allocations, found.Cost);
        if (eventPayment is not null)
        {
            cast.PaidWith(eventPayment.Commit(occurrence, events));
        }
        ApplyPayment(arrowPayment.Commit(cardPlayAbilities, cast.Trigger, events), cast);
        if (cast.Suspended)
        {
            SuspendAfterCost(cast, ability.Ordinal, costOwner, costOccurrence);
            return events;
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
    public IReadOnlyList<PendingAbility> WhenRevealedAbilities(
        World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        if (!program.KnowsWhenRevealed(card.FaceId))
        {
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was revealed and no ability data is written for it; "
                + $"this engine has {program.Authored.Count} authored card(s)");
        }

        return [.. On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenRevealed)
            .Where(ability => string.Equals(
                ability.Trigger.Event, Steps.CardRevealed, StringComparison.Ordinal))
            .Select((_, ordinal) => new PendingAbility(
                card.ObjectId, AbilityType.WhenRevealed, player, ordinal))];
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenRevealed(
        World world, Card card, int player, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(occurrence);

        if (!program.KnowsWhenRevealed(card.FaceId))
        {
            // Authored-and-does-nothing is a different thing from nobody having
            // read the card, and only one of them is safe to treat as silence.
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was revealed and no ability data is written for it; "
                + $"this engine has {program.Authored.Count} authored card(s)");
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
            var cast = new AbilityResolutionState(world, card, occurrence, player, events)
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
        var kind = world.Facts.Kind(card.FaceId);
        bool mayBeCanceled = !CardKinds.IsVillain(kind) && kind != CardKind.MainScheme;
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
            var cast = new AbilityResolutionState(world, card, occurrence, player, events)
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
        var cast = new AbilityResolutionState(
            world, card,
            new Occurrence(0, [Steps.ResolveSpecial], Subject: card.ObjectId, Player: player),
            player, events)
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
    /// <summary>Records one use of a limited ability, until the round ends.</summary>
    private void Use(
        World world, Card card, CompiledCardAbility ability, Occurrence? occurrence = null)
        => AbilityUseRecording.Record(world, program, card, ability, occurrence);

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

            var cast = new AbilityResolutionState(world, card, occurrence, target.Owner, events)
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
    public static long WouldTake(
        World world, Card target, Card source, long amount, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(events);

        var prevention = world.Effects.Active().FirstOrDefault(effect =>
            string.Equals(effect.Kind, "preventDamage", StringComparison.Ordinal)
            && effect.Affects == target.ObjectId);
        if (prevention is null || !world.Effects.Use(prevention))
        {
            return amount;
        }

        long prevented = prevention.Amount <= 0 ? amount : prevention.Amount;
        return Math.Max(0, amount - prevented);
    }

    /// <inheritdoc/>
    public static void DamagePreventedByTough(
        World world, Card target, Card source, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(events);

        var prevention = world.Effects.Active().Where(effect =>
            string.Equals(effect.Kind, "preventDamage", StringComparison.Ordinal)
            && effect.Affects == target.ObjectId).ToList();
        foreach (var effect in prevention)
        {
            world.Effects.Use(effect);
        }
    }

    /// <inheritdoc/>
    public void WouldBeDefeated(World world, Card target, List<GameEvent> events)
    {
        _ = WouldBeDefeated(
            world, target, target, Steps.CardWouldBeDefeated,
            Steps.CardWouldBeDefeated, -1, events);
    }

    /// <inheritdoc/>
    public bool WouldBeDefeated(
        World world, Card target, Card source, string trigger, string verb, int by,
        List<GameEvent> events, Occurrence? recordDefeatOn = null)
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
                SuspendWouldBeDefeated(
                    world, target, source, trigger, verb, by, occurrence, optional,
                    recordDefeatOn);
                return false;
            }

            if (mandatory.Count > 1)
            {
                SuspendWouldBeDefeated(
                    world, target, source, trigger, verb, by, occurrence, mandatory,
                    recordDefeatOn);
                return false;
            }

            occurrence.Trigger(WindowKind.Interrupt, mandatory[0].Card);
            spent?.Trigger(WindowKind.Interrupt, mandatory[0].Card);
            events.AddRange(Resolve(world, occurrence, mandatory[0], [], []));

            // `rr:would.1`: once the interrupt changes the imminent defeat,
            // no later interrupt to that original condition may be used.
            if (Damage.Health(world, world.Facts, target) - target.Damage > 0)
            {
                return true;
            }
        }

        return true;
    }

    private static void SuspendWouldBeDefeated(
        World world, Card target, Card source, string trigger, string verb, int by,
        Occurrence occurrence, IReadOnlyList<PendingAbility> pending,
        Occurrence? recordDefeatOn)
    {
        var step = new PhaseStep(
            Steps.ChooseWouldBeDefeated,
            world.Agenda.Current?.Round ?? 0,
            6,
            Subject: target.ObjectId,
            Seat: target.Owner >= 0 ? target.Owner : world.FirstPlayer,
            Plan: true,
            ProcedureAbilities: [.. pending],
            ProcedureOccurrence: occurrence,
            ProcedureOwnerOccurrence: recordDefeatOn,
            ProcedureSource: source.ObjectId,
            ProcedureTrigger: trigger,
            ProcedureVerb: verb,
            ProcedureBy: by);

        if (world.Agenda.Occurrence is { } parent)
        {
            world.Agenda.ThenContinuation(step, parent);
            world.Agenda.BeforeResponses(parent);
        }
        else
        {
            world.Agenda.Add(step);
        }
    }

    /// <summary>Every authored ability answering one occurrence, with its card.</summary>
    /// <remarks>
    /// <b>Gathered before any of it runs.</b> An ability can make an area —
    /// giving a status card creates one to hold it — and walking
    /// <c>World.Areas</c> lazily while resolving would be modifying the
    /// collection being read.
    /// </remarks>
    private List<(Card Card, CompiledCardAbility Ability)> Waiting(World world, Occurrence what) =>
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
        World world, CompiledCardAbility ability, Card card, Occurrence what)
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
    public IReadOnlyList<GameEvent> Setup(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        if (!program.Authored.Contains(card.FaceId))
        {
            // The same distinction `WhenRevealed` makes, and setup is where it
            // matters most: a scenario whose main scheme nobody has read would
            // otherwise deal a board that is quietly missing whatever its first
            // card said, and every later assertion would be about the wrong
            // game.
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' is being set up and no ability data is written for it; "
                + $"this engine has {program.Authored.Count} authored card(s)");
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
                var cast = new AbilityResolutionState(world, card, occurrence, card.Owner, events)
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
    public IReadOnlyList<GameEvent> ResolveEachPlayer(
        World world, Card source, int player, int stoppedAt,
        AbilityType? tier, bool finalStep, bool finalPlayer)
    {
        var step = world.Agenda.Current;
        var restored = AbilityContinuationCodec.DecodeEachPlayer(
            program, source, step, stoppedAt, tier, player, finalPlayer);

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
            restored.Ordinal, restored.Frames,
            step?.AbilityFace);
        cast.TrackResolution(restored.Ordinal);
        RestoreAlteredFromFrames(cast, cast.StructuralPath.ToImmutableArray());
        cast.At(stoppedAt - 1);
        cast.SetContinuation(restored.HasContinuation);
        Run(restored.Body, cast);
        var next = AbilityContinuationCodec.AfterEachPlayer(
            program, source, restored, Capture(cast, restored.Ordinal),
            world.Agenda.Current?.Round ?? 0, tier, finalPlayer, cast.Suspended);
        if (next is not null)
        {
            return ResumeContinuation(cast, source, next);
        }
        cast.CompleteResolution();
        return cast.Events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenCardDefeated(World world, Card card, Defeated defeated)
    {
        var events = new List<GameEvent>();
        _ = WhenCardDefeated(world, card, defeated, Steps.CardDefeated, events);
        return events;
    }

    /// <inheritdoc/>
    public bool WhenCardDefeated(
        World world, Card card, Defeated defeated, string trigger,
        List<GameEvent> events)
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
        if (elsewhere.Count == 0)
        {
            // `rr:when-defeated-abilities.2` says all abilities on the defeated
            // card resolve. Their printed/data order is already authoritative;
            // the cross-card ordering question from `rr:forced.5` does not
            // arise until another card answers the same defeat.
            foreach (var ability in written)
            {
                var cast = new AbilityResolutionState(world, card, occurrence, card.Owner, events)
                {
                    Tier = ability.Trigger.Timing,
                };
                TrackResolution(cast, ability);
                Run(ability, cast);
                cast.CompleteResolution();
            }
            return true;
        }

        var own = written.Select((_, ordinal) => new PendingAbility(
            card.ObjectId, AbilityType.WhenDefeated, card.Owner, ordinal));
        var waiting = own.Concat(elsewhere).ToList();
        while (waiting.Count > 0)
        {
            var mandatory = waiting
                .Where(ability => AbilityTypes.IsMandatory(ability.Type))
                .ToList();
            var offered = mandatory.Count > 0
                ? mandatory
                : waiting.Where(ability => !AbilityTypes.IsMandatory(ability.Type)).ToList();
            if (offered.Count > 1 || mandatory.Count == 0)
            {
                SuspendCardDefeated(
                    world, card, trigger, occurrence, defeated, offered);
                return false;
            }

            var next = offered[0];
            occurrence.Trigger(WindowKind.Interrupt, next.Card);
            spent?.Trigger(WindowKind.Interrupt, next.Card);
            events.AddRange(Resolve(world, occurrence, next, [], []));
            waiting.Remove(next);
        }

        return true;
    }

    private static void SuspendCardDefeated(
        World world, Card card, string trigger, Occurrence occurrence,
        Defeated defeated, IReadOnlyList<PendingAbility> pending)
    {
        occurrence.Also(defeated);
        var step = new PhaseStep(
            Steps.ChooseCardDefeatedAbility,
            world.Agenda.Current?.Round ?? 0,
            7,
            Subject: card.ObjectId,
            Seat: card.Owner >= 0 ? card.Owner : world.FirstPlayer,
            Plan: true,
            ProcedureAbilities: [.. pending],
            ProcedureOccurrence: occurrence,
            ProcedureTrigger: trigger,
            ProcedureVerb: defeated.How,
            ProcedureBy: defeated.By);

        if (world.Agenda.Occurrence is { } parent)
        {
            world.Agenda.ThenContinuation(step, parent);
            world.Agenda.BeforeResponses(parent);
        }
        else
        {
            world.Agenda.Add(step);
        }
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
    /// A non-forced interrupt is returned as the earliest waiting tier. The
    /// rules layer persists that tier as a procedure continuation and offers it
    /// without moving the defeated card or replaying the damage.
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
            if (mandatory.Count > 0 || optional.Count > 0)
            {
                return mandatory.Count > 0 ? mandatory : optional;
            }
        }

        return [];
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
        var cast = new AbilityResolutionState(
            world,
            card,
            occurrence,
            ability.Player,
            events)
        {
            Tier = found.Trigger.Timing,
        };

        // The wire only names an affordance; it is not authority to use one
        // after its legality has changed. Re-run the same initiation checks
        // that produced Actions before any cost moves a card or exhausts a
        // game element. `rr:cost.6` and `rr:event.3` make target availability
        // part of whether the cost may be paid at all.
        var admission = offerQueries.ActionAdmission(
            world, card, found, ability.Player, occurrence);
        if (admission is null)
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' cannot initiate this action in the current state");
        }
        foreach (var thwart in admission.CrisisIgnoringThwarts)
        {
            cast.ValidateCrisisIgnoringThwart(thwart);
        }
        if (!CounterCostsPayable(world, card, ability.Player, found.Cost))
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' can no longer pay this ability's counter cost");
        }

        // `rr:initiating-abilities` keeps the steps apart, and step 5 pays
        // before step 6 resolves.
        var costOwner = world.Agenda.Current;
        var costOccurrence = world.Agenda.Occurrence;
        var arrowPayment = AbilityCostPayment.Prepare(
            world, card, cast.Player, found.Cost, paying, chosen,
            program, resourceAbilities, values,
            resourcesPaidByEvent: world.Facts.Kind(card.FaceId) == CardKind.Event
                && ResourceRequirement(found.Cost, card).Length > 0);
        var eventPayment = AbilityEventPayment.Prepare(
            world, card, cast.Player, paying, found.Effect, resourceAbilities,
            allocations, found.Cost);
        if (eventPayment is not null)
        {
            cast.PaidWith(eventPayment.Commit(occurrence, events));
        }
        ApplyPayment(arrowPayment.Commit(cardPlayAbilities, cast.Trigger, events), cast);
        if (cast.Suspended)
        {
            SuspendAfterCost(cast, ability.Ordinal, costOwner, costOccurrence);
            return events;
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

    /// <summary>The exact same-timing ability named by a pending ordinal.</summary>
    private CompiledCardAbility Pending(Card card, PendingAbility pending) =>
        On(card)
            .Where(candidate => candidate.Trigger.Timing == pending.Type)
            .ElementAtOrDefault(pending.Ordinal)
        ?? throw new AbilityException(
            $"card '{card.FaceId}' has no '{pending.Type}' ability at ordinal "
            + pending.Ordinal);

}
