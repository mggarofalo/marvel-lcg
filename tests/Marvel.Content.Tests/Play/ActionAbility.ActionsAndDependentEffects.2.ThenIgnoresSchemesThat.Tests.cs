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
public sealed class ActionAbilityActionsAndDependentEffectsThenIgnoresSchemesThatTests
{
    [Rule("rr:then.2")]
    [Fact]
    public void ThenIgnoresSchemesThatAreNotValidThreatTargets()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "then": { "effect": { "removeThreat": { "scheme": { "query": "sideSchemes" }, "amount": 2 } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        Card? empty = null;
        Card? threatened = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            empty = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
            threatened = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            threatened.PlaceTokens("k_threat", 2);
        }, hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(0, empty!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, threatened!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AThreatPlacementBeforeThenRaisesBeforeChangingTheBoard()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "then": { "effect": { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner));
        Assert.Contains("none/partial/full", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:otherwise")]
    [Rule("rr:otherwise.1")]
    [Rule("rr:otherwise.1.2")]
    [Rule("rr:otherwise.2")]
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void OtherwiseRequiresThePrecedingEffectToResolveNotAtAll(int threat, bool draws)
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", // `effect` explicitly delimits the preceding effect. That preserves
 // rr:otherwise.2's semicolon/sentence boundary without recovering
        // punctuation after printed text has become an ability tree.
        """{ "otherwise": { "effect": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 2 } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", threat);
        }, hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + (draws ? 1 : 0), world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void OtherwiseResolvesWhenThreatRemovalIsProhibited()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "otherwise": { "effect": { "removeThreat": { "scheme": { "titled": "Countdown to Oblivion" }, "amount": 2 } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""", includeAuthored: true);
        Card? source = null;
        Card? scheme = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            scheme = board.CreateCard("01139b", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 2);
        }, hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise")]
    [Fact]
    public void OtherwiseTreatsAMissingPrecedingTargetAsNoResolution()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "otherwise": { "effect": { "discard": { "titled": "Missing Card" } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise.1.1")]
    [Fact]
    public void OtherwiseResolvesWhenThePrecedingConditionIsFalse()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "otherwise": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "exhaust": "this" } } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.True(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void AnUnusedOtherwiseBranchDoesNotAskForItsChoice()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "otherwise": { "effect": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 2 } }, "otherwise": { "choose": { "options": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] } } } }""", limit: 1);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 1);
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.True(source!.Ready);
    }

    [Rule("rr:then.1")]
    [Fact]
    public void AChoiceInAnInactiveConditionBranchDoesNotSuspendThen()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "then": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "exhaust": "this" }, "else": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ADependentEffectResumesAfterItsChoice()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "then": { "effect": { "choose": { "options": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] } }, "then": { "discard": "this" } } }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
    }

    [Fact]
    public void ADependentEachPlayerEffectResumesTheAbility()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "then": { "effect": { "draw": { "player": "you", "count": 1 } }, "then": { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.False(source!.Ready);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ADependentFirstActivationPersistsItsOuterContinuation()
    {
        var runner = Runner(AuthoredCards.AuntMay, "WhenRevealed", """{ "seq": [ { "then": { "effect": { "draw": { "player": "you", "count": 1 } }, "then": { "enemyAttacks": { "enemies": { "query": "villain" }, "first": "true" } } } }, { "draw": { "player": "you", "count": 1 } } ] }""", eventName: Steps.CardRevealed);
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        runner.WhenRevealed(world, source!, 0);
        var attack = Assert.Single(world.Agenda.Outstanding, pending => pending.What == Steps.Attack);
        runner.ActivationCompleted(world, new EnemyActivation(attack.Subject, attack.Seat, Attacking: true, attack.ActivationId, Made: false));
        Assert.True(source!.Ready);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AChoiceOptionCanSuspendInsideAnd()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "and": [ { "draw": { "player": "you", "count": 1 } }, { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } ] } ] } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(1));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));
        Assert.False(source!.Ready);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }
}
