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
public sealed class ActionAbilityProjectedMovementCompiledCostsProjectCombinedDamageTests
{
    [Rule("rr:defeat.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompiledCostsProjectCombinedDamageAndHealingBeforeAnAreaQuery(bool healFirst)
    {
        // rr:defeat.1: "If an ally, minion, or side scheme is defeated, it is
        // discarded." Two cost packets can jointly defeat the source;
        // healing first can keep it and its hosted card in play.
        string healing = healFirst ? """{ "heal": { "card": "this", "amount": 1 } },""" : "";
        var runner = Runner("01030", "Action", """
            { "removeFromGame": { "cardsIn": {
              "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
            } } }
            """, cost: $$$"""
            { "seq": [
              { "exhaust": "this" },
              {{{healing}}}
              { "dealDamage": { "cards": "this", "amount": 1 } },
              { "dealDamage": { "cards": "this", "amount": 1 } }
            ] }
            """);
        Card? source = null;
        Card? hosted = null;
        Card? discarded = null;
        (Game Game, World World) Start() => Playing(board =>
        {
            source = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            source.TakeDamage(2);
            hosted = board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), source.ObjectId));
            discarded = board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner);
        if (healFirst)
        {
            var(game, _) = Start();
            var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
            game.Resolve(Decision.Take(action.Id));
            Assert.Equal(3, source!.Damage);
            Assert.False(source.Ready);
            Assert.Equal(DeckType.RemovedArea, discarded!.Area.Type);
        }
        else
        {
            Assert.Throws<RulesNotImplementedException>(() => Start());
            Assert.Equal(2, source!.Damage);
            Assert.True(source.Ready);
            Assert.Equal(DeckType.EncounterDiscardPile, discarded!.Area.Type);
        }

        Assert.Equal(DeckType.AlliesArea, source.Area.Type);
        Assert.Equal(source.ObjectId, hosted!.Area.Host);
    }

    [Rule("rr:otherwise.1")]
    [Fact]
    public void ProjectedIfWithoutAnActiveBranchResolvesNone()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }

    [Rule("rr:victory-x.1")]
    [Fact]
    public void VictoryMinionDoesNotProjectToEncounterDiscard()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var minion = board.CreateCard("16183", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            board.CreateCard("16183", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }

    [Rule("rr:victory-x.1.2")]
    [Fact]
    public void NestedVictoryAttachmentProjectsItsOrdinaryDiscard()
    {
        // Only a Victory attachment directly on the defeated character uses
        // the Forced Interrupt that moves it to the victory display. Its own
        // hosted card leaves through ordinary attachment cleanup, even when
        // that nested card also has Victory X.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            direct = board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId));
            nested = board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), direct.ObjectId));
            foreach (var attachment in new[]
            {
                direct,
                nested
            }

            )
            {
                board.Effects.Register(new ContinuousEffect(EffectSource.ConstantAbility, "victory", Amount: 1, Card: attachment.ObjectId, Affects: attachment.ObjectId, Lasts: Duration.WhileInPlay));
            }

            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
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
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var scheme = board.CreateCard("16182a", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 1);
            board.CreateCard("16182a", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }

    [Rule("rr:victory-x.1")]
    [Fact]
    public void LaterSelectorCannotSeeProjectedDepartedVictoryMinion()
    {
        // After the Victory minion leaves play, a later title reference no
        // longer denotes it. Its no-op discard therefore cannot destabilize an
        // unrelated singular encounter-discard query.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var minion = board.CreateCard("16183", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }

    [Rule("rr:otherwise.1")]
    [Rule("rr:victory-x.1")]
    [Fact]
    public void DepartedTargetMakesProjectedOtherwiseFallbackReachable()
    {
        // The defeated Victory minion is no longer an in-play title reference,
        // so healing it resolves nothing and the otherwise discard applies.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
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
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? victory = null;
        Card? hydra = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            victory = board.CreateCard("16183", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            victory.TakeDamage(Damage.Health(board, board.Facts, victory) - 1);
            hydra = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, victory!.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }
}
