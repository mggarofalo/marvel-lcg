using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
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
public sealed class ActionAbilityPowerTraceFormChangeInvalidatesHeroCountTests
{
    [Rule("rr:form-change-form.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormChangeInvalidatesHeroCount(bool repeated)
    {
        // Spider-Man is the only hero in play. Changing him to alter-ego makes
        // the hero count zero and activates the conditional villain grant
        // before Klaw advances, so the cost must remain unpaid.
        var runner = FormHeroCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormChangeInvalidatesYourHeroCount(bool repeated)
    {
        // The resolving player begins in alter-ego, so "yourHero" names no
        // card. Changing to hero makes its count one and activates the villain
        // grant before Klaw advances.
        var runner = YourHeroCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:player-elimination.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeroEliminationDeactivatesHeroCountGrant(bool repeated)
    {
        // Spider-Man is the only hero. His elimination removes him from the
        // player-order-backed hero query, so the live villain grant ends before
        // Klaw advances and cannot reject the otherwise legal action.
        var runner = EliminatedHeroCountVillainGrantRunner(repeated);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:player-elimination.step.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EliminationReengagementActivatesMinionCountGrant(bool repeated)
    {
        // Hydra Mercenary begins with player one. Eliminating that player
        // makes the minion engage player zero, activating player zero's
        // engagement-count villain grant before Klaw advances.
        var runner = EliminationEngagementCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), mercenary!.Area.PlayArea);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:player-elimination.step.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EliminationReengagementMovesHostedUpgradeForCount(bool repeated)
    {
        // Re-engagement moves the minion's complete hosted tree. The hosted
        // upgrade therefore enters player zero's play area and activates that
        // player's upgrade-count villain grant before Klaw advances.
        var runner = EliminationHostedUpgradeCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? implant = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            implant = board.CreateCard("04119", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), mercenary.ObjectId));
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), mercenary!.Area.PlayArea);
        Assert.Equal(PlayArea.Of(1), implant!.Area.PlayArea);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ConstantYouUsesItsControllersFormDuringPreflight()
    {
        // Player one controls the constant, so its "you" reads player one even
        // though player zero initiates the labelled action.
        var runner = ControllerFormVillainGrantRunner();
        Card? source = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            board.Seats[1].IdentityCard.TurnTo("01010a");
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ConstantYouDoesNotUseInitiatorsFormDuringPreflight()
    {
        // Player zero is in hero form, but player one's alter-ego controls the
        // constant. Its inactive branch cannot be borrowed from the initiator.
        var runner = ControllerFormVillainGrantRunner();
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:player-elimination.step.2")]
    [Rule("rr:ownership-and-control.5")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RelocatedUpgradeControllerUsesItsProjectedPlayArea()
    {
        // Spider-Tracer is a player upgrade hosted by player one's minion.
        // Re-engagement moves it to hero player zero, changing its controller
        // and activating its controller-form villain grant.
        var runner = RelocatedUpgradeControllerVillainGrantRunner();
        Card? source = null;
        Card? tracer = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            tracer = board.CreateCard("01007", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), mercenary.ObjectId, cardOwner: 0));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), tracer!.Area.PlayArea);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:referential-ability.step.3")]
    [Fact]
    public void PlayerCardReferenceDoesNotTrackSameTitledEncounterCards()
    {
        // A player card's ambiguous title reference resolves only among player
        // cards. Neither Shocker is therefore the numeric target, so
        // removing one cannot retarget the reference to the other. The
        // independently valid villain target keeps the action initiable.
        var runner = SameTitleNumericRebindingVillainGrantRunner();
        Card? source = null;
        Card? first = null;
        Card? villain = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            first = board.CreateCard("01103", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            var second = board.CreateCard("01103", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            second.TakeDamage(1);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, hero: true, abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, first!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }
}
