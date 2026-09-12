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
public sealed class ActionAbilityProjectedMovementDependentHealTests
{
    [Rule("rr:villain-defeat.2")]
    [Rule("rr:then")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DependentHealUsesTheNewVillainStage()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "then": {
                  "effect": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } },
                  "then": { "heal": {
                    "card": { "titled": "Weapons Runner" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "titled": "Weapons Runner" },
                  "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
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
        World? world = null;
        Card? source = null;
        Card? minion = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01121", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(1);
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, minion!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:villain-defeat.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LabelledRankedSelectorDropsTheNewVillainStage()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "minBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
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
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01103", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:villain-defeat.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LabelledRankedSelectorCanGainTheNewVillainStage()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
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
        World? world = null;
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:lasting-effects")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LabelledNumericGrantChangesALaterRankedTargetSet()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "grantUntil": {
                  "card": { "query": "villain" },
                  "keyword": "attack", "amount": 2, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
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
        World? world = null;
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:villain-defeat.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RankedSelectorAfterFinalVillainDefeatDoesNotCrash()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "minBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } }
              ] }
            } }
            """);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01103", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LabelledGuardEntryImmediatelyProtectsTheVillain()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "putIntoPlay": {
                  "card": { "cardsIn": {
                    "areas": [ "encounterDiscardPile" ],
                    "title": "Hydra Mercenary"
                  } },
                  "where": "engagedWithYou"
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
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
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }
}
