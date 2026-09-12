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
public abstract class LocalGameClientTestBase
{
    protected static GameSetupSelection DefaultSelection(SetupChoices choices) => new([choices.Heroes.Single(choice => choice.Key == "spider_man").Key], choices.Scenarios.Single(choice => choice.Key == "rhino").Key, ModularConfiguration.Recommended, ModularKeys: [], Seed: "7");
    protected static EngineDecision VisibleDecision(Prompt prompt)
    {
        var composer = new DecisionComposer(prompt);
        if (prompt.Cancellable)
        {
            Assert.True(composer.TryDecline(out EngineDecision? declined, out _));
            return declined!;
        }

        Affordance offered = prompt.Affordances.First(option => option.IsLegal);
        composer.SelectAffordance(offered.Id);
        Assert.True(composer.TryBuild(out EngineDecision? submitted, out string? error), error);
        return submitted!;
    }

    protected static void AssertEquivalentResponses(EngineResponse local, EngineResponse remote) => Assert.Equal(EngineJson.Write(local with { Capability = null, RequestId = "request" }), EngineJson.Write(remote with { Capability = null, RequestId = "request" }));
    protected static GameSpecification Specification() => new("rhino", ["spider_man"], ModularSets: null, Seed: 7);
    protected static SetupChoices Choices() => Assert.IsType<SetupChoices>(Host().Exchange(EngineRequest.ReadSetup("choices")).Setup);
    protected static EngineHost Host() => new(DatasetGameFactory.Load(RepositoryPaths.Root), new FixedCapabilityIssuer());
    protected sealed class FixedCapabilityIssuer : ISessionCapabilityIssuer
    {
        public string Issue() => "development-capability";
    }

    protected sealed class SequenceCapabilityIssuer(params string[] capabilities) : ISessionCapabilityIssuer
    {
        private readonly Queue<string> remaining = new(capabilities);
        public string Issue() => remaining.Dequeue();
    }

    protected sealed class FixedTransport(EngineResponse response) : IEngineTransport
    {
        public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default) => ValueTask.FromResult(response);
    }

    protected sealed class CapturingTransport : IEngineTransport
    {
        public List<EngineRequest> Requests { get; } = [];

        public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return ValueTask.FromResult(new EngineResponse(EngineProtocol.Version, request.RequestId, request.GameId, Capability: null, Prompt: null, Events: [], Error: new EngineError("stopped", "capture complete")));
        }
    }

    protected sealed class RecoveringTransport(EngineResponse current) : IEngineTransport
    {
        public List<EngineRequest> Requests { get; } = [];

        public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            if (Requests.Count == 1)
            {
                return ValueTask.FromException<EngineResponse>(new IOException("response lost"));
            }

            return ValueTask.FromResult(current with { RequestId = request.RequestId, GameId = request.GameId, Capability = null, Events = [], });
        }
    }

    protected sealed class CollectingOperationalSink : IOperationalSink
    {
        private readonly ConcurrentQueue<OperationalRecord> records = new();
        public IReadOnlyList<OperationalRecord> Records => [..records];

        public void Write(OperationalRecord record) => records.Enqueue(record);
    }

    protected sealed class ScriptedTransport(params object[] results) : IEngineTransport
    {
        private readonly Queue<object> remaining = new(results);
        public List<EngineRequest> Requests { get; } = [];

        public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            object result = remaining.Dequeue();
            return result is Exception failure ? ValueTask.FromException<EngineResponse>(failure) : ValueTask.FromResult((EngineResponse)result);
        }
    }

    protected sealed class FailingTransport(string message) : IEngineTransport
    {
        public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default) => ValueTask.FromException<EngineResponse>(new IOException(message));
    }

    protected sealed class CommitAwareCancellingTransport(EngineResponse response, CancellationTokenSource cancellation) : IEngineTransport
    {
        public List<EngineRequest> Requests { get; } = [];
        public CancellationToken ReceivedToken { get; private set; }

        public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            ReceivedToken = cancellationToken;
            cancellation.Cancel();
            return ValueTask.FromResult(response);
        }
    }
}
