using System.Net;
using System.Text.Json;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class DeferredTargetTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BothTransportsAndViewPreserveTheEngineBoundaryWithoutInventingTargets(bool deferred)
    {
        // A contract fixture keeps event kind, command verb and current target
        // request identical: only the engine-authored continuation fact differs.
        var offer = new Affordance(7, "Action", 4, 0, "Action")
        {
            PlaysCard = true,
            DeferredTargetSelection = deferred,
            CommitLabel = "Attack Rhino",
            CostDescription = "Exhaust the offered source",
        };
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "Turn", "Choose", false, [offer]);
        var world = new WorldDescriptor([], [], [], Outcome.Unfinished);
        var request = EngineRequest.ResolveGame("boundary", "game", "capability", new EngineDecision(7, []));
        var response = new EngineResponse(EngineProtocol.Version, request.RequestId,
            request.GameId, Capability: null, prompt, Events: [], world);
        var endpoint = new EchoEndpoint(request, response);
        var local = new InProcessTransport(endpoint);
        var server = new SocketEngineServer(endpoint, IPAddress.Loopback, port: 0);

        EngineResponse inProcess = await local.ExchangeAsync(request, TestContext.Current.CancellationToken);
        EngineResponse socket = await ExchangeOverSocket(server, request);

        Assert.Equal(EngineJson.Write(inProcess), EngineJson.Write(socket));
        using JsonDocument json = JsonDocument.Parse(EngineJson.Write(socket));
        Assert.Equal(deferred, json.RootElement.GetProperty("prompt").GetProperty("affordances")[0]
            .GetProperty("deferred_target_selection").GetBoolean());
        Assert.Equal(deferred, Assert.Single(socket.Prompt!.Affordances).DeferredTargetSelection);
        AffordancePresentation presented = Assert.Single(PromptPresentation.From(socket.Prompt, socket.World!).Affordances);
        Assert.Equal(deferred, presented.DeferredTargetSelection);
        Assert.Equal("Attack Rhino", presented.CommitLabel);
        Assert.True(presented.PlaysCard);
        Assert.Equal("Exhaust the offered source", presented.CostDescription);
        Assert.Equal("Attack Rhino", Assert.Single(socket.Prompt.Affordances).CommitLabel);
        Assert.Equal("Attack Rhino", json.RootElement.GetProperty("prompt").GetProperty("affordances")[0]
            .GetProperty("commit_label").GetString());
        Assert.Null(presented.TargetRequest);
        Assert.Empty(presented.Relationships);
    }
}
