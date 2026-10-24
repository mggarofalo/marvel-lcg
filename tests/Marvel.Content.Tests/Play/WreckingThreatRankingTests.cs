using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingThreatRankingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:permanent.4")]
    public void RankingDoesNotImplyThatAProtectedSchemeMustLeavePlay(bool maximum)
    {
        // Permanent excludes effects "that would cause the permanent card to leave play".
        // Ranking a target for threat does not cause departure.
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        foreach (var group in groups)
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                "permanent", Amount: 1, Affects: group.Scheme.ObjectId));
        groups[0].Scheme.PlaceTokens("k_threat", 2);
        groups[1].Scheme.PlaceTokens("k_threat", 5);
        groups[2].Scheme.PlaceTokens("k_threat", 2);
        groups[3].Scheme.PlaceTokens("k_threat", 5);
        var selection = Selection(maximum);
        int[] expected = maximum ? [1, 3] : [0, 2];
        Assert.All(groups, group => Assert.False(Discard.EffectCanRemove(world, world.Facts,
            scope.Source, group.Scheme)));
        Assert.Equal(expected.Select(index => groups[index].Scheme),
            AbilityRuntimeQueries.Every(selection, scope.Context));
        Assert.Equal(expected.Select(index => groups[index].Scheme),
            AbilityAreaRankedSelection.Select(selection, new(scope.Context), scope.Context));
        Assert.Equal(expected.Select(index => groups[index].Scheme),
            AbilityPowerSelectionTrace.PowerEvery(selection, scope, AbilityPowerStateFactory.Initial(scope)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProjectedThreatChangesTheRankWithoutChangingTheLiveBoard(bool maximum)
    {
        var (_, groups, scope) = WreckingProjectionFixture.Create();
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat", index + 1);
        var selection = Selection(maximum);
        var area = new AreaProjectionState(scope.Context);
        var power = AbilityPowerStateFactory.Initial(scope);
        area.Threat[groups[0].Scheme.ObjectId] = 8;
        area.Threat[groups[3].Scheme.ObjectId] = 0;
        power.SchemeThreat[groups[0].Scheme.ObjectId] = 8;
        power.SchemeThreat[groups[3].Scheme.ObjectId] = 0;
        var expected = groups[maximum ? 0 : 3].Scheme;
        Assert.Same(expected, Assert.Single(AbilityAreaRankedSelection.Select(selection, area, scope.Context)));
        Assert.Same(expected, Assert.Single(AbilityPowerSelectionTrace.PowerEvery(selection, scope, power)));
        Assert.Equal(1, groups[0].Scheme.Tokens["k_threat"]);
        Assert.Equal(4, groups[3].Scheme.Tokens["k_threat"]);
    }

    [Fact]
    public void MissingThreatProjectionRaisesInsteadOfRankingTheLiveBoard()
    {
        var (_, groups, scope) = WreckingProjectionFixture.Create();
        var trace = new AbilitySelectorTraceContext(-1, scope, [], [], [], []);
        Assert.Throws<RulesNotImplementedException>(() =>
            AbilityRepeatedStatusTrace.TraceRankedCandidatesInclude(
                groups.Select(group => group.Scheme).ToList(), groups[0].Scheme,
                AbilityCardRank.Threat, true, trace));
    }

    [Fact]
    [Rule("rr:permanent.4.1")]
    public void AnExplicitDiscardablePoolRanksTheEligibleAlternative()
    {
        // Such an effect "instead targets the non-permanent card that fits its criteria".
        var (world, _, scope) = WreckingProjectionFixture.Create();
        var area = world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0);
        var permanent = world.CreateCard("01091", area);
        var eligible = world.CreateCard("01092", area);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "permanent", Amount: 1, Affects: permanent.ObjectId));
        var pool = new AbilityCardSelection.Query(AbilityCardQuery.SupportsYouControl);
        var unrestricted = new AbilityCardSelection.Ranked(pool, AbilityCardRank.Cost, true);
        var removable = unrestricted with { Cards = new AbilityCardSelection.Discardable(pool) };
        Assert.Same(permanent, Assert.Single(AbilityRuntimeQueries.Every(unrestricted, scope.Context)));
        Assert.Same(eligible, Assert.Single(AbilityRuntimeQueries.Every(removable, scope.Context)));
        Assert.Same(eligible, Assert.Single(AbilityAreaRankedSelection.Select(removable, new(scope.Context), scope.Context)));
        Assert.Same(eligible, Assert.Single(AbilityPowerSelectionTrace.PowerEvery(removable, scope,
            AbilityPowerStateFactory.Initial(scope))));
    }

    [Theory]
    [InlineData("minBy", 0)]
    [InlineData("maxBy", 3)]
    public void SyntheticThreatRankingPlacesThreatOnTheActualExtreme(string operation, int selected)
    {
        // Synthetic ability on a prepared actual-card board, not the printed Escape Plan effect.
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07043", 2);
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat", index + 1);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"07043","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"placeThreat":{"scheme":{"RANK":{"of":{"query":"sideSchemes"},"by":"threat"}},"amount":2}}
            }]}]}
            """.Replace("RANK", operation, StringComparison.Ordinal)));
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, 0);
        Sequence.Finish(world, world.Facts, runner, []);
        for (int index = 0; index < groups.Length; index++)
            Assert.Equal(index + 1 + (index == selected ? 2 : 0),
                groups[index].Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    private static AbilityCardSelection.Ranked Selection(bool maximum) =>
        new(new AbilityCardSelection.Query(AbilityCardQuery.SideSchemes), AbilityCardRank.Threat, maximum);
}
