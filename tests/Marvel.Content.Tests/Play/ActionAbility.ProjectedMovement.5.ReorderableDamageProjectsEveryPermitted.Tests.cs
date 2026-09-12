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
public sealed class ActionAbilityProjectedMovementReorderableDamageProjectsEveryPermittedTests
{
    [Rule("rr:and")]
    [Rule("rr:first-player.3")]
    [Rule("rr:cost.6")]
    [Fact]
    public void ReorderableDamageProjectsEveryPermittedOrderBeforeCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 3);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);
        Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:and")]
    [Rule("rr:first-player.3")]
    [Rule("rr:cost.6")]
    [Fact]
    public void ReorderableDamageInventoryFeedsALaterMoveBeforeCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Tough);
        Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
        Assert.Equal(0, minion.Damage);
    }

    [Rule("rr:and")]
    [Rule("rr:first-player.3")]
    [Fact]
    public void ReorderableDamageInventoryKeepsCardsCorrelated()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var hawkeye = board.CreateCard("01066", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            hawkeye.TakeDamage(1);
            board.CreateCard("01002", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            var minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 2);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Fact]
    public void GuaranteedToughProtectsALaterAreaSensitiveDamageStep()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }

    [Rule("rr:for-each.1")]
    [Rule("rr:and")]
    [Rule("rr:cost.6")]
    [Fact]
    public void RepeatedAndGroupKeepsItsWholeIterationCountBeforeCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 2);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:tough.2")]
    [Fact]
    public void ConditionalUsesTheProjectedToughState()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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

    [Rule("rr:vulnerable.1")]
    [Rule("rr:cost.6")]
    [Fact]
    public void VulnerableStatusDiscardIsProjectedBeforeCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "giveStatus": {
                "card": { "titled": "Scientist Supreme" },
                "status": "stunned"
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Scientist Supreme"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? vulnerable = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            vulnerable = board.CreateCard("50125", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("50125", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner, scenario: "klaw"));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, vulnerable!.Area.Type);
    }
}
