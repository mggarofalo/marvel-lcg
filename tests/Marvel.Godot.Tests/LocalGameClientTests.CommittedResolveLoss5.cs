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
public sealed class LocalGameClientCommittedResolveLossTests : LocalGameClientTestBase
{
    [Fact]
    public async Task CommittedResolveLossIsUncertainAndSynchronizesExactlyOnce()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        var transport = new ScriptedTransport(new EngineTransportException(requestMayHaveCommitted: true, new IOException("response lost")), current with { RequestId = "local-recover", Capability = null, Events = [] });
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(new ClientSession("shared-table", current.Capability!), EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal(ClientSessionDisposition.Active, result.SessionDisposition);
        Assert.True(result.HasAuthoritativeView);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync], transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task ServerRefusalIsRejectedAndRecoversWithoutRepeatingDecision()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("source", "shared-table", Specification()));
        var transport = new ScriptedTransport(current with { RequestId = "local-resolve", Capability = null, Prompt = null, World = null, Events = [], Error = new EngineError("stale_decision", "The prompt changed."), }, current with { RequestId = "local-recover", Capability = null, Events = [] });
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(new ClientSession("shared-table", current.Capability!), EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.Equal(ClientMutationDisposition.Rejected, result.MutationDisposition);
        Assert.Equal(ClientSessionDisposition.Active, result.SessionDisposition);
        Assert.True(result.HasAuthoritativeView);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync], transport.Requests.Select(request => request.Operation));
    }

    [Theory]
    [InlineData("session_not_found", ClientMutationDisposition.Rejected)]
    [InlineData("game_aborted", ClientMutationDisposition.Uncertain)]
    public async Task ResolveUnavailableErrorsDoNotEchoSecretsOrRetry(string code, ClientMutationDisposition expectedMutation)
    {
        const string secret = "private-session-capability";
        var transport = new ScriptedTransport(new EngineResponse(EngineProtocol.Version, "local-resolve", "shared-table", Capability: null, Prompt: null, Events: [], Error: new EngineError(code, secret)));
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(new ClientSession("shared-table", secret), EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.Equal(expectedMutation, result.MutationDisposition);
        Assert.Equal(ClientSessionDisposition.Unavailable, result.SessionDisposition);
        Assert.Equal("session_unavailable", result.Error?.Code);
        Assert.DoesNotContain(secret, result.Error?.Message, StringComparison.Ordinal);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task RecoveryExpirationRetainsTheUncertainMutationDisposition()
    {
        var transport = new ScriptedTransport(new IOException("response lost"), new EngineResponse(EngineProtocol.Version, "local-recover", "shared-table", Capability: null, Prompt: null, Events: [], Error: new EngineError("session_not_found", "private diagnostic")));
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(new ClientSession("shared-table", "secret"), EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal(ClientSessionDisposition.Unavailable, result.SessionDisposition);
        Assert.Equal("session_unavailable", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync], transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task UnknownOutcomesAndMalformedEventsAreNeverRendered()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame("local-open", LocalGameSession.GameId, Specification()));
        EngineResponse[] malformed = [complete with
        {
            World = complete.World!with
            {
                Outcome = (Outcome)999
            }
        }, complete with
        {
            Events = [new FieldSet(1, null!, From: 0, To: 1)],
        }, complete with
        {
            Events = [new CardsFlipped(null!, FaceUp: true)],
        }, ];
        foreach (EngineResponse response in malformed)
        {
            ClientStartupResult result = await new LocalGameClient(new FixedTransport(response)).OpenAsync(Specification(), TestContext.Current.CancellationToken);
            Assert.Equal("invalid_response", result.Error?.Code);
        }
    }
}
