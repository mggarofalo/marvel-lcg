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
public sealed class ActionAbilityRepeatedTraceDecisiveFalseBranchIgnoresChangingEnteredTraitTests
{
    [Rule("rr:play-put-into-play")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DecisiveFalseBranchIgnoresChangingEnteredTrait(bool repeated)
    {
        // A villain exists before and after advancement, so the first false
        // conjunct decisively keeps the modifier inactive even though Hydra
        // Mercenary enters and makes the second conjunct true.
        var runner = EnteredTraitModifierVillainGrantRunner(repeated, decisiveFalse: true);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            board.CreateCard("01091", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:vulnerable.1")]
    [Rule("rr:permanent.5")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VulnerableStatusDiscardPreflightsPermanentBeforeCost(bool repeated)
    {
        // Becoming Stunned discards a Vulnerable character. Its Permanent
        // attachment makes that cleanup unsupported, so both trace shapes
        // refuse before the labelled action exhausts its source.
        var runner = VulnerableStatusRunner(repeated);
        World? world = null;
        Card? source = null;
        Card? scientist = null;
        Card? permanent = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            scientist = board.CreateCard("50083", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            permanent = board.CreateCard("27189a", board.AreaOf(DeckType.UpgradesArea, scientist.Area.PlayArea, scientist.ObjectId));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("rr:permanent.5 is not implemented", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, scientist!.Area.Type);
        Assert.Equal(scientist.ObjectId, permanent!.Area.Host);
        Assert.False(Statuses.Has(world!, scientist, Statuses.Stunned));
    }

    [Rule("rr:target.4")]
    [Rule("rr:target.4.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyFinalStatusGroupDoesNotInvalidateEarlierTargets(bool repeated)
    {
        // The earlier status effects have a valid target. The final effect's
        // empty group is simply skipped: an ability that targets multiple game
        // elements can initiate with one valid target and does not resolve
        // against an element that is no longer valid.
        var runner = ReenteredVulnerableStatusRunner(repeated);
        Card? source = null;
        Card? scientist = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            scientist = board.CreateCard("50083", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, scientist!.Area.Type);
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RestoredStatusInventoryDoesNotRemainMarkedChanged(bool repeated)
    {
        // Vulture begins Stunned, loses that attachment while discarded, then
        // re-enters and regains Stunned. The final predicate equals the live
        // board again, so its inactive inverse grant cannot block the action.
        var runner = RestoredStatusVillainGrantRunner(repeated);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var vulture = board.CreateCard("01167", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, vulture, Statuses.Stunned);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReentryWithoutToughDoesNotCreateAStatusChange(bool repeated)
    {
        // Vulture has no Tough before or after leaving and re-entering play.
        // A zero trace override is equivalent to the live board and cannot
        // make the inactive Tough-conditioned villain grant appear reachable.
        var runner = ReenteredNoToughVillainGrantRunner(repeated);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01167", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EntryWithoutToughDoesNotCreateAStatusChange(bool repeated)
    {
        // Hydra Mercenary enters play without Tough. The trace must preserve
        // that absence, so an inactive Tough-conditioned villain grant does
        // not make the otherwise legal action appear unsafe.
        var runner = EnteredNoToughVillainGrantRunner(repeated);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attach-to.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LeavingHostInvalidatesItsStatusPredicate(bool repeated)
    {
        // A status is an attachment and leaves with its host. Discarding the
        // Stunned scientist therefore activates the inverse constant before
        // Klaw advances, which must be recognized before paying the cost.
        var runner = DiscardedStatusVillainGrantRunner(repeated);
        World? world = null;
        Card? source = null;
        Card? scientist = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            scientist = board.CreateCard("50083", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, scientist, Statuses.Stunned);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, scientist!.Area.Type);
        Assert.True(Statuses.Has(world!, scientist, Statuses.Stunned));
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormChangeEndingVillainGrantDoesNotPreventStageAdvancement(bool repeated)
    {
        // Changing to alter-ego ends this hero-only continuous hit-point grant
        // before Klaw I is defeated. Klaw II therefore enters without the
        // modifier in both a direct and an each-player trace.
        var runner = FormConditionalVillainGrantRunner(repeated);
        Card? source = null;
        Card? conditional = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            conditional = board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
        Assert.Equal(AuthoredCards.SpiderMan, world.Seats[0].IdentityCard.FaceId);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FormChangeEndingHealthGrantActivatesVillainGrantBeforePayment()
    {
        // Spider-Man's hero-only hit-point grant keeps his remaining health at
        // 11. Changing to alter-ego ends it, which activates the conditional
        // villain grant before Klaw advances; refusal must precede the cost.
        var runner = FormConditionalHealthDependencyRunner();
        Card? source = null;
        Card? conditional = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            conditional = board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
    }
}
