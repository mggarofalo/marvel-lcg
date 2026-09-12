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
public sealed class ActionAbilityRepeatedEffectsForEachDamageTests
{
    [Rule("rr:for-each.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void ForEachDamageIsFullyTracedBeforeARepeatedFrameCanMutate()
    {
        // The first frame's two points are one combined for-each instance and
        // eliminate Spider-Man at two remaining hit points. That changes the
        // first player before the next frame and exposes the unsupported
        // branch. Initiation must trace both points and refuse before the
        // exhaust cost or damage can mutate the board.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "forEach": { "count": 2, "effect": {
                "dealDamage": { "cards": { "titled": "Spider-Man" }, "amount": 1 }
              } } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """, cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(8);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:for-each.3")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void MutableForEachAmountsFailClosedDuringRepeatedTrace()
    {
        // Each chosen instance would read damageOn again: three damage first,
        // then six, not the same three copied twice. The trace cannot yet
        // evaluate that expression against its intermediate board, so it must
        // refuse before an earlier frame can eliminate the first player and
        // expose the unsupported branch for the next one.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "forEach": { "count": 2, "effect": { "chooseCard": {
                "from": { "query": "villain" },
                "effect": { "dealDamage": {
                  "cards": { "titled": "Spider-Man" },
                  "amount": { "damageOn": { "titled": "Spider-Man" } }
                } }
              } } } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(3);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("between traced iterations", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(3, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:for-each.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void MultiTargetForEachIsNotOfferedBeforeItsCost()
    {
        // Without “choose,” for-each applies to one target. Two matching
        // minions make this authored selector unsupported. The exact-one
        // boundary is an initiation check so the action cannot first exhaust
        // its source and only then discover the problem.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "forEach": { "count": 2, "effect": {
              "dealDamage": { "cards": { "query": "minions" }, "amount": 1 }
            } } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("01121", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:for-each.1")]
    [Fact]
    public void AStableVillainTargetSurvivesAnExhaustCost()
    {
        // Exhausting this support cannot change which single card occupies the
        // villain area. The conservative mutation boundary therefore keeps
        // this supported target shape available.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "forEach": { "count": 2, "effect": {
              "dealDamage": { "cards": { "query": "villain" }, "amount": 1 }
            } } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AForEachTargetCannotAppearAfterPaymentOrAnEarlierStep()
    {
        // The action starts with one minion, but the preceding step would put
        // a second into play. Because the no-choice target is not yet a
        // persisted binding, initiation refuses this changing-cardinality
        // shape before either exhausting Aunt May or moving Sandman.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDiscardPile" ], "title": "Sandman"
                } },
                "where": "engagedWithYou"
              } },
              { "forEach": { "count": 2, "effect": {
                "dealDamage": { "cards": { "query": "minions" }, "amount": 1 }
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;
        Card? sandman = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            sandman = board.CreateCard("01102", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.Contains("after state may change", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, sandman!.Area.Type);
        Assert.Equal(0, world!.Cards[source.ObjectId].Damage);
    }

    [Rule("rr:for-each")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void NegativeForEachCountIsRejectedBeforeItsCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "forEach": { "count": -1, "effect": { "draw": { "player": "you", "count": 1 } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var refused = Assert.Throws<AbilityException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner));
        Assert.Contains("non-negative", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:alteration-effect")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void NegativeEachTimeCountIsRejectedBeforeItsCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachTime": {
              "effect": { "discardTop": { "from": "encounterDeck", "count": -1 } },
              "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
              "then": { "draw": { "player": "you", "count": 1 } }
            } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var refused = Assert.Throws<AbilityException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner));
        Assert.Contains("non-negative", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:alteration-effect")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void MutableEachTimeCountAfterAnEarlierEffectIsRejectedBeforeItsCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "heal": { "card": "you", "amount": 1 } },
              { "eachTime": {
                "effect": { "discardTop": {
                  "from": "encounterDeck",
                  "count": { "add": [ -1, { "damageOn": "you" } ] }
                } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "draw": { "player": "you", "count": 1 } }
              } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(1);
        }, hero: true, abilities: runner));
        Assert.Contains("each-time count after state may change", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(1, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:for-each")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void DynamicNegativeForEachCountIsRejectedBeforeALabelledPowerCost()
    {
        // A changing count still has a definite value at initiation. It must
        // be validated before suspension analysis can schedule the attack and
        // pay its cost; mutability only determines whether zero can prune the
        // body.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "forEach": {
                "count": { "add": [ -1, { "damageOn": "you" } ] },
                "effect": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } }
            } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var refused = Assert.Throws<AbilityException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner));
        Assert.Contains("non-negative", refused.Message);
        Assert.True(source!.Ready);
    }
}
