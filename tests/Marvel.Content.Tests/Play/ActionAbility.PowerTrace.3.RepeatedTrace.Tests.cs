using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
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
public sealed class ActionAbilityPowerTraceRepeatedTraceTests
{
    [Rule("rr:ability.step.1")]
    [Rule("rr:hit-points.2.3")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RepeatedTraceUsesHealthAfterAConditionalConstantEnds()
    {
        // The repeated-frame tracer sees the same continuous update as direct
        // reachability: at eight Gene Pool threat, Infinite Soldier has three
        // hit points, so its defeat removes Guard before the next frame.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? pool = null;
        Card? soldier = null;
        Card? villain = null;
        World? world = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            pool = board.CreateCard("45071", board.AreaOf(DeckType.SideSchemesArea));
            pool.PlaceTokens("k_threat", 9);
            soldier = board.CreateCard("45069", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            villain = board.TheCardIn(DeckType.VillainArea)!;
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? pool = null;
        Card? soldier = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            pool = board.CreateCard("45071", board.AreaOf(DeckType.SideSchemesArea));
            pool.PlaceTokens("k_threat", 10);
            soldier = board.CreateCard("45069", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
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
        var runner = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
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
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            pool = board.CreateCard("45071", board.AreaOf(DeckType.SideSchemesArea));
            pool.PlaceTokens("k_threat", 9);
            grants = board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? armor = null;
        World? world = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            board.Seats[0].IdentityCard.TurnTo("01029a");
            source = InPlay(board, AuthoredCards.AuntMay);
            armor = board.CreateCard("01036", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            board.Seats[0].IdentityCard.TakeDamage(8);
        }, heroes: ["iron_man", "captain_marvel"], abilities: runner));
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "enemyAttacks": { "enemies": { "query": "villain" } } }
              ] }
            } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? villain = null;
        Card? permanent = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            villain = board.TheCardIn(DeckType.VillainArea)!;
            board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            permanent = board.CreateCard("27189a", board.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
        }, hero: true, abilities: runner));
        Assert.Contains("rr:permanent.5 is not implemented", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(villain.ObjectId, permanent!.Area.Host);
    }
}
