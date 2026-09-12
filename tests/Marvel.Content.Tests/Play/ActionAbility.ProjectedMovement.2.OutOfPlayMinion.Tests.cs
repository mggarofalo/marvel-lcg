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
public sealed class ActionAbilityProjectedMovementOutOfPlayMinionTests
{
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void OutOfPlayMinionDoesNotJoinALabelledRankedSelector()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
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
        World? world = null;
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01102", board.AreaOf(DeckType.EncounterDiscardPile));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteringConstantAbilityRaisesBeforeALabelledPowerMutates()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "putIntoPlay": {
                  "card": { "cardsIn": {
                    "areas": [ "encounterDiscardPile" ], "title": "Titania"
                  } },
                  "where": "engagedWithYou"
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Titania" }, "to": "you", "amount": 1
                } }
              ] }
            } }
            """, includeAuthored: true);
        World? world = null;
        Card? source = null;
        Card? titania = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            titania = board.CreateCard("01162", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.Contains("constant abilities", refused.Message);
        Assert.Equal(DeckType.EncounterDiscardPile, titania!.Area.Type);
        Assert.True(source!.Ready);
        Assert.NotNull(world);
    }

    [Rule("rr:defeat.1")]
    [Rule("rr:guard.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DefeatedGuardStopsProtectingTheVillainInALabelledPower()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """);
        World? world = null;
        Card? source = null;
        Card? guard = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            guard = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, guard.Area.Type);
        Assert.True(source!.Ready);
    }

    [Rule("rr:permanent.1")]
    [Fact]
    public void RankedDiscardCanTargetAPermanentFromTheSourcesSet()
    {
        // Permanent prevents a card from leaving play "except by card
        // abilities in the same set." Both S.H.I.E.L.D. Tech cards carry the
        // same printed set, so the target remains in the ranked candidates
        // while the action is traced.
        var runner = Runner("27182a", "Action", """
            { "chooseCard": {
              "from": { "minBy": {
                "of": { "titled": "Wrist Navigator" }, "by": "cost"
              } },
              "effect": { "discard": "chosen" }
            } }
            """);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, "27182a");
            InPlay(board, "27189a");
        }, hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:permanent.4")]
    [Rule("rr:target.3.4")]
    [Theory]
    [InlineData("discard")]
    [InlineData("removeFromGame")]
    public void InvalidPermanentRemovalComponentDoesNotAbortAValidSibling(string removal)
    {
        // One valid target makes the combined effect legal, but an invalid
        // component does not resolve against that target. Exhaust succeeds;
        // the cross-set Permanent neither leaves play nor turns the component
        // into an exception after mutation.
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$"""
            { "chooseCard": {
              "from": { "titled": "Compact Darts" },
              "effect": { "seq": [
                { "exhaust": "chosen" },
                { "{{removal}}": "chosen" }
              ] }
            } }
            """);
        Card? source = null;
        Card? permanent = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            permanent = board.CreateCard("27182a", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        var suspended = game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Element, suspended.Prompt!.Asking);
        game.Resolve(Decision.Take(permanent!.ObjectId));
        Assert.False(permanent.Ready);
        Assert.Equal(DeckType.UpgradesArea, permanent.Area.Type);
    }

    [Rule("rr:removed-from-the-game.2")]
    [Rule("rr:target.4.1")]
    [Fact]
    public void LaterComponentSkipsACardAlreadyRemovedFromTheGame()
    {
        // The first component removes the bound source. It is no longer a
        // valid target when the second component resolves, so that component
        // does nothing rather than trying to move the terminal card again.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "removeFromGame": "this" },
              { "discard": "this" }
            ] }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        var resolved = game.Resolve(Decision.Take(action.Id));
        Assert.Equal(DeckType.RemovedArea, source!.Area.Type);
        Assert.Single(resolved.Events.OfType<CardsMoved>(), moved => moved.Cards.Any(card => card.Card == source.ObjectId));
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Fact]
    public void RetainedBindingCannotFollowACardIntoTheHand()
    {
        // Returning the source to hand makes it out of play. The following
        // component retains `this`, but it does not expressly refer to the
        // hand and therefore cannot discard the card from there.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "returnToHand": "this" },
              { "discard": "this" }
            ] }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(DeckType.HandsArea, source!.Area.Type);
    }
}
