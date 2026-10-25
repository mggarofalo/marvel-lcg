using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

/// <summary>Moves only the active relation on independently projected encounter boards.</summary>
internal static class AbilityActiveCounterProjection
{
    internal static bool CanMove(World world, int active, Card? destination,
        IReadOnlyDictionary<int, int>? stages, IReadOnlySet<int> departed)
    {
        if (world.EncounterTopology is null || stages is null)
            throw new RulesNotImplementedException("active-counter projection requires encounter topology and stages");
        if (destination is null || destination.ObjectId == active || departed.Contains(active)
            || departed.Contains(destination.ObjectId)) return false;
        var live = world.EncounterTopology.Groups.Select(group =>
            stages.TryGetValue(group.Villain.ObjectId, out int id) ? id
                : throw new RulesNotImplementedException("active-counter projection is missing an encounter group"));
        var ids = live.Where(id => id >= 0).ToArray();
        return ids.Contains(active) && ids.Contains(destination.ObjectId)
            && ReferenceEquals(world.Cards[destination.ObjectId], destination);
    }

    internal static bool CanMovePower(AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state) =>
        CanMove(cast.World, state.CurrentVillain,
            AbilityPowerSelectionTrace.PowerFind(((AbilityEffect.CardAction)node).Selection, cast, state),
            state.EncounterStages, state.Discarded);

    internal static AbilityPowerState MovePower(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var destination = AbilityPowerSelectionTrace.PowerFind(
            ((AbilityEffect.CardAction)node).Selection, cast, state);
        return CanMove(cast.World, state.CurrentVillain, destination, state.EncounterStages, state.Discarded)
            ? state with { CurrentVillain = destination!.ObjectId } : state;
    }

    internal static bool CanMoveArea(
        AbilityEffect node, AreaProjectionState state, AbilityAdmissionContext context)
    {
        var destination = AbilityProjectedStateQueries.ProjectedFind(
            ((AbilityEffect.CardAction)node).Selection, state, context);
        return CanMove(context.World, state.ActiveVillain, destination, AreaStages(state, context), state.Departed);
    }

    internal static List<AreaProjectionState>? TraceArea(
        AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states)
    {
        if (effect is not AbilityEffect.CardAction { Instruction: AbilityCardInstruction.MoveActiveCounter } action)
            return null;
        foreach (var state in states)
        {
            if (CanMoveArea(effect, state, projection.context))
            {
                // A singular "villain" query observes the active relation, even
                // though moving its counter does not move the physical cards.
                projection.couldDiscard |= projection.queried.Contains(DeckType.VillainArea);
                state.ActiveVillain = AbilityProjectedStateQueries.ProjectedFind(
                    action.Selection, state, projection.context)!.ObjectId;
            }
        }
        return states;
    }

    private static Dictionary<int, int> AreaStages(AreaProjectionState state, AbilityAdmissionContext context) =>
        (context.World.EncounterTopology
            ?? throw new RulesNotImplementedException("active-counter projection requires encounter topology"))
        .Groups.ToDictionary(group => group.Villain.ObjectId,
            group => AbilityAreaEncounterProjection.Current(context.World, state, group.Villain)?.ObjectId ?? -1);
}
