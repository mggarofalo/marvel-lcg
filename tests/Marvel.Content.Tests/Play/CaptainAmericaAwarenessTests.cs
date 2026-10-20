using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaAwarenessTests
{
    [Fact]
    [Rule("rr:uses-x-type")]
    public void ActualPlayPlacesThreeMentalCounters()
    {
        // rr:uses-x-type.1: "This card enters play with X all-purpose counters."
        Card? awareness = null;
        int[] payment = [];
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            awareness = board.CreateCard("03034", board.Seats[0].Hand);
            payment = [board.CreateCard("03022", board.Seats[0].Hand).ObjectId];
        });
        var play = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == awareness!.ObjectId && item.PlaysCard);
        game.Resolve(new Decision(play.Id, [world.Seats[0].IdentityCard.ObjectId], payment,
            Allocations: ResourcePayment.Allocate(play.CostOptions[0], payment)));
        Assert.Equal(DeckType.UpgradesArea, awareness!.Area.Type);
        Assert.Equal(3, awareness.Tokens["c_mental"]);
        Assert.True(awareness.Ready);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(1)]
    [Rule("rr:uses-x-type")]
    public void ResourcePaysAfterExhaustionAndTheLastCounterDiscardsOnlyTheGenerator(int counters)
    {
        // rr:uses-x-type: "When the last all-purpose counter is removed from a card with uses, discard that card."
        Card? awareness = null;
        Card? support = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            awareness = board.CreateCard("03034", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            awareness.PlaceTokens("c_mental", counters);
            support = board.CreateCard("01026", board.Seats[0].Hand);
        });
        var runner = AuthoredCards.Runner();
        var generator = Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, support!),
            item => item.Effect == awareness!.ObjectId);
        Assert.Equal("B", generator.Generates);
        var play = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == support!.ObjectId && item.PlaysCard);
        int[] payment = [awareness!.ObjectId];
        game.Resolve(new Decision(play.Id, [world.Seats[0].IdentityCard.ObjectId], payment,
            Allocations: ResourcePayment.Allocate(play.CostOptions[0], payment)));
        Assert.Equal(DeckType.SupportsArea, support!.Area.Type);
        Assert.Equal(counters == 1 ? DeckType.DiscardPile : DeckType.UpgradesArea, awareness.Area.Type);
        if (counters > 1)
        {
            Assert.False(awareness.Ready);
            Assert.Equal(counters - 1, awareness.Tokens["c_mental"]);
        }
        var next = world.CreateCard("01026", world.Seats[0].Hand);
        Assert.DoesNotContain(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, next),
            item => item.Effect == awareness.ObjectId);
    }

    [Theory]
    [InlineData("alter-ego")]
    [InlineData("exhausted")]
    [InlineData("other-player")]
    [Rule("rr:resource-ability.1")]
    public void UnavailableGeneratorCannotBeOffered(string boundary)
    {
        // Printed 03034: "Hero Resource: Exhaust Enhanced Awareness and remove 1 mental counter from it"
        Card? awareness = null;
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            int owner = boundary == "other-player" ? 1 : 0;
            awareness = board.CreateCard("03034", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(owner), cardOwner: owner));
            awareness.PlaceTokens("c_mental", 3);
            if (boundary == "exhausted") awareness.Exhaust();
        }, hero: boundary != "alter-ego", twoPlayers: true);
        var support = world.CreateCard("01026", world.Seats[0].Hand);
        var before = world.Digest().Canonical();
        var rng = world.Random.Generator.WordsConsumed;
        Assert.DoesNotContain(CardPayment.Generators(world, world.Facts, world.Seats[0], AuthoredCards.Runner(), support),
            item => item.Effect == awareness!.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng, world.Random.Generator.WordsConsumed);
    }
}
