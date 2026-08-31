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
    [Rule("rr:player-elimination.5")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RemovedIdentityRemainingHealthIsExactlyZero()
    {
        // Spider-Man's removal makes the selector absent, so remainingHealth
        // is zero and the surviving support's villain grant ends.
        var runner = RemovedIdentityHealthVillainGrantRunner();
        Card? source = null;

        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TakeDamage(9);
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:player-elimination.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EliminationPermanentAttachmentRaisesBeforePayment()
    {
        // Power Stone is a Permanent attachment. Eliminating its hero would
        // require resolving its attach-to text, which is intentionally
        // unsupported, so eligibility must refuse before the exhaust cost.
        var runner = PermanentEliminationRunner();
        Card? source = null;
        Card? stone = null;
        World? world = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                var identity = board.Seats[0].IdentityCard;
                stone = board.CreateCard(
                    "16149",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        identity.ObjectId, cardOwner: -1));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("permanent attachment", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.UpgradesArea, stone!.Area.Type);
    }

    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HealthModifierInvalidatesRemainingHealthPredicate(bool repeated)
    {
        // The lasting +1 health makes undamaged Spider-Man's remaining health
        // eleven before Klaw advances. That activates the retargeting constant,
        // which must be refused before the labelled cost or lasting state lands.
        var runner = HealthModifierVillainGrantRunner(repeated);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:discard.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DepartureInvalidatesRemainingHealthPredicate(bool repeated)
    {
        // Discarding Vulture removes it from play, so its queried remaining
        // health becomes zero. The inverse constant then grants health to the
        // new villain and must be recognized before the action exhausts.
        var runner = DepartedAmountVillainGrantRunner(repeated);
        Card? source = null;
        Card? vulture = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                vulture = board.CreateCard(
                    "01167",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, vulture!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ZeroHealthModifierDoesNotInvalidatePredicate(bool repeated)
    {
        // A zero modifier does not alter Spider-Man's remaining health. The
        // threshold remains false, so its inactive villain grant cannot make
        // either legal action shape look unsupported.
        var runner = ZeroHealthModifierVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VillainDamageDoesNotInvalidateHeroModifiedField(bool repeated)
    {
        // Damaging Klaw does not modify Spider-Man's attack. His threshold
        // remains false, so unrelated damage cannot expose the inactive
        // villain-health branch in either preflight shape.
        var runner = UnrelatedModifiedVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EntryInvalidatesRemainingHealthPredicate(bool repeated)
    {
        // Hydra Mercenary begins out of play with queried remaining health
        // zero. Putting it into play makes that amount positive and activates
        // the villain grant before advancement, so cost must remain unpaid.
        var runner = EnteredAmountVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrossCardConditionalModifierInvalidatesModifiedField(bool repeated)
    {
        // Damage on Vulture activates one constant that grants Spider-Man +1
        // attack. A second constant then reaches its threshold and retargets
        // health to the new villain, a dependency chain preflight must follow.
        var runner = CrossCardModifierVillainGrantRunner(repeated);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01167",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01091",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnteredTraitActivatesConditionalModifierDependency(bool repeated)
    {
        // Hydra Mercenary's entry makes its printed HYDRA trait query true.
        // That activates Spider-Man's attack grant, which in turn activates
        // the villain-health grant before Klaw advances.
        var runner = EnteredTraitModifierVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
                board.CreateCard(
                    "01091",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DecisiveFalseBranchIgnoresChangingEnteredTrait(bool repeated)
    {
        // A villain exists before and after advancement, so the first false
        // conjunct decisively keeps the modifier inactive even though Hydra
        // Mercenary enters and makes the second conjunct true.
        var runner = EnteredTraitModifierVillainGrantRunner(
            repeated, decisiveFalse: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
                board.CreateCard(
                    "01091",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:vulnerable.1")]
    [Rule("rr:permanent.5")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VulnerableStatusDiscardPreflightsPermanentBeforeCost(bool repeated)
    {
        // Becoming Stunned discards a Vulnerable character. Its Permanent
        // attachment makes that cleanup unsupported, so both trace shapes
        // refuse before the labelled action exhausts its source.
        var runner = VulnerableStatusRunner(repeated);
        World? world = null;
        Card? source = null;
        Card? scientist = null;
        Card? permanent = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                scientist = board.CreateCard(
                    "50083",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                permanent = board.CreateCard(
                    "27189a",
                    board.AreaOf(
                        DeckType.UpgradesArea, scientist.Area.PlayArea,
                        scientist.ObjectId));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("rr:permanent.5 is not implemented", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, scientist!.Area.Type);
        Assert.Equal(scientist.ObjectId, permanent!.Area.Host);
        Assert.False(Statuses.Has(world!, scientist, Statuses.Stunned));
    }

    [Rule("rr:target.4")]
    [Rule("rr:target.4.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyFinalStatusGroupDoesNotInvalidateEarlierTargets(bool repeated)
    {
        // The earlier status effects have a valid target. The final effect's
        // empty group is simply skipped: an ability that targets multiple game
        // elements can initiate with one valid target and does not resolve
        // against an element that is no longer valid.
        var runner = ReenteredVulnerableStatusRunner(repeated);
        Card? source = null;
        Card? scientist = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scientist = board.CreateCard(
                    "50083",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, scientist!.Area.Type);
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RestoredStatusInventoryDoesNotRemainMarkedChanged(bool repeated)
    {
        // Vulture begins Stunned, loses that attachment while discarded, then
        // re-enters and regains Stunned. The final predicate equals the live
        // board again, so its inactive inverse grant cannot block the action.
        var runner = RestoredStatusVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var vulture = board.CreateCard(
                    "01167",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, vulture, Statuses.Stunned);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReentryWithoutToughDoesNotCreateAStatusChange(bool repeated)
    {
        // Vulture has no Tough before or after leaving and re-entering play.
        // A zero trace override is equivalent to the live board and cannot
        // make the inactive Tough-conditioned villain grant appear reachable.
        var runner = ReenteredNoToughVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01167",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EntryWithoutToughDoesNotCreateAStatusChange(bool repeated)
    {
        // Hydra Mercenary enters play without Tough. The trace must preserve
        // that absence, so an inactive Tough-conditioned villain grant does
        // not make the otherwise legal action appear unsafe.
        var runner = EnteredNoToughVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attach-to.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LeavingHostInvalidatesItsStatusPredicate(bool repeated)
    {
        // A status is an attachment and leaves with its host. Discarding the
        // Stunned scientist therefore activates the inverse constant before
        // Klaw advances, which must be recognized before paying the cost.
        var runner = DiscardedStatusVillainGrantRunner(repeated);
        World? world = null;
        Card? source = null;
        Card? scientist = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                scientist = board.CreateCard(
                    "50083",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, scientist, Statuses.Stunned);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, scientist!.Area.Type);
        Assert.True(Statuses.Has(world!, scientist, Statuses.Stunned));
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormChangeEndingVillainGrantDoesNotPreventStageAdvancement(
        bool repeated)
    {
        // Changing to alter-ego ends this hero-only continuous hit-point grant
        // before Klaw I is defeated. Klaw II therefore enters without the
        // modifier in both a direct and an each-player trace.
        var runner = FormConditionalVillainGrantRunner(repeated);
        Card? source = null;
        Card? conditional = null;

        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
        Assert.Equal(AuthoredCards.SpiderMan, world.Seats[0].IdentityCard.FaceId);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FormChangeEndingHealthGrantActivatesVillainGrantBeforePayment()
    {
        // Spider-Man's hero-only hit-point grant keeps his remaining health at
        // 11. Changing to alter-ego ends it, which activates the conditional
        // villain grant before Klaw advances; refusal must precede the cost.
        var runner = FormConditionalHealthDependencyRunner();
        Card? source = null;
        Card? conditional = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FirstPlayerRebindActivatesVillainGrantBeforeAdvancement()
    {
        // Eliminating alter-ego Spider-Man passes the first-player token to
        // Captain Marvel in hero form. The first-player hero condition then
        // activates and its villain health grant must retarget to Klaw II.
        var runner = FirstPlayerVillainGrantRunner();
        Card? source = null;
        Card? conditional = null;
        Card? villain = null;
        World? world = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                board.Seats[1].IdentityCard.TurnTo("01010a");
                board.Seats[0].IdentityCard.TakeDamage(9);
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), conditional!.Area.PlayArea);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FirstPlayerRebindEndsVillainGrantBeforeAdvancement()
    {
        // Eliminating hero Spider-Man passes the first-player token to Carol
        // Danvers in alter-ego form. The hero-only villain health grant ends
        // before Klaw II enters and therefore does not retarget.
        var runner = FirstPlayerVillainGrantRunner();
        Card? source = null;
        Card? conditional = null;

        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TakeDamage(9);
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), conditional!.Area.PlayArea);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:player-elimination.5")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FirstPlayerRebindEndsConditionalHealthGrantBeforePayment()
    {
        // Hero Spider-Man initially makes the first-player condition grant
        // Carol +1 health. His elimination rebinds first player to alter-ego
        // Carol, ending that grant and activating the villain-health branch.
        var runner = FirstPlayerConditionalHealthDependencyRunner();
        Card? source = null;
        Card? villain = null;
        World? world = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                board.Seats[0].IdentityCard.TakeDamage(9);
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01091",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:each-player.1")]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EachPlayerOrderingChecksEveryVillainGrantPath()
    {
        // The first player chooses the each-player order. Resolving Spider-Man
        // first eliminates him and ends the hero-first-player grant; resolving
        // Carol Danvers first leaves Spider-Man and the grant active when Klaw
        // advances. Eligibility must include that legal ordering.
        var runner = OrderedFirstPlayerVillainGrantRunner();
        Card? source = null;
        Card? villain = null;
        World? world = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                board.Seats[0].IdentityCard.TakeDamage(9);
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.3")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RepeatedAdvanceDiscardsOldVillainConstantAttachment()
    {
        // Rhino's hosted attachment leaves when different-title Ultron III
        // enters play. Its continuous villain health grant is therefore gone
        // before the repeated continuation reads the new stage.
        var runner = DepartingVillainAttachmentRunner();
        Card? source = null;
        Card? attachment = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                attachment = board.CreateCard(
                    AuthoredCards.Charge,
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.NotNull(attachment);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteredCardActivatingVillainGrantRaisesBeforePowerMutates()
    {
        // Putting Hydra Mercenary into play makes the conditional constant
        // active before Klaw I is defeated. Its +10 hit points follows Klaw II,
        // so the trace must not test that branch against the unchanged discard.
        var runner = ConditionalVillainGrantRunner(repeated: false);
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

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FinalVillainAdvanceNeedNotModelEnteringConstants()
    {
        // Ultron III's constants become active when it enters play, but this
        // labelled effect ends at that point. No continuation reads them, so
        // the action is legal and can be advertised without projecting them.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FinalVillainAdvanceAfterAnotherStepNeedNotModelEnteringConstants()
    {
        // The same boundary holds when advancement is the last of several
        // effects: only a later sibling would observe the entering constant.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "heal": { "card": "you", "amount": 1 } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                board.Seats[0].IdentityCard.TakeDamage(1);
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Fact]
    public void ZeroForEachDoesNotHideALaterResolvableStep()
    {
        // Zero count means the repeated effect does not run; it does not make
        // the enclosing sequence unresolvable. The draw remains a meaningful
        // action and must still be advertised.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "forEach": { "count": 0, "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": { "chooseCard": {
                  "from": { "query": "minions" },
                  "effect": { "discard": "chosen" }
                } }
              } } } },
              { "draw": { "player": "you", "count": 1 } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Fact]
    public void ZeroForEachBodyIsUnreachableToContinuationPreflight()
    {
        // The zero-count body contains simultaneous threat placement, a shape
        // that would require a continuation if it ran. It cannot run, so
        // branch preflight must skip it and preserve the later draw action.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "if": {
                "test": { "titleInPlay": "Aunt May" },
                "then": { "forEach": { "count": 0, "effect": { "and": [
                  { "placeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } },
                  { "draw": { "player": "you", "count": 1 } }
                ] } } }
              } },
              { "draw": { "player": "you", "count": 1 } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Rule("rr:and")]
    [Fact]
    public void ZeroForEachDoesNotMakeASimultaneousSiblingSuspend()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "and": [
              { "forEach": { "count": 0, "effect": { "placeThreat": {
                "scheme": { "query": "mainScheme" }, "amount": 1
              } } } },
              { "draw": { "player": "you", "count": 1 } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void ZeroForEachHasNoResolutionForOtherwise()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "otherwise": {
                "effect": { "forEach": { "count": 0, "effect": {
                  "draw": { "player": "you", "count": 1 }
                } } },
                "otherwise": { "forEach": { "count": 0, "effect": {
                  "placeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  }
                } } }
              } },
              { "draw": { "player": "you", "count": 1 } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Fact]
    public void ZeroForEachChoiceDoesNotMakeALabelledPowerSuspend()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "forEach": { "count": 0, "effect": { "chooseCard": {
                  "from": { "query": "minions" },
                  "effect": { "discard": "chosen" }
                } } } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void ToughGrantedBeforeEachRepeatedDamagePreventsIt()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "giveStatus": {
                  "card": { "titled": "Spider-Man" }, "status": "tough"
                } },
                { "dealDamage": {
                  "cards": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void HealthGrantedBeforeRepeatedDamageRaisesItsLethalThreshold()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Spider-Man" },
                  "keyword": "health", "amount": 1, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void ProhibitedMoveLeavesDamageForALaterRepeatedMove()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Madame Hydra" },
                  "amount": 1
                } },
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
        World? world = null;
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                villain.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(1, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void ProhibitedDamageCannotReplenishARepeatedMoveSource()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Madame Hydra" },
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
                board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
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
    public void EarlierDiscardCanRemoveARepeatedMoveProhibition()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "if": {
                  "test": { "titleInPlay": "Legions of Hydra" },
                  "then": { "discard": { "titled": "Legions of Hydra" } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Madame Hydra" },
                  "amount": 1
                } },
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
                board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
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
    public void SideSchemeDefeatCanRemoveARepeatedDamageProhibition()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Legions of Hydra" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Madame Hydra" },
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
        World? world = null;
        Card? source = null;
        Card? legions = null;
        Card? madame = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
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

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(1, legions!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, madame!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void MinionDefeatCanRemoveARepeatedDamageProhibition()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "drones" }, "amount": 100 } },
                { "dealDamage": { "cards": { "titled": "Ultron" }, "amount": 1 } },
                { "moveDamage": {
                  "from": { "titled": "Ultron" },
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
        World? world = null;
        Card? source = null;
        Card? drone = null;
        Card? ultron = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                ultron = board.CreateCard(
                    "01136", board.AreaOf(DeckType.VillainArea));
                drone = FacedownDrones.EngageTop(
                    board, 0, "test", "Create_Drone", []);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.NotNull(drone);
        Assert.Equal(0, drone!.Damage);
        Assert.Equal(0, ultron!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.2")]
    [Fact]
    public void ARepeatedFrameUsesTheNewVillainStageAfterDefeat()
    {
        // "Excess damage that is dealt to defeat a villain stage does not
        // carry over to the new stage." The later move therefore reads zero
        // damage from the newly revealed stage, not the defeated card's dial.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 100 } },
                { "moveDamage": {
                  "from": { "titled": "Rhino" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
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
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.2")]
    [Fact]
    public void AFilteredVillainSelectorMutatesTheNewStageBeforeTheNextFrame()
    {
        // The new stage is the current in-play card titled Rhino. It begins
        // without the defeated stage's excess damage, then receives this
        // effect's next point. The following move must see that point or the
        // trace would offer an ability whose first frame defeats the player.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 100 } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "villain" }, "trait": "BRUTE"
                  } },
                  "amount": 1
                } },
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
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.4")]
    [Fact]
    public void ATitleSelectorDoesNotFollowADifferentVillainCharacter()
    {
        // A different-title stage is not Rhino. The live title selector finds
        // nothing after Ultron replaces Rhino, so it cannot put damage on
        // Ultron for the following move to carry to the wounded hero.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 100 } },
                { "dealDamage": { "cards": { "titled": "Rhino" }, "amount": 1 } },
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
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.2")]
    [Fact]
    public void ARankedSelectorIsRecomputedForTheNewVillainStage()
    {
        // Rhino I and Shocker tie for the lowest attack, but Rhino II does
        // not. The selector is evaluated after the stage changes, so only
        // Shocker receives the point and the new villain has none to move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 100 } },
                { "dealDamage": {
                  "cards": { "minBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
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
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.2")]
    [Fact]
    public void ARankedSelectorCanGainTheNewVillainStage()
    {
        // Rhino I is below Sandman's attack, while Rhino II ties it. The live
        // maximum therefore gains the new stage after the defeat; its damage
        // must be visible to the following move before the action is offered.
        var runner = RepeatedDynamicTargetRunner(
            """{ "query": "villain" }""",
            """{ "maxBy": { "of": { "query": "enemies" }, "by": "attack" } }""");
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Fact]
    public void DefeatingGuardCanMakeTheVillainADynamicDamageTarget()
    {
        // "The engaged player cannot attack any villain" only while Guard is
        // present. Defeating Hydra Mercenary makes Rhino attackable before the
        // next effect resolves, so the subsequent move has damage to carry.
        var runner = RepeatedDynamicTargetRunner(
            """{ "titled": "Hydra Mercenary" }""",
            """{ "query": "attackableEnemies" }""");
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability")]
    [Fact]
    public void DiscardedAttachmentStopsGrantingItsTraitDuringTheTrace()
    {
        // A constant ability remains active only while its card is in play.
        // Discarding Cosmic Flight removes AERIAL before the filtered damage,
        // so the wounded identity is no longer one of that effect's targets.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": { "titled": "Cosmic Flight" } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "characters" }, "trait": "AERIAL"
                  } },
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
                board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void DiscardedAttachmentKeepsItsLastingTraitDuringTheTrace()
    {
        // A lasting effect continues for its specified duration whether or not
        // its source remains in play. Discarding Rocket Boots therefore does
        // not remove the AERIAL it already granted until the phase ends.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": { "titled": "Rocket Boots" } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "characters" }, "trait": "AERIAL"
                  } },
                  "amount": 1
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

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                var boots = board.CreateCard(
                    "01039",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
                board.Effects.Register(new ContinuousEffect(
                    EffectSource.LastingEffect,
                    Traits.Granted + "AERIAL",
                    Card: boots.ObjectId,
                    Affects: board.Seats[0].IdentityCard.ObjectId,
                    Lasts: new Duration(Until: TimingPoints.EndOfPlayerPhase)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:attachment.1")]
    [Fact]
    public void DiscardedAttachmentStopsModifyingARankedField()
    {
        // An attachment may modify its attached character's ATK "as indicated
        // by the values in the associated fields on the attachment card."
        // Discarding Enhanced Ivory Horn therefore drops Rhino to Shocker's
        // ATK, so both are minimum targets and Rhino's point is available for
        // the lethal move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": { "titled": "Enhanced Ivory Horn" } },
                { "dealDamage": {
                  "cards": { "minBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
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

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    "01100",
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:traits.1")]
    [Fact]
    public void EarlierTraitGrantChangesALaterDynamicTargetSet()
    {
        // A granted trait is immediately part of what later card abilities
        // query. Granting Rhino AERIAL makes both it and the already-AERIAL
        // Vulture targets before the villain's point is moved to the hero.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "query": "villain" },
                  "trait": "AERIAL", "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "enemies" }, "trait": "AERIAL"
                  } },
                  "amount": 1
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

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "27163",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:traits.1")]
    [Fact]
    public void EarlierTraitGrantChangesAMinionOnlyTargetSet()
    {
        // Dynamic membership is not limited to sets that can contain the
        // villain. Giving Shocker AERIAL adds it to the later minion-only set,
        // so its damage is present for the move to the wounded hero.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Shocker" },
                  "trait": "AERIAL", "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "minions" }, "trait": "AERIAL"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Shocker" },
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

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "27163",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void EarlierNumericGrantChangesALaterRankedTargetSet()
    {
        // Rhino begins below Sandman's attack. The +2 ATK grant makes Rhino
        // the unique maximum before the ranked damage resolves, leaving a
        // point for the following move to carry to the wounded hero.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "query": "villain" },
                  "keyword": "attack", "amount": 2, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
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

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void RankedSelectorCanDropAnInitiallySelectedNonVillain()
    {
        // Sandman begins as the maximum attack enemy, but Ultron's next stage
        // exceeds him. The live maximum drops Sandman, so no damage is present
        // on him for the following move and the ability remains legal.
        var runner = RepeatedDynamicTargetRunner(
            """{ "query": "villain" }""",
            """{ "maxBy": { "of": { "query": "enemies" }, "by": "attack" } }""",
            """{ "titled": "Sandman" }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
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
    public void RankedPlayerCharacterSelectorRetainsItsControllerScope()
    {
        // The dynamic rank is over characters the resolving player controls,
        // not over enemies. Hulk is in that set and has the highest attack, so
        // its damage is available for the following move to Spider-Man.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "charactersYouControl" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Hulk" },
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

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01050",
                    board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:dash-value.3")]
    [Fact]
    public void ATraceLocalModifierCannotChangeADashPowerForRanking()
    {
        // A referenced dash is "treated as having a value of 0" and "cannot
        // be modified." Giving alter-ego Carol +5 ATK therefore leaves Hulk
        // as the maximum-ATK character and makes his damage available for the
        // lethal move that follows.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Carol Danvers" },
                  "keyword": "attack", "amount": 5, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "characters" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Hulk" },
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

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01050",
                    board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:enters-play")]
    [Rule("rr:toughness.1")]
    [Fact]
    public void AMinionPutIntoPlayJoinsLaterRankedTargetSets()
    {
        // A card enters play when it moves from an out-of-play area into play,
        // and Toughness gives it a tough status at that point. Sandman joins
        // the enemy set before the ranked damage: the first point consumes
        // tough, the second frame damages Sandman, and Rhino has none to move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "if": {
                  "test": { "not": { "titleInPlay": "Sandman" } },
                  "then": { "putIntoPlay": {
                    "card": { "cardsIn": {
                      "areas": [ "encounterDiscardPile" ], "title": "Sandman"
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
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Fact]
    public void AGuardMinionPutIntoPlayImmediatelyProtectsTheVillain()
    {
        // Guard means "the engaged player cannot attack any villain."
        // Putting Hydra Mercenary into play engaged with the resolving hero
        // therefore removes Rhino from attackableEnemies before damage lands.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
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
                    "01101",
                    board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

}
