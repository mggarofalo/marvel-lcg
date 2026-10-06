using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ClientHistoryLifecycleTests : LocalGameClientTestBase
{
    [Fact]
    public async Task UndoProgressNamesHistoryCommitmentAndNotSentPermitsOnlyExplicitRetry()
    {
        var transport = new PausedUndoTransport(new InProcessTransport(Host()));
        var client = new LocalGameClient(transport);
        ClientEntryResult opened = await client.OpenSessionAsync("undo-copy", Specification(), TestContext.Current.CancellationToken);
        var lifecycle = new ClientGameLifecycle();
        Assert.True(lifecycle.Enter(lifecycle.BeginEntry(), client, opened));
        await lifecycle.ResolveAsync(0, VisibleDecision(lifecycle.CurrentGame!.Prompt!), TestContext.Current.CancellationToken);
        var form = Assert.Single(lifecycle.CurrentGame!.Prompt!.Affordances, option => option.Verb == Game.ChangeForm);
        await lifecycle.ResolveAsync(lifecycle.CurrentGame.Revision, new EngineDecision(form.Id, []), TestContext.Current.CancellationToken);
        EngineResponse before = lifecycle.CurrentGame!;
        int cursor = before.History!.Cursor - 1;
        Task<ClientLifecycleUpdate?> pending = lifecycle.UndoAsync(cursor, TestContext.Current.CancellationToken);
        Assert.Contains("Undo", lifecycle.Progress!.Title);
        Assert.Contains("history boundary", lifecycle.Progress.Description);
        Assert.Equal("UNDO SENT  ·  VERIFYING HISTORY", lifecycle.Progress.Status);
        Assert.True(lifecycle.Progress.LocksDecisions);
        transport.FailBeforeSend();
        ClientLifecycleUpdate failed = Assert.IsType<ClientLifecycleUpdate>(await pending);
        Assert.Equal("Undo not sent.", failed.Progress.Title);
        Assert.Contains("retry Undo", failed.Progress.Description);
        Assert.DoesNotContain("selection is preserved", failed.Progress.Description);
        Assert.Equal(ClientDraftDisposition.Preserve, failed.Draft);
        Assert.Same(before, lifecycle.CurrentGame);
        Assert.True(lifecycle.CanUndo(cursor));
        Assert.Equal(1, transport.UndoRequests);
    }

    private sealed class PausedUndoTransport(IEngineTransport inner) : IEngineTransport
    {
        private readonly TaskCompletionSource<EngineResponse> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int UndoRequests { get; private set; }
        public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Operation != EngineProtocol.Undo) return inner.ExchangeAsync(request, cancellationToken);
            UndoRequests++;
            return new(response.Task);
        }
        internal void FailBeforeSend() => response.SetException(new EngineTransportException(false, new IOException("not sent")));
    }
}
