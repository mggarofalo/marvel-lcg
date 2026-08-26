using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// The keywords that fire when a card is revealed or enters play.
/// </summary>
/// <remarks>
/// <c>rr:keywords</c>: "a keyword is an attribute that conveys specific rules to
/// its card", and each entry writes its keyword out as the ability it is
/// equivalent to. That is what these hold the engine to — the ability, not the
/// reminder text.
/// </remarks>
public sealed class KeywordTests
{
    [Rule("rr:toughness")]
    [Rule("rr:toughness.1")]
    [Fact]
    public void AMinionWithToughnessEntersPlayWithAToughStatusCard()
    {
        // "**Forced Response**: after this character enters play, give it a
        // tough status card." A status is a card with its own object id, not a
        // flag -- the recorded board is unambiguous about that.
        var printed = new Printed().With("minion", ("Toughness", "1"), ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.RevealingArea));

        Reveal.Resolve(world, printed, minion, 0, []);

        Assert.True(Statuses.Has(world, minion, Statuses.Tough));
    }

    [Rule("rr:tough.2")]
    [Rule("rr:tough.3")]
    [Fact]
    public void AToughStatusPreventsAllTheDamageAndIsDiscarded()
    {
        // "Prevent all of that damage and discard a tough status card from that
        // character instead", and `.3`: the character "is not considered to
        // have taken damage". Nine damage against three hit points, and it
        // survives untouched.
        var printed = new Printed().With("minion", ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);

        bool defeated = Damage.Deal(world, printed, minion, 9, "test", "test", []);

        Assert.False(defeated);
        Assert.Equal(0, minion.Damage);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
    }

    [Rule("rr:tough.2.1")]
    [Fact]
    public void OnlyOneToughCardGoesPerInstanceOfDamage()
    {
        // "A character with multiple tough status cards discards only **one**
        // tough status card each time it would take damage." Two cards is two
        // instances of damage prevented, not one.
        var printed = new Printed().With("minion", ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);
        Statuses.Give(world, minion, Statuses.Tough);

        Damage.Deal(world, printed, minion, 1, "test", "test", []);
        Assert.True(Statuses.Has(world, minion, Statuses.Tough));

        Damage.Deal(world, printed, minion, 1, "test", "test", []);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));

        Damage.Deal(world, printed, minion, 1, "test", "test", []);
        Assert.Equal(1, minion.Damage);
    }

    [Rule("rr:incite-x")]
    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    public void InciteXPlacesThreatOnTheMainScheme(int incite, int expected)
    {
        // "**When Revealed**: place X threat on the main scheme."
        var printed = new Printed().With("treachery", ("Incite", incite.ToString()));
        var world = Board(printed);
        var card = world.CreateCard("treachery", world.AreaOf(DeckType.RevealingArea));

        Reveal.Keywords(world, printed, card, 0, []);

        Assert.Equal(
            expected, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:surge")]
    [Fact]
    public void SurgeDealsTheRevealingPlayerAnotherEncounterCard()
    {
        // "**When Revealed**: deal yourself 1 facedown encounter card." Dealt,
        // not revealed -- `rr:surge.2` finishes the original card first, and the
        // queue is what makes that happen without any extra rule.
        var printed = new Printed().With("treachery", ("Surge", "1"));
        var world = Board(printed);
        var card = world.CreateCard("treachery", world.AreaOf(DeckType.RevealingArea));

        // Two cards in the deck, so "one card" is a claim rather than an
        // accident of the deck running out.
        world.CreateCard("after", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("next", world.AreaOf(DeckType.EncounterDeck));

        Reveal.Keywords(world, printed, card, 0, []);

        var queue = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0));
        Assert.Equal(["next"], queue.Cards.Select(dealt => dealt.FaceId));
    }

    [Rule("rr:hinder-x")]
    [Fact]
    public void HinderXPutsThreatOnTheCardItself()
    {
        // "A card with the hinder X keyword enters play with X threat **on
        // it**" -- on the card, not on the main scheme, which is what separates
        // it from incite.
        var printed = new Printed().With("sideScheme", ("Hinder", "3"));
        var world = Board(printed);
        var scheme = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));

        Reveal.Resolve(world, printed, scheme, 0, []);

        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(3, scheme.Tokens["k_threat"]);
        Assert.Equal(
            0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:side-scheme")]
    [Fact]
    public void ASideSchemeEntersPlayWithItsStartingThreatAndItsHinder()
    {
        // Two different sources of threat on one card, and they add: the
        // printed starting threat every scheme has, and the keyword.
        var printed = new Printed()
            .With("sideScheme", ("Hinder", "2"), ("StartingThreat", "3"));
        var world = Board(printed);
        var scheme = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));

        Reveal.Resolve(world, printed, scheme, 0, []);

        Assert.Equal(5, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:reveal.5")]
    [Rule("rr:reveal.4")]
    [Theory]
    [InlineData("minion", DeckType.EngagedEnemiesArea)]
    [InlineData("sideScheme", DeckType.SideSchemesArea)]
    [InlineData("obligation", DeckType.ObligationsArea)]
    // A treachery is "placed on the table in front of the player revealing it
    // *(it is not in play)*", which is where it already was.
    [InlineData("treachery", DeckType.RevealingArea)]
    public void EachCardTypeGoesWhereItsOwnClauseSays(string faceId, DeckType where)
    {
        var printed = new Printed();
        var world = Board(printed);
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));

        Reveal.Resolve(world, printed, card, 0, []);

        Assert.Equal(where, card.Area.Type);
    }

    private static World Board(Printed printed)
    {
        var world = new World(printed, players: 1);
        world.CreateSeat("p0");
        world.Seats[0].IdentityCard = world.CreateCard("identity", world.Seats[0].Hero);
        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }

    private sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes =
            new(StringComparer.Ordinal);

        public Printed With(string faceId, params (string Key, string Value)[] values)
        {
            var table = attributes.TryGetValue(faceId, out var found)
                ? found
                : attributes[faceId] = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                table[key] = value;
            }

            return this;
        }

        public CardKind Kind(string faceId) => faceId switch
        {
            "identity" => CardKind.AlterEgo,
            "villain" => CardKind.EncounterVillain,
            "scheme" => CardKind.MainScheme,
            "minion" => CardKind.Minion,
            "sideScheme" => CardKind.EncounterSideScheme,
            "obligation" => CardKind.Obligation,
            "tough" => CardKind.Status,
            _ => CardKind.Treachery,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            attributes.TryGetValue(faceId, out var found)
                ? found
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number)
                ? number
                : fallback;
    }
}
