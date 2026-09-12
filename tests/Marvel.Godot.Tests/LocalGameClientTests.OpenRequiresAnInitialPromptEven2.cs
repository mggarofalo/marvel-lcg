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
public sealed class LocalGameClientOpenRequiresAnInitialPromptEvenTests : LocalGameClientTestBase
{
    [Fact]
    public async Task OpenRequiresAnInitialPromptEvenWhenTheReturnedWorldIsTerminal()
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame("source", "table", Specification()));
        var transport = new ScriptedTransport(complete with { RequestId = "local-open", World = complete.World!with { Outcome = Outcome.PlayersWin }, Prompt = null, });
        ClientEntryResult result = await new LocalGameClient(transport).OpenSessionAsync("table", Specification(), TestContext.Current.CancellationToken);
        Assert.Equal("invalid_response", result.Error?.Code);
    }

    [Fact]
    public async Task CommittedCoreChoicesOpenACompleteVisibleGame()
    {
        LocalClientConnection connection = LocalGameClient.ConnectLocal(RepositoryPaths.Root);
        Assert.True(connection.Succeeded);
        ClientSetupResult setup = await connection.Client!.ReadSetupAsync(TestContext.Current.CancellationToken);
        ClientStartupResult startup = await connection.Client.OpenAsync(Assert.IsType<SetupChoices>(setup.Choices), DefaultSelection(setup.Choices!), TestContext.Current.CancellationToken);
        EngineResponse opened = Assert.IsType<EngineResponse>(startup.Response);
        Assert.True(startup.Succeeded);
        Assert.Null(startup.Error);
        Assert.NotNull(opened.World);
        Assert.NotNull(opened.Prompt);
        Assert.NotNull(opened.Events);
        Assert.Equal(Outcome.Unfinished, opened.World.Outcome);
        Assert.Equal(LocalGameSession.GameId, opened.GameId);
        BoardPresentation board = BoardPresentation.From(opened.World);
        Assert.Equal(opened.World.Areas.Sum(area => area.Cards.Count), board.Areas.Sum(area => area.Cards.Sum(card => card.Count)));
        Assert.Equal(opened.World.Areas.Sum(area => area.Removed.Count), board.Areas.Sum(area => area.Removed.Sum(card => card.Count)));
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
        ClientStartupResult opened = await client.OpenAsync(Specification(), TestContext.Current.CancellationToken);
        ClientResolutionResult resolved = await client.ResolveAsync(opened.Response!.Capability!, new EngineDecision(Assert.Single(opened.Response.Prompt!.Affordances).Id, []), TestContext.Current.CancellationToken);
        Assert.True(resolved.Succeeded);
        Assert.NotNull(resolved.Response?.World);
        Assert.NotNull(resolved.Response?.Prompt);
        Assert.Null(resolved.Error);
    }

    [Fact]
    public async Task VisibleControlsCanPlayASeededLocalGameToItsEnding()
    {
        var client = new LocalGameClient(new InProcessTransport(Host()));
        ClientSetupResult setup = await client.ReadSetupAsync(TestContext.Current.CancellationToken);
        ClientStartupResult opened = await client.OpenAsync(setup.Choices!, DefaultSelection(setup.Choices!)with { Seed = "1" }, TestContext.Current.CancellationToken);
        EngineResponse current = Assert.IsType<EngineResponse>(opened.Response);
        string capability = Assert.IsType<string>(current.Capability);
        var labels = new List<string>();
        bool passed = false;
        int decisions = 0;
        while (current.Prompt is not null)
        {
            Assert.True(decisions < 20, $"local UI journey is still playing at '{current.Prompt.Label}'");
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
                Assert.True(composer.TryBuild(out EngineDecision? submitted, out string? error), error);
                decision = submitted!;
            }

            ClientResolutionResult resolved = await client.ResolveAsync(capability, decision, TestContext.Current.CancellationToken);
            Assert.True(resolved.Succeeded, resolved.Error?.Message);
            current = Assert.IsType<EngineResponse>(resolved.Response);
            decisions++;
        }

        Assert.Equal(7, decisions);
        Assert.True(passed, "the journey never used the visible pass control");
        Assert.Contains(labels, label => label.Contains("Mulligan", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(labels, label => label.Contains("End", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(Outcome.VillainWins, current.World?.Outcome);
        Assert.Equal(GameProgressKind.VillainWins, GameProgressPresentation.FromResponse(current).Kind);
    }

    [Fact]
    public async Task ARejectedDecisionSynchronizesWithoutRepeatingTheMutation()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("open", LocalGameSession.GameId, Specification()));
        var rejected = new EngineResponse(EngineProtocol.Version, "local-resolve", LocalGameSession.GameId, Capability: null, Prompt: null, Events: [], Error: new EngineError("stale_decision", "The prompt changed."));
        var transport = new ScriptedTransport(rejected, current with { RequestId = "local-recover", Capability = null, Events = [] });
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(current.Capability!, EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.True(result.HasAuthoritativeView);
        Assert.Equal(ClientMutationDisposition.Rejected, result.MutationDisposition);
        Assert.Equal("stale_decision", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync], transport.Requests.Select(request => request.Operation));
        Assert.NotNull(transport.Requests[0].Decision);
        Assert.Null(transport.Requests[1].Decision);
    }

    [Fact]
    public async Task AnUncertainResolveReadsStateAndNeverRetriesTheDecision()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("open", LocalGameSession.GameId, Specification()));
        var transport = new ScriptedTransport(new IOException("response lost"), current with { RequestId = "local-recover", Capability = null, Events = [] });
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(current.Capability!, EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.True(result.HasAuthoritativeView);
        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal("transport_unavailable", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync], transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task AMalformedResolveReadsStateAndNeverRetriesTheDecision()
    {
        EngineResponse current = Host().Exchange(EngineRequest.OpenGame("open", LocalGameSession.GameId, Specification()));
        var transport = new ScriptedTransport(current with { RequestId = "local-resolve", Prompt = current.Prompt!with { Affordances = null! }, }, current with { RequestId = "local-recover", Capability = null, Events = [] });
        ClientResolutionResult result = await new LocalGameClient(transport).ResolveAsync(current.Capability!, EngineDecision.Decline, TestContext.Current.CancellationToken);
        Assert.True(result.HasAuthoritativeView);
        Assert.Equal(ClientMutationDisposition.Uncertain, result.MutationDisposition);
        Assert.Equal("invalid_response", result.Error?.Code);
        Assert.Equal([EngineProtocol.Resolve, EngineProtocol.Sync], transport.Requests.Select(request => request.Operation));
    }

    [Fact]
    public async Task ACompleteJourneyIsIdenticalOverLocalAndRemoteComposition()
    {
        LocalGameClient local = ClientComposition.Connect(RepositoryPaths.Root, configuredEndpoint: null).Client!;
        ClientSetupResult localSetup = await local.ReadSetupAsync(TestContext.Current.CancellationToken);
        ClientStartupResult localOpen = await local.OpenAsync(localSetup.Choices!, DefaultSelection(localSetup.Choices!)with { Seed = "1" }, TestContext.Current.CancellationToken);
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
            LocalGameClient remote = ClientComposition.Connect(dataRoot: "content-must-not-be-read-for-remote-composition", $"tcp://127.0.0.1:{port}").Client!;
            ClientSetupResult remoteSetup = await remote.ReadSetupAsync(TestContext.Current.CancellationToken);
            Assert.Equal(localSetup.Choices!.Heroes.Select(choice => (choice.Key, choice.Name)), remoteSetup.Choices!.Heroes.Select(choice => (choice.Key, choice.Name)));
            ClientStartupResult remoteOpen = await remote.OpenAsync(remoteSetup.Choices, DefaultSelection(remoteSetup.Choices)with { Seed = "1" }, TestContext.Current.CancellationToken);
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
                ClientResolutionResult localResolved = await local.ResolveAsync(localCapability, decision, TestContext.Current.CancellationToken);
                ClientResolutionResult remoteResolved = await remote.ResolveAsync(remoteCapability, decision, TestContext.Current.CancellationToken);
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
            catch (SocketException)when (!listener.Server.IsBound)
            {
            }
            catch (InvalidOperationException)when (!listener.Server.IsBound)
            {
            }
        }

        Assert.Equal(9, Volatile.Read(ref exchanges));
    }

    [Fact]
    public void MissingContentBecomesABoundedProductError()
    {
        string missing = Path.Combine(Path.GetTempPath(), "marvel-missing-content", Guid.NewGuid().ToString("N"));
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
        var response = new EngineResponse(EngineProtocol.Version, "local-setup", GameId: string.Empty, Capability: null, Prompt: null, Events: [], Setup: null);
        ClientSetupResult setup = await new LocalGameClient(new FixedTransport(response)).ReadSetupAsync(TestContext.Current.CancellationToken);
        Assert.Equal("invalid_response", setup.Error?.Code);
    }

    [Fact]
    public async Task SetupDiscoveryRejectsUnknownRecommendations()
    {
        SetupChoices choices = Choices();
        SetupChoices malformed = choices with
        {
            Scenarios = [choices.Scenarios[0] with
            {
                RecommendedModularSets = ["future_encounter"],
            }, ],
        };
        var response = new EngineResponse(EngineProtocol.Version, "local-setup", GameId: string.Empty, Capability: null, Prompt: null, Events: [], Setup: malformed);
        ClientSetupResult setup = await new LocalGameClient(new FixedTransport(response)).ReadSetupAsync(TestContext.Current.CancellationToken);
        Assert.Equal("invalid_response", setup.Error?.Code);
    }
}
