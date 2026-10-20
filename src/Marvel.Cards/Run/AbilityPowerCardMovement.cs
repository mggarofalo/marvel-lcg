using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityPowerHealthTrace;
using static Marvel.Cards.Run.AbilityPowerOutcomeTrace;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityResolutionAdmission;

namespace Marvel.Cards.Run;

internal static class AbilityPowerCardMovement
{
    internal static AbilityPowerState Enter(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var entry = EffectOf<AbilityEffect.PutIntoPlay>(node, cast);
        var card = Find(entry.Card, cast);
        if (card is null) return state;
        if (AbilityProgramQueries.On(cast.Context.Program, card).Any(ability =>
                ability.Trigger.Timing == AbilityType.Constant))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' puts '{card.FaceId}' into play before "
                + "a labelled-power continuation reads its constant abilities, "
                + "which is not implemented");
        }
        var discarded = new HashSet<int>(state.Discarded);
        if (!discarded.Remove(card.ObjectId)) return state;
        var engagement = new Dictionary<int, int>(state.Engagement);
        if (!entry.PrintedDestination && !entry.AllyUnderYourControl)
            engagement[card.ObjectId] = entry.FirstPlayer ? cast.World.FirstPlayer : Resolver(cast);
        state = state with { Discarded = discarded, Engagement = engagement };
        if (StateFields.Modified(
                cast.World, card, "toughness", cast.World.Facts,
                cast.World.Players) <= 0) return state;
        var first = cast.World.Seats[cast.World.FirstPlayer].IdentityCard;
        return SetPowerTough(state, card, true, first, cast);
    }

    internal static AbilityPowerState Draw(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var draw = EffectOf<AbilityEffect.Draw>(node, cast);
        foreach (int player in Seats(draw.Players, cast))
        {
            long available = PowerCardsAvailable(state, player, cast);
            state = SetPowerCardsAvailable(
                state, player, Math.Max(0, available - draw.Count), cast);
        }
        return state;
    }

    internal static AbilityPowerState Discard(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var card = PowerFind(
            EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast, state);
        if (card is null) return state;
        var discarded = new HashSet<int>(state.Discarded);
        var engagement = new Dictionary<int, int>(state.Engagement);
        var counts = new Dictionary<(int Card, string Status), int>(state.StatusCounts);
        var changes = new HashSet<(int Card, string Status)>(state.StatusChanges);
        foreach (int leaving in PowerLeavingTree(card, cast))
        {
            discarded.Add(leaving);
            TraceStatusesLeave(leaving, cast, counts, changes);
            engagement.Remove(leaving);
        }
        return state with
        {
            Discarded = discarded, Engagement = engagement,
            StatusCounts = counts, StatusChanges = changes,
        };
    }

}
