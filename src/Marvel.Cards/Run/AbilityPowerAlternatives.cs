using static Marvel.Cards.Run.AbilityPowerAlternatives;
using static Marvel.Cards.Run.AbilityRepeatedCharacteristicTrace;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityEffectStructure;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using PowerReachability = Marvel.Rules.Play.RuleProjection<Marvel.Cards.Run.AbilityPowerState>;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

using static Marvel.Cards.Run.AbilityPowerStateProjection;
namespace Marvel.Cards.Run;

internal static class AbilityPowerAlternatives
{
    internal static PowerReachability MergePowerStates(
        PowerReachability left, PowerReachability right, AbilityAdmissionScope cast) =>
        MergePowerAlternatives([left, right]);

    internal static PowerReachability MergePowerAlternatives(
        IEnumerable<PowerReachability> states)
    {
        var alternatives = new List<AbilityPowerState>();
        foreach (var projection in states)
        {
            if (projection is PowerReachability.Unsupported)
            {
                return projection;
            }
            foreach (var state in PowerPaths(projection))
            {
                if (!alternatives.Any(existing => SameConcretePowerState(existing, state)))
                {
                    alternatives.Add(state);
                }
            }
        }
        if (alternatives.Count == 0)
        {
            throw new InvalidOperationException("A power state must have a reachable path.");
        }
        return alternatives.Count == 1
            ? new PowerReachability.Known(alternatives[0])
            : new PowerReachability.Possible([.. alternatives]);
    }

    internal static ImmutableArray<AbilityPowerState> PowerPaths(
        PowerReachability state) => state switch
        {
            PowerReachability.Known known => [known.Value],
            PowerReachability.Possible possible => possible.Alternatives,
            PowerReachability.Unsupported unsupported =>
                throw new RulesNotImplementedException(unsupported.Reason),
            _ => throw new InvalidOperationException("Unknown power projection outcome."),
        };

    internal static ulong PowerForms(PowerReachability state) =>
        PowerPaths(state).Aggregate(0UL, (forms, path) => forms | path.FormsMayChange);

    internal static bool SamePowerState(
        PowerReachability left, PowerReachability right)
    {
        var leftPaths = PowerPaths(left).ToList();
        var rightPaths = PowerPaths(right).ToList();
        return leftPaths.Count == rightPaths.Count
            && leftPaths.All(path => rightPaths.Any(other =>
                SameConcretePowerState(path, other)));
    }

    internal static bool SameConcretePowerState(
        AbilityPowerState left, AbilityPowerState right) =>
        SamePowerHeader(left, right)
        && SamePowerCardState(left, right)
        && SamePowerBoardState(left, right);

    private static bool SamePowerHeader(
        AbilityPowerState left, AbilityPowerState right) =>
        left.FormsMayChange == right.FormsMayChange
        && left.FirstPlayer == right.FirstPlayer
        && left.FirstPlayerDamage == right.FirstPlayerDamage
        && left.FirstPlayerTough == right.FirstPlayerTough;

    private static bool SamePowerCardState(
        AbilityPowerState left, AbilityPowerState right) =>
        SameValues(left.CardDamage, right.CardDamage)
        && SameValues(left.CardTough, right.CardTough)
        && left.StatusChanges.SetEquals(right.StatusChanges)
        && SameValues(left.StatusCounts, right.StatusCounts)
        && SameValues(left.CardReadiness, right.CardReadiness)
        && left.Discarded.SetEquals(right.Discarded);

    private static bool SamePowerBoardState(
        AbilityPowerState left, AbilityPowerState right) =>
        SameValues(left.SchemeThreat, right.SchemeThreat)
        && SameValues(left.PlayerCardsAvailable, right.PlayerCardsAvailable)
        && SameValues(left.Modifiers, right.Modifiers)
        && SameTraits(left, right)
        && SameValues(left.Engagement, right.Engagement)
        && left.CurrentVillain == right.CurrentVillain
        && left.VillainStagesDrawn == right.VillainStagesDrawn
        && left.Finished == right.Finished
        && SameUnprojectedReads(left, right);

    private static bool SameUnprojectedReads(AbilityPowerState left, AbilityPowerState right) =>
        left.EncounterCardsChanged == right.EncounterCardsChanged
        && left.DefeatWindowDeferred == right.DefeatWindowDeferred
        && left.UnprojectedStageEntry == right.UnprojectedStageEntry;

    private static bool SameTraits(AbilityPowerState left, AbilityPowerState right) =>
        left.Traits.Count == right.Traits.Count && left.Traits.All(pair =>
            right.Traits.TryGetValue(pair.Key, out var traits) && pair.Value.SetEquals(traits));

    private static bool SameValues<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> left,
        IReadOnlyDictionary<TKey, TValue> right)
        where TKey : notnull =>
        left.Count == right.Count
        && left.All(pair => right.TryGetValue(pair.Key, out var value)
            && EqualityComparer<TValue>.Default.Equals(pair.Value, value));

}
