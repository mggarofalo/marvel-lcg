using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>Hosted setup from controlled campaign records, not played victory evidence.</summary>
public sealed class CampaignInitialHistoryTests : EngineHostTestBase
{
    private static readonly string[] Heroes = ["spider_man", "captain_marvel"];

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public void InitialRecoveryPersistsAndReplaysThroughAllSeats(bool recover, int seats)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var specification = Specification(factory, seats);
        var response = host.Exchange(EngineRequest.OpenGame("open", "recovery", specification));
        Assert.Null(response.Error);
        string capability = Assert.IsType<string>(response.Capability);
        int recoveries = 0;
        int decisions = 0;
        while (response.Prompt!.PublicKind != Marvel.Rules.Prompts.PublicDecisionKind.OpeningHand)
        {
            Assert.True(decisions++ < 20);
            var prompt = response.Prompt;
            bool recovery = prompt.Trigger == "CampaignRecovery";
            if (recovery) recoveries++;
            var offered = prompt.Affordances.First(option => option.IsLegal);
            var decision = recovery && !recover ? Decision.Decline
                : new Decision(offered.Id, offered.Targets is { } targets ? [.. targets.Legal.Take(targets.Min)] : []);
            response = host.Exchange(EngineRequest.ResolveGame("resolve-" + decisions,
                "recovery", capability, decision, response.Revision));
            Assert.Null(response.Error);
            var saved = Assert.Single(store.Load()).Save!;
            Assert.Single(saved.Units);
            Assert.Equal(decisions, saved.Units[0].Decisions.Count);
            host = new EngineHost(factory, store: store);
            var resumed = host.Exchange(EngineRequest.SyncGame("resume", "recovery", capability));
            Assert.Null(resumed.Error);
            Assert.Equal(response.Revision, resumed.Revision);
            Assert.Equal(JsonSerializer.Serialize(response.Prompt), JsonSerializer.Serialize(resumed.Prompt));
            response = resumed;
        }
        Assert.Equal(seats, recoveries);
        Assert.Equal("complete", Assert.Single(Assert.Single(store.Load()).Save!.Units).Status);
        // Recovery samples a hidden obligation; declining preserves the information frontier.
        Assert.Equal(recover ? 1 : 0, Assert.Single(store.Load()).Save!.EditFrontier);
        var undo = host.Exchange(EngineRequest.UndoGame("undo", "recovery", capability, 0, response.Revision));
        if (recover || seats > 1)
        {
            Assert.NotNull(undo.Error);
            if (!recover) Assert.Equal("history_authority", undo.Error.Code);
            Assert.Equal(response.Revision, Assert.Single(store.Load()).Save!.Revision);
        }
        else
        {
            Assert.Null(undo.Error);
            Assert.Equal("CampaignRecovery", undo.Prompt!.Trigger);
            var redo = host.Exchange(EngineRequest.RedoGame("redo", "recovery", capability, 1, undo.Revision));
            Assert.Null(redo.Error);
            Assert.Equal(JsonSerializer.Serialize(response.Prompt), JsonSerializer.Serialize(redo.Prompt));
            var rewound = host.Exchange(EngineRequest.UndoGame("rewind", "recovery", capability, 0, redo.Revision));
            Assert.Null(rewound.Error);
            var replaced = host.Exchange(EngineRequest.ResolveGame("replace", "recovery", capability,
                Decision.Decline, rewound.Revision));
            Assert.Null(replaced.Error);
            Assert.Single(Assert.Single(store.Load()).Save!.Units);
            Assert.Equal(JsonSerializer.Serialize(response.Prompt), JsonSerializer.Serialize(replaced.Prompt));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedInitialRecoveryCommitPreservesTheOriginalPromptAndCanBeRetried(bool recover)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var memory = new MemorySessionStore();
        var host = new EngineHost(factory, store: new FailingSessionStore(memory, 2));
        var opened = host.Exchange(EngineRequest.OpenGame("open", "recovery", Specification(factory, 1)));
        Assert.Null(opened.Error);
        var decision = recover ? new Decision(opened.Prompt!.Affordances[0].Id, []) : Decision.Decline;
        var request = EngineRequest.ResolveGame("resolve", "recovery", opened.Capability!, decision, opened.Revision);
        Assert.Equal("save_failed", host.Exchange(request).Error!.Code);
        Assert.Empty(Assert.Single(memory.Load()).Save!.Units);
        var current = host.Exchange(EngineRequest.SyncGame("sync", "recovery", opened.Capability!));
        Assert.Null(current.Error);
        Assert.Equal(opened.Revision, current.Revision);
        Assert.Equal(JsonSerializer.Serialize(opened.Prompt), JsonSerializer.Serialize(current.Prompt));
        var retried = host.Exchange(request);
        Assert.Null(retried.Error);
        Assert.Equal(1, retried.Revision);
        Assert.Single(Assert.Single(memory.Load()).Save!.Units);
    }

    private static GameSpecification Specification(DatasetGameFactory factory, int seats)
    {
        var definition = Assert.IsType<CampaignDefinition>(factory.CampaignDefinition);
        string[] heroes = [.. Heroes.Take(seats)];
        var catalog = factory.DiscoverDecks();
        var roster = heroes.Select(hero => new CampaignRosterEntry(hero,
            new PlayerDeckRecipe(catalog.Heroes.Single(candidate => candidate.Key == hero).Starter),
            factory.CampaignBaseHitPoints(hero))).ToArray();
        var state = CampaignRules.Create(definition, roster, true, 42) with
            { Stage = CampaignStage.AbsorbingMan, Attempt = 1 };
        state = state with { Players = [.. state.Players.Select(player => player with { RecordedHitPoints = 7 })] };
        return new(definition.Scenarios[1].Standard, heroes, null, 42,
            [.. roster.Select(player => player.Deck)], CampaignRules.Setup(state));
    }
}




