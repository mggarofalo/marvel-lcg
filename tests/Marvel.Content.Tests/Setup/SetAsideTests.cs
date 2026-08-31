using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

/// <summary>
/// The cards that begin the game outside every deck.
/// </summary>
/// <remarks>
/// <para>
/// Three keywords say so and they say it plainly. <c>rr:permanent.2</c>:
/// "permanent cards are set aside <b>before step 1 of setup</b> and are put into
/// play later by abilities on other cards." <c>rr:setup-keyword.1</c>: a setup
/// card is "put into play during the <i>Put Setup Cards Into Play</i> step".
/// <c>rr:linked-card-title.1</c>: "set this card aside during setup."
/// </para>
/// <para>
/// <b>What the engine did instead was shuffle them into a deck.</b> 139 cards,
/// and the failure is not that they never entered play — it is worse than that.
/// A permanent attachment in the encounter deck is dealt, revealed and
/// discarded like a treachery, so the board is plausible and the card is gone.
/// </para>
/// <para>
/// <b>No object id moves.</b> A creation's position in the deal <i>is</i> the
/// card's id and the id is on the wire, so this changes where a card goes and
/// not when it is made. A set's cards land contiguously — in one measured game
/// the modular set ran 40147-40150 into the encounter deck at ids 202-205 and
/// 40151-40158 into the aside pile at ids 206-215, unbroken.
/// </para>
/// </remarks>
public sealed class SetAsideTests
{
    /// <summary>A scenario whose modular set is Flight, which is set aside.</summary>
    private const string Campaign = "2401_the_ground_is_lava";

    /// <summary>"Flight" — Setup, Permanent, and an attachment.</summary>
    private const string Flight = "40151";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:permanent.2")]
    [Fact]
    public void APermanentCardIsSetAsideRatherThanShuffledIn()
    {
        // "Permanent cards are set aside **before step 1 of setup**." Asserted
        // on the deal order rather than on the finished board, because the
        // board no longer shows it: `rr:appendix-ii-setup.step.11` searches the
        // set-aside area and puts the card into play, so being set aside is a
        // stage of the deal and not where it ends up — the same shape
        // `SetupOrderTests` already records for the obligations.
        var blueprint = Assert.Single(
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, ["spider_man"]), Cards),
            each => each.Spec.StartsWith(Flight, StringComparison.Ordinal));

        Assert.Equal(SetupSlot.SetAside, blueprint.Slot);
        Assert.Equal(-1, blueprint.Seat);
    }

    [Fact]
    public void TheRestOfTheModularSetIsStillDealtNormally()
    {
        // The three keywords reroute a card and nothing else does. The rest of
        // the same set is shuffled into the encounter deck as it always was,
        // which is what makes this a routing change rather than a new pile.
        var others = Dealt()
            .Where(card => card.Spec is "40152" or "40153" or "40154")
            .ToList();

        Assert.NotEmpty(others);
        Assert.All(others, card => Assert.Equal(SetupSlot.Encounter, card.Slot));
    }

    [Fact]
    public void NoObjectIdMoves()
    {
        // The id contract. Every card's id is its position in the deal order,
        // and rerouting must not renumber anything — so the ids of the whole
        // scenario are exactly the sequence they would be without the change.
        var order = Dealer.DealOrder(Setup, Campaign, ["spider_man"]);
        var dealt = Dealt();

        Assert.Equal(order.Count, dealt.Count);
        for (int id = 0; id < order.Count; id++)
        {
            Assert.Equal(order[id].Spec, dealt[id].Spec);
        }
    }

    [Theory]
    // One card per keyword, each without the others, so that a reading which
    // honoured only the first would fail here rather than pass on Flight --
    // which prints Setup *and* Permanent and cannot tell them apart.
    [InlineData("40151", "Permanent and Setup")]
    [InlineData("04155", "Setup alone")]
    [InlineData("43034", "Linked alone")]
    public void EachOfTheThreeKeywordsSetsACardAside(string faceId, string why)
    {
        var blueprint = Assert.Single(Blueprints.From(
            [new Creation(faceId, CreationSource.PlayerDeck, 0)], Cards));

        Assert.True(blueprint.Slot == SetupSlot.SetAside, $"{faceId} is {why}");
    }

    [Rule("rr:linked-card-title")]
    [Rule("rr:linked-card-title.1")]
    [Rule("rr:linked-card-title.2")]
    [Rule("rr:linked-card-title.3")]
    [Rule("rr:linked-card-title.3.1")]
    [Fact]
    public void LinkedProductCardsAreAllocatedOnceBeforeTheirParentPerDeck()
    {
        // Specialized Training has four linked upgrades. Two copies in one
        // deck still name the one product set; the same parent in a second
        // player's deck gets that player another complete set.
        var blueprints = Blueprints.From(
        [
            new Creation("43021", CreationSource.PlayerDeck, 0),
            new Creation("43021", CreationSource.PlayerDeck, 0),
            new Creation("43021", CreationSource.PlayerDeck, 1),
        ], Cards);

        Assert.Equal(
        [
            "43034", "43035", "43036", "43037", "43021", "43021",
            "43034", "43035", "43036", "43037", "43021",
        ], blueprints.Select(card => card.Spec));
        Assert.All(blueprints.Take(4), card =>
        {
            Assert.Equal(SetupSlot.SetAside, card.Slot);
            Assert.Equal(0, card.Seat);
        });
        Assert.All(blueprints.Skip(6).Take(4), card =>
        {
            Assert.Equal(SetupSlot.SetAside, card.Slot);
            Assert.Equal(1, card.Seat);
        });
        Assert.Equal(3, blueprints.Count(card => card.Slot == SetupSlot.PlayerDeck));
    }

    [Rule("rr:linked-card-title.1")]
    [Fact]
    public void LinkedPlayerCardsUseTheGeneralSetAsidePileNotTheNemesisSet()
    {
        var order = Dealer.DealOrder(Setup, "rhino", ["spider_man"])
            .Concat([new Creation("43021", CreationSource.PlayerDeck, 0)])
            .ToList();
        var blueprints = Blueprints.From(order, Cards)
            // Specialized Training itself is a player side scheme, whose play
            // surface is outside this placement test. Its linked cards have
            // already been allocated ahead of it; deal those to isolate which
            // set-aside pile receives them.
            .Where(card => card.Spec != "43021")
            .ToList();
        var world = WorldSetup.Deal(
            Cards,
            blueprints,
            [Setup.Hero("spider_man").Name],
            12345);
        string[] linked = ["43034", "43035", "43036", "43037"];

        Assert.All(
            world.Seats[0].SetAside.Cards,
            card => Assert.Contains(card.FaceId, linked));
        Assert.Equal(
            linked,
            world.Seats[0].SetAside.Cards.Select(card => card.FaceId));
        Assert.DoesNotContain(
            world.Seats[0].Nemesis.Cards,
            card => linked.Contains(card.FaceId, StringComparer.Ordinal));
    }

    [Rule("rr:linked-card-title")]
    [Theory]
    [InlineData("49020", "49033")] // title: New Recruits
    [InlineData("53023", "53034")] // qualified title: Captain America upgrade
    [InlineData("55057", "55064")] // qualified title: Titania minion
    public void LinkedParentReferencesAcceptPrintedTitleAndQualifiedTitle(
        string parent, string linked)
    {
        var blueprints = Blueprints.From(
            [new Creation(parent, CreationSource.PlayerDeck, 0)], Cards);

        var brought = Assert.Single(blueprints, card => card.Spec == linked);
        Assert.Equal(SetupSlot.SetAside, brought.Slot);
        Assert.Equal(parent, blueprints[^1].Spec);
        Assert.True(blueprints.ToList().IndexOf(brought) < blueprints.Count - 1);
    }

    [Rule("rr:linked-card-title.4")]
    [Fact]
    public void APlayerWhoTakesControlOfALinkedCardBecomesItsOwner()
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        var linked = world.CreateCard("53034", seat.SetAside);
        Assert.Equal(World.Scenario, linked.Owner);
        World.MoveToTop(
            linked,
            world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));

        Reveal.EnterPlay(world, Cards, linked, new List<GameEvent>());

        Assert.Equal(0, linked.Owner);
    }

    [Rule("rr:linked-card-title.4")]
    [Fact]
    public void ALinkedCardAddedFromSetAsideBecomesOwnedBeforeItEntersPlay()
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        var linked = world.CreateCard("53034", seat.SetAside);
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "53034", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed", "subject": "this" },
              "effect": { "addToHand": "this" }
            } ] } ] }
            """));
        world.Abilities = runner;

        runner.WhenRevealed(world, linked, player: 0);

        Assert.Contains(linked, seat.Hand.Cards);
        Assert.Equal(0, linked.Owner);
    }

    [Fact]
    public void AnIdentityIsNeverSetAsideWhateverItPrints()
    {
        // Nothing in the pool carries the combination; the guard is there so
        // that a future card cannot make a player begin with no hero. The
        // printed data is made up for exactly that reason -- there is no such
        // card to deal.
        var blueprints = Blueprints.From(
            [
                new Creation("perm", CreationSource.Identity, 0),
                new Creation("perm", CreationSource.PlayerDeck, 0),
            ],
            new Permanently());

        Assert.Equal(SetupSlot.Identity, blueprints[0].Slot);
        Assert.Equal(SetupSlot.SetAside, blueprints[1].Slot);
    }

    /// <summary>Printed data in which every card is permanent.</summary>
    private sealed class Permanently : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Unknown;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal) { ["Permanent"] = "1" };

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            fallback;
    }

    [Fact]
    public void TheRecordedBoardIsUntouched()
    {
        // The reason this could land without renumbering a fixture: no card in
        // the Rhino scenario, and none in Spider-Man's set, carries any of the
        // three keywords. Stated as a test so that adding one to the dataset by
        // mistake is a failure here rather than seven broken digests.
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);

        Assert.DoesNotContain(
            world.Cards,
            card => card.Area.Type == DeckType.AsideDeck && card.Area.PlayArea.IsVillains);
    }

    /// <summary>The deal order, which is where routing is decided.</summary>
    /// <remarks>
    /// Read off the blueprints rather than off a dealt board, and that is not a
    /// convenience. Where a card is <i>routed</i> and where it <i>ends up</i>
    /// stopped being the same question at
    /// <c>rr:appendix-ii-setup.step.11</c>: a setup card is searched out of the
    /// pile it was set aside in and put into play, so a board shows the second
    /// and this file is about the first. <c>SetupCardTests</c> is where the
    /// step itself is held.
    /// </remarks>
    private static IReadOnlyList<CardBlueprint> Dealt() =>
        Blueprints.From(Dealer.DealOrder(Setup, Campaign, ["spider_man"]), Cards);
}
