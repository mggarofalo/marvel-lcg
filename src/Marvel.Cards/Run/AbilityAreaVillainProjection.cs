using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Projects villain departures, replacement stages and carried state.</summary>
internal sealed class AbilityAreaVillainProjection(AbilityAreaProjection projection)
{
    private AbilityAdmissionContext context => projection.context;

    internal bool RootLeavesOnDefeat(Card root)
    {
        var kind = EffectiveCards.Kind(root, context.World.Facts);
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
        var next = context.World.EncounterTopology is null
            ? villainDeck.Count > 0 ? villainDeck[^1] : null
            : AbilityAreaEncounterProjection.Next(context.World, root);
        return next is null || !string.Equals(
            context.World.Facts.Title(root.FaceId),
            context.World.Facts.Title(next.FaceId),
            StringComparison.Ordinal);
    }

    private Card? NextVillainStage(AreaProjectionState state, Card target) =>
        context.World.EncounterTopology is null
            ? context.World.AreaOf(DeckType.VillainDeck).Cards
                .LastOrDefault(card => !state.Entered.Contains(card.ObjectId))
            : AbilityAreaEncounterProjection.Next(context.World, target);

    internal Card ProjectedDamageTarget(AreaProjectionState state, Card target) =>
        context.World.EncounterTopology is not null
            ? AbilityAreaEncounterProjection.Current(context.World, state, target) ?? target
            :         CardKinds.IsVillain(EffectiveCards.Kind(target, context.World.Facts))
            && state.ActiveVillain >= 0
                ? context.World.Cards[state.ActiveVillain]
                : target;

    internal void DefeatProjectedVillain(AreaProjectionState state, Card target)
    {
        var next = NextVillainStage(state, target);
        bool carries = next is not null && string.Equals(
            context.World.Facts.Title(target.FaceId),
            context.World.Facts.Title(next.FaceId),
            StringComparison.Ordinal);
        int attachmentHost = context.World.EncounterTopology is null && state.VillainAttachmentHost >= 0
            ? state.VillainAttachmentHost : target.ObjectId;
        if (!carries)
        {
            projection.couldDiscard |= projection.HostedCardsChangeArea(state, attachmentHost);
            projection.MarkHostedCardsDiscarded(state, attachmentHost);
        }
        state.Departed.Add(target.ObjectId);
        if (context.World.EncounterTopology is null)
            state.ActiveVillain = next?.ObjectId ?? -1;
        else
            AbilityAreaEncounterProjection.Advance(projection, state, target, next);
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
        foreach (var attachment in projection.ProjectedHostedCards(state, attachmentHost).ToList())
        {
            state.Hosts[attachment.ObjectId] = next.ObjectId;
        }
        state.VillainAttachmentHost = next.ObjectId;
    }
}
