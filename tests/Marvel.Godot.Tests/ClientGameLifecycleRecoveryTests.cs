using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ClientGameLifecycleRecoveryTests : LocalGameClientTestBase
{
    [Fact]
    public async Task RealSessionUndoReplacesRevisionHistoryAndPromptTogether()
    {
        var client = new LocalGameClient(new InProcessTransport(Host()));
        ClientEntryResult opened = await client.OpenSessionAsync("lifecycle-undo", Specification(), TestContext.Current.CancellationToken);
        var lifecycle = new ClientGameLifecycle();
        Assert.True(lifecycle.Enter(lifecycle.BeginEntry(), client, opened));
        await lifecycle.ResolveAsync(0, VisibleDecision(lifecycle.CurrentGame!.Prompt!), TestContext.Current.CancellationToken);
        var change = Assert.Single(lifecycle.CurrentGame!.Prompt!.Affordances, option => option.Verb == Game.ChangeForm);
        await lifecycle.ResolveAsync(lifecycle.CurrentGame.Revision, new EngineDecision(change.Id, []), TestContext.Current.CancellationToken);
        EngineResponse before = lifecycle.CurrentGame!;
        int cursor = before.History!.Cursor - 1;
        Assert.True(lifecycle.CanUndo(cursor));
        ClientLifecycleUpdate result = Assert.IsType<ClientLifecycleUpdate>(await lifecycle.UndoAsync(cursor, TestContext.Current.CancellationToken));
        Assert.True(result.MutationAccepted);
        Assert.Equal(ClientDraftDisposition.Replace, result.Draft);
        Assert.Same(result.Response, lifecycle.CurrentGame);
        Assert.Equal(before.Revision + 1, lifecycle.CurrentGame!.Revision);
        Assert.Equal(cursor, lifecycle.CurrentGame.History!.Cursor);
        Assert.Contains(lifecycle.CurrentGame.World!.Areas.SelectMany(area => area.Cards), card => card.Face?.Title == "Peter Parker");
        Assert.Null(lifecycle.CurrentGame.Capability);
        Assert.Null(lifecycle.CurrentGame.Invitations);
    }

    [Fact]
    public async Task RejectionWithFailedRecoveryRequiresSyncAndNeverPermitsAnotherMutation()
    {
        EngineResponse original = Initial();
        var transport = new ScriptedTransport(original with { RequestId = "local-resolve",
            Error = new("stale_decision", "The prompt changed.") }, new IOException("recovery lost"), new IOException("sync lost"));
        ClientGameLifecycle lifecycle = Enter(transport, original);
        ClientLifecycleUpdate rejected = Assert.IsType<ClientLifecycleUpdate>(
            await lifecycle.ResolveAsync(0, EngineDecision.Decline, TestContext.Current.CancellationToken));
        Assert.Equal(GameProgressKind.DecisionRejected, rejected.Progress.Kind);
        Assert.False(lifecycle.CanResolve);
        Assert.Null(await lifecycle.ResolveAsync(0, EngineDecision.Decline, TestContext.Current.CancellationToken));
        await lifecycle.SynchronizeAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(lifecycle.Progress!.LocksDecisions);
        Assert.False(lifecycle.CanResolve);
        Assert.Equal(3, transport.Requests.Count);
    }

    [Fact]
    public async Task RecoveredRejectionReplacesTheDraftEvenWhenRevisionDidNotAdvance()
    {
        EngineResponse original = Initial();
        var transport = new ScriptedTransport(original with { RequestId = "local-resolve",
            Error = new("invalid_decision", "The answer was rejected.") },
            original with { RequestId = "local-recover", Events = [] });
        ClientGameLifecycle lifecycle = Enter(transport, original);
        ClientLifecycleUpdate recovered = Assert.IsType<ClientLifecycleUpdate>(
            await lifecycle.ResolveAsync(0, EngineDecision.Decline, TestContext.Current.CancellationToken));
        Assert.False(recovered.MutationAccepted);
        Assert.Equal(ClientDraftDisposition.Replace, recovered.Draft);
        Assert.Equal(0, lifecycle.CurrentGame!.Revision);
        Assert.True(lifecycle.CanResolve);
    }

    [Fact]
    public async Task StorageFailureSurvivesPresentationFailureThenFailedAndSuccessfulSynchronization()
    {
        EngineResponse original = Initial();
        var transport = new ScriptedTransport(original with { RequestId = "local-resolve",
            Error = new("save_failed", "Storage requires operator attention.") },
            original with { RequestId = "local-recover", Events = [] },
            new IOException("sync response lost"),
            original with { RequestId = "local-sync", Events = [] });
        ClientGameLifecycle lifecycle = Enter(transport, original);
        await lifecycle.ResolveAsync(0, EngineDecision.Decline, TestContext.Current.CancellationToken);
        lifecycle.PresentationFailed();
        Assert.NotNull(lifecycle.Progress!.OperationalLock);
        Assert.True(lifecycle.CanSynchronize);
        ClientLifecycleUpdate failed = Assert.IsType<ClientLifecycleUpdate>(
            await lifecycle.SynchronizeAsync(hasDraft: true, TestContext.Current.CancellationToken));
        Assert.Equal(GameProgressKind.Unconfirmed, failed.Progress.Kind);
        Assert.NotNull(failed.Progress.OperationalLock);
        Assert.False(lifecycle.CanResolve);
        ClientLifecycleUpdate synced = Assert.IsType<ClientLifecycleUpdate>(
            await lifecycle.SynchronizeAsync(hasDraft: true, TestContext.Current.CancellationToken));
        Assert.Equal(ClientDraftDisposition.Replace, synced.Draft);
        Assert.Equal(GameProgressKind.StorageFailure, synced.Progress.Kind);
        Assert.NotNull(synced.Progress.OperationalLock);
        Assert.False(lifecycle.CanResolve);
    }

    [Fact]
    public async Task FailedPresentationLocksAnAcceptedResponseUntilAuthorityCanBeRenderedAgain()
    {
        EngineResponse original = Initial();
        var transport = new ScriptedTransport(original with { RequestId = "local-sync", Events = [] });
        ClientGameLifecycle lifecycle = Enter(transport, original);
        lifecycle.PresentationFailed();
        Assert.Equal(GameProgressKind.Unconfirmed, lifecycle.Progress!.Kind);
        Assert.False(lifecycle.CanResolve);
        Assert.True(lifecycle.CanSynchronize);
        await lifecycle.SynchronizeAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(lifecycle.CanResolve);
    }

    private static EngineResponse Initial() => Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()))
        with { Capability = null, Invitations = null };

    private static ClientGameLifecycle Enter(IEngineTransport transport, EngineResponse response)
    {
        var lifecycle = new ClientGameLifecycle();
        Assert.True(lifecycle.Enter(lifecycle.BeginEntry(), new LocalGameClient(transport),
            new ClientEntryResult(new ClientSession(response.GameId, "authority"), response, [], null)));
        return lifecycle;
    }
}
