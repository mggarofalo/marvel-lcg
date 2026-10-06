using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Managed lifecycle checks over validated protocol responses; no native UI claims.</summary>
public sealed class ClientGameLifecycleTests : LocalGameClientTestBase
{
    [Fact]
    public async Task ProvenNotSentRetainsTheTableAndPermitsOneExplicitRetry()
    {
        EngineResponse original = Initial();
        var transport = new ScriptedTransport(
            new EngineTransportException(requestMayHaveCommitted: false, new IOException("not delivered")),
            original with { RequestId = "local-resolve", Revision = 1, Capability = null });
        ClientGameLifecycle lifecycle = Enter(transport, original);
        ClientLifecycleUpdate first = Assert.IsType<ClientLifecycleUpdate>(await lifecycle.ResolveAsync(0, EngineDecision.Decline, TestContext.Current.CancellationToken));
        Assert.Equal(ClientDraftDisposition.Retry, first.Draft);
        Assert.Same(original, lifecycle.CurrentGame);
        Assert.True(lifecycle.CanResolve);
        Assert.Equal(GameProgressKind.DecisionNotSent, lifecycle.Progress!.Kind);
        ClientLifecycleUpdate second = Assert.IsType<ClientLifecycleUpdate>(await lifecycle.ResolveAsync(0, EngineDecision.Decline, TestContext.Current.CancellationToken));
        Assert.True(second.MutationAccepted);
        Assert.Equal(ClientDraftDisposition.Replace, second.Draft);
        Assert.Equal(1, lifecycle.CurrentGame!.Revision);
        Assert.Null(await lifecycle.ResolveAsync(0, EngineDecision.Decline, TestContext.Current.CancellationToken));
        Assert.Equal(2, transport.Requests.Count);
    }

    [Fact]
    public async Task UncertainMutationAndFailedSynchronizationNeverUnlockOrResend()
    {
        EngineResponse original = Initial();
        var transport = new ScriptedTransport(new IOException("lost"), new IOException("recovery lost"),
            new IOException("sync lost"), original with { RequestId = "local-sync", Capability = null, Events = [], Revision = 1 });
        ClientGameLifecycle lifecycle = Enter(transport, original);
        ClientLifecycleUpdate lost = Assert.IsType<ClientLifecycleUpdate>(await lifecycle.ResolveAsync(0, EngineDecision.Decline, TestContext.Current.CancellationToken));
        Assert.Equal(ClientDraftDisposition.Preserve, lost.Draft);
        Assert.Equal(GameProgressKind.Unconfirmed, lifecycle.Progress!.Kind);
        Assert.False(lifecycle.CanResolve);
        Assert.True(lifecycle.CanSynchronize);
        Assert.Null(await lifecycle.ResolveAsync(0, EngineDecision.Decline, TestContext.Current.CancellationToken));
        await lifecycle.SynchronizeAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(GameProgressKind.Unconfirmed, lifecycle.Progress.Kind);
        Assert.False(lifecycle.CanResolve);
        ClientLifecycleUpdate recovered = Assert.IsType<ClientLifecycleUpdate>(await lifecycle.SynchronizeAsync(hasDraft: true, TestContext.Current.CancellationToken));
        Assert.Equal(ClientDraftDisposition.Replace, recovered.Draft);
        Assert.Contains("Accepted actions remain in play", recovered.Progress.Description);
        Assert.Equal(1, lifecycle.CurrentGame!.Revision);
        Assert.True(lifecycle.CanResolve);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync, EngineProtocol.Sync, EngineProtocol.Sync],
            transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task InFlightSubmissionExcludesRepeatedInputAndSynchronization()
    {
        EngineResponse original = Initial();
        var transport = new DelayedTransport();
        ClientGameLifecycle lifecycle = Enter(transport, original);
        Task<ClientLifecycleUpdate?> pending = lifecycle.ResolveAsync(0, EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.Equal(GameProgressKind.Resolving, lifecycle.Progress!.Kind);
        Assert.False(lifecycle.CanSynchronize);
        Assert.Null(await lifecycle.ResolveAsync(0, EngineDecision.Decline, TestContext.Current.CancellationToken));
        Assert.Null(await lifecycle.SynchronizeAsync(cancellationToken: TestContext.Current.CancellationToken));
        transport.Complete(original with { RequestId = "local-resolve", Revision = 1, Capability = null });
        Assert.NotNull(await pending);
        Assert.Single(transport.Requests);
    }

    [Theory]
    [InlineData("shared-table")]
    [InlineData("replacement")]
    public async Task DetachAndReplacementDiscardOldResponsesWithoutTouchingNewAuthority(string replacementId)
    {
        EngineResponse original = Initial();
        var transport = new DelayedTransport();
        ClientGameLifecycle lifecycle = Enter(transport, original);
        Task<ClientLifecycleUpdate?> pending = lifecycle.ResolveAsync(0, EngineDecision.Decline, TestContext.Current.CancellationToken);
        lifecycle.Detach();
        EngineResponse replacement = original with { GameId = replacementId, Revision = 9 };
        var replacementTransport = new CapturingTransport();
        long ticket = lifecycle.BeginEntry();
        Assert.True(lifecycle.Enter(ticket, new LocalGameClient(replacementTransport), Entry(replacement)));
        transport.Complete(original with { RequestId = "local-resolve", Revision = 1, Capability = null });
        Assert.Null(await pending);
        Assert.Same(replacement, lifecycle.CurrentGame);
        Assert.True(lifecycle.CanResolve);
        Assert.Empty(replacementTransport.Requests);
    }

    [Fact]
    public async Task CancellationBeforeAdmissionIsNotSentAndDuringTransmissionIsUncertain()
    {
        EngineResponse original = Initial();
        var transport = new DelayedTransport();
        ClientGameLifecycle lifecycle = Enter(transport, original);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        ClientLifecycleUpdate notSent = Assert.IsType<ClientLifecycleUpdate>(
            await lifecycle.ResolveAsync(0, EngineDecision.Decline, cancelled.Token));
        Assert.Equal(ClientDraftDisposition.Retry, notSent.Draft);
        Assert.Empty(transport.Requests);
        using var inFlight = new CancellationTokenSource();
        Task<ClientLifecycleUpdate?> pending = lifecycle.ResolveAsync(0, EngineDecision.Decline, inFlight.Token);
        inFlight.Cancel();
        transport.Cancel(inFlight.Token);
        ClientLifecycleUpdate uncertain = Assert.IsType<ClientLifecycleUpdate>(await pending);
        Assert.Equal(ClientDraftDisposition.Preserve, uncertain.Draft);
        Assert.Equal(GameProgressKind.Unconfirmed, lifecycle.Progress!.Kind);
        Assert.False(lifecycle.CanResolve);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task MalformedMutationCannotReplaceTheCompleteTable()
    {
        EngineResponse original = Initial();
        var transport = new ScriptedTransport(original with { RequestId = "local-resolve", Revision = 1, World = null },
            new IOException("recovery unavailable"));
        ClientGameLifecycle lifecycle = Enter(transport, original);
        await lifecycle.ResolveAsync(0, EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.Same(original, lifecycle.CurrentGame);
        Assert.False(lifecycle.CanResolve);
        Assert.Equal(GameProgressKind.Unconfirmed, lifecycle.Progress!.Kind);
    }

    [Fact]
    public async Task LostSessionClearsAuthorityAndTerminalSnapshotRetainsInspection()
    {
        EngineResponse original = Initial();
        var transport = new ScriptedTransport(original with { RequestId = "local-sync", Capability = null,
            Events = [], Prompt = null, World = original.World! with { Outcome = Outcome.PlayersWin } },
            original with { RequestId = "local-sync", Error = new("session_not_found", "unavailable") });
        ClientGameLifecycle lifecycle = Enter(transport, original);
        await lifecycle.SynchronizeAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(Outcome.PlayersWin, lifecycle.CurrentGame!.World!.Outcome);
        Assert.False(lifecycle.CanResolve);
        Assert.True(lifecycle.CanSynchronize);
        ClientLifecycleUpdate unavailable = Assert.IsType<ClientLifecycleUpdate>(await lifecycle.SynchronizeAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(ClientDraftDisposition.Clear, unavailable.Draft);
        Assert.Null(lifecycle.CurrentGame);
        Assert.False(lifecycle.CanSynchronize);
        Assert.False(lifecycle.CanResolve);
    }

    [Fact]
    public void NewEntryAndDetachInvalidateOldSetupAndEntryTickets()
    {
        var lifecycle = new ClientGameLifecycle();
        long old = lifecycle.BeginEntry();
        long current = lifecycle.BeginEntry();
        Assert.False(lifecycle.FinishEntry(old));
        Assert.False(lifecycle.Enter(old, new LocalGameClient(new CapturingTransport()), Entry(Initial())));
        Assert.True(lifecycle.IsCurrentEntry(current));
        lifecycle.Detach();
        Assert.False(lifecycle.IsCurrentEntry(current));
        Assert.False(lifecycle.EntryPending);
        Assert.Null(lifecycle.CurrentGame);
    }

    private static EngineResponse Initial() => Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()))
        with { Capability = null, Invitations = null };

    private static ClientEntryResult Entry(EngineResponse response) => new(
        new ClientSession(response.GameId, "private-authority"), response, [], null);

    private static ClientGameLifecycle Enter(IEngineTransport transport, EngineResponse response)
    {
        var lifecycle = new ClientGameLifecycle();
        Assert.True(lifecycle.Enter(lifecycle.BeginEntry(), new LocalGameClient(transport), Entry(response)));
        return lifecycle;
    }

    private sealed class DelayedTransport : IEngineTransport
    {
        private readonly TaskCompletionSource<EngineResponse> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<EngineRequest> Requests { get; } = [];
        public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return new(completion.Task);
        }
        public void Complete(EngineResponse response) => completion.SetResult(response);
        public void Cancel(CancellationToken token) => completion.SetCanceled(token);
    }
}
