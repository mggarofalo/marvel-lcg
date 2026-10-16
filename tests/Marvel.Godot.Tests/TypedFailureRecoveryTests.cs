using Marvel.Client;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TypedFailureRecoveryTests
{
    [Theory]
    [InlineData("game_aborted")]
    [InlineData("session_not_found")]
    public async Task RejectedCandidateKeepsAuthorityRegardlessOfDiagnosticCode(string code)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "typed",
            new GameSpecification("rhino", ["spider_man"], [], 91)));
        var transport = new RejectionThenSync(host, code, EngineFailureDisposition.Rejected);
        using var client = new LocalGameClient(transport);
        ClientResolutionResult result = await client.ResolveAsync(new ClientSession("typed", opened.Capability!),
            new Decision(Assert.Single(opened.Prompt!.Affordances).Id, []), TestContext.Current.CancellationToken);
        Assert.Equal(ClientSessionDisposition.Active, result.SessionDisposition);
        Assert.Equal(ClientMutationDisposition.Rejected, result.MutationDisposition);
        Assert.True(result.HasAuthoritativeView);
        Assert.Equal(0, result.Response!.Revision);
        Assert.Equal(2, transport.Calls);
    }

    [Fact]
    public async Task ExplicitUnavailableDispositionRevokesClientAuthorityWithoutInspectingCode()
    {
        var transport = new RejectionThenSync(null, "new_diagnostic", EngineFailureDisposition.SessionUnavailable);
        using var client = new LocalGameClient(transport);
        ClientResolutionResult result = await client.ResolveAsync(new ClientSession("typed", "capability"),
            Decision.Decline, TestContext.Current.CancellationToken);
        Assert.Equal(ClientSessionDisposition.Unavailable, result.SessionDisposition);
        Assert.Equal(1, transport.Calls);
    }

    private sealed class RejectionThenSync(IEngineEndpoint? host, string code,
        EngineFailureDisposition disposition) : IEngineTransport
    {
        public int Calls { get; private set; }
        public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return ValueTask.FromResult(request.Operation == EngineProtocol.Sync ? host!.Exchange(request)
                : new EngineResponse(EngineProtocol.Version, request.RequestId, request.GameId, null, null, [],
                    Error: new EngineError(code, "recovery contract", disposition)));
        }
    }
}
