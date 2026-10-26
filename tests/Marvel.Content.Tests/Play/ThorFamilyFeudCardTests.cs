using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorFamilyFeudCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData("06001a", false)]
    [InlineData("06001b", true)]
    [Rule("rr:in-play-and-out-of-play.1")]
    [Rule("rr:in-play-and-out-of-play.2")]
    [Rule("rr:in-play-and-out-of-play.8")]
    [Rule("rr:in-play-and-out-of-play.10")]
    public void RevealAddsWholeTableAsgardCountToPrintedTwoThreat(string form, bool faceUpStorage)
    {
        // "the faceup side of a player's identity card is in play";
        // "Cards in a player's hand, deck, and discard pile are out of play."
        var world = ThorActualFixture.Board(form, 2);
        var runner = AuthoredCards.Runner();
        var ally = world.CreateCard("06002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(1), host: ally.ObjectId, cardOwner: 0));
        world.CreateCard("06010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("06007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        // Thor/Odinson, Lady Sif, hosted Mjolnir, Helmet, Asgard and Loki: six physical cards.
        foreach (var area in new[] { world.Seats[1].Hand, world.Seats[1].Deck,
            world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)), world.AreaOf(DeckType.AsideDeck),
            world.AreaOf(DeckType.RemovedArea), world.AreaOf(DeckType.EncounterDeck),
            world.AreaOf(DeckType.EncounterDiscardPile), world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)) })
            world.CreateCard("06007", area);
        var stored = world.CreateCard("06007", world.Seats[1].Hand);
        BeneathCards.Place(world, stored, ally, faceUpStorage, "test", []);
        var fixture = new RedSkullFixture(world, runner);
        var scheme = fixture.Reveal("06027", 1);
        fixture.Finish();
        Assert.Equal(8, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        world.CreateCard("06007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        Assert.Equal(8, scheme.Tokens["k_threat"]);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void NoAsgardLeavesOnlyPrintedStartingThreat()
    {
        var world = Hero("01001a");
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var fixture = new RedSkullFixture(world, runner);
        var scheme = fixture.Reveal("06027");
        fixture.Finish();
        Assert.Equal(2, scheme.Tokens["k_threat"]);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
