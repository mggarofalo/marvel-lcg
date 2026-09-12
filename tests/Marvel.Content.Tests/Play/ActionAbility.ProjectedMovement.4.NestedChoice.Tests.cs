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
public sealed class ActionAbilityProjectedMovementNestedChoiceTests
{
    [Rule("rr:cost.6")]
    [Rule("rr:choose-option.2")]
    [Fact]
    public void NestedChoiceWithNoContinuationSafeOptionIsNotOfferedBeforeCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:choose-option.2")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void PluralCardsInChoiceDoesNotRequireSingularAreaStability()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:each-player")]
    [Fact]
    public void EachPlayerAreaMutationChecksEverySeatBeforeCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "eachPlayer": { "effect": {
                "discard": { "query": "minionsEngagedWithYou" }
              } } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? secondPlayersMinion = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            secondPlayersMinion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, secondPlayersMinion!.Area.Type);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:search.1")]
    [Fact]
    public void ToughMinionDoesNotMakeEncounterDiscardUnstable()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        Card? discarded = null;
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            discarded = board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            discarded = board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? ally = null;
        Card? minion = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            ally = board.CreateCard("01066", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(0, ally!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:move.1")]
    [Fact]
    public void RepeatedMoveCannotReuseHealedDamage()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(1);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 2);
            discarded = board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        runner.Act(world, action, [], []);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(Damage.Health(world, world.Facts, minion!) - 1, minion!.Damage);
        Assert.Equal(DeckType.RemovedArea, discarded!.Area.Type);
    }
}
