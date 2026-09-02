using System.Net;
using System.Net.Sockets;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
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

        BoardPresentation board = BoardPresentation.From(opened.World);
        Assert.Equal(opened.World.Areas.Select(area => area.Id),
            board.Areas.Select(area => area.Id));
        Assert.Equal(
            opened.World.Areas.Select(area => area.Cards.Count),
            board.Areas.Select(area => area.Cards.Sum(card => card.Count)));
        Assert.Equal(
            opened.World.Areas.Select(area => area.Removed.Count),
            board.Areas.Select(area => area.Removed.Sum(card => card.Count)));
        Assert.Contains(board.Areas, area => area.Title == "ENCOUNTER DECK");
        Assert.Contains(board.Areas, area => area.Title == "VILLAIN");
        Assert.Contains(board.Areas, area => area.Title == "PLAYER DECK");
        Assert.Contains(board.Areas, area => area.Title == "HANDS");
        Assert.Contains(board.Areas, area => area.Title == "HERO");
    }

    [Fact]
    public async Task ALocalDecisionReturnsTheNextAuthoritativePromptAndBoard()
    {
        var client = new LocalGameClient(new InProcessTransport(Host()));
        ClientStartupResult opened = await client.OpenAsync(
            Specification(), TestContext.Current.CancellationToken);

        ClientResolutionResult resolved = await client.ResolveAsync(
            opened.Response!.Capability!,
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.True(resolved.Succeeded);
        Assert.NotNull(resolved.Response?.World);
        Assert.NotNull(resolved.Response?.Prompt);
        Assert.Null(resolved.Error);
    }

    [Fact]
    public async Task VisibleControlsCanPlayASeededLocalGameToItsEnding()
    {
        var client = new LocalGameClient(new InProcessTransport(Host()));
        ClientSetupResult setup = await client.ReadSetupAsync(
            TestContext.Current.CancellationToken);
        ClientStartupResult opened = await client.OpenAsync(
            setup.Choices!,
            DefaultSelection(setup.Choices!) with { Seed = "1" },
            TestContext.Current.CancellationToken);
        EngineResponse current = Assert.IsType<EngineResponse>(opened.Response);
        string capability = Assert.IsType<string>(current.Capability);
        var labels = new List<string>();
        bool passed = false;
        int decisions = 0;

        while (current.Prompt is not null)
        {
            Assert.True(
                decisions < 20,
                $"local UI journey is still playing at '{current.Prompt.Label}'");
            labels.Add(current.Prompt.Label);
            var composer = new DecisionComposer(current.Prompt);
            EngineDecision decision;
            if (current.Prompt.Cancellable)
            {
                passed = true;
                Assert.True(composer.TryDecline(out EngineDecision? declined, out _));
                decision = declined!;
            }
            else
            {
                Affordance offered = current.Prompt.Affordances.First(option => option.IsLegal);
                composer.SelectAffordance(offered.Id);
                Assert.True(
                    composer.TryBuild(out EngineDecision? submitted, out string? error),
                    error);
                decision = submitted!;
            }

            ClientResolutionResult resolved = await client.ResolveAsync(
                capability, decision, TestContext.Current.CancellationToken);
            Assert.True(resolved.Succeeded, resolved.Error?.Message);
            current = Assert.IsType<EngineResponse>(resolved.Response);
            decisions++;
        }

        Assert.Equal(7, decisions);
        Assert.True(passed, "the journey never used the visible pass control");
        Assert.Contains(labels, label =>
            label.Contains("Mulligan", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(labels, label =>
            label.Contains("End", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(Outcome.VillainWins, current.World?.Outcome);
        Assert.Equal(
            GameProgressKind.VillainWins,
            GameProgressPresentation.FromResponse(current).Kind);
    }

    [Fact]
    public async Task ARejectedDecisionSynchronizesWithoutRepeatingTheMutation()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "open", LocalGameSession.GameId, Specification()));
        var rejected = new EngineResponse(
            EngineProtocol.Version,
            "local-resolve",
            LocalGameSession.GameId,
            Capability: null,
            Prompt: null,
            Events: [],
            Error: new EngineError("stale_decision", "The prompt changed."));
        var transport = new ScriptedTransport(
            rejected,
            current with { RequestId = "local-recover", Capability = null });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            current.Capability!,
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.True(result.HasAuthoritativeView);
        Assert.Equal("stale_decision", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync],
            transport.Requests.Select(request => request.Operation));
        Assert.NotNull(transport.Requests[0].Decision);
        Assert.Null(transport.Requests[1].Decision);
    }

    [Fact]
    public async Task AnUncertainResolveReadsStateAndNeverRetriesTheDecision()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "open", LocalGameSession.GameId, Specification()));
        var transport = new ScriptedTransport(
            new IOException("response lost"),
            current with { RequestId = "local-recover", Capability = null });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            current.Capability!,
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.True(result.HasAuthoritativeView);
        Assert.Equal("transport_unavailable", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync],
            transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task AMalformedResolveReadsStateAndNeverRetriesTheDecision()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "open", LocalGameSession.GameId, Specification()));
        var transport = new ScriptedTransport(
            current with
            {
                RequestId = "local-resolve",
                Prompt = current.Prompt! with { Affordances = null! },
            },
            current with { RequestId = "local-recover", Capability = null });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            current.Capability!,
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.True(result.HasAuthoritativeView);
        Assert.Equal("invalid_response", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync],
            transport.Requests.Select(request => request.Operation));
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
    public async Task AnIncompletePromptIsRejectedBeforeRendering()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame(
            "local-open", LocalGameSession.GameId, Specification()));

        ClientStartupResult startup = await new LocalGameClient(
            new FixedTransport(complete with
            {
                Prompt = complete.Prompt! with { Affordances = null! },
            })).OpenAsync(Specification(), TestContext.Current.CancellationToken);

        Assert.Equal("invalid_response", startup.Error?.Code);
    }

    [Fact]
    public async Task AnEmptyPromptIsRejectedBeforeRendering()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame(
            "local-open", LocalGameSession.GameId, Specification()));

        ClientStartupResult startup = await new LocalGameClient(
            new FixedTransport(complete with
            {
                Prompt = complete.Prompt! with { Affordances = [] },
            })).OpenAsync(Specification(), TestContext.Current.CancellationToken);

        Assert.Equal("invalid_response", startup.Error?.Code);
    }

    [Fact]
    public async Task ATerminalWorldWithAStalePromptIsNeverRenderedAsAuthoritative()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame(
            "local-open", LocalGameSession.GameId, Specification()));
        EngineResponse malformed = complete with
        {
            Capability = null,
            World = complete.World! with { Outcome = Outcome.PlayersWin },
        };
        var transport = new ScriptedTransport(malformed, malformed);

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            complete.Capability!,
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.False(result.HasAuthoritativeView);
        Assert.Equal("invalid_response", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync],
            transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task IncompleteBoardCollectionsAreRejectedBeforeRendering()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame(
            "local-open", LocalGameSession.GameId, Specification()));
        WorldDescriptor world = Assert.IsType<WorldDescriptor>(complete.World);
        AreaDescriptor area = world.Areas[0];
        CardDescriptor readable = world.Areas
            .SelectMany(candidate => candidate.Cards.Concat(candidate.Removed))
            .First(card => card.Face is not null);
        AreaDescriptor withReadable = world.Areas.First(candidate =>
            candidate.Cards.Contains(readable) || candidate.Removed.Contains(readable));
        WorldDescriptor[] incomplete =
        [
            world with { Players = null! },
            world with { GameAreas = null! },
            world with { Areas = [area with { Cards = null! }] },
            world with { Areas = [area with { Removed = null! }] },
            world with
            {
                Areas =
                [
                    withReadable with
                    {
                        Cards = withReadable.Cards.Contains(readable)
                            ? [readable with { Face = readable.Face! with { Fields = null! } }]
                            : [],
                        Removed = withReadable.Removed.Contains(readable)
                            ? [readable with { Face = readable.Face! with { Fields = null! } }]
                            : [],
                    },
                ],
            },
        ];

        foreach (WorldDescriptor malformed in incomplete)
        {
            ClientStartupResult startup = await new LocalGameClient(
                new FixedTransport(complete with { World = malformed }))
                .OpenAsync(Specification(), TestContext.Current.CancellationToken);

            Assert.Equal("invalid_response", startup.Error?.Code);
        }
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

    [Fact]
    public async Task MutationCancellationBeforeDispatchSendsNoRequest()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var transport = new CapturingTransport();
        var client = new LocalGameClient(transport);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await client.OpenAsync(Specification(), cancelled.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await client.ResolveAsync("capability", EngineDecision.Decline, cancelled.Token));

        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task CancellationAfterMutationDispatchStillConsumesItsResponse()
    {
        EngineResponse opened = Host().Exchange(EngineRequest.OpenGame(
            "local-open", LocalGameSession.GameId, Specification()));
        using var cancellation = new CancellationTokenSource();
        var transport = new CommitAwareCancellingTransport(
            opened with { RequestId = "local-resolve", Capability = null }, cancellation);

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            opened.Capability!, EngineDecision.Decline, cancellation.Token);

        Assert.True(result.Succeeded);
        Assert.True(cancellation.IsCancellationRequested);
        Assert.True(transport.ReceivedToken.CanBeCanceled);
        Assert.Single(transport.Requests);
    }

    [Theory]
    [InlineData(999, "local-open", "local-core-game", "unsupported_version")]
    [InlineData(6, "other", "local-core-game", "invalid_response")]
    [InlineData(6, "local-open", "other-game", "invalid_response")]
    public async Task OpenRejectsMismatchedResponseEnvelopes(
        int version,
        string requestId,
        string gameId,
        string expectedCode)
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame(
            "local-open", LocalGameSession.GameId, Specification()));

        ClientStartupResult result = await new LocalGameClient(new FixedTransport(
            complete with { Version = version, RequestId = requestId, GameId = gameId }))
            .OpenAsync(Specification(), TestContext.Current.CancellationToken);

        Assert.Equal(expectedCode, result.Error?.Code);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task MismatchedResolveEnvelopeRecoversBySync()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "local-open", LocalGameSession.GameId, Specification()));
        var transport = new ScriptedTransport(
            current with { RequestId = "wrong", Capability = null },
            current with { RequestId = "local-recover", Capability = null });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            current.Capability!, EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.True(result.HasAuthoritativeView);
        Assert.Equal("invalid_response", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync],
            transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task UnknownOutcomesAndMalformedEventsAreNeverRendered()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame(
            "local-open", LocalGameSession.GameId, Specification()));
        EngineResponse[] malformed =
        [
            complete with { World = complete.World! with { Outcome = (Outcome)999 } },
            complete with
            {
                Events = [new FieldSet(1, null!, From: 0, To: 1)],
            },
            complete with
            {
                Events = [new CardsFlipped(null!, FaceUp: true)],
            },
        ];

        foreach (EngineResponse response in malformed)
        {
            ClientStartupResult result = await new LocalGameClient(
                new FixedTransport(response)).OpenAsync(
                    Specification(), TestContext.Current.CancellationToken);

            Assert.Equal("invalid_response", result.Error?.Code);
        }
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

    private sealed class ScriptedTransport(params object[] results) : IEngineTransport
    {
        private readonly Queue<object> remaining = new(results);

        public List<EngineRequest> Requests { get; } = [];

        public ValueTask<EngineResponse> ExchangeAsync(
            EngineRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            object result = remaining.Dequeue();
            return result is Exception failure
                ? ValueTask.FromException<EngineResponse>(failure)
                : ValueTask.FromResult((EngineResponse)result);
        }
    }

    private sealed class FailingTransport(string message) : IEngineTransport
    {
        public ValueTask<EngineResponse> ExchangeAsync(
            EngineRequest request,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException<EngineResponse>(new IOException(message));
    }

    private sealed class CommitAwareCancellingTransport(
        EngineResponse response,
        CancellationTokenSource cancellation) : IEngineTransport
    {
        public List<EngineRequest> Requests { get; } = [];

        public CancellationToken ReceivedToken { get; private set; }

        public ValueTask<EngineResponse> ExchangeAsync(
            EngineRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            ReceivedToken = cancellationToken;
            cancellation.Cancel();
            return ValueTask.FromResult(response);
        }
    }
}
