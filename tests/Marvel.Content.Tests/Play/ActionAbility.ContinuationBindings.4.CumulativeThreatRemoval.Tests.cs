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
public sealed class ActionAbilityContinuationBindingsCumulativeThreatRemovalTests
{
    [Rule("rr:each-player.1")]
    [Fact]
    public void CumulativeThreatRemovalCanDefeatATitleBetweenFrames()
    {
        // All threat removed during one player's frame counts when deciding
        // whether a later player's title-dependent branch can become active.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Bomb Scare" },
              "then": { "seq": [
                { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } },
                { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            scheme = board.CreateCard("01109", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 2);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void CumulativeDamageCanEliminateAPlayerBetweenFrames()
    {
        // Individually nonlethal damage instances combine before the next
        // player's frame and can move the first-player binding.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "dealDamage": { "cards": "you", "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """, cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(8);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void OrderedMutationCanExposeLethalDamageWithinARepeatedFrame()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": "this" },
                { "if": {
                  "test": { "not": { "titleInPlay": "Aunt May" } },
                  "then": { "dealDamage": { "cards": "you", "amount": 2 } },
                  "else": { "draw": { "player": "you", "count": 1 } }
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
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(8);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void DamageToFixedTargetsAccumulatesAcrossRepeatedFrames()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "dealDamage": {
                "cards": { "query": "identities" }, "amount": 1
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
        }, hero: true, heroes: ["spider_man", "captain_marvel", "she_hulk"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Theory]
    [InlineData("\"yourHero\"")]
    [InlineData("{ \"query\": \"charactersYouControl\" }")]
    [InlineData("{ \"withTrait\": { \"cards\": \"yourHero\", \"trait\": \"AVENGER\" } }")]
    [InlineData("{ \"withTrait\": { \"cards\": { \"query\": \"charactersYouControl\" }, \"trait\": \"AVENGER\" } }")]
    [InlineData("{ \"maxBy\": { \"of\": { \"query\": \"charactersYouControl\" }, \"by\": \"attack\" } }")]
    public void PlayerRelativeDamageTargetsApplyOnlyInTheirPlayersFrame(string targets)
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$"""
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "dealDamage": { "cards": {{targets}}, "amount": 1 } },
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

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void MovedDamageCannotBeReusedAcrossRepeatedFrames()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "moveDamage": {
                "from": { "query": "villain" },
                "to": { "titled": "Spider-Man" },
                "amount": 1
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
            board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            board.Seats[0].IdentityCard.TakeDamage(8);
        }, hero: true, heroes: ["spider_man", "captain_marvel", "she_hulk"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void DamageBeforeAMoveReplenishesItsRepeatedSource()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "titled": "Hydra Mercenary" }, "amount": 2 } },
                { "discard": "this" },
                { "if": {
                  "test": { "not": { "titleInPlay": "Aunt May" } },
                  "then": { "moveDamage": {
                    "from": { "titled": "Hydra Mercenary" },
                    "to": { "titled": "Spider-Man" },
                    "amount": 2
                  } },
                  "else": { "moveDamage": {
                    "from": { "query": "villain" },
                    "to": { "titled": "Spider-Man" },
                    "amount": 1
                  } }
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
        Card? minion = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            board.Seats[0].IdentityCard.TakeDamage(8);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
    }
}
