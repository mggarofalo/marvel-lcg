using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingRankedQueryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProjectedRanksUseTheirOwnThreatAndStageMembership(bool maximum)
    {
        // Synthetic projected boards distinguish the 07013/07028/07040/07057
        // "villain whose side scheme has the least [or most] threat" relation.
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        int[] live = maximum ? [1, 9, 2, 3] : [9, 1, 8, 7];
        int[] projected = maximum ? [8, 1, 8, 2] : [2, 9, 2, 8];
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat", live[index]);
        var selector = new AbilityCardSelection.Query(maximum
            ? AbilityCardQuery.VillainsWithMostSchemeThreat : AbilityCardQuery.VillainsWithLeastSchemeThreat);
        var power = AbilityPowerStateFactory.Initial(scope);
        var area = new AreaProjectionState(scope.Context);
        for (int index = 0; index < groups.Length; index++)
        {
            power.SchemeThreat[groups[index].Scheme.ObjectId] = projected[index];
            area.Threat[groups[index].Scheme.ObjectId] = projected[index];
        }
        power.EncounterStages![groups[0].Villain.ObjectId] = groups[0].Stages[1].ObjectId;
        power = power with { EncounterOrder = [groups[1].Villain.ObjectId, groups[2].Villain.ObjectId,
            groups[3].Villain.ObjectId, groups[0].Stages[1].ObjectId] };
        power.Discarded.Add(groups[0].Villain.ObjectId);
        area.Departed.Add(groups[0].Villain.ObjectId);
        area.Entered.Add(groups[0].Stages[1].ObjectId);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            world.Agenda.Outstanding.ToArray());

        Assert.Equal(new[] { groups[1].Villain }, AbilityInitiationPrimitives.Every(selector, scope));
        Card[] expected = [groups[0].Stages[1], groups[2].Villain];
        Assert.Equal(expected, AbilityPowerSelectionTrace.PowerEvery(selector, scope, power));
        Assert.Equal(expected, AbilityProjectedStateQueries.ProjectedEvery(selector, area, scope.Context));
        var trace = new AbilitySelectorTraceContext(power.CurrentVillain, scope, power.Discarded,
            power.Traits, power.Modifiers, power.Engagement)
        { EncounterStages = power.EncounterStages, SchemeThreat = power.SchemeThreat,
            EncounterOrder = power.EncounterOrder };
        Assert.Equal(expected, AbilitySelectorTrace.MatchingCandidates(selector, trace));
        var last = new AbilityCardSelection.Last(selector);
        Assert.Equal(new[] { groups[2].Villain }, AbilityPowerSelectionTrace.PowerEvery(last, scope, power));
        Assert.Equal(new[] { groups[2].Villain },
            AbilityProjectedStateQueries.ProjectedEvery(last, area, scope.Context));
        Assert.Equal(new[] { groups[2].Villain }, AbilitySelectorTrace.MatchingCandidates(last, trace));

        Assert.Equal(before.Item1, world.Digest().Canonical());
        Assert.Equal(before.Item2, world.Random.Generator.WordsConsumed);
        Assert.Equal(before.Item3, world.Agenda.Outstanding);
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    [Fact]
    public void DefeatedGroupCannotWinRankAndAnUnpairedLivingGroupRaises()
    {
        var (_, groups, scope) = WreckingProjectionFixture.Create();
        var selector = new AbilityCardSelection.Query(AbilityCardQuery.VillainsWithMostSchemeThreat);
        var power = AbilityPowerStateFactory.Initial(scope);
        var area = new AreaProjectionState(scope.Context);
        power.SchemeThreat[groups[0].Scheme.ObjectId] = 100;
        area.Threat[groups[0].Scheme.ObjectId] = 100;
        power.EncounterStages![groups[0].Villain.ObjectId] = -1;
        power.Discarded.Add(groups[0].Villain.ObjectId);
        power.Discarded.Add(groups[0].Scheme.ObjectId);
        area.Departed.UnionWith([groups[0].Villain.ObjectId, groups[0].Scheme.ObjectId]);

        var expected = groups.Skip(1).Select(group => group.Villain).ToArray();
        Assert.Equal(expected, AbilityPowerSelectionTrace.PowerEvery(selector, scope, power));
        Assert.Equal(expected, AbilityProjectedStateQueries.ProjectedEvery(selector, area, scope.Context));
        power.Discarded.Add(groups[1].Scheme.ObjectId);
        area.Departed.Add(groups[1].Scheme.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => AbilityPowerSelectionTrace.PowerEvery(selector, scope, power));
        Assert.Throws<RulesNotImplementedException>(() =>
            AbilityProjectedStateQueries.ProjectedEvery(selector, area, scope.Context));
    }

    [Fact]
    public void MissingProjectedThreatCannotFallBackToLiveRank()
    {
        var (_, groups, scope) = WreckingProjectionFixture.Create();
        var state = AbilityPowerStateFactory.Initial(scope);
        var trace = new AbilitySelectorTraceContext(state.CurrentVillain, scope, [], [], [], [])
        { EncounterStages = state.EncounterStages };
        Assert.Throws<RulesNotImplementedException>(() => AbilityRepeatedSelectorTrace.TraceQueryMatches(
            AbilityCardQuery.VillainsWithLeastSchemeThreat, groups[0].Villain, trace));
    }
}
