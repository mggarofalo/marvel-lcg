using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Synthetic set searches exercise paired encounter decks.</summary>
public sealed class EncounterSetRoutingReviewTests
{
    [Rule("rr:encounter-deck.1")]
    [Rule("rr:encounter-deck.2")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SearchUsesActiveDeckAndResetsOnlyItsDiscard(bool matchLast)
    {
        // pack:mc03:new-rules: "the encounter deck" refers to the active villain's deck.
        // rr:encounter-deck.2: stop when the criterion is met or the deck is empty.
        var world = new World(new Printed(), 1);
        var scheme = world.CreateCard("main", world.AreaOf(DeckType.MainSchemesArea));
        var inactive = Group(world, "match", "other");
        var active = Group(world, matchLast ? "match" : "other", "other");
        world.BindEncounterTopology(new EncounterTopology(world, [inactive, active], active.Villain));
        var events = new List<GameEvent>();

        var found = EncounterDeck.DiscardUntil(world, world.Facts, "wanted", "review", events);

        Assert.Same(matchLast ? active.Members[0] : null, found);
        Assert.Equal(inactive.Members, inactive.Deck.Cards);
        Assert.Empty(inactive.Discard.Cards);
        Assert.Equal(active.Members.Select(card => card.ObjectId).Order(),
            active.Deck.Cards.Select(card => card.ObjectId).Order());
        Assert.Empty(active.Discard.Cards);
        Assert.Single(events, item => item.Verb == "Reset");
        Assert.Equal(1, scheme.Tokens[EncounterDeck.AccelerationToken]);
    }

    private static EncounterDeckGroup Group(World world, string bottom, string top)
    {
        var villain = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        var side = world.CreateCard("side", world.AreaOf(DeckType.SideSchemesArea));
        var deck = world.CreateArea(DeckType.EncounterDeck);
        var discard = world.CreateArea(DeckType.EncounterDiscardPile);
        var first = world.CreateCard(bottom, deck);
        var second = world.CreateCard(top, deck);
        return new EncounterDeckGroup(villain, side, deck, discard, [first, second]);
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "villain" => CardKind.EncounterVillain,
            "side" => CardKind.EncounterSideScheme,
            "main" => CardKind.MainScheme,
            _ => CardKind.Minion,
        };
        public string EncounterSet(string faceId) => faceId == "match" ? "wanted" : "other";
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
