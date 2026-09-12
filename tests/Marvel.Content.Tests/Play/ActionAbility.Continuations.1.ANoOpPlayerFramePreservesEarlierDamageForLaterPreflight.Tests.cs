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
public sealed class ActionAbilityContinuationsANoOpPlayerFramePreservesEarlierDamageForLaterPreflightTests
{
    [Fact]
    public void ANoOpPlayerFramePreservesEarlierDamageForLaterPreflight()
    {
        // The engine checks every frame before an action cost is paid. An
        // alter-ego player's empty branch must preserve the hero's preceding
        // lethal damage and the resulting change of first player.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "alter-ego" } },
              "then": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "dealDamage": { "cards": { "titled": "Peter Parker" }, "amount": 1 } }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """, cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(9);
            board.Seats[1].IdentityCard.TurnTo("01010a");
        }, heroes: ["spider_man", "captain_marvel", "she_hulk"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:engage.3")]
    [Fact]
    public void AOneTimeGuardEntryKeepsItsOriginalEngagementAcrossFrames()
    {
        // A card ability cannot make a minion engage the player it is already
        // engaged with. Hydra Mercenary enters engaged with player zero once;
        // the next frame does not put it into play again or move that
        // engagement to player one, so the lethal continuation is rejected.
        var runner = GuardEntryRunner();
        World? world = null;
        Card? source = null;
        Card? mercenary = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel", "she_hulk"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Fact]
    public void TraceLocalEngagementOverridesTheCardsFrozenBoardArea()
    {
        // Guard protects only the engaged player. After the hero frame moves
        // Hydra Mercenary from player zero to player one, the unchanged board
        // area must not also leave player zero guarded in the trace. Their
        // later frame damages Rhino, eliminates them, and exposes the third
        // player's unsupported branch before any real mutation occurs.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": {
                "player": "firstPlayer", "form": "alter-ego"
              } },
              "then": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "seq": [
                  { "discard": { "titled": "Hydra Mercenary" } },
                  { "putIntoPlay": {
                    "card": { "titled": "Hydra Mercenary" },
                    "where": "engagedWithYou"
                  } }
                ] },
                "else": { "seq": [
                  { "dealDamage": {
                    "cards": { "query": "attackableEnemies" }, "amount": 1
                  } },
                  { "moveDamage": {
                    "from": { "query": "villain" },
                    "to": { "titled": "Peter Parker" }, "amount": 1
                  } }
                ] }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;
        Card? mercenary = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(9);
            board.Seats[1].IdentityCard.TurnTo("01010a");
        }, heroes: ["spider_man", "captain_marvel", "she_hulk"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(0), mercenary!.Area.PlayArea);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:engage.1")]
    [Rule("rr:guard.1")]
    [Fact]
    public void EngagementRelativeQueriesUseTheTraceLocalPlayer()
    {
        // Engagement is the player's play area at runtime. During eligibility,
        // trace-local entry is that area's synthetic equivalent: after the
        // hero re-engages Hydra, minionsEngagedWithYou must find and defeat it,
        // exposing Rhino before the lethal move and unsupported third frame.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": {
                "player": "firstPlayer", "form": "alter-ego"
              } },
              "then": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "seq": [
                  { "discard": { "titled": "Hydra Mercenary" } },
                  { "putIntoPlay": {
                    "card": { "titled": "Hydra Mercenary" },
                    "where": "engagedWithYou"
                  } },
                  { "dealDamage": {
                    "cards": { "query": "minionsEngagedWithYou" }, "amount": 3
                  } },
                  { "dealDamage": {
                    "cards": { "query": "attackableEnemies" }, "amount": 1
                  } },
                  { "moveDamage": {
                    "from": { "query": "villain" },
                    "to": { "titled": "Peter Parker" }, "amount": 1
                  } }
                ] }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """, cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;
        Card? mercenary = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, mercenary!, Statuses.Tough);
            board.Seats[0].IdentityCard.TakeDamage(9);
            board.Seats[1].IdentityCard.TurnTo("01010a");
        }, heroes: ["spider_man", "captain_marvel", "she_hulk"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(0), mercenary!.Area.PlayArea);
        Assert.True(Statuses.Has(world!, mercenary, Statuses.Tough));
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void TraceLocalGuardImmediatelyProtectsTheVillain()
    {
        // Guard means the engaged player cannot attack the villain, and a
        // lasting effect persists for its specified duration. Granting Sandman
        // Guard therefore removes Rhino from attackableEnemies before damage,
        // leaving no villain damage for the later move.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Sandman" },
                  "keyword": "guard", "amount": 1,
                  "until": "EndOfPlayerPhase"
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
                ] }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:damage.step.7")]
    [Fact]
    public void DefeatingATraceEnteredGuardImmediatelyExposesTheVillain()
    {
        // Guard prevents attacks only while the minion remains engaged. Three
        // damage defeats Hydra Mercenary, so Guard leaves play before the next
        // effect damages Rhino and makes the following lethal move reachable.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "if": {
                  "test": { "not": { "titleInPlay": "Hydra Mercenary" } },
                  "then": { "putIntoPlay": {
                    "card": { "cardsIn": {
                      "areas": [ "encounterDiscardPile" ],
                      "title": "Hydra Mercenary"
                    } },
                    "where": "engagedWithYou"
                  } }
                } },
                { "dealDamage": {
                  "cards": { "enemiesWithTrait": "HYDRA" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;
        Card? mercenary = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }
}
