using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;
public sealed class LocalGameClientUncertainMutationTests : LocalGameClientTestBase
{
    [Fact]
    public async Task UncertainMutationUsesUniqueCorrelationAndRecordsAReconnect()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        var transport = new RecoveringTransport(current);
        var sink = new CollectingOperationalSink();
        var log = new OperationalLog(sink, "client-test");
        ClientResolutionResult result = await new LocalGameClient(transport, log).ResolveAsync(new ClientSession("shared-table", current.Capability!), EngineDecision.Decline, TestContext.Current.CancellationToken);
        log.Flush(TimeSpan.FromSeconds(2));
        Assert.True(result.HasAuthoritativeView);
        Assert.Equal(2, transport.Requests.Count);
        Assert.StartsWith("local-resolve-", transport.Requests[0].RequestId, StringComparison.Ordinal);
        Assert.StartsWith("local-recover-", transport.Requests[1].RequestId, StringComparison.Ordinal);
        Assert.NotEqual(transport.Requests[0].RequestId, transport.Requests[1].RequestId);
        OperationalRecord reconnect = Assert.Single(sink.Records);
        Assert.Equal(OperationalEventIds.ReconnectCompleted, reconnect.EventId);
        Assert.Equal("accepted", reconnect.Disposition);
        Assert.Equal("reconnect", reconnect.Operation);
    }

    [Fact]
    public async Task SynchronizeReturnsOneSanitizedCompleteCurrentView()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        var transport = new ScriptedTransport(current with { RequestId = "local-sync", Capability = "must-not-escape", Invitations = [new SeatInvitation(1, "must-not-escape-either")], Events = [], });
        ClientSynchronizationResult result = await new LocalGameClient(transport).SynchronizeAsync(new ClientSession("shared-table", current.Capability!), TestContext.Current.CancellationToken);
        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.Equal(ClientSessionDisposition.Active, result.SessionDisposition);
        Assert.NotNull(result.Response?.Prompt);
        Assert.Null(result.Response?.Capability);
        Assert.Null(result.Response?.Invitations);
        EngineRequest request = Assert.Single(transport.Requests);
        Assert.Equal(EngineProtocol.Sync, request.Operation);
        Assert.Equal(current.Capability, request.Capability);
    }

    [Fact]
    public async Task ResolveAcceptsACancellablePromptWithNoLegalActions()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", LocalGameSession.GameId, Specification()));
        var emptyTurn = new Prompt(0, Question.TurnOption, TimingPriority.Untimed, "WhenPlayerInTurn", "Player turn", Cancellable: true, Affordances: []);
        var transport = new ScriptedTransport(current with { RequestId = "local-resolve", Revision = 1, Capability = null, Prompt = emptyTurn, Events = [], });
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(current.Capability!, EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.Equal(ClientMutationDisposition.Accepted, result.MutationDisposition);
        Assert.Empty(result.Response!.Prompt!.Affordances);
    }

    [Fact]
    public async Task SynchronizeAcceptsWaitingAndTerminalViews()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        EngineResponse[] complete = [current with
        {
            RequestId = "local-sync",
            Capability = null,
            Prompt = null,
            Events = [],
        }, current with
        {
            RequestId = "local-sync",
            Capability = null,
            Prompt = null,
            Events = [],
            World = current.World!with
            {
                Outcome = Outcome.PlayersWin
            },
        }, ];
        foreach (EngineResponse response in complete)
        {
            ClientSynchronizationResult result = await new LocalGameClient(new FixedTransport(response)).SynchronizeAsync(new ClientSession("shared-table", current.Capability!), TestContext.Current.CancellationToken);
            Assert.True(result.Succeeded, result.Error?.Message);
            Assert.Null(result.Response?.Prompt);
        }
    }

    [Fact]
    public async Task SynchronizationRejectsNonemptyEventsWithoutMakingSessionUnavailable()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        GameEvent replayed = new FieldSet(11, "damage", 0, 1)
        {
            Trigger = "WhenPlayerInTurn",
            Verb = "Attack",
        };
        var transport = new ScriptedTransport(current with { RequestId = "local-sync", Capability = null, Events = [replayed], });
        ClientSynchronizationResult result = await new LocalGameClient(transport).SynchronizeAsync(new ClientSession("shared-table", current.Capability!), TestContext.Current.CancellationToken);
        Assert.False(result.HasAuthoritativeView);
        Assert.Equal("invalid_response", result.Error?.Code);
        Assert.Equal(ClientSessionDisposition.Active, result.SessionDisposition);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task AnOlderSynchronizationCannotRollTheRememberedRevisionBackward()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        var transport = new ScriptedTransport(current with { RequestId = "local-attach", Capability = "session", Revision = 3, }, current with { RequestId = "local-sync", Capability = null, Events = [], Revision = 2, }, current with { RequestId = "local-resolve", Capability = null, Revision = 4, });
        var client = new LocalGameClient(transport);
        ClientEntryResult attached = await client.AttachAsync("shared-table", "invitation", TestContext.Current.CancellationToken);
        ClientSynchronizationResult old = await client.SynchronizeAsync(attached.Session!, TestContext.Current.CancellationToken);
        ClientResolutionResult resolved = await client.ResolveAsync(attached.Session!, EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.Equal("invalid_response", old.Error?.Code);
        Assert.Null(old.Response);
        Assert.Equal(ClientMutationDisposition.Accepted, resolved.MutationDisposition);
        Assert.Equal(3, transport.Requests[2].ExpectedRevision);
    }

    [Fact]
    public async Task ASuccessfulResolveMustAdvanceExactlyOneRevision()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        var transport = new ScriptedTransport(current with { RequestId = "local-attach", Capability = "session", Revision = 3, }, current with { RequestId = "local-resolve", Capability = null, Revision = 3, }, current with { RequestId = "local-recover", Capability = null, Events = [], Revision = 3, });
        var client = new LocalGameClient(transport);
        ClientEntryResult attached = await client.AttachAsync("shared-table", "invitation", TestContext.Current.CancellationToken);
        ClientResolutionResult result = await client.ResolveAsync(attached.Session!, EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal("invalid_response", result.Error?.Code);
        Assert.Equal(3, result.Response?.Revision);
        Assert.Equal(3, transport.Requests[1].ExpectedRevision);
    }

    [Fact]
    public async Task RecoveryDoesNotRenderOrReplayNonemptySynchronizationEvents()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        GameEvent replayed = new FieldSet(11, "damage", 0, 1)
        {
            Trigger = "WhenPlayerInTurn",
            Verb = "Attack",
        };
        var transport = new ScriptedTransport(new IOException("response lost"), current with { RequestId = "local-recover", Capability = null, Events = [replayed], });
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(new ClientSession("shared-table", current.Capability!), EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.False(result.HasAuthoritativeView);
        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal(ClientSessionDisposition.Active, result.SessionDisposition);
        Assert.Equal("transport_unavailable", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync], transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task InvalidAndExpiredSynchronizationSessionsAreUnavailable()
    {
        var capture = new CapturingTransport();
        ClientSynchronizationResult invalid = await new LocalGameClient(capture).SynchronizeAsync(new ClientSession("shared-table", ""), TestContext.Current.CancellationToken);
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilityIssuer("owner"));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "shared-table", Specification()));
        host.Exchange(EngineRequest.CloseGame("close", "shared-table", opened.Capability!));
        ClientSynchronizationResult expired = await new LocalGameClient(new InProcessTransport(host)).SynchronizeAsync(new ClientSession("shared-table", opened.Capability!), TestContext.Current.CancellationToken);
        Assert.Equal("session_unavailable", invalid.Error?.Code);
        Assert.Equal(ClientSessionDisposition.Unavailable, invalid.SessionDisposition);
        Assert.Empty(capture.Requests);
        Assert.Equal("session_unavailable", expired.Error?.Code);
        Assert.Equal(ClientSessionDisposition.Unavailable, expired.SessionDisposition);
    }

    [Fact]
    public async Task SynchronizationFailuresKeepTheSessionActiveAndDoNotEchoSecrets()
    {
        const string secret = "private-session-capability";
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        ClientSynchronizationResult malformed = await new LocalGameClient(new FixedTransport(current with { RequestId = "local-sync", Capability = null, Prompt = current.Prompt!with { Affordances = null! }, })).SynchronizeAsync(new ClientSession("shared-table", secret), TestContext.Current.CancellationToken);
        ClientSynchronizationResult lost = await new LocalGameClient(new FailingTransport(secret)).SynchronizeAsync(new ClientSession("shared-table", secret), TestContext.Current.CancellationToken);
        ClientSynchronizationResult expired = await new LocalGameClient(new FixedTransport(new EngineResponse(EngineProtocol.Version, "local-sync", "shared-table", Capability: null, Prompt: null, Events: [], Error: new EngineError("session_not_found", secret)))).SynchronizeAsync(new ClientSession("shared-table", secret), TestContext.Current.CancellationToken);
        Assert.Equal("invalid_response", malformed.Error?.Code);
        Assert.Equal(ClientSessionDisposition.Active, malformed.SessionDisposition);
        Assert.Equal("transport_unavailable", lost.Error?.Code);
        Assert.Equal(ClientSessionDisposition.Active, lost.SessionDisposition);
        Assert.Equal("session_unavailable", expired.Error?.Code);
        Assert.Equal(ClientSessionDisposition.Unavailable, expired.SessionDisposition);
        Assert.DoesNotContain(secret, lost.Error?.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, expired.Error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PrecommitResolveFailureIsNotSentAndDoesNotSynchronize()
    {
        var transport = new ScriptedTransport(new EngineTransportException(requestMayHaveCommitted: false, new IOException("private diagnostic")));
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(new ClientSession("shared-table", "secret"), EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.Equal(ClientMutationDisposition.NotSent, result.MutationDisposition);
        Assert.Equal(ClientSessionDisposition.Active, result.SessionDisposition);
        Assert.Equal("transport_unavailable", result.Error?.Code);
        Assert.Single(transport.Requests);
        Assert.Equal(EngineProtocol.Resolve, transport.Requests[0].Operation);
    }
}
