using System.Net;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class VariablePaymentTransportTests : TransportTestBase
{
    [Fact]
    public async Task BothTransportsPreserveEveryTypedSlotAndWildDeclaration()
    {
        // Synthetic contract fixture: X energy requires X energy declarations.
        var cost = new CostOption(0, "X",
            Sources: [new ResourceSource(1, "Y"), new ResourceSource(2, "B"), new ResourceSource(3, "G")],
            Variables: [new VariableRequest("X", 1, 2)],
            Components: [new ResourceCost("X") { RepeatedResource = Resources.Energy }]);
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "Action", "Pay", false, [new Affordance(7, "Action", 4, 0, "Pay", Costs: [cost])]);
        var request = EngineRequest.SyncGame("variable", "table", "capability");
        var response = new EngineResponse(EngineProtocol.Version, request.RequestId,
            request.GameId, Capability: null, prompt, Events: []);
        var endpoint = new EchoEndpoint(request, response);
        EngineResponse local = await new InProcessTransport(endpoint).ExchangeAsync(
            request, TestContext.Current.CancellationToken);
        EngineResponse socket = await ExchangeOverSocket(
            new SocketEngineServer(endpoint, IPAddress.Loopback, 0), request);

        Assert.Equal(EngineJson.Write(local), EngineJson.Write(socket));
        foreach (EngineResponse restored in new[] { local, socket })
        {
            var composer = new DecisionComposer(restored.Prompt!);
            composer.SelectAffordance(7);
            composer.Define("X", 2);
            composer.ToggleResource(1);
            Assert.False(DecisionResourceEligibility.CanToggle(composer, 2));
            Assert.False(composer.TryBuild(out _, out _));
            composer.ToggleResource(3);
            Assert.True(composer.TryBuild(out var answer, out var error), error);
            Assert.All(answer!.Allocations!, allocation => Assert.Equal("Y", allocation.PaidAs));
            Assert.Equal(2, answer.Values!["X"]);
        }
    }
}
