using static Marvel.Cards.Run.AbilityProjectedDefeat;
using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityRuntimeQueries;
using static Marvel.Cards.Run.AbilityProjectedStateQueries;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Traces area mutations across every reachable resolution order.</summary>
internal sealed class AbilityAreaProjection
{
    internal readonly IReadOnlyList<AbilityEffect> effects;
    internal readonly AbilityCost? cost;
    internal readonly IReadOnlySet<DeckType> queried;
    internal AbilityAdmissionContext context;
    internal readonly long baseMultiplier;
    internal bool couldDiscard;
    internal readonly Dictionary<int, long> removedCounters = [];

    internal AbilityAreaProjection(
        IReadOnlyList<AbilityEffect> effects,
        AbilityCost? cost,
        IReadOnlySet<DeckType> queried,
        AbilityAdmissionContext context,
        long baseMultiplier)
    {
        this.effects = effects;
        this.cost = cost;
        this.queried = queried;
        this.context = context;
        this.baseMultiplier = baseMultiplier;
    }


    internal void MarkDiscardedTree(AreaProjectionState state, Card root)
    {
        var pending = new Stack<Card>();
        pending.Push(root);
        while (pending.TryPop(out var card))
        {
            if (!state.Departed.Add(card.ObjectId))
            {
                continue;
            }
            foreach (var child in ProjectedHostedCards(
                         state, card.ObjectId).ToList())
            {
                pending.Push(child);
            }
        }
    }

    internal IEnumerable<Card> ProjectedHostedCards(
        AreaProjectionState state, int host) => context.World.Cards
        .Where(card => !state.Departed.Contains(card.ObjectId))
        .Where(card => state.Hosts.TryGetValue(card.ObjectId, out int projected)
            ? projected == host
            : card.Area.Host == host);

    internal void MarkHostedCardsDiscarded(AreaProjectionState state, int host)
    {
        foreach (var child in ProjectedHostedCards(state, host).ToList())
        {
            MarkDiscardedTree(state, child);
        }
    }

    internal bool HostedCardsChangeArea(AreaProjectionState state, int host) =>
        ProjectedHostedCards(state, host)
        .Any(card => DiscardTreeChangesArea(card));


    internal bool DiscardTreeChangesArea(Card root)
    {
        var pending = new Stack<Card>();
        var seen = new HashSet<int>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var card = pending.Pop();
            if (!seen.Add(card.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"'{context.Source.FaceId}' reaches a hosted-card cycle while "
                    + "projecting a discard");
            }
            var destination = card.Owner < 0
                ? DeckType.EncounterDiscardPile : DeckType.DiscardPile;
            if (queried.Contains(destination))
            {
                return true;
            }
            foreach (var child in context.World.Areas
                         .Where(area => area.Host == card.ObjectId)
                         .SelectMany(area => area.Cards))
            {
                pending.Push(child);
            }
        }
        return false;
    }



    internal void DealProjected(AreaProjectionState state, Card target, long amount, long repetitions) =>
        new AbilityAreaDamageProjection(this).DealProjected(state, target, amount, repetitions);

    internal List<AreaProjectionState> TraceSequence(
        IEnumerable<AbilityEffect> sequence,
        List<AreaProjectionState> states, long multiplier = 1)
    {
        foreach (var step in sequence)
        {
            states = Trace(step, states, baseMultiplier: multiplier);
        }
        return states;
    }

    internal List<AreaProjectionState> Trace(
        AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions = 1, long baseMultiplier = 1)
    {
        if (repetitions <= 0 || states.Count == 0)
        {
            return states;
        }













































        return TraceNumeric(effect, states, repetitions, baseMultiplier)
            ?? TraceModifiers(effect, states, repetitions, baseMultiplier)
            ?? TraceFlow(effect, states, repetitions, baseMultiplier)
            ?? TraceMovement(effect, states, repetitions, baseMultiplier)
            ?? TraceFallback(effect, states, repetitions, baseMultiplier);
    }























    private List<AreaProjectionState>? TraceNumeric(
        AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier) =>
        this.TryTraceDamage(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceMoveDamage(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceHeal(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceRemoveThreat(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceForEach(effect, states, repetitions, baseMultiplier);

    private List<AreaProjectionState>? TraceModifiers(
        AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier) =>
        this.TryTraceGiveStatus(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceGrantTrait(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceGrantField(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceEachPlayer(effect, states, repetitions, baseMultiplier);

    private List<AreaProjectionState>? TraceFlow(
        AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier) =>
        this.TryTraceAnd(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceConditional(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceDependent(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceChoice(effect, states, repetitions, baseMultiplier)
        ?? this.TryTracePower(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceThwartGroup(effect, states, repetitions, baseMultiplier);

    private List<AreaProjectionState>? TraceMovement(
        AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier) =>
        this.TryTraceDelayedStun(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceDelayedBody(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceDiscard(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceAttach(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceMoveCard(effect, states, repetitions, baseMultiplier)
        ?? this.TryTracePutIntoPlay(effect, states, repetitions, baseMultiplier)
        ?? this.TryTraceOpaqueAreaMutation(effect, states, repetitions, baseMultiplier);

    private List<AreaProjectionState> TraceFallback(
        AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (repetitions > 1)
        {
            for (long repeat = 0; repeat < repetitions; repeat++)
            {
                states = Trace(effect, states, 1, baseMultiplier);
            }
            return states;
        }
        return TraceSequence(
            ResolutionChildren(effect), states, baseMultiplier);
    }


    internal bool MayChange()
    {
        var initial = new AreaProjectionState(context);
        if (cost is not null) this.TraceCost(cost, initial);
        _ = TraceSequence(effects, [initial], baseMultiplier);
        return couldDiscard;

    }
}
