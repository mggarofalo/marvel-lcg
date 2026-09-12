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
public sealed class ActionAbilityContinuationsEnteredCardActivatingVillainGrantRaisesTests
{
    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteredCardActivatingVillainGrantRaisesBeforeRepeatedEffectMutates()
    {
        // The repeated-frame trace also treats Hydra Mercenary as in play
        // after its entry. That activates the continuous villain hit-point
        // grant before Klaw advances, so refusal precedes the exhaust cost.
        var runner = ConditionalVillainGrantRunner(repeated: true);
        Card? source = null;
        Card? conditional = null;
        Card? mercenary = null;
        Card? villain = null;
        World? world = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            conditional = board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            villain = board.TheCardIn(DeckType.VillainArea)!;
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:damage.step.1")]
    [Rule("rr:replacement-effect.1")]
    [Fact]
    public void ForcedReplacementCanLeaveARepeatedMoveSourceEmpty()
    {
        // Damage replacement abilities resolve before damage is placed. Once
        // Armored Rhino Suit puts that damage on itself "instead", "the effect
        // is no longer considered imminent" and Rhino has nothing to move.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """, includeAuthored: true);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var villain = board.TheCardIn(DeckType.VillainArea)!;
            board.CreateCard(AuthoredCards.ArmoredSuit, board.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void BoundedThreatRemovalCannotDefeatADistantSideScheme()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Bomb Scare" }, "then": { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var scheme = board.CreateCard("01109", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 3);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void InactiveRemovalBranchDoesNotInflateARepeatedMutationBudget()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Bomb Scare" },
              "then": { "removeThreat": {
                "scheme": { "titled": "Bomb Scare" }, "amount": 1
              } },
              "else": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Bomb Scare" }, "amount": 100
                } },
                { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              ] }
            } } } }
            """);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var scheme = board.CreateCard("01109", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 3);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void RepeatedMutationAnalysisFlowsThroughOnePlayersSequence()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "seq": [
              { "if": {
                "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
                "then": { "draw": { "player": "you", "count": 1 } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } },
              { "discard": "this" },
              { "if": {
                "test": { "not": { "titleInPlay": "Aunt May" } },
                "then": { "changeForm": { "player": "firstPlayer", "to": "alter-ego" } }
              } }
            ] } } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
    }

    [Rule("rr:choose-option")]
    [Fact]
    public void AChoiceContinuationPreservesEarlierEffectResults()
    {
        // "This way" is scoped to the one ability resolution. Asking a
        // question cannot replace that resolution with a fresh result map.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "seq": [ { "heal": { "card": "you", "amount": 1 } }, { "choose": { "options": [ { "exhaust": "this" }, { "ready": "this" } ] } }, { "if": { "test": { "atLeast": { "value": { "result": "healed" }, "count": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""");
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(1);
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void PayOrExhaustContinuesTheContainingSequence()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "seq": [ { "payOrExhaust": { "resources": "YBR", "otherwise": { "exhaust": "this" } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(1));
        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:activation.7")]
    [Fact]
    public void SequentialActivationWaitsUseTheOriginatingFaceAndFreshResults()
    {
        // The first attack changes neither the identity's current face nor the
        // authored face that owns this ability. Its damage result must not leak
        // into the second wait's later condition.
        var runner = Runner(AuthoredCards.SpiderMan, "WhenRevealed", """{ "seq": [ { "changeForm": { "player": "you", "to": "alter-ego" } }, { "seq": [ { "enemyAttacks": { "enemies": { "query": "villain" } } }, { "enemyAttacks": { "enemies": { "query": "villain" } } } ] }, { "if": { "test": { "atLeast": { "value": { "result": "activationDamage" }, "count": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""", eventName: Steps.CardRevealed);
        var(_, world) = Playing(_ =>
        {
        }, hero: true, abilities: runner);
        var identity = world.Seats[0].IdentityCard;
        int held = world.Seats[0].Hand.Cards.Count;
        runner.WhenRevealed(world, identity, 0);
        Assert.Equal("01001b", identity.FaceId);
        var first = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.Attack);
        runner.ActivationCompleted(world, new EnemyActivation(first.Subject, first.Seat, Attacking: true, first.ActivationId, Made: true, DamageDealt: 2));
        var second = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.Attack && step.ActivationId != first.ActivationId);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        runner.ActivationCompleted(world, new EnemyActivation(second.Subject, second.Seat, Attacking: true, second.ActivationId, Made: true, DamageDealt: 0));
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerReconstructionUsesTheOriginatingIdentityFace()
    {
        var runner = Runner(AuthoredCards.SpiderMan, "WhenRevealed", """{ "seq": [ { "heal": { "card": "you", "amount": 1 } }, { "changeForm": { "player": "you", "to": "alter-ego" } }, { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "if": { "test": { "atLeast": { "value": { "result": "healed" }, "count": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""", eventName: Steps.CardRevealed);
        var(_, world) = Playing(board => board.Seats[0].IdentityCard.TakeDamage(1), hero: true, abilities: runner);
        var identity = world.Seats[0].IdentityCard;
        int held = world.Seats[0].Hand.Cards.Count;
        runner.WhenRevealed(world, identity, 0);
        var frame = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.ResolveEachPlayer);
        for (int advances = 0; world.Agenda.Current?.What != Steps.ResolveEachPlayer && advances < 20; advances++)
        {
            world.Agenda.Advance();
        }

        Assert.Equal(Steps.ResolveEachPlayer, world.Agenda.Current?.What);
        runner.ResolveEachPlayer(world, identity, frame.Seat, frame.Index, frame.Tier, frame.FinalStep, frame.FinalPlayer);
        Assert.Equal("01001b", identity.FaceId);
        Assert.Equal(0, identity.Damage);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }
}
