using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;
public sealed class TransportSetupDiscoveryTests : TransportTestBase
{
    [Fact]
    public async Task SetupDiscoveryHasSocketAndInProcessSerializationParity()
    {
        var host = new EngineHost(DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root));
        var request = EngineRequest.ReadSetup("choices");
        var local = new InProcessTransport(host);
        var server = new SocketEngineServer(host, IPAddress.Loopback, port: 0);
        EngineResponse inProcess = await local.ExchangeAsync(request, TestContext.Current.CancellationToken);
        EngineResponse socket = await ExchangeOverSocket(server, request);
        Assert.Equal(EngineJson.Write(inProcess), EngineJson.Write(socket));
        Assert.NotNull(socket.Setup);
    }

    [Fact]
    public async Task SocketAndInProcessTransportsExposeTheSameContract()
    {
        var request = EngineRequest.ResolveGame("correlation", "game", "capability", new EngineDecision(4, [11], [7]));
        var expected = new EngineResponse(EngineProtocol.Version, request.RequestId, request.GameId, Capability: null, new Prompt(0, Question.TurnOption, TimingPriority.Untimed, "WhenPlayerInTurn", "Choose", Cancellable: true, []), [new FieldSet(11, "damage", 0, 1) { Trigger = "WhenPlayerInTurn", Verb = "Attack" }]);
        var endpoint = new EchoEndpoint(request, expected);
        var inProcess = new InProcessTransport(endpoint);
        Assert.NotEmpty(EngineJson.Write(expected));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = new SocketEngineServer(endpoint, IPAddress.Loopback, port: 0);
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            server.Serve(accepted);
        }, TestContext.Current.CancellationToken);
        var socket = new SocketTransport(IPAddress.Loopback.ToString(), port);
        EngineResponse remote;
        try
        {
            remote = await socket.ExchangeAsync(request, TestContext.Current.CancellationToken);
        }
        finally
        {
            await serving;
        }

        Assert.Equal(EngineJson.Write(await inProcess.ExchangeAsync(request, TestContext.Current.CancellationToken)), EngineJson.Write(remote));
        Assert.Equal(2, endpoint.Calls);
    }

    [Fact]
    public async Task SocketTransportPreservesAnUnknownFutureResponseVersion()
    {
        int futureVersion = EngineProtocol.Version + 1;
        byte[] futureResponse = Encoding.UTF8.GetBytes("{\"version\":" + futureVersion + ",\"request_id\":\"future\",\"game_id\":\"\",\"capability\":null," + "\"prompt\":null,\"events\":[],\"future_field\":{\"shape\":\"unknown\"}}");
        Assert.Throws<JsonException>(() => EngineJson.ReadResponse(futureResponse));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            Assert.NotNull(SocketFrame.Read(stream));
            SocketFrame.Write(stream, futureResponse);
        }, TestContext.Current.CancellationToken);
        var transport = new SocketTransport(IPAddress.Loopback.ToString(), port);
        EngineResponse response;
        try
        {
            response = await transport.ExchangeAsync(EngineRequest.ReadSetup("future"), TestContext.Current.CancellationToken);
        }
        finally
        {
            await serving;
        }

        Assert.Equal(futureVersion, response.Version);
        Assert.Equal("future", response.RequestId);
        Assert.Equal(string.Empty, response.GameId);
    }

    [Fact]
    public void DecisionsHaveFiveWireFieldsAndNoConvenienceGetters()
    {
        var request = EngineRequest.ResolveGame("wire", "game", "capability", new EngineDecision(4, [11], [7], new Dictionary<string, long>(StringComparer.Ordinal) { ["X"] = 2 }, [new ResourceAllocation(7, 0, "M")]));
        using JsonDocument document = JsonDocument.Parse(EngineJson.Write(request));
        JsonElement decision = document.RootElement.GetProperty("decision");
        Assert.Equal(["affordance", "targets", "resources", "values", "allocations"], decision.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public void UnknownWireFieldsAreRejected()
    {
        byte[] request = Encoding.UTF8.GetBytes($$"""
            {"version":{{EngineProtocol.Version}},"request_id":"r","operation":"resolve","game_id":"g","decision":{"affordance":-1,"targets":[]},"surprise":true}
            """);
        Assert.Throws<JsonException>(() => EngineJson.ReadRequest(request));
    }

    [Fact]
    public void HistoryCommandsHaveAnExplicitDirectionRevisionAndCursor()
    {
        EngineRequest undo = EngineRequest.UndoGame("undo", "game", "capability", cursor: 2, expectedRevision: 11);
        EngineRequest redo = EngineRequest.RedoGame("redo", "game", "capability", cursor: 4, expectedRevision: 12);
        EngineRequest undoAgain = EngineJson.ReadRequest(EngineJson.Write(undo));
        EngineRequest redoAgain = EngineJson.ReadRequest(EngineJson.Write(redo));
        Assert.Equal(EngineProtocol.Undo, undoAgain.Operation);
        Assert.Equal(11, undoAgain.ExpectedRevision);
        Assert.Equal(2, undoAgain.Cursor);
        Assert.Null(undoAgain.Decision);
        Assert.Equal(EngineProtocol.Redo, redoAgain.Operation);
        Assert.Equal(12, redoAgain.ExpectedRevision);
        Assert.Equal(4, redoAgain.Cursor);
        Assert.Null(redoAgain.Decision);
    }

    [Fact]
    public void HistoryResponsesCarryCompletedActionsAtTheirBeforeCursor()
    {
        var response = new EngineResponse(EngineProtocol.Version, "history", "game", Capability: null, Prompt: null, Events: [], History: new HistoryDescriptor(2, [0, 1], [], [new HistoryEntryDescriptor(0, "Spider-Man kept their opening hand.", []), new HistoryEntryDescriptor(1, "Spider-Man changed form.", []), ], ActionOpen: false));
        EngineResponse restored = EngineJson.ReadResponse(EngineJson.Write(response));
        Assert.Equal(response.History!.Cursor, restored.History!.Cursor);
        Assert.Equal(response.History.Undo, restored.History.Undo);
        Assert.Equal(response.History.Redo, restored.History.Redo);
        Assert.Equal(response.History.Entries.Select(entry => (entry.Cursor, entry.Summary)), restored.History.Entries.Select(entry => (entry.Cursor, entry.Summary)));
        Assert.Equal(response.History.Entries.SelectMany(entry => entry.Details), restored.History.Entries.SelectMany(entry => entry.Details));
        Assert.Equal(response.History.ActionOpen, restored.History.ActionOpen);
        Assert.Equal(1, restored.History.Entries[1].Cursor);
    }

    [Fact]
    public void ReorderCarriesOnlyRevisionAndOriginalUnitPositions()
    {
        EngineRequest request = EngineRequest.ReorderGame("reorder", "game", "capability", [4, 2, 3], expectedRevision: 17);
        EngineRequest again = EngineJson.ReadRequest(EngineJson.Write(request));
        Assert.Equal(EngineProtocol.Reorder, again.Operation);
        Assert.Equal(17, again.ExpectedRevision);
        Assert.Equal([4, 2, 3], again.Order);
        Assert.Null(again.Cursor);
        Assert.Null(again.Decision);
    }

    [Fact]
    public void TheWireCarriesAFilteredWorldAndNeverAStateDigest()
    {
        var response = new EngineResponse(EngineProtocol.Version, "request", "game", Capability: null, Prompt: null, Events: [], World: new WorldDescriptor([], [], [], Outcome.Unfinished));
        string json = Encoding.UTF8.GetString(EngineJson.Write(response));
        Assert.Contains("\"world\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("digest", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("audience", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StateDigest", ResponseTypeNames(), StringComparison.Ordinal);
        Assert.DoesNotContain("Marvel.Rules.State.World", ResponseTypeNames(), StringComparison.Ordinal);
    }

    [Fact]
    public void CurrentProtocolRoundTripsEveryTopologyEventKind()
    {
        var joined = new PlayAreaJoined(1, 4)
        {
            Trigger = "test",
            Verb = "Join",
        };
        var detached = new PlayAreaDetached(1, 4)
        {
            Trigger = "test",
            Verb = "Detach",
        };
        var response = new EngineResponse(EngineProtocol.Version, "topology", "game", Capability: null, Prompt: null, Events: [joined, detached]);
        var again = EngineJson.ReadResponse(EngineJson.Write(response));
        Assert.Equal(EngineProtocol.Version, again.Version);
        Assert.Collection(again.Events, happened => Assert.Equal(joined, Assert.IsType<PlayAreaJoined>(happened)), happened => Assert.Equal(detached, Assert.IsType<PlayAreaDetached>(happened)));
    }

    [Fact]
    public void RepeatedTargetCapacitiesHaveAPinnedWireField()
    {
        var response = new EngineResponse(EngineProtocol.Version, "targets", "game", Capability: null, Prompt: new Prompt(0, Question.Element, TimingPriority.Untimed, "Indirect_Damage", "Assign damage", Cancellable: false, [new Affordance(7, "Choose", 7, World.Scenario, "indirectDamage", new TargetRequest([11, 12], 3, 3, Rule: "rr:indirect-damage.1", AllowRepeated: true, MaximumOccurrences: new Dictionary<int, int> { [11] = 1, [12] = 2, }))]), Events: []);
        using JsonDocument document = JsonDocument.Parse(EngineJson.Write(response));
        JsonElement targets = document.RootElement.GetProperty("prompt").GetProperty("affordances")[0].GetProperty("targets");
        Assert.Equal(1, targets.GetProperty("maximum_occurrences").GetProperty("11").GetInt32());
        Assert.Equal(2, targets.GetProperty("maximum_occurrences").GetProperty("12").GetInt32());
        EngineResponse restored = EngineJson.ReadResponse(EngineJson.Write(response));
        Assert.Equal(2, restored.Prompt!.Affordances[0].Targets!.MaximumOccurrences![12]);
    }

    [Fact]
    public void WildDeclarationSensitivityHasAPinnedWireField()
    {
        var response = new EngineResponse(EngineProtocol.Version, "wild-declaration", "game", Capability: null, Prompt: new Prompt(0, Question.Element, TimingPriority.Untimed, "PlayCard", "Pay", Cancellable: false, [new Affordance(7, "Play", 20, World.Scenario, "Play card", Costs: [new CostOption(20, "1", Sources: [new ResourceSource(40, "W")], DeclarationSensitive: true), ])]), Events: []);
        using JsonDocument document = JsonDocument.Parse(EngineJson.Write(response));
        JsonElement cost = document.RootElement.GetProperty("prompt").GetProperty("affordances")[0].GetProperty("costs")[0];
        Assert.True(cost.GetProperty("declaration_sensitive").GetBoolean());
        EngineResponse restored = EngineJson.ReadResponse(EngineJson.Write(response));
        Assert.True(restored.Prompt!.Affordances[0].CostOptions[0].DeclarationSensitive);
    }

    [Fact]
    public void FramingPinsTheLengthAndRejectsOversizedPayloads()
    {
        using var frame = new MemoryStream();
        SocketFrame.Write(frame, [1, 2, 3]);
        Assert.Equal([0, 0, 0, 3, 1, 2, 3], frame.ToArray());
        Assert.Throws<InvalidDataException>(() => SocketFrame.Write(Stream.Null, new byte[SocketFrame.MaximumPayload + 1]));
    }

    [Fact]
    public void ATruncatedFrameIsNeverTreatedAsARequest()
    {
        using var frame = new MemoryStream([0, 0, 0, 2, 1]);
        Assert.Throws<EndOfStreamException>(() => SocketFrame.Read(frame));
    }
}
