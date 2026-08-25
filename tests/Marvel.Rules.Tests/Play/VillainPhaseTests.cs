using Marvel.Tests;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Rules.Tests.Play;

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
    [Rule("rr:scheme-enemy-activation.step.2.c")]
    [Rule("rr:scheme-enemy-activation.step.3")]
    [Theory]
    // SCH 1, and a boost card worth nothing: the recorded round one, which is
    // why a resolve that ignored boost icons entirely passed every test.
    [InlineData(0, 1)]
    // SCH 1 plus one boost icon: the recorded round two, at 2.
    [InlineData(1, 2)]
    [InlineData(3, 4)]
    public void BoostIconsAddToTheSchemeValue(int boost, int expected)
    {
        // Threat placed is the *modified* SCH, so the two steps have to be
        // read together: a resolve that placed printed SCH would be right
        // whenever the boost card happened to be worth nothing.
        var printed = new Printed()
            .With("villain", ("SCH", "1"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("boost", ("Boost", boost.ToString()));
        var world = Board(printed, players: 1);

        Run(world, printed);

        Assert.Equal(expected, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }

    [Rule("rr:scheme-enemy-activation.step.2.d")]
    [Rule("rr:boost-boost-icon.5")]
    [Fact]
    public void TheBoostCardIsDiscardedWhicheverWayItCounts()
    {
        // A zero-icon card is still discarded, so the discard is not
        // conditional on the count.
        var printed = new Printed()
            .With("villain", ("SCH", "1"))
            .With("scheme", ("EscalationThreat", "0"));
        var world = Board(printed, players: 1);

        Run(world, printed);

        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        Assert.Equal(["boost", "encounter"], discard.Cards.Select(card => card.FaceId));
    }

    [Rule("rr:villain-phase.step.5")]
    [Fact]
    public void TheFirstPlayerTokenGoesToTheNextSeat()
    {
        // At one player the token returns to the same seat, which is why the
        // recorded game cannot tell a modulo from a no-op.
        var printed = new Printed()
            .With("villain", ("SCH", "0"))
            .With("scheme", ("EscalationThreat", "0"));
        var world = Board(printed, players: 3);

        Assert.Equal(0, world.FirstPlayer);
        Run(world, printed);
        Assert.Equal(1, world.FirstPlayer);
        Run(world, printed);
        Assert.Equal(2, world.FirstPlayer);
        Run(world, printed);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:villain-phase.step.2")]
    [Rule("rr:activation.1")]
    [Fact]
    public void TheVillainActivatesOncePerPlayer()
    {
        // Three players means three activations, three boost cards and three
        // lots of threat -- not one.
        var printed = new Printed()
            .With("villain", ("SCH", "1"))
            .With("scheme", ("EscalationThreat", "0"));
        var world = Board(printed, players: 3);

        Run(world, printed);

        Assert.Equal(3, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }

    // Deliberately uncited: no published rule says a card acquires a token
    // pool. It is an artefact of the Python engine's serialisation that the
    // digest forces the port to reproduce, and `docs/rules-citations.md` uses
    // it as the example of what an uncited test honestly is.
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

        Run(world, printed);

        Assert.True(card.HasRegisteredTokens);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.Contains("k_threat", StateFields.Keys(CardKind.Treachery, hasHeldPools: true));
        Assert.DoesNotContain("k_threat", StateFields.Keys(CardKind.Treachery, hasHeldPools: false));
    }

    [Rule("rr:activation.1")]
    [Fact]
    public void AVillainFacingAHeroAttacksRatherThanSchemes()
    {
        // "If the identity of the player resolving the activation is in hero
        // form, the villain initiates an attack against that player's identity.
        // If the identity [...] is in alter-ego form, the villain initiates a
        // scheme." Which face is showing decides, and the two are exclusive:
        // an engine that schemed anyway would place threat the rules do not.
        var printed = new Printed()
            .With("villain", ("SCH", "1"), ("ATK", "2"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("identity", ("HP", "10"), ("DEF", "3"));
        printed.Kinds["identity"] = CardKind.Hero;
        var world = Board(printed, players: 1);

        // Exhausted, so there is nobody who could defend and the attack asks
        // nothing -- `rr:defend-defense.2`, a hero must exhaust to defend.
        world.Seats[0].IdentityCard.Exhaust();

        Run(world, printed);

        Assert.False(
            world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.ContainsKey("k_threat"));
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-phase.step.2.b")]
    [Rule("rr:activation.2")]
    [Fact]
    public void AMinionEngagedWithAPlayerSaysSoRatherThanBeingSkipped()
    {
        // Skipping it silently is the dangerous failure: the board is right
        // about everything except the damage nobody took.
        var printed = new Printed()
            .With("villain", ("SCH", "0"))
            .With("scheme", ("EscalationThreat", "0"));
        var world = Board(printed, players: 1);
        world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => Run(world, printed));
        Assert.Contains("engaged", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:main-scheme-main-scheme-deck.2")]
    [Theory]
    // The recorded game reaches 8 threat against a target of 7, so *greater*
    // and *at least* both complete the scheme there and the boundary is
    // untested by it: a resolve that required strictly greater produces every
    // recorded digest and ends the game one round late.
    [InlineData(2, 2, true)]
    [InlineData(2, 3, false)]
    [InlineData(3, 2, true)]
    public void AMainSchemeCompletesAtItsTargetAndNotOnlyPastIt(
        int escalation, int target, bool completes)
    {
        var printed = new Printed()
            .With("villain", ("SCH", "0"))
            .With("scheme", ("EscalationThreat", escalation.ToString()),
                            ("TargetThreat", target.ToString()));
        var world = Board(printed, players: 1);

        Run(world, printed);

        Assert.Equal(completes, world.IsOver);
    }

    [Rule("rr:modifiers.6.1")]
    [Rule("rr:ability.1")]
    [Fact]
    public void AnAttachmentModifiesItsHostOnlyWhileInPlay()
    {
        // The recorded game cannot tell: its one modifier is an attachment in
        // `UpgradesArea`, which is in play, and its one other hosted card is a
        // Tough with no modifier printed on it. So a resolve that counted
        // modifiers from anywhere produces every recorded digest.
        //
        // A discarded attachment does not modify the card it used to be on.
        var printed = new Printed()
            .With("villain", ("SCH", "1"), ("ATK", "2"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("charge", ("ATK+", "3"));
        var world = Board(printed, players: 1);
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        long Attack() => StateFields
            .For(villain, printed, 1, inPlay: true, hasHeldPools: true,
                 hasFirstPlayerToken: false, world: world)["attack"];

        Assert.Equal(2, Attack());

        var upgrades = world.AreaOf(
            DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId);
        var charge = world.CreateCard("charge", upgrades);
        Assert.Equal(5, Attack());

        // Bound to the same host, and out of play. `StatusArea` is the case
        // that exists on a real board: the recorded Tough hangs off Rhino from
        // a zone that is not in play.
        var aside = world.AreaOf(DeckType.StatusArea, villain.Area.PlayArea, villain.ObjectId);
        World.MoveToTop(charge, aside);
        Assert.Equal(2, Attack());
    }

    /// <summary>Schedules the villain phase and walks it to the end.</summary>
    private static void Run(World world, Printed printed)
    {
        VillainPhase.Schedule(world.Agenda, round: 1);
        Sequence.Finish(world, printed, new NoCardAbilities(), []);
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
            ["charge"] = CardKind.Attachment,
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
