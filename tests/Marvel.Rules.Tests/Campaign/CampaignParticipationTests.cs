using Marvel.Rules.Campaign;
using Xunit;
namespace Marvel.Rules.Tests.Campaign;

public sealed class CampaignParticipationTests
{
    [Fact]
    public void DefeatedPlayerCanSitOutWithoutChangingRosterOrScalingTheNextAttempt()
    {
        // pack:mc10:persistent-damage: a defeated player "can rejoin their teammates for the next scenario".
        var state = CampaignFixture.Create(expert: true, seats: 3) with { Stage = CampaignStage.RedSkull, DelayCounters = 2 };
        state = state with { Players = state.Players.SetItem(1, state.Players[1] with
            { RecordedHitPoints = 0, RequiresRecovery = true }) };
        state = CampaignParticipationRules.Select(state, 1, false);
        var setup = CampaignRules.Setup(state);
        Assert.Equal(3, state.Players.Length);
        Assert.Equal<int>([0, 1], setup.Seats.Select(seat => seat.Seat));
        Assert.Equal<int?>([0, 2], setup.Seats.Select(seat => seat.CampaignSeat));
        Assert.Equal(4, setup.AdditionalThreat);
        Assert.False(state.Players[1].Participating);
        Assert.Throws<InvalidOperationException>(() => CampaignParticipationRules.Select(state, 0, false));
    }

    [Fact]
    public void EmptyRecoveryPoolAndEmptyAttemptAreExplainedBeforeGameAdmission()
    {
        var state = CampaignFixture.Create(expert: true);
        state = state with { Players = state.Players.SetItem(0, state.Players[0] with
            { RequiresRecovery = true, RecordedHitPoints = 0, AvailableObligations = [] }) };
        Assert.Contains("no recovery obligations", Assert.Single(CampaignPreparationRules.Problems(state)), StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => CampaignRules.Start(state));
        state = CampaignParticipationRules.Select(state, 0, false);
        Assert.Contains("at least one player", Assert.Single(CampaignPreparationRules.Problems(state)), StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => CampaignRules.Setup(state));
    }

    [Fact]
    public void VictoryRewardsOnlyParticipatingSeatsAndRetainsAbsentPlayerRecoveryState()
    {
        var state = CampaignFixture.Create(expert: true, seats: 3) with { Stage = CampaignStage.AbsorbingMan };
        state = state with { Players = state.Players.SetItem(1, state.Players[1] with
            { RecordedHitPoints = 0, RequiresRecovery = true, Participating = false }) };
        state = CampaignRules.Start(state);
        var outcome = CampaignFixture.Outcome(state);
        outcome = outcome with { Seats = [.. outcome.Seats.Where(seat => seat.Seat != 1)] };
        state = CampaignOutcomeRules.Finish(state, CampaignFixture.Definition, outcome);
        Assert.Equal<int>([0, 2], state.RewardSeats);
        state = CampaignRewardRules.Choose(state, CampaignFixture.Definition, 0, null);
        state = CampaignRewardRules.Choose(state, CampaignFixture.Definition, 2, null);
        Assert.True(state.Players[1].RequiresRecovery);
        Assert.Equal(0, state.Players[1].RecordedHitPoints);
        state = CampaignParticipationRules.Select(state, 1, true);
        Assert.Equal(3, CampaignRules.Setup(state).Seats.Length);
    }
}
