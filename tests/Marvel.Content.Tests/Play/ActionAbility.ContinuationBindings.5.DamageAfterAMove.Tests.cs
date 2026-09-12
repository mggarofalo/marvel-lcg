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
public sealed class ActionAbilityContinuationBindingsDamageAfterAMoveTests
{
    [Fact]
    public void DamageAfterAMoveIsAvailableOnlyToLaterRepeatedFrames()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } },
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }
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

    [Fact]
    public void DamageMovedAwayDoesNotAccumulateAcrossRepeatedFrames()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "titled": "Spider-Man" }, "amount": 1 } },
                { "moveDamage": {
                  "from": { "titled": "Spider-Man" },
                  "to": { "query": "villain" },
                  "amount": 1
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

    [Fact]
    public void DamageHealedAfterEachMoveDoesNotAccumulateAcrossFrames()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } },
                { "heal": { "card": { "titled": "Spider-Man" }, "amount": 1 } }
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
            board.TheCardIn(DeckType.VillainArea)!.TakeDamage(3);
            board.Seats[0].IdentityCard.TakeDamage(8);
        }, hero: true, heroes: ["spider_man", "captain_marvel", "she_hulk"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void DistinctPlayersCanSupplyDamageAcrossRepeatedFrames()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "moveDamage": {
                "from": "you",
                "to": { "titled": "Spider-Man" },
                "amount": 1
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(8);
            board.Seats[1].IdentityCard.TakeDamage(1);
            board.Seats[2].IdentityCard.TakeDamage(1);
        }, hero: true, heroes: ["spider_man", "captain_marvel", "she_hulk", "iron_man"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[2].IdentityCard.Damage);
    }

    [Fact]
    public void PlayerRelativeDamageRebindsBetweenFixedTargetFrames()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "titled": "Spider-Man" }, "amount": 1 } },
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "heal": { "card": { "titled": "Spider-Man" }, "amount": 1 } }
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
            board.Seats[0].IdentityCard.TakeDamage(7);
        }, hero: true, heroes: ["spider_man", "captain_marvel", "she_hulk"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void ToughPreventsLethalDamageInAnEarlierRepeatedFrame()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "dealDamage": {
                "cards": { "titled": "Spider-Man" }, "amount": 1
              } },
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
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }
}
