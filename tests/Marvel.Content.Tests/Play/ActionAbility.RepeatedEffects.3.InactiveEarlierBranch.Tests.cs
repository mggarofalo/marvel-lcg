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
public sealed class ActionAbilityRepeatedEffectsInactiveEarlierBranchTests
{
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void InactiveEarlierBranchDoesNotChangePowerForm()
    {
        // The first condition's alter-ego branch cannot execute while the hero
        // branch merely draws. Its unreachable form change must not make the
        // later hero-only condition appear switchable.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "draw": { "player": "you", "count": 1 } },
                  "else": { "changeForm": {
                    "player": "you", "to": "hero"
                  } }
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
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
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ZeroForEachBodyDoesNotChangePowerForm()
    {
        // A zero-count form change never executes. It cannot make the later
        // form condition switch or expose its suspending alter-ego branch.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "forEach": {
                  "count": 0,
                  "effect": { "changeForm": {
                    "player": "you", "to": "alter-ego"
                  } }
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
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
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void NoOpFormChangeDoesNotSwitchALaterPowerBranch()
    {
        // Changing to the form already showing does nothing. The later hero
        // condition therefore cannot switch to its suspending alter-ego branch.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "changeForm": { "player": "you", "to": "hero" } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
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
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void AnotherPlayersFormChangeDoesNotSwitchYourPowerBranch()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "changeForm": {
                  "player": "firstPlayer", "to": "alter-ego"
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.FirstPlayer = 1;
            board.Seats[1].IdentityCard.TurnTo("01010a");
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void StableFirstPlayerNoOpDoesNotSwitchALaterPowerBranch()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "changeForm": {
                  "player": "firstPlayer", "to": "hero"
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.FirstPlayer = 1;
            board.Seats[1].IdentityCard.TurnTo("01010a");
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LethalPowerDamageCanRebindAFormTestedFirstPlayer()
    {
        // Eliminating the hero holding the first-player token moves that
        // selector to the alter-ego player. The newly reachable activation is
        // refused before the lethal damage mutates the board.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": { "cards": "you", "amount": 99 } },
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
        Assert.True(source!.Ready);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DeterministicFormRestorationKeepsTheLaterPowerBranchStable()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "changeForm": { "player": "you", "to": "alter-ego" } },
                { "changeForm": { "player": "you", "to": "hero" } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
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
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }
}
