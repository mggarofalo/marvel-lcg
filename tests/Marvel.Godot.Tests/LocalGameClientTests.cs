using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class LocalGameClientTests
{
    [Fact]
    public async Task OpeningATwoSeatSessionPreservesHeroOrderAndSeparatesBearerMaterial()
    {
        SetupChoices choices = Choices();
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame(
            "source", "table-alpha", new GameSpecification(
                "rhino", ["spider_man", "captain_marvel"], null, Seed: 7)));
        var transport = new ScriptedTransport(complete with
        {
            RequestId = "local-open",
            Capability = "owner-secret",
            Invitations = [new SeatInvitation(1, "seat-secret")],
        });
        var selection = DefaultSelection(choices) with
        {
            HeroKeys = ["captain_marvel", "spider_man"],
        };

        ClientEntryResult result = await new LocalGameClient(transport).OpenSessionAsync(
            "table-alpha", choices, selection, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.Equal(new ClientSession("table-alpha", "owner-secret"), result.Session);
        Assert.Equal(["captain_marvel", "spider_man"],
            Assert.Single(transport.Requests).Game?.Heroes);
        Assert.Null(result.Response?.Capability);
        Assert.Null(result.Response?.Invitations);
        SeatInvitation invitation = Assert.Single(result.Invitations);
        Assert.Equal(1, invitation.Seat);
        Assert.Equal("seat-secret", invitation.Invitation);
    }

    [Fact]
    public async Task AttachAcceptsAWaitingViewAndSendsTheInvitationExactlyOnce()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        var transport = new ScriptedTransport(complete with
        {
            RequestId = "local-attach",
            Capability = "attached-secret",
            Prompt = null,
        });

        ClientEntryResult result = await new LocalGameClient(transport).AttachAsync(
            "shared-table", "one-time-secret", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.Equal(new ClientSession("shared-table", "attached-secret"), result.Session);
        Assert.Null(result.Response?.Prompt);
        Assert.Null(result.Response?.Capability);
        Assert.Null(result.Response?.Invitations);
        EngineRequest request = Assert.Single(transport.Requests);
        Assert.Equal(EngineProtocol.Attach, request.Operation);
        Assert.Equal("one-time-secret", request.Capability);
    }

    [Fact]
    public async Task RestrictedSessionInvitationAttachesOnceToItsAuthorizedSeat()
    {
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilityIssuer("owner", "invitation", "guest"),
            new RestrictedVisibilityPolicy(0));
        var client = new LocalGameClient(new InProcessTransport(host));

        ClientEntryResult owner = await client.OpenSessionAsync(
            "shared-table",
            new GameSpecification(
                "rhino", ["spider_man", "captain_marvel"], null, Seed: 7),
            TestContext.Current.CancellationToken);
        SeatInvitation invitation = Assert.Single(owner.Invitations);
        ClientEntryResult guest = await client.AttachAsync(
            "shared-table", invitation.Invitation, TestContext.Current.CancellationToken);
        ClientEntryResult reused = await client.AttachAsync(
            "shared-table", invitation.Invitation, TestContext.Current.CancellationToken);

        Assert.True(owner.Succeeded, owner.Error?.Message);
        Assert.True(guest.Succeeded, guest.Error?.Message);
        Assert.Equal(1, invitation.Seat);
        Assert.Equal(new ClientSession("shared-table", "guest"), guest.Session);
        Assert.Null(guest.Response?.Prompt);
        Assert.Equal("invitation_unavailable", reused.Error?.Code);
    }

    [Fact]
    public async Task UnavailableInvitationIsMappedWithoutEchoingOrRetryingItsSecret()
    {
        const string secret = "expired-seat-secret";
        var transport = new ScriptedTransport(new EngineResponse(
            EngineProtocol.Version,
            "local-attach",
            "shared-table",
            Capability: null,
            Prompt: null,
            Events: [],
            Error: new EngineError("session_not_found", secret)));

        ClientEntryResult result = await new LocalGameClient(transport).AttachAsync(
            "shared-table", secret, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("invitation_unavailable", result.Error?.Code);
        Assert.DoesNotContain(secret, result.Error?.Message, StringComparison.Ordinal);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task UnexpectedAttachFailuresAlsoDoNotEchoInvitationDiagnostics()
    {
        const string secret = "one-time-seat-secret";
        var transport = new ScriptedTransport(new EngineResponse(
            EngineProtocol.Version,
            "local-attach",
            "shared-table",
            Capability: null,
            Prompt: null,
            Events: [],
            Error: new EngineError(secret, secret)));

        ClientEntryResult result = await new LocalGameClient(transport).AttachAsync(
            "shared-table", secret, TestContext.Current.CancellationToken);

        Assert.Equal("attach_failed", result.Error?.Code);
        Assert.DoesNotContain(secret, result.Error?.Message, StringComparison.Ordinal);
        Assert.Single(transport.Requests);
    }

    [Theory]
    [InlineData("", "invitation", "invalid_game_id")]
    [InlineData("   ", "invitation", "invalid_game_id")]
    [InlineData("game", "", "invalid_invitation")]
    [InlineData("game", "   ", "invalid_invitation")]
    public async Task InvalidAttachIdentifiersAreRejectedBeforeTransport(
        string gameId,
        string invitation,
        string expectedCode)
    {
        var transport = new CapturingTransport();

        ClientEntryResult result = await new LocalGameClient(transport).AttachAsync(
            gameId, invitation, TestContext.Current.CancellationToken);

        Assert.Equal(expectedCode, result.Error?.Code);
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task OversizedEntryIdentifiersAreRejectedWithoutEchoingThem()
    {
        string secret = new('s', EngineProtocol.MaximumIdentifierLength + 1);
        var transport = new CapturingTransport();
        var client = new LocalGameClient(transport);

        ClientEntryResult open = await client.OpenSessionAsync(
            secret, Specification(), TestContext.Current.CancellationToken);
        ClientEntryResult attach = await client.AttachAsync(
            "game", secret, TestContext.Current.CancellationToken);

        Assert.Equal("invalid_game_id", open.Error?.Code);
        Assert.Equal("invalid_invitation", attach.Error?.Code);
        Assert.DoesNotContain(secret, open.Error?.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, attach.Error?.Message, StringComparison.Ordinal);
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task ExplicitSessionResolveAndRecoveryKeepTheSessionGameId()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        var transport = new ScriptedTransport(
            current with
            {
                RequestId = "local-resolve",
                Capability = null,
                Error = new EngineError("stale_decision", "The prompt changed."),
                Prompt = null,
                World = null,
                Events = [],
            },
            current with { RequestId = "local-recover", Capability = null, Events = [] });
        var session = new ClientSession("shared-table", current.Capability!);

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            session, EngineDecision.Decline, TestContext.Current.CancellationToken);

        Assert.True(result.HasAuthoritativeView);
        Assert.Equal("stale_decision", result.Error?.Code);
        Assert.Equal(["shared-table", "shared-table"],
            transport.Requests.Select(request => request.GameId));
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync],
            transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task ResolveKeepsAnAuthoritativeWaitingViewForTheOtherSeat()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        var transport = new ScriptedTransport(current with
        {
            RequestId = "local-resolve",
            Capability = null,
            Prompt = null,
            Revision = current.Revision + 1,
        });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            new ClientSession("shared-table", current.Capability!),
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.True(result.HasAuthoritativeView);
        Assert.Null(result.Response?.Prompt);
        Assert.Null(result.Response?.Capability);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task RecoveryKeepsAnAuthoritativeWaitingViewForTheOtherSeat()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        var transport = new ScriptedTransport(
            new IOException("response lost"),
            current with
            {
                RequestId = "local-recover",
                Capability = null,
                Prompt = null,
                Events = [],
            });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            new ClientSession("shared-table", current.Capability!),
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.True(result.HasAuthoritativeView);
        Assert.Null(result.Response?.Prompt);
        Assert.Equal("transport_unavailable", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync],
            transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task InvalidSessionIsRejectedBeforeResolveTransport()
    {
        var transport = new CapturingTransport();

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            new ClientSession("game", ""),
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.Equal("invalid_session", result.Error?.Code);
        Assert.Empty(transport.Requests);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task OpenRejectsUnsupportedPlayerCountsBeforeTransport(int playerCount)
    {
        string[] heroes = ["spider_man", "captain_marvel", "black_panther"];
        var transport = new CapturingTransport();

        ClientEntryResult result = await new LocalGameClient(transport).OpenSessionAsync(
            "table",
            new GameSpecification("rhino", heroes.Take(playerCount).ToList(), null, Seed: 7),
            TestContext.Current.CancellationToken);

        Assert.Equal("invalid_selection", result.Error?.Code);
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task OpenRejectsDuplicateHeroesBeforeTransport()
    {
        var transport = new CapturingTransport();

        ClientEntryResult result = await new LocalGameClient(transport).OpenSessionAsync(
            "table",
            new GameSpecification("rhino", ["spider_man", "spider_man"], null, Seed: 7),
            TestContext.Current.CancellationToken);

        Assert.Equal("invalid_selection", result.Error?.Code);
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task OpenRequiresAnInitialPromptEvenWhenTheReturnedWorldIsTerminal()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame(
            "source", "table", Specification()));
        var transport = new ScriptedTransport(complete with
        {
            RequestId = "local-open",
            World = complete.World! with { Outcome = Outcome.PlayersWin },
            Prompt = null,
        });

        ClientEntryResult result = await new LocalGameClient(transport).OpenSessionAsync(
            "table", Specification(), TestContext.Current.CancellationToken);

        Assert.Equal("invalid_response", result.Error?.Code);
    }

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
            new EngineDecision(
                Assert.Single(opened.Response.Prompt!.Affordances).Id,
                []),
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
            current with { RequestId = "local-recover", Capability = null, Events = [] });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            current.Capability!,
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.True(result.HasAuthoritativeView);
        Assert.Equal(ClientMutationDisposition.Rejected, result.MutationDisposition);
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
            current with { RequestId = "local-recover", Capability = null, Events = [] });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            current.Capability!,
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.True(result.HasAuthoritativeView);
        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
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
            current with { RequestId = "local-recover", Capability = null, Events = [] });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            current.Capability!,
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.True(result.HasAuthoritativeView);
        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal("invalid_response", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync],
            transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task ACompleteJourneyIsIdenticalOverLocalAndRemoteComposition()
    {
        LocalGameClient local = ClientComposition.Connect(
            RepositoryPaths.Root, configuredEndpoint: null).Client!;
        ClientSetupResult localSetup = await local.ReadSetupAsync(
            TestContext.Current.CancellationToken);
        ClientStartupResult localOpen = await local.OpenAsync(
            localSetup.Choices!,
            DefaultSelection(localSetup.Choices!) with { Seed = "1" },
            TestContext.Current.CancellationToken);

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = new SocketEngineServer(Host(), IPAddress.Loopback, port: 0);
        int exchanges = 0;
        Task serving = Task.Run(() =>
        {
            while (true)
            {
                using TcpClient accepted = listener.AcceptTcpClient();
                server.Serve(accepted);
                Interlocked.Increment(ref exchanges);
            }
        }, TestContext.Current.CancellationToken);

        try
        {
            LocalGameClient remote = ClientComposition.Connect(
                dataRoot: "content-must-not-be-read-for-remote-composition",
                $"tcp://127.0.0.1:{port}").Client!;
            ClientSetupResult remoteSetup = await remote.ReadSetupAsync(
                TestContext.Current.CancellationToken);
            Assert.Equal(
                localSetup.Choices!.Heroes.Select(choice => (choice.Key, choice.Name)),
                remoteSetup.Choices!.Heroes.Select(choice => (choice.Key, choice.Name)));
            ClientStartupResult remoteOpen = await remote.OpenAsync(
                remoteSetup.Choices,
                DefaultSelection(remoteSetup.Choices) with { Seed = "1" },
                TestContext.Current.CancellationToken);

            Assert.True(localOpen.Succeeded);
            Assert.True(remoteOpen.Succeeded);
            EngineResponse localCurrent = localOpen.Response!;
            EngineResponse remoteCurrent = remoteOpen.Response!;
            string localCapability = localCurrent.Capability!;
            string remoteCapability = remoteCurrent.Capability!;
            AssertEquivalentResponses(localCurrent, remoteCurrent);

            int decisions = 0;
            while (localCurrent.Prompt is not null)
            {
                EngineDecision decision = VisibleDecision(localCurrent.Prompt);
                ClientResolutionResult localResolved = await local.ResolveAsync(
                    localCapability, decision,
                    TestContext.Current.CancellationToken);
                ClientResolutionResult remoteResolved = await remote.ResolveAsync(
                    remoteCapability, decision,
                    TestContext.Current.CancellationToken);
                localCurrent = Assert.IsType<EngineResponse>(localResolved.Response);
                remoteCurrent = Assert.IsType<EngineResponse>(remoteResolved.Response);
                AssertEquivalentResponses(localCurrent, remoteCurrent);
                decisions++;
            }

            Assert.Equal(7, decisions);
            Assert.Equal(Outcome.VillainWins, remoteCurrent.World?.Outcome);
        }
        finally
        {
            listener.Stop();
            try
            {
                await serving;
            }
            catch (SocketException) when (!listener.Server.IsBound)
            {
            }
            catch (InvalidOperationException) when (!listener.Server.IsBound)
            {
            }
        }

        Assert.Equal(9, Volatile.Read(ref exchanges));
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
            DefaultSelection(choices) with { HeroKeys = ["wolverine"] },
            TestContext.Current.CancellationToken);
        ClientStartupResult invalidModular = await client.OpenAsync(
            choices,
            DefaultSelection(choices) with
            {
                Modular = ModularConfiguration.Selected,
                ModularKeys = ["mojo_mania"],
            },
            TestContext.Current.CancellationToken);
        ClientStartupResult invalidSeed = await client.OpenAsync(
            choices,
            DefaultSelection(choices) with { Seed = "-1" },
            TestContext.Current.CancellationToken);
        ClientStartupResult duplicateModular = await client.OpenAsync(
            choices,
            DefaultSelection(choices) with
            {
                Modular = ModularConfiguration.Selected,
                ModularKeys = ["bomb_scare", "bomb_scare"],
            },
            TestContext.Current.CancellationToken);
        ClientStartupResult nullModular = await client.OpenAsync(
            choices,
            DefaultSelection(choices) with { ModularKeys = null! },
            TestContext.Current.CancellationToken);
        ClientStartupResult nullSeed = await client.OpenAsync(
            choices,
            DefaultSelection(choices) with { Seed = null! },
            TestContext.Current.CancellationToken);

        Assert.Equal("invalid_selection", invalidHero.Error?.Code);
        Assert.Equal("invalid_selection", invalidModular.Error?.Code);
        Assert.Equal("invalid_seed", invalidSeed.Error?.Code);
        Assert.Equal("invalid_selection", duplicateModular.Error?.Code);
        Assert.Equal("invalid_selection", nullModular.Error?.Code);
        Assert.Equal("invalid_seed", nullSeed.Error?.Code);
        Assert.Empty(transport.Requests);
    }

    [Theory]
    [InlineData(ModularConfiguration.Recommended, null)]
    [InlineData(ModularConfiguration.None, "")]
    [InlineData(ModularConfiguration.Selected, "bomb_scare,masters_of_evil")]
    public async Task ModularConfigurationPreservesTheThreeProtocolMeanings(
        ModularConfiguration configuration,
        string? expected)
    {
        SetupChoices choices = Choices();
        var transport = new CapturingTransport();

        await new LocalGameClient(transport).OpenAsync(
            choices,
            DefaultSelection(choices) with
            {
                Modular = configuration,
                ModularKeys = expected is { Length: > 0 }
                    ? Enumerable.Reverse(expected.Split(',')).ToArray()
                    : [],
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
            Assert.Equal(expected.Split(','), request.Game?.ModularSets);
        }
    }

    [Fact]
    public async Task BlankSeedUsesPreGameEntropyAsAnExplicitReplayableSeed()
    {
        SetupChoices choices = Choices();
        var transport = new CapturingTransport();
        var client = new LocalGameClient(transport, seedSource: () => 4294967290);

        await client.OpenAsync(
            choices,
            DefaultSelection(choices) with { Seed = "  " },
            TestContext.Current.CancellationToken);

        Assert.Equal(4294967290u, Assert.Single(transport.Requests).Game?.Seed);
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
            opened with
            {
                RequestId = "local-resolve",
                Capability = null,
                Revision = opened.Revision + 1,
            },
            cancellation);

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            opened.Capability!, EngineDecision.Decline, cancellation.Token);

        Assert.True(result.Succeeded);
        Assert.True(cancellation.IsCancellationRequested);
        Assert.True(transport.ReceivedToken.CanBeCanceled);
        Assert.Single(transport.Requests);
    }

    [Theory]
    [InlineData(999, "local-open", "local-core-game", "unsupported_version")]
    [InlineData(EngineProtocol.Version, "other", "local-core-game", "invalid_response")]
    [InlineData(EngineProtocol.Version, "local-open", "other-game", "invalid_response")]
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
            current with { RequestId = "local-recover", Capability = null, Events = [] });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            current.Capability!, EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.True(result.HasAuthoritativeView);
        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal("invalid_response", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync],
            transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task UncertainMutationUsesUniqueCorrelationAndRecordsAReconnect()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        var transport = new RecoveringTransport(current);
        var sink = new CollectingOperationalSink();
        var log = new OperationalLog(sink, "client-test");

        ClientResolutionResult result = await new LocalGameClient(transport, log).ResolveAsync(
            new ClientSession("shared-table", current.Capability!),
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);
        log.Flush(TimeSpan.FromSeconds(2));

        Assert.True(result.HasAuthoritativeView);
        Assert.Equal(2, transport.Requests.Count);
        Assert.StartsWith("local-resolve-", transport.Requests[0].RequestId,
            StringComparison.Ordinal);
        Assert.StartsWith("local-recover-", transport.Requests[1].RequestId,
            StringComparison.Ordinal);
        Assert.NotEqual(transport.Requests[0].RequestId, transport.Requests[1].RequestId);
        OperationalRecord reconnect = Assert.Single(sink.Records);
        Assert.Equal(OperationalEventIds.ReconnectCompleted, reconnect.EventId);
        Assert.Equal("accepted", reconnect.Disposition);
        Assert.Equal("reconnect", reconnect.Operation);
    }

    [Fact]
    public async Task SynchronizeReturnsOneSanitizedCompleteCurrentView()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        var transport = new ScriptedTransport(current with
        {
            RequestId = "local-sync",
            Capability = "must-not-escape",
            Invitations = [new SeatInvitation(1, "must-not-escape-either")],
            Events = [],
        });

        ClientSynchronizationResult result = await new LocalGameClient(transport)
            .SynchronizeAsync(
                new ClientSession("shared-table", current.Capability!),
                TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.Equal(ClientSessionDisposition.Active, result.SessionDisposition);
        Assert.NotNull(result.Response?.Prompt);
        Assert.Null(result.Response?.Capability);
        Assert.Null(result.Response?.Invitations);
        EngineRequest request = Assert.Single(transport.Requests);
        Assert.Equal(EngineProtocol.Sync, request.Operation);
        Assert.Equal(current.Capability, request.Capability);
    }

    [Fact]
    public async Task ResolveAcceptsACancellablePromptWithNoLegalActions()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", LocalGameSession.GameId, Specification()));
        var emptyTurn = new Prompt(
            0,
            Question.TurnOption,
            TimingPriority.Untimed,
            "WhenPlayerInTurn",
            "Player turn",
            Cancellable: true,
            Affordances: []);
        var transport = new ScriptedTransport(current with
        {
            RequestId = "local-resolve",
            Revision = 1,
            Capability = null,
            Prompt = emptyTurn,
            Events = [],
        });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            current.Capability!,
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.Equal(ClientMutationDisposition.Accepted, result.MutationDisposition);
        Assert.Empty(result.Response!.Prompt!.Affordances);
    }

    [Fact]
    public async Task SynchronizeAcceptsWaitingAndTerminalViews()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        EngineResponse[] complete =
        [
            current with
            {
                RequestId = "local-sync",
                Capability = null,
                Prompt = null,
                Events = [],
            },
            current with
            {
                RequestId = "local-sync",
                Capability = null,
                Prompt = null,
                Events = [],
                World = current.World! with { Outcome = Outcome.PlayersWin },
            },
        ];

        foreach (EngineResponse response in complete)
        {
            ClientSynchronizationResult result = await new LocalGameClient(
                new FixedTransport(response)).SynchronizeAsync(
                    new ClientSession("shared-table", current.Capability!),
                    TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded, result.Error?.Message);
            Assert.Null(result.Response?.Prompt);
        }
    }

    [Fact]
    public async Task SynchronizationRejectsNonemptyEventsWithoutMakingSessionUnavailable()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        GameEvent replayed = new FieldSet(11, "damage", 0, 1)
        {
            Trigger = "WhenPlayerInTurn",
            Verb = "Attack",
        };
        var transport = new ScriptedTransport(current with
        {
            RequestId = "local-sync",
            Capability = null,
            Events = [replayed],
        });

        ClientSynchronizationResult result = await new LocalGameClient(transport)
            .SynchronizeAsync(
                new ClientSession("shared-table", current.Capability!),
                TestContext.Current.CancellationToken);

        Assert.False(result.HasAuthoritativeView);
        Assert.Equal("invalid_response", result.Error?.Code);
        Assert.Equal(ClientSessionDisposition.Active, result.SessionDisposition);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task AnOlderSynchronizationCannotRollTheRememberedRevisionBackward()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        var transport = new ScriptedTransport(
            current with
            {
                RequestId = "local-attach",
                Capability = "session",
                Revision = 3,
            },
            current with
            {
                RequestId = "local-sync",
                Capability = null,
                Events = [],
                Revision = 2,
            },
            current with
            {
                RequestId = "local-resolve",
                Capability = null,
                Revision = 4,
            });
        var client = new LocalGameClient(transport);
        ClientEntryResult attached = await client.AttachAsync(
            "shared-table", "invitation", TestContext.Current.CancellationToken);

        ClientSynchronizationResult old = await client.SynchronizeAsync(
            attached.Session!, TestContext.Current.CancellationToken);
        ClientResolutionResult resolved = await client.ResolveAsync(
            attached.Session!, EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.Equal("invalid_response", old.Error?.Code);
        Assert.Null(old.Response);
        Assert.Equal(ClientMutationDisposition.Accepted, resolved.MutationDisposition);
        Assert.Equal(3, transport.Requests[2].ExpectedRevision);
    }

    [Fact]
    public async Task ASuccessfulResolveMustAdvanceExactlyOneRevision()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        var transport = new ScriptedTransport(
            current with
            {
                RequestId = "local-attach",
                Capability = "session",
                Revision = 3,
            },
            current with
            {
                RequestId = "local-resolve",
                Capability = null,
                Revision = 3,
            },
            current with
            {
                RequestId = "local-recover",
                Capability = null,
                Events = [],
                Revision = 3,
            });
        var client = new LocalGameClient(transport);
        ClientEntryResult attached = await client.AttachAsync(
            "shared-table", "invitation", TestContext.Current.CancellationToken);

        ClientResolutionResult result = await client.ResolveAsync(
            attached.Session!, EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal("invalid_response", result.Error?.Code);
        Assert.Equal(3, result.Response?.Revision);
        Assert.Equal(3, transport.Requests[1].ExpectedRevision);
    }

    [Fact]
    public async Task RecoveryDoesNotRenderOrReplayNonemptySynchronizationEvents()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        GameEvent replayed = new FieldSet(11, "damage", 0, 1)
        {
            Trigger = "WhenPlayerInTurn",
            Verb = "Attack",
        };
        var transport = new ScriptedTransport(
            new IOException("response lost"),
            current with
            {
                RequestId = "local-recover",
                Capability = null,
                Events = [replayed],
            });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            new ClientSession("shared-table", current.Capability!),
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.False(result.HasAuthoritativeView);
        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal(ClientSessionDisposition.Active, result.SessionDisposition);
        Assert.Equal("transport_unavailable", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync],
            transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task InvalidAndExpiredSynchronizationSessionsAreUnavailable()
    {
        var capture = new CapturingTransport();
        ClientSynchronizationResult invalid = await new LocalGameClient(capture)
            .SynchronizeAsync(
                new ClientSession("shared-table", ""),
                TestContext.Current.CancellationToken);

        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilityIssuer("owner"));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", "shared-table", Specification()));
        host.Exchange(EngineRequest.CloseGame(
            "close", "shared-table", opened.Capability!));
        ClientSynchronizationResult expired = await new LocalGameClient(
            new InProcessTransport(host)).SynchronizeAsync(
                new ClientSession("shared-table", opened.Capability!),
                TestContext.Current.CancellationToken);

        Assert.Equal("session_unavailable", invalid.Error?.Code);
        Assert.Equal(ClientSessionDisposition.Unavailable, invalid.SessionDisposition);
        Assert.Empty(capture.Requests);
        Assert.Equal("session_unavailable", expired.Error?.Code);
        Assert.Equal(ClientSessionDisposition.Unavailable, expired.SessionDisposition);
    }

    [Fact]
    public async Task SynchronizationFailuresKeepTheSessionActiveAndDoNotEchoSecrets()
    {
        const string secret = "private-session-capability";
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        ClientSynchronizationResult malformed = await new LocalGameClient(
            new FixedTransport(current with
            {
                RequestId = "local-sync",
                Capability = null,
                Prompt = current.Prompt! with { Affordances = null! },
            })).SynchronizeAsync(
                new ClientSession("shared-table", secret),
                TestContext.Current.CancellationToken);
        ClientSynchronizationResult lost = await new LocalGameClient(
            new FailingTransport(secret)).SynchronizeAsync(
                new ClientSession("shared-table", secret),
                TestContext.Current.CancellationToken);
        ClientSynchronizationResult expired = await new LocalGameClient(
            new FixedTransport(new EngineResponse(
                EngineProtocol.Version,
                "local-sync",
                "shared-table",
                Capability: null,
                Prompt: null,
                Events: [],
                Error: new EngineError("session_not_found", secret))))
            .SynchronizeAsync(
                new ClientSession("shared-table", secret),
                TestContext.Current.CancellationToken);

        Assert.Equal("invalid_response", malformed.Error?.Code);
        Assert.Equal(ClientSessionDisposition.Active, malformed.SessionDisposition);
        Assert.Equal("transport_unavailable", lost.Error?.Code);
        Assert.Equal(ClientSessionDisposition.Active, lost.SessionDisposition);
        Assert.Equal("session_unavailable", expired.Error?.Code);
        Assert.Equal(ClientSessionDisposition.Unavailable, expired.SessionDisposition);
        Assert.DoesNotContain(secret, lost.Error?.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, expired.Error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PrecommitResolveFailureIsNotSentAndDoesNotSynchronize()
    {
        var transport = new ScriptedTransport(
            new EngineTransportException(
                requestMayHaveCommitted: false,
                new IOException("private diagnostic")));

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            new ClientSession("shared-table", "secret"),
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.Equal(ClientMutationDisposition.NotSent, result.MutationDisposition);
        Assert.Equal(ClientSessionDisposition.Active, result.SessionDisposition);
        Assert.Equal("transport_unavailable", result.Error?.Code);
        Assert.Single(transport.Requests);
        Assert.Equal(EngineProtocol.Resolve, transport.Requests[0].Operation);
    }

    [Fact]
    public async Task CommittedResolveLossIsUncertainAndSynchronizesExactlyOnce()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        var transport = new ScriptedTransport(
            new EngineTransportException(
                requestMayHaveCommitted: true,
                new IOException("response lost")),
            current with { RequestId = "local-recover", Capability = null, Events = [] });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            new ClientSession("shared-table", current.Capability!),
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal(ClientSessionDisposition.Active, result.SessionDisposition);
        Assert.True(result.HasAuthoritativeView);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync],
            transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task ServerRefusalIsRejectedAndRecoversWithoutRepeatingDecision()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame(
            "source", "shared-table", Specification()));
        var transport = new ScriptedTransport(
            current with
            {
                RequestId = "local-resolve",
                Capability = null,
                Prompt = null,
                World = null,
                Events = [],
                Error = new EngineError("stale_decision", "The prompt changed."),
            },
            current with { RequestId = "local-recover", Capability = null, Events = [] });

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            new ClientSession("shared-table", current.Capability!),
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.Equal(ClientMutationDisposition.Rejected, result.MutationDisposition);
        Assert.Equal(ClientSessionDisposition.Active, result.SessionDisposition);
        Assert.True(result.HasAuthoritativeView);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync],
            transport.Requests.Select(request => request.Operation));
    }

    [Theory]
    [InlineData("session_not_found", ClientMutationDisposition.Rejected)]
    [InlineData("game_aborted", ClientMutationDisposition.Uncertain)]
    public async Task ResolveUnavailableErrorsDoNotEchoSecretsOrRetry(
        string code,
        ClientMutationDisposition expectedMutation)
    {
        const string secret = "private-session-capability";
        var transport = new ScriptedTransport(new EngineResponse(
            EngineProtocol.Version,
            "local-resolve",
            "shared-table",
            Capability: null,
            Prompt: null,
            Events: [],
            Error: new EngineError(code, secret)));

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            new ClientSession("shared-table", secret),
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedMutation, result.MutationDisposition);
        Assert.Equal(ClientSessionDisposition.Unavailable, result.SessionDisposition);
        Assert.Equal("session_unavailable", result.Error?.Code);
        Assert.DoesNotContain(secret, result.Error?.Message, StringComparison.Ordinal);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task RecoveryExpirationRetainsTheUncertainMutationDisposition()
    {
        var transport = new ScriptedTransport(
            new IOException("response lost"),
            new EngineResponse(
                EngineProtocol.Version,
                "local-recover",
                "shared-table",
                Capability: null,
                Prompt: null,
                Events: [],
                Error: new EngineError("session_not_found", "private diagnostic")));

        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(
            new ClientSession("shared-table", "secret"),
            EngineDecision.Decline,
            TestContext.Current.CancellationToken);

        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal(ClientSessionDisposition.Unavailable, result.SessionDisposition);
        Assert.Equal("session_unavailable", result.Error?.Code);
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
            [choices.Heroes.Single(choice => choice.Key == "spider_man").Key],
            choices.Scenarios.Single(choice => choice.Key == "rhino").Key,
            ModularConfiguration.Recommended,
            ModularKeys: [],
            Seed: "7");

    private static EngineDecision VisibleDecision(Prompt prompt)
    {
        var composer = new DecisionComposer(prompt);
        if (prompt.Cancellable)
        {
            Assert.True(composer.TryDecline(out EngineDecision? declined, out _));
            return declined!;
        }

        Affordance offered = prompt.Affordances.First(option => option.IsLegal);
        composer.SelectAffordance(offered.Id);
        Assert.True(
            composer.TryBuild(out EngineDecision? submitted, out string? error),
            error);
        return submitted!;
    }

    private static void AssertEquivalentResponses(
        EngineResponse local,
        EngineResponse remote) =>
        Assert.Equal(
            EngineJson.Write(local with { Capability = null, RequestId = "request" }),
            EngineJson.Write(remote with { Capability = null, RequestId = "request" }));

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

    private sealed class SequenceCapabilityIssuer(params string[] capabilities)
        : ISessionCapabilityIssuer
    {
        private readonly Queue<string> remaining = new(capabilities);

        public string Issue() => remaining.Dequeue();
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

    private sealed class RecoveringTransport(EngineResponse current) : IEngineTransport
    {
        public List<EngineRequest> Requests { get; } = [];

        public ValueTask<EngineResponse> ExchangeAsync(
            EngineRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            if (Requests.Count == 1)
            {
                return ValueTask.FromException<EngineResponse>(
                    new IOException("response lost"));
            }

            return ValueTask.FromResult(current with
            {
                RequestId = request.RequestId,
                GameId = request.GameId,
                Capability = null,
                Events = [],
            });
        }
    }

    private sealed class CollectingOperationalSink : IOperationalSink
    {
        private readonly ConcurrentQueue<OperationalRecord> records = new();

        public IReadOnlyList<OperationalRecord> Records => [.. records];

        public void Write(OperationalRecord record) => records.Enqueue(record);
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
