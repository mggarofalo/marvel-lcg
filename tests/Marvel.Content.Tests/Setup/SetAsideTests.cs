using Marvel.Content.Setup;
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
/// not when it is made. The recorded corpus reads the same way — in one game
/// the modular set runs 40147-40150 into the encounter deck at ids 202-205 and
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
