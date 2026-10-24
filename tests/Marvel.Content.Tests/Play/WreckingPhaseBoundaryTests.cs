using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingPhaseBoundaryTests
{
    [Fact]
    public void ObserverIsReadWhenStepOneStartsRatherThanWhenThePhaseWasQueued()
    {
        // Setup planning is not the observation boundary: preceding phase-end work may change play.
        var (world, groups, _) = WreckingEncounterCardFixture.Create("07037", 0);
        var runner = WreckingSchemeFixture.Runner();
        world.Abilities = runner;
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        var area = main.Area;
        World.MoveToTop(main, world.AreaOf(DeckType.AsideDeck));
        Assert.False(runner.ObservesVillainStepOneCompletion(world));
        VillainPhase.Schedule(world.Agenda, 1);
        World.MoveToTop(main, area);
        var choice = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.False(choice.Cancellable);
        Assert.Equal(3, choice.Affordances.Count);
        Assert.All(groups, group => Assert.Equal(1, group.Scheme.Tokens["k_threat"]));
    }

    [Fact]
    public void UnobservedCompletionDoesNotAllocateAnExtraCoreReplayFrame()
    {
        var (world, _, _) = WreckingEncounterCardFixture.Create("07037", 0);
        var runner = WreckingSchemeFixture.Runner();
        world.Abilities = runner;
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.True(runner.ObservesVillainStepOneCompletion(world));
        World.MoveToTop(main, world.AreaOf(DeckType.AsideDeck));
        Assert.False(runner.ObservesVillainStepOneCompletion(world));
        VillainPhase.Schedule(world.Agenda, 2);
        Assert.Equal(6, world.Agenda.Outstanding.Count);
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.What == VillainStepOneTiming.Completed);
    }

    [Theory]
    [InlineData("normal", "01019b", 1, 0)]
    [InlineData("prevented", "01019b", 0, 0)]
    [InlineData("replaced", "01001a", 0, 1)]
    public void BreakoutRunsAfterStepOneRegardlessOfItsThreatOutcome(string mode, string identity, int threat, int damage)
    {
        // 07001b: "After step one of the villain phase"; unlike NORAD this does
        // not require positive main-scheme placement.
        var (world, groups, _) = WreckingEncounterCardFixture.Create("07037", 0, identity);
        var runner = WreckingSchemeFixture.Runner();
        world.Abilities = runner;
        Card? replacement = mode == "replaced" ? world.CreateCard("01061", world.Seats[0].Hand) : null;
        Assert.True(world.WindowAbilities.ObservesVillainStepOneCompletion(world));
        VillainPhase.Schedule(world.Agenda, 1);
        var interrupt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.True(interrupt.Cancellable);
        var answer = mode switch
        {
            "normal" => Decision.Decline,
            "prevented" => Decision.Take(world.Seats[0].IdentityCard.ObjectId),
            _ => Decision.Take(replacement!.ObjectId),
        };
        Sequence.Answer(world, world.Facts, runner, interrupt, answer, []);
        var choice = Sequence.Work(world, world.Facts, runner, [])!;
        for (int step = 0; choice.Cancellable; step++)
        {
            Assert.True(step < 4);
            Sequence.Answer(world, world.Facts, runner, choice, Decision.Decline, []);
            choice = Sequence.Work(world, world.Facts, runner, [])!;
        }
        Assert.False(choice.Cancellable);
        Assert.Equal(0, choice.Player);
        Assert.Equal(groups.Where((_, index) => index != 1).Select(group => group.Villain.ObjectId).Order(),
            choice.Affordances.Select(offer => offer.Id).Order());
        Assert.All(groups, group => Assert.Equal(1, group.Scheme.Tokens["k_threat"]));
        Assert.Equal(threat,
            world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void MainSchemeCompletionEndsTheGameBeforeBreakoutCanPlaceSideThreat()
    {
        var (world, groups, _) = WreckingEncounterCardFixture.Create("07037", 0);
        var runner = WreckingSchemeFixture.Runner();
        world.Abilities = runner;
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        VillainPhase.Schedule(world.Agenda, 1);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.True(world.IsOver);
        Assert.All(groups, group => Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
    }
}
