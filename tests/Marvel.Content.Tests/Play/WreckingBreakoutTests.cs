using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingBreakoutTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void BreakoutChoosesTheLargestSchemeAfterItsThresholdResponsesFinish(int players)
    {
        // 07001b: place one on each side scheme, then move to the villain whose
        // scheme has the most threat. Day of Reckoning's forced response resets it first.
        var (world, groups, _) = WreckingEncounterCardFixture.Create("07037", 0, players: players);
        var runner = WreckingSchemeFixture.Runner();
        world.Abilities = runner;
        groups[0].Scheme.PlaceTokens("k_threat", 9);
        groups[1].Scheme.PlaceTokens("k_threat", 6);
        groups[2].Scheme.PlaceTokens("k_threat", 7);
        Begin(world, runner);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(new long[] { 3, 7, 8, 1 }, groups.Select(group => group.Scheme.Tokens["k_threat"]));
        Assert.All(world.Seats, seat => Assert.Equal(2, seat.IdentityCard.Damage));
        WreckingSchemeFixture.Finish(world, runner);
        Assert.Same(groups[2], world.EncounterTopology!.Active);
    }

    [Fact]
    public void BreakoutTiesBelongToFirstPlayerAndOfferAllDifferentLivingDestinations()
    {
        var (world, groups, _) = WreckingEncounterCardFixture.Create("07037", 0, players: 3);
        var runner = WreckingSchemeFixture.Runner();
        world.Abilities = runner;
        world.FirstPlayer = 2;
        Begin(world, runner);
        var choice = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(2, choice.Player);
        Assert.Equal(groups.Where((_, index) => index != 1).Select(group => group.Villain.ObjectId).Order(),
            choice.Affordances.Select(offer => offer.Id).Order());
        Assert.All(groups, group => Assert.Equal(1, group.Scheme.Tokens["k_threat"]));
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(groups[3].Villain.ObjectId), []);
        WreckingSchemeFixture.Finish(world, runner);
        Assert.Same(groups[3], world.EncounterTopology!.Active);
    }

    [Fact]
    public void BreakoutKeepsTheCounterWhenCurrentVillainIsTheUniqueMaximum()
    {
        var (world, groups, _) = WreckingEncounterCardFixture.Create("07037", 0);
        var runner = WreckingSchemeFixture.Runner();
        world.Abilities = runner;
        groups[1].Scheme.PlaceTokens("k_threat", 4);
        Begin(world, runner);
        WreckingSchemeFixture.Finish(world, runner);
        Assert.Same(groups[1], world.EncounterTopology!.Active);
        Assert.Equal(5, groups[1].Scheme.Tokens["k_threat"]);
        Assert.Equal(1, groups[0].Scheme.Tokens["k_threat"]);
    }

    private static void Begin(World world, Marvel.Cards.Run.AbilityRunner runner)
    {
        var occurrence = new Occurrence(1, [VillainStepOneTiming.Completed]);
        var pending = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        runner.ResolveWindow(world, occurrence, pending, Decision.Take(pending.Card));
    }
}
