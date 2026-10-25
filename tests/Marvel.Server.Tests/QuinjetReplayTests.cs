using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class QuinjetReplayTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TurnBeginningResponseRestartsAndResolvesExactlyOnce(bool accept)
    {
        var factory = new QuinjetReplayFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "quinjet",
            new GameSpecification("rhino", ["spider_man", "captain_marvel"], [], Seed: 12345)));
        Assert.Null(opened.Error);
        var pending = host.Exchange(EngineRequest.ResolveGame("end-turn", "quinjet", opened.Capability!,
            Decision.Decline, opened.Revision));
        Assert.Null(pending.Error);
        var uninterrupted = factory.Game;
        Assert.Equal(1, uninterrupted.Active);
        Assert.Equal(0, uninterrupted.State.Cards[factory.Quinjet].Tokens.GetValueOrDefault("c_time"));
        string saved = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var restarted = new EngineHost(factory, store: store);
        var restored = restarted.Exchange(EngineRequest.SyncGame("sync", "quinjet", opened.Capability!));
        Assert.Null(restored.Error);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(pending.Prompt!)),
            JsonSerializer.Serialize(PromptRecord.From(restored.Prompt!)));
        Assert.Equal(saved, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        var response = Assert.Single(restored.Prompt!.Affordances, offer => offer.AnchorId == factory.Quinjet);
        var answer = accept ? Decision.Take(response.Id) : Decision.Decline;
        uninterrupted.Resolve(answer);
        var result = restarted.Exchange(EngineRequest.ResolveGame("respond", "quinjet", opened.Capability!,
            answer, restored.Revision));
        Assert.Null(result.Error);
        Assert.Equal(uninterrupted.State.Digest().Canonical(), factory.Game.State.Digest().Canonical());
        Assert.Equal(accept ? 1 : 0, factory.Game.State.Cards[factory.Quinjet].Tokens.GetValueOrDefault("c_time"));
        Assert.True(factory.Game.IsRootPrompt);
        Assert.Equal(1, factory.Game.Active);
    }
}
