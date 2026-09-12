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
public sealed class ActionAbilityProjectedStateEnteredVillainStageProjectsItsPrintedToughnessTests
{
    [Rule("rr:toughness.1")]
    [Rule("rr:villain-defeat.3.2")]
    [Fact]
    public void EnteredVillainStageProjectsItsPrintedToughness()
    {
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var villain = board.TheCardIn(DeckType.VillainArea)!;
            board.CreateCard("01096", board.AreaOf(DeckType.VillainDeck));
            board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:victory-x")]
    [Fact]
    public void EnteredGuardRecomputesProjectedAttackableEnemies()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDeck" ], "title": "Absorbing Man"
                } },
                "where": "engagedWithYou"
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
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var villain = board.TheCardIn(DeckType.VillainArea)!;
            villain.TakeDamage(Damage.Health(board, board.Facts, villain) - 1);
            board.CreateCard("55056", board.AreaOf(DeckType.EncounterDeck));
            board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }

    [Rule("rr:attachment.1")]
    [Rule("rr:victory-x.1")]
    [Fact]
    public void ProjectedAttachmentJoinsItsHostsDefeatTree()
    {
        var runner = Runner("01098", "Action", """
            { "seq": [
              { "attachTo": { "titled": "Badoon Headhunter" } },
              { "dealDamage": {
                "cards": { "titled": "Badoon Headhunter" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        var(_, world) = Playing(board =>
        {
            source = board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea));
            minion = board.CreateCard("16183", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, new PendingAbility(source!.ObjectId, AbilityType.Action, 0), [], []));
        Assert.Equal(DeckType.UpgradesArea, source!.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:thwart")]
    [Rule("rr:victory-x")]
    [Fact]
    public void ThwartSchemesSkipsProjectedDepartedSchemes()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "removeThreat": {
                "scheme": { "titled": "Kree Supremacy" }, "amount": 1
              } },
              { "thwartSchemes": {
                "schemes": { "query": "thwartableSchemes" },
                "power": { "thwart": {
                  "target": "chosen",
                  "effect": { "discard": {
                    "titled": "Hydra Mercenary"
                  } }
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
            var scheme = board.CreateCard("16182a", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 1);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.6")]
    [Fact]
    public void CancelledLabelSkipsAreaSensitivePostArrowEffects()
    {
        var runner = Runner("01017", "Action", """
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
            """, cost: """{ "exhaust": "this" }""", labels: "[ \"attack\" ]");
        Card? source = null;
        Card? minion = null;
        var(_, world) = Playing(board =>
        {
            source = board.CreateCard("01017", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
        }, hero: true, abilities: AuthoredCards.Runner());
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        runner.Act(world, action, [], []);
        Assert.False(source!.Ready);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:thwart")]
    [Rule("rr:cost.6")]
    [Fact]
    public void ThwartSchemesPowerIsProjectedBeforeCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "thwartSchemes": {
                "schemes": { "query": "thwartableSchemes" },
                "power": { "thwart": {
                  "target": "chosen",
                  "effect": { "removeThreat": {
                    "scheme": { "query": "powerTargets" }, "amount": 1
                  } }
                } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Breakin' & Takin'"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""", labels: "[ \"thwart\" ]");
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
        Assert.Equal(1, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:search.1")]
    [Fact]
    public void InactiveBranchDoesNotMakeALaterAreaQueryUnstable()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "if": {
                "test": { "inForm": { "player": "you", "form": "alter-ego" } },
                "then": { "discard": { "titled": "Hydra Mercenary" } }
              } },
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
}
