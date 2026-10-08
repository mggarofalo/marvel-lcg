using System.Net;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class TargetExclusionTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BothTransportsRetainTheSameTargetCombinationLegality(bool socket)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "exclusions",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7)));
        // Synthetic prompt contract: two individually offered copies cannot be combined.
        Affordance offer = opened.Prompt!.Affordances[0] with
        { Targets = new TargetRequest([1, 2, 3], 1, 3) { ExclusiveSets = [[1, 2]] } };
        EngineRequest request = EngineRequest.SyncGame("sync", "exclusions", opened.Capability!);
        var endpoint = new EchoEndpoint(request, opened with
        { RequestId = request.RequestId, Prompt = opened.Prompt with { Affordances = [offer] } });

        EngineResponse received = socket
            ? await ExchangeOverSocket(new SocketEngineServer(endpoint, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(endpoint).ExchangeAsync(request, TestContext.Current.CancellationToken);

        TargetRequest targets = Assert.Single(received.Prompt!.Affordances).Targets!;
        Assert.Equal([1, 2], Assert.Single(targets.ExclusiveSets!));
        Assert.True(targets.Allows([1, 3]));
        Assert.True(targets.Allows([2, 3]));
        Assert.False(targets.Allows([1, 2]));
    }
}
