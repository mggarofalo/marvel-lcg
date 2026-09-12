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
internal static class ActionAbilitySupportFixtures5
{
    internal static Marvel.Cards.Run.AbilityRunner DepartingVillainAttachmentRunner() => new(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
            { "cards": [
              { "card": "01006", "abilities": [ {
                "trigger": {
                  "event": "WhenActionTriggered", "timing": "Action",
                  "subject": "game"
                },
                "cost": { "exhaust": "this" },
                "effect": { "eachPlayer": { "effect": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "seq": [
                    { "dealDamage": {
                      "cards": { "query": "villain" }, "amount": 100
                    } },
                    { "dealDamage": {
                      "cards": { "query": "villain" }, "amount": 1
                    } }
                  ] }
                } } } }
              } ] },
              { "card": "01099", "abilities": [ {
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "grant": {
                  "card": { "query": "villain" },
                  "keyword": "health", "amount": 10
                } }
              } ] }
            ] }
            """));
    internal static Marvel.Cards.Run.AbilityRunner BooleanShortCircuitVillainGrantRunner(bool useOr)
    {
        string test = useOr ? """
              { "or": [
                { "exists": { "query": "villain" } },
                { "hasStatus": {
                  "card": { "query": "villain" }, "status": "tough"
                } }
              ] }
              """ : """
              { "and": [
                { "not": { "exists": { "query": "villain" } } },
                { "hasStatus": {
                  "card": { "query": "villain" }, "status": "tough"
                } }
              ] }
              """;
        string branches = useOr ? """
              "then": { "grant": {
                "card": "this", "keyword": "attack", "amount": 1
              } },
              "else": { "grant": {
                "card": { "query": "villain" },
                "keyword": "health", "amount": 10
              } }
              """ : """
              "then": { "grant": {
                "card": { "query": "villain" },
                "keyword": "health", "amount": 10
              } },
              "else": { "grant": {
                "card": "this", "keyword": "attack", "amount": 1
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
                      "test": {{test}},
                      {{branches}}
                    } }
                  } ] }
                ] }
                """));
    }

    internal static Marvel.Cards.Run.AbilityRunner OrderedFirstPlayerVillainGrantRunner() => new(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
            { "cards": [
              { "card": "01006", "abilities": [ {
                "trigger": {
                  "event": "WhenActionTriggered", "timing": "Action",
                  "subject": "game"
                },
                "cost": { "exhaust": "this" },
                "effect": { "eachPlayer": { "effect": { "seq": [
                  { "dealDamage": { "cards": "you", "amount": 1 } },
                  { "attack": {
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
                ] } } }
              } ] },
              { "card": "01092", "abilities": [ {
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "grant": {
                    "card": { "query": "villain" },
                    "keyword": "health", "amount": 10
                  } }
                } }
              } ] }
            ] }
            """));
    internal static Marvel.Cards.Run.AbilityRunner VillainTitleExistenceGrantRunner(bool grantWhenExists)
    {
        string test = grantWhenExists ? """{ "exists": { "titled": "Rhino" } }""" : """{ "not": { "exists": { "titled": "Rhino" } } }""";
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

    internal static Marvel.Cards.Run.AbilityRunner FirstPlayerVillainGrantRunner()
    {
        const string sequence = """
            { "seq": [
              { "dealDamage": {
                "cards": "you", "amount": 1
              } },
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
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "inForm": {
                        "player": "firstPlayer", "form": "hero"
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

    internal static Marvel.Cards.Run.AbilityRunner FirstPlayerConditionalHealthDependencyRunner()
    {
        const string sequence = """
            { "seq": [
              { "dealDamage": { "cards": "you", "amount": 1 } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
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
                      "effect": {{sequence}}
                    } }
                  } ] },
                  { "card": "01091", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "inForm": {
                        "player": "firstPlayer", "form": "hero"
                      } },
                      "then": { "grant": {
                        "card": { "titled": "Carol Danvers" },
                        "keyword": "health", "amount": 1
                      } }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "not": { "atLeast": {
                        "value": { "remainingHealth": {
                          "titled": "Carol Danvers"
                        } },
                        "count": 13
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
}
