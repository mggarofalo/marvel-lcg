using System.Net;
using System.Net.Sockets;
using Marvel.Rules.Play;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class LocalGameClientTests
{
    [Fact]
    public async Task CommittedCoreChoicesOpenACompleteVisibleGame()
    {
        LocalClientConnection connection = LocalGameClient.ConnectLocal(RepositoryPaths.Root);
        Assert.True(connection.Succeeded);
        ClientSetupResult setup = await connection.Client!.ReadSetupAsync(
            TestContext.Current.CancellationToken);

        ClientStartupResult startup = await connection.Client.OpenAsync(
            Assert.IsType<SetupChoices>(setup.Choices),
            DefaultSelection(setup.Choices!),
            TestContext.Current.CancellationToken);

        EngineResponse opened = Assert.IsType<EngineResponse>(startup.Response);
        Assert.True(startup.Succeeded);
        Assert.Null(startup.Error);
        Assert.NotNull(opened.World);
        Assert.NotNull(opened.Prompt);
        Assert.NotNull(opened.Events);
        Assert.Equal(Outcome.Unfinished, opened.World.Outcome);
        Assert.Equal(LocalGameSession.GameId, opened.GameId);
    }

    [Fact]
    public async Task AppSetupAndOpenUseTheSameRequestsOverLocalAndRemoteTransports()
    {
        var local = new LocalGameClient(new InProcessTransport(Host()));
        ClientSetupResult localSetup = await local.ReadSetupAsync(
            TestContext.Current.CancellationToken);
        ClientStartupResult localOpen = await local.OpenAsync(
            localSetup.Choices!,
            DefaultSelection(localSetup.Choices!),
            TestContext.Current.CancellationToken);

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = new SocketEngineServer(Host(), IPAddress.Loopback, port: 0);
        Task serving = Task.Run(() =>
        {
            for (int exchange = 0; exchange < 2; exchange++)
            {
                using TcpClient accepted = listener.AcceptTcpClient();
                server.Serve(accepted);
            }
        }, TestContext.Current.CancellationToken);

        ClientStartupResult remoteOpen;
        try
        {
            var remote = new LocalGameClient(
                new SocketTransport(IPAddress.Loopback.ToString(), port));
            ClientSetupResult remoteSetup = await remote.ReadSetupAsync(
                TestContext.Current.CancellationToken);
            Assert.Equal(
                localSetup.Choices!.Heroes.Select(choice => (choice.Key, choice.Name)),
                remoteSetup.Choices!.Heroes.Select(choice => (choice.Key, choice.Name)));
            remoteOpen = await remote.OpenAsync(
                remoteSetup.Choices,
                DefaultSelection(remoteSetup.Choices),
                TestContext.Current.CancellationToken);
        }
        finally
        {
            await serving;
        }

        Assert.True(localOpen.Succeeded);
        Assert.True(remoteOpen.Succeeded);
        Assert.Equal(
            EngineJson.Write(localOpen.Response!),
            EngineJson.Write(remoteOpen.Response!));
    }

    [Fact]
    public void MissingContentBecomesABoundedProductError()
    {
        string missing = Path.Combine(
            Path.GetTempPath(), "marvel-missing-content", Guid.NewGuid().ToString("N"));

        LocalClientConnection connection = LocalGameClient.ConnectLocal(missing);

        Assert.False(connection.Succeeded);
        Assert.Null(connection.Client);
        Assert.Equal("content_unavailable", connection.Error?.Code);
        Assert.DoesNotContain(missing, connection.Error?.Message, StringComparison.Ordinal);
        Assert.True(connection.Error?.Message.Length <= 240);
    }

    [Fact]
    public async Task SetupDiscoveryRejectsIncompleteResponses()
    {
        var response = new EngineResponse(
            EngineProtocol.Version,
            "local-setup",
            GameId: string.Empty,
            Capability: null,
            Prompt: null,
            Events: [],
            Setup: null);

        ClientSetupResult setup = await new LocalGameClient(new FixedTransport(response))
            .ReadSetupAsync(TestContext.Current.CancellationToken);

        Assert.Equal("invalid_response", setup.Error?.Code);
    }

    [Fact]
    public async Task SetupDiscoveryRejectsUnknownRecommendations()
    {
        SetupChoices choices = Choices();
        SetupChoices malformed = choices with
        {
            Scenarios =
            [
                choices.Scenarios[0] with
                {
                    RecommendedModularSets = ["future_encounter"],
                },
            ],
        };
        var response = new EngineResponse(
            EngineProtocol.Version,
            "local-setup",
            GameId: string.Empty,
            Capability: null,
            Prompt: null,
            Events: [],
            Setup: malformed);

        ClientSetupResult setup = await new LocalGameClient(new FixedTransport(response))
            .ReadSetupAsync(TestContext.Current.CancellationToken);

        Assert.Equal("invalid_response", setup.Error?.Code);
    }

    [Fact]
    public async Task EngineRejectionsAreBoundedBeforeDisplay()
    {
        string diagnostic = new('x', 400);
        var response = new EngineResponse(
            EngineProtocol.Version,
            "local-open",
            LocalGameSession.GameId,
            Capability: null,
            Prompt: null,
            Events: [],
            Error: new EngineError(diagnostic, diagnostic));

        ClientStartupResult startup = await new LocalGameClient(
            new FixedTransport(response)).OpenAsync(
                Specification(), TestContext.Current.CancellationToken);

        Assert.False(startup.Succeeded);
        Assert.Equal(240, startup.Error?.Code.Length);
        Assert.Equal(240, startup.Error?.Message.Length);
    }

    [Fact]
    public async Task IncompleteSuccessfulOpenResponsesAreRejected()
    {
        var response = new EngineResponse(
            EngineProtocol.Version,
            "local-open",
            LocalGameSession.GameId,
            "capability",
            Prompt: null,
            Events: []);

        ClientStartupResult startup = await new LocalGameClient(
            new FixedTransport(response)).OpenAsync(
                Specification(), TestContext.Current.CancellationToken);

        Assert.Equal("invalid_response", startup.Error?.Code);
    }

    [Fact]
    public async Task ASuccessfulResponseWithoutEventsIsRejectedBeforeRendering()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame(
            "local-open", LocalGameSession.GameId, Specification()));

        ClientStartupResult startup = await new LocalGameClient(
            new FixedTransport(complete with { Events = null! }))
            .OpenAsync(Specification(), TestContext.Current.CancellationToken);

        Assert.Equal("invalid_response", startup.Error?.Code);
    }

    [Fact]
    public async Task InvalidSelectionsSendNoOpenRequest()
    {
        SetupChoices choices = Choices();
        var transport = new CapturingTransport();
        var client = new LocalGameClient(transport);

        ClientStartupResult invalidHero = await client.OpenAsync(
            choices,
            DefaultSelection(choices) with { HeroKey = "wolverine" },
            TestContext.Current.CancellationToken);
        ClientStartupResult invalidModular = await client.OpenAsync(
            choices,
            DefaultSelection(choices) with
            {
                Modular = ModularConfiguration.Selected,
                ModularKey = "mojo_mania",
            },
            TestContext.Current.CancellationToken);
        ClientStartupResult invalidSeed = await client.OpenAsync(
            choices,
            DefaultSelection(choices) with { Seed = "-1" },
            TestContext.Current.CancellationToken);

        Assert.Equal("invalid_selection", invalidHero.Error?.Code);
        Assert.Equal("invalid_selection", invalidModular.Error?.Code);
        Assert.Equal("invalid_seed", invalidSeed.Error?.Code);
        Assert.Empty(transport.Requests);
    }

    [Theory]
    [InlineData(ModularConfiguration.Recommended, null, null)]
    [InlineData(ModularConfiguration.None, null, "")]
    [InlineData(ModularConfiguration.Selected, "bomb_scare", "bomb_scare")]
    public async Task ModularConfigurationPreservesTheThreeProtocolMeanings(
        ModularConfiguration configuration,
        string? selected,
        string? expected)
    {
        SetupChoices choices = Choices();
        var transport = new CapturingTransport();

        await new LocalGameClient(transport).OpenAsync(
            choices,
            DefaultSelection(choices) with
            {
                Modular = configuration,
                ModularKey = selected,
                Seed = uint.MaxValue.ToString(),
            },
            TestContext.Current.CancellationToken);

        EngineRequest request = Assert.Single(transport.Requests);
        Assert.Equal(EngineProtocol.Open, request.Operation);
        Assert.Equal(uint.MaxValue, request.Game?.Seed);
        if (expected is null)
        {
            Assert.Null(request.Game?.ModularSets);
        }
        else if (expected.Length == 0)
        {
            Assert.Empty(request.Game!.ModularSets!);
        }
        else
        {
            Assert.Equal([expected], request.Game?.ModularSets);
        }
    }

    [Fact]
    public async Task TransportDiagnosticsDoNotEscapeToTheProduct()
    {
        ClientSetupResult setup = await new LocalGameClient(
            new FailingTransport("secret socket diagnostic"))
            .ReadSetupAsync(TestContext.Current.CancellationToken);

        Assert.Equal("transport_unavailable", setup.Error?.Code);
        Assert.DoesNotContain(
            "secret socket diagnostic", setup.Error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CallerCancellationIsNotPresentedAsAStartupFailure()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await new LocalGameClient(new InProcessTransport(Host()))
                .ReadSetupAsync(cancelled.Token));
    }

    private static GameSetupSelection DefaultSelection(SetupChoices choices) =>
        new(
            choices.Heroes.Single(choice => choice.Key == "spider_man").Key,
            choices.Scenarios.Single(choice => choice.Key == "rhino").Key,
            ModularConfiguration.Recommended,
            ModularKey: null,
            Seed: "7");

    private static GameSpecification Specification() =>
        new("rhino", ["spider_man"], ModularSets: null, Seed: 7);

    private static SetupChoices Choices() =>
        Assert.IsType<SetupChoices>(Host().Exchange(
            EngineRequest.ReadSetup("choices")).Setup);

    private static EngineHost Host() =>
        new(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new FixedCapabilityIssuer());

    private sealed class FixedCapabilityIssuer : ISessionCapabilityIssuer
    {
        public string Issue() => "development-capability";
    }

    private sealed class FixedTransport(EngineResponse response) : IEngineTransport
    {
        public ValueTask<EngineResponse> ExchangeAsync(
            EngineRequest request,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(response);
    }

    private sealed class CapturingTransport : IEngineTransport
    {
        public List<EngineRequest> Requests { get; } = [];

        public ValueTask<EngineResponse> ExchangeAsync(
            EngineRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return ValueTask.FromResult(new EngineResponse(
                EngineProtocol.Version,
                request.RequestId,
                request.GameId,
                Capability: null,
                Prompt: null,
                Events: [],
                Error: new EngineError("stopped", "capture complete")));
        }
    }

    private sealed class FailingTransport(string message) : IEngineTransport
    {
        public ValueTask<EngineResponse> ExchangeAsync(
            EngineRequest request,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException<EngineResponse>(new IOException(message));
    }
}
