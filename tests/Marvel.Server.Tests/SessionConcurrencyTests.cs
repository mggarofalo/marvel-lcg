using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class SessionConcurrencyTests
{
    [Fact]
    public async Task CommittingOneGameDoesNotBlockAnotherAndRacingAnswersCommitOnce()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        using var store = new BlockingStore();
        var host = new EngineHost(factory, store: store);
        EngineResponse first = host.Exchange(Open("slow"));
        EngineResponse second = host.Exchange(Open("other"));
        Assert.Null(first.Error);
        Assert.Null(second.Error);
        var answer = EngineRequest.ResolveGame("answer", "slow", first.Capability!,
            new Decision(Assert.Single(first.Prompt!.Affordances).Id, []));
        Task<EngineResponse> committing = Task.Run(() => host.Exchange(answer));
        try
        {
            Assert.True(store.Entered.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            EngineResponse synchronized = await Task.Run(() => host.Exchange(
                EngineRequest.SyncGame("sync", "other", second.Capability!))).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Null(synchronized.Error);
            Task<EngineResponse> competing = Task.Run(() => host.Exchange(answer with { RequestId = "race" }));
            store.Release.Set();
            Assert.Null((await committing).Error);
            Assert.Equal("stale_decision", (await competing).Error?.Code);
            Assert.Equal(1, Assert.Single(store.Load(), item => item.Save.Session.Label == "slow").Save.Revision);
        }
        finally { store.Release.Set(); await committing; }
    }

    [Fact]
    public void RepeatedSynchronizationReusesCommittedVerifiedHistory()
    {
        var factory = new CountingFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var host = new EngineHost(factory);
        EngineResponse opened = host.Exchange(Open("cache"));
        Assert.Null(opened.Error);
        string capability = opened.Capability!;
        EngineResponse committed = host.Exchange(EngineRequest.ResolveGame("keep", "cache", capability,
            new Decision(Assert.Single(opened.Prompt!.Affordances).Id, [])));
        Assert.Null(committed.Error);
        Assert.Equal(1, committed.Revision);
        int before = factory.Calls;
        for (int i = 0; i < 3; i++)
        {
            EngineResponse current = host.Exchange(EngineRequest.SyncGame("sync", "cache", capability));
            Assert.Null(current.Error);
            Assert.Equal(committed.History!.Entries, current.History!.Entries);
            Assert.Equal(committed.History.Undo, current.History.Undo);
        }
        Assert.Equal(before, factory.Calls);
    }

    private static EngineRequest Open(string id) => EngineRequest.OpenGame("open", id,
        new GameSpecification("rhino", ["spider_man"], [], 91));

    private sealed class CountingFactory(IDurableGameFactory inner) : IDurableGameFactory
    {
        public int Calls { get; private set; }
        public SessionCompatibility Compatibility => inner.Compatibility;
        public OpenedGame Create(GameSpecification specification) { Calls++; return inner.Create(specification); }
    }

    private sealed class BlockingStore : ISessionStore, IDisposable
    {
        private readonly MemorySessionStore inner = new();
        public ManualResetEventSlim Entered { get; } = new();
        public ManualResetEventSlim Release { get; } = new();
        public IReadOnlyList<StoredSession> Load() => inner.Load();
        public string? Commit(StoredSession session)
        {
            if (session.Save.Session.Label == "slow" && session.Save.Revision == 1)
            {
                Entered.Set();
                if (!Release.Wait(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken)) throw new IOException("test release was not signaled");
            }
            return inner.Commit(session);
        }
        public void Dispose() { Entered.Dispose(); Release.Dispose(); }
    }
}
