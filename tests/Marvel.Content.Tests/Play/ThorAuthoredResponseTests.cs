using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorAuthoredResponseTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:limit")]
    public void ActualThorResponseUsesDestinationSeatAndResetsAtBothPhaseBoundaries()
    {
        // 06001a: "draw 2 cards. (Limit once per phase.)"
        var world = Board(1);
        var runner = AuthoredCards.Runner();
        for (int phase = 0; phase < 3; phase++)
        {
            for (int arrival = 0; arrival < 2; arrival++)
            {
                var minion = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDiscardPile));
                EncounterEntry.Schedule(world, EncounterEntryRequest.For(minion, 1, new CardEntryPlacement()), new EffectCompletion(1, 1));
                Assert.Equal(arrival == 0 ? 1 : 0, Finish(world, 1));
            }
            Assert.Equal((phase + 1) * 2, world.Seats[1].Hand.Cards.Count);
            Assert.Empty(world.Seats[0].Hand.Cards);
            if (phase == 0) PhaseEnd.EndPlayerPhase(world, []);
            if (phase == 1) PhaseEnd.EndVillainPhase(world, world.Facts, []);
        }
    }

    private static World Board(int thor)
    {
        var world = ThorActualFixture.Board("06001a", 2);
        if (thor == 0) return world;
        World.MoveToTop(world.Seats[0].IdentityCard, world.AreaOf(DeckType.RemovedArea));
        World.MoveToTop(world.Seats[1].IdentityCard, world.AreaOf(DeckType.RemovedArea));
        world.Seats[0].IdentityCard = world.CreateCard("01001a,01001b", world.Seats[0].Hero);
        world.Seats[1].IdentityCard = world.CreateCard("06001a,06001b", world.Seats[1].Hero);
        return world;
    }

    private static int Finish(World world, int thor, bool decline = false)
    {
        var runner = AuthoredCards.Runner();
        int responses = 0;
        for (int step = 0; step < 30; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) { Assert.Empty(world.Agenda.Outstanding); return responses; }
            var response = prompt.Affordances.SingleOrDefault(a => a.AnchorId == world.Seats[thor].IdentityCard.ObjectId && a.Timing == AbilityType.Response);
            if (response is not null) { responses++; Assert.Equal(thor, prompt.Player); }
            var answer = response is not null && !decline ? Decision.Take(response.Id)
                : prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id);
            Sequence.Answer(world, world.Facts, runner, prompt, answer, []);
        }
        Assert.Fail("Actual Thor engagement did not finish.");
        return responses;
    }
}
