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
internal static class ActionAbilitySupportFixtures
{
    /// <summary>`01003` Backflip — a Spider-Man card printing a physical.</summary>
    internal const string Physicals = "01003";
    /// <summary>`01004` Enhanced Spider-Sense — the same count, a mental.</summary>
    internal const string Mentals = "01004";
    /// <summary>Empties the hand and fills it with physical resources.</summary>
    internal static void Physical(World world, int count) => Hand(world, Physicals, count);
    internal static void Hand(World world, string faceId, int count) => Hand(world, player: 0, faceId, count);
    internal static void Hand(World world, int player, string faceId, int count)
    {
        foreach (var card in world.Seats[player].Hand.Cards.ToList())
        {
            World.MoveToTop(card, world.Seats[player].Deck);
        }

        for (int made = 0; made < count; made++)
        {
            world.CreateCard(faceId, world.Seats[player].Hand);
        }
    }

    /// <summary>Puts a support into play under the first player.</summary>
    internal static Card InPlay(World world, string faceId) => world.CreateCard(faceId, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
    internal static Marvel.Cards.Run.AbilityRunner Runner(string card, string timing, string effect, string? cost = null, string eventName = "WhenActionTriggered", string? player = null, long? limit = null, bool anyPlayer = false, bool includeAuthored = false, string? labels = null, string? maximum = null)
    {
        var local = Marvel.Cards.Dsl.AbilityCatalog.Parse($$"""
            { "cards": [ { "card": "{{card}}", "abilities": [ {
                "trigger": { "event": "{{eventName}}", "timing": "{{timing}}", "subject": "game"{{(player is null ? string.Empty : $", \"player\": \"{player}\"")}} },
                {{(cost is null ? string.Empty : $"\"cost\": {cost},")}}
                {{(limit is null ? string.Empty : $"\"limitPerRound\": {limit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},")}}
                {{(anyPlayer ? "\"anyPlayer\": true," : string.Empty)}}
                {{(labels is null ? string.Empty : $"\"labels\": {labels},")}}
                {{(maximum is null ? string.Empty : $"\"maxPer{maximum}\": 1,")}}
                "effect": {{effect}}
            } ] } ] }
            """);
        if (!includeAuthored)
        {
            return new Marvel.Cards.Run.AbilityRunner(local);
        }

        var book = new Marvel.Cards.Dsl.AbilityBook([..AuthoredCards.Book.Abilities, ..local.Abilities], AuthoredCards.Book.Authored.Concat(local.Authored).ToHashSet(StringComparer.Ordinal), AuthoredCards.Book.AttachTo);
        return new Marvel.Cards.Run.AbilityRunner(book);
    }

    internal static Marvel.Cards.Run.AbilityRunner GuardEntryRunner() => Runner(AuthoredCards.AuntMay, "Action", """
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
    internal static Marvel.Cards.Run.AbilityRunner ReenteredAttachmentRankRunner(string rank) => Runner(AuthoredCards.AuntMay, "Action", $$"""
        { "eachPlayer": { "effect": { "if": {
          "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
          "then": { "seq": [
            { "discard": { "titled": "Hydra Mercenary" } },
            { "putIntoPlay": {
              "card": { "titled": "Hydra Mercenary" },
              "where": "engagedWithYou"
            } },
            { "dealDamage": {
              "cards": { "{{rank}}": {
                "of": { "query": "enemies" }, "by": "attack"
              } },
              "amount": 1
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
    internal static Marvel.Cards.Run.AbilityRunner UnrelatedStatusVillainGrantRunner(bool repeated, bool sameCardDifferentStatus = false, bool giveStunned = false, bool grantWhenStatusAbsent = false)
    {
        string givenStatus = giveStunned ? "stunned" : "tough";
        string sequence = $$"""
            { "seq": [
              { "giveStatus": { "card": "you", "status": "{{givenStatus}}" } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string statusTest = sameCardDifferentStatus ? """
              { "hasStatus": {
                "card": { "titled": "Spider-Man" }, "status": "stunned"
              } }
              """ : """
              { "hasStatus": { "card": "this", "status": "tough" } }
              """;
        if (grantWhenStatusAbsent)
        {
            statusTest = $$"""{ "not": {{statusTest}} }""";
        }

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
                      "test": {{statusTest}},
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    internal static Marvel.Cards.Run.AbilityRunner UnrelatedTraitVillainGrantRunner(bool repeated, bool sameCardDifferentTrait = false)
    {
        const string sequence = """
            { "seq": [
              { "grantUntil": {
                "card": "you", "trait": "AERIAL", "until": "EndOfAttack"
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string traitTest = sameCardDifferentTrait ? """
              { "hasTrait": {
                "card": { "titled": "Spider-Man" }, "trait": "BRUTE"
              } }
              """ : """
              { "hasTrait": { "card": "this", "trait": "BRUTE" } }
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
                      "test": {{traitTest}},
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    internal static Marvel.Cards.Run.AbilityRunner VulnerableStatusRunner(bool repeated)
    {
        const string sequence = """
            { "seq": [
              { "giveStatus": {
                "card": { "titled": "A.I.M. Scientist" },
                "status": "stunned"
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
        return Runner(AuthoredCards.AuntMay, "Action", effect, cost: """{ "exhaust": "this" }""");
    }
}
