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
    [Rule("rr:guard.1")]
    [Rule("rr:engage.3")]
    [Fact]
    public void AOneTimeGuardEntryKeepsItsOriginalEngagementAcrossFrames()
    {
        // A card ability cannot make a minion engage the player it is already
        // engaged with. Hydra Mercenary enters engaged with player zero once;
        // the next frame does not put it into play again or move that
        // engagement to player one, so the lethal continuation is rejected.
        var runner = GuardEntryRunner();
        World? world = null;
        Card? source = null;
        Card? mercenary = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Fact]
    public void TraceLocalEngagementOverridesTheCardsFrozenBoardArea()
    {
        // Guard protects only the engaged player. After the hero frame moves
        // Hydra Mercenary from player zero to player one, the unchanged board
        // area must not also leave player zero guarded in the trace. Their
        // later frame damages Rhino, eliminates them, and exposes the third
        // player's unsupported branch before any real mutation occurs.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": {
                "player": "firstPlayer", "form": "alterEgo"
              } },
              "then": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "seq": [
                  { "discard": { "titled": "Hydra Mercenary" } },
                  { "putIntoPlay": {
                    "card": { "titled": "Hydra Mercenary" },
                    "where": "engagedWithYou"
                  } }
                ] },
                "else": { "seq": [
                  { "dealDamage": {
                    "cards": { "query": "attackableEnemies" }, "amount": 1
                  } },
                  { "moveDamage": {
                    "from": { "query": "villain" },
                    "to": { "titled": "Spider-Man" }, "amount": 1
                  } }
                ] }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;
        Card? mercenary = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(0), mercenary!.Area.PlayArea);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:engage.1")]
    [Rule("rr:guard.1")]
    [Fact]
    public void EngagementRelativeQueriesUseTheTraceLocalPlayer()
    {
        // Engagement is the player's play area at runtime. During eligibility,
        // trace-local entry is that area's synthetic equivalent: after the
        // hero re-engages Hydra, minionsEngagedWithYou must find and defeat it,
        // exposing Rhino before the lethal move and unsupported third frame.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": {
                "player": "firstPlayer", "form": "alterEgo"
              } },
              "then": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "seq": [
                  { "discard": { "titled": "Hydra Mercenary" } },
                  { "putIntoPlay": {
                    "card": { "titled": "Hydra Mercenary" },
                    "where": "engagedWithYou"
                  } },
                  { "dealDamage": {
                    "cards": { "query": "minionsEngagedWithYou" }, "amount": 3
                  } },
                  { "dealDamage": {
                    "cards": { "query": "attackableEnemies" }, "amount": 1
                  } },
                  { "moveDamage": {
                    "from": { "query": "villain" },
                    "to": { "titled": "Spider-Man" }, "amount": 1
                  } }
                ] }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;
        Card? mercenary = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, mercenary!, Statuses.Tough);
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(0), mercenary!.Area.PlayArea);
        Assert.True(Statuses.Has(world!, mercenary, Statuses.Tough));
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void TraceLocalGuardImmediatelyProtectsTheVillain()
    {
        // Guard means the engaged player cannot attack the villain, and a
        // lasting effect persists for its specified duration. Granting Sandman
        // Guard therefore removes Rhino from attackableEnemies before damage,
        // leaving no villain damage for the later move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Sandman" },
                  "keyword": "guard", "amount": 1,
                  "until": "EndOfPlayerPhase"
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
                ] }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:damage.step.7")]
    [Fact]
    public void DefeatingATraceEnteredGuardImmediatelyExposesTheVillain()
    {
        // Guard prevents attacks only while the minion remains engaged. Three
        // damage defeats Hydra Mercenary, so Guard leaves play before the next
        // effect damages Rhino and makes the following lethal move reachable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "if": {
                  "test": { "not": { "titleInPlay": "Hydra Mercenary" } },
                  "then": { "putIntoPlay": {
                    "card": { "cardsIn": {
                      "areas": [ "encounterDiscardPile" ],
                      "title": "Hydra Mercenary"
                    } },
                    "where": "engagedWithYou"
                  } }
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

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:status-cards.2")]
    [Rule("rr:guard.1")]
    [Fact]
    public void DiscardingAndReenteringAMinionDoesNotKeepHostedTough()
    {
        // A tough status card is placed on its character. Discarding Hydra
        // Mercenary discards that hosted status too, so re-entering without
        // printed Toughness leaves it vulnerable: three damage defeats it,
        // Guard leaves, and the later lethal move is reachable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, mercenary!, Statuses.Tough);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

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
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "30027",
                    board.AreaOf(
                        DeckType.UpgradesArea, mercenary.Area.PlayArea,
                        mercenary.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
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

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                implants = board.CreateCard(
                    "04119",
                    board.AreaOf(
                        DeckType.UpgradesArea, mercenary.Area.PlayArea,
                        mercenary.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;
        Card? mercenary = null;
        Card? navigator = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                navigator = board.CreateCard(
                    "27189a",
                    board.AreaOf(
                        DeckType.UpgradesArea, mercenary.Area.PlayArea,
                        mercenary.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;
        Card? titania = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                titania = board.CreateCard(
                    "01162",
                    board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? villain = null;
        Card? immortal = null;
        World? world = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                immortal = board.CreateCard(
                    "01127", board.AreaOf(DeckType.SideSchemesArea));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(DeckType.SideSchemesArea, immortal!.Area.Type);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteredCardActivatingVillainGrantRaisesBeforeRepeatedEffectMutates()
    {
        // The repeated-frame trace also treats Hydra Mercenary as in play
        // after its entry. That activates the continuous villain hit-point
        // grant before Klaw advances, so refusal precedes the exhaust cost.
        var runner = ConditionalVillainGrantRunner(repeated: true);
        Card? source = null;
        Card? conditional = null;
        Card? mercenary = null;
        Card? villain = null;
        World? world = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                mercenary = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:damage.step.1")]
    [Rule("rr:replacement-effect.1")]
    [Fact]
    public void ForcedReplacementCanLeaveARepeatedMoveSourceEmpty()
    {
        // Damage replacement abilities resolve before damage is placed. Once
        // Armored Rhino Suit puts that damage on itself "instead", "the effect
        // is no longer considered imminent" and Rhino has nothing to move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    AuthoredCards.ArmoredSuit,
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void BoundedThreatRemovalCannotDefeatADistantSideScheme()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Bomb Scare" }, "then": { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "01109", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 3);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void InactiveRemovalBranchDoesNotInflateARepeatedMutationBudget()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Bomb Scare" },
              "then": { "removeThreat": {
                "scheme": { "titled": "Bomb Scare" }, "amount": 1
              } },
              "else": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Bomb Scare" }, "amount": 100
                } },
                { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              ] }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "01109", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 3);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void RepeatedMutationAnalysisFlowsThroughOnePlayersSequence()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "seq": [
              { "if": {
                "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
                "then": { "draw": { "player": "you", "count": 1 } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } },
              { "discard": "this" },
              { "if": {
                "test": { "not": { "titleInPlay": "Aunt May" } },
                "then": { "changeForm": { "player": "firstPlayer", "to": "alterEgo" } }
              } }
            ] } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
    }

    [Rule("rr:choose-option")]
    [Fact]
    public void AChoiceContinuationPreservesEarlierEffectResults()
    {
        // "This way" is scoped to the one ability resolution. Asking a
        // question cannot replace that resolution with a fresh result map.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "heal": { "card": "you", "amount": 1 } }, { "choose": { "options": [ { "exhaust": "this" }, { "ready": "this" } ] } }, { "if": { "test": { "atLeast": { "value": { "result": "healed" }, "count": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(1);
            },
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void PayOrExhaustContinuesTheContainingSequence()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "payOrExhaust": { "resources": "YBR", "otherwise": { "exhaust": "this" } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(1));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:activation.7")]
    [Fact]
    public void SequentialActivationWaitsUseTheOriginatingFaceAndFreshResults()
    {
        // The first attack changes neither the identity's current face nor the
        // authored face that owns this ability. Its damage result must not leak
        // into the second wait's later condition.
        var runner = Runner(
            AuthoredCards.SpiderMan,
            "WhenRevealed",
            """{ "seq": [ { "changeForm": { "player": "you", "to": "alter-ego" } }, { "seq": [ { "enemyAttacks": { "enemies": { "query": "villain" } } }, { "enemyAttacks": { "enemies": { "query": "villain" } } } ] }, { "if": { "test": { "atLeast": { "value": { "result": "activationDamage" }, "count": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""",
            eventName: Steps.CardRevealed);
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var identity = world.Seats[0].IdentityCard;
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, identity, 0);
        Assert.Equal("01001b", identity.FaceId);
        var first = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Attack);

        runner.ActivationCompleted(world, new EnemyActivation(
            first.Subject, first.Seat, Attacking: true, first.ActivationId,
            Made: true, DamageDealt: 2));
        var second = Assert.Single(
            world.Agenda.Outstanding,
            step => step.What == Steps.Attack && step.ActivationId != first.ActivationId);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);

        runner.ActivationCompleted(world, new EnemyActivation(
            second.Subject, second.Seat, Attacking: true, second.ActivationId,
            Made: true, DamageDealt: 0));

        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerReconstructionUsesTheOriginatingIdentityFace()
    {
        var runner = Runner(
            AuthoredCards.SpiderMan,
            "WhenRevealed",
            """{ "seq": [ { "heal": { "card": "you", "amount": 1 } }, { "changeForm": { "player": "you", "to": "alter-ego" } }, { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "if": { "test": { "atLeast": { "value": { "result": "healed" }, "count": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""",
            eventName: Steps.CardRevealed);
        var (_, world) = Playing(
            board => board.Seats[0].IdentityCard.TakeDamage(1),
            hero: true,
            abilities: runner);
        var identity = world.Seats[0].IdentityCard;
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, identity, 0);
        var frame = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.ResolveEachPlayer);
        for (int advances = 0;
             world.Agenda.Current?.What != Steps.ResolveEachPlayer && advances < 20;
             advances++)
        {
            world.Agenda.Advance();
        }
        Assert.Equal(Steps.ResolveEachPlayer, world.Agenda.Current?.What);

        runner.ResolveEachPlayer(
            world, identity, frame.Seat, frame.Index, frame.Tier,
            frame.FinalStep, frame.FinalPlayer);

        Assert.Equal("01001b", identity.FaceId);
        Assert.Equal(0, identity.Damage);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:and.1")]
    [Fact]
    public void AFinalOrderedPowerKeepsNestedAncestorWorkPending()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "seq": [ { "and": [ { "draw": { "player": "you", "count": 1 } }, { "attack": { "target": { "query": "villain" }, "effect": { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } } } } ] }, { "draw": { "player": "you", "count": 1 } } ] } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.Equal(1, villain.Damage);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void InvalidOrderedContinuationIsRejectedBeforeRunningAnySibling()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var forged = new PhaseStep(
            Steps.ResumeAbility, 1, 2, Subject: source!.ObjectId, Seat: 0,
            Tier: AbilityType.Action, AbilityOrdinal: 0,
            AbilityPath: ["and:0:0,1:"], AbilityFace: source.FaceId,
            AbilityHasContinuation: true);

        Assert.Throws<RulesNotImplementedException>(
            () => runner.ResumeAbility(world, forged));

        Assert.True(source.Ready);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void LegacyContinuationWithoutSourceProvenanceRaisesInsteadOfSkipping()
    {
        // Continuation metadata from before card-incarnation bindings cannot
        // prove that `this` still means the same copy. That ambiguity raises;
        // it must not silently turn the remaining discard into a no-op.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "draw": { "player": "you", "count": 1 } }, { "discard": "this" } ] }""");
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var legacy = new PhaseStep(
            Steps.ResumeAbility, 1, 2, Subject: source!.ObjectId, Seat: 0,
            Tier: AbilityType.Action, AbilityOrdinal: 0,
            AbilityPath: ["seq:0"], AbilityFace: source.FaceId,
            AbilityHasContinuation: false);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => runner.ResumeAbility(world, legacy));

        Assert.Contains("source-card provenance", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
    }

    [Fact]
    public void ADirectLastingEffectCannotBeginOutsideItsPeriod()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """{ "grantUntil": { "card": "this", "trait": "AERIAL", "until": "EndOfAttack" } }""",
            eventName: Steps.CardRevealed);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => runner.WhenRevealed(world, source!, 0));

        Assert.Contains("outside its named period", thrown.Message, StringComparison.Ordinal);
        Assert.Empty(world.Effects.Active());
    }

    [Fact]
    public void PaymentCannotSwitchIntoALastingEffectOutsideItsPeriod()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "grantUntil": { "card": "you", "trait": "AERIAL", "until": "EndOfAttack" } } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("outside its named period", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void AStableFormBranchIgnoresAnUnreachableLastingConstraint()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "grantUntil": { "card": "this", "trait": "AERIAL", "until": "EndOfAttack" } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void PaymentCannotSwitchIntoALastingEffectWithNoTarget()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "grantUntil": { "card": "attachedTo", "trait": "AERIAL", "until": "EndOfRound" } } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("no target after payment", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void AFirstActivationResumesTheRestOfASequence()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """{ "seq": [ { "enemyAttacks": { "enemies": { "query": "villain" }, "first": "true" } }, { "draw": { "player": "you", "count": 1 } } ] }""",
            eventName: Steps.CardRevealed);
        Card? source = null;

        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, source!, 0);
        var attack = Assert.Single(
            world.Agenda.Outstanding, pending => pending.What == Steps.Attack);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);

        runner.ActivationCompleted(world, new EnemyActivation(
            attack.Subject, attack.Seat, Attacking: true, attack.ActivationId, Made: false));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AChoiceCannotOfferALastingEffectOutsideItsPeriod()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "choose": { "options": [ { "grantUntil": { "card": "this", "keyword": "attack", "amount": 1, "until": "EndOfAttack" } }, { "draw": { "player": "you", "count": 1 } } ] } }""");
        Card? source = null;

        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        var option = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(1, option.Id);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AResourceCostInsideASequenceIsAdvertised()
    {
        // Tenacity pays one physical resource and discards itself. Discarding
        // the upgrade is automatic, but the resource is a player choice and
        // therefore has to survive the surrounding cost sequence onto the
        // affordance.
        Card? tenacity = null;
        var (game, _) = Playing(
            board =>
            {
                tenacity = InPlay(board, "01093");
                board.Seats[0].IdentityCard.Exhaust();
                Physical(board, 1);
            },
            hero: true);

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == tenacity!.ObjectId);
        var price = Assert.Single(action.CostOptions);

        Assert.Equal("1", price.Cost);
        Assert.Equal(["R"], price.Rule);
    }

}
