using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

internal static class CareForCassieFixture
{
    internal static World Board(string form, int cardsInHand = 2)
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 2, 571);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "01001a,01001b" : "12001a,12001b,12001c", seat.Hero);
            if (player == 1) seat.IdentityCard.TurnTo(form);
            for (int n = 0; n < 20; n++) world.CreateCard("01088", seat.Deck);
            for (int n = 0; n < (player == 1 ? cardsInHand : 1); n++) world.CreateCard("01089", seat.Hand);
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        for (int n = 0; n < 20; n++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        world.Abilities = AuthoredCards.Runner();
        return world;
    }
}
