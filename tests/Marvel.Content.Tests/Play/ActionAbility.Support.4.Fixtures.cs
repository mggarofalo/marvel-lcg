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
internal static class ActionAbilitySupportFixtures4
{
    internal static Marvel.Cards.Run.AbilityRunner EnteredAmountVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
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
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """, """
        { "atLeast": {
          "value": { "remainingHealth": {
            "titled": "Hydra Mercenary"
          } },
          "count": 1
        } }
        """);
    internal static Marvel.Cards.Run.AbilityRunner CrossCardModifierVillainGrantRunner(bool repeated)
    {
        const string sequence = """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Vulture" }, "amount": 1
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string effect = repeated ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
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
                  { "card": "01091", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "atLeast": {
                        "value": { "damageOn": { "titled": "Vulture" } },
                        "count": 1
                      } },
                      "then": { "grant": {
                        "card": { "titled": "Spider-Man" },
                        "keyword": "attack", "amount": 1
                      } }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "atLeast": {
                        "value": { "modified": {
                          "card": { "titled": "Spider-Man" },
                          "field": "attack"
                        } },
                        "count": 3
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

    internal static Marvel.Cards.Run.AbilityRunner EnteredTraitModifierVillainGrantRunner(bool repeated, bool decisiveFalse = false)
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
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string effect = repeated ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """ : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        string modifierTest = decisiveFalse ? """
              { "and": [
                { "not": { "exists": { "query": "villain" } } },
                { "hasTrait": {
                  "card": { "titled": "Hydra Mercenary" },
                  "trait": "HYDRA"
                } }
              ] }
              """ : """
              { "hasTrait": {
                "card": { "titled": "Hydra Mercenary" },
                "trait": "HYDRA"
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
                  { "card": "01091", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": {{modifierTest}},
                      "then": { "grant": {
                        "card": { "titled": "Spider-Man" },
                        "keyword": "attack", "amount": 1
                      } }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "atLeast": {
                        "value": { "modified": {
                          "card": { "titled": "Spider-Man" },
                          "field": "attack"
                        } },
                        "count": 3
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

    internal static Marvel.Cards.Run.AbilityRunner ConditionalVillainGrantRunner(bool repeated, string sequence, string test)
    {
        string effect = repeated ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
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
                      "test": {{test}},
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    internal static Marvel.Cards.Run.AbilityRunner DiscardedStatusVillainGrantRunner(bool repeated)
    {
        const string sequence = """
            { "seq": [
              { "discard": { "titled": "A.I.M. Scientist" } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string effect = repeated ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
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
                      "test": { "not": { "hasStatus": {
                        "card": { "titled": "A.I.M. Scientist" },
                        "status": "stunned"
                      } } },
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    internal static Marvel.Cards.Run.AbilityRunner TitleInPlayVillainGrantRunner(bool grantWhenPresent)
    {
        string branches = grantWhenPresent ? """
              "then": { "grant": {
                "card": { "query": "villain" },
                "keyword": "health", "amount": 10
              } }
              """ : """
              "then": { "grant": {
                "card": "this", "keyword": "attack", "amount": 1
              } },
              "else": { "grant": {
                "card": { "query": "villain" },
                "keyword": "health", "amount": 10
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
                    "effect": { "attack": {
                      "target": { "query": "villain" },
                      "effect": { "seq": [
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 100
                        } },
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 1
                        } }
                      ] }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "titleInPlay": "Klaw" },
                      {{branches}}
                    } }
                  } ] }
                ] }
                """));
    }
}
