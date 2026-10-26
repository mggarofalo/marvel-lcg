using Marvel.Content.Tests.Cards;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

internal static class ThorActualFixture
{
    internal static World Board(string face, int players = 1)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, players);
        for (int p = 0; p < players; p++)
        {
            var seat = world.CreateSeat($"p{p}");
            seat.IdentityCard = world.CreateCard(p == 0 ? "06001a,06001b" : "01001a,01001b", seat.Hero);
            if (p == 0) seat.IdentityCard.TurnTo(face);
            for (int i = 0; i < 12; i++) world.CreateCard("01088", seat.Deck);
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        world.Abilities = AuthoredCards.Runner();
        return world;
    }
}

