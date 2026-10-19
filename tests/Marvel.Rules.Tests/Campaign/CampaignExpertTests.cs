using Marvel.Rules.Campaign;
using Xunit;

namespace Marvel.Rules.Tests.Campaign;

public sealed class CampaignExpertTests
{
    [Fact]
    public void DefeatedExpertSeatSkipsVictoryAndMustRecoverFromItsOwnPool()
    {
        // pack:mc10:persistent-damage: defeated player "does not participate in any of the victory steps".
        var state = CampaignRules.Start(CampaignFixture.Create(expert: true, seats: 2));
        var outcome = CampaignFixture.Outcome(state);
        outcome = outcome with { Seats = outcome.Seats.SetItem(0, outcome.Seats[0] with { Defeated = true, RemainingHitPoints = 0 }) };
        state = CampaignOutcomeRules.Finish(state, CampaignFixture.Definition, outcome);
        Assert.Equal(1, CampaignRewardRules.Choice(state, CampaignFixture.Definition)!.Seat);
        state = CampaignRewardRules.Choose(state, CampaignFixture.Definition, 1, "tech0");
        Assert.True(CampaignRules.Setup(state).Seats[0].RequiresRecovery);
        Assert.Empty(state.Players[0].Cards);
        state = CampaignRules.Start(state);
        var wrong = new CampaignGameProgress([], [new(0, state.Players[1].AvailableObligations[0])], null);
        Assert.Throws<ArgumentException>(() => CampaignProgressRules.Apply(state, wrong));
        var selected = state.Players[0].AvailableObligations[0];
        var progress = new CampaignGameProgress([], [new(0, selected)], null);
        state = CampaignProgressRules.Apply(state, progress);
        state = CampaignProgressRules.Apply(state, progress);
        Assert.Single(state.Players[0].Cards);
        Assert.Equal(3, state.Players[0].AvailableObligations.Length);
        Assert.False(state.Players[0].RequiresRecovery);
    }

    [Fact]
    public void ExpertHpIsCappedAndConditionRewardIsAppliedBeforeRecording()
    {
        // pack:mc10:persistent-damage and absorbing-man-campaign-instructions.victory.
        var state = CampaignRules.Start(CampaignFixture.Create(expert: true) with { Stage = CampaignStage.AbsorbingMan });
        var outcome = CampaignFixture.Outcome(state);
        state = CampaignOutcomeRules.Finish(state, CampaignFixture.Definition, outcome);
        state = CampaignRewardRules.Choose(state, CampaignFixture.Definition, 0, "condition3");
        Assert.Equal(10, state.Players[0].RecordedHitPoints);
        Assert.Equal(10, CampaignRules.Setup(state).Seats[0].RecordedHitPoints);
        Assert.Equal(4, state.Players[0].ConditionHitPointBonus);
        Assert.Equal(4, CampaignRules.Setup(state).Seats[0].ConditionHitPointBonus);
    }

    [Theory]
    [InlineData(false, 5)]
    [InlineData(true, 15)]
    public void FinalThreatAndEncounterCardsFollowCampaignDifficulty(bool expert, int threat)
    {
        var state = CampaignFixture.Create(expert, seats: 3) with { Stage = CampaignStage.RedSkull, DelayCounters = 5 };
        state = state with { Players = state.Players.SetItem(1, state.Players[1] with { EngagedWithEnemy = true }) };
        var setup = CampaignRules.Setup(state);
        Assert.Equal(threat, setup.AdditionalThreat);
        Assert.Equal(expert, setup.Seats[1].DealEncounterCard);
        Assert.False(setup.Seats[0].DealEncounterCard);
    }

    [Fact]
    public void PrisonBansAlliesAcrossRecipesAndRewardsAndPreventsConditionImprovement()
    {
        var state = CampaignFixture.Create() with { Stage = CampaignStage.Zola };
        var player = state.Players[0] with { Cards = [CampaignFixture.Definition.Conditions[0].Basic,
            new("captive", "captive0", CampaignCardRole.RescuedAlly)], Deck = new(["ordinary", "captive0"]) };
        state = CampaignRules.Start(state with { Players = state.Players.SetItem(0, player) });
        var outcome = CampaignFixture.Outcome(state) with { HydraPrisonInPlay = true, ImprisonedAllyFaces = ["captive0"] };
        state = CampaignOutcomeRules.Finish(state, CampaignFixture.Definition, outcome);
        Assert.Equal(CampaignStage.RedSkull, state.Stage);
        Assert.Contains("captive0", state.BannedAllyFaces);
        Assert.Contains("captive0", CampaignRules.Setup(state).BannedAllyFaces);
        Assert.Equal("basic0", Assert.Single(state.Players[0].Cards).Face);
        Assert.DoesNotContain("captive0", state.Players[0].Deck.Cards);
        Assert.Throws<ArgumentException>(() => CampaignRules.ReplaceDeck(state, 0, new(["captive0"])));
    }

    [Fact]
    public void OnlyHeroFormSeatsWithConditionMayImproveAfterLiberatingPrison()
    {
        var state = CampaignFixture.Create(seats: 2) with { Stage = CampaignStage.Zola };
        state = state with { Players = [.. state.Players.Select(player => player with { Cards = [CampaignFixture.Definition.Conditions[0].Basic] })] };
        state = CampaignRules.Start(state);
        var outcome = CampaignFixture.Outcome(state);
        outcome = outcome with { Seats = outcome.Seats.SetItem(1, outcome.Seats[1] with { HeroForm = false }) };
        state = CampaignOutcomeRules.Finish(state, CampaignFixture.Definition, outcome);
        Assert.Equal(0, CampaignRewardRules.Choice(state, CampaignFixture.Definition)!.Seat);
        state = CampaignRewardRules.Choose(state, CampaignFixture.Definition, 0, "condition0");
        Assert.Equal("improved0", Assert.Single(state.Players[0].Cards).Face);
        Assert.Equal("basic0", Assert.Single(state.Players[1].Cards).Face);
        Assert.Equal(CampaignStage.RedSkull, state.Stage);
    }
}
