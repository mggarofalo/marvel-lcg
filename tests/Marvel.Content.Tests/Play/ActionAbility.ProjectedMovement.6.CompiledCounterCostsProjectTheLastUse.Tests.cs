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
public sealed class ActionAbilityProjectedMovementCompiledCounterCostsProjectTheLastUseTests
{
    [Rule("rr:uses-x-type.1")]
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void CompiledCounterCostsProjectTheLastUseBeforeAnAreaQuery(int counters)
    {
        // rr:uses-x-type.1: "If there are no all-purpose counters on this card,
        // discard this card." Two components of one payment share that pool.
        var runner = new Marvel.Cards.Run.AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [{ "card": "01080",
              "startingCounters": { "type": "medical", "count": 3, "uses": true },
              "abilities": [{
                "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                "cost": { "seq": [
                  { "removeCounters": { "card": "this", "counter": "medical", "count": 1 } },
                  { "removeCounters": { "card": "this", "counter": "medical", "count": 1 } }
                ] },
                "effect": { "removeFromGame": { "cardsIn": {
                  "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
                } } }
              }]
            }] }
            """));
        Card? source = null;
        Card? hosted = null;
        Card? discarded = null;
        (Game Game, World World) Start() => Playing(board =>
        {
            source = InPlay(board, "01080");
            source.PlaceTokens("c_medical", counters);
            hosted = board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), source.ObjectId));
            discarded = board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner);
        if (counters == 2)
        {
            Assert.Throws<RulesNotImplementedException>(() => Start());
            Assert.Equal(2, source!.Tokens["c_medical"]);
            Assert.Equal(DeckType.EncounterDiscardPile, discarded!.Area.Type);
        }
        else
        {
            var(game, _) = Start();
            var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
            game.Resolve(Decision.Take(action.Id));
            Assert.Equal(1, source!.Tokens["c_medical"]);
            Assert.Equal(DeckType.RemovedArea, discarded!.Area.Type);
        }

        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
        Assert.Equal(source.ObjectId, hosted!.Area.Host);
    }

    [Rule("rr:otherwise.1")]
    [Fact]
    public void DependentContinuationUsesTheProjectedOutcome()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(1);
            var minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Fact]
    public void ProjectedBooleanTestsKeepDecisiveShortCircuits()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Rule("rr:cost.6")]
    [Fact]
    public void LabelledAttackBodyIsProjectedBeforeCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:then.1")]
    [Rule("rr:cost.6")]
    [Fact]
    public void WrappedDependentOutcomeUsesProjectedStateBeforeCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:vulnerable.1")]
    [Rule("rr:attach-to.1")]
    [Rule("rr:cost.6")]
    [Fact]
    public void VulnerableDiscardProjectsHostedCardsBeforeCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "giveStatus": {
                "card": { "titled": "Scientist Supreme" },
                "status": "stunned"
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? vulnerable = null;
        Card? attachment = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            vulnerable = board.CreateCard("50125", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            attachment = board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), vulnerable.ObjectId));
            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner, scenario: "klaw"));
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hawkeye" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? ally = null;
        Card? attachment = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            ally = board.CreateCard("01066", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            ally.TakeDamage(Damage.Health(board, board.Facts, ally) - 1);
            attachment = board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId));
            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.AlliesArea, ally!.Area.Type);
        Assert.Equal(ally.ObjectId, attachment!.Area.Host);
    }
}
