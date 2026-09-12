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
public sealed class ActionAbilityPowerTraceDirectDiscardPreflightsPermanentHostedCardsTests
{
    [Rule("rr:permanent.5")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DirectDiscardPreflightsPermanentHostedCardsBeforeCost()
    {
        // A Permanent card cannot leave play. Discarding its host would require
        // attachment cleanup, so eligibility must refuse before exhausting the
        // source rather than discovering the unsupported cleanup afterwards.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? guard = null;
        Card? permanent = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            guard = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            permanent = board.CreateCard("27189a", board.AreaOf(DeckType.UpgradesArea, guard.Area.PlayArea, guard.ObjectId));
        }, hero: true, abilities: runner));
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? advanced = null;
        World? world = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            advanced = board.CreateCard("01143", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("defeat-triggered ability", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, advanced!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:triggering-condition.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void AnUnrelatedDefeatInterruptDoesNotSuspendALabelledPowerTrace()
    {
        // An interrupt answers only when all of its triggering condition is
        // true. This support's `this` is not the minion the attack would
        // defeat, so the ordinary window filter must exclude it from the
        // read-only trace just as it excludes it during resolution.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [
              { "card": "01006", "abilities": [ {
                "trigger": {
                  "event": "WhenActionTriggered", "timing": "Action",
                  "subject": "game"
                },
                "cost": { "exhaust": "this" },
                "effect": { "attack": {
                  "target": { "titled": "Hydra Mercenary" },
                  "effect": { "seq": [
                    { "dealDamage": {
                      "cards": { "titled": "Hydra Mercenary" },
                      "amount": 100
                    } },
                    { "dealAttackDamage": {
                      "cards": { "query": "villain" }, "amount": 1
                    } }
                  ] }
                } }
              } ] },
              { "card": "01092", "abilities": [ {
                "trigger": {
                  "event": "WhenCardDefeated", "timing": "Interrupt",
                  "subject": "this"
                },
                "effect": { "draw": { "player": "you", "count": 1 } }
              } ] }
            ] }
            """));
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            InPlay(board, "01092");
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? minion = null;
        Card? tracer = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01103", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            tracer = board.CreateCard("01007", board.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId, 0));
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 1);
        }, hero: true, abilities: runner));
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? guard = null;
        Card? tracer = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            guard = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            tracer = board.CreateCard("01007", board.AreaOf(DeckType.UpgradesArea, guard.Area.PlayArea, guard.ObjectId, 0));
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 1);
        }, hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(guard.ObjectId, tracer!.Area.Host);
    }
}
