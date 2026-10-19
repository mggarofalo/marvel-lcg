using Marvel.Rules.Campaign;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Campaign;

public sealed class CampaignLifecycleTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:modes-of-play.11")]
    public void ScenarioDifficultyDoesNotRewriteCampaignDifficulty(bool campaignExpert, bool scenarioExpert)
    {
        // "The difficulty of the campaign mode does not require players to play any other specific game mode."
        var state = CampaignRules.SelectScenarioMode(CampaignFixture.Create(campaignExpert), scenarioExpert);
        Assert.Equal(campaignExpert, state.Expert);
        Assert.Equal(scenarioExpert, state.ExpertScenario);
        Assert.Equal(campaignExpert, CampaignRules.Setup(state).ExpertCampaign);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void AllFiveWinsAreRequiredAndEachPhysicalRewardHasOnlyOneOwner(int seats)
    {
        // pack:mc10:campaign-mode-rules: "win all five scenarios in numerical order".
        var state = CampaignFixture.Create(seats: seats);
        for (int stage = 0; stage < 5; stage++)
        {
            Assert.Equal((CampaignStage)stage, state.Stage);
            Assert.Equal(CampaignPhase.Preparing, state.Phase);
            state = CampaignFixture.Win(state);
        }
        Assert.Equal(CampaignPhase.Complete, state.Phase);
        var rewards = state.Players.SelectMany(player => player.Cards).ToArray();
        Assert.Equal(seats * 2, rewards.Length);
        Assert.Equal(rewards.Length, rewards.Select(card => card.Key).Distinct().Count());
        Assert.Throws<InvalidOperationException>(() => CampaignRules.Retry(state));
    }

    [Theory]
    [InlineData(false, CampaignStage.RedSkull, CampaignPhase.Retry)]
    [InlineData(true, CampaignStage.Zola, CampaignPhase.Retry)]
    [InlineData(true, CampaignStage.RedSkull, CampaignPhase.Failed)]
    public void OnlyExpertFinalLossEndsTheCampaign(bool expert, CampaignStage stage, CampaignPhase expected)
    {
        // pack:mc10:red-skull-campaign-instructions.setup: "If the players lose this game ... lose the campaign."
        var state = CampaignRules.Start(CampaignFixture.Create(expert) with { Stage = stage });
        state = CampaignOutcomeRules.Finish(state, CampaignFixture.Definition, CampaignFixture.Outcome(state, won: false));
        Assert.Equal(expected, state.Phase);
        Assert.Empty(state.Players[0].Cards);
    }

    [Fact]
    [Rule("rr:modes-of-play.10")]
    public void ConsumedTechRemainsRemovedAfterLossAndRetry()
    {
        // "even if players retry the scenario wherein that card was removed."
        var state = CampaignRules.Start(CampaignFixture.Win(CampaignFixture.Create()));
        var tech = Assert.Single(state.Players[0].Cards);
        var progress = new CampaignGameProgress([new(0, tech.Key)], [], null);
        state = CampaignProgressRules.Apply(state, progress);
        state = CampaignProgressRules.Apply(state, progress);
        Assert.Single(state.RemovedCardKeys);
        state = CampaignOutcomeRules.Finish(state, CampaignFixture.Definition, CampaignFixture.Outcome(state, won: false));
        state = CampaignRules.Retry(state);
        Assert.Empty(CampaignRules.Setup(state).Seats[0].Cards);
        Assert.Contains(tech.Key, state.RemovedCardKeys);
        Assert.Equal(CampaignStage.AbsorbingMan, state.Stage);
    }

    [Fact]
    public void RewardChoicesEnforceSeatAndOptionalityAndDoNotAdvancePrematurely()
    {
        var state = CampaignRules.Start(CampaignFixture.Create(seats: 2));
        state = CampaignOutcomeRules.Finish(state, CampaignFixture.Definition, CampaignFixture.Outcome(state));
        Assert.Throws<ArgumentException>(() => CampaignRewardRules.Choose(state, CampaignFixture.Definition, 1, "tech0"));
        Assert.Throws<ArgumentException>(() => CampaignRewardRules.Choose(state, CampaignFixture.Definition, 0, null));
        state = CampaignRewardRules.Choose(state, CampaignFixture.Definition, 0, "tech0");
        Assert.Equal(CampaignPhase.Rewards, state.Phase);
        Assert.DoesNotContain(CampaignRewardRules.Choice(state, CampaignFixture.Definition)!.Options, card => card.Key == "tech0");
        state = CampaignRewardRules.Choose(state, CampaignFixture.Definition, 1, "tech1");
        state = CampaignRules.Start(state);
        state = CampaignOutcomeRules.Finish(state, CampaignFixture.Definition, CampaignFixture.Outcome(state));
        state = CampaignRewardRules.Choose(state, CampaignFixture.Definition, 0, null);
        state = CampaignRewardRules.Choose(state, CampaignFixture.Definition, 1, null);
        Assert.Equal(CampaignStage.Taskmaster, state.Stage);
    }

    [Fact]
    public void PreparingDeckEditPreservesHeroAndCannotRunDuringAnAttempt()
    {
        var state = CampaignFixture.Create();
        var replacement = new PlayerDeckRecipe(["replacement"]);
        state = CampaignRules.ReplaceDeck(state, 0, replacement);
        Assert.Equal("hero0", state.Players[0].Hero);
        Assert.Equal("replacement", Assert.Single(state.Players[0].Deck.Cards));
        Assert.Throws<InvalidOperationException>(() => CampaignRules.ReplaceDeck(CampaignRules.Start(state), 0, replacement));
    }
}
