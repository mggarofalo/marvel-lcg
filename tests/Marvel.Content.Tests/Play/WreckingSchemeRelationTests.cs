using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingSchemeRelationTests
{
    [Rule("rr:move.1")]
    [Rule("rr:move.2")]
    [Theory]
    [InlineData(false, 0, 0, 2, 3, new[] { 0, 1 }, new[] { 0 })]
    [InlineData(false, 1, 0, 2, 3, new[] { 1 }, new int[] { })]
    [InlineData(false, 0, 2, 0, 3, new[] { 0, 2 }, new[] { 0, 2 })]
    [InlineData(true, 3, 3, 2, 0, new[] { 0, 1 }, new[] { 0 })]
    [InlineData(true, 2, 3, 1, 0, new[] { 1 }, new int[] { })]
    public void CounterMovementRanksBeforeExcludingItsCurrentHolder(
        bool maximum, int first, int second, int third, int fourth, int[] ranked, int[] destinations)
    {
        // rr:move.1: "it cannot move to its same (current) placement".
        // rr:move.2: "If there is no valid source or destination for a move, the move cannot be made."
        var (world, groups, _) = WreckingProjectionFixture.Create();
        int[] threat = [first, second, third, fourth];
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat", threat[index]);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);

        Assert.Equal(ranked.Select(index => groups[index].Villain),
            EncounterSchemeRelations.AtThreatExtreme(world, maximum));
        Assert.Equal(destinations.Select(index => groups[index].Villain),
            EncounterSchemeRelations.CounterDestinations(world, maximum));

        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    [Fact]
    public void CorrespondenceFollowsPhysicalPromotionAndNeverTheActiveCounterOrTitle()
    {
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var first = groups[0];
        var next = first.Stages[1];
        var foreign = world.CreateCard(first.Villain.FaceId, world.AreaOf(DeckType.VillainArea));
        Assert.Same(first.Scheme, EncounterSchemeRelations.Scheme(world, first.Villain));
        Assert.Same(first.Villain, EncounterSchemeRelations.Villain(world, first.Scheme));
        Assert.Throws<RulesNotImplementedException>(() => EncounterSchemeRelations.Scheme(world, foreign));
        World.MoveToTop(first.Villain, world.AreaOf(DeckType.RemovedArea));
        World.MoveToTop(next, world.AreaOf(DeckType.VillainArea));

        Assert.Same(first.Scheme, EncounterSchemeRelations.Scheme(world, next));
        Assert.Same(next, EncounterSchemeRelations.Villain(world, first.Scheme));
        Assert.Throws<RulesNotImplementedException>(() => EncounterSchemeRelations.Scheme(world, first.Villain));
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    [Fact]
    public void DefeatedGroupsAreExcludedButMissingLiveSchemeIsNotGuessed()
    {
        var (world, groups, _) = WreckingProjectionFixture.Create();
        groups[0].Scheme.PlaceTokens("k_threat", 100);
        World.MoveToTop(groups[0].Villain, world.AreaOf(DeckType.RemovedArea));
        World.MoveToTop(groups[0].Scheme, world.AreaOf(DeckType.RemovedArea));
        Assert.Equal(groups.Skip(1).Select(group => group.Villain),
            EncounterSchemeRelations.AtThreatExtreme(world, maximum: true));
        Assert.Throws<RulesNotImplementedException>(() => EncounterSchemeRelations.Villain(world, groups[0].Scheme));
        World.MoveToTop(groups[2].Scheme, world.AreaOf(DeckType.RemovedArea));
        Assert.Throws<RulesNotImplementedException>(() => EncounterSchemeRelations.AtThreatExtreme(world, maximum: true));
        Assert.Throws<RulesNotImplementedException>(() => EncounterSchemeRelations.Scheme(world, groups[2].Villain));
    }
}
