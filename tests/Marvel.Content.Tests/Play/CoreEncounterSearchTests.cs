using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreEncounterSearchTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:discard.4")]
    [Rule("rr:first-player")]
    [Rule("rr:when-revealed-abilities.2")]
    [Theory]
    [InlineData("01116b")]
    [InlineData("01117a")]
    public void KlawMainSchemesPutTheExactDiscardedMinionIntoPlayForTheFirstPlayer(
        string faceId)
    {
        var world = Board(players: 2);
        world.FirstPlayer = 1;
        var deck = EmptyEncounterDeck(world);
        var below = world.CreateCard("01131", deck);
        var found = world.CreateCard("01129", deck);
        var passed = world.CreateCard("01186", deck);
        var scheme = world.CreateCard(faceId, world.AreaOf(DeckType.MainSchemesArea));

        AuthoredCards.Runner().WhenRevealed(world, scheme, player: 0);

        Assert.Equal([below], deck.Cards);
        Assert.Contains(passed, world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Equal(
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)),
            found.Area);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
    }

    [Rule("rr:discard.4")]
    [Rule("rr:ability")]
    [Fact]
    public void MastersOfEvilSkipsAPlainMinionAndMovesTheExactTraitMatch()
    {
        var world = Board(players: 1);
        var deck = EmptyEncounterDeck(world);
        var below = world.CreateCard("01186", deck);
        var found = world.CreateCard("01129", deck);
        var plain = world.CreateCard("01103", deck);
        var scheme = world.CreateCard("01128", world.AreaOf(DeckType.SideSchemesArea));

        AuthoredCards.Runner().WhenRevealed(world, scheme, player: 0);

        Assert.Contains(plain, world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Contains(below, deck.Cards);
        Assert.Equal(
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)),
            found.Area);
    }

    private static World Board(int players)
    {
        var world = new World(Cards, players);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        }

        return world;
    }

    private static Area EmptyEncounterDeck(World world)
    {
        var deck = world.AreaOf(DeckType.EncounterDeck);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        }

        return deck;
    }
}
