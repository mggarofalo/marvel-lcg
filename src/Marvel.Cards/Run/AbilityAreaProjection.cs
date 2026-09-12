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

    private bool DiscardedByDamage(AreaProjectionState state, Card target) =>
        state.DamageOf(target) >= state.HealthOf(context, target)
        && DefeatTreeChangesArea(target, queried, context);

    private bool RootLeavesOnDefeat(Card root)
    {
        var kind = context.World.Facts.Kind(root.FaceId);
        if (kind is CardKind.Minion or CardKind.Ally
            or CardKind.EncounterSideScheme)
        {
            return true;
        }
        if (!CardKinds.IsVillain(kind))
        {
            return false;
        }
        var villainDeck = context.World.AreaOf(DeckType.VillainDeck).Cards;
        var next = villainDeck.Count > 0 ? villainDeck[^1] : null;
        return next is null || !string.Equals(
            context.World.Facts.Title(root.FaceId),
            context.World.Facts.Title(next.FaceId),
            StringComparison.Ordinal);
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

    private IEnumerable<Card> ProjectedHostedCards(
        AreaProjectionState state, int host) => context.World.Cards
        .Where(card => !state.Departed.Contains(card.ObjectId))
        .Where(card => state.Hosts.TryGetValue(card.ObjectId, out int projected)
            ? projected == host
            : card.Area.Host == host);

    private void MarkHostedCardsDiscarded(AreaProjectionState state, int host)
    {
        foreach (var child in ProjectedHostedCards(state, host).ToList())
        {
            MarkDiscardedTree(state, child);
        }
    }

    internal bool HostedCardsChangeArea(AreaProjectionState state, int host) =>
        ProjectedHostedCards(state, host)
        .Any(card => DiscardTreeChangesArea(card));

    private bool DefeatedHostsCardsChangeArea(
        AreaProjectionState state, int host) =>
        ProjectedHostedCards(state, host).Any(card =>
        {
            bool movesToVictory = context.World.Facts.Kind(card.FaceId) is
                    CardKind.Attachment or CardKind.Upgrade
                && Keywords.Has(
                    context.World, card, "victory", context.World.Facts);
            return movesToVictory
                ? ProjectedHostedCards(state, card.ObjectId)
                    .Any(child => DiscardTreeChangesArea(child))
                : DiscardTreeChangesArea(card);
        });

    private Card? NextVillainStage(AreaProjectionState state) =>
        context.World.AreaOf(DeckType.VillainDeck).Cards
            .LastOrDefault(card => !state.Entered.Contains(card.ObjectId));

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

    internal void DealProjected(
        AreaProjectionState state, Card target, long amount,
        long repetitions)
    {
        target = ProjectedDamageTarget(state, target);
        if (!CanDealProjectedDamage(state, target, amount, repetitions))
        {
            return;
        }

        long prevented = Math.Min(state.ToughOf(context, target), repetitions);
        state.Tough[target.ObjectId] = state.ToughOf(context, target) - prevented;
        long dealt = AbilityAmounts.SaturatingMultiply(amount, repetitions - prevented);
        state.Damage[target.ObjectId] = AbilityAmounts.SaturatingSum(
            state.DamageOf(target), [dealt]);
        couldDiscard |= DiscardedByDamage(state, target);
        if (state.DamageOf(target) < state.HealthOf(context, target))
        {
            return;
        }

        if (CardKinds.IsVillain(context.World.Facts.Kind(target.FaceId)))
        {
            DefeatProjectedVillain(state, target);
        }
        else if (RootLeavesOnDefeat(target))
        {
            couldDiscard |= DefeatedHostsCardsChangeArea(state, target.ObjectId);
            MarkDiscardedTree(state, target);
        }
    }

    private Card ProjectedDamageTarget(AreaProjectionState state, Card target) =>
        CardKinds.IsVillain(context.World.Facts.Kind(target.FaceId))
            && state.ActiveVillain >= 0
                ? context.World.Cards[state.ActiveVillain]
                : target;

    private bool CanDealProjectedDamage(
        AreaProjectionState state, Card target, long amount, long repetitions) =>
        amount > 0
        && repetitions > 0
        && !state.Departed.Contains(target.ObjectId)
        && AbilityProgramQueries.CanTakeDamage(
            context.World, context.Program, target, context.Source);

    private void DefeatProjectedVillain(AreaProjectionState state, Card target)
    {
        var next = NextVillainStage(state);
        bool carries = next is not null && string.Equals(
            context.World.Facts.Title(target.FaceId),
            context.World.Facts.Title(next.FaceId),
            StringComparison.Ordinal);
        int attachmentHost = state.VillainAttachmentHost >= 0
            ? state.VillainAttachmentHost : target.ObjectId;
        if (!carries)
        {
            couldDiscard |= HostedCardsChangeArea(state, attachmentHost);
            MarkHostedCardsDiscarded(state, attachmentHost);
        }
        state.Departed.Add(target.ObjectId);
        state.ActiveVillain = next?.ObjectId ?? -1;
        if (next is null)
        {
            return;
        }

        EnterVillainStage(state, next);
        if (carries)
        {
            CarryVillainState(state, target, next, attachmentHost);
        }
    }

    private void EnterVillainStage(AreaProjectionState state, Card next)
    {
        state.Entered.Add(next.ObjectId);
        state.Damage[next.ObjectId] = 0;
        if (StateFields.Modified(
                context.World, next, "toughness",
                context.World.Facts, context.World.Players) > 0)
        {
            state.Tough[next.ObjectId] = Math.Max(1, state.ToughOf(context, next));
        }
    }

    private void CarryVillainState(
        AreaProjectionState state, Card target, Card next, int attachmentHost)
    {
        foreach (string status in new[] { Statuses.Tough, Statuses.Stunned, Statuses.Confused })
        {
            long carried = state.StatusOf(context, target, status);
            if (status == Statuses.Tough)
            {
                state.Tough[next.ObjectId] = Math.Max(
                    state.ToughOf(context, next), carried);
            }
            else
            {
                state.Status[(next.ObjectId, status)] = carried;
            }
        }
        foreach (var attachment in ProjectedHostedCards(state, attachmentHost).ToList())
        {
            state.Hosts[attachment.ObjectId] = next.ObjectId;
        }
        state.VillainAttachmentHost = next.ObjectId;
    }

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
