using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Authored Depowered beside a synthetic window row for the hero-specific
// Enhanced Spider-Sense, on a nonpublic research board.
public sealed class KangDepoweredStaleOfferTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    [Rule("rr:cannot")]
    public void AnOfferedWindowEventIsRefusedOnceDepoweredArrivesAndAcceptedOnceItLeaves()
    {
        // 11020: "You cannot play hero-specific cards." The event was offered on an
        // earlier board; answering it after Depowered enters player 1's play area
        // is refused without changing the game, and accepted after it leaves.
        var runner = Runner();
        var world = Board(runner);
        var source = world.Seats[1].Hand.Cards.Single(card => card.FaceId == "01004");
        var payment = world.Seats[1].Hand.Cards.Single(card => card.FaceId == "01088");
        var game = Game.Begin(world, Cards, runner);
        for (int i = 0; i < 2; i++)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ResolveMulligans).Id));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm).Id));
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source.ObjectId);
        Assert.True(offer.PlaysCard);
        var depowered = world.CreateCard("11020", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)));
        var prompt = game.Pending;
        string digest = world.Digest().Canonical();

        Assert.Throws<ArgumentException>(() => game.Resolve(PaidDecision.Take(offer, [payment.ObjectId])));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(digest, world.Digest().Canonical());

        World.MoveToTop(depowered, world.AreaOf(DeckType.RemovedArea));
        game.Resolve(PaidDecision.Take(offer, [payment.ObjectId]));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    private static AbilityRunner Runner()
    {
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var row = root["cards"]!.AsArray().Single(card => (string?)card!["card"] == "01004")!;
        row["abilities"] = JsonNode.Parse("""
            [{"trigger":{"event":"WhenFormChanged","timing":"Response","subject":"game"},
              "effect":{"draw":{"player":"you","count":1}}}]
            """);
        return new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
    }

    private static World Board(AbilityRunner runner)
    {
        var world = new World(Cards, 2, 241);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
            for (int i = 0; i < 20; i++) world.CreateCard("01087", seat.Deck);
        }
        for (int i = 0; i < 20; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        world.CreateCard("01004", world.Seats[1].Hand);
        world.CreateCard("01088", world.Seats[1].Hand);
        world.Abilities = runner;
        return world;
    }
}
