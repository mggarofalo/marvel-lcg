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
public sealed class ActionAbilityActionsAndDependentEffectsLegalPracticeTests
{
    [Rule("rr:labeled-ability.4")]
    [Rule("rr:for-each")]
    [Fact]
    public void LegalPracticeCanBindAForEachPowerAmountAfterItIsOffered()
    {
        // powerAmount is unbound while the Legal Practice prompt is built.
        // That sentinel is not an authored negative count: the selected hand
        // cards bind it before the labelled thwart effect resolves.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "legalPractice": {
              "schemes": { "query": "thwartableSchemes" },
              "power": { "thwart": {
                "target": "chosen",
                "effect": { "forEach": {
                  "count": { "powerAmount": "cardsDiscarded" },
                  "effect": { "removeThreat": {
                    "scheme": "chosen", "amount": 1
                  } }
                } }
              } }
            } }
            """);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var scheme = board.CreateCard("01151", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 1);
        }, hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DifferentSchemesPowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        // A labeled thwart ability "is considered to be a thwart made by that
        // player's identity." Its whole nested power must be preflighted.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "thwartDifferentSchemes": { "schemes": { "query": "thwartableSchemes" }, "power": { "thwart": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void DelayedPowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        // The delayed subtree is still the action's effect. Preflight must see
        // its labeled attack before any cost is paid or activation state changes.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "afterActivation": { "effect": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Activation = new EnemyActivation(board.TheCardIn(DeckType.VillainArea)!.ObjectId, Player: 0, Attacking: true, Id: 41);
        }, abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Fact]
    public void AStableFormBranchIgnoresAnUnreachableSuspendingPower()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:form-change-form")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void APredecessorMutationPreflightsEveryReachableDependentBranch()
    {
        // Changing form flips which dependent branch resolves. The unsupported
        // labeled attack must be found before the form change or cost occurs.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "then": { "effect": { "changeForm": { "player": "you", "to": "alter-ego" } }, "then": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void EachPlayerPreflightUsesEveryPlayersCurrentForm()
    {
        // "For each player" reaches both identities. A safe branch for the
        // initiating hero cannot hide an unsupported branch for an alter-ego.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:choose-game-element.3")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void ChosenCardContextIsPreflightedBeforeTheActionCost()
    {
        // The choice binds "chosen" before its effect resolves. Every branch
        // that binding can open must be checked before the cost is paid.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "if": { "test": { "exists": "chosen" }, "then": { "attack": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } }, "else": { "draw": { "player": "you", "count": 1 } } } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Fact]
    public void ChosenCardDoesNotDestabilizeAnUnrelatedFormBranch()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void ChosenPlayerBindingIsPreflightedAfterSelection()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "chooseCard": { "from": { "query": "identities" }, "effect": { "if": { "test": { "inForm": { "player": "chosenPlayer", "form": "hero" } }, "then": { "draw": { "player": "chosenPlayer", "count": 1 } }, "else": { "draw": { "player": "chosenPlayer", "count": 1 } } } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void SequenceFormReachabilityWaitsForAChosenPlayerBinding()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "if": {
                  "test": { "inForm": {
                    "player": "chosenPlayer", "form": "hero"
                  } },
                  "then": { "changeForm": {
                    "player": "chosenPlayer", "to": "alter-ego"
                  } },
                  "else": { "draw": {
                    "player": "chosenPlayer", "count": 1
                  } }
                } }
              } },
              { "if": {
                "test": { "inForm": {
                  "player": "chosenPlayer", "form": "alter-ego"
                } },
                "then": { "draw": {
                  "player": "chosenPlayer", "count": 1
                } },
                "else": { "draw": {
                  "player": "chosenPlayer", "count": 1
                } }
              } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Fact]
    public void LaterChosenPlayerTargetsAreCheckedAgainstTheOfferedCandidates()
    {
        // “The act of choosing a game element … makes that game element a
        // target.” Both offered identities have an engaged enemy, so the
        // continuation has a valid target whichever identity is selected.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } },
              { "dealDamage": {
                "cards": { "query": "enemiesEngagedWithChosenPlayer" },
                "amount": 1
              } }
            ] }
            """);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("01167", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void AChosenEnemyCanTargetALaterAttackWrapper()
    {
        // The earlier choice establishes the target used by the later labelled
        // attack. Every offered enemy is currently attackable and damageable.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "attackableEnemies" },
                "effect": { "seq": [] }
              } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }
}
