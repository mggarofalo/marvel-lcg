using Marvel.Rules.State;

namespace Marvel.Rules.Tests.Play;

/// <summary>Synthetic physical components for isolated Wrecking rules; not a pack-admission fixture.</summary>
internal sealed class WreckingRuntimeFixture
{
    internal WreckingRuntimeFixture(bool extreme = false)
    {
        World = new World(new Printed(), 2);
        for (int seat = 0; seat < 2; seat++)
        {
            var player = World.CreateSeat($"Player {seat + 1}");
            player.IdentityCard = World.CreateCard("hero", player.Hero);
        }
        World.FirstPlayer = 1;
        World.CreateCard("main", World.AreaOf(DeckType.MainSchemesArea));
        Groups = Enumerable.Range(0, 4).Select(index => Group(index, extreme)).ToArray();
        World.BindEncounterTopology(new EncounterTopology(World, Groups, Groups[0].Villain));
    }

    internal World World { get; }
    internal EncounterDeckGroup[] Groups { get; }

    private EncounterDeckGroup Group(int index, bool extreme)
    {
        var villain = World.CreateCard($"villain{index}", World.AreaOf(DeckType.VillainArea));
        var side = World.CreateCard("side", World.AreaOf(DeckType.SideSchemesArea));
        var deck = World.CreateArea(DeckType.EncounterDeck);
        var pile = World.CreateArea(DeckType.EncounterDiscardPile);
        var first = World.CreateCard("minion", deck);
        var second = World.CreateCard("minion", deck);
        Card[] stages = extreme
            ? [villain, World.CreateCard($"villain{index}", World.AreaOf(DeckType.VillainDeck))]
            : [villain];
        return new EncounterDeckGroup(villain, side, deck, pile, [first, second], stages);
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId.StartsWith("villain", StringComparison.Ordinal)
            ? CardKind.EncounterVillain : faceId switch
            {
                "hero" => CardKind.Hero,
                "side" => CardKind.EncounterSideScheme,
                "main" => CardKind.MainScheme,
                _ => CardKind.Minion,
            };
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            attribute == "HP" ? 5 : fallback;
    }
}
