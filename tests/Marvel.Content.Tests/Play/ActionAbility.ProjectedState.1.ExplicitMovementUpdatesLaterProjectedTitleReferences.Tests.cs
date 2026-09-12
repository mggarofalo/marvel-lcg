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
public sealed class ActionAbilityProjectedStateExplicitMovementUpdatesLaterProjectedTitleReferencesTests
{
    [Rule("rr:in-play-and-out-of-play.4")]
    [Fact]
    public void ExplicitMovementUpdatesLaterProjectedTitleReferences()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "removeFromGame": { "titled": "Hydra Mercenary" } },
              { "discard": { "titled": "Hydra Mercenary" } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """);
        Card? source = null;
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }

    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:villain-defeat.4.2")]
    [Fact]
    public void ConsecutiveVillainStagesProjectCarriedAttachmentDeparture()
    {
        // Rhino I carries the attachment to Rhino II. Defeating the final
        // stage then discards it ordinarily, even when it has Victory X.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? villain = null;
        Card? attachment = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            villain = board.TheCardIn(DeckType.VillainArea)!;
            attachment = board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
            board.Effects.Register(new ContinuousEffect(EffectSource.ConstantAbility, "victory", Amount: 1, Card: attachment.ObjectId, Affects: attachment.ObjectId, Lasts: Duration.WhileInPlay));
            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.True(DeckTypes.IsInPlay(attachment!.Area.Type));
        Assert.Equal(villain.ObjectId, attachment.Area.Host);
    }

    [Rule("rr:enters-play")]
    [Fact]
    public void EnteredMinionParticipatesInLaterProjectedQueries()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDeck" ], "title": "Hired Gun"
                } },
                "where": "engagedWithYou"
              } },
              { "dealDamage": {
                "cards": { "query": "minionsEngagedWithYou" }, "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hired Gun"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? hydra = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var protectedMinion = board.CreateCard("16183", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, protectedMinion, Statuses.Tough);
            hydra = board.CreateCard("02007", board.AreaOf(DeckType.EncounterDeck));
            board.CreateCard("02007", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDeck, hydra!.Area.Type);
    }

    [Rule("rr:enters-play")]
    [Fact]
    public void EnteredMinionParticipatesInLaterProjectedRankedQueries()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDeck" ], "title": "Hired Gun"
                } },
                "where": "engagedWithYou"
              } },
              { "dealDamage": {
                "cards": { "minBy": {
                  "of": { "query": "minions" }, "by": "printedHealth"
                } },
                "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hired Gun"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? hydra = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var protectedMinion = board.CreateCard("16183", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, protectedMinion, Statuses.Tough);
            hydra = board.CreateCard("02007", board.AreaOf(DeckType.EncounterDeck));
            board.CreateCard("02007", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDeck, hydra!.Area.Type);
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:toughness.1")]
    [Fact]
    public void EnteredMinionReceivesLaterProjectedStatus()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDeck" ], "title": "Hired Gun"
                } },
                "where": "engagedWithYou"
              } },
              { "giveStatus": {
                "card": { "query": "minionsEngagedWithYou" },
                "status": "tough"
              } },
              { "dealDamage": {
                "cards": { "titled": "Hired Gun" }, "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hired Gun"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? hydra = null;
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            hydra = board.CreateCard("02007", board.AreaOf(DeckType.EncounterDeck));
            board.CreateCard("02007", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
        Assert.Equal(DeckType.EncounterDeck, hydra!.Area.Type);
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Fact]
    public void ReenteringSourceInvalidatesLaterProjectedThisBinding()
    {
        var runner = Runner("02007", "Action", """
            { "seq": [
              { "removeFromGame": "this" },
              { "putIntoPlay": { "card": "this", "where": "engagedWithYou" } },
              { "discard": "this" },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hired Gun"
              } } }
            ] }
            """);
        Card? source = null;
        var(_, world) = Playing(board =>
        {
            source = board.CreateCard("02007", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("02007", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
        Assert.Equal(DeckType.EngagedEnemiesArea, source!.Area.Type);
    }

    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:status-cards.1")]
    [Fact]
    public void VillainAdvancementCarriesProjectedStatusCards()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "if": {
                "test": { "hasStatus": {
                  "card": { "query": "villain" }, "status": "stunned"
                } },
                "then": { "discard": { "titled": "Hired Gun" } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hired Gun"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            Statuses.Give(board, board.TheCardIn(DeckType.VillainArea)!, Statuses.Stunned);
            minion = board.CreateCard("02007", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("02007", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }
}
