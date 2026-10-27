using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class FacedownWindowIndependentReviewTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(DeckType.PlayerDeck)]
    [InlineData(DeckType.DiscardPile)]
    [InlineData(DeckType.AsideDeck)]
    [InlineData(DeckType.RemovedArea)]
    [Rule("rr:in-play-and-out-of-play.8")]
    [Rule("rr:in-play-and-out-of-play.11")]
    public void AnEventOutsideItsPermittedOriginCannotLeakAWindow(DeckType location)
    {
        // "Cards in a player's hand, deck, and discard pile are out of play."
        // "Any cards that have been removed from the game or that have been set aside are out of play."
        var world = Board();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var card = world.CreateCard("01061", world.Seats[0].Hand);
        Assert.Single(runner.Waiting(world, Threat(world), WindowKind.Interrupt));
        World.MoveToTop(card, world.AreaOf(location, PlayArea.Of(0), cardOwner: 0));

        Assert.Empty(runner.Waiting(world, Threat(world), WindowKind.Interrupt));
        Assert.Null(Offering.Work(world, runner, Threat(world), WindowKind.Interrupt, []));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitStoragePermissionAllowsAWindowWithoutMakingTheEventInPlay(bool faceUp)
    {
        // Synthetic permission/window combination: no printed Core card stores
        // an Arrow interrupt. This tests the existing explicit-origin contract.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[{
                "trigger":{"timing":"Constant","subject":"this"},
                "effect":{"allowPlayBeneath":{"kind":"Event","trait":"ARROW"}}
              }]},
              {"card":"04008","abilities":[{
                "trigger":{"event":"WhenThreatWouldBePlaced","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}
              }]}
            ]}
            """));
        var world = Board();
        world.Abilities = runner;
        var host = world.CreateCard("01006",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("04008", world.Seats[0].Hand);
        world.CreateCard("01088", world.Seats[0].Hand);
        BeneathCards.Place(world, card, host, faceUp, "synthetic", []);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var agenda = world.Agenda.Outstanding.ToArray();

        var offered = Assert.Single(runner.Waiting(world, Threat(world), WindowKind.Interrupt));

        Assert.Equal(card.ObjectId, offered.Card);
        Assert.True(CardPlayOrigin.CanPlayEvent(world, card, 0));
        Assert.False(DeckTypes.IsInPlay(card.Area.Type));
        Assert.Equal(faceUp, card.FaceUp);
        Assert.DoesNotContain(card, world.Seats[0].Hand.Cards);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(agenda, world.Agenda.Outstanding);

        Discard.Card(world, host, "synthetic", []);
        Assert.Empty(runner.Waiting(world, Threat(world), WindowKind.Interrupt));
    }

    [Fact]
    [Rule("rr:in-play-and-out-of-play.1")]
    public void FaceupIdentityRetainsItsPrintedInterrupt()
    {
        // "For player cards, the faceup side of a player's identity card is in play."
        var world = Board("01019b");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var occurrence = Threat(world);

        var offered = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));

        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, offered.Card);
        runner.ResolveWindow(world, occurrence, offered, Decision.Take(offered.Card));
        Assert.Equal(0, occurrence.Threat!.Remaining);
    }

    private static World Board(string identity = "01001a")
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat(identity);
        seat.IdentityCard = world.CreateCard(identity, seat.Hero);
        world.CreateCard("01087", seat.Deck);
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }

    private static Occurrence Threat(World world)
    {
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        return Occurrence.ForThreat(100, [Steps.ThreatWouldBePlaced], world, Cards,
            new ThreatPlacement(scheme.ObjectId, scheme.ObjectId, 1,
                ThreatCause.VillainPhase, "test", player: 0));
    }
}
