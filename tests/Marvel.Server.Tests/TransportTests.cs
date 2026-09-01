using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class TransportTests
{
    [Fact]
    public async Task SetupDiscoveryHasSocketAndInProcessSerializationParity()
    {
        var host = new EngineHost(
            DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root));
        var request = EngineRequest.ReadSetup("choices");
        var local = new InProcessTransport(host);
        var server = new SocketEngineServer(host, IPAddress.Loopback, port: 0);

        EngineResponse inProcess = await local.ExchangeAsync(
            request, TestContext.Current.CancellationToken);
        EngineResponse socket = await ExchangeOverSocket(server, request);

        Assert.Equal(EngineJson.Write(inProcess), EngineJson.Write(socket));
        Assert.NotNull(socket.Setup);
    }

    [Fact]
    public async Task SocketAndInProcessTransportsExposeTheSameContract()
    {
        var request = EngineRequest.ResolveGame(
            "correlation", "game", "capability", new EngineDecision(4, [11], [7]));
        var expected = new EngineResponse(
            EngineProtocol.Version,
            request.RequestId,
            request.GameId,
            Capability: null,
            new Prompt(
                0, Question.TurnOption, TimingPriority.Untimed, "WhenPlayerInTurn",
                "Choose", Cancellable: true, []),
            [new FieldSet(11, "damage", 0, 1) { Trigger = "WhenPlayerInTurn", Verb = "Attack" }]);
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
            remote = await socket.ExchangeAsync(
                request, TestContext.Current.CancellationToken);
        }
        finally
        {
            await serving;
        }

        Assert.Equal(
            EngineJson.Write(await inProcess.ExchangeAsync(
                request, TestContext.Current.CancellationToken)),
            EngineJson.Write(remote));
        Assert.Equal(2, endpoint.Calls);
    }

    [Fact]
    public void DecisionsHaveFiveWireFieldsAndNoConvenienceGetters()
    {
        var request = EngineRequest.ResolveGame(
            "wire",
            "game",
            "capability",
            new EngineDecision(
                4,
                [11],
                [7],
                new Dictionary<string, long>(StringComparer.Ordinal) { ["X"] = 2 },
                [new ResourceAllocation(7, 0, "M")]));

        using JsonDocument document = JsonDocument.Parse(EngineJson.Write(request));
        JsonElement decision = document.RootElement.GetProperty("decision");

        Assert.Equal(
            ["affordance", "targets", "resources", "values", "allocations"],
            decision.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public void UnknownWireFieldsAreRejected()
    {
        byte[] request = Encoding.UTF8.GetBytes(
            $$"""
            {"version":{{EngineProtocol.Version}},"request_id":"r","operation":"resolve","game_id":"g","decision":{"affordance":-1,"targets":[]},"surprise":true}
            """);

        Assert.Throws<JsonException>(() => EngineJson.ReadRequest(request));
    }

    [Fact]
    public void TheWireCarriesAFilteredWorldAndNeverAStateDigest()
    {
        var response = new EngineResponse(
            EngineProtocol.Version,
            "request",
            "game",
            Capability: null,
            Prompt: null,
            Events: [],
            World: new WorldDescriptor([], [], [], Outcome.Unfinished));

        string json = Encoding.UTF8.GetString(EngineJson.Write(response));

        Assert.Contains("\"world\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("digest", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("audience", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StateDigest", ResponseTypeNames(), StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Marvel.Rules.State.World", ResponseTypeNames(), StringComparison.Ordinal);
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
        var response = new EngineResponse(
            EngineProtocol.Version,
            "topology",
            "game",
            Capability: null,
            Prompt: null,
            Events: [joined, detached]);

        var again = EngineJson.ReadResponse(EngineJson.Write(response));

        Assert.Equal(4, again.Version);
        Assert.Collection(
            again.Events,
            happened => Assert.Equal(joined, Assert.IsType<PlayAreaJoined>(happened)),
            happened => Assert.Equal(detached, Assert.IsType<PlayAreaDetached>(happened)));
    }

    [Fact]
    public void FramingPinsTheLengthAndRejectsOversizedPayloads()
    {
        using var frame = new MemoryStream();
        SocketFrame.Write(frame, [1, 2, 3]);

        Assert.Equal([0, 0, 0, 3, 1, 2, 3], frame.ToArray());
        Assert.Throws<InvalidDataException>(
            () => SocketFrame.Write(
                Stream.Null, new byte[SocketFrame.MaximumPayload + 1]));
    }

    [Fact]
    public void ATruncatedFrameIsNeverTreatedAsARequest()
    {
        using var frame = new MemoryStream([0, 0, 0, 2, 1]);

        Assert.Throws<EndOfStreamException>(() => SocketFrame.Read(frame));
    }

    [Fact]
    public async Task ACancelledInProcessExchangeNeverReachesGameState()
    {
        var endpoint = new CountingEndpoint();
        var transport = new InProcessTransport(endpoint);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await transport.ExchangeAsync(
                EngineRequest.CloseGame("cancelled", "game", "capability"),
                cancelled.Token));
        Assert.Equal(0, endpoint.Calls);
    }

    [Fact]
    public async Task CallerCancellationStopsAtTheCompletedRequestWrite()
    {
        var request = EngineRequest.CloseGame("commit", "game", "capability");
        var response = new EngineResponse(
            EngineProtocol.Version,
            request.RequestId,
            request.GameId,
            Capability: null,
            Prompt: null,
            Events: [],
            Error: new EngineError("session_not_found", "missing"));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            Assert.NotNull(SocketFrame.Read(stream));
            SocketFrame.Write(stream, EngineJson.Write(response));
        }, TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource();
        var socket = new SocketTransport(
            IPAddress.Loopback.ToString(), port, cancelled.Cancel);

        Task<EngineResponse> exchange = socket.ExchangeAsync(request, cancelled.Token).AsTask();

        Assert.Equal(
            EngineJson.Write(response),
            EngineJson.Write(await exchange));
        await serving;
    }

    [Fact]
    public async Task LargeClientIdsAreBoundedAndTheNextConnectionStillWorks()
    {
        var server = new SocketEngineServer(
            new EngineHost(new UnusedFactory()), IPAddress.Loopback, port: 0);
        string largeGameId = new('g', 2_200_000);

        var rejected = await ExchangeOverSocket(
            server,
            EngineRequest.CloseGame("large", largeGameId, "capability"));
        var next = await ExchangeOverSocket(
            server,
            EngineRequest.CloseGame("next", "missing", "capability"));

        Assert.Equal("invalid_request", rejected.Error?.Code);
        Assert.Equal(EngineProtocol.MaximumIdentifierLength, rejected.GameId.Length);
        Assert.True(EngineJson.Write(rejected).Length < SocketFrame.MaximumPayload);
        Assert.Equal("session_not_found", next.Error?.Code);
    }

    [Fact]
    public async Task AnUnrepresentableResponseIsContainedToItsConnection()
    {
        var normal = new EngineResponse(
            EngineProtocol.Version, "next", "game", Capability: null,
            Prompt: null, Events: []);
        var endpoint = new SequenceEndpoint(
            new EngineResponse(
                EngineProtocol.Version,
                "large",
                new string('g', SocketFrame.MaximumPayload),
                Capability: null,
                Prompt: null,
                Events: []),
            normal);
        var server = new SocketEngineServer(endpoint, IPAddress.Loopback, port: 0);

        var failed = await ExchangeOverSocket(
            server, EngineRequest.CloseGame("first", "game", "capability"));
        var next = await ExchangeOverSocket(
            server, EngineRequest.CloseGame("second", "game", "capability"));

        Assert.Equal("response_failed", failed.Error?.Code);
        Assert.Equal(EngineJson.Write(normal), EngineJson.Write(next));
    }

    [Fact]
    public async Task SeparateConnectionsCannotResolveOrCloseEachOthersSession()
    {
        var host = new EngineHost(
            DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root),
            new SequenceCapabilities("capability-a", "capability-b"));
        var server = new SocketEngineServer(host, IPAddress.Loopback, port: 0);
        var specification = new GameSpecification(
            "rhino", ["spider_man"], ModularSets: [], Seed: 7);

        var first = await ExchangeOverSocket(
            server, EngineRequest.OpenGame("first", "same-id", specification));
        var second = await ExchangeOverSocket(
            server, EngineRequest.OpenGame("second", "same-id", specification));
        var guessed = await ExchangeOverSocket(
            server, EngineRequest.CloseGame("guess", "same-id", "not-the-capability"));
        var closeSecond = await ExchangeOverSocket(
            server,
            EngineRequest.CloseGame("close-second", "same-id", second.Capability!));
        var resolveFirst = await ExchangeOverSocket(
            server,
            EngineRequest.ResolveGame(
                "resolve-first", "same-id", first.Capability!, EngineDecision.Decline));

        Assert.NotEqual(first.Capability, second.Capability);
        Assert.Equal("session_not_found", guessed.Error?.Code);
        Assert.Null(closeSecond.Error);
        Assert.Null(resolveFirst.Error);
    }

    [Theory]
    [InlineData("omitted")]
    [InlineData("watch")]
    [InlineData("hot-seat")]
    public async Task RestrictedSeatsAdvanceOneSharedGameWithIndependentCapabilities(
        string viewerMode)
    {
        var host = new EngineHost(
            DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root),
            new SequenceCapabilities(
                "seat-zero", "seat-zero", "invite-one", "seat-one"),
            new RestrictedVisibilityPolicy(0));
        var server = new SocketEngineServer(host, IPAddress.Loopback, port: 0);
        var specification = new GameSpecification(
            "rhino", ["spider_man", "captain_marvel"], ModularSets: [], Seed: 7);

        ViewerClaim? viewer = viewerMode switch
        {
            "omitted" => null,
            "watch" => new ViewerClaim(Watch: true),
            "hot-seat" => new ViewerClaim(HotSeat: true),
            _ => throw new InvalidOperationException($"unknown test mode {viewerMode}"),
        };
        EngineResponse opened = await ExchangeOverSocket(
            server, EngineRequest.OpenGame("open", "shared", specification, viewer));
        SeatInvitation invitation = Assert.Single(opened.Invitations!);
        EngineResponse capabilityCannotAttach = await ExchangeOverSocket(
            server,
            EngineRequest.AttachGame("steal-seat", "shared", opened.Capability!));
        EngineResponse attached = await ExchangeOverSocket(
            server,
            EngineRequest.AttachGame("attach", "shared", invitation.Invitation));

        Assert.Equal("session_not_found", capabilityCannotAttach.Error?.Code);
        Assert.Equal(1, invitation.Seat);
        Assert.Equal("seat-zero", opened.Capability);
        Assert.Equal("seat-one", attached.Capability);
        Assert.Equal(0, opened.Prompt?.Player);
        Assert.Null(attached.Prompt);
        Assert.All(Hand(attached, 0), card => Assert.Null(card.Face));
        Assert.All(Hand(attached, 1), card => Assert.NotNull(card.Face));

        EngineResponse forZero = await ExchangeOverSocket(
            server,
            EngineRequest.ResolveGame(
                "zero-mulligan", "shared", opened.Capability!, EngineDecision.Decline));
        Assert.Null(forZero.Error);
        Assert.Null(forZero.Prompt);

        EngineResponse forbidden = await ExchangeOverSocket(
            server,
            EngineRequest.ResolveGame(
                "steal-one", "shared", opened.Capability!, EngineDecision.Decline));
        Assert.Equal("not_your_turn", forbidden.Error?.Code);

        EngineResponse forOne = await ExchangeOverSocket(
            server, EngineRequest.SyncGame("sync-one", "shared", attached.Capability!));
        Assert.Equal(1, forOne.Prompt?.Player);
        EngineResponse replay = await ExchangeOverSocket(
            server,
            EngineRequest.AttachGame("replay", "shared", invitation.Invitation));
        Assert.Equal("session_not_found", replay.Error?.Code);

        EngineResponse afterOne = await ExchangeOverSocket(
            server,
            EngineRequest.ResolveGame(
                "one-mulligan", "shared", attached.Capability!, EngineDecision.Decline));
        Assert.Null(afterOne.Error);
        Assert.Null(afterOne.Prompt);
        EngineResponse resumedZero = await ExchangeOverSocket(
            server, EngineRequest.SyncGame("sync-zero", "shared", opened.Capability!));
        Assert.Equal(0, resumedZero.Prompt?.Player);

        Assert.Null((await ExchangeOverSocket(
            server,
            EngineRequest.CloseGame("close", "shared", opened.Capability!))).Error);
        EngineResponse afterClose = await ExchangeOverSocket(
            server, EngineRequest.SyncGame("after-close", "shared", attached.Capability!));
        Assert.Equal("session_not_found", afterClose.Error?.Code);
    }

    [Fact]
    public async Task HostedEliminationDeliversThePlayAreaTopologyChange()
    {
        var factory = new EliminatingFactory(
            DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root));
        var server = new SocketEngineServer(
            new EngineHost(factory), IPAddress.Loopback, port: 0);
        var specification = new GameSpecification(
            "rhino", ["spider_man", "captain_marvel"], ModularSets: [], Seed: 7);

        EngineResponse opened = await ExchangeOverSocket(
            server, EngineRequest.OpenGame("eliminate", "shared", specification));

        Assert.Null(opened.Error);
        var detached = Assert.Single(opened.Events.OfType<PlayAreaDetached>());
        Assert.Equal(0, detached.PlayArea);
        Assert.DoesNotContain(
            Assert.IsType<WorldDescriptor>(opened.World).GameAreas,
            area => area.PlayAreas.Contains(0));
        EngineResponse synced = await ExchangeOverSocket(
            server,
            EngineRequest.SyncGame(
                "after-elimination", "shared", opened.Capability!));
        Assert.Null(synced.Error);
    }

    [Fact]
    public async Task HighwayRobberyReturnsFaceUpCardsOnlyToTheirOwnersView()
    {
        var factory = new HighwayRobberyFactory(
            DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root));
        var host = new EngineHost(
            factory,
            new SequenceCapabilities("seat-zero", "invite-one", "seat-one"),
            new RestrictedVisibilityPolicy(0));
        var server = new SocketEngineServer(host, IPAddress.Loopback, port: 0);
        var specification = new GameSpecification(
            "rhino", ["spider_man", "she_hulk"], ModularSets: [], Seed: 12345);

        EngineResponse forZero = await ExchangeOverSocket(
            server, EngineRequest.OpenGame("robbery", "shared", specification));
        SeatInvitation invitation = Assert.Single(forZero.Invitations!);
        EngineResponse forOne = await ExchangeOverSocket(
            server,
            EngineRequest.AttachGame("owner", "shared", invitation.Invitation));

        Assert.True(factory.ReturnedForSeatOne >= 0);
        Assert.All(Hand(forZero, 1), card =>
        {
            Assert.Null(card.Id);
            Assert.Null(card.Face);
        });
        Assert.DoesNotContain(
            forZero.Events.OfType<CardsMoved>().SelectMany(moved => moved.Cards),
            landing => landing.Card == factory.ReturnedForSeatOne);
        Assert.DoesNotContain(
            forZero.Events.OfType<CardDetached>(),
            detached => detached.Card == factory.ReturnedForSeatOne);

        var returned = Assert.Single(
            Hand(forOne, 1), card => card.Id == factory.ReturnedForSeatOne);
        Assert.NotNull(returned.Face);
        Assert.True(returned.FaceUp);
    }

    private static async Task<EngineResponse> ExchangeOverSocket(
        SocketEngineServer server, EngineRequest request)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            server.Serve(accepted);
        }, TestContext.Current.CancellationToken);
        var transport = new SocketTransport(IPAddress.Loopback.ToString(), port);

        EngineResponse response = await transport.ExchangeAsync(
            request, TestContext.Current.CancellationToken);
        await serving;
        return response;
    }

    private static IReadOnlyList<Marvel.View.CardDescriptor> Hand(
        EngineResponse response, int seat) =>
        Assert.Single(
            Assert.IsType<WorldDescriptor>(response.World).Areas,
            area => area.Zone == "HandsArea" && area.Owner == seat).Cards;

    private sealed class EchoEndpoint(
        EngineRequest expectedRequest,
        EngineResponse response) : IEngineEndpoint
    {
        public int Calls { get; private set; }

        public EngineResponse Exchange(EngineRequest request)
        {
            Calls++;
            Assert.Equal(EngineJson.Write(expectedRequest), EngineJson.Write(request));
            return response;
        }
    }

    private sealed class CountingEndpoint : IEngineEndpoint
    {
        public int Calls { get; private set; }

        public EngineResponse Exchange(EngineRequest request)
        {
            Calls++;
            throw new InvalidOperationException("should not be called");
        }
    }

    private sealed class SequenceEndpoint(params EngineResponse[] responses)
        : IEngineEndpoint
    {
        private readonly Queue<EngineResponse> responses = new(responses);

        public EngineResponse Exchange(EngineRequest request) => responses.Dequeue();
    }

    private sealed class SequenceCapabilities(params string[] capabilities)
        : ISessionCapabilityIssuer
    {
        private readonly Queue<string> capabilities = new(capabilities);

        public string Issue() => capabilities.Dequeue();
    }

    private sealed class UnusedFactory : IGameFactory
    {
        public OpenedGame Create(GameSpecification specification) =>
            throw new InvalidOperationException("should not be called");
    }

    private sealed class EliminatingFactory(IGameFactory inner) : IGameFactory
    {
        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            var events = opened.SetupEvents.ToList();
            Elimination.Eliminate(
                opened.Game.State,
                opened.Game.State.Facts,
                player: 0,
                trigger: "test",
                events);
            return opened with { SetupEvents = events };
        }
    }

    private sealed class HighwayRobberyFactory(IGameFactory inner) : IGameFactory
    {
        public int ReturnedForSeatOne { get; private set; } = -1;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            var world = opened.Game.State;
            var events = opened.SetupEvents.ToList();
            Card scheme = world.CreateCard(
                "01166", world.AreaOf(DeckType.SideSchemesArea));
            events.AddRange(world.Abilities.WhenRevealed(world, scheme, player: 0));
            ReturnedForSeatOne = Assert.Single(
                world.Areas
                    .Where(area => area.Host == scheme.ObjectId)
                    .SelectMany(area => area.Cards),
                card => card.Owner == 1).ObjectId;

            world.Agenda.Add(new PhaseStep(Steps.DealAttackDamage, 1, 4, Plan: true));
            world.Agenda.Begin(world, world.Facts);
            Defeat.Scheme(world, world.Facts, scheme, "test", events);
            return opened with { SetupEvents = events };
        }
    }

    private static string ResponseTypeNames() => string.Join(
        ",",
        typeof(EngineResponse).GetProperties().Select(property =>
            property.PropertyType.FullName));
}
