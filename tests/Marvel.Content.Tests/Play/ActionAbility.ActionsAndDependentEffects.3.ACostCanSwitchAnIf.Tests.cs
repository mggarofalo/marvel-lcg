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
public sealed class ActionAbilityActionsAndDependentEffectsACostCanSwitchAnIfTests
{
    [Fact]
    public void ACostCanSwitchAnIfIntoAResumableBranch()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "and": [ { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] } } }""", cost: """{ "discard": "this" }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ACostCannotSwitchAnIfIntoAnUnknownDependentOutcome()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "then": { "effect": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } } }""", cost: """{ "discard": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner));
        Assert.Contains("none/partial/full resolution", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void ACostCannotSwitchADependentPredecessorToAnUnknownOutcome()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "then": { "effect": { "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } } } }, "then": { "draw": { "player": "you", "count": 1 } } } }""", cost: """{ "discard": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner));
        Assert.Contains("none/partial/full resolution", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void AnEarlierStepCannotSwitchADependentPredecessorToAnUnknownOutcome()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "seq": [ { "changeForm": { "player": "you", "to": "alter-ego" } }, { "then": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "alter-ego" } }, "then": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }, "else": { "draw": { "player": "you", "count": 1 } } } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner));
        Assert.Contains("none/partial/full resolution", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Fact]
    public void ACostCanActivateAResumableOtherwiseBranch()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "otherwise": { "effect": { "exhaust": "this" }, "otherwise": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));
        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AnEarlierStepCanActivateAResumableOtherwiseBranch()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "seq": [ { "exhaust": "this" }, { "otherwise": { "effect": { "exhaust": "this" }, "otherwise": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } } ] }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));
        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ANestedSequenceResumesPastAnEarlierMutationBoundary()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "seq": [ { "exhaust": "this" }, { "seq": [ { "otherwise": { "effect": { "exhaust": "this" }, "otherwise": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } } ] } ] }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));
        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ADirectSequencePersistsItsNestedContinuationAfterItsFirstMutation()
    {
        var runner = Runner(AuthoredCards.AuntMay, "WhenRevealed", """{ "seq": [ { "draw": { "player": "you", "count": 1 } }, { "and": [ { "draw": { "player": "you", "count": 1 } }, { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } ] } ] }""", eventName: Steps.CardRevealed);
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        runner.WhenRevealed(world, source!, 0);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.ChooseOption, waiting.What);
        Assert.Equal(["seq:1"], waiting.AbilityPath);
    }

    [Rule("rr:and.1")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void AnOrderedCardAttackResumesTheRemainingSimultaneousEffect()
    {
        // The first player chooses the order of the independent effects. The
        // attack has its own agenda procedure, so the later draw must wait for
        // that procedure and then resume from the exact `and` branch.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "and": [ { "attack": { "target": { "query": "villain" }, "effect": { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));
        Assert.Equal(1, villain.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void LeavingTheFinalEachPlayerFrameRestoresTheOriginalResolver()
    {
        // Each frame reads "you" as that frame's player. Text after the frame
        // belongs to the player resolving the ability, not whichever player
        // the first player put last in the chosen order.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "seq": [ { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        int firstHeld = world.Seats[0].Hand.Cards.Count;
        int secondHeld = world.Seats[1].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [world.Seats[0].IdentityCard.ObjectId, world.Seats[1].IdentityCard.ObjectId]));
        Assert.Equal(firstHeld + 2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHeld + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:and.1")]
    [Fact]
    public void UnsupportedThreatPlacementOrderingRaisesBeforeTheActionCost()
    {
        // Threat placement has its own interrupt and response windows. Until
        // that agenda record carries a structural card continuation, accepting
        // an order that places it first would silently skip the later effect.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "and": [ { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }, { "draw": { "player": "you", "count": 1 } } ] }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner));
        Assert.Contains("threat placement continuation", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void NestedEachPlayerFramesRaiseBeforeTheActionCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "eachPlayer": { "effect": { "choose": { "options": [ { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner));
        Assert.Contains("nests one each-player", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void ACardPowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "attack": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LegalPracticePowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        // A labeled thwart ability "is considered to be a thwart made by that
        // player's identity." Its whole nested power must be preflighted.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "legalPractice": { "schemes": { "query": "thwartableSchemes" }, "power": { "thwart": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }
}
