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
public sealed class ActionAbilityPowerTraceVillainExistenceRemainsTrueAcrossStageAdvancementTests
{
    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void VillainExistenceRemainsTrueAcrossStageAdvancement()
    {
        // Klaw II replaces Klaw I during advancement, so a villain exists on
        // both sides of the transition. The impossible else branch cannot
        // contribute a continuous villain health grant.
        var runner = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": { "attack": {
                      "target": { "query": "villain" },
                      "effect": { "seq": [
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 100
                        } },
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 1
                        } }
                      ] }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "exists": { "query": "villain" } },
                      "then": { "grant": {
                        "card": "this", "keyword": "attack", "amount": 1
                      } },
                      "else": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
        Card? source = null;
        Card? conditional = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            conditional = board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void OldVillainTitleEndingActivatesGrantBeforeNewStageContinues()
    {
        // Rhino leaves play before Ultron III enters. The absence of a card
        // titled Rhino activates this conditional villain health grant, so the
        // continuation cannot use the unchanged board's old-title answer.
        var runner = VillainTitleExistenceGrantRunner(grantWhenExists: false);
        Card? source = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            villain = board.TheCardIn(DeckType.VillainArea)!;
            board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
        }, hero: true, abilities: runner));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01094", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void OldVillainTitleEndingDeactivatesGrantBeforeNewStageContinues()
    {
        // This inverse grant is active only while Rhino exists. Moving to
        // Ultron III ends it, so the unreachable branch does not prevent the
        // otherwise traceable continuation from being advertised.
        var runner = VillainTitleExistenceGrantRunner(grantWhenExists: true);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
        }, hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DecisiveBooleanConstantBranchRemainsTraceable(bool useOr)
    {
        // A known true decides OR and a known false decides AND even when the
        // other operand reads the advancing villain's status. In either case
        // the branch containing the villain health grant is unreachable.
        var runner = BooleanShortCircuitVillainGrantRunner(useOr);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteringSameTitleStageKeepsVillainGrantActive()
    {
        // Klaw II enters as Klaw I leaves, so a card titled Klaw remains in
        // play throughout advancement. The title-gated health grant follows
        // the new villain and must be refused before the labelled cost.
        var runner = TitleInPlayVillainGrantRunner(grantWhenPresent: true);
        Card? source = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, hero: true, abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteringSameTitleStageKeepsInverseGrantInactive()
    {
        // The inverse branch stays inactive because Klaw II preserves the
        // title's in-play truth. Its unreachable villain grant does not block
        // advertising the continuation.
        var runner = TitleInPlayVillainGrantRunner(grantWhenPresent: false);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnrelatedToughChangeDoesNotActivateSupportGrant(bool repeated)
    {
        // Giving an identity Tough does not make this support Tough. Its
        // conditional villain health grant stays inactive in both direct and
        // repeated traces, so unrelated status state cannot block the action.
        var runner = UnrelatedStatusVillainGrantRunner(repeated);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToughChangeDoesNotInvalidateStunnedPredicate(bool repeated)
    {
        // Spider-Man gains Tough, but his Stunned state remains false. Status
        // invalidation is keyed by both card and status, so the unreachable
        // villain health grant remains inactive in either trace shape.
        var runner = UnrelatedStatusVillainGrantRunner(repeated, sameCardDifferentStatus: true);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StunnedGainActivatesVillainGrantBeforeAdvancement(bool repeated)
    {
        // Giving Spider-Man Stunned makes the matching conditional constant
        // active before Klaw advances. The new-stage health grant cannot be
        // projected from the unchanged board, so refusal precedes the cost.
        var runner = UnrelatedStatusVillainGrantRunner(repeated, sameCardDifferentStatus: true, giveStunned: true);
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
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
    }
}
