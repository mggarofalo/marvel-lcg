using Marvel.Transport;
using Marvel.Protocol;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EmbeddedTransportIsolationTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConsumerEditsCannotChangeEndpointOwnedNestedResponseValues(bool queued)
    {
        // Synthetic wire values isolate the ownership contract from game legality.
        var targets = new TargetRequest([1, 2], 1, 1, Groups: [[1]],
            MaximumOccurrences: new Dictionary<int, int> { [1] = 1 })
        {
            Details = new Dictionary<int, string> { [1] = "original" },
            ExclusiveSets = [[1, 2]],
        };
        var cost = new CostOption(1, "1", Rule: ["E"], Sources: [new(3, "E")],
            Components: [new ResourceCost("1", ["M"])]);
        var option = new Affordance(7, "Action", 1, 0, "Choose", targets, [cost])
        {
            WindowChoices = [new(null, "Choose", null) { RecipientIds = [1, 2] }],
        };
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "WhenPlayerInTurn", "Choose", true, [option])
        {
            ContextCardIds = [1],
            ExposesConcealedCandidates = true,
        };
        var response = new EngineResponse(EngineProtocol.Version, "request", "game", null,
            prompt, [new FieldSet(1, "damage", 0, 1)]);
        byte[] original = EngineJson.Write(response);
        var endpoint = new SequenceEndpoint(response);
        await using var owner = queued ? new QueuedInProcessTransport(endpoint) : null;
        IEngineTransport transport = owner ?? (IEngineTransport)new InProcessTransport(endpoint);

        EngineResponse received = await transport.ExchangeAsync(
            EngineRequest.ReadSetup("request"), TestContext.Current.CancellationToken);
        var receivedPrompt = Assert.IsType<Prompt>(received.Prompt);
        var receivedOption = Assert.Single(receivedPrompt.Affordances);
        var receivedTargets = Assert.IsType<TargetRequest>(receivedOption.Targets);
        Mutable(receivedTargets.Legal)[0] = 99;
        Mutable(receivedTargets.Groups![0])[0] = 99;
        Mutable(receivedTargets.ExclusiveSets![0])[0] = 99;
        Assert.IsAssignableFrom<IDictionary<int, int>>(receivedTargets.MaximumOccurrences)[1] = 99;
        Assert.IsAssignableFrom<IDictionary<int, string>>(receivedTargets.Details)[1] = "changed";
        var receivedCost = Assert.Single(receivedOption.Costs!);
        Mutable(receivedCost.Rule!)[0] = "P";
        Mutable(receivedCost.Sources!)[0] = new ResourceSource(99, "W");
        Mutable(receivedCost.Components![0].Rule!)[0] = "P";
        Mutable(receivedOption.WindowChoices[0].RecipientIds)[0] = 99;
        Mutable(receivedPrompt.ContextCardIds)[0] = 99;
        Mutable(received.Events)[0] = new FieldSet(99, "damage", 0, 99);

        Assert.Equal(original, EngineJson.Write(response));
        Assert.False(receivedPrompt.ExposesConcealedCandidates);
        Assert.True(prompt.ExposesConcealedCandidates);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EditingReceivedMulliganTargetsCannotAuthorizeAnInjectedTarget(bool queued)
    {
        var endpoint = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        await using var owner = queued ? new QueuedInProcessTransport(endpoint) : null;
        IEngineTransport transport = owner ?? (IEngineTransport)new InProcessTransport(endpoint);
        EngineResponse opened = await transport.ExchangeAsync(EngineRequest.OpenGame(
            "open", "isolated", new GameSpecification("rhino", ["spider_man"], [], 7)),
            TestContext.Current.CancellationToken);
        Assert.Null(opened.Error);
        var option = Assert.Single(Assert.IsType<Prompt>(opened.Prompt).Affordances);
        var targets = Assert.IsType<TargetRequest>(option.Targets);
        int[] originalTargets = [.. targets.Legal];
        const int injected = int.MaxValue;
        Mutable(targets.Legal)[0] = injected;

        EngineResponse synchronized = await transport.ExchangeAsync(EngineRequest.SyncGame(
            "sync", "isolated", opened.Capability!), TestContext.Current.CancellationToken);
        Assert.Equal(originalTargets, Assert.Single(synchronized.Prompt!.Affordances).Targets!.Legal);
        EngineResponse rejected = await transport.ExchangeAsync(EngineRequest.ResolveGame(
            "inject", "isolated", opened.Capability!, new Decision(option.Id, [injected]), opened.Revision),
            TestContext.Current.CancellationToken);
        Assert.Equal("invalid_decision", rejected.Error?.Code);
        EngineResponse unchanged = await transport.ExchangeAsync(EngineRequest.SyncGame(
            "unchanged", "isolated", opened.Capability!), TestContext.Current.CancellationToken);
        Assert.Equal(opened.Revision, unchanged.Revision);
        Assert.Equal(originalTargets, Assert.Single(unchanged.Prompt!.Affordances).Targets!.Legal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EndpointEditsCannotChangeTheCallersSubmittedDecision(bool queued)
    {
        var decision = new Decision(7, [1], [2], new Dictionary<string, long> { ["X"] = 3 });
        var request = EngineRequest.ResolveGame("request", "game", "authority", decision);
        byte[] original = EngineJson.Write(request);
        var endpoint = new EditingEndpoint();
        await using var owner = queued ? new QueuedInProcessTransport(endpoint) : null;
        IEngineTransport transport = owner ?? (IEngineTransport)new InProcessTransport(endpoint);

        await transport.ExchangeAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(original, EngineJson.Write(request));
        Assert.Equal(1, endpoint.Calls);
    }

    private static IList<T> Mutable<T>(IReadOnlyList<T> values) => Assert.IsAssignableFrom<IList<T>>(values);

    private sealed class EditingEndpoint : IEngineEndpoint
    {
        internal int Calls { get; private set; }

        public EngineResponse Exchange(EngineRequest request)
        {
            Calls++;
            Mutable(request.Decision!.Targets)[0] = 99;
            Mutable(request.Decision.Resources!)[0] = 99;
            Assert.IsAssignableFrom<IDictionary<string, long>>(request.Decision.Values!)["X"] = 99;
            return new(EngineProtocol.Version, request.RequestId, request.GameId, null, null, []);
        }
    }
}
