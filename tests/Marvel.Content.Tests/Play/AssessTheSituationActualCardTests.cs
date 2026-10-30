using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AssessTheSituationActualCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData("01001a", 5)]
    [InlineData("01001b", 6)]
    [Rule("rr:end-of-player-phase.step.2")]
    [Rule("rr:end-of-player-phase.step.4")]
    public void StacksThroughEndPlayerDrawAndExpiresAtPhaseEnd(string face, int printed)
    {
        // "Each player simultaneously draws up to their hand size."
        // Then: "Any effects that last ‘until the end of the [player] phase’ end."
        var world = Hero("01001a,01001b", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Seats[0].IdentityCard.TurnTo(face);
        for (int p = 0; p < 2; p++)
            for (int i = 0; i < 12; i++) world.CreateCard("01088", world.Seats[p].Deck);
        var first = Play(world);
        Assert.Equal(printed + 1, PhaseEnd.HandSize(world, world.Seats[0], Cards));
        var second = Play(world);
        Assert.Equal(printed + 2, PhaseEnd.HandSize(world, world.Seats[0], Cards));
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.Equal(0, first.Area.CardOwner);
        Assert.Equal(0, second.Area.CardOwner);
        PhaseEnd.EndPlayerTurn(world, 0, []);
        Assert.Equal(printed + 2, PhaseEnd.HandSize(world, world.Seats[0], Cards));
        PhaseEnd.DrawToHandSize(world, Cards, []);
        Assert.Equal(printed + 2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(5, world.Seats[1].Hand.Cards.Count);
        PhaseEnd.ReadyCards(world, []);
        PhaseEnd.EndPlayerPhase(world, []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(printed, PhaseEnd.HandSize(world, world.Seats[0], Cards));
        Assert.Equal(printed + 2, world.Seats[0].Hand.Cards.Count);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
    }

    [Fact]
    public void LastingBonusSurvivesChangingForm()
    {
        // Printed 12033: "You get +1 hand size until the end of the phase."
        var world = Hero("01001a,01001b");
        world.Abilities = AuthoredCards.Runner();
        Play(world);
        Assert.Equal(6, PhaseEnd.HandSize(world, world.Seats[0], Cards));
        Forms.ChangeAndSchedule(world, world.Seats[0], Cards, 1);
        Sequence.Finish(world, Cards, world.Abilities, []);
        Assert.Equal(7, PhaseEnd.HandSize(world, world.Seats[0], Cards));
        PhaseEnd.EndPlayerPhase(world, []);
        Assert.Equal(6, PhaseEnd.HandSize(world, world.Seats[0], Cards));
    }

    private static Card Play(World world)
    {
        var runner = AuthoredCards.Runner();
        var assess = world.CreateCard("12033", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == assess.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, Cards, runner, []);
        return assess;
    }
}
