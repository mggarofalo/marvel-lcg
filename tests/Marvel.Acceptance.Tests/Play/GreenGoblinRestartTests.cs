using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class GreenGoblinRestartTests
{
    [Theory]
    [InlineData("risky_business", 1u)]
    [InlineData("risky_business_expert", 2u)]
    [InlineData("mutagen_formula", 3u)]
    [InlineData("mutagen_formula_expert", 4u)]
    public void PublicGameSurvivesRestartDuringDependentChoicesWithExactAuthority(
        string scenario, uint seed)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var specification = new GameSpecification(scenario, ["spider_man"], null, seed);
        var control = factory.Create(specification).Game;
        var policy = new CoreGamePolicy(control.State.Facts);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var response = host.Exchange(EngineRequest.OpenGame("open", "goblin", specification));
        Assert.Null(response.Error);
        string capability = response.Capability!;
        int dependentRestarts = 0;
        for (int index = 0; control.Pending is not null && index < 100; index++)
        {
            Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(control.Pending)),
                JsonSerializer.Serialize(PromptRecord.From(response.Prompt!)));
            var decision = policy.Answer(control);
            var expectedPrompt = PromptRecord.From(control.Pending);
            var expectedDecision = DurableDecision.From(control.Pending.Player, control.Pending, decision);
            var result = control.Resolve(decision);
            response = host.Exchange(EngineRequest.ResolveGame($"answer-{index}", "goblin",
                capability, decision, response.Revision));
            Assert.Null(response.Error);
            var saved = Assert.Single(store.Load());
            var step = saved.Save.Units.SelectMany(unit => unit.Decisions).Last();
            Assert.Equal(JsonSerializer.Serialize(expectedPrompt), JsonSerializer.Serialize(step.Prompt));
            Assert.Equal(JsonSerializer.Serialize(expectedDecision), JsonSerializer.Serialize(step.Decision));
            Assert.Equal(control.State.Digest().Fingerprint(), step.StateFingerprint);
            Assert.Equal(control.State.Random.Generator.WordsConsumed, step.RngWords);
            Assert.Equal(result.Events.Select(JournalJson.Event).Select(item => item.GetRawText()),
                step.Events.Select(item => item.GetRawText()));
            if (control.Pending?.Asking is Question.Element or Question.Option or Question.Defender)
            {
                host = Restart(factory, store, host, capability);
                response = host.Exchange(EngineRequest.SyncGame("sync", "goblin", capability));
                dependentRestarts++;
            }
        }
        Assert.True(dependentRestarts > 0);
        Assert.True(policy.CardsPlayed > 0);
        Assert.True(policy.PlayerAttacks > 0);
        Assert.True(control.Round > 1);
    }

    private static EngineHost Restart(
        DatasetGameFactory factory, MemorySessionStore store, EngineHost host, string capability)
    {
        var before = host.Exchange(EngineRequest.SyncGame("sync", "goblin", capability));
        string authority = JsonSerializer.Serialize(Assert.Single(store.Load()));
        var restarted = new EngineHost(factory, store: store);
        var after = restarted.Exchange(EngineRequest.SyncGame("sync", "goblin", capability));
        Assert.Null(after.Error);
        Assert.Equal(EngineJson.Write(before), EngineJson.Write(after));
        Assert.Equal(authority, JsonSerializer.Serialize(Assert.Single(store.Load())));
        var rejected = restarted.Exchange(EngineRequest.ResolveGame("invalid", "goblin",
            capability, Decision.Take(int.MaxValue), after.Revision));
        Assert.NotNull(rejected.Error);
        Assert.Equal(authority, JsonSerializer.Serialize(Assert.Single(store.Load())));
        Assert.Equal(EngineJson.Write(after), EngineJson.Write(restarted.Exchange(
            EngineRequest.SyncGame("sync", "goblin", capability))));
        return restarted;
    }
}
