using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;
public sealed class RestrictedMultiplayerJourneyTwoRestrictedSocketClientsCompleteOneSeededCoreGTests : RestrictedMultiplayerJourneyTestBase
{
    [Fact]
    public async Task TwoRestrictedSocketClientsCompleteOneSeededCoreGame()
    {
        string saveRoot = Path.Combine(Path.GetTempPath(), $"marvel-restricted-journey-{Guid.NewGuid():N}");
        var diagnostics = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        var exporter = new CollectingExporter();
        var log = new OperationalLog(new CompositeOperationalSink(new JsonTextOperationalSink(diagnostics), new OperationalTelemetrySink(exporter)), "journey");
        DatasetGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        RunningServer? server = null;
        try
        {
            server = await RunningServer.StartAsync(new EngineHost(factory, visibility: new RestrictedVisibilityPolicy(0), store: new FileSessionStore(saveRoot), log: log), port: 0);
            IPEndPoint endpoint = server.Endpoint;
            LocalGameClient NewClient() => new(new SocketTransport(endpoint.Address.ToString(), endpoint.Port, log), log);
            LocalGameClient[] clients = [NewClient(), NewClient()];
            ClientEntryResult owner = await clients[0].OpenSessionAsync(GameId, new GameSpecification("rhino", ["spider_man", "captain_marvel"], [], Seed: 7), TestContext.Current.CancellationToken);
            Assert.True(owner.Succeeded, EntryErrorMessage(owner));
            SeatInvitation invitation = Assert.Single(owner.Invitations);
            Assert.Equal(1, invitation.Seat);
            ClientEntryResult guessed = await clients[1].AttachAsync(GameId, "guessed-invitation", TestContext.Current.CancellationToken);
            ClientEntryResult wrongLabel = await clients[1].AttachAsync("wrong-table", invitation.Invitation, TestContext.Current.CancellationToken);
            Assert.Equal("invitation_unavailable", EntryErrorCode(guessed));
            Assert.Equal("invitation_unavailable", EntryErrorCode(wrongLabel));
            ClientEntryResult guest = await clients[1].AttachAsync(GameId, invitation.Invitation, TestContext.Current.CancellationToken);
            ClientEntryResult replay = await clients[1].AttachAsync(GameId, invitation.Invitation, TestContext.Current.CancellationToken);
            Assert.True(guest.Succeeded, EntryErrorMessage(guest));
            Assert.Equal("invitation_unavailable", EntryErrorCode(replay));
            ClientSession[] sessions = [owner.Session!, guest.Session!];
            Assert.NotEqual(sessions[0].Capability, sessions[1].Capability);
            // This second owner client retains revision zero while the primary
            // owner advances. It later proves that a syntactically reusable
            // old answer cannot cross a prompt revision.
            var staleOwner = NewClient();
            ClientSynchronizationResult staleBaseline = await staleOwner.SynchronizeAsync(sessions[0], TestContext.Current.CancellationToken);
            Assert.Null(staleBaseline.Error);
            EngineResponse[] views = [owner.Response!, guest.Response!, ];
            bool[] testedWrongSeat = [false, false ];
            bool[] sawPrivateSearch = [false, false ];
            bool testedStale = false;
            bool testedDroppedResponse = false;
            bool restarted = false;
            EngineDecision? ownerRevisionZeroDecision = null;
            int decisions = 0;
            while (views[0].World!.Outcome == Outcome.Unfinished)
            {
                Assert.True(decisions < 600, "the restricted socket game did not terminate");
                views = await SynchronizeBoth(clients, sessions);
                AssertRestrictedViews(views);
                if (views[0].World!.Outcome != Outcome.Unfinished)
                {
                    break;
                }

                int actor = PromptedActor(views);
                int peer = 1 - actor;
                Prompt prompt = Assert.IsType<Prompt>(views[actor].Prompt);
                Assert.Equal(actor, prompt.Player);
                AssertOffTurnPrompt(views[peer], peer);
                ObservePrivateSearch(views[actor], actor, prompt, sawPrivateSearch);
                EngineDecision decision = VisibleDecision(prompt);
                views = await TestWrongSeat(clients, sessions, views, testedWrongSeat, actor, peer, decision);
                (ownerRevisionZeroDecision, testedStale) = await TestStaleDecision(staleOwner, sessions[0], views[0], actor, decision, ownerRevisionZeroDecision, testedStale);
                if (await TestDroppedResponse(endpoint, sessions[actor], views[actor], decision, testedDroppedResponse, decisions))
                {
                    testedDroppedResponse = true;
                    decisions++;
                    continue;
                }

                await ResolveAccepted(clients[actor], sessions[actor], decision);
                decisions++;
                if (!restarted && decisions >= 2)
                {
                    (server, clients, views) = await Restart(server, factory, saveRoot, log, endpoint, sessions, NewClient);
                    restarted = true;
                }
            }

            views = await SynchronizeBoth(clients, sessions);
            AssertRestrictedViews(views);
            AssertTerminalJourney(views, testedWrongSeat, sawPrivateSearch, testedStale, testedDroppedResponse, restarted, decisions);
            EngineResponse[] terminalAgain = await SynchronizeBoth(clients, sessions);
            Assert.Equal(views[0].Revision, terminalAgain[0].Revision);
            Assert.Equal(views[1].Revision, terminalAgain[1].Revision);
            AssertPublicAgreement(views[0].World, terminalAgain[0].World);
            AssertPublicAgreement(views[1].World, terminalAgain[1].World);
            AssertDiagnostics(log, diagnostics, exporter, sessions, invitation);
        }
        finally
        {
            await Cleanup(server, log, saveRoot);
        }
    }
}
