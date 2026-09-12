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

internal static class AbilityResolutionPowers
{
    internal static void ResolveCardAttack(this AbilityResolutionExecution execution,
        World world, CharacterAttack attack, Occurrence occurrence, List<GameEvent> events) =>
        execution.ResolvePower(
            world, attack.Source, attack.Enemy, attack.Player, attack.AbilityIndex,
            attack.PowerOrdinal, attack.ResumeFrom, attack.FinalStep,
            attack.Targets ?? [attack.Enemy], attack.Amount, null,
            attack.Trigger, attack.SurgeGained, occurrence, events,
            BasicPowers.AttackVerb, attack.AbilityPath, attack.AbilityFace,
            attack.AbilityResults, attack.AbilityOccurrence, attack.Discarded,
            attack.EachPlayerFrame, attack.FinalPlayer, attack.AbilityPlayer,
            attack.AbilityHasContinuation, attack.AbilityActor);

    /// <inheritdoc/>
    internal static void ResolveCardThwart(this AbilityResolutionExecution execution,
        World world, CharacterThwart thwart, Occurrence occurrence, List<GameEvent> events) =>
        execution.ResolvePower(
            world, thwart.Source, thwart.Scheme, thwart.Player, thwart.AbilityIndex,
            thwart.PowerOrdinal, thwart.ResumeFrom, thwart.FinalStep,
            thwart.Targets ?? [thwart.Scheme], thwart.Amount, thwart.ImminentThreat,
            thwart.Trigger, thwart.SurgeGained, occurrence, events,
            BasicPowers.ThwartVerb, thwart.AbilityPath, thwart.AbilityFace,
            thwart.AbilityResults, thwart.AbilityOccurrence, thwart.Discarded,
            thwart.EachPlayerFrame, thwart.FinalPlayer, thwart.AbilityPlayer,
            thwart.AbilityHasContinuation, thwart.AbilityActor);

    internal static void ResolvePower(this AbilityResolutionExecution execution,
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
            execution.program, source, abilityIndex, powerOrdinal, power, resumeFrom, abilityPath,
            abilityFace, eachPlayerFrame, finalPlayer);
        var ability = restored.Ability;
        var effect = restored.Body;
        var cast = CreatePowerState(
            world, source, ability, abilityOccurrence, occurrence, player, events,
            finalStep, power, powerAmount, imminentThreat, eventTrigger, targets,
            eachPlayerFrame, finalPlayer, abilityPlayer, abilityActor, surgeGained);
        cast.Choose(world.Cards[targetId]);
        execution.RestorePersisted(cast, discarded, abilityResults);
        if (execution.SuspendsPowerEffect(effect, cast))
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' suspends inside a {power.ToLowerInvariant()}, "
                + "which is not implemented");
        }
        int ordinal = restored.Ordinal;
        var continuationFrames = restored.Frames;
        cast.RestoreAbility(ordinal, continuationFrames, abilityFace);
        execution.RestoreAlteredFromFrames(cast, continuationFrames);
        cast.TrackResolution(ordinal);
        var attackModifiers = power == BasicPowers.AttackVerb
            ? execution.EventModifierEffects(cast, "attackDamage")
            : [];
        execution.Run(effect, cast);

        // A modifier to "an attack" lasts through every damage node belonging
        // to that attack, then is consumed once. This is deliberately at the
        // wrapper boundary rather than in generic dealDamage: one attack may
        // damage several characters, while a later wrapper is a later attack.
        foreach (var modifier in attackModifiers) world.Effects.Use(modifier);
        FinishAttack(world, player, power, cast, events);

        // The labelled power owns `chosen` while its effect runs. The outer
        // ability's earlier selection is a different binding and becomes
        // current again only when that outer continuation resumes.
        RestoreOuterChosen(world, source, abilityResults, cast);
        execution.CompletePower(world, source, restored, cast, ordinal);
    }

    private static AbilityResolutionState CreatePowerState(
        World world, Card source, CompiledCardAbility ability,
        Occurrence? abilityOccurrence, Occurrence occurrence, int player,
        List<GameEvent> events, bool finalStep, string power, long powerAmount,
        ThreatPlacement? imminentThreat, string eventTrigger,
        IReadOnlyList<int> targets, bool eachPlayerFrame, bool finalPlayer,
        int abilityPlayer, int abilityActor, bool surgeGained) =>
        new(world, source, abilityOccurrence ?? occurrence, player, events)
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

    private static void FinishAttack(
        World world, int player, string power, AbilityResolutionState cast,
        List<GameEvent> events)
    {
        if (power != BasicPowers.AttackVerb) return;
        var attacker = cast.PowerActor ?? world.Seats[player].IdentityCard;
        if (Keywords.Has(world, attacker, Keywords.Ranged, world.Facts)) return;
        foreach (var target in cast.Attacked.DistinctBy(card => card.ObjectId))
        {
            DamageAttacks.Retaliate(
                world, world.Facts, target, attacker, cast.Trigger, events);
        }
    }

    private static void RestoreOuterChosen(
        World world, Card source, IReadOnlyDictionary<string, long>? abilityResults,
        AbilityResolutionState cast)
    {
        if (AbilityContinuationCodec.ChosenBinding(
            world.Cards, abilityResults, source.FaceId) is not { } outerChosen) return;
        cast.RestorePersistedSelection(
            world.Cards[outerChosen.ObjectId], outerChosen.AreaId,
            outerChosen.Incarnation, overwriteChosen: true);
    }

    private static void CompletePower(
        this AbilityResolutionExecution execution, World world, Card source,
        DecodedPowerContinuation restored, AbilityResolutionState cast, int ordinal)
    {
        var next = AbilityContinuationCodec.AfterPower(
            execution.program, source, restored, execution.Capture(cast, ordinal),
            world.Agenda.Current?.Round ?? 0, cast.Suspended);
        if (next is not null)
        {
            _ = execution.ResumeContinuation(cast, source, next);
            return;
        }
        cast.CompleteResolution();
        execution.DiscardEvent(source, cast);
    }

    /// <inheritdoc/>
    internal static IReadOnlyList<PendingAbility> Waiting(this AbilityResolutionExecution execution,
        World world, Occurrence occurrence, WindowKind window)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);
        return [.. AbilityWindowAdmission.Waiting(
                execution.program, world, occurrence, window, execution.resourceAbilities)
            .Select(candidate => new PendingAbility(
                candidate.Card.ObjectId, candidate.Ability.Trigger.Timing,
                candidate.Controller, candidate.Ordinal))];
    }

    /// <inheritdoc/>
    internal static Affordance Describe(this AbilityResolutionExecution execution, World world, PendingAbility ability)
    {
        ArgumentNullException.ThrowIfNull(world);

        var card = world.Cards[ability.Card];
        var found = execution.Pending(card, ability);

        // The ability's own name is the verb: an affordance for Foresight is
        // offered as `Foresight`, so a client has something to render without
        // knowing what the ability does. One string does for both fields
        // because the engine carries one -- see the remarks on `Affordance.Id`.
        var price = AbilityPaymentPricing.CombinedPrice(
            world, card, ability.Player, found, execution.resourceAbilities);
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
    internal static IReadOnlyList<GameEvent> Resolve(this AbilityResolutionExecution execution,
        World world, Occurrence occurrence, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen) =>
        execution.Resolve(world, occurrence, ability, paying, chosen, values: null, allocations: null);

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> Resolve(this AbilityResolutionExecution execution,
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
        var found = execution.Pending(card, ability);
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
        var cast = CreateAbilityState(world, card, occurrence, resolving, events, found);
        EnsureAvailable(execution, world, card, found, occurrence);
        if (!CanPayAbility(execution, world, card, found, resolving)) return events;
        if (!CanInitiateAbility(execution, card, found, resolving, cast)) return events;

        // `rr:initiating-abilities` keeps the steps apart, and step 5 pays
        // before step 6 resolves. Nothing here can abort for want of resources,
        // because step 3 -- `Payable`, when the ability was offered -- already
        // asked whether the cost could be paid at all. What it cannot check is
        // that the player named a payment that works, and `CardPlay.Spend`
        // refuses one that does not.
        if (!execution.PayAbility(
            world, card, found, ability, occurrence, paying, chosen,
            values, allocations, cast, events)) return events;
        execution.ExecuteAbility(world, card, found, ability, occurrence, cast);
        return events;
    }

    private static AbilityResolutionState CreateAbilityState(
        World world, Card card, Occurrence occurrence, int resolving,
        List<GameEvent> events, CompiledCardAbility ability)
    {
        var cast = new AbilityResolutionState(world, card, occurrence, resolving, events)
        {
            Tier = ability.Trigger.Timing,
        };
        return cast.ForReachability(cast.Reachability with
        {
            PaymentMayMutate = ability.Cost is not null
                || world.Facts.Kind(card.FaceId) == CardKind.Event,
            PaymentCost = ability.Cost,
        });
    }

    private static void EnsureAvailable(
        AbilityResolutionExecution execution, World world, Card card,
        CompiledCardAbility ability, Occurrence occurrence)
    {
        if (AbilityAvailability.Available(
            world, card, ability,
            AbilityAvailability.IndexOf(execution.program, card, ability), occurrence)) return;
        throw new RulesNotImplementedException(
            $"'{card.FaceId}' has reached its printed maximum for this ability's period");
    }

    private static bool CanPayAbility(
        AbilityResolutionExecution execution, World world, Card card,
        CompiledCardAbility ability, int resolving)
    {
        // Forced abilities pay their printed arrow cost, but the timing window
        // cannot choose a non-automatic payment on a player's behalf.
        if (AbilityTypes.IsMandatory(ability.Trigger.Timing) && ability.Cost is not null)
        {
            if (!MandatoryCostIsAutomatic(ability.Cost))
            {
                throw new RulesNotImplementedException(
                    $"'{card.FaceId}' has a mandatory ability whose "
                    + $"'{ability.Cost.OperationName()}' cost requires a player decision");
            }
            return Payable(
                world, card, resolving, ability.Cost,
                execution.program, execution.resourceAbilities);
        }
        if (CounterCostsPayable(world, card, resolving, ability.Cost)) return true;
        throw new RulesNotImplementedException(
            $"'{card.FaceId}' can no longer pay this ability's cost");
    }

    private static bool CanInitiateAbility(
        AbilityResolutionExecution execution, Card card,
        CompiledCardAbility ability, int resolving, AbilityResolutionState cast)
    {
        if (resolving < 0 || execution.CanInitiate(ability, cast)) return true;
        // A mandatory ability with no target is reached but does nothing.
        if (AbilityTypes.IsMandatory(ability.Trigger.Timing)) return false;
        throw new RulesNotImplementedException(
            $"'{card.FaceId}' cannot initiate this ability in the current state");
    }

    private static bool PayAbility(
        this AbilityResolutionExecution execution, World world, Card card,
        CompiledCardAbility found, PendingAbility address, Occurrence occurrence,
        IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values,
        IReadOnlyList<ResourceAllocation>? allocations,
        AbilityResolutionState cast, List<GameEvent> events)
    {
        var costOwner = world.Agenda.Current;
        var costOccurrence = world.Agenda.Occurrence;
        var arrowPayment = AbilityCostPayment.Prepare(
            world, card, cast.Player, found.Cost, paying, chosen,
            execution.program, execution.resourceAbilities, values,
            resourcesPaidByEvent: world.Facts.Kind(card.FaceId) == CardKind.Event
                && ResourceRequirement(found.Cost, card).Length > 0);
        var eventPayment = AbilityEventPayment.Prepare(
            world, card, cast.Player, paying, found.Effect,
            execution.resourceAbilities, allocations, found.Cost);
        if (eventPayment is not null)
            cast.PaidWith(eventPayment.Commit(occurrence, events));
        execution.ApplyPayment(
            arrowPayment.Commit(execution.cardPlayAbilities, cast.Trigger, events), cast);
        if (!cast.Suspended) return true;
        execution.SuspendAfterCost(cast, address.Ordinal, costOwner, costOccurrence);
        return false;
    }

    private static void ExecuteAbility(
        this AbilityResolutionExecution execution, World world, Card card,
        CompiledCardAbility found, PendingAbility address, Occurrence occurrence,
        AbilityResolutionState cast)
    {
        execution.Use(world, card, found, occurrence);
        if (world.Facts.Kind(card.FaceId) == CardKind.Event)
            occurrence.BeginCard(card.ObjectId, [address]);
        cast.RestoreAbility(address.Ordinal, []);
        cast.TrackResolution(address.Ordinal);
        execution.Run(found, cast);
        cast.CompleteResolution();
        execution.DiscardEvent(card, cast);
    }
}
