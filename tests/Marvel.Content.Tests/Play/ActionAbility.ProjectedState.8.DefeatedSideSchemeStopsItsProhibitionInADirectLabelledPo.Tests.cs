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
public sealed class ActionAbilityProjectedStateDefeatedSideSchemeStopsItsProhibitionInADirectLabelledPoTests
{
    [Rule("rr:defeat.1")]
    [Rule("rr:side-scheme.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DefeatedSideSchemeStopsItsProhibitionInADirectLabelledPower()
    {
        // A side scheme with no threat is defeated and discarded. Removing
        // Legions of Hydra's final threat therefore ends the prohibition that
        // kept Madame Hydra from taking the following damage.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Legions of Hydra" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Madame Hydra" },
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
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? legions = null;
        Card? madame = null;
        World? world = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            legions = board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
            legions.PlaceTokens("k_threat", 1);
            madame = board.CreateCard("01181", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(1, legions!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, madame!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:crisis-icon.1")]
    [Rule("rr:defeat.1")]
    [Rule("rr:side-scheme.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DefeatedCrisisSchemeUnlocksMainSchemeRemovalInTheTrace()
    {
        // While a crisis icon is in play, player cards cannot remove threat
        // from the main scheme. Crowd Control is discarded when its last
        // threat is removed, so the following main-scheme removal resolves.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Crowd Control" }, "amount": 1
                } },
                { "then": {
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } },
                  "then": { "dealDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
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
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? crowd = null;
        Card? main = null;
        Card? villain = null;
        World? world = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            crowd = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crowd.PlaceTokens("k_threat", 1);
            main = board.TheCardIn(DeckType.MainSchemesArea)!;
            main.PlaceTokens("k_threat", 1);
            villain = board.TheCardIn(DeckType.VillainArea)!;
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(1, crowd!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(1, main!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:crisis-icon.1")]
    [Rule("rr:then")]
    [Fact]
    public void ExplicitCrisisExceptionIsHonoredByInitiationResolutionAndThen()
    {
        // Crisis says a player card cannot remove threat from the main scheme.
        // This exact instruction says it ignores crisis, so the explicit card
        // exception wins and the fully resolved removal permits its `then`.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "then": {
              "effect": { "removeThreat": {
                "scheme": { "query": "mainScheme" },
                "amount": 1,
                "ignoresCrisis": "true"
              } },
              "then": { "draw": { "player": "you", "count": 1 } }
            } }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }
}
