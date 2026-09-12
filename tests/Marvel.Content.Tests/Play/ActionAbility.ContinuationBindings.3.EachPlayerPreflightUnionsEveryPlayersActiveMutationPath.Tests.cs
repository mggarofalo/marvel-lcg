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
public sealed class ActionAbilityContinuationBindingsEachPlayerPreflightUnionsEveryPlayersActiveMutationPathTests
{
    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerPreflightUnionsEveryPlayersActiveMutationPath()
    {
        // The first player chooses the order. A hero can discard the source
        // before an alter-ego's different branch tests whether it remains.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "you", "form": "hero" } },
              "then": { "discard": "this" },
              "else": { "if": {
                "test": { "titleInPlay": "Aunt May" },
                "then": { "draw": { "player": "you", "count": 1 } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } }
            } } } }
            """);
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void RepeatedMutationAnalysisDoesNotReadAnUnansweredChosenPlayer()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Aunt May" },
              "then": { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "if": {
                  "test": { "inForm": { "player": "chosenPlayer", "form": "hero" } },
                  "then": { "draw": { "player": "chosenPlayer", "count": 1 } },
                  "else": { "draw": { "player": "chosenPlayer", "count": 1 } }
                } }
              } },
              "else": { "draw": { "player": "you", "count": 1 } }
            } } } }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:form-change-form")]
    [Fact]
    public void RepeatedMutationAnalysisIncludesLabelledPowerEffects()
    {
        // The first player can order another hero first. That hero's attack
        // flips the first player before the first player's own frame resolves.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "you", "form": "hero" } },
              "then": { "attack": {
                "target": { "query": "villain" },
                "effect": { "changeForm": { "player": "firstPlayer", "to": "alter-ego" } }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[1].IdentityCard.TurnTo("01010a");
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void RepeatedMutationAnalysisReachesAFixedPoint()
    {
        // One frame removes the title, making a form change reachable; that
        // form change makes the unsupported third-frame branch reachable.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Aunt May" },
              "then": { "discard": "this" },
              "else": { "if": {
                "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
                "then": { "changeForm": { "player": "firstPlayer", "to": "alter-ego" } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } }
            } } } }
            """);
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, heroes: ["spider_man", "captain_marvel", "she_hulk"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void RepeatedMutationAnalysisIsBoundedByRemainingFrames()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Aunt May" },
              "then": { "discard": "this" },
              "else": { "if": {
                "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
                "then": { "changeForm": { "player": "firstPlayer", "to": "alter-ego" } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } }
            } } } }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void RemovingThreatDoesNotChangeWhichTitlesAreInPlay()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void LethalDamageCanMoveTheFirstPlayerBindingBetweenFrames()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "dealDamage": { "cards": "you", "amount": 99 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        World? world = null;
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(0, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Fact]
    public void VillainDamageCannotMoveTheFirstPlayerBinding()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void NonlethalIdentityDamageCannotMoveTheFirstPlayerBinding()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "dealDamage": { "cards": "you", "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void MovingLethalDamageCanMoveTheFirstPlayerBinding()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "moveDamage": { "from": { "query": "villain" }, "to": "you", "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        World? world = null;
        Card? source = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            villain = board.TheCardIn(DeckType.VillainArea)!;
            villain.TakeDamage(1);
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(1, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void RemovingLastThreatCanDefeatATitleBetweenFrames()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Bomb Scare" }, "then": { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? scheme = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            InPlay(board, AuthoredCards.AuntMay);
            scheme = board.CreateCard("01109", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 1);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(1, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
    }
}
