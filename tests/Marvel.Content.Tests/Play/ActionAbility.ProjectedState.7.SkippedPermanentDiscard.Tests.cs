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
public sealed class ActionAbilityProjectedStateSkippedPermanentDiscardTests
{
    [Rule("rr:permanent.4")]
    [Rule("rr:then.1")]
    [Rule("rr:then.2")]
    [Theory]
    [InlineData("then", 0)]
    [InlineData("otherwise", 1)]
    public void SkippedPermanentDiscardHasTheCorrectDependentOutcome(string dependency, int cardsDrawn)
    {
        // The cross-set Permanent is not a legal discard target, so that
        // component resolves none. "Then" does not run; "otherwise" does.
        // A valid exhaust sibling keeps the overall target legal.
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$"""
            { "chooseCard": {
              "from": { "titled": "Compact Darts" },
              "effect": { "seq": [
                { "exhaust": "chosen" },
                { "{{dependency}}": {
                  "effect": { "discard": "chosen" },
                  "{{dependency}}": { "draw": { "player": "you", "count": 1 } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        Card? permanent = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            permanent = board.CreateCard("27182a", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner);
        int hand = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(permanent!.ObjectId));
        Assert.False(permanent.Ready);
        Assert.Equal(DeckType.UpgradesArea, permanent.Area.Type);
        Assert.Equal(hand + cardsDrawn, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:permanent.5")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void PermanentOnLethalTargetRaisesBeforeALabelledActionCostMutates()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 3
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
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? guard = null;
        Card? permanent = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            guard = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            permanent = board.CreateCard("27189a", board.AreaOf(DeckType.UpgradesArea, guard.Area.PlayArea, guard.ObjectId));
            board.Seats[0].IdentityCard.TakeDamage(9);
            board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("rr:permanent.5 is not implemented", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, guard.Area.Type);
        Assert.Equal(guard.ObjectId, permanent!.Area.Host);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:target.3.8")]
    [Fact]
    public void GuardMakesTheVillainInvalidBeforeTracingALabelledAttack()
    {
        // Guard says “The engaged player cannot attack any villain,” and a
        // target that cannot be attacked is not valid for an attack-labeled
        // ability. Later effects cannot first remove that initiation limit.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Hydra Mercenary" },
                  "keyword": "health", "amount": 2, "until": "EndOfRound"
                } },
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
            """);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:defeat.1")]
    [Rule("rr:guard.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DiscardedConstantHealthGrantCannotKeepAGuardAliveInTheTrace()
    {
        // Constant abilities are active only while their source remains in
        // play. Discarding Genetically Enhanced removes its +3 hit points, so
        // the following 3 damage defeats Hydra Mercenary and removes Guard.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Genetically Enhanced" } },
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
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? guard = null;
        Card? enhanced = null;
        World? world = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            guard = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            enhanced = board.CreateCard("01163", board.AreaOf(DeckType.UpgradesArea, guard.Area.PlayArea, guard.ObjectId));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(guard.ObjectId, enhanced!.Area.Host);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DiscardedConstantHealthGrantCannotKeepAVillainStageAliveInTheTrace()
    {
        // Constant abilities are active only while their source remains in
        // play. Discarding The "Immortal" Klaw removes +10 hit points, so the
        // next damage defeats this stage and the new stage begins undamaged.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "The \"Immortal\" Klaw" } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
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
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? villain = null;
        Card? immortal = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            villain = board.TheCardIn(DeckType.VillainArea)!;
            immortal = board.CreateCard("01127", board.AreaOf(DeckType.SideSchemesArea));
            // Rhino I has 28 hit points in this two-player game. Leave him
            // one below that printed maximum; Immortal Klaw raises the
            // live maximum to 38 until the first effect discards it.
            villain.TakeDamage(27);
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(27, villain!.Damage);
        Assert.Equal(DeckType.SideSchemesArea, immortal!.Area.Type);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }
}
