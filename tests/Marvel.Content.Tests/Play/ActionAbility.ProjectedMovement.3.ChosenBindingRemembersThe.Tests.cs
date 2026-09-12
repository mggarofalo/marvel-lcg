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
public sealed class ActionAbilityProjectedMovementChosenBindingRemembersTheTests
{
    [Rule("rr:in-play-and-out-of-play.4")]
    [Fact]
    public void ChosenBindingRemembersTheAreaWhereItWasSelected()
    {
        // `chosen` is an express reference to the selected in-play card, not
        // permission to follow it into a later out-of-play area. Its selection
        // origin therefore survives the first component's hand move.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            target = InPlay(board, "01092");
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "removeFromGame": { "cardsIn": {
              "area": "encounterDiscardPile", "title": "Hydra Mercenary"
            } } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? target = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            target = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "removeFromGame": { "cardsIn": {
              "area": "encounterDiscardPile", "title": "Hydra Mercenary"
            } } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "discard": { "titled": "Hydra Mercenary" } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? inPlay = null;
        Card? discarded = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            inPlay = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            discarded = board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "discard": { "titled": "Hydra Mercenary" } },
              { "discard": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? inPlay = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            inPlay = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "search": {
              "in": [ { "encounterDeck": 1 }, { "encounterDiscardPile": 1 } ],
              "for": "08028"
            } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDeck));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "choose": { "options": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "draw": { "player": "you", "count": 1 } }
              ] } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? inPlay = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            inPlay = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "choose": { "options": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "discard": { "titled": "Hydra Mercenary" } }
              ] } },
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
}
