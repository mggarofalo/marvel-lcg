using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>Observes the unchanged production factory; never prepares or mutates a world.</summary>
internal sealed class PublicDoctorStrangeRestartFixture : IDurableGameFactory
{
    private readonly DatasetGameFactory _factory = DatasetGameFactory.Load(RepositoryPaths.Root);
    internal Game Game { get; private set; } = null!;
    public SessionCompatibility Compatibility => _factory.Compatibility;

    public OpenedGame Create(GameSpecification specification)
    {
        var opened = _factory.Create(specification);
        Game = opened.Game;
        return opened;
    }

    internal static Decision Paid(Affordance offer)
    {
        var cost = Assert.Single(offer.CostOptions);
        int[] generators = cost.Generators.Select(generator => generator.Effect).Distinct().ToArray();
        Assert.True(generators.Length < 12, "Bounded starter-hand payment enumeration");
        for (int mask = 0; mask < 1 << generators.Length; mask++)
        {
            int[] paying = generators.Where((_, index) => (mask & (1 << index)) != 0).ToArray();
            var allocation = ResourcePayment.Allocate(cost, paying);
            if (allocation is not null && ResourcePayment.Allows(cost, paying, null, allocation))
                return Decision.Take(offer.Id, offer.Targets?.Legal ?? [], paying,
                    new Dictionary<string, long>(), allocation);
        }
        throw new Xunit.Sdk.XunitException("The printed starter must afford this Invocation");
    }

    internal static EngineResponse Answer(EngineHost host, EngineResponse opened,
        EngineResponse current, Decision answer)
    {
        var result = host.Exchange(EngineRequest.ResolveGame("answer-" + current.Revision,
            opened.GameId, opened.Capability!, answer, current.Revision));
        Assert.Null(result.Error);
        return result;
    }

    internal static (EngineHost Host, MemorySessionStore Store, PublicDoctorStrangeRestartFixture Factory,
        EngineResponse Response) Restart(EngineHost originalHost, MemorySessionStore store, PublicDoctorStrangeRestartFixture original,
        EngineResponse opened, EngineResponse current)
    {
        string bytes = StoredSessionJson.Write(Assert.Single(store.Load()));
        var freshStore = new MemorySessionStore();
        Assert.Null(freshStore.Commit(StoredSessionJson.Read(bytes)));
        var freshFactory = new PublicDoctorStrangeRestartFixture();
        var freshHost = new EngineHost(freshFactory, store: freshStore);
        var restored = freshHost.Exchange(EngineRequest.SyncGame("restart", opened.GameId, opened.Capability!));
        Assert.Null(restored.Error);
        Assert.NotSame(original.Game, freshFactory.Game);
        Assert.NotSame(original.Game.State, freshFactory.Game.State);
        Assert.Equal(bytes, StoredSessionJson.Write(Assert.Single(freshStore.Load())));
        var synced = originalHost.Exchange(EngineRequest.SyncGame("control-sync", opened.GameId, opened.Capability!));
        Assert.Null(synced.Error);
        Assert.Equal(current.Revision, synced.Revision);
        Equivalent(original, freshFactory, synced, restored);

        // A stale commitment cannot charge costs again or consume the saved continuation.
        var rejected = freshHost.Exchange(EngineRequest.ResolveGame("stale", opened.GameId,
            opened.Capability!, Decision.Decline, restored.Revision - 1));
        Assert.NotNull(rejected.Error);
        Assert.Equal(bytes, StoredSessionJson.Write(Assert.Single(freshStore.Load())));
        return (freshHost, freshStore, freshFactory, restored);
    }

    internal static void Equivalent(PublicDoctorStrangeRestartFixture expected,
        PublicDoctorStrangeRestartFixture actual, EngineResponse left, EngineResponse right)
    {
        Assert.Equal(expected.Game.State.Digest().Canonical(), actual.Game.State.Digest().Canonical());
        Assert.Equal(JsonSerializer.Serialize(expected.Game.State.Random.Generator.GetState()),
            JsonSerializer.Serialize(actual.Game.State.Random.Generator.GetState()));
        Assert.Equal(expected.Game.State.Random.Generator.WordsConsumed, actual.Game.State.Random.Generator.WordsConsumed);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(left.Prompt!)),
            JsonSerializer.Serialize(PromptRecord.From(right.Prompt!)));
        Assert.Equal(JsonSerializer.Serialize(left.World), JsonSerializer.Serialize(right.World));
        Assert.Equal(JsonSerializer.Serialize(left.History), JsonSerializer.Serialize(right.History));
        Assert.Equal(left.Revision, right.Revision);
    }

    internal static void Continued(MemorySessionStore expected, MemorySessionStore actual,
        EngineResponse left, EngineResponse right)
    {
        Assert.Equal(JsonSerializer.Serialize(left.Events), JsonSerializer.Serialize(right.Events));
        Assert.Equal(SessionSaveJson.Write(Assert.Single(expected.Load()).Save),
            SessionSaveJson.Write(Assert.Single(actual.Load()).Save));
    }
}
