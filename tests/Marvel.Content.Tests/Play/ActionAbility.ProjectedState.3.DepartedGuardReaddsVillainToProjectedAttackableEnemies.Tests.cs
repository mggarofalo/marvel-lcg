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
public sealed class ActionAbilityProjectedStateDepartedGuardReaddsVillainToProjectedAttackableEnemiesTests
{
    [Rule("rr:guard.1")]
    [Rule("rr:villain-defeat.4.2")]
    [Fact]
    public void DepartedGuardReaddsVillainToProjectedAttackableEnemies()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 3
              } },
              { "dealDamage": {
                "cards": { "query": "attackableEnemies" }, "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? guard = null;
        Card? attachment = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var villain = board.TheCardIn(DeckType.VillainArea)!;
            villain.TakeDamage(Damage.Health(board, board.Facts, villain) - 1);
            board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            guard = board.CreateCard("08028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            attachment = board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, guard!.Area.Type);
        Assert.True(DeckTypes.IsInPlay(attachment!.Area.Type));
    }

    [Rule("rr:ability")]
    [Rule("rr:hit-points.2.3")]
    [Fact]
    public void UnconditionalHealthConstantSurvivesUnrelatedProjectedChange()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "removeThreat": {
                "scheme": { "query": "mainScheme" }, "amount": 1
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
            scheme.PlaceTokens("k_threat", 1);
            board.CreateCard("01127", board.AreaOf(DeckType.SideSchemesArea));
            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }

    [Rule("rr:ability.9")]
    [Fact]
    public void ProjectedStatusRefusesNewlyActiveConditionalAttackConstant()
    {
        var runner = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": { "seq": [
                      { "giveStatus": {
                        "card": { "titled": "Hydra Mercenary" },
                        "status": "stunned"
                      } },
                      { "dealDamage": {
                        "cards": { "maxBy": {
                          "of": { "query": "minions" }, "by": "attack"
                        } },
                        "amount": 100
                      } },
                      { "removeFromGame": { "cardsIn": {
                        "area": "encounterDiscardPile",
                        "title": "Hydra Mercenary"
                      } } }
                    ] }
                  } ] },
                  { "card": "08028", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "hasStatus": {
                        "card": "this", "status": "stunned"
                      } },
                      "then": { "grant": {
                        "card": "this", "keyword": "attack", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
        Card? source = null;
        Card? hydra = null;
        World? world = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            hydra = board.CreateCard("08028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            var criminal = board.CreateCard("02007", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, criminal, Statuses.Tough);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.Contains("conditional constant", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
        Assert.Equal(0, Statuses.Count(world!, hydra, Statuses.Stunned));
    }

    [Rule("rr:in-play-and-out-of-play.5")]
    [Rule("rr:in-play-and-out-of-play.13")]
    [Fact]
    public void ProjectedStatusIgnoresAFacedownDronesPrintedConditionalConstant()
    {
        // A facedown card's printed text cannot affect the game. Projecting a
        // status onto the Drone therefore cannot activate the conditional
        // attack modifier printed on the card underneath or make a later
        // attack-ranked minion selection unsafe to admit.
        var runner = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": { "seq": [
                      { "giveStatus": {
                        "card": { "query": "dronesEngagedWithYou" },
                        "status": "stunned"
                      } },
                      { "dealDamage": {
                        "cards": { "maxBy": {
                          "of": { "query": "minions" }, "by": "attack"
                        } },
                        "amount": 100
                      } },
                      { "removeFromGame": { "cardsIn": {
                        "area": "encounterDiscardPile",
                        "title": "Hydra Mercenary"
                      } } }
                    ] }
                  } ] },
                  { "card": "08028", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "hasStatus": {
                        "card": "this", "status": "stunned"
                      } },
                      "then": { "grant": {
                        "card": "this", "keyword": "attack", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
        Card? source = null;
        Card? drone = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            drone = board.CreateCard("08028", board.Seats[0].Deck);
            FacedownDrones.EngageTop(board, 0, "test", "Create_Drone", []);
            var criminal = board.CreateCard("02007", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, criminal, Statuses.Tough);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner);
        Assert.True(FacedownDrones.Is(drone!));
        Assert.Contains(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:ability.9")]
    [Fact]
    public void UnrelatedConditionalHealthConstantDoesNotBlockVillainProjection()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var pool = board.CreateCard("45071", board.AreaOf(DeckType.SideSchemesArea));
            pool.PlaceTokens("k_threat", 9);
            board.CreateCard("45069", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }
}
