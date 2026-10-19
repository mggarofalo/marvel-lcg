using System.Collections.Immutable;
using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>Actual scenario setup from controlled campaign records; not a five-game victory claim.</summary>
public sealed class RiseOfRedSkullSetupTests
{
    public static TheoryData<CampaignStage, bool, bool> Modes
    {
        get
        {
            var cases = new TheoryData<CampaignStage, bool, bool>();
            foreach (var stage in Enum.GetValues<CampaignStage>())
            foreach (bool campaign in new[] { false, true })
            foreach (bool scenario in new[] { false, true }) cases.Add(stage, campaign, scenario);
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void ActualScenarioRestoresCampaignHealthBeforeOpeningHands(
        CampaignStage stage, bool expertCampaign, bool expertScenario)
    {
        // pack:mc10:persistent-damage and scenario expert campaign setup: "Set each player's hit points to the value recorded in the campaign log."
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var definition = Assert.IsType<CampaignDefinition>(factory.CampaignDefinition);
        string[] heroes = ["spider_man", "captain_marvel"];
        var catalog = factory.DiscoverDecks();
        var roster = heroes.Select(hero => new CampaignRosterEntry(hero,
            new PlayerDeckRecipe(catalog.Heroes.Single(candidate => candidate.Key == hero).Starter),
            factory.CampaignBaseHitPoints(hero))).ToArray();
        var state = CampaignRules.Create(definition, roster, expertCampaign, 42) with
            { Stage = stage, Attempt = (int)stage, ExpertScenario = expertScenario };
        bool hasRecordedHealth = expertCampaign && stage != CampaignStage.Crossbones;
        if (hasRecordedHealth) state = state with
            { Players = [.. state.Players.Select(player => player with { RecordedHitPoints = 7 })] };
        var scenario = definition.Scenarios[(int)stage];
        var specification = new GameSpecification(expertScenario ? scenario.Expert : scenario.Standard,
            heroes, null, 42, [.. roster.Select(player => player.Deck)], CampaignRules.Setup(state));
        var opened = factory.Create(specification);
        int recovered = DeclineOptionalRecovery(opened.Game);
        Assert.Equal(hasRecordedHealth ? 2 : 0, recovered);
        Assert.Equal(GamePhase.Mulligan, opened.Game.Phase);
        Assert.Equal(0, opened.Game.Pending!.Player);
        for (int seat = 0; seat < roster.Length; seat++)
            Assert.Equal(hasRecordedHealth ? roster[seat].BaseHitPoints - 7 : 0,
                opened.Game.State.Seats[seat].IdentityCard.Damage);
        Assert.NotNull(opened.Game.State.Campaign);
        Assert.Empty(factory.InspectCampaign(opened.Game).Obligations);
    }

    private static int DeclineOptionalRecovery(Game game)
    {
        int count = 0;
        int decisions = 0;
        while (game.Phase == GamePhase.ScenarioSetup)
        {
            Assert.True(decisions++ < 20, "Setup must make bounded progress");
            var prompt = Assert.IsType<Marvel.Rules.Prompts.Prompt>(game.Pending);
            if (prompt.Trigger == "CampaignRecovery") { Assert.True(prompt.Cancellable); count++; }
            game.Resolve(CampaignSetupAnswers.Next(prompt));
        }
        return count;
    }
}
