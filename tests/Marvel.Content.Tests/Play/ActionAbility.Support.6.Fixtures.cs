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
internal static class ActionAbilitySupportFixtures6
{
    internal static Marvel.Cards.Run.AbilityRunner FormConditionalVillainGrantRunner(bool repeated, bool includeIdentityDamage = false)
    {
        string changingPlayer = includeIdentityDamage ? "firstPlayer" : "you";
        string identityDamage = includeIdentityDamage ? """, { "dealDamage": { "cards": "you", "amount": 1 } }""" : string.Empty;
        string sequence = $$"""
            { "seq": [
              { "changeForm": { "player": "{{changingPlayer}}", "to": "alter-ego" } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }{{identityDamage}}
            ] }
            """;
        if (includeIdentityDamage)
        {
            sequence = $$"""
                { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
                  "then": {{sequence}}, "else": { "heal": { "card": "you", "amount": 1 } } } }
                """;
        }

        string effect = repeated ? $$"""{ "eachPlayer": { "effect": {{sequence}} } }""" : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse($$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "inForm": {
                        "player": "you", "form": "hero"
                      } },
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    internal static Marvel.Cards.Run.AbilityRunner FormConditionalHealthDependencyRunner()
    {
        const string sequence = """
            { "seq": [
              { "changeForm": { "player": "you", "to": "alter-ego" } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string effect = $$"""
            { "attack": {
              "target": { "query": "villain" },
              "effect": {{sequence}}
            } }
            """;
        return new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse($$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01092", "abilities": [
                    {
                      "trigger": { "timing": "Constant", "subject": "this" },
                      "effect": { "if": {
                        "test": { "inForm": {
                          "player": "you", "form": "hero"
                        } },
                        "then": { "grant": {
                          "card": { "titled": "Spider-Man" },
                          "keyword": "health", "amount": 1
                        } }
                      } }
                    },
                    {
                      "trigger": { "timing": "Constant", "subject": "this" },
                      "effect": { "if": {
                        "test": { "not": { "atLeast": {
                          "value": { "remainingHealth": {
                            "titled": "Spider-Man"
                          } },
                          "count": 11
                        } } },
                        "then": { "grant": {
                          "card": { "query": "villain" },
                          "keyword": "health", "amount": 10
                        } }
                      } }
                    }
                  ] }
                ] }
                """));
    }

    internal static Marvel.Cards.Run.AbilityRunner ConditionalVillainGrantRunner(bool repeated)
    {
        const string sequence = """
            { "seq": [
              { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDiscardPile" ],
                  "title": "Hydra Mercenary"
                } },
                "where": "engagedWithYou"
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 36
              } },
              { "moveDamage": {
                "from": { "query": "villain" },
                "to": { "titled": "Spider-Man" }, "amount": 1
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
            """;
        string effect = repeated ? $$"""
              { "eachPlayer": { "effect": { "if": {
                "test": { "inForm": {
                  "player": "firstPlayer", "form": "hero"
                } },
                "then": {{sequence}},
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              } } } }
              """ : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse($$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "titleInPlay": "Hydra Mercenary" },
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    internal static Marvel.Cards.Run.AbilityRunner RepeatedDynamicTargetRunner(string firstTarget, string secondTarget, string moveSource = """{ "query": "villain" }""", bool includeAuthored = false) => Runner(AuthoredCards.AuntMay, "Action", $$"""
        { "eachPlayer": { "effect": { "if": {
          "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
          "then": { "seq": [
            { "dealDamage": { "cards": {{firstTarget}}, "amount": 100 } },
            { "dealDamage": { "cards": {{secondTarget}}, "amount": 1 } },
            { "moveDamage": {
              "from": {{moveSource}},
              "to": { "titled": "Spider-Man" },
              "amount": 1
            } }
          ] },
          "else": { "attack": {
            "target": { "query": "villain" },
            "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
          } }
        } } } }
        """, includeAuthored: includeAuthored);
    /// <summary>
    /// A game past the mulligan, on the first player's turn.
    /// </summary>
    /// <remarks>
    /// The board is prepared <b>before</b> the game begins, because a turn
    /// prompt is built once and lists what was there when it was built. A card
    /// put into play afterwards is on the board and not in the question.
    /// </remarks>
    internal static (Game Game, World World) Playing(Action<World> prepare, bool hero = false, string[]? heroes = null, ICardAbilities? abilities = null, string scenario = "rhino")
    {
        string[] playing = heroes ?? ["spider_man"];
        var world = WorldSetup.DealWithoutCardAbilities(CardCatalogData, Blueprints.From(Dealer.DealOrder(SetupCatalogData, scenario, playing), CardCatalogData), [..playing.Select(name => SetupCatalogData.Hero(name).Name)], 12345);
        if (hero)
        {
            world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        }

        prepare(world);
        // The mulligan is asked as a turn option too, so the loop watches the
        // verb rather than the question: declining it keeps the opening hand.
        var game = Game.Begin(world, CardCatalogData, abilities ?? AuthoredCards.Runner());
        while (game.Pending is { } asked && asked.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
        {
            game.Resolve(Decision.Decline);
        }

        return (game, world);
    }
}
