using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class TacticalProwessChoiceTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [Rule("rr:first-player.1")]
    [Rule("rr:move.1")]
    public void FirstPlayerSelectsDistinctRankedEndpointsIncludingAllEqual(int firstPlayer, bool equal)
    {
        // "The first player selects among the eligible options."
        // An element "cannot move to its same (current) placement".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07031", 2, players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.FirstPlayer = firstPlayer;
        int[] threat = equal ? [2, 2, 2, 2] : [2, 2, 5, 5];
        for (int index = 0; index < groups.Length; index++) groups[index].Scheme.PlaceTokens("k_threat", threat[index]);
        int player = 1 - firstPlayer;
        runner.ExecuteReveal(world, source, player);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        Assert.NotNull(prompt);
        Assert.Equal(firstPlayer, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.Equal(equal ? 12 : 4, prompt.Affordances.Count);
        Assert.All(prompt.Affordances, offer => Assert.True(offer.IsLegal));
        Assert.Equal(threat, groups.Select(group => checked((int)group.Scheme.Tokens["k_threat"])));
        int answer = equal ? 0 : 3;
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(answer), []);
        WreckingSchemeFixture.Finish(world, runner);
        int[] expected = equal ? [0, 4, 2, 2] : [2, 0, 5, 7];
        Assert.Equal(expected, groups.Select(group => checked((int)group.Scheme.Tokens.GetValueOrDefault("k_threat"))));
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)).Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(firstPlayer)).Cards);
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    [Fact]
    [Rule("rr:move.2")]
    public void OneRemainingSchemeCannotMoveToItselfAndStillSurges()
    {
        // "If there is no valid source or destination ... the move cannot be made."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07031", 0);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        foreach (var group in groups.Take(3)) World.MoveToTop(group.Scheme, group.Discard);
        groups[3].Scheme.PlaceTokens("k_threat", 4);
        runner.ExecuteReveal(world, source, 0);
        WreckingSchemeFixture.Finish(world, runner);
        Assert.Equal(4, groups[3].Scheme.Tokens["k_threat"]);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }
}
