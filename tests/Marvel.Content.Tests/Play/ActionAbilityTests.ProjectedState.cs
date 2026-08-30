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

public sealed partial class ActionAbilityTests
{
    [Rule("rr:in-play-and-out-of-play.4")]
    [Fact]
    public void ExplicitMovementUpdatesLaterProjectedTitleReferences()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "removeFromGame": { "titled": "Hydra Mercenary" } },
              { "discard": { "titled": "Hydra Mercenary" } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:villain-defeat.4.2")]
    [Fact]
    public void ConsecutiveVillainStagesProjectCarriedAttachmentDeparture()
    {
        // Rhino I carries the attachment to Rhino II. Defeating the final
        // stage then discards it ordinarily, even when it has Victory X.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? villain = null;
        Card? attachment = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                attachment = board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.Effects.Register(new ContinuousEffect(
                    EffectSource.ConstantAbility,
                    "victory",
                    Amount: 1,
                    Card: attachment.ObjectId,
                    Affects: attachment.ObjectId,
                    Lasts: Duration.WhileInPlay));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.True(DeckTypes.IsInPlay(attachment!.Area.Type));
        Assert.Equal(villain.ObjectId, attachment.Area.Host);
    }

    [Rule("rr:enters-play")]
    [Fact]
    public void EnteredMinionParticipatesInLaterProjectedQueries()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? hydra = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var protectedMinion = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, protectedMinion, Statuses.Tough);
                hydra = board.CreateCard(
                    "02007", board.AreaOf(DeckType.EncounterDeck));
                board.CreateCard(
                    "02007", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDeck, hydra!.Area.Type);
    }

    [Rule("rr:enters-play")]
    [Fact]
    public void EnteredMinionParticipatesInLaterProjectedRankedQueries()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? hydra = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var protectedMinion = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, protectedMinion, Statuses.Tough);
                hydra = board.CreateCard(
                    "02007", board.AreaOf(DeckType.EncounterDeck));
                board.CreateCard(
                    "02007", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDeck, hydra!.Area.Type);
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:toughness.1")]
    [Fact]
    public void EnteredMinionReceivesLaterProjectedStatus()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? hydra = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                hydra = board.CreateCard(
                    "02007", board.AreaOf(DeckType.EncounterDeck));
                board.CreateCard(
                    "02007", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
        Assert.Equal(DeckType.EncounterDeck, hydra!.Area.Type);
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Fact]
    public void ReenteringSourceInvalidatesLaterProjectedThisBinding()
    {
        var runner = Runner(
            "02007",
            "Action",
            """
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
        var (_, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "02007", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "02007", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
        Assert.Equal(DeckType.EngagedEnemiesArea, source!.Area.Type);
    }

    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:status-cards.1")]
    [Fact]
    public void VillainAdvancementCarriesProjectedStatusCards()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                Statuses.Give(
                    board, board.TheCardIn(DeckType.VillainArea)!,
                    Statuses.Stunned);
                minion = board.CreateCard(
                    "02007", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "02007", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:enters-play")]
    [Rule("rr:enemy")]
    [Fact]
    public void EnteredEnemyParticipatesInProjectedTraitQueries()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? entrant = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var protectedMinion = board.CreateCard(
                    "02007", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, protectedMinion, Statuses.Tough);
                entrant = board.CreateCard(
                    "02007", board.AreaOf(DeckType.EncounterDeck));
                board.CreateCard(
                    "02007", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDeck, entrant!.Area.Type);
    }

    [Rule("rr:lasting-effects")]
    [Rule("rr:traits.1")]
    [Fact]
    public void LastingTraitGrantChangesLaterProjectedTraitQuery()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? specialist = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var criminal = board.CreateCard(
                    "02007", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, criminal, Statuses.Tough);
                specialist = board.CreateCard(
                    "02008", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "02008", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, specialist!.Area.Type);
    }

    [Rule("rr:ability")]
    [Rule("rr:traits.1")]
    [Fact]
    public void DepartedConstantSourceStopsProjectedTraitGrant()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "discard": { "titled": "Helicarrier" } },
              { "dealDamage": {
                "cards": { "enemiesWithTrait": "CRIMINAL" }, "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? grantSource = null;
        Card? hydra = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                grantSource = board.CreateCard(
                    "01092", board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                hydra = board.CreateCard(
                    "08028", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                var criminal = board.CreateCard(
                    "02007", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, criminal, Statuses.Tough);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
                board.Effects.Register(new ContinuousEffect(
                    EffectSource.ConstantAbility,
                    Rules.State.Traits.Granted + "CRIMINAL",
                    Amount: 1,
                    Card: grantSource.ObjectId,
                    Affects: hydra.ObjectId,
                    Lasts: Duration.WhileInPlay));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
        Assert.Equal(DeckType.SupportsArea, grantSource!.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

    [Rule("rr:lasting-effects")]
    [Fact]
    public void LastingAttackGrantChangesLaterProjectedRanking()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? hydra = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                hydra = board.CreateCard(
                    "08028", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                var criminal = board.CreateCard(
                    "02007", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, criminal, Statuses.Tough);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

    [Rule("rr:ability")]
    [Rule("rr:hit-points.2.3")]
    [Fact]
    public void DepartedConstantSourceStopsProjectedHealthGrant()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "discard": { "titled": "Helicarrier" } },
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 3
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? grantSource = null;
        Card? hydra = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                grantSource = board.CreateCard(
                    "01092", board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                hydra = board.CreateCard(
                    "08028", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
                board.Effects.Register(new ContinuousEffect(
                    EffectSource.ConstantAbility,
                    "health",
                    Amount: 3,
                    Card: grantSource.ObjectId,
                    Affects: hydra.ObjectId,
                    Lasts: Duration.WhileInPlay));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, grantSource!.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:lasting-effects")]
    [Fact]
    public void LastingGuardGrantChangesProjectedAttackableEnemies()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                villain.TakeDamage(
                    Damage.Health(board, board.Facts, villain) - 1);
                minion = board.CreateCard(
                    "08028", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, minion, Statuses.Tough);
                board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:hit-points.2.3")]
    [Fact]
    public void ChangedThreatRefusesStaleConditionalHealthProjection()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "removeThreat": {
                "scheme": { "titled": "Gene Pool" }, "amount": 1
              } },
              { "dealDamage": {
                "cards": { "titled": "Infinite Soldier" }, "amount": 3
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Infinite Soldier"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? pool = null;
        Card? soldier = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                pool = board.CreateCard(
                    "45071", board.AreaOf(DeckType.SideSchemesArea));
                pool.PlaceTokens("k_threat", 9);
                soldier = board.CreateCard(
                    "45069", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "45069", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("conditional constant", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(9, pool!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, soldier!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, soldier.Area.Type);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:villain-defeat.4.2")]
    [Fact]
    public void DepartedGuardReaddsVillainToProjectedAttackableEnemies()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 3
              } },
              { "dealDamage": {
                "cards": { "query": "attackableEnemies" }, "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? guard = null;
        Card? attachment = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                villain.TakeDamage(
                    Damage.Health(board, board.Facts, villain) - 1);
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                guard = board.CreateCard(
                    "08028", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                attachment = board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, guard!.Area.Type);
        Assert.True(DeckTypes.IsInPlay(attachment!.Area.Type));
    }

    [Rule("rr:ability")]
    [Rule("rr:hit-points.2.3")]
    [Fact]
    public void UnconditionalHealthConstantSurvivesUnrelatedProjectedChange()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "removeThreat": {
                "scheme": { "query": "mainScheme" }, "amount": 1
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
                scheme.PlaceTokens("k_threat", 1);
                board.CreateCard(
                    "01127", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:ability.9")]
    [Fact]
    public void ProjectedStatusRefusesNewlyActiveConditionalAttackConstant()
    {
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": { "seq": [
                      { "giveStatus": {
                        "card": { "titled": "Hydra Mercenary" },
                        "status": "stunned"
                      } },
                      { "dealDamage": {
                        "cards": { "maxBy": {
                          "of": { "query": "minions" }, "by": "attack"
                        } },
                        "amount": 100
                      } },
                      { "removeFromGame": { "cardsIn": {
                        "area": "encounterDiscardPile",
                        "title": "Hydra Mercenary"
                      } } }
                    ] }
                  } ] },
                  { "card": "08028", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "hasStatus": {
                        "card": "this", "status": "stunned"
                      } },
                      "then": { "grant": {
                        "card": "this", "keyword": "attack", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
        Card? source = null;
        Card? hydra = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                hydra = board.CreateCard(
                    "08028", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                var criminal = board.CreateCard(
                    "02007", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, criminal, Statuses.Tough);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("conditional constant", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
        Assert.Equal(0, Statuses.Count(world!, hydra, Statuses.Stunned));
    }

    [Rule("rr:ability.9")]
    [Fact]
    public void UnrelatedConditionalHealthConstantDoesNotBlockVillainProjection()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var pool = board.CreateCard(
                    "45071", board.AreaOf(DeckType.SideSchemesArea));
                pool.PlaceTokens("k_threat", 9);
                board.CreateCard(
                    "45069", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:ability.9")]
    [Fact]
    public void DynamicConstantAmountRefusesStaleProjectedRanking()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
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
            ] }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? titania = null;
        Card? hydra = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                titania = board.CreateCard(
                    "01162", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                hydra = board.CreateCard(
                    "08028", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? titania = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var criminal = board.CreateCard(
                    "02007", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, criminal, Statuses.Tough);
                titania = board.CreateCard(
                    "01162", board.AreaOf(DeckType.EncounterDeck));
                board.CreateCard(
                    "01162", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("conditional constant", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDeck, titania!.Area.Type);
    }

    [Rule("rr:attachment.1")]
    [Rule("rr:ability")]
    [Fact]
    public void RehostedAttachmentRetargetsProjectedHealthGrant()
    {
        var runner = Runner(
            "01163",
            "Action",
            """
            { "seq": [
              { "attachTo": { "titled": "Hydra Mercenary" } },
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 3
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            includeAuthored: true);
        Card? attachment = null;
        Card? hydra = null;
        var (_, world) = Playing(
            board =>
            {
                var originalHost = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                attachment = board.CreateCard(
                    "01163", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        originalHost.ObjectId));
                hydra = board.CreateCard(
                    "08028", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == attachment!.ObjectId);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

    [Rule("rr:attachment.1")]
    [Rule("rr:ability")]
    [Fact]
    public void DepartedRehostedAttachmentStopsProjectedHealthGrant()
    {
        var runner = Runner(
            "01163",
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? attachment = null;
        Card? originalHost = null;
        Card? hydra = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                originalHost = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                attachment = board.CreateCard(
                    "01163", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        originalHost.ObjectId));
                hydra = board.CreateCard(
                    "08028", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(attachment!.Ready);
        Assert.Equal(originalHost!.ObjectId, attachment.Area.Host);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

    [Rule("rr:attachment.1")]
    [Rule("rr:traits.1")]
    [Fact]
    public void RehostedAttachmentRetargetsProjectedTraitGrant()
    {
        var runner = Runner(
            "40151",
            "Action",
            """
            { "seq": [
              { "attachTo": { "titled": "Hydra Mercenary" } },
              { "dealDamage": {
                "cards": { "enemiesWithTrait": "AERIAL" }, "amount": 3
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            includeAuthored: true);
        Card? attachment = null;
        Card? originalHost = null;
        Card? hydra = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                originalHost = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                attachment = board.CreateCard(
                    "40151", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        originalHost.ObjectId));
                hydra = board.CreateCard(
                    "08028", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.Equal(originalHost!.ObjectId, attachment!.Area.Host);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:villain-defeat.4.2")]
    [Fact]
    public void AttachmentAddedAfterAdvancementProjectsFinalStageDeparture()
    {
        var runner = Runner(
            "01098",
            "Action",
            """
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

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                attachment = board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.Equal(DeckType.UpgradesArea, attachment!.Area.Type);
        Assert.Equal(-1, attachment.Area.Host);
    }

    [Rule("rr:toughness.1")]
    [Fact]
    public void EnteredCharactersProjectTheirPrintedToughness()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? sandman = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                sandman = Assert.Single(
                    board.AreaOf(DeckType.EncounterDeck).Cards,
                    card => string.Equals(
                        board.Facts.Title(card.FaceId),
                        "Sandman",
                        StringComparison.Ordinal));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
        Assert.Equal(DeckType.EncounterDeck, sandman!.Area.Type);
    }

    [Rule("rr:toughness.1")]
    [Rule("rr:villain-defeat.3.2")]
    [Fact]
    public void EnteredVillainStageProjectsItsPrintedToughness()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard("01096", board.AreaOf(DeckType.VillainDeck));
                board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:victory-x")]
    [Fact]
    public void EnteredGuardRecomputesProjectedAttackableEnemies()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                villain.TakeDamage(
                    Damage.Health(board, board.Facts, villain) - 1);
                board.CreateCard("55056", board.AreaOf(DeckType.EncounterDeck));
                board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:attachment.1")]
    [Rule("rr:victory-x.1")]
    [Fact]
    public void ProjectedAttachmentJoinsItsHostsDefeatTree()
    {
        var runner = Runner(
            "01098",
            "Action",
            """
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
        var (_, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "01098", board.AreaOf(DeckType.UpgradesArea));
                minion = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world,
            new PendingAbility(source!.ObjectId, AbilityType.Action, 0),
            [],
            []));

        Assert.Equal(DeckType.UpgradesArea, source!.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:thwart")]
    [Rule("rr:victory-x")]
    [Fact]
    public void ThwartSchemesSkipsProjectedDepartedSchemes()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "16182a", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.6")]
    [Fact]
    public void CancelledLabelSkipsAreaSensitivePostArrowEffects()
    {
        var runner = Runner(
            "01017",
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""",
            labels: "[ \"attack\" ]");
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
                Statuses.Give(
                    board, board.Seats[0].IdentityCard, Statuses.Stunned);
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        var action = Assert.Single(runner.Actions(world, 0), option =>
            option.Card == source!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.False(source!.Ready);
        Assert.False(Statuses.Has(
            world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:thwart")]
    [Rule("rr:cost.6")]
    [Fact]
    public void ThwartSchemesPowerIsProjectedBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""",
            labels: "[ \"thwart\" ]");
        Card? source = null;
        Card? scheme = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01107", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
                board.CreateCard(
                    "01107", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(1, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:search.1")]
    [Fact]
    public void InactiveBranchDoesNotMakeALaterAreaQueryUnstable()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "if": {
                "test": { "inForm": { "player": "you", "form": "alterEgo" } },
                "then": { "discard": { "titled": "Hydra Mercenary" } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? target = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                target = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.RemovedArea, target!.Area.Type);
    }

    [Rule("rr:for-each.1")]
    [Rule("rr:search.1")]
    [Fact]
    public void ZeroIterationDoesNotMakeALaterAreaQueryUnstable()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "forEach": { "count": 0, "effect": {
                "discard": { "titled": "Hydra Mercenary" }
              } } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? target = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                target = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.RemovedArea, target!.Area.Type);
    }

    [Rule("rr:damage.3")]
    [Rule("rr:search.1")]
    [Fact]
    public void NonlethalVillainDamageDoesNotMakeEncounterDiscardUnstable()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? target = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                target = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);

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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "removeThreat": {
                "scheme": { "titled": "Breakin' & Takin'" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Breakin' & Takin'"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01107", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
                board.CreateCard(
                    "01107", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01107", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 2);
                board.CreateCard(
                    "01107", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cost.6")]
    [Rule("rr:defeat.1")]
    [Fact]
    public void SequentialThreatRemovalProjectsItsCombinedAmountBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01107", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 2);
                board.CreateCard(
                    "01107", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cost.6")]
    [Rule("rr:defeat.1")]
    [Rule("rr:move.1")]
    [Fact]
    public void LethalMovedDamageRaisesBeforeHealingOrActionCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "moveDamage": {
                "from": "you", "to": { "titled": "Hydra Mercenary" },
                "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        Card? identity = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                identity = board.Seats[0].IdentityCard;
                identity.TakeDamage(1);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

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
        var runner = Runner(
            "01005",
            "Action",
            """
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
        var (_, world) = Playing(
            board =>
            {
                played = board.CreateCard("01005", board.Seats[0].Hand);
                scheme = board.CreateCard(
                    "01107", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 2);
                board.CreateCard(
                    "01107", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "eventThreatRemoval", Amount: 1,
            Card: played!.ObjectId, Affects: played.ObjectId));

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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "removeFromGame": { "cardsIn": {
              "area": "encounterDiscardPile", "title": "Compact Darts"
            } } }
            """);
        Card? source = null;
        Card? target = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                target = board.CreateCard(
                    "27182a", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.RemovedArea, target!.Area.Type);
    }

    [Rule("rr:permanent.4")]
    [Rule("rr:then.1")]
    [Rule("rr:then.2")]
    [Theory]
    [InlineData("then", 0)]
    [InlineData("otherwise", 1)]
    public void SkippedPermanentDiscardHasTheCorrectDependentOutcome(
        string dependency, int cardsDrawn)
    {
        // The cross-set Permanent is not a legal discard target, so that
        // component resolves none. "Then" does not run; "otherwise" does.
        // A valid exhaust sibling keeps the overall target legal.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            $$"""
            { "chooseCard": {
              "from": { "titled": "Compact Darts" },
              "effect": { "seq": [
                { "exhaust": "chosen" },
                { "{{dependency}}": {
                  "effect": { "discard": "chosen" },
                  "{{dependency}}": { "draw": { "player": "you", "count": 1 } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        Card? permanent = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                permanent = board.CreateCard(
                    "27182a", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner);
        int hand = world.Seats[0].Hand.Cards.Count;

        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(permanent!.ObjectId));

        Assert.False(permanent.Ready);
        Assert.Equal(DeckType.UpgradesArea, permanent.Area.Type);
        Assert.Equal(hand + cardsDrawn, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:permanent.5")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void PermanentOnLethalTargetRaisesBeforeALabelledActionCostMutates()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 3
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? guard = null;
        Card? permanent = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                guard = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                permanent = board.CreateCard(
                    "27189a",
                    board.AreaOf(
                        DeckType.UpgradesArea, guard.Area.PlayArea,
                        guard.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("rr:permanent.5 is not implemented", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, guard.Area.Type);
        Assert.Equal(guard.ObjectId, permanent!.Area.Host);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:target.3.8")]
    [Fact]
    public void GuardMakesTheVillainInvalidBeforeTracingALabelledAttack()
    {
        // Guard says “The engaged player cannot attack any villain,” and a
        // target that cannot be attacked is not valid for an attack-labeled
        // ability. Later effects cannot first remove that initiation limit.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Hydra Mercenary" },
                  "keyword": "health", "amount": 2, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:defeat.1")]
    [Rule("rr:guard.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DiscardedConstantHealthGrantCannotKeepAGuardAliveInTheTrace()
    {
        // Constant abilities are active only while their source remains in
        // play. Discarding Genetically Enhanced removes its +3 hit points, so
        // the following 3 damage defeats Hydra Mercenary and removes Guard.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Genetically Enhanced" } },
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? guard = null;
        Card? enhanced = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                guard = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                enhanced = board.CreateCard(
                    "01163",
                    board.AreaOf(
                        DeckType.UpgradesArea, guard.Area.PlayArea,
                        guard.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(guard.ObjectId, enhanced!.Area.Host);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DiscardedConstantHealthGrantCannotKeepAVillainStageAliveInTheTrace()
    {
        // Constant abilities are active only while their source remains in
        // play. Discarding The "Immortal" Klaw removes +10 hit points, so the
        // next damage defeats this stage and the new stage begins undamaged.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "The \"Immortal\" Klaw" } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? villain = null;
        Card? immortal = null;

        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                immortal = board.CreateCard(
                    "01127", board.AreaOf(DeckType.SideSchemesArea));
                // Rhino I has 28 hit points in this two-player game. Leave him
                // one below that printed maximum; Immortal Klaw raises the
                // live maximum to 38 until the first effect discards it.
                villain.TakeDamage(27);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(27, villain!.Damage);
        Assert.Equal(DeckType.SideSchemesArea, immortal!.Area.Type);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:defeat.1")]
    [Rule("rr:side-scheme.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DefeatedSideSchemeStopsItsProhibitionInADirectLabelledPower()
    {
        // A side scheme with no threat is defeated and discarded. Removing
        // Legions of Hydra's final threat therefore ends the prohibition that
        // kept Madame Hydra from taking the following damage.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Legions of Hydra" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Madame Hydra" },
                  "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? legions = null;
        Card? madame = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                legions = board.CreateCard(
                    "01180", board.AreaOf(DeckType.SideSchemesArea));
                legions.PlaceTokens("k_threat", 1);
                madame = board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(1, legions!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, madame!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:crisis-icon.1")]
    [Rule("rr:defeat.1")]
    [Rule("rr:side-scheme.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DefeatedCrisisSchemeUnlocksMainSchemeRemovalInTheTrace()
    {
        // While a crisis icon is in play, player cards cannot remove threat
        // from the main scheme. Crowd Control is discarded when its last
        // threat is removed, so the following main-scheme removal resolves.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Crowd Control" }, "amount": 1
                } },
                { "then": {
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } },
                  "then": { "dealDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? crowd = null;
        Card? main = null;
        Card? villain = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                crowd = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crowd.PlaceTokens("k_threat", 1);
                main = board.TheCardIn(DeckType.MainSchemesArea)!;
                main.PlaceTokens("k_threat", 1);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(1, crowd!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(1, main!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:crisis-icon.1")]
    [Rule("rr:then")]
    [Fact]
    public void ExplicitCrisisExceptionIsHonoredByInitiationResolutionAndThen()
    {
        // Crisis says a player card cannot remove threat from the main scheme.
        // This exact instruction says it ignores crisis, so the explicit card
        // exception wins and the fully resolved removal permits its `then`.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "then": {
              "effect": { "removeThreat": {
                "scheme": { "query": "mainScheme" },
                "amount": 1,
                "ignoresCrisis": "true"
              } },
              "then": { "draw": { "player": "you", "count": 1 } }
            } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(
            1,
            world.TheCardIn(DeckType.MainSchemesArea)!
                .Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

}
