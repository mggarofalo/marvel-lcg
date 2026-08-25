using Marvel.Rules.Events;
using Marvel.Rules.Fold;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Rules.Tests.Fold;

/// <summary>
/// The villain phase on boards the recording cannot reach.
/// </summary>
/// <remarks>
/// <para>
/// <c>PlayerPhaseTests</c> holds the phase against the recorded milestone game,
/// which is the strongest check available and has a blind spot: that game has
/// one player, and its round-one boost card is worth nothing. So the modulo in
/// "pass the first player token" and the whole boost-icon addition are exercised
/// by nothing there — both survived a mutation that deleted them.
/// </para>
/// <para>
/// These are the boards that separate them, built by hand. Small enough to
/// reason about, and none of them is asserting anything the recorded game
/// contradicts.
/// </para>
/// </remarks>
public sealed class VillainPhaseTests
{
    [Theory]
    // SCH 1, and a boost card worth nothing: the recorded round one, which is
    // why a fold that ignored boost icons entirely passed every test.
    [InlineData(0, 1)]
    // SCH 1 plus one boost icon: the recorded round two, at 2.
    [InlineData(1, 2)]
    [InlineData(3, 4)]
    public void BoostIconsAddToTheSchemeValue(int boost, int expected)
    {
        // `rr:scheme-enemy-activation.2c`: "Increase the scheming enemy's SCH
        // value by one for each boost icon on the card." Then step 3 places
        // threat equal to the modified value.
        var printed = new Printed()
            .With("villain", ("SCH", "1"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("boost", ("Boost", boost.ToString()));
        var world = Board(printed, players: 1);

        VillainPhase.Run(world, printed, new NoCardAbilities());

        Assert.Equal(expected, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }

    [Fact]
    public void TheBoostCardIsDiscardedWhicheverWayItCounts()
    {
        // `rr:boost-boost-icon.5`. A zero-icon card is still discarded, so the
        // discard is not conditional on the count.
        var printed = new Printed()
            .With("villain", ("SCH", "1"))
            .With("scheme", ("EscalationThreat", "0"));
        var world = Board(printed, players: 1);

        VillainPhase.Run(world, printed, new NoCardAbilities());

        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        Assert.Equal(["boost", "encounter"], discard.Cards.Select(card => card.FaceId));
    }

    [Fact]
    public void TheFirstPlayerTokenGoesToTheNextSeat()
    {
        // `rr:villain-phase.5`, "to the next clockwise player". At one player it
        // returns to the same seat, which is why the recorded game cannot tell
        // a modulo from a no-op.
        var printed = new Printed()
            .With("villain", ("SCH", "0"))
            .With("scheme", ("EscalationThreat", "0"));
        var world = Board(printed, players: 3);

        Assert.Equal(0, world.FirstPlayer);
        VillainPhase.Run(world, printed, new NoCardAbilities());
        Assert.Equal(1, world.FirstPlayer);
        VillainPhase.Run(world, printed, new NoCardAbilities());
        Assert.Equal(2, world.FirstPlayer);
        VillainPhase.Run(world, printed, new NoCardAbilities());
        Assert.Equal(0, world.FirstPlayer);
    }

    [Fact]
    public void TheVillainActivatesOncePerPlayer()
    {
        // `rr:villain-phase.2`, "in player order, each player resolves". Three
        // players means three activations, three boost cards and three lots of
        // threat -- not one.
        var printed = new Printed()
            .With("villain", ("SCH", "1"))
            .With("scheme", ("EscalationThreat", "0"));
        var world = Board(printed, players: 3);

        VillainPhase.Run(world, printed, new NoCardAbilities());

        Assert.Equal(3, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }

    [Fact]
    public void TokenPoolsSurviveLeavingPlay()
    {
        // The property the recorded discard pile shows and a synthetic board
        // states directly: a treachery has no `k_threat` key in the encounter
        // deck, has one after passing through the revealing area, and still has
        // one from the discard pile.
        var printed = new Printed()
            .With("villain", ("SCH", "0"))
            .With("scheme", ("EscalationThreat", "0"));
        printed.Kinds["encounter"] = CardKind.Treachery;
        var world = Board(printed, players: 1);

        var card = world.AreaOf(DeckType.EncounterDeck).Cards
            .Single(c => c.FaceId == "encounter");
        Assert.False(card.HasRegisteredTokens);

        VillainPhase.Run(world, printed, new NoCardAbilities());

        Assert.True(card.HasRegisteredTokens);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.Contains("k_threat", StateFields.Keys(CardKind.Treachery, hasHeldPools: true));
        Assert.DoesNotContain("k_threat", StateFields.Keys(CardKind.Treachery, hasHeldPools: false));
    }

    [Fact]
    public void AVillainThatWouldAttackSaysSoRatherThanScheming()
    {
        // `rr:activation.1`. Hero form and the villain attacks. Producing a
        // scheme anyway would place threat that the rules do not, and the board
        // would look plausible.
        var printed = new Printed()
            .With("villain", ("SCH", "1"))
            .With("scheme", ("EscalationThreat", "0"));
        printed.Kinds["identity"] = CardKind.Hero;
        var world = Board(printed, players: 1);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => VillainPhase.Run(world, printed, new NoCardAbilities()));
        Assert.Contains("hero form", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AMinionEngagedWithAPlayerSaysSoRatherThanBeingSkipped()
    {
        // `rr:villain-phase.2b`. Skipping it silently is the dangerous failure:
        // the board is right about everything except the damage nobody took.
        var printed = new Printed()
            .With("villain", ("SCH", "0"))
            .With("scheme", ("EscalationThreat", "0"));
        var world = Board(printed, players: 1);
        world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => VillainPhase.Run(world, printed, new NoCardAbilities()));
        Assert.Contains("engaged", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>A villain, a main scheme, one identity per seat, two encounter cards each.</summary>
    private static World Board(Printed printed, int players)
    {
        var world = new World(printed, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            var identity = world.CreateCard("identity", world.Seats[seat].Hero);
            world.Seats[seat].IdentityCard = identity;
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));

        // The deck is drawn from the top, which is the end of the list, so the
        // boost card has to be appended last to be taken first.
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateCard("encounter", deck);
            world.CreateCard("boost", deck);
        }

        return world;
    }

    /// <summary>Printed data for a handful of made-up cards.</summary>
    private sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes = new(StringComparer.Ordinal);

        public Dictionary<string, CardKind> Kinds { get; } = new(StringComparer.Ordinal)
        {
            ["identity"] = CardKind.AlterEgo,
            ["villain"] = CardKind.EncounterVillain,
            ["scheme"] = CardKind.MainScheme,
            ["boost"] = CardKind.Treachery,
            ["encounter"] = CardKind.Treachery,
            ["minion"] = CardKind.Minion,
        };

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

        public CardKind Kind(string faceId) =>
            Kinds.TryGetValue(faceId, out var kind) ? kind : CardKind.Unknown;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            attributes.TryGetValue(faceId, out var found)
                ? found
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? printed)
            && long.TryParse(printed, out long value)
                ? value
                : fallback;
    }
}
