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
internal static class ActionAbilitySupportFixtures3
{
    internal static Marvel.Cards.Run.AbilityRunner EliminationEngagementCountVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
        { "seq": [
          { "dealDamage": {
            "cards": { "titled": "Carol Danvers" }, "amount": 99
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
    internal static Marvel.Cards.Run.AbilityRunner EliminationHostedUpgradeCountVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
        { "seq": [
          { "dealDamage": {
            "cards": { "titled": "Carol Danvers" }, "amount": 99
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
          "value": { "count": { "query": "upgradesYouControl" } },
          "count": 1
        } }
        """);
    internal static Marvel.Cards.Run.AbilityRunner ControllerFormVillainGrantRunner() => ConditionalVillainGrantRunner(false, """
            { "seq": [
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """, """
            { "inForm": { "player": "you", "form": "hero" } }
            """);
    internal static Marvel.Cards.Run.AbilityRunner RemovedIdentityHealthVillainGrantRunner() => ConditionalVillainGrantRunner(false, """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Spider-Man" }, "amount": 1
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
              "value": { "remainingHealth": { "titled": "Spider-Man" } },
              "count": 1
            } }
            """);
    internal static Marvel.Cards.Run.AbilityRunner RelocatedUpgradeControllerVillainGrantRunner() => new(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
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
                      "cards": { "titled": "Carol Danvers" }, "amount": 99
                    } },
                    { "dealDamage": {
                      "cards": { "query": "villain" }, "amount": 100
                    } },
                    { "dealDamage": {
                      "cards": { "query": "villain" }, "amount": 1
                    } }
                  ] }
                } }
              } ] },
              { "card": "01007", "abilities": [ {
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "if": {
                  "test": { "inForm": {
                    "player": "controller", "form": "hero"
                  } },
                  "then": { "grant": {
                    "card": { "query": "villain" },
                    "keyword": "health", "amount": 10
                  } }
                } }
              } ] }
            ] }
            """));
    internal static Marvel.Cards.Run.AbilityRunner SameTitleNumericRebindingVillainGrantRunner() => ConditionalVillainGrantRunner(false, """
            { "seq": [
              { "discard": { "titled": "Shocker" } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """, """
            { "atLeast": {
              "value": { "damageOn": { "titled": "Shocker" } },
              "count": 1
            } }
            """);
    internal static Marvel.Cards.Run.AbilityRunner PermanentEliminationRunner() => new(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
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
                      "cards": { "titled": "Spider-Man" }, "amount": 99
                    } },
                    { "draw": { "player": "you", "count": 1 } }
                  ] }
                } }
              } ] }
            ] }
            """));
    internal static Marvel.Cards.Run.AbilityRunner ReenteredNoToughVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
        { "seq": [
          { "discard": { "titled": "Vulture" } },
          { "putIntoPlay": {
            "card": { "titled": "Vulture" },
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
        { "hasStatus": {
          "card": { "titled": "Vulture" }, "status": "tough"
        } }
        """);
    internal static Marvel.Cards.Run.AbilityRunner EnteredNoToughVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
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
        { "hasStatus": {
          "card": { "titled": "Hydra Mercenary" }, "status": "tough"
        } }
        """);
    internal static Marvel.Cards.Run.AbilityRunner HealthModifierVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
        { "seq": [
          { "grantUntil": {
            "card": { "titled": "Spider-Man" },
            "keyword": "health", "amount": 1,
            "until": "EndOfRound"
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
          "value": { "remainingHealth": { "titled": "Spider-Man" } },
          "count": 11
        } }
        """);
    internal static Marvel.Cards.Run.AbilityRunner DepartedAmountVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
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
        { "not": { "atLeast": {
          "value": { "remainingHealth": { "titled": "Vulture" } },
          "count": 1
        } } }
        """);
    internal static Marvel.Cards.Run.AbilityRunner ZeroHealthModifierVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
        { "seq": [
          { "grantUntil": {
            "card": { "titled": "Spider-Man" },
            "keyword": "health", "amount": 0,
            "until": "EndOfRound"
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
          "value": { "remainingHealth": { "titled": "Spider-Man" } },
          "count": 11
        } }
        """);
    internal static Marvel.Cards.Run.AbilityRunner UnrelatedModifiedVillainGrantRunner(bool repeated) => ConditionalVillainGrantRunner(repeated, """
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
          "value": { "modified": {
            "card": { "titled": "Spider-Man" }, "field": "attack"
          } },
          "count": 3
        } }
        """);
}
