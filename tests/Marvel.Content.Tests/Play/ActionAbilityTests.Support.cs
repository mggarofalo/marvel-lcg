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

public sealed partial class ActionAbilityTests
{
    /// <summary>`01003` Backflip — a Spider-Man card printing a physical.</summary>
    private const string Physicals = "01003";

    /// <summary>`01004` Enhanced Spider-Sense — the same count, a mental.</summary>
    private const string Mentals = "01004";

    /// <summary>Empties the hand and fills it with physical resources.</summary>
    private static void Physical(World world, int count) =>
        Hand(world, Physicals, count);

    private static void Hand(World world, string faceId, int count) =>
        Hand(world, player: 0, faceId, count);

    private static void Hand(World world, int player, string faceId, int count)
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

    [Rule("rr:resource.4")]
    [Fact]
    public void ThreeOfTheWrongResourceIsNotThreePhysicals()
    {
        // Enough cards and the wrong kind. `rr:resource.4`: "many abilities
        // require specific resource types, and the specified types in the
        // specified quantities must be generated **in order to pay the cost**"
        // -- so a cost of three physicals is not a cost of three.
        //
        // The board is the same as the paying one except for the letter on the
        // cards, which is what makes this a test of the types rather than of
        // the count.
        Card? horn = null;
        var (game, world) = Playing(
            board =>
            {
                horn = board.CreateCard(
                    AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));

                // Through the reveal: `rr:attach-to` makes "Attach to Rhino" a
                // rule about the card entering play rather than a "When
                // Revealed" ability, so the route in is what attaches it.
                board.Abilities = AuthoredCards.Runner();
                Reveal.Resolve(board, Cards, horn, 0, []);
                Hand(board, Mentals, 3);
            },
            hero: true);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);

        // And paying with them anyway is refused by name rather than half-paid:
        // `rr:initiating-abilities.step.5` aborts "without paying any costs".
        var ability = Assert.Single(AuthoredCards.Runner().Actions(world, 0)
            .Where(pending => pending.Card == horn!.ObjectId)
            .DefaultIfEmpty(new PendingAbility(horn!.ObjectId, AbilityType.Action, 0)));

        int before = world.Seats[0].Hand.Cards.Count;
        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => AuthoredCards.Runner().Act(
                world,
                ability,
                [.. world.Seats[0].Hand.Cards.Select(card => card.ObjectId)],
                []));

        Assert.Contains("requiring 'RRR'", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(before, world.Seats[0].Hand.Cards.Count);
    }

    /// <summary>Puts a support into play under the first player.</summary>
    private static Card InPlay(World world, string faceId) => world.CreateCard(
        faceId, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));

    private static Marvel.Cards.Run.AbilityRunner Runner(
        string card,
        string timing,
        string effect,
        string? cost = null,
        string eventName = "WhenActionTriggered",
        string? player = null,
        long? limit = null,
        bool anyPlayer = false,
        bool includeAuthored = false,
        string? labels = null,
        string? maximum = null)
    {
        var local = Marvel.Cards.Dsl.AbilityCatalog.Parse(
            $$"""
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

        var book = new Marvel.Cards.Dsl.AbilityBook(
            [.. AuthoredCards.Book.Abilities, .. local.Abilities],
            AuthoredCards.Book.Authored.Concat(local.Authored)
                .ToHashSet(StringComparer.Ordinal),
            AuthoredCards.Book.AttachTo);
        return new Marvel.Cards.Run.AbilityRunner(book);
    }

    private static Marvel.Cards.Run.AbilityRunner GuardEntryRunner() => Runner(
        AuthoredCards.AuntMay,
        "Action",
        """
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

    private static Marvel.Cards.Run.AbilityRunner ReenteredAttachmentRankRunner(
        string rank) => Runner(
        AuthoredCards.AuntMay,
        "Action",
        $$"""
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

    private static Marvel.Cards.Run.AbilityRunner UnrelatedStatusVillainGrantRunner(
        bool repeated, bool sameCardDifferentStatus = false,
        bool giveStunned = false, bool grantWhenStatusAbsent = false)
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
        string statusTest = sameCardDifferentStatus
            ? """
              { "hasStatus": {
                "card": { "titled": "Spider-Man" }, "status": "stunned"
              } }
              """
            : """
              { "hasStatus": { "card": "this", "status": "tough" } }
              """;
        if (grantWhenStatusAbsent)
        {
            statusTest = $$"""{ "not": {{statusTest}} }""";
        }
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner UnrelatedTraitVillainGrantRunner(
        bool repeated, bool sameCardDifferentTrait = false)
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
        string traitTest = sameCardDifferentTrait
            ? """
              { "hasTrait": {
                "card": { "titled": "Spider-Man" }, "trait": "BRUTE"
              } }
              """
            : """
              { "hasTrait": { "card": "this", "trait": "BRUTE" } }
              """;
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner VulnerableStatusRunner(
        bool repeated)
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
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return Runner(
            AuthoredCards.AuntMay, "Action", effect,
            cost: """{ "exhaust": "this" }""");
    }

    private static Marvel.Cards.Run.AbilityRunner DiscardedTraitVillainGrantRunner(
        bool repeated, string predicate = "trait")
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
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
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
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner ReenteredVulnerableStatusRunner(
        bool repeated)
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
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return Runner(
            AuthoredCards.AuntMay, "Action", effect,
            cost: """{ "exhaust": "this" }""");
    }

    private static Marvel.Cards.Run.AbilityRunner RestoredStatusVillainGrantRunner(
        bool repeated)
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
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner CappedToughVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "giveStatus": { "card": "you", "status": "tough" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "not": { "hasStatus": {
          "card": { "titled": "Spider-Man" }, "status": "tough"
        } } }
        """);

    private static Marvel.Cards.Run.AbilityRunner UnrelatedDamageVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "damageOn": { "titled": "Spider-Man" } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner UnrelatedMinionCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "discard": { "titled": "Vulture" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "count": { "query": "alliesYouControl" } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner EnteredEngagementCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
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
        """,
        """
        { "atLeast": {
          "value": { "count": {
            "query": "minionsEngagedWithYou"
          } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner FormHeroCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "changeForm": { "player": "you", "to": "alter-ego" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "not": { "atLeast": {
          "value": { "count": { "query": "heroes" } },
          "count": 1
        } } }
        """);

    private static Marvel.Cards.Run.AbilityRunner YourHeroCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "changeForm": { "player": "you", "to": "hero" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "count": "yourHero" },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner EliminatedHeroCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
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
        """,
        """
        { "atLeast": {
          "value": { "count": { "query": "heroes" } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner EliminationEngagementCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
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
        """,
        """
        { "atLeast": {
          "value": { "count": {
            "query": "minionsEngagedWithYou"
          } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner EliminationHostedUpgradeCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
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
        """,
        """
        { "atLeast": {
          "value": { "count": { "query": "upgradesYouControl" } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner ControllerFormVillainGrantRunner() =>
        ConditionalVillainGrantRunner(
            false,
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """,
            """
            { "inForm": { "player": "you", "form": "hero" } }
            """);

    private static Marvel.Cards.Run.AbilityRunner RemovedIdentityHealthVillainGrantRunner() =>
        ConditionalVillainGrantRunner(
            false,
            """
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
            """,
            """
            { "atLeast": {
              "value": { "remainingHealth": { "titled": "Spider-Man" } },
              "count": 1
            } }
            """);

    private static Marvel.Cards.Run.AbilityRunner RelocatedUpgradeControllerVillainGrantRunner() =>
        new(Marvel.Cards.Dsl.AbilityCatalog.Parse(
            """
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

    private static Marvel.Cards.Run.AbilityRunner SameTitleNumericRebindingVillainGrantRunner() =>
        ConditionalVillainGrantRunner(
            false,
            """
            { "seq": [
              { "discard": { "titled": "Shocker" } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """,
            """
            { "atLeast": {
              "value": { "damageOn": { "titled": "Shocker" } },
              "count": 1
            } }
            """);

    private static Marvel.Cards.Run.AbilityRunner PermanentEliminationRunner() =>
        new(Marvel.Cards.Dsl.AbilityCatalog.Parse(
            """
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

    private static Marvel.Cards.Run.AbilityRunner ReenteredNoToughVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
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
        """,
        """
        { "hasStatus": {
          "card": { "titled": "Vulture" }, "status": "tough"
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner EnteredNoToughVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
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
        """,
        """
        { "hasStatus": {
          "card": { "titled": "Hydra Mercenary" }, "status": "tough"
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner HealthModifierVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
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
        """,
        """
        { "atLeast": {
          "value": { "remainingHealth": { "titled": "Spider-Man" } },
          "count": 11
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner DepartedAmountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "discard": { "titled": "Vulture" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "not": { "atLeast": {
          "value": { "remainingHealth": { "titled": "Vulture" } },
          "count": 1
        } } }
        """);

    private static Marvel.Cards.Run.AbilityRunner ZeroHealthModifierVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
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
        """,
        """
        { "atLeast": {
          "value": { "remainingHealth": { "titled": "Spider-Man" } },
          "count": 11
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner UnrelatedModifiedVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "modified": {
            "card": { "titled": "Spider-Man" }, "field": "attack"
          } },
          "count": 3
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner EnteredAmountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
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
        """,
        """
        { "atLeast": {
          "value": { "remainingHealth": {
            "titled": "Hydra Mercenary"
          } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner CrossCardModifierVillainGrantRunner(
        bool repeated)
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
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner EnteredTraitModifierVillainGrantRunner(
        bool repeated, bool decisiveFalse = false)
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
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        string modifierTest = decisiveFalse
            ? """
              { "and": [
                { "not": { "exists": { "query": "villain" } } },
                { "hasTrait": {
                  "card": { "titled": "Hydra Mercenary" },
                  "trait": "HYDRA"
                } }
              ] }
              """
            : """
              { "hasTrait": {
                "card": { "titled": "Hydra Mercenary" },
                "trait": "HYDRA"
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner ConditionalVillainGrantRunner(
        bool repeated, string sequence, string test)
    {
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner DiscardedStatusVillainGrantRunner(
        bool repeated)
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
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner TitleInPlayVillainGrantRunner(
        bool grantWhenPresent)
    {
        string branches = grantWhenPresent
            ? """
              "then": { "grant": {
                "card": { "query": "villain" },
                "keyword": "health", "amount": 10
              } }
              """
            : """
              "then": { "grant": {
                "card": "this", "keyword": "attack", "amount": 1
              } },
              "else": { "grant": {
                "card": { "query": "villain" },
                "keyword": "health", "amount": 10
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner DepartingVillainAttachmentRunner()
        => new(Marvel.Cards.Dsl.AbilityCatalog.Parse(
            """
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

    private static Marvel.Cards.Run.AbilityRunner BooleanShortCircuitVillainGrantRunner(
        bool useOr)
    {
        string test = useOr
            ? """
              { "or": [
                { "exists": { "query": "villain" } },
                { "hasStatus": {
                  "card": { "query": "villain" }, "status": "tough"
                } }
              ] }
              """
            : """
              { "and": [
                { "not": { "exists": { "query": "villain" } } },
                { "hasStatus": {
                  "card": { "query": "villain" }, "status": "tough"
                } }
              ] }
              """;
        string branches = useOr
            ? """
              "then": { "grant": {
                "card": "this", "keyword": "attack", "amount": 1
              } },
              "else": { "grant": {
                "card": { "query": "villain" },
                "keyword": "health", "amount": 10
              } }
              """
            : """
              "then": { "grant": {
                "card": { "query": "villain" },
                "keyword": "health", "amount": 10
              } },
              "else": { "grant": {
                "card": "this", "keyword": "attack", "amount": 1
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner OrderedFirstPlayerVillainGrantRunner()
        => new(Marvel.Cards.Dsl.AbilityCatalog.Parse(
            """
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

    private static Marvel.Cards.Run.AbilityRunner VillainTitleExistenceGrantRunner(
        bool grantWhenExists)
    {
        string test = grantWhenExists
            ? """{ "exists": { "titled": "Rhino" } }"""
            : """{ "not": { "exists": { "titled": "Rhino" } } }""";
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner FirstPlayerVillainGrantRunner()
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
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner FirstPlayerConditionalHealthDependencyRunner()
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
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner FormConditionalVillainGrantRunner(
        bool repeated)
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
        string effect = repeated
            ? $$"""{ "eachPlayer": { "effect": {{sequence}} } }"""
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner FormConditionalHealthDependencyRunner()
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
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner ConditionalVillainGrantRunner(
        bool repeated)
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
        string effect = repeated
            ? $$"""
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
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
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

    private static Marvel.Cards.Run.AbilityRunner RepeatedDynamicTargetRunner(
        string firstTarget, string secondTarget,
        string moveSource = """{ "query": "villain" }""",
        bool includeAuthored = false) => Runner(
        AuthoredCards.AuntMay,
        "Action",
        $$"""
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
        """,
        includeAuthored: includeAuthored);

    /// <summary>
    /// A game past the mulligan, on the first player's turn.
    /// </summary>
    /// <remarks>
    /// The board is prepared <b>before</b> the game begins, because a turn
    /// prompt is built once and lists what was there when it was built. A card
    /// put into play afterwards is on the board and not in the question.
    /// </remarks>
    private static (Game Game, World World) Playing(
        Action<World> prepare,
        bool hero = false,
        string[]? heroes = null,
        ICardAbilities? abilities = null,
        string scenario = "rhino")
    {
        string[] playing = heroes ?? ["spider_man"];
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, scenario, playing), Cards),
            [.. playing.Select(name => Setup.Hero(name).Name)],
            12345);

        if (hero)
        {
            world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        }

        prepare(world);

        // The mulligan is asked as a turn option too, so the loop watches the
        // verb rather than the question: declining it keeps the opening hand.
        var game = Game.Begin(world, Cards, abilities ?? AuthoredCards.Runner());
        while (game.Pending is { } asked
            && asked.Affordances.Any(
                option => option.Verb == Game.ResolveMulligans))
        {
            game.Resolve(Decision.Decline);
        }

        return (game, world);
    }
}
