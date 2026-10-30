using Marvel.Client;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
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

    [Fact]
    public async Task AResolveAnsweredWithARefusalKeepsItsTypedReason()
    {
        var table = await RefusableTable.OpenAsync();
        table.Refuse();

        ClientResolutionResult result = await table.Client.ResolveAsync(table.Session,
            VisibleDecision(table.Opened.Prompt!), TestContext.Current.CancellationToken);

        AssertRefused(result);
        Assert.Equal(ClientMutationDisposition.Rejected, result.MutationDisposition);
    }

    [Fact]
    public async Task AnUndoAnsweredWithARefusalKeepsItsTypedReason()
    {
        var table = await RefusableTable.OpenAsync();
        ClientResolutionResult kept = await table.Client.ResolveAsync(table.Session,
            VisibleDecision(table.Opened.Prompt!), TestContext.Current.CancellationToken);
        Affordance change = Assert.Single(kept.Response!.Prompt!.Affordances, option => option.Verb == Game.ChangeForm);
        ClientResolutionResult changed = await table.Client.ResolveAsync(table.Session,
            new Decision(change.Id, []), TestContext.Current.CancellationToken);
        table.Refuse();

        ClientResolutionResult undone = await table.Client.UndoAsync(table.Session,
            changed.Response!.History!.Cursor - 1, TestContext.Current.CancellationToken);

        AssertRefused(undone);
    }

    [Fact]
    public async Task ALostResolveWhoseRecoveryIsRefusedKeepsItsTypedReason()
    {
        var table = await RefusableTable.OpenAsync();
        table.Refuse();
        table.Transport.LoseNextMutation = true;

        ClientResolutionResult result = await table.Client.ResolveAsync(table.Session,
            VisibleDecision(table.Opened.Prompt!), TestContext.Current.CancellationToken);

        AssertRefused(result);
        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal(EngineProtocol.Sync, table.Transport.Last?.Operation);
    }

    [Fact]
    public async Task AnInvitationToARefusedSaveKeepsItsTypedReason()
    {
        var refusal = new EngineResponse(EngineProtocol.Version, "local-attach", "shared-table", null, null, [],
            Error: new EngineError("save_incompatible", "Written by another version.",
                EngineFailureDisposition.SessionUnavailable));
        using var client = new LocalGameClient(new ScriptedTransport(refusal));

        ClientEntryResult attached = await client.AttachAsync(
            "shared-table", "invitation", TestContext.Current.CancellationToken);

        Assert.Equal("save_incompatible", attached.Error?.Code);
        Assert.Equal("Written by another version.", attached.Error?.Message);
    }

    private static void AssertRefused(ClientResolutionResult result)
    {
        Assert.Equal("save_incompatible", result.Error?.Code);
        Assert.Contains("Start a new game", result.Error!.Message, StringComparison.Ordinal);
        Assert.Equal(ClientSessionDisposition.Unavailable, result.SessionDisposition);
    }

    private sealed record RefusableTable(LocalGameClient Client, ClientSession Session, EngineResponse Opened,
        RestartableTransport Transport, MemorySessionStore Store, IDurableGameFactory Factory)
    {
        public static async Task<RefusableTable> OpenAsync()
        {
            var store = new MemorySessionStore();
            IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
            var transport = new RestartableTransport(new EngineHost(factory, store: store));
            var client = new LocalGameClient(transport);
            ClientEntryResult opened = await client.OpenSessionAsync(
                "refusable", Specification(), TestContext.Current.CancellationToken);
            return new(client, opened.Session!, opened.Response!, transport, store, factory);
        }

        public void Refuse()
        {
            StoredSession saved = Assert.Single(Store.Load());
            Store.Commit(saved with
            {
                Save = saved.Save with
                {
                    Compatibility = saved.Save.Compatibility with { ReplayContract = "engine-replay-v1" },
                },
            });
            Transport.Host = new EngineHost(Factory, store: Store);
        }
    }

    private sealed class RestartableTransport(IEngineEndpoint host) : IEngineTransport
    {
        public IEngineEndpoint Host { get; set; } = host;
        public bool LoseNextMutation { get; set; }
        public EngineRequest? Last { get; private set; }

        public ValueTask<EngineResponse> ExchangeAsync(
            EngineRequest request, CancellationToken cancellationToken = default)
        {
            Last = request;
            if (LoseNextMutation && request.Operation != EngineProtocol.Sync)
            {
                LoseNextMutation = false;
                return ValueTask.FromException<EngineResponse>(new IOException("response lost"));
            }
            return ValueTask.FromResult(Host.Exchange(request));
        }
    }
}
