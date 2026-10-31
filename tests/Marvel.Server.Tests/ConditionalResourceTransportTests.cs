using System.Net;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Transport;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ConditionalResourceTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConditionalQuoteSurvivesEitherTransport(bool socket)
    {
        var source = new ResourceSource(7, "G") { ConditionalBonus = new("Y", 3) };
        var price = new CostOption(3, "2", Sources: [source], Components: [new("1"), new("1")]);
        var prompt = new Prompt(0, Question.Opportunity, TimingPriority.Untimed, "test", "test", false,
            [new Affordance(4, "Play", 3, 0, "Play", Costs: [price])]);
        var request = EngineRequest.SyncGame("conditional", "game", "capability");
        var expected = new EngineResponse(EngineProtocol.Version, request.RequestId, request.GameId, null, prompt, []);
        var endpoint = new EchoEndpoint(request, expected);
        var actual = socket
            ? await ExchangeOverSocket(new SocketEngineServer(endpoint, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(endpoint).ExchangeAsync(request, TestContext.Current.CancellationToken);
        var received = Assert.Single(Assert.Single(actual.Prompt!.Affordances[0].CostOptions).Generators);
        Assert.Equal("G", received.Generates);
        Assert.Equal(new ConditionalResourceBonus("Y", 3), received.ConditionalBonus);
        Assert.Equal(1, endpoint.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Protocol42IsRejectedBeforeGameCreation(bool socket)
    {
        var host = new EngineHost(new UnusedFactory());
        var request = EngineRequest.OpenGame("old", "game",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7)) with { Version = 42 };
        var response = socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal("unsupported_version", response.Error?.Code);
        Assert.Equal(44, response.Version);
        Assert.Null(response.World);
        Assert.Null(response.Prompt);
    }
}
