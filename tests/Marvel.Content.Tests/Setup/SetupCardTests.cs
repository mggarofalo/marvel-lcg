using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

/// <summary>
/// "Put Setup Cards Into Play" — <c>rr:appendix-ii-setup.step.11</c>.
/// </summary>
/// <remarks>
/// <para>
/// "Search each deck and the set aside area for any cards with the setup
/// keyword and put them into play." Thirty-nine cards in the pool carry the
/// keyword and fourteen of them reach a board some campaign in
/// <c>setup.json</c> deals; until now every one of them was searched out at the
/// deal, set aside by <c>rr:permanent.2</c>, and left there.
/// </para>
/// <para>
/// <b>Put into play, not revealed.</b>
/// <c>rr:when-revealed-abilities.2</c>: "if an encounter card with a 'When
/// Revealed' ability is put into play <b>without being revealed</b>, the 'When
/// Revealed' ability does not trigger." The keywords still fire, because
/// <c>rr:enters-play</c> covers "any time when a card transitions from an
/// out-of-play area into play".
/// </para>
/// <para>
/// The board here is built from a deal order rather than from a campaign,
/// because no campaign in the dataset deals a Superpower attachment onto a
/// scenario whose own cards are all authored — <c>2407_breakin_and_takin</c>
/// comes closest and brings the Infinity Gauntlet with it, whose "Special"
/// abilities and infinity-stone deck are not written.
/// </para>
/// </remarks>
public sealed class SetupCardTests
{
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:setup-keyword.1")]
    [Rule("rr:attach-to")]
    [Theory]
    [InlineData("40151")]
    [InlineData("40155")]
    [InlineData("40159")]
    public void ASuperpowerIsPutIntoPlayAttachedToTheVillain(string faceId)
    {
        // Set aside before step 1 by `rr:permanent.2`, searched out again by
        // step 11, and placed by its own `rr:attach-to` phrase.
        var world = Deal(faceId);
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        var card = Assert.Single(world.Cards, each => each.FaceId == faceId);

        Assert.Equal(DeckType.UpgradesArea, card.Area.Type);
        Assert.Equal(villain.ObjectId, card.Area.Host);
    }

    [Rule("rr:traits")]
    [Fact]
    public void TheAttachedVillainGainsTheTraitTheAttachmentNames()
    {
        // "Attached villain gains the [[AERIAL]] trait."
        //
        // **Flight and not Super Strength**, and the guard below is why: Rhino
        // is printed BRUTE, so a test that dealt Super Strength and asked
        // whether the villain was a brute would pass with the grant deleted.
        // That is the shape of vacuous test this suite has been caught by
        // before, so the premise is asserted rather than assumed.
        var world = Deal("40151");
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        Assert.DoesNotContain("AERIAL", Cards.Traits(villain.FaceId));
        Assert.True(Traits.Has(world, villain, "AERIAL", Cards));

        // And the printed ones are still there. A granted trait is added to the
        // list, not a list of its own.
        Assert.True(Traits.Has(world, villain, "BRUTE", Cards));
    }

    [Rule("rr:ability")]
    [Fact]
    public void TheGrantedTraitGoesWhenTheAttachmentDoes()
    {
        // "A constant ability becomes active as soon as its card enters play
        // and remains active while the card is in play." Discarding Flight
        // takes the villain's AERIAL with it, and the villain has not moved.
        var world = Deal("40151");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var card = world.Cards.First(each => each.FaceId == "40151");

        Assert.True(Traits.Has(world, villain, "AERIAL", Cards));

        Discard.Card(world, card, "test", []);

        Assert.False(Traits.Has(world, villain, "AERIAL", Cards));
    }

    [Rule("rr:traits.1")]
    [Fact]
    public void TheGrantedTraitLandsOnTheNamedCardAndNoOther()
    {
        // "Some card abilities reference cards that possess or lack specific
        // traits", so a trait that leaked onto everything would make every such
        // ability answer yes. The hero is on the same board and wearing
        // nothing.
        var world = Deal("40151");
        var hero = world.Seats[0].IdentityCard;

        Assert.False(Traits.Has(world, hero, "AERIAL", Cards));
        Assert.True(Traits.Has(world, world.TheCardIn(DeckType.VillainArea)!, "AERIAL", Cards));
    }

    [Fact]
    public void TheGrantedTraitIsOnTheWire()
    {
        // The digest emits one `t_` key per trait a card has, and a card that
        // has gained one has it — `rr:traits` makes a trait an attribute rather
        // than text. A digest carrying only the printed list would describe a
        // board nobody is playing, and the two engines would agree about it.
        var world = Deal("40151");
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        var record = Assert.Single(
            world.Digest().Cards, each => each.Id == villain.ObjectId);

        Assert.Contains("t_AERIAL", record.Fields.Keys, StringComparer.Ordinal);
        Assert.Contains("t_BRUTE", record.Fields.Keys, StringComparer.Ordinal);
    }

    [Rule("rr:retaliate-x")]
    [Fact]
    public void TelepathyGivesTheVillainTheRetaliateItNames()
    {
        // "Attached villain gains the [[PSIONIC]] trait **and retaliate 1**."
        // The number is the half a trait does not have, and `rr:retaliate-x`
        // is where the engine reads it.
        var world = Deal("40159");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var hero = world.Seats[0].IdentityCard;

        Assert.DoesNotContain("PSIONIC", Cards.Traits(villain.FaceId));
        Assert.True(Traits.Has(world, villain, "PSIONIC", Cards));

        Damage.Attack(world, Cards, hero, villain, 1, "test", "Attack", []);

        Assert.Equal(1, hero.Damage);
    }

    [Rule("rr:status-cards.1.1")]
    [Fact]
    public void TheGrantedKeywordReachesTheRuleThatReadsIt()
    {
        // "Characters with the steady keyword can have one additional confused
        // status card and one additional stunned status card." A trait is a name
        // other cards ask about; steady is a field the rules read, and Super
        // Strength grants both in one ability.
        var world = Deal("40155");
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        Assert.Equal(2, Statuses.Limit(world, Cards, villain, Statuses.Stunned));
    }

    [Rule("rr:enters-play")]
    [Rule("rr:when-revealed-abilities.2")]
    [Fact]
    public void ASetupCardGetsItsKeywordsButNotItsWhenRevealed()
    {
        // The two halves of being put into play without being revealed. The
        // side scheme arrives with its printed starting threat, which is
        // `Reveal.EnterPlay`'s doing; nothing runs its text.
        var world = Deal("45071", AuthoredCards.Runner());

        var pool = Assert.Single(world.Cards, each => each.FaceId == "45071");

        Assert.Equal(DeckType.SideSchemesArea, pool.Area.Type);
        Assert.Equal(
            Cards.PrintedValue("45071", "StartingThreat", 1),
            pool.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:appendix-ii-setup.step.11")]
    [Fact]
    public void ACardTheEngineCannotPlaceStopsTheDealByName()
    {
        // The Infinity Gauntlet has the setup keyword and its placement is not
        // written, so there is nowhere to put it. Leaving it in the pile it was
        // searched out of would deal a board quietly missing a card the rules
        // put on the table — which is the failure this whole file exists to
        // stop, one level up from a card that does nothing.
        var refused = Assert.Throws<RulesNotImplementedException>(
            () => Deal("21129", AuthoredCards.Runner()));

        Assert.Contains("nowhere to put it", refused.Message, StringComparison.Ordinal);
    }

    [Rule("rr:appendix-ii-setup.step.11")]
    [Fact]
    public void ACardWithoutTheKeywordIsLeftWhereItWas()
    {
        // The control, and the reason the step reads the keyword rather than
        // the pile: Rhino's own attachments are set aside by nothing and dealt
        // into the encounter deck, and step 11 must walk past them.
        var world = Deal("40151");

        Assert.All(
            world.Cards.Where(card => card.FaceId == AuthoredCards.Charge),
            card => Assert.Equal(DeckType.EncounterDeck, card.Area.Type));
    }

    /// <summary>
    /// A Rhino board with one extra encounter card in it.
    /// </summary>
    /// <remarks>
    /// The Rhino scenario's own three cards are authored, so what the deal does
    /// with the card under test is the only thing that varies.
    /// </remarks>
    private static World Deal(string extra, ICardAbilities? abilities = null)
    {
        var order = Dealer.DealOrder(Setup, "rhino", ["spider_man"]).ToList();
        order.Add(new Creation(extra, CreationSource.EncounterSet, Creation.Scenario));

        return WorldSetup.Deal(
            Cards,
            Blueprints.From(order, Cards),
            ["Spider-Man"],
            Seed,
            abilities ?? AuthoredCards.Runner());
    }
}
