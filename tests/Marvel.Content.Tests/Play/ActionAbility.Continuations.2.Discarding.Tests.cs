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
public sealed class ActionAbilityContinuationsDiscardingTests
{
    [Rule("rr:status-cards.2")]
    [Rule("rr:guard.1")]
    [Fact]
    public void DiscardingAndReenteringAMinionDoesNotKeepHostedTough()
    {
        // A tough status card is placed on its character. Discarding Hydra
        // Mercenary discards that hosted status too, so re-entering without
        // printed Toughness leaves it vulnerable: three damage defeats it,
        // Guard leaves, and the later lethal move is reachable.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "putIntoPlay": {
                  "card": { "titled": "Hydra Mercenary" },
                  "where": "engagedWithYou"
                } },
                { "dealDamage": {
                  "cards": { "enemiesWithTrait": "HYDRA" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;
        Card? mercenary = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, mercenary!, Statuses.Tough);
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, mercenary!.Area.Type);
        Assert.True(Statuses.Has(world!, mercenary, Statuses.Tough));
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:attachment.1")]
    [Fact]
    public void ReenteringAMinionDoesNotKeepItsDiscardedAttachmentModifier()
    {
        // An attachment modifies the character it is attached to. Discarding
        // Hydra Mercenary also discards Gobbler Glider, so the re-entered
        // minion has ATK 1 and is the sole minimum below Rhino's ATK 2.
        var runner = ReenteredAttachmentRankRunner("minBy");
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("30027", board.AreaOf(DeckType.UpgradesArea, mercenary.Area.PlayArea, mercenary.ObjectId));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:attachment.1")]
    [Fact]
    public void DiscardedHostedModifierCannotHideALethalRankedContinuation()
    {
        // Neurological Implants gives its attached minion +2 ATK only while
        // attached. Once Hydra Mercenary and the attachment are discarded,
        // re-entered Hydra is ATK 1 and Rhino is the maximum-ATK enemy whose
        // damage makes the unsupported later frame reachable.
        var runner = ReenteredAttachmentRankRunner("maxBy");
        World? world = null;
        Card? source = null;
        Card? mercenary = null;
        Card? implants = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            implants = board.CreateCard("04119", board.AreaOf(DeckType.UpgradesArea, mercenary.Area.PlayArea, mercenary.ObjectId));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, mercenary!.Area.Type);
        Assert.Equal(mercenary.ObjectId, implants!.Area.Host);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:permanent.5")]
    [Fact]
    public void PermanentHostedDescendantRaisesBeforeAnActionCostMutates()
    {
        // When a permanent attachment loses its host, its attach-to text must
        // resolve and it is removed only if no valid target exists. That path
        // is explicitly unimplemented, so the complete hosted tree is checked
        // before exhausting the action source or discarding its host.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """, cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;
        Card? mercenary = null;
        Card? navigator = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            navigator = board.CreateCard("27189a", board.AreaOf(DeckType.UpgradesArea, mercenary.Area.PlayArea, mercenary.ObjectId));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("rr:permanent.5 is not implemented", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, mercenary!.Area.Type);
        Assert.Equal(mercenary.ObjectId, navigator!.Area.Host);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Fact]
    public void EnteringConstantAbilityRaisesBeforeARepeatedEffectMutates()
    {
        // Constant abilities are active while their source is in play. The
        // eligibility trace does not move cards, so it must raise rather than
        // rank Titania without her remaining-health ATK constant.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "if": {
                  "test": { "not": { "titleInPlay": "Titania" } },
                  "then": { "putIntoPlay": {
                    "card": { "cardsIn": {
                      "areas": [ "encounterDiscardPile" ], "title": "Titania"
                    } },
                    "where": "engagedWithYou"
                  } }
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Titania" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """, includeAuthored: true);
        World? world = null;
        Card? source = null;
        Card? titania = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            titania = board.CreateCard("01162", board.AreaOf(DeckType.EncounterDiscardPile));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("constant abilities", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, titania!.Area.Type);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RetargetingVillainConstantRaisesBeforeARepeatedEffectMutates()
    {
        // Defeating Klaw I makes Klaw II the villain, so The Immortal Klaw's
        // continuous +10 hit points follows the new stage. A repeated-effect
        // trace refuses before payment rather than evaluate its next frame
        // with that modifier still bound to Klaw I.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": {
                "player": "firstPlayer", "form": "hero"
              } },
              "then": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 36
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": {
                  "enemies": { "query": "villain" }
                } }
              } }
            } } } }
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        Card? villain = null;
        Card? immortal = null;
        World? world = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            villain = board.TheCardIn(DeckType.VillainArea)!;
            immortal = board.CreateCard("01127", board.AreaOf(DeckType.SideSchemesArea));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(DeckType.SideSchemesArea, immortal!.Area.Type);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }
}
