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

public sealed partial class ActionAbilityTests
{
    [Rule("rr:villain-defeat.2")]
    [Rule("rr:then")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DependentHealUsesTheNewVillainStage()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "then": {
                  "effect": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } },
                  "then": { "heal": {
                    "card": { "titled": "Weapons Runner" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "titled": "Weapons Runner" },
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
            """);
        World? world = null;
        Card? source = null;
        Card? minion = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01121",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, minion!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:villain-defeat.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LabelledRankedSelectorDropsTheNewVillainStage()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "minBy": {
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
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:villain-defeat.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LabelledRankedSelectorCanGainTheNewVillainStage()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
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

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:lasting-effects")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LabelledNumericGrantChangesALaterRankedTargetSet()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "grantUntil": {
                  "card": { "query": "villain" },
                  "keyword": "attack", "amount": 2, "until": "EndOfRound"
                } },
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

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:villain-defeat.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RankedSelectorAfterFinalVillainDefeatDoesNotCrash()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "minBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LabelledGuardEntryImmediatelyProtectsTheVillain()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "putIntoPlay": {
                  "card": { "cardsIn": {
                    "areas": [ "encounterDiscardPile" ],
                    "title": "Hydra Mercenary"
                  } },
                  "where": "engagedWithYou"
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
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void OutOfPlayMinionDoesNotJoinALabelledRankedSelector()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01102", board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteringConstantAbilityRaisesBeforeALabelledPowerMutates()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;
        Card? titania = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                titania = board.CreateCard(
                    "01162", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                guard = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

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
        var runner = Runner(
            "27182a",
            "Action",
            """
            { "chooseCard": {
              "from": { "minBy": {
                "of": { "titled": "Wrist Navigator" }, "by": "cost"
              } },
              "effect": { "discard": "chosen" }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, "27182a");
                InPlay(board, "27189a");
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:permanent.4")]
    [Rule("rr:target.3.4")]
    [Theory]
    [InlineData("discard")]
    [InlineData("removeFromGame")]
    public void InvalidPermanentRemovalComponentDoesNotAbortAValidSibling(
        string removal)
    {
        // One valid target makes the combined effect legal, but an invalid
        // component does not resolve against that target. Exhaust succeeds;
        // the cross-set Permanent neither leaves play nor turns the component
        // into an exception after mutation.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            $$"""
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
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                permanent = board.CreateCard(
                    "27182a", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner);

        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "removeFromGame": "this" },
              { "discard": "this" }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        var resolved = game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.RemovedArea, source!.Area.Type);
        Assert.Single(resolved.Events.OfType<CardsMoved>(), moved =>
            moved.Cards.Any(card => card.Card == source.ObjectId));
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Fact]
    public void RetainedBindingCannotFollowACardIntoTheHand()
    {
        // Returning the source to hand makes it out of play. The following
        // component retains `this`, but it does not expressly refer to the
        // hand and therefore cannot discard the card from there.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "returnToHand": "this" },
              { "discard": "this" }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.HandsArea, source!.Area.Type);
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Fact]
    public void ChosenBindingRemembersTheAreaWhereItWasSelected()
    {
        // `chosen` is an express reference to the selected in-play card, not
        // permission to follow it into a later out-of-play area. Its selection
        // origin therefore survives the first component's hand move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "chooseCard": {
              "from": { "titled": "Helicarrier" },
              "effect": { "seq": [
                { "returnToHand": "chosen" },
                { "discard": "chosen" }
              ] }
            } }
            """);
        Card? source = null;
        Card? target = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                target = InPlay(board, "01092");
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));

        game.Resolve(Decision.Take(target!.ObjectId));

        Assert.Equal(DeckType.HandsArea, target.Area.Type);
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Rule("rr:removed-from-the-game")]
    [Fact]
    public void AreaNamedSelectorCanRemoveAnOutOfPlayCard()
    {
        // cardsIn expressly names the encounter discard pile, so the ability
        // may affect its matching out-of-play card. This is distinct from a
        // stale binding that merely follows a target after an earlier move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "removeFromGame": { "cardsIn": {
              "area": "encounterDiscardPile", "title": "Hydra Mercenary"
            } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? target = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                target = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.RemovedArea, target!.Area.Type);
        Assert.Contains(target, world.AreaOf(DeckType.RemovedArea).Cards);
        Assert.False(source!.Ready);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:search.1")]
    [Fact]
    public void AmbiguousOutOfPlayRemovalRaisesBeforeActionCost()
    {
        // A singular remove node cannot choose between two matching search
        // results. The ambiguity is found while the action is offered, before
        // its exhaust cost can change the source.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "removeFromGame": { "cardsIn": {
              "area": "encounterDiscardPile", "title": "Hydra Mercenary"
            } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
                board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:search.1")]
    [Fact]
    public void EarlierMoveCannotCreateRemovalAmbiguityAfterActionCost()
    {
        // The first component would add a second Hydra Mercenary to the named
        // discard pile. The singular second component cannot choose between
        // them, so the engine refuses the action before its exhaust cost or
        // the first discard changes the board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "discard": { "titled": "Hydra Mercenary" } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? inPlay = null;
        Card? discarded = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                inPlay = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                discarded = board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, inPlay!.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, discarded!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:search.1")]
    [Fact]
    public void EarlierMoveCannotCreateDiscardAmbiguityAfterActionCost()
    {
        // Every singular cardsIn consumer has the same atomicity boundary.
        // The second discard would need a player choice after the first adds a
        // matching card, so the action is refused before either component or
        // its exhaust cost runs.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "discard": { "titled": "Hydra Mercenary" } },
              { "discard": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? inPlay = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                inPlay = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, inPlay!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:search.1")]
    [Fact]
    public void AmbiguousSearchRaisesBeforeActionCost()
    {
        // Searching two named areas finds two copies and therefore requires
        // the player's rr:search.1 choice. Until that prompt exists, the
        // unsupported branch raises while the source can still remain ready.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "search": {
              "in": [ { "encounterDeck": 1 }, { "encounterDiscardPile": 1 } ],
              "for": "08028"
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("08028", board.AreaOf(DeckType.EncounterDeck));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
    }

    [Rule("rr:choose-option.2")]
    [Rule("rr:search.1")]
    [Fact]
    public void ChoiceFiltersAnOptionThatWouldMakeTheSuffixAmbiguous()
    {
        // The discard option is locally legal but would add a second matching
        // card before the singular suffix. It is filtered while the harmless
        // draw remains available, before either option changes the board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "choose": { "options": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "draw": { "player": "you", "count": 1 } }
              ] } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? inPlay = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                inPlay = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Id == 0);
        Assert.Contains(game.Pending.Affordances, option => option.Id == 1);
        Assert.Equal(DeckType.EngagedEnemiesArea, inPlay!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:choose-option.2")]
    [Fact]
    public void ChoiceWithNoContinuationSafeOptionIsNotOfferedBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "choose": { "options": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "discard": { "titled": "Hydra Mercenary" } }
              ] } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:choose-option.2")]
    [Fact]
    public void NestedChoiceWithNoContinuationSafeOptionIsNotOfferedBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "choose": { "options": [
                  { "discard": { "titled": "Hydra Mercenary" } },
                  { "discard": { "titled": "Hydra Mercenary" } }
                ] } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:choose-option.2")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void PluralCardsInChoiceDoesNotRequireSingularAreaStability()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "choose": { "options": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "discard": { "titled": "Hydra Mercenary" } }
              ] } },
              { "chooseCard": {
                "from": { "cardsIn": {
                  "area": "encounterDiscardPile", "title": "Hydra Mercenary"
                } },
                "effect": { "removeFromGame": "chosen" }
              } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);

        Assert.Contains(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:each-player")]
    [Fact]
    public void EachPlayerAreaMutationChecksEverySeatBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "eachPlayer": { "effect": {
                "discard": { "query": "minionsEngagedWithYou" }
              } } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? secondPlayersMinion = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                secondPlayersMinion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(
            DeckType.EngagedEnemiesArea, secondPlayersMinion!.Area.Type);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:search.1")]
    [Fact]
    public void ToughMinionDoesNotMakeEncounterDiscardUnstable()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        Card? discarded = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                discarded = board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(DeckType.RemovedArea, discarded!.Area.Type);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:for-each.1")]
    [Fact]
    public void CombinedRepeatedDamageIsOneToughPreventedInstance()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "forEach": { "count": 2, "effect": {
                "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                }
              } } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        Card? discarded = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                discarded = board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(DeckType.RemovedArea, discarded!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:move.1")]
    [Fact]
    public void DamageCreatedBeforeAMoveIsProjectedBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hawkeye" }, "amount": 1
              } },
              { "moveDamage": {
                "from": { "titled": "Hawkeye" },
                "to": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? ally = null;
        Card? minion = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                ally = board.CreateCard(
                    "01066", board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(0, ally!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:move.1")]
    [Fact]
    public void RepeatedMoveCannotReuseHealedDamage()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "moveDamage": {
                "from": "you", "to": { "titled": "Hydra Mercenary" },
                "amount": 1
              } },
              { "moveDamage": {
                "from": "you", "to": { "titled": "Hydra Mercenary" },
                "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        Card? discarded = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(1);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 2);
                discarded = board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(
            Damage.Health(world, world.Facts, minion!) - 1, minion!.Damage);
        Assert.Equal(DeckType.RemovedArea, discarded!.Area.Type);
    }

    [Rule("rr:and")]
    [Rule("rr:first-player.3")]
    [Rule("rr:cost.6")]
    [Fact]
    public void ReorderableDamageProjectsEveryPermittedOrderBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "and": [
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                } }
              ] },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 3);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);

        Assert.Throws<RulesNotImplementedException>(() =>
            runner.Actions(world, 0));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:and")]
    [Rule("rr:first-player.3")]
    [Rule("rr:cost.6")]
    [Fact]
    public void ReorderableDamageInventoryFeedsALaterMoveBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "and": [
                { "dealDamage": { "cards": "you", "amount": 3 } },
                { "dealDamage": { "cards": "you", "amount": 1 } }
              ] },
              { "moveDamage": {
                "from": "you", "to": { "titled": "Hydra Mercenary" },
                "amount": 3
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        Statuses.Give(
            world, world.Seats[0].IdentityCard, Statuses.Tough);

        Assert.Throws<RulesNotImplementedException>(() =>
            runner.Actions(world, 0));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
        Assert.Equal(0, minion.Damage);
    }

    [Rule("rr:and")]
    [Rule("rr:first-player.3")]
    [Fact]
    public void ReorderableDamageInventoryKeepsCardsCorrelated()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "and": [
                { "moveDamage": {
                  "from": { "titled": "Hawkeye" },
                  "to": { "titled": "Black Cat" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Black Cat" },
                  "to": { "titled": "Hawkeye" }, "amount": 1
                } }
              ] },
              { "moveDamage": {
                "from": { "titled": "Hawkeye" },
                "to": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "moveDamage": {
                "from": { "titled": "Black Cat" },
                "to": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var hawkeye = board.CreateCard(
                    "01066", board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                hawkeye.TakeDamage(1);
                board.CreateCard(
                    "01002", board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                var minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 2);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Fact]
    public void GuaranteedToughProtectsALaterAreaSensitiveDamageStep()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "giveStatus": {
                "card": { "titled": "Hydra Mercenary" }, "status": "tough"
              } },
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:for-each.1")]
    [Rule("rr:and")]
    [Rule("rr:cost.6")]
    [Fact]
    public void RepeatedAndGroupKeepsItsWholeIterationCountBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "forEach": { "count": 2, "effect": { "and": [
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                } },
                { "draw": { "player": "you", "count": 1 } }
              ] } } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 2);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:tough.2")]
    [Fact]
    public void ConditionalUsesTheProjectedToughState()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "if": {
                "test": { "hasStatus": {
                  "card": { "titled": "Hydra Mercenary" }, "status": "tough"
                } },
                "then": { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                } },
                "else": { "draw": { "player": "you", "count": 1 } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:vulnerable.1")]
    [Rule("rr:cost.6")]
    [Fact]
    public void VulnerableStatusDiscardIsProjectedBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "giveStatus": {
                "card": { "titled": "Scientist Supreme" },
                "status": "stunned"
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Scientist Supreme"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? vulnerable = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                vulnerable = board.CreateCard(
                    "50125", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "50125", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw"));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, vulnerable!.Area.Type);
    }

    [Rule("rr:otherwise.1")]
    [Fact]
    public void DependentContinuationUsesTheProjectedOutcome()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "otherwise": {
                "effect": { "heal": { "card": "you", "amount": 1 } },
                "otherwise": { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(1);
                var minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Fact]
    public void ProjectedBooleanTestsKeepDecisiveShortCircuits()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "if": {
                "test": { "and": [
                  { "hasStatus": {
                    "card": { "titled": "Hydra Mercenary" }, "status": "tough"
                  } },
                  { "inForm": { "player": "you", "form": "hero" } }
                ] },
                "then": { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                } },
                "else": { "draw": { "player": "you", "count": 1 } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Rule("rr:cost.6")]
    [Fact]
    public void LabelledAttackBodyIsProjectedBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "attack": {
                "target": { "titled": "Hydra Mercenary" },
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:then.1")]
    [Rule("rr:cost.6")]
    [Fact]
    public void WrappedDependentOutcomeUsesProjectedStateBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": { "cards": "you", "amount": 1 } },
              { "then": {
                "effect": { "seq": [
                  { "heal": { "card": "you", "amount": 1 } }
                ] },
                "then": { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:vulnerable.1")]
    [Rule("rr:attach-to.1")]
    [Rule("rr:cost.6")]
    [Fact]
    public void VulnerableDiscardProjectsHostedCardsBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "giveStatus": {
                "card": { "titled": "Scientist Supreme" },
                "status": "stunned"
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? vulnerable = null;
        Card? attachment = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                vulnerable = board.CreateCard(
                    "50125", board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                attachment = board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        vulnerable.ObjectId));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw"));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.AlliesArea, vulnerable!.Area.Type);
        Assert.Equal(vulnerable.ObjectId, attachment!.Area.Host);
    }

    [Rule("rr:defeat.1")]
    [Rule("rr:attach-to.1")]
    [Rule("rr:cost.6")]
    [Fact]
    public void LethalDamageProjectsHostedCardsBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hawkeye" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? ally = null;
        Card? attachment = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                ally = board.CreateCard(
                    "01066", board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                ally.TakeDamage(
                    Damage.Health(board, board.Facts, ally) - 1);
                attachment = board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.AlliesArea, ally!.Area.Type);
        Assert.Equal(ally.ObjectId, attachment!.Area.Host);
    }

    [Rule("rr:otherwise.1")]
    [Fact]
    public void ProjectedIfWithoutAnActiveBranchResolvesNone()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "otherwise": {
                "effect": { "if": {
                  "test": { "hasStatus": {
                    "card": { "titled": "Hydra Mercenary" }, "status": "tough"
                  } },
                  "then": { "heal": { "card": "you", "amount": 1 } }
                } },
                "otherwise": { "draw": { "player": "you", "count": 1 } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:victory-x.1")]
    [Fact]
    public void VictoryMinionDoesNotProjectToEncounterDiscard()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Badoon Headhunter" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Badoon Headhunter"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var minion = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "16183", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:victory-x.1.2")]
    [Fact]
    public void NestedVictoryAttachmentProjectsItsOrdinaryDiscard()
    {
        // Only a Victory attachment directly on the defeated character uses
        // the Forced Interrupt that moves it to the victory display. Its own
        // hosted card leaves through ordinary attachment cleanup, even when
        // that nested card also has Victory X.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        Card? direct = null;
        Card? nested = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                direct = board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId));
                nested = board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), direct.ObjectId));
                foreach (var attachment in new[] { direct, nested })
                {
                    board.Effects.Register(new ContinuousEffect(
                        EffectSource.ConstantAbility,
                        "victory",
                        Amount: 1,
                        Card: attachment.ObjectId,
                        Affects: attachment.ObjectId,
                        Lasts: Duration.WhileInPlay));
                }
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
        Assert.Equal(minion.ObjectId, direct!.Area.Host);
        Assert.Equal(direct.ObjectId, nested!.Area.Host);
    }

    [Rule("rr:victory-x")]
    [Fact]
    public void VictorySideSchemeDoesNotProjectToEncounterDiscard()
    {
        // A side scheme with Victory X enters the victory display when it is
        // defeated instead of entering the encounter discard pile.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "removeThreat": {
                "scheme": { "titled": "Kree Supremacy" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Kree Supremacy"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "16182a", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
                board.CreateCard(
                    "16182a", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:victory-x.1")]
    [Fact]
    public void LaterSelectorCannotSeeProjectedDepartedVictoryMinion()
    {
        // After the Victory minion leaves play, a later title reference no
        // longer denotes it. Its no-op discard therefore cannot destabilize an
        // unrelated singular encounter-discard query.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Badoon Headhunter" }, "amount": 1
              } },
              { "discard": { "titled": "Badoon Headhunter" } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var minion = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:otherwise.1")]
    [Rule("rr:victory-x.1")]
    [Fact]
    public void DepartedTargetMakesProjectedOtherwiseFallbackReachable()
    {
        // The defeated Victory minion is no longer an in-play title reference,
        // so healing it resolves nothing and the otherwise discard applies.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Badoon Headhunter" }, "amount": 1
              } },
              { "otherwise": {
                "effect": { "heal": {
                  "card": { "titled": "Badoon Headhunter" }, "amount": 1
                } },
                "otherwise": { "discard": {
                  "titled": "Hydra Mercenary"
                } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? victory = null;
        Card? hydra = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                victory = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                victory.TakeDamage(
                    Damage.Health(board, board.Facts, victory) - 1);
                hydra = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, victory!.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

}
