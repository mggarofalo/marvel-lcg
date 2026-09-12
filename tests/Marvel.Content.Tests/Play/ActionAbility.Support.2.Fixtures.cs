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
internal static class ActionAbilitySupportFixtures2
{
    internal static Marvel.Cards.Run.AbilityRunner DiscardedTraitVillainGrantRunner(bool repeated, string predicate = "trait")
    {
        const string sequence = """
            { "seq": [
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
        string test = predicate switch
        {
            "kind" => """
              { "isKind": {
                "card": { "titled": "Rocket Boots" },
                "kind": "upgrade"
              } }
              """,
            "title" => """
              { "isTitle": {
                "card": { "titled": "Rocket Boots" },
                "title": "Rocket Boots"
              } }
              """,
            _ => """
              { "hasTrait": {
                "card": { "titled": "Enhanced Ivory Horn" },
                "trait": "WEAPON"
              } }
              """,
        };
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
                        "card": "this", "keyword": "attack", "amount": 1
                      } },
                      "else": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    internal static Marvel.Cards.Run.AbilityRunner ReenteredVulnerableStatusRunner(bool repeated)
    {
        const string sequence = """
            { "seq": [
              { "giveStatus": {
                "card": { "titled": "A.I.M. Scientist" },
                "status": "stunned"
              } },
              { "putIntoPlay": {
                "card": { "titled": "A.I.M. Scientist" },
                "where": "engagedWithYou"
              } },
              { "giveStatus": {
                "card": { "titled": "A.I.M. Scientist" },
                "status": "stunned"
              } },
              { "giveStatus": {
                "card": { "titled": "A.I.M. Scientist" },
                "status": "confused"
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
        return Runner(AuthoredCards.AuntMay, "Action", effect, cost: """{ "exhaust": "this" }""");
    }

    internal static Marvel.Cards.Run.AbilityRunner RestoredStatusVillainGrantRunner(bool repeated)
    {
        const string sequence = """
            { "seq": [
              { "discard": { "titled": "Vulture" } },
              { "putIntoPlay": {
                "card": { "titled": "Vulture" },
                "where": "engagedWithYou"
              } },
              { "giveStatus": {
                "card": { "titled": "Vulture" }, "status": "stunned"
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
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "not": { "hasStatus": {
                        "card": { "titled": "Vulture" },
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

    internal static Marvel.Cards.Run.AbilityRunner CappedToughVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
        { "seq": [
          { "giveStatus": { "card": "you", "status": "tough" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """, """
        { "not": { "hasStatus": {
          "card": { "titled": "Spider-Man" }, "status": "tough"
        } } }
        """);
    internal static Marvel.Cards.Run.AbilityRunner UnrelatedDamageVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
        { "seq": [
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """, """
        { "atLeast": {
          "value": { "damageOn": { "titled": "Spider-Man" } },
          "count": 1
        } }
        """);
    internal static Marvel.Cards.Run.AbilityRunner UnrelatedMinionCountVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
        { "seq": [
          { "discard": { "titled": "Vulture" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """, """
        { "atLeast": {
          "value": { "count": { "query": "alliesYouControl" } },
          "count": 1
        } }
        """);
    internal static Marvel.Cards.Run.AbilityRunner EnteredEngagementCountVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
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
          "value": { "count": {
            "query": "minionsEngagedWithYou"
          } },
          "count": 1
        } }
        """);
    internal static Marvel.Cards.Run.AbilityRunner FormHeroCountVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
        { "seq": [
          { "changeForm": { "player": "you", "to": "alter-ego" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """, """
        { "not": { "atLeast": {
          "value": { "count": { "query": "heroes" } },
          "count": 1
        } } }
        """);
    internal static Marvel.Cards.Run.AbilityRunner YourHeroCountVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
        { "seq": [
          { "changeForm": { "player": "you", "to": "hero" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """, """
        { "atLeast": {
          "value": { "count": "yourHero" },
          "count": 1
        } }
        """);
    internal static Marvel.Cards.Run.AbilityRunner EliminatedHeroCountVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
        { "seq": [
          { "dealDamage": {
            "cards": { "titled": "Spider-Man" }, "amount": 99
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
          "value": { "count": { "query": "heroes" } },
          "count": 1
        } }
        """);
}
