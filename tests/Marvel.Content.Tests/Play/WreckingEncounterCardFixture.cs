using Marvel.Rules.State;

namespace Marvel.Content.Tests.Play;

/// <summary>Real encounter faces with explicit physical ownership and two zero-icon boost cards per deck.</summary>
internal sealed class WreckingEncounterCardFixture : CoreCardTestBase
{
    internal static (World World, EncounterDeckGroup[] Groups, Card Source) Create(
        string face, int owner, string identity = "01001a", int players = 1)
    {
        var world = Hero(identity, players);
        world.CreateCard("01088", world.Seats[0].Deck);
        world.CreateCard("07001b", world.AreaOf(DeckType.MainSchemesArea));
        var source = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea));
        string[] villains = ["07002", "07017", "07032", "07046"];
        string[] schemes = ["07004", "07019", "07034", "07048"];
        var groups = Enumerable.Range(0, 4).Select(index =>
        {
            var deck = world.CreateArea(DeckType.EncounterDeck);
            Card[] boosts = [world.CreateCard("07037", deck), world.CreateCard("07037", deck)];
            return new EncounterDeckGroup(
                world.CreateCard(villains[index], world.AreaOf(DeckType.VillainArea)),
                world.CreateCard(schemes[index], world.AreaOf(DeckType.SideSchemesArea)),
                deck, world.CreateArea(DeckType.EncounterDiscardPile),
                index == owner ? [.. boosts, source] : boosts);
        }).ToArray();
        world.BindEncounterTopology(new EncounterTopology(world, groups, groups[1].Villain));
        return (world, groups, source);
    }
}
