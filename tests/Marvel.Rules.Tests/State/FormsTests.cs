using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.State;

/// <summary>
/// What form a player is in, on boards built to separate the cases.
/// </summary>
/// <remarks>
/// <para>
/// The rule these all come back to is one sentence of <c>rr:identity</c>: "the
/// side that is face up indicates the form that player is currently in". Form
/// is therefore a <b>reading</b> and never a stored flag, and most of what is
/// below is checking that nothing quietly stores it.
/// </para>
/// <para>
/// The cards are made up, because the shapes matter and the names do not: an
/// identity of two faces, an identity of three, a card granting a keyword form,
/// and the same card out of play. <c>FormDataTests</c> holds the same rules
/// against the real pool.
/// </para>
/// </remarks>
public sealed class FormsTests
{
    [Rule("rr:identity")]
    [Fact]
    public void TheFaceupSideIsTheForm()
    {
        // Not "a flag the flip also sets" -- the same card, read twice.
        var printed = new Printed();
        var world = Board(printed);
        var seat = world.Seats[0];

        Assert.Equal([Forms.AlterEgo], Forms.Of(world, seat, printed));

        seat.IdentityCard.TurnTo("hero");

        Assert.Equal([Forms.Hero], Forms.Of(world, seat, printed));
    }

    [Rule("rr:form-change-form.1")]
    [Fact]
    public void ChangingFormFlipsToTheOtherSideAndBack()
    {
        var printed = new Printed();
        var world = Board(printed);
        var seat = world.Seats[0];

        Assert.Equal("alterego", Forms.Change(seat, printed));
        Assert.True(Forms.In(world, seat, printed, Forms.Hero));

        Assert.Equal("hero", Forms.Change(seat, printed));
        Assert.True(Forms.In(world, seat, printed, Forms.AlterEgo));
    }

    [Rule("rr:form-change-form.2")]
    [Fact]
    public void OnlyTheFormChanges()
    {
        // "The character retains their sustained damage, status cards, lasting
        // effects, attached cards, tucked cards, tokens, and current state
        // (ready or exhausted)." Every noun in that list that this engine has
        // is checked, because the tempting implementation -- make a new card
        // for the new face -- passes a test that only reads the face.
        var printed = new Printed();
        var world = Board(printed);
        var seat = world.Seats[0];
        var identity = seat.IdentityCard;

        identity.TakeDamage(4);
        identity.PlaceTokens("k_threat", 2);
        identity.Exhaust();
        var upgrades = world.AreaOf(
            DeckType.UpgradesArea, identity.Area.PlayArea, identity.ObjectId, seat.Index);
        var attached = world.CreateCard("upgrade", upgrades);

        Forms.Change(seat, printed);

        Assert.Same(identity, seat.IdentityCard);
        Assert.Equal(4, identity.Damage);
        Assert.Equal(2, identity.Tokens["k_threat"]);
        Assert.False(identity.Ready);
        Assert.Contains(attached, upgrades.Cards);
    }

    [Rule("rr:form-change-form.6.1")]
    [Fact]
    public void AKeywordFormIsInAdditionToHeroFormRatherThanInsteadOfIt()
    {
        // Spectrum's shape. `21002` Gamma reads "Spectrum gets +2 ATK" and
        // "Hero Response", both of which need her to be in hero form while an
        // energy form is faceup -- so this is a set, not a choice.
        var printed = new Printed();
        printed.Forms["gamma"] = "energy";
        var world = Board(printed);
        var seat = world.Seats[0];
        seat.IdentityCard.TurnTo("hero");
        InPlay(world, seat, "gamma");

        Assert.Equal(["energy", Forms.Hero], Forms.Of(world, seat, printed));
    }

    [Rule("rr:identity.4")]
    [Fact]
    public void AFacedownFormCardGrantsNothing()
    {
        // Spectrum's three energy forms are all in play at once and at most one
        // is faceup -- `21001a` says "choose a **facedown** energy form
        // upgrade → flip that card faceup". `rr:identity.4` is the same
        // statement about the identity card: a facedown side is out of play.
        //
        // **All three facedown**, which is the state between putting them into
        // play and changing into one. Leaving a faceup card of the same form
        // beside them would prove nothing, because a set cannot tell one
        // "energy" from two.
        var printed = new Printed();
        printed.Forms["gamma"] = "energy";
        printed.Forms["pulsar"] = "energy";
        var world = Board(printed);
        var seat = world.Seats[0];
        seat.IdentityCard.TurnTo("hero");
        InPlay(world, seat, "gamma").TurnFaceDown();
        InPlay(world, seat, "pulsar").TurnFaceDown();

        Assert.Equal([Forms.Hero], Forms.Of(world, seat, printed));

        // And one of them turned up is the form, so this is not passing by the
        // cards being unreachable.
        world.Cards.First(card => card.FaceId == "pulsar").TurnFaceUp();

        Assert.Equal(["energy", Forms.Hero], Forms.Of(world, seat, printed));
    }

    [Rule("rr:form-change-form.6")]
    [Rule("rr:in-play-and-out-of-play.11")]
    [Rule("rr:set-aside-set-aside")]
    [Fact]
    public void AFormCardOutOfPlayGrantsNothing()
    {
        // "Set-aside cards are out of play and have no interaction with the
        // game until they are referenced." Asserted here rather than left to
        // `DeckTypes` because this feature rests on it: all nine form cards are
        // permanents, and a permanent's whole life before it enters play is
        // set aside. Nothing else in the suite pins it -- adding `AsideDeck` to
        // the in-play set passes every other test in the repository.
        Assert.False(DeckTypes.IsInPlay(DeckType.AsideDeck));

        // Every one of the nine is "Permanent", so it is set aside before the
        // game and put into play -- and the interval between those is a board
        // where the card exists and the form does not.
        //
        // **Faceup, deliberately.** A card in a deck is facedown, so a test
        // that left it there would pass with the in-play check deleted
        // entirely: the facedown check alone would carry it. Set aside faceup
        // is the state that separates the two.
        var printed = new Printed();
        printed.Forms["gamma"] = "energy";
        printed.Forms["pulsar"] = "energy";
        var world = Board(printed);
        var seat = world.Seats[0];
        seat.IdentityCard.TurnTo("hero");
        world.CreateCard("gamma", seat.Identity).TurnFaceUp();
        world.CreateCard("pulsar", world.AreaOf(DeckType.RemovedArea)).TurnFaceUp();

        Assert.Equal([Forms.Hero], Forms.Of(world, seat, printed));
    }

    [Rule("rr:form-change-form.6")]
    [Fact]
    public void EveryFormInPlayCounts()
    {
        // `rr:form-change-form.6` is written in the plural -- "cards with the
        // '[type] form' keyword grant an identity unique forms" -- and nothing
        // in it says one card, or one type. No hero in the pool today holds two
        // types at once, so this is the engine declining to bake in a limit the
        // rules do not state; the alternative is a reader that stops at the
        // first one it finds and is wrong the day a card is printed.
        var printed = new Printed();
        printed.Forms["gamma"] = "energy";
        printed.Forms["pulsar"] = "mass";
        var world = Board(printed);
        var seat = world.Seats[0];
        seat.IdentityCard.TurnTo("hero");
        InPlay(world, seat, "gamma");
        InPlay(world, seat, "pulsar");

        Assert.Equal(["energy", Forms.Hero, "mass"], Forms.Of(world, seat, printed));
    }

    [Rule("rr:form-change-form.6")]
    [Fact]
    public void AnotherPlayersFormIsNotYours()
    {
        // Two seats, because at one player every card in play is yours and the
        // owner check is unreachable.
        var printed = new Printed();
        printed.Forms["gamma"] = "energy";
        var world = Board(printed, players: 2);
        world.Seats[0].IdentityCard.TurnTo("hero");
        world.Seats[1].IdentityCard.TurnTo("hero");
        InPlay(world, world.Seats[1], "gamma");

        Assert.Equal([Forms.Hero], Forms.Of(world, world.Seats[0], printed));
        Assert.Equal(["energy", Forms.Hero], Forms.Of(world, world.Seats[1], printed));
    }

    [Rule("rr:flip.1")]
    [Fact]
    public void AnIdentityOfThreeFacesSaysSoRatherThanGuessing()
    {
        // Ant-Man `12001a/c`, Wasp `13001a/c` and Angel `42001a` / Archangel
        // `42001c` are foldable three-sided cards. Which hero face a flip from
        // alter-ego arrives at is not settled where the flip is described, and
        // the two faces do not print the same numbers -- Archangel prints THW 0
        // where Angel prints 2 -- so a guess is a wrong stat line, not a
        // cosmetic one.
        var printed = new Printed();
        var world = Board(printed, players: 1, faces: "alterego,hero,archangel");
        var seat = world.Seats[0];

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => Forms.Change(seat, printed));

        Assert.Contains("3 faces", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("archangel", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACardThatIsNotAnIdentityIsInNoForm()
    {
        var printed = new Printed();
        Assert.Null(Forms.OfFace("villain", printed));
        Assert.Equal(Forms.Hero, Forms.OfFace("hero", printed));
        Assert.Equal(Forms.AlterEgo, Forms.OfFace("alterego", printed));
    }

    /// <summary>Puts a card into play in a seat's own upgrades area.</summary>
    private static Card InPlay(World world, Seat seat, string faceId)
    {
        var upgrades = world.AreaOf(
            DeckType.UpgradesArea,
            seat.IdentityCard.Area.PlayArea,
            seat.IdentityCard.ObjectId,
            seat.Index);
        return world.CreateCard(faceId, upgrades);
    }

    private static World Board(
        Printed printed, int players = 1, string faces = "alterego,hero")
    {
        var world = new World(printed, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            world.Seats[seat].IdentityCard =
                world.CreateCard(faces, world.Seats[seat].Hero);
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        return world;
    }

    /// <summary>Printed data for a handful of made-up faces.</summary>
    private sealed class Printed : ICardFacts
    {
        public Dictionary<string, CardKind> Kinds { get; } = new(StringComparer.Ordinal)
        {
            ["alterego"] = CardKind.AlterEgo,
            ["hero"] = CardKind.Hero,
            ["archangel"] = CardKind.Hero,
            ["villain"] = CardKind.EncounterVillain,
            ["upgrade"] = CardKind.Upgrade,
            ["gamma"] = CardKind.Upgrade,
            ["pulsar"] = CardKind.Upgrade,
        };

        /// <summary>Which faces print a "[type] form" keyword.</summary>
        public Dictionary<string, string> Forms { get; } = new(StringComparer.Ordinal);

        public CardKind Kind(string faceId) =>
            Kinds.TryGetValue(faceId, out var kind) ? kind : CardKind.Unknown;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) => fallback;

        public string? FormKeyword(string faceId) =>
            Forms.TryGetValue(faceId, out string? form) ? form : null;
    }
}
