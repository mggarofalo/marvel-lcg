using Marvel.Cards.Dsl;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class ActionAbilityRepeatedTraceRemovedIdentityRemainingHealthTests
{
    [Rule("rr:player-elimination.5")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RemovedIdentityRemainingHealthIsExactlyZero()
    {
        // Spider-Man's removal makes the selector absent, so remainingHealth
        // is zero and the surviving support's villain grant ends.
        var runner = RemovedIdentityHealthVillainGrantRunner();
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TakeDamage(9);
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:player-elimination.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EliminationPermanentAttachmentRaisesBeforePayment()
    {
        // Power Stone is a Permanent attachment. Eliminating its hero would
        // require resolving its attach-to text, which is intentionally
        // unsupported, so eligibility must refuse before the exhaust cost.
        var runner = PermanentEliminationRunner();
        Card? source = null;
        Card? stone = null;
        World? world = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            var identity = board.Seats[0].IdentityCard;
            stone = board.CreateCard("16149", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), identity.ObjectId, cardOwner: -1));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("permanent attachment", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.UpgradesArea, stone!.Area.Type);
    }

    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HealthModifierInvalidatesRemainingHealthPredicate(bool repeated)
    {
        // The lasting +1 health makes undamaged Spider-Man's remaining health
        // eleven before Klaw advances. That activates the retargeting constant,
        // which must be refused before the labelled cost or lasting state lands.
        var runner = HealthModifierVillainGrantRunner(repeated);
        Card? source = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            villain = board.TheCardIn(DeckType.VillainArea)!;
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:discard.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DepartureInvalidatesRemainingHealthPredicate(bool repeated)
    {
        // Discarding Vulture removes it from play, so its queried remaining
        // health becomes zero. The inverse constant then grants health to the
        // new villain and must be recognized before the action exhausts.
        var runner = DepartedAmountVillainGrantRunner(repeated);
        Card? source = null;
        Card? vulture = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            vulture = board.CreateCard("01167", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            villain = board.TheCardIn(DeckType.VillainArea)!;
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, vulture!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ZeroHealthModifierDoesNotInvalidatePredicate(bool repeated)
    {
        // A zero modifier does not alter Spider-Man's remaining health. The
        // threshold remains false, so its inactive villain grant cannot make
        // either legal action shape look unsupported.
        var runner = ZeroHealthModifierVillainGrantRunner(repeated);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VillainDamageDoesNotInvalidateHeroModifiedField(bool repeated)
    {
        // Damaging Klaw does not modify Spider-Man's attack. His threshold
        // remains false, so unrelated damage cannot expose the inactive
        // villain-health branch in either preflight shape.
        var runner = UnrelatedModifiedVillainGrantRunner(repeated);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EntryInvalidatesRemainingHealthPredicate(bool repeated)
    {
        // Hydra Mercenary begins out of play with queried remaining health
        // zero. Putting it into play makes that amount positive and activates
        // the villain grant before advancement, so cost must remain unpaid.
        var runner = EnteredAmountVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            villain = board.TheCardIn(DeckType.VillainArea)!;
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrossCardConditionalModifierInvalidatesModifiedField(bool repeated)
    {
        // Damage on Vulture activates one constant that grants Spider-Man +1
        // attack. A second constant then reaches its threshold and retargets
        // health to the new villain, a dependency chain preflight must follow.
        var runner = CrossCardModifierVillainGrantRunner(repeated);
        Card? source = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01167", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("01091", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnteredTraitActivatesConditionalModifierDependency(bool repeated)
    {
        // Hydra Mercenary's entry makes its printed HYDRA trait query true.
        // That activates Spider-Man's attack grant, which in turn activates
        // the villain-health grant before Klaw advances.
        var runner = EnteredTraitModifierVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            board.CreateCard("01091", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }
}
