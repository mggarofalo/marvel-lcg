using Marvel.Client;
using Marvel.Protocol;
using Marvel.Server;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class SaveRefusalClientTests : LocalGameClientTestBase
{
    [Fact]
    public async Task ARefusedSaveEndsTheTableWithItsTypedReason()
    {
        var store = new MemorySessionStore();
        IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var transport = new RestartableTransport(new EngineHost(factory, store: store));
        using var client = new LocalGameClient(transport);
        ClientEntryResult opened = await client.OpenSessionAsync(
            "refused-save", Specification(), TestContext.Current.CancellationToken);
        var lifecycle = new ClientGameLifecycle();
        Assert.True(lifecycle.Enter(lifecycle.BeginEntry(), client, opened));
        StoredSession saved = Assert.Single(store.Load());
        store.Commit(saved with
        {
            Save = saved.Save with
            {
                Compatibility = saved.Save.Compatibility with { ReplayContract = "engine-replay-v1" },
            },
        });
        transport.Host = new EngineHost(factory, store: store);

        ClientLifecycleUpdate update = Assert.IsType<ClientLifecycleUpdate>(
            await lifecycle.SynchronizeAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("save_incompatible", update.Error?.Code);
        Assert.Equal(GameProgressKind.SessionUnavailable, update.Progress.Kind);
        Assert.Equal("Saved game cannot be loaded.", update.Progress.Title);
        Assert.Contains("SAVE INCOMPATIBLE", update.Progress.Status, StringComparison.Ordinal);
        Assert.Contains("Start a new game", update.Progress.Description, StringComparison.Ordinal);
        Assert.True(update.Progress.LocksDecisions);
    }

    [Fact]
    public void OnlyARefusedSaveKeepsItsCodeThroughASessionLoss()
    {
        GameProgressPresentation refused = GameProgressPresentation.Unavailable(
            new ClientStartupError("save_incompatible", "Written by another version."));
        GameProgressPresentation lost = GameProgressPresentation.Unavailable(
            new ClientStartupError("session_unavailable", "Gone."));

        Assert.Equal(GameProgressKind.SessionUnavailable, refused.Kind);
        Assert.Equal(GameProgressKind.SessionUnavailable, lost.Kind);
        Assert.NotEqual(refused.Status, lost.Status);
        Assert.Equal("Written by another version.", refused.Description);
    }

    private sealed class RestartableTransport(IEngineEndpoint host) : IEngineTransport
    {
        public IEngineEndpoint Host { get; set; } = host;

        public ValueTask<EngineResponse> ExchangeAsync(
            EngineRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Host.Exchange(request));
    }
}
