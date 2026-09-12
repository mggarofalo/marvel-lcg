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
public sealed class ActionAbilityProjectedStateEnteredEnemyParticipatesInProjectedTraitQueriesTests
{
    [Rule("rr:enters-play")]
    [Rule("rr:enemy")]
    [Fact]
    public void EnteredEnemyParticipatesInProjectedTraitQueries()
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
                "cards": { "enemiesWithTrait": "CRIMINAL" }, "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hired Gun"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? entrant = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var protectedMinion = board.CreateCard("02007", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, protectedMinion, Statuses.Tough);
            entrant = board.CreateCard("02007", board.AreaOf(DeckType.EncounterDeck));
            board.CreateCard("02007", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDeck, entrant!.Area.Type);
    }

    [Rule("rr:lasting-effects")]
    [Rule("rr:traits.1")]
    [Fact]
    public void LastingTraitGrantChangesLaterProjectedTraitQuery()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "grantUntil": {
                "card": { "titled": "Private Security Specialist" },
                "trait": "CRIMINAL", "until": "EndOfRound"
              } },
              { "dealDamage": {
                "cards": { "enemiesWithTrait": "CRIMINAL" }, "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile",
                "title": "Private Security Specialist"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? specialist = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var criminal = board.CreateCard("02007", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, criminal, Statuses.Tough);
            specialist = board.CreateCard("02008", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("02008", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, specialist!.Area.Type);
    }

    [Rule("rr:ability")]
    [Rule("rr:traits.1")]
    [Fact]
    public void DepartedConstantSourceStopsProjectedTraitGrant()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "discard": { "titled": "Helicarrier" } },
              { "dealDamage": {
                "cards": { "enemiesWithTrait": "CRIMINAL" }, "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? grantSource = null;
        Card? hydra = null;
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            grantSource = board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            hydra = board.CreateCard("08028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            var criminal = board.CreateCard("02007", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, criminal, Statuses.Tough);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
            board.Effects.Register(new ContinuousEffect(EffectSource.ConstantAbility, Rules.State.Traits.Granted + "CRIMINAL", Amount: 1, Card: grantSource.ObjectId, Affects: hydra.ObjectId, Lasts: Duration.WhileInPlay));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
        Assert.Equal(DeckType.SupportsArea, grantSource!.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

    [Rule("rr:lasting-effects")]
    [Fact]
    public void LastingAttackGrantChangesLaterProjectedRanking()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "grantUntil": {
                "card": { "titled": "Hydra Mercenary" },
                "keyword": "attack", "amount": 10, "until": "EndOfRound"
              } },
              { "dealDamage": {
                "cards": { "maxBy": {
                  "of": { "query": "minions" }, "by": "attack"
                } },
                "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? hydra = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            hydra = board.CreateCard("08028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            var criminal = board.CreateCard("02007", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, criminal, Statuses.Tough);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

    [Rule("rr:ability")]
    [Rule("rr:hit-points.2.3")]
    [Fact]
    public void DepartedConstantSourceStopsProjectedHealthGrant()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "discard": { "titled": "Helicarrier" } },
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 3
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? grantSource = null;
        Card? hydra = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            grantSource = board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            hydra = board.CreateCard("08028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
            board.Effects.Register(new ContinuousEffect(EffectSource.ConstantAbility, "health", Amount: 3, Card: grantSource.ObjectId, Affects: hydra.ObjectId, Lasts: Duration.WhileInPlay));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, grantSource!.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:lasting-effects")]
    [Fact]
    public void LastingGuardGrantChangesProjectedAttackableEnemies()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "grantUntil": {
                "card": { "titled": "Hydra Mercenary" },
                "keyword": "guard", "amount": 1, "until": "EndOfRound"
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
        Card? minion = null;
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var villain = board.TheCardIn(DeckType.VillainArea)!;
            villain.TakeDamage(Damage.Health(board, board.Facts, villain) - 1);
            minion = board.CreateCard("08028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, minion, Statuses.Tough);
            board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }
}
