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
public sealed class ActionAbilityRepeatedEffectsBindingDependentForEachCountTests
{
    [Rule("rr:for-each.3")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void BindingDependentForEachCountCannotPruneItsBodyBeforePayment()
    {
        // Before chooseCard binds `chosen`, the count appears to be zero. The
        // chosen card makes it one, so the unsupported labelled continuation
        // must be found before the exhaust cost rather than hidden as a
        // zero-count body during initiation.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "chooseCard": {
              "from": { "query": "minions" },
              "effect": { "forEach": {
                "count": { "count": "chosen" },
                "effect": { "seq": [
                  { "choose": { "options": [
                    { "draw": { "player": "you", "count": 1 } },
                    { "seq": [] }
                  ] } },
                  { "attack": {
                    "target": { "query": "villain" },
                    "effect": { "enemyAttacks": {
                      "enemies": { "query": "villain" }
                    } }
                  } }
                ] }
              } }
            } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner));
        Assert.Contains("for-each count after state may change", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:for-each")]
    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void CurrentDynamicZeroHasNoChoiceForOtherwisePreflight()
    {
        // No payment, prior step, or binding can change damageOn before this
        // predecessor executes. Its current zero count makes the nested choice
        // unreachable, so otherwise may resolve the draw.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "otherwise": {
              "effect": { "forEach": {
                "count": { "damageOn": "you" },
                "effect": { "chooseCard": {
                  "from": { "query": "minions" },
                  "effect": { "discard": "chosen" }
                } }
              } },
              "otherwise": { "draw": { "player": "you", "count": 1 } }
            } }
            """);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void InactiveStablePowerBranchDoesNotValidateItsForEachCount()
    {
        // Form cannot change between offering and paying this action. Only the
        // hero branch can execute, so an alter-ego-only count must not reject
        // the labelled attack from an unreachable branch.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                "else": { "forEach": {
                  "count": { "add": [ -1, { "damageOn": "you" } ] },
                  "effect": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
                } }
              } }
            } }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Rule("rr:form-change-form.2")]
    [Fact]
    public void EarlierPowerStepCanExposeASuspendingBranch()
    {
        // The first step changes the fact tested by the second. Suspension
        // preflight must therefore inspect both reachable branches and refuse
        // the enemy activation before the labelled attack is scheduled.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "changeForm": { "player": "you", "to": "alter-ego" } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Rule("rr:for-each")]
    [Fact]
    public void PowerTargetBindingCannotHideAForEachContinuation()
    {
        // The villain becomes `chosen` when the labelled attack is scheduled.
        // Its existing damage makes this count one, so the nested threat
        // continuation must be refused while the action is still only offered.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "forEach": {
                "count": { "damageOn": "chosen" },
                "effect": { "placeThreat": {
                  "scheme": { "query": "mainScheme" }, "amount": 1
                } }
              } }
            } }
            """);
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
        }, hero: true, abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData("{\"powerAmount\":\"cardsDiscarded\"}")]
    [InlineData("{\"add\":[0,{\"powerAmount\":\"cardsDiscarded\"}]}")]
    [InlineData("{\"mul\":[1,{\"powerAmount\":\"cardsDiscarded\"}]}")]
    [InlineData("{\"min\":[2,{\"powerAmount\":\"cardsDiscarded\"}]}")]
    public void PowerAmountBranchIsRefusedBeforeLegalPracticeDiscards(string amount)
    {
        // The labeled effect is "a thwart made by that player's identity."
        // Suspending within that wrapper is an unsupported engine situation.
        // The selected card count binds powerAmount. Every branch that binding
        // can open must be checked before Legal Practice discards a hand card.
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$"""
            { "legalPractice": {
              "schemes": { "query": "thwartableSchemes" },
              "power": { "thwart": {
                "target": "chosen",
                "effect": { "if": {
                  "test": { "atLeast": {
                    "value": {{amount}},
                    "count": 1
                  } },
                  "then": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } },
                  "else": { "removeThreat": {
                    "scheme": "chosen", "amount": 1
                  } }
                } }
              } }
            } }
            """);
        World? world = null;
        Card? source = null;
        int handBefore = -1;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            var scheme = board.CreateCard("01151", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 1);
            handBefore = board.Seats[0].Hand.Cards.Count;
        }, hero: true, abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(handBefore, world!.Seats[0].Hand.Cards.Count);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void HarmlessEarlierPowerStepDoesNotSwitchAFormBranch()
    {
        // Drawing changes state but cannot change form. The hero-only branch
        // therefore remains the only executable branch of this labelled power.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }
}
