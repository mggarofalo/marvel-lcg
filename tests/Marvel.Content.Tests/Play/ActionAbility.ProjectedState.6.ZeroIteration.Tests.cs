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
public sealed class ActionAbilityProjectedStateZeroIterationTests
{
    [Rule("rr:for-each.1")]
    [Rule("rr:search.1")]
    [Fact]
    public void ZeroIterationDoesNotMakeALaterAreaQueryUnstable()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "forEach": { "count": 0, "effect": {
                "discard": { "titled": "Hydra Mercenary" }
              } } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? target = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            target = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(DeckType.RemovedArea, target!.Area.Type);
    }

    [Rule("rr:damage.3")]
    [Rule("rr:search.1")]
    [Fact]
    public void NonlethalVillainDamageDoesNotMakeEncounterDiscardUnstable()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? target = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            target = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(1, villain.Damage);
        Assert.Equal(DeckType.RemovedArea, target!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:defeat.1")]
    [Rule("rr:search.1")]
    [Fact]
    public void DefeatedSchemeCannotCreateAreaAmbiguityAfterActionCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "removeThreat": {
                "scheme": { "titled": "Breakin' & Takin'" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Breakin' & Takin'"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            scheme = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 1);
            board.CreateCard("01107", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SideSchemesArea, scheme!.Area.Type);
        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cost.6")]
    [Rule("rr:defeat.1")]
    [Rule("rr:for-each.1")]
    [Fact]
    public void RepeatedThreatRemovalProjectsItsCombinedAmountBeforeCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "forEach": { "count": 2, "effect": {
                "removeThreat": {
                  "scheme": { "titled": "Breakin' & Takin'" }, "amount": 1
                }
              } } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Breakin' & Takin'"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            scheme = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 2);
            board.CreateCard("01107", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cost.6")]
    [Rule("rr:defeat.1")]
    [Fact]
    public void SequentialThreatRemovalProjectsItsCombinedAmountBeforeCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "removeThreat": {
                "scheme": { "titled": "Breakin' & Takin'" }, "amount": 1
              } },
              { "removeThreat": {
                "scheme": { "titled": "Breakin' & Takin'" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Breakin' & Takin'"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            scheme = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 2);
            board.CreateCard("01107", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cost.6")]
    [Rule("rr:defeat.1")]
    [Rule("rr:move.1")]
    [Fact]
    public void LethalMovedDamageRaisesBeforeHealingOrActionCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "moveDamage": {
                "from": "you", "to": { "titled": "Hydra Mercenary" },
                "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        Card? identity = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            identity = board.Seats[0].IdentityCard;
            identity.TakeDamage(1);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(1, identity!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:defeat.1")]
    [Rule("rr:event.5")]
    [Fact]
    public void EventThreatModifierIsIncludedInAreaMutationPreflight()
    {
        var runner = Runner("01005", "Action", """
            { "seq": [
              { "removeThreat": {
                "scheme": { "titled": "Breakin' & Takin'" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Breakin' & Takin'"
              } } }
            ] }
            """);
        Card? played = null;
        Card? scheme = null;
        var(_, world) = Playing(board =>
        {
            played = board.CreateCard("01005", board.Seats[0].Hand);
            scheme = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 2);
            board.CreateCard("01107", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "eventThreatRemoval", Amount: 1, Card: played!.ObjectId, Affects: played.ObjectId));
        Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Equal(DeckType.HandsArea, played.Area.Type);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Rule("rr:permanent.4")]
    [Rule("rr:removed-from-the-game")]
    [Fact]
    public void PermanentDoesNotProtectAnExplicitOutOfPlayTarget()
    {
        // Permanent prevents an effect from making a card leave play. This
        // target is already in an expressly named discard pile, so a card from
        // another set may remove it from the game.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "removeFromGame": { "cardsIn": {
              "area": "encounterDiscardPile", "title": "Compact Darts"
            } } }
            """);
        Card? source = null;
        Card? target = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            target = board.CreateCard("27182a", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(DeckType.RemovedArea, target!.Area.Type);
    }
}
