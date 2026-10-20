using Marvel.Core.Random;
using Marvel.Rules.Campaign;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Campaign;

public sealed class CampaignRecoveryProcedureTests
{
    [Fact]
    [Rule("rr:appendix-ii-setup.step.13")]
    [Rule("rr:appendix-ii-setup.step.14")]
    public void RecordedHealthAndOptionalRecoveryPrecedeOpeningHands()
    {
        // Step13 resolves campaign setup; step14 draws opening cards afterward.
        // pack:mc10:absorbing-man-campaign-instructions: set recorded HP; each player may recover.
        var game = CampaignRecoveryFixture.Start(CampaignRecoveryFixture.Setup());
        Assert.Equal(GamePhase.ScenarioSetup, game.Phase);
        Assert.Empty(game.State.Seats[0].Hand.Cards);
        Assert.Equal(5, game.State.Seats[0].IdentityCard.Damage);
        Assert.True(game.Pending!.Cancellable);
        long rng = game.State.Random.Generator.WordsConsumed;
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.Mulligan, game.Phase);
        Assert.Equal(3, game.State.Seats[0].Hand.Cards.Count);
        Assert.Equal(rng, game.State.Random.Generator.WordsConsumed);
        Assert.DoesNotContain(game.State.Campaign!.Cards, progress => progress.Assigned);
    }

    [Fact]
    [Rule("rr:hit-points.2.3")]
    public void RecoveryUsesModifiedMaximumAndRecordsOneSeededObligation()
    {
        // +X hit points increases maximum and remaining HP. Campaign recovery heals to full.
        var first = CampaignRecoveryFixture.Start(CampaignRecoveryFixture.Setup(), healthBonus: true);
        var second = CampaignRecoveryFixture.Start(CampaignRecoveryFixture.Setup(), healthBonus: true);
        Assert.Equal(8, first.State.Seats[0].IdentityCard.Damage);
        Assert.Contains("13 HP", Assert.Single(first.Pending!.Affordances).Description);
        var result = first.Resolve(Decision.Take(0));
        second.Resolve(Decision.Take(0));
        Assert.Equal(0, first.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(13, DamagePlacement.Health(first.State, first.State.Facts, first.State.Seats[0].IdentityCard));
        var assigned = Assert.Single(first.State.Campaign!.Cards, value => value.Assigned);
        Assert.Equal(assigned.Binding.Card.Key, Assert.Single(second.State.Campaign!.Cards, value => value.Assigned).Binding.Card.Key);
        Assert.Equal(first.State.Digest().Canonical(), second.State.Digest().Canonical());
        Assert.Contains(result.Events.OfType<FieldSet>(), value => value.Field == "campaign_assigned" && value.To == 1);
        Assert.Equal(GamePhase.Mulligan, first.Phase);
    }

    [Fact]
    public void DefeatedSeatCannotDeclineRecovery()
    {
        // pack:mc10:persistent-damage: a defeated player rejoins by adding an obligation and recovering.
        var game = CampaignRecoveryFixture.Start(CampaignRecoveryFixture.Setup(hp: 0, required: true));
        Assert.False(game.Pending!.Cancellable);
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Decline));
        Assert.Empty(game.State.Seats[0].Hand.Cards);
        game.Resolve(Decision.Take(0));
        Assert.Equal(0, game.State.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(false, CampaignStage.AbsorbingMan)]
    [InlineData(true, CampaignStage.Crossbones)]
    public void StandardAndFirstScenarioDoNotOfferExpertRecovery(bool expert, CampaignStage stage)
    {
        // mc10 expert-only instructions are ignored in standard; Crossbones has no recovery setup.
        var game = CampaignRecoveryFixture.Start(CampaignRecoveryFixture.Setup(expert, stage));
        Assert.Equal(GamePhase.Mulligan, game.Phase);
        Assert.Equal(0, game.State.Seats[0].IdentityCard.Damage);
        Assert.DoesNotContain(game.State.Campaign!.Cards, value => value.Assigned);
    }

    [Fact]
    public void NoAvailableObligationsSkipsOptionalChoiceAndRejectsRequiredRecovery()
    {
        var game = CampaignRecoveryFixture.Start(CampaignRecoveryFixture.Setup(available: false));
        Assert.Equal(GamePhase.Mulligan, game.Phase);
        Assert.Equal(5, game.State.Seats[0].IdentityCard.Damage);
        Assert.Throws<RulesNotImplementedException>(() => CampaignRecoveryFixture.Start(
            CampaignRecoveryFixture.Setup(hp: 0, required: true, available: false)));
    }

    [Fact]
    public void RecoveryConsumesTheSingleSeededStreamForChoiceThenDeckShuffle()
    {
        var game = CampaignRecoveryFixture.Start(CampaignRecoveryFixture.Setup());
        World world = game.State;
        var oracle = new MersenneTwister(0);
        oracle.SetState(world.Random.Generator.GetState());
        long before = world.Random.Generator.WordsConsumed;
        int[] candidates = [.. world.Campaign!.Cards.OrderBy(value => value.Binding.ObjectId).Select(value => value.Binding.ObjectId)];
        int expected = oracle.Choice(candidates);
        var deck = world.Seats[0].Deck.Cards.Select(card => card.ObjectId).ToList();
        deck.Add(expected);
        oracle.Shuffle(deck);
        game.Resolve(Decision.Take(0));
        Assert.Equal(expected, Assert.Single(world.Campaign.Cards, value => value.Assigned).Binding.ObjectId);
        Assert.Equal(before + oracle.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(deck.Take(deck.Count - 3), world.Seats[0].Deck.Cards.Select(card => card.ObjectId));
    }

    [Fact]
    public void InvalidRecoveryAnswerChangesNeitherStateNorRandomStream()
    {
        var game = CampaignRecoveryFixture.Start(CampaignRecoveryFixture.Setup());
        string digest = game.State.Digest().Canonical();
        long words = game.State.Random.Generator.WordsConsumed;
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(23)));
        Assert.Equal(digest, game.State.Digest().Canonical());
        Assert.Equal(words, game.State.Random.Generator.WordsConsumed);
        Assert.Equal(GamePhase.ScenarioSetup, game.Phase);
    }

    [Fact]
    public void RecordedWeaponsThreatAndEncounterPenaltyApplyOnce()
    {
        // pack:mc10:red-skull-campaign-instructions#setup: shuffle recorded Experimental
        // attachments, add recorded delay threat, recover, then deal recorded encounter cards.
        var setup = CampaignRecoveryFixture.Setup(stage: CampaignStage.RedSkull);
        setup = setup with { ExperimentalWeapons = ["weapon"], AdditionalThreat = 4,
            Seats = [setup.Seats[0] with { DealEncounterCard = true }] };
        var game = CampaignRecoveryFixture.Start(setup);
        var world = game.State;
        Assert.Equal(DeckType.EncounterDeck, world.Cards.Single(card => card.FaceId == "weapon").Area.Type);
        Assert.Equal(5, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        game.Resolve(Decision.Decline);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(5, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }
    [Fact]
    [Rule("rr:appendix-ii-setup.step.13")]
    public void EachSeatFinishesRecoveryBeforeAnyOpeningHandOrEncounterPenalty()
    {
        var setup = CampaignRecoveryFixture.Setup(stage: CampaignStage.RedSkull);
        setup = setup with { Seats = [setup.Seats[0] with { DealEncounterCard = true },
            setup.Seats[0] with { Seat = 1, RecordedHitPoints = 2, DealEncounterCard = true,
                AvailableObligations = [.. setup.Seats[0].AvailableObligations.Select(card => card with { Key = $"1-{card.Key}" })] }] };
        var game = CampaignRecoveryFixture.Start(setup);
        var world = game.State;
        int[] order = world.PlayerOrder.ToArray();
        Assert.Equal(order[0], game.Pending!.Player);
        game.Resolve(Decision.Decline);
        Assert.Equal(order[1], game.Pending!.Player);
        Assert.All(world.Seats, seat => Assert.Empty(seat.Hand.Cards));
        Assert.All(world.Seats, seat => Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat.Index)).Cards));
        game.Resolve(Decision.Take(0));
        Assert.Equal(GamePhase.Mulligan, game.Phase);
        Assert.All(world.Seats, seat => Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat.Index)).Cards));
        Assert.Equal(order[1], Assert.Single(world.Campaign!.Cards, value => value.Assigned).Binding.Seat);
        Assert.Equal(0, world.Seats[order[1]].IdentityCard.Damage);
        Assert.DoesNotContain(world.Campaign.Cards, value => value.Assigned && value.Binding.Seat == order[0]);
    }

    [Fact]
    [Rule("rr:appendix-ii-setup.step.13")]
    public void CarriedHealthAddsOnlyPrintedConditionBonusAndCapsAtLiveMaximum()
    {
        // ruling:1516351f87fc4382: record base HP, then add the campaign
        // upgrade's HP at the beginning of the next scenario.
        var setup = CampaignRecoveryFixture.Setup();
        setup = setup with { Seats = [setup.Seats[0] with { ConditionHitPointBonus = 2 }] };
        var game = CampaignRecoveryFixture.Start(setup, healthBonus: true);
        Assert.Equal(6, game.State.Seats[0].IdentityCard.Damage); // max13 - (recorded5 + Condition2)
        game.Resolve(Decision.Decline);
        Assert.Equal(6, game.State.Seats[0].IdentityCard.Damage);
        setup = setup with { Seats = [setup.Seats[0] with { RecordedHitPoints = 10, ConditionHitPointBonus = 4 }] };
        game = CampaignRecoveryFixture.Start(setup, healthBonus: true);
        Assert.Equal(0, game.State.Seats[0].IdentityCard.Damage); // capped13 rather than14
    }

    [Fact]
    [Rule("rr:main-scheme-main-scheme-deck.2.1")]
    public void CampaignThreatCompletingFinalSchemeEndsSetupBeforeRecoveryOrHands()
    {
        // If the villain completes the final stage, the villain wins the game.
        var setup = CampaignRecoveryFixture.Setup(stage: CampaignStage.RedSkull) with { AdditionalThreat = 98 };
        var game = CampaignRecoveryFixture.Start(setup);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.Null(game.Pending);
        Assert.Equal(Outcome.VillainWins, game.State.Result);
        Assert.Equal(1, game.State.TheCardIn(DeckType.MainSchemesArea)!.Tokens["is_completed"]);
        Assert.Empty(game.State.Seats[0].Hand.Cards);
        Assert.DoesNotContain(game.State.Campaign!.Cards, value => value.Assigned);
    }

}
