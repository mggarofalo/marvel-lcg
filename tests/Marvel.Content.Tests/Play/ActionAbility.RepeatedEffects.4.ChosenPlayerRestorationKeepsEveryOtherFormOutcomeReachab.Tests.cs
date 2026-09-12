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
public sealed class ActionAbilityRepeatedEffectsChosenPlayerRestorationKeepsEveryOtherFormOutcomeReachabTests
{
    [Rule("rr:choose-game-element.3")]
    [Rule("rr:form-change-form.2")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void ChosenPlayerRestorationKeepsEveryOtherFormOutcomeReachable()
    {
        // A choice must be made among “eligible game elements.” Restoring the
        // chosen identity to hero form changes only that identity; another
        // identity that a prior option changed can remain in alter-ego form.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "choose": { "options": [
                { "changeForm": {
                  "player": "firstPlayer", "to": "alter-ego"
                } },
                { "changeForm": { "player": "you", "to": "alter-ego" } }
              ] } },
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "changeForm": {
                  "player": "chosenPlayer", "to": "hero"
                } }
              } },
              { "if": {
                "test": { "inForm": {
                  "player": "firstPlayer", "form": "hero"
                } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1, "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.FirstPlayer = 1;
            board.Seats[1].IdentityCard.TurnTo("01010a");
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.DoesNotContain(runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:form-change-form.2")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void EveryLegalEachPlayerOrderContributesFormReachability()
    {
        // “The first player decides the order” for an each-player effect. If
        // player one resolves first, both identities can change form; the
        // later target check must include that legal ordering.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "eachPlayer": { "effect": { "if": {
                "test": { "inForm": {
                  "player": "firstPlayer", "form": "hero"
                } },
                "then": { "changeForm": {
                  "player": "you", "to": "alter-ego"
                } },
                "else": { "seq": [] }
              } } } },
              { "if": {
                "test": { "inForm": {
                  "player": "you", "form": "hero"
                } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1, "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var(_, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01001a");
            board.Seats[1].IdentityCard.TurnTo("01010a");
            source = board.CreateCard(AuthoredCards.AuntMay, board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.DoesNotContain(runner.Actions(world, 1), ability => ability.Card == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void CumulativePowerDamageCanRebindTheFirstPlayer()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "dealDamage": { "cards": "you", "amount": 1 } },
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
            board.Seats[0].IdentityCard.TakeDamage(8);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LethalDamageToAnotherPlayerDoesNotRebindFirstPlayer()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Carol Danvers" }, "amount": 99
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
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DamagePlacedEarlierCanBeMovedToEliminateFirstPlayer()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
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
            """);
        World? world = null;
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:for-each.2")]
    [Rule("rr:tough.2")]
    [Fact]
    public void CombinedForEachDamageUsesOneToughInPowerPreflight()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "forEach": {
                  "count": 2,
                  "effect": { "dealDamage": { "cards": "you", "amount": 1 } }
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
            board.Seats[0].IdentityCard.TakeDamage(9);
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void NonToughStatusDoesNotEnterPowerDamageSimulation()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "giveStatus": { "card": "you", "status": "stunned" } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }
}
