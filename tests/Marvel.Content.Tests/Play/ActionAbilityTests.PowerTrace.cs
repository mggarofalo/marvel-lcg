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
    [Rule("rr:permanent.5")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DirectDiscardPreflightsPermanentHostedCardsBeforeCost()
    {
        // A Permanent card cannot leave play. Discarding its host would require
        // attachment cleanup, so eligibility must refuse before exhausting the
        // source rather than discovering the unsupported cleanup afterwards.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? guard = null;
        Card? permanent = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                guard = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                permanent = board.CreateCard(
                    "27189a",
                    board.AreaOf(
                        DeckType.UpgradesArea, guard.Area.PlayArea,
                        guard.ObjectId));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("rr:permanent.5 is not implemented", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, guard!.Area.Type);
        Assert.Equal(guard.ObjectId, permanent!.Area.Host);
    }

    [Rule("rr:damage.step.7")]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void CharacterWhenDefeatedRaisesBeforeALabelledPowerMutates()
    {
        // When Defeated resolves before the defeated card leaves play. Advanced
        // Ultron Drone creates another Drone at that point, so eligibility must
        // refuse rather than trace the later effects against an empty board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Advanced Ultron Drone" }, "amount": 100
                } },
                { "grantUntil": {
                  "card": { "query": "dronesEngagedWithYou" },
                  "keyword": "health", "amount": 1, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "query": "drones" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "dronesEngagedWithYou" },
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
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? advanced = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                advanced = board.CreateCard(
                    "01143",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("defeat-triggered ability", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, advanced!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:damage.step.7")]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ExternalDefeatInterruptRaisesBeforeALabelledPowerMutates()
    {
        // Damage step 7 resolves every forced interrupt that answers the
        // defeat before step 8 discards the character. Spider-Tracer answers
        // its host's defeat and asks the player to choose a scheme.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "titled": "Shocker" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "minionsEngagedWithYou" }, "amount": 100
                } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? minion = null;
        Card? tracer = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                tracer = board.CreateCard(
                    "01007",
                    board.AreaOf(
                        DeckType.UpgradesArea, minion.Area.PlayArea,
                        minion.ObjectId, 0));
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 1);
            },
            hero: true,
            abilities: runner));

        Assert.Contains("defeat-triggered ability", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(minion.ObjectId, tracer!.Area.Host);
    }

    [Rule("rr:damage.step.7")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EarlierDiscardedDefeatInterruptDoesNotCauseAFalseRefusal()
    {
        // Spider-Tracer only answers while it remains attached. Once the first
        // effect discards it, defeating its former host has no step-7 ability
        // to resolve and the labelled sequence is safe to advertise.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "titled": "Hydra Mercenary" },
              "effect": { "seq": [
                { "discard": { "titled": "Spider-Tracer" } },
                { "dealDamage": {
                  "cards": { "query": "minionsEngagedWithYou" }, "amount": 100
                } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? guard = null;
        Card? tracer = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                guard = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                tracer = board.CreateCard(
                    "01007",
                    board.AreaOf(
                        DeckType.UpgradesArea, guard.Area.PlayArea,
                        guard.ObjectId, 0));
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 1);
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(guard.ObjectId, tracer!.Area.Host);
    }

    [Rule("rr:damage.step.6")]
    [Rule("rr:would.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void WouldBeDefeatedInterruptRaisesBeforeALabelledPowerMutates()
    {
        // Step 6 resolves "would be defeated" interrupts after damage is
        // placed and before defeat. Biomechanical Upgrades heals its host and
        // discards itself, invalidating the imminent defeat under rr:would.1.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
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
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? guard = null;
        Card? upgrade = null;
        Card? villain = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                guard = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                upgrade = board.CreateCard(
                    "01185",
                    board.AreaOf(
                        DeckType.UpgradesArea, guard.Area.PlayArea,
                        guard.ObjectId));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("step-6 interrupt", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(guard.ObjectId, upgrade!.Area.Host);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:damage.step.7")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void UnauthoredPrintedDefeatAbilityRaisesBeforeLabelledCost()
    {
        // Goblin Soldier prints a When Defeated ability that has no authored
        // behavior. The engine raises rather than guessing, and eligibility
        // must do so before either the cost or lethal damage mutates the board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Goblin Soldier" }, "amount": 100
                } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? soldier = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                soldier = board.CreateCard(
                    "02023",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("defeat-triggered ability", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, soldier!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, soldier.Area.Type);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CompiledHealthProjectionUsesItsSnapshotAndTheProjectedThreat(bool each, bool replacePredicate)
    {
        string grant = each
            ? """{"grantEach":{"cards":{"query":"minions"},"keyword":"health","amount":3}}"""
            : """{"grant":{"card":"this","keyword":"health","amount":3}}""";
        var parsed = AbilityCatalog.Parse($$$$$$"""
            {"cards":[
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"exhaust":"this"},
                "effect":{"attack":{"target":{"query":"villain"},"effect":{"seq":[
                  {"removeThreat":{"scheme":{"query":"mainScheme"},"amount":1}},
                  {"dealDamage":{"cards":{"titled":"Hydra Mercenary"},"amount":3}},
                  {"dealDamage":{"cards":{"query":"attackableEnemies"},"amount":1}},
                  {"moveDamage":{"from":{"query":"villain"},"to":{"titled":"Spider-Man"},"amount":1}},
                  {"if":{"test":{"inForm":{"player":"firstPlayer","form":"hero"}},
                    "then":{"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}},
                    "else":{"enemyAttacks":{"enemies":{"query":"villain"}}}}}
                ]}}}
              }]},
              {"card":"01101","abilities":[{
                "trigger":{"timing":"Constant","subject":"this"},
                "effect":{"if":{
                  "test":{"atLeast":{"value":{"tokensOn":{"query":"mainScheme"}},"count":2}},
                  "then":{{{{{{grant}}}}}}
                }}
              }]}
            ]}
            """);
        var fields = ((AbilityValue.Map)parsed.Abilities[1].Effect.Argument).Entries
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var constant = parsed.Abilities[1] with { Effect = new AbilityNode("if", new AbilityValue.Map(fields)) };
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook([parsed.Abilities[0], constant], parsed.Authored));
        if (replacePredicate)
        {
            fields["test"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
            {
                ["atLeast"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
                {
                    ["value"] = new AbilityValue.Number(1),
                    ["count"] = new AbilityValue.Number(1),
                }),
            });
        }
        World? world = null;
        Card? source = null;
        Card? guard = null;
        Card? scheme = null;
        // Engine-choice fixture: a synthetic health constant is explicitly
        // installed on a Core minion. Compilation freezes its predicate, while
        // the preview must remove its health bonus after the threat change.
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            guard = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
            scheme.PlaceTokens("k_threat", 2);
            board.Seats[0].IdentityCard.TakeDamage(9);
            Assert.Equal(3, Assert.Single(runner.Constant(board, guard)).Amount);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(6, Damage.Health(world!, Cards, guard!));
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:hit-points.2.3")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ChangedThreatConditionCannotLeaveStaleConstantHealthInTheTrace()
    {
        // Constant abilities update whenever the game state changes. Infinite
        // Soldier has +3 hit points only while Gene Pool has at least 9 threat;
        // removing one threat makes the following 3 damage lethal.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Gene Pool" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "titled": "Infinite Soldier" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
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
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? pool = null;
        Card? soldier = null;
        Card? villain = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                pool = board.CreateCard(
                    "45071", board.AreaOf(DeckType.SideSchemesArea));
                pool.PlaceTokens("k_threat", 9);
                soldier = board.CreateCard(
                    "45069",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(9, pool!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, soldier!.Damage);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:hit-points.2.3")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RepeatedTraceUsesHealthAfterAConditionalConstantEnds()
    {
        // The repeated-frame tracer sees the same continuous update as direct
        // reachability: at eight Gene Pool threat, Infinite Soldier has three
        // hit points, so its defeat removes Guard before the next frame.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": {
                "player": "firstPlayer", "form": "hero"
              } },
              "then": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Gene Pool" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "titled": "Infinite Soldier" }, "amount": 3
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
                "effect": { "enemyAttacks": {
                  "enemies": { "query": "villain" }
                } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? pool = null;
        Card? soldier = null;
        Card? villain = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                pool = board.CreateCard(
                    "45071", board.AreaOf(DeckType.SideSchemesArea));
                pool.PlaceTokens("k_threat", 9);
                soldier = board.CreateCard(
                    "45069",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(9, pool!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, soldier!.Damage);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:target.3.8")]
    [Fact]
    public void GuardPreventsTracingAnOtherwiseSafeLabelledAttack()
    {
        // Guard says “The engaged player cannot attack any villain,” and a
        // target that cannot be attacked is not valid for an attack-labeled
        // ability. Trace safety cannot make the current target legal.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Gene Pool" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "titled": "Infinite Soldier" }, "amount": 1
                } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? pool = null;
        Card? soldier = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                pool = board.CreateCard(
                    "45071", board.AreaOf(DeckType.SideSchemesArea));
                pool.PlaceTokens("k_threat", 10);
                soldier = board.CreateCard(
                    "45069",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(10, pool!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, soldier!.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:hit-points.2.3")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void OneEndingConditionalGrantDoesNotRemoveAnotherFromTheSameSource()
    {
        // At eight threat the >=9 grant ends while the >=5 grant from the same
        // source remains. Trace health is therefore 13, not the printed 10,
        // and one damage at nine does not eliminate Spider-Man.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
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
                        { "removeThreat": {
                          "scheme": { "titled": "Gene Pool" }, "amount": 1
                        } },
                        { "dealDamage": {
                          "cards": { "titled": "Spider-Man" }, "amount": 1
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
                  } ] },
                  { "card": "01092", "abilities": [
                    {
                      "trigger": { "timing": "Constant", "subject": "this" },
                      "effect": { "if": {
                        "test": { "atLeast": {
                          "value": { "tokensOn": { "titled": "Gene Pool" } },
                          "count": 9
                        } },
                        "then": { "grant": {
                          "card": "you", "keyword": "health", "amount": 3
                        } }
                      } }
                    },
                    {
                      "trigger": { "timing": "Constant", "subject": "this" },
                      "effect": { "if": {
                        "test": { "atLeast": {
                          "value": { "tokensOn": { "titled": "Gene Pool" } },
                          "count": 5
                        } },
                        "then": { "grant": {
                          "card": "you", "keyword": "health", "amount": 3
                        } }
                      } }
                    }
                  ] }
                ] }
                """));
        Card? source = null;
        Card? pool = null;
        Card? grants = null;

        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                pool = board.CreateCard(
                    "45071", board.AreaOf(DeckType.SideSchemesArea));
                pool.PlaceTokens("k_threat", 9);
                grants = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, pool!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.SupportsArea, grants!.Area.Type);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:hit-points.2.3")]
    [Rule("rr:player-elimination")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DiscardedIdentityHealthGrantRebindsFirstPlayerInTheTrace()
    {
        // Mark V Armor raises Iron Man from 9 to 15 hit points. Once the first
        // effect discards it, one more damage at eight is lethal and the first
        // player token moves before the following form-dependent branch.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Mark V Armor" } },
                { "dealDamage": {
                  "cards": { "titled": "Iron Man" }, "amount": 1
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
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? armor = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                board.Seats[0].IdentityCard.TurnTo("01029a");
                source = InPlay(board, AuthoredCards.AuntMay);
                armor = board.CreateCard(
                    "01036",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            heroes: ["iron_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.UpgradesArea, armor!.Area.Type);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:permanent.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void PermanentOnDepartingVillainRaisesBeforeALabelledCostMutates()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "enemyAttacks": { "enemies": { "query": "villain" } } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? villain = null;
        Card? permanent = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                permanent = board.CreateCard(
                    "27189a",
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("rr:permanent.5 is not implemented", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(villain.ObjectId, permanent!.Area.Host);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteringVillainConstantRaisesBeforeALabelledPowerMutates()
    {
        // Constant abilities apply as soon as their source enters play. Ultron
        // III therefore gives each Drone +1 hit point when the stage advances;
        // the eligibility trace refuses to guess at that changed board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "query": "drones" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
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
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? villain = null;
        Card? drone = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                drone = FacedownDrones.EngageTop(
                    board, 0, "test", "Create_Drone", []);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("constant abilities", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(0, drone!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RetargetingVillainConstantRaisesBeforeALabelledPowerMutates()
    {
        // When Klaw I is defeated, Klaw II enters play and becomes the
        // villain. The Immortal Klaw's continuous +10 hit points therefore
        // applies to Klaw II before the next effect resolves; the eligibility
        // trace refuses to keep that modifier bound to the defeated stage.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 36
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
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? villain = null;
        Card? immortal = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                immortal = board.CreateCard(
                    "01127", board.AreaOf(DeckType.SideSchemesArea));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(DeckType.SideSchemesArea, immortal!.Area.Type);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void InactiveConstantVillainGrantDoesNotPreventStageAdvancement()
    {
        // Bomb Scare is not in play before or after Klaw I is defeated, so
        // this conditional constant never grants hit points to either stage.
        // Only an active branch can retarget and make the trace unsafe.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
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
                      "test": { "titleInPlay": "Bomb Scare" },
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
        Card? source = null;
        Card? conditional = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
    }

    [Fact]
    public void UnprojectedConstantCountersAreAValidButUnsupportedSituation()
    {
        var runner = ConditionalVillainGrantRunner(false, """
            {"seq":[
              {"dealDamage":{"cards":{"query":"villain"},"amount":100}},
              {"dealDamage":{"cards":{"query":"villain"},"amount":1}}
            ]}
            """, """
            {"atLeast":{"value":{"countersOn":{"card":"this","counter":"condition"}},"count":1}}
            """);
        Card? source = null;
        Card? conditional = null;
        World? world = null;

        // Engine choice: the counter expression is valid authored data, but
        // this preview cannot project it. Refuse before paying or applying it.
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            conditional = board.CreateCard("01092",
                board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner, scenario: "klaw"));

        Assert.Contains("all-purpose counters", refused.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(0, conditional!.Tokens.GetValueOrDefault("c_condition"));
        Assert.Equal(0, world!.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void InvariantAmountTestDoesNotActivateVillainGrantBranch()
    {
        // One is at least one regardless of damage placed during the trace.
        // The live branch grants this support attack; the unreachable else
        // branch cannot retarget its villain health grant during advancement.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
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
                      "test": { "atLeast": { "value": 1, "count": 1 } },
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
        Card? source = null;
        Card? conditional = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void VillainExistenceRemainsTrueAcrossStageAdvancement()
    {
        // Klaw II replaces Klaw I during advancement, so a villain exists on
        // both sides of the transition. The impossible else branch cannot
        // contribute a continuous villain health grant.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
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
                      "test": { "exists": { "query": "villain" } },
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
        Card? source = null;
        Card? conditional = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void OldVillainTitleEndingActivatesGrantBeforeNewStageContinues()
    {
        // Rhino leaves play before Ultron III enters. The absence of a card
        // titled Rhino activates this conditional villain health grant, so the
        // continuation cannot use the unchanged board's old-title answer.
        var runner = VillainTitleExistenceGrantRunner(grantWhenExists: false);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01094", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void OldVillainTitleEndingDeactivatesGrantBeforeNewStageContinues()
    {
        // This inverse grant is active only while Rhino exists. Moving to
        // Ultron III ends it, so the unreachable branch does not prevent the
        // otherwise traceable continuation from being advertised.
        var runner = VillainTitleExistenceGrantRunner(grantWhenExists: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DecisiveBooleanConstantBranchRemainsTraceable(bool useOr)
    {
        // A known true decides OR and a known false decides AND even when the
        // other operand reads the advancing villain's status. In either case
        // the branch containing the villain health grant is unreachable.
        var runner = BooleanShortCircuitVillainGrantRunner(useOr);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteringSameTitleStageKeepsVillainGrantActive()
    {
        // Klaw II enters as Klaw I leaves, so a card titled Klaw remains in
        // play throughout advancement. The title-gated health grant follows
        // the new villain and must be refused before the labelled cost.
        var runner = TitleInPlayVillainGrantRunner(grantWhenPresent: true);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteringSameTitleStageKeepsInverseGrantInactive()
    {
        // The inverse branch stays inactive because Klaw II preserves the
        // title's in-play truth. Its unreachable villain grant does not block
        // advertising the continuation.
        var runner = TitleInPlayVillainGrantRunner(grantWhenPresent: false);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnrelatedToughChangeDoesNotActivateSupportGrant(bool repeated)
    {
        // Giving an identity Tough does not make this support Tough. Its
        // conditional villain health grant stays inactive in both direct and
        // repeated traces, so unrelated status state cannot block the action.
        var runner = UnrelatedStatusVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToughChangeDoesNotInvalidateStunnedPredicate(bool repeated)
    {
        // Spider-Man gains Tough, but his Stunned state remains false. Status
        // invalidation is keyed by both card and status, so the unreachable
        // villain health grant remains inactive in either trace shape.
        var runner = UnrelatedStatusVillainGrantRunner(
            repeated, sameCardDifferentStatus: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StunnedGainActivatesVillainGrantBeforeAdvancement(bool repeated)
    {
        // Giving Spider-Man Stunned makes the matching conditional constant
        // active before Klaw advances. The new-stage health grant cannot be
        // projected from the unchanged board, so refusal precedes the cost.
        var runner = UnrelatedStatusVillainGrantRunner(
            repeated, sameCardDifferentStatus: true, giveStunned: true);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnrelatedTraitChangeDoesNotActivateSupportGrant(bool repeated)
    {
        // Giving an identity Aerial does not give this support Brute. Its
        // conditional villain health grant stays inactive in direct and
        // repeated traces, so an unrelated trait cannot block the action.
        var runner = UnrelatedTraitVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AerialGainDoesNotInvalidateBrutePredicate(bool repeated)
    {
        // Spider-Man gains Aerial, but his Brute predicate remains false.
        // Trait invalidation is keyed by both card and trait, so its inactive
        // villain grant cannot hide either shape of the legal action.
        var runner = UnrelatedTraitVillainGrantRunner(
            repeated, sameCardDifferentTrait: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attach-to.1")]
    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LeavingHostedCardInvalidatesItsTraitPredicate(bool repeated)
    {
        // A different-title villain stage discards the old stage's hosted
        // attachment. Enhanced Ivory Horn therefore stops being an in-play
        // Weapon and activates the new-stage grant before either traced cost.
        var runner = DiscardedTraitVillainGrantRunner(repeated);
        Card? source = null;
        Card? horn = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                horn = board.CreateCard(
                    "01100",
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(villain.ObjectId, horn!.Area.Host);
    }

    [Rule("rr:attach-to.1")]
    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false, "kind")]
    [InlineData(true, "kind")]
    [InlineData(false, "title")]
    [InlineData(true, "title")]
    public void LeavingHostedCardInvalidatesItsIdentityPredicate(
        bool repeated, string predicate)
    {
        // Once Rocket Boots leaves with the old villain stage, it is neither
        // an in-play upgrade nor the in-play card of that title.
        // Both exact predicates therefore activate the new-stage grant.
        var runner = DiscardedTraitVillainGrantRunner(repeated, predicate);
        Card? source = null;
        Card? boots = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                boots = board.CreateCard(
                    "01039",
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(villain.ObjectId, boots!.Area.Host);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CappedStatusGrantDoesNotChangeStatusPredicate(bool repeated)
    {
        // A character cannot receive a second status of the same type. Giving
        // Spider-Man Stunned while he already carries it is a no-op, so the
        // inverse predicate and its villain grant remain inactive.
        var runner = UnrelatedStatusVillainGrantRunner(
            repeated, sameCardDifferentStatus: true,
            giveStunned: true, grantWhenStatusAbsent: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                Statuses.Give(
                    board, board.Seats[0].IdentityCard, Statuses.Stunned);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CappedToughGrantDoesNotChangeStatusPredicate(bool repeated)
    {
        // Spider-Man already has Tough, so another grant is capped and leaves
        // the inverse predicate false. Neither direct nor repeated preflight
        // may explore its inactive villain-health branch.
        var runner = CappedToughVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                Statuses.Give(
                    board, board.Seats[0].IdentityCard, Statuses.Tough);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VillainDamageDoesNotInvalidateHeroDamagePredicate(bool repeated)
    {
        // Damage on the villain does not put damage on Spider-Man. His numeric
        // predicate remains false, so the inactive villain-health grant cannot
        // reject either otherwise legal trace shape.
        var runner = UnrelatedDamageVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:discard.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MinionDepartureDoesNotInvalidateAllyCount(bool repeated)
    {
        // Discarding an engaged minion does not change the number of allies
        // this player controls. The ally-count condition remains false, so its
        // inactive villain grant cannot reject the legal action.
        var runner = UnrelatedMinionCountVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01167",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:engage.1")]
    [Rule("rr:engage.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnteredMinionInvalidatesEngagementCount(bool repeated)
    {
        // A minion an ability instructs a player to engage "is also considered
        // to have engaged that player." Putting Hydra Mercenary into play this
        // way changes the engaged-minion count from zero to one, which must be
        // recognized before paying the cost.
        var runner = EnteredEngagementCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormChangeInvalidatesHeroCount(bool repeated)
    {
        // Spider-Man is the only hero in play. Changing him to alter-ego makes
        // the hero count zero and activates the conditional villain grant
        // before Klaw advances, so the cost must remain unpaid.
        var runner = FormHeroCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormChangeInvalidatesYourHeroCount(bool repeated)
    {
        // The resolving player begins in alter-ego, so "yourHero" names no
        // card. Changing to hero makes its count one and activates the villain
        // grant before Klaw advances.
        var runner = YourHeroCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:player-elimination.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeroEliminationDeactivatesHeroCountGrant(bool repeated)
    {
        // Spider-Man is the only hero. His elimination removes him from the
        // player-order-backed hero query, so the live villain grant ends before
        // Klaw advances and cannot reject the otherwise legal action.
        var runner = EliminatedHeroCountVillainGrantRunner(repeated);
        Card? source = null;

        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:player-elimination.step.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EliminationReengagementActivatesMinionCountGrant(bool repeated)
    {
        // Hydra Mercenary begins with player one. Eliminating that player
        // makes the minion engage player zero, activating player zero's
        // engagement-count villain grant before Klaw advances.
        var runner = EliminationEngagementCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), mercenary!.Area.PlayArea);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:player-elimination.step.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EliminationReengagementMovesHostedUpgradeForCount(bool repeated)
    {
        // Re-engagement moves the minion's complete hosted tree. The hosted
        // upgrade therefore enters player zero's play area and activates that
        // player's upgrade-count villain grant before Klaw advances.
        var runner = EliminationHostedUpgradeCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? implant = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
                implant = board.CreateCard(
                    "04119",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(1),
                        mercenary.ObjectId));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), mercenary!.Area.PlayArea);
        Assert.Equal(PlayArea.Of(1), implant!.Area.PlayArea);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ConstantYouUsesItsControllersFormDuringPreflight()
    {
        // Player one controls the constant, so its "you" reads player one even
        // though player zero initiates the labelled action.
        var runner = ControllerFormVillainGrantRunner();
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                board.Seats[1].IdentityCard.TurnTo("01010a");
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ConstantYouDoesNotUseInitiatorsFormDuringPreflight()
    {
        // Player zero is in hero form, but player one's alter-ego controls the
        // constant. Its inactive branch cannot be borrowed from the initiator.
        var runner = ControllerFormVillainGrantRunner();
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:player-elimination.step.2")]
    [Rule("rr:ownership-and-control.5")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RelocatedUpgradeControllerUsesItsProjectedPlayArea()
    {
        // Spider-Tracer is a player upgrade hosted by player one's minion.
        // Re-engagement moves it to hero player zero, changing its controller
        // and activating its controller-form villain grant.
        var runner = RelocatedUpgradeControllerVillainGrantRunner();
        Card? source = null;
        Card? tracer = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
                tracer = board.CreateCard(
                    "01007",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(1),
                        mercenary.ObjectId, cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), tracer!.Area.PlayArea);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:referential-ability.step.3")]
    [Fact]
    public void PlayerCardReferenceDoesNotTrackSameTitledEncounterCards()
    {
        // A player card's ambiguous title reference resolves only among player
        // cards. Neither Shocker is therefore the numeric target, so
        // removing one cannot retarget the reference to the other. The
        // independently valid villain target keeps the action initiable.
        var runner = SameTitleNumericRebindingVillainGrantRunner();
        Card? source = null;
        Card? first = null;
        Card? villain = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                first = board.CreateCard(
                    "01103",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                var second = board.CreateCard(
                    "01103",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                second.TakeDamage(1);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, first!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }

}
