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
public sealed class ActionAbilityProjectedStateDynamicConstantAmountRefusesStaleProjectedRankingTests
{
    [Rule("rr:ability.9")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DynamicConstantAmountRefusesStaleProjectedRanking(bool replaceAuthoredAmount)
    {
        var local = AbilityCatalog.Parse("""
            { "cards": [ { "card": "01006", "abilities": [ {
              "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
              "cost": { "exhaust": "this" },
              "effect": { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Titania" }, "amount": 5
              } },
              { "giveStatus": {
                "card": { "titled": "Titania" }, "status": "tough"
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
            ] } } ] } ] }
            """);
        var titaniaAbility = Assert.Single(AuthoredCards.Book.Abilities, ability => ability.Card == "01162");
        var grant = ((AbilityValue.Map)titaniaAbility.Effect.Argument).Entries.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var snapshotSource = titaniaAbility with
        {
            Effect = new AbilityNode("grant", new AbilityValue.Map(grant))
        };
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook([..AuthoredCards.Book.Abilities.Where(ability => ability.Card != "01162"), snapshotSource, ..local.Abilities], AuthoredCards.Book.Authored.Concat(local.Authored).ToHashSet(StringComparer.Ordinal), AuthoredCards.Book.AttachTo));
        if (replaceAuthoredAmount)
        {
            // Engine choice: projection analyzes the compiled instruction,
            // not a caller-owned replacement that hides its dynamic amount.
            grant["amount"] = new AbilityValue.Number(0);
        }

        Card? source = null;
        Card? titania = null;
        Card? hydra = null;
        World? world = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            titania = board.CreateCard("01162", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            hydra = board.CreateCard("08028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.Contains("conditional constant", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, titania!.Damage);
        Assert.Equal(0, Statuses.Count(world!, titania, Statuses.Tough));
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

    [Rule("rr:enters-play")]
    [Rule("rr:ability.9")]
    [Fact]
    public void EnteredDynamicConstantRefusesStaleProjectedRanking()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDeck" ], "title": "Titania"
                } },
                "where": "engagedWithYou"
              } },
              { "dealDamage": {
                "cards": { "maxBy": {
                  "of": { "query": "minions" }, "by": "attack"
                } },
                "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Titania"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? titania = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var criminal = board.CreateCard("02007", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, criminal, Statuses.Tough);
            titania = board.CreateCard("01162", board.AreaOf(DeckType.EncounterDeck));
            board.CreateCard("01162", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.Contains("conditional constant", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDeck, titania!.Area.Type);
    }

    [Rule("rr:attachment.1")]
    [Rule("rr:ability")]
    [Fact]
    public void RehostedAttachmentRetargetsProjectedHealthGrant()
    {
        var runner = Runner("01163", "Action", """
            { "seq": [
              { "attachTo": { "titled": "Hydra Mercenary" } },
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 3
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """, includeAuthored: true);
        Card? attachment = null;
        Card? hydra = null;
        var(_, world) = Playing(board =>
        {
            var originalHost = board.CreateCard("16183", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            attachment = board.CreateCard("01163", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), originalHost.ObjectId));
            hydra = board.CreateCard("08028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == attachment!.ObjectId);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

    [Rule("rr:attachment.1")]
    [Rule("rr:ability")]
    [Fact]
    public void DepartedRehostedAttachmentStopsProjectedHealthGrant()
    {
        var runner = Runner("01163", "Action", """
            { "seq": [
              { "attachTo": { "titled": "Hydra Mercenary" } },
              { "removeFromGame": "this" },
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 3
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? attachment = null;
        Card? originalHost = null;
        Card? hydra = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            originalHost = board.CreateCard("16183", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            attachment = board.CreateCard("01163", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), originalHost.ObjectId));
            hydra = board.CreateCard("08028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.True(attachment!.Ready);
        Assert.Equal(originalHost!.ObjectId, attachment.Area.Host);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:villain-defeat.4.2")]
    [Fact]
    public void AttachmentAddedAfterAdvancementProjectsFinalStageDeparture()
    {
        var runner = Runner("01098", "Action", """
            { "seq": [
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "attachTo": { "query": "villain" } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """);
        Card? attachment = null;
        Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            attachment = board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0)));
            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: runner));
        Assert.Equal(DeckType.UpgradesArea, attachment!.Area.Type);
        Assert.Equal(-1, attachment.Area.Host);
    }

    [Rule("rr:toughness.1")]
    [Fact]
    public void EnteredCharactersProjectTheirPrintedToughness()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDeck" ], "title": "Sandman"
                } },
                "where": "engagedWithYou"
              } },
              { "dealDamage": {
                "cards": { "titled": "Sandman" }, "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? sandman = null;
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            sandman = Assert.Single(board.AreaOf(DeckType.EncounterDeck).Cards, card => string.Equals(board.Facts.Title(card.FaceId), "Sandman", StringComparison.Ordinal));
            board.CreateCard("01098", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
        Assert.Equal(DeckType.EncounterDeck, sandman!.Area.Type);
    }
}
