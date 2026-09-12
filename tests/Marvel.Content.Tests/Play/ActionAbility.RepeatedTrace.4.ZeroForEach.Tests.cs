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
public sealed class ActionAbilityRepeatedTraceZeroForEachTests
{
    [Rule("rr:for-each")]
    [Fact]
    public void ZeroForEachDoesNotHideALaterResolvableStep()
    {
        // Zero count means the repeated effect does not run; it does not make
        // the enclosing sequence unresolvable. The draw remains a meaningful
        // action and must still be advertised.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "forEach": { "count": 0, "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": { "chooseCard": {
                  "from": { "query": "minions" },
                  "effect": { "discard": "chosen" }
                } }
              } } } },
              { "draw": { "player": "you", "count": 1 } }
            ] }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Fact]
    public void ZeroForEachBodyIsUnreachableToContinuationPreflight()
    {
        // The zero-count body contains simultaneous threat placement, a shape
        // that would require a continuation if it ran. It cannot run, so
        // branch preflight must skip it and preserve the later draw action.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "if": {
                "test": { "titleInPlay": "Aunt May" },
                "then": { "forEach": { "count": 0, "effect": { "and": [
                  { "placeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } },
                  { "draw": { "player": "you", "count": 1 } }
                ] } } }
              } },
              { "draw": { "player": "you", "count": 1 } }
            ] }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Rule("rr:and")]
    [Fact]
    public void ZeroForEachDoesNotMakeASimultaneousSiblingSuspend()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "and": [
              { "forEach": { "count": 0, "effect": { "placeThreat": {
                "scheme": { "query": "mainScheme" }, "amount": 1
              } } } },
              { "draw": { "player": "you", "count": 1 } }
            ] }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void ZeroForEachHasNoResolutionForOtherwise()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "otherwise": {
                "effect": { "forEach": { "count": 0, "effect": {
                  "draw": { "player": "you", "count": 1 }
                } } },
                "otherwise": { "forEach": { "count": 0, "effect": {
                  "placeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  }
                } } }
              } },
              { "draw": { "player": "you", "count": 1 } }
            ] }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Fact]
    public void ZeroForEachChoiceDoesNotMakeALabelledPowerSuspend()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "forEach": { "count": 0, "effect": { "chooseCard": {
                  "from": { "query": "minions" },
                  "effect": { "discard": "chosen" }
                } } } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void ToughGrantedBeforeEachRepeatedDamagePreventsIt()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "giveStatus": {
                  "card": { "titled": "Spider-Man" }, "status": "tough"
                } },
                { "dealDamage": {
                  "cards": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(8);
        }, hero: true, heroes: ["spider_man", "captain_marvel", "she_hulk"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void HealthGrantedBeforeRepeatedDamageRaisesItsLethalThreshold()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Spider-Man" },
                  "keyword": "health", "amount": 1, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void ProhibitedMoveLeavesDamageForALaterRepeatedMove()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Madame Hydra" },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """, includeAuthored: true);
        World? world = null;
        Card? source = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
            board.CreateCard("01181", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            villain = board.TheCardIn(DeckType.VillainArea)!;
            villain.TakeDamage(1);
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(1, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void ProhibitedDamageCannotReplenishARepeatedMoveSource()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Madame Hydra" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """, includeAuthored: true);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
            board.CreateCard("01181", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }
}
