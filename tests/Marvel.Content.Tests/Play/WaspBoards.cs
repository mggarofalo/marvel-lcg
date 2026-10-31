using Marvel.Content.Tests.Cards;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic one-player Wasp boards over the production ability book.</summary>
internal static class WaspBoards
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    /// <summary>The three-face identity on <paramref name="face"/>, Rhino and his main scheme.</summary>
    internal static World Board(string face, int players = 1)
    {
        var world = new World(Cards, players, 241);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "13001a,13001b,13001c" : "01001a,01001b", seat.Hero);
            if (player == 0) seat.IdentityCard.TurnTo(face);
            for (int i = 0; i < 12; i++) world.CreateCard("01087", seat.Deck);
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        world.Abilities = AuthoredCards.Runner();
        return world;
    }

    internal static Card Engaged(World world, string face, int player = 0) =>
        world.CreateCard(face, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));

    internal static Card Side(World world, string face, int threat)
    {
        var scheme = world.CreateCard(face, world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", threat);
        return scheme;
    }

    internal static Card InPlay(World world, string face, DeckType area, int player = 0) =>
        world.CreateCard(face, world.AreaOf(area, PlayArea.Of(player), cardOwner: player));
}
