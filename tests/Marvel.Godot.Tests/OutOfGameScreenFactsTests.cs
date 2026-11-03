using System.Collections.Immutable;
using Marvel.Client;
using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Rules.Setup;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>The setup, join and campaign screens state their facts as chips and trackers, not sentences.</summary>
public sealed class OutOfGameScreenFactsTests
{
    [Fact]
    public void TheJoinTrackerFillsOnePipPerProvidedInputAndNamesOnlyTheMissingOnes()
    {
        TrackerState none = SetupJoinReadiness.State(SetupJoinReadiness.Provided("", "", ""));
        Assert.Equal((0, 3), (none.Filled, none.Total));
        Assert.Equal("Still needed: Engine endpoint, Game label, Seat invitation.", none.Tooltip);

        TrackerState partial = SetupJoinReadiness.State(SetupJoinReadiness.Provided("tcp://host:1", "", "secret"));
        Assert.Equal(2, partial.Filled);
        Assert.Equal("Still needed: Game label.", partial.Tooltip);

        TrackerState ready = SetupJoinReadiness.State(SetupJoinReadiness.Provided("tcp://host:1", "table", "secret"));
        Assert.Equal(3, ready.Filled);
        Assert.Equal("Ready to join.", ready.Tooltip);
    }

    [Fact]
    public void ASeatsDeckChipNamesTheChosenDeckOrTheStarter()
    {
        Assert.Equal("1 · Starter", SetupWording.DeckSeat(0, null).Label);
        Assert.Equal("2 · Aggro Cap", SetupWording.DeckSeat(1, "Aggro Cap").Label);
        Assert.Contains("seat 2", SetupWording.Invitation(1).Tooltip, StringComparison.Ordinal);
        Assert.Equal("Seat 2 invitation", SetupWording.Invitation(1).Label);
    }

    [Theory]
    [InlineData(CampaignStage.Crossbones, 1)]
    [InlineData(CampaignStage.Zola, 4)]
    [InlineData(CampaignStage.RedSkull, 5)]
    public void TheScenarioTrackerFillsThroughTheCurrentScenario(CampaignStage stage, int filled)
    {
        TrackerState tracker = CampaignWording.StageTracker(stage);
        Assert.Equal((filled, 5), (tracker.Filled, tracker.Total));
        Assert.Equal(CampaignWording.Stage(stage), tracker.Label);
        Assert.Contains("[" + CampaignWording.Stage(stage) + "]", tracker.Tooltip, StringComparison.Ordinal);
    }

    [Fact]
    public void PreparingLeavesScenarioDifficultyToItsToggle()
    {
        string[] preparing = Labels(CampaignPresentation.Facts(Campaign(State(CampaignPhase.Preparing))));
        Assert.Equal(["Preparing", "Standard campaign"], preparing);

        string[] playing = Labels(CampaignPresentation.Facts(Campaign(State(CampaignPhase.Playing) with
            { Expert = true, ExpertScenario = true, Attempt = 2, DelayCounters = 3 })));
        Assert.Equal(["Playing", "Expert campaign", "Expert scenario", "Attempt 2", "Delay 3"], playing);
    }

    [Fact]
    public void AnArchivedCampaignSaysSoFirstAndStatesItsScenarioDifficulty()
    {
        string[] facts = Labels(CampaignPresentation.Facts(Campaign(State(CampaignPhase.Preparing)) with { Archived = true }));
        Assert.Equal(["Archived", "Preparing", "Standard campaign", "Standard scenario"], facts);
    }

    [Fact]
    public void ASeatShowsHitPointsOnlyInAnExpertCampaignAndFlagsRecoveryAndSittingOut()
    {
        CampaignPlayerState player = Player() with { RecordedHitPoints = 7, RequiresRecovery = true, Participating = false };
        Assert.Equal(["Recovery", "Sitting out"], Labels(CampaignPlayerRow.Facts(player, State(CampaignPhase.Preparing))));
        Assert.Equal(["7/10", "Recovery", "Sitting out"],
            Labels(CampaignPlayerRow.Facts(player, State(CampaignPhase.Preparing) with { Expert = true })));
        Assert.Empty(CampaignPlayerRow.Facts(Player(), State(CampaignPhase.Preparing) with { Expert = true }));
    }

    [Fact]
    public void PreparationProblemsAreCountedWithEachCorrectionWholeOnHover()
    {
        Wording problems = CampaignWording.Problems(["Choose at least one player for this attempt.", "Player 2: deck too small."]);
        Assert.Equal("To fix 2", problems.Label);
        Assert.Equal("Choose at least one player for this attempt.\nPlayer 2: deck too small.", problems.Tooltip);
    }

    [Fact]
    public void ASavedCampaignSummarizesItsScenarioAndPhase()
    {
        var connection = new CampaignConnection("local", "", "Friday heroes", "id", "owner", null, null)
            { Snapshot = Campaign(State(CampaignPhase.Rewards) with { Stage = CampaignStage.Taskmaster, Expert = true }) };
        Assert.Equal(["Taskmaster", "Rewards", "Expert campaign"], Labels(CampaignLibrary.Summary(connection)));
        Assert.Equal([CampaignWording.NotLoaded], CampaignLibrary.Summary(connection with { Snapshot = null }));
    }

    private static string[] Labels(IEnumerable<Wording> facts) => [.. facts.Select(fact => fact.Label)];

    private static CampaignDescriptor Campaign(CampaignState state) => new("id", 1, state, null, null) { Name = "Friday heroes" };

    private static CampaignPlayerState Player() =>
        new("spider_man", new PlayerDeckRecipe(["01002"]), 10, null, false, [], [], false);

    private static CampaignState State(CampaignPhase phase) =>
        new("rise_of_red_skull", 42, false, CampaignStage.Crossbones, phase, 1, false, [Player()],
            ImmutableArray<string>.Empty, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty, 0,
            ImmutableArray<int>.Empty, null);
}
