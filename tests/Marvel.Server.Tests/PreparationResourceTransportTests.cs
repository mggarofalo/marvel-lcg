using System.Net;
using Marvel.Protocol;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Transport;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PreparationResourceTransportTests : TransportTestBase
{
    [Fact]
    public async Task BothTransportsPreservePreparationRestrictionAndSourceCommitmentDescription()
    {
        // Synthetic wire contract fixture; does not admit Black Widow content.
        var source = new ResourceSource(41, "G")
        {
            Restriction = ResourceRestriction.PreparationCard,
            CostDescription = "Exhaust Black Widow's Gauntlet; For a Preparation card's resource or ability cost",
        };
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "Play", "Pay", false, [new Affordance(7, "Play", 20, 0, "Grappling Hook",
                Costs: [new CostOption(20, "1", Sources: [source])])]);
        var request = EngineRequest.SyncGame("preparation", "table", "capability");
        var response = new EngineResponse(EngineProtocol.Version, request.RequestId,
            request.GameId, null, prompt, []);
        // The appended numeric value is an engine wire choice. Prior enum values remain stable.
        Assert.Equal(4, (int)ResourceRestriction.PreparationCard);
        Assert.Contains("\"restriction\":4", System.Text.Encoding.UTF8.GetString(EngineJson.Write(response)),
            StringComparison.Ordinal);
        var endpoint = new EchoEndpoint(request, response);
        var local = await new InProcessTransport(endpoint).ExchangeAsync(request, TestContext.Current.CancellationToken);
        var socket = await ExchangeOverSocket(new SocketEngineServer(endpoint, IPAddress.Loopback, 0), request);
        Assert.Equal(EngineJson.Write(local), EngineJson.Write(socket));
        foreach (var restored in new[] { local, socket })
            Assert.Equal(source, Assert.Single(Assert.Single(restored.Prompt!.Affordances).CostOptions[0].Generators));
    }
}
