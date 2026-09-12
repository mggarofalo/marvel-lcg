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
public abstract class RestrictedMultiplayerJourneyTestBase
{
    protected const string GameId = "restricted-rhino-journey";
    protected static string? EntryErrorMessage(ClientEntryResult result) => result.Error?.Message;
    protected static string? EntryErrorCode(ClientEntryResult result) => result.Error?.Code;
    protected static void AssertTerminalJourney(EngineResponse[] views, bool[] testedWrongSeat, bool[] sawPrivateSearch, bool testedStale, bool testedDroppedResponse, bool restarted, int decisions)
    {
        var owner = Assert.IsType<WorldDescriptor>(views[0].World);
        var guest = Assert.IsType<WorldDescriptor>(views[1].World);
        Assert.NotEqual(Outcome.Unfinished, owner.Outcome);
        Assert.Equal(owner.Outcome, guest.Outcome);
        Assert.Null(views[0].Prompt);
        Assert.Null(views[1].Prompt);
        Assert.All(testedWrongSeat, Assert.True);
        Assert.All(sawPrivateSearch, Assert.True);
        Assert.True(testedStale);
        Assert.True(testedDroppedResponse);
        Assert.True(restarted);
        Assert.Equal(10, decisions);
        Assert.Equal(Outcome.VillainWins, owner.Outcome);
    }

    protected static void AssertDiagnostics(OperationalLog log, StringWriter diagnostics, CollectingExporter exporter, ClientSession[] sessions, SeatInvitation invitation)
    {
        log.Flush(TimeSpan.FromSeconds(3));
        string rendered = diagnostics.ToString();
        Assert.Contains(OperationalEventIds.SessionRestored, rendered, StringComparison.Ordinal);
        string envelopes = JsonSerializer.Serialize(exporter.Envelopes);
        foreach (string secret in sessions.Select(session => session.Capability).Append(invitation.Invitation))
        {
            Assert.DoesNotContain(secret, rendered, StringComparison.Ordinal);
            Assert.DoesNotContain(secret, envelopes, StringComparison.Ordinal);
        }

        Assert.Contains(exporter.Envelopes.SelectMany(envelope => envelope.Spans).GroupBy(span => span.TraceId), trace => trace.Any(span => span.Name == OperationalEventIds.TransportCompleted) && trace.Any(span => span.Name == OperationalEventIds.RequestCompleted));
    }

    protected static async Task Cleanup(RunningServer? server, OperationalLog log, string saveRoot)
    {
        if (server is not null)
            await server.StopAsync();
        log.Flush(TimeSpan.FromSeconds(3));
        if (Directory.Exists(saveRoot))
            Directory.Delete(saveRoot, recursive: true);
    }

    protected static int PromptedActor(EngineResponse[] views)
    {
        int[] prompted = Enumerable.Range(0, views.Length).Where(seat => views[seat].Prompt is not null).ToArray();
        return prompted.Length == 2 ? Assert.Single(prompted, seat => views[seat].Prompt!.Cancellable) : Assert.Single(prompted);
    }

    protected static async Task<EngineResponse[]> TestWrongSeat(LocalGameClient[] clients, ClientSession[] sessions, EngineResponse[] views, bool[] tested, int actor, int peer, EngineDecision decision)
    {
        if (tested[actor])
            return views;
        var denied = await clients[peer].ResolveAsync(sessions[peer], decision, TestContext.Current.CancellationToken);
        Assert.Equal(ClientMutationDisposition.Rejected, denied.MutationDisposition);
        Assert.Equal("not_your_turn", denied.Error?.Code);
        Assert.NotNull(denied.Response);
        tested[actor] = true;
        var unchanged = await SynchronizeBoth(clients, sessions);
        Assert.Equal(views[actor].Revision, unchanged[actor].Revision);
        AssertSamePrompt(views[actor].Prompt, unchanged[actor].Prompt);
        return unchanged;
    }

    protected static async Task<(EngineDecision? Baseline, bool Tested)> TestStaleDecision(LocalGameClient staleOwner, ClientSession owner, EngineResponse view, int actor, EngineDecision decision, EngineDecision? baseline, bool tested)
    {
        if (actor != 0)
            return (baseline, tested);
        if (baseline is null)
            return (decision, tested);
        if (tested)
            return (baseline, true);
        var stale = await staleOwner.ResolveAsync(owner, baseline, TestContext.Current.CancellationToken);
        Assert.Equal(ClientMutationDisposition.Rejected, stale.MutationDisposition);
        Assert.Equal("stale_decision", stale.Error?.Code);
        Assert.Equal(view.Revision, stale.Response?.Revision);
        AssertSamePrompt(view.Prompt, stale.Response?.Prompt);
        return (baseline, true);
    }

    protected static async Task<bool> TestDroppedResponse(IPEndPoint endpoint, ClientSession session, EngineResponse view, EngineDecision decision, bool tested, int decisions)
    {
        if (tested || decisions < 2)
            return false;
        var disconnected = new LocalGameClient(new DropCommittedResponseTransport(new SocketTransport(endpoint.Address.ToString(), endpoint.Port)));
        var connected = await disconnected.SynchronizeAsync(session, TestContext.Current.CancellationToken);
        Assert.Null(connected.Error);
        var recovered = await disconnected.ResolveAsync(session, decision, TestContext.Current.CancellationToken);
        Assert.Equal(ClientMutationDisposition.Uncertain, recovered.MutationDisposition);
        Assert.Equal("transport_unavailable", recovered.Error?.Code);
        Assert.Equal(view.Revision + 1, recovered.Response?.Revision);
        return true;
    }

    protected static async Task ResolveAccepted(LocalGameClient client, ClientSession session, EngineDecision decision)
    {
        var resolved = await client.ResolveAsync(session, decision, TestContext.Current.CancellationToken);
        Assert.Equal(ClientMutationDisposition.Accepted, resolved.MutationDisposition);
        Assert.Equal(ClientSessionDisposition.Active, resolved.SessionDisposition);
        Assert.Null(resolved.Error);
        Assert.NotNull(resolved.Response);
    }

    protected static async Task<(RunningServer Server, LocalGameClient[] Clients, EngineResponse[] Views)> Restart(RunningServer server, DatasetGameFactory factory, string saveRoot, OperationalLog log, IPEndPoint endpoint, ClientSession[] sessions, Func<LocalGameClient> newClient)
    {
        await server.StopAsync();
        server = await RunningServer.StartAsync(new EngineHost(factory, visibility: new RestrictedVisibilityPolicy(0), store: new FileSessionStore(saveRoot), log: log), endpoint.Port);
        Assert.Equal(endpoint, server.Endpoint);
        LocalGameClient[] clients = [newClient(), newClient()];
        var views = await SynchronizeBoth(clients, sessions);
        AssertRestrictedViews(views);
        Assert.Equal(views[0].Revision, views[1].Revision);
        return (server, clients, views);
    }

    protected static void AssertOffTurnPrompt(EngineResponse view, int peer)
    {
        if (view.Prompt is not { } prompt)
            return;
        Assert.Equal(peer, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.All(prompt.Affordances, option =>
        {
            Assert.Equal(Game.ActionVerb, option.Verb);
            Assert.Equal(peer, option.AnchorPlayer);
        });
    }

    protected static void ObservePrivateSearch(EngineResponse view, int actor, Prompt prompt, bool[] observed)
    {
        var search = prompt.Affordances.Select(option => option.Targets).FirstOrDefault(targets => targets?.IsSearch == true);
        if (search is null)
            return;
        var visible = Hand(view, actor).Select(card => card.Id!.Value).ToHashSet();
        Assert.All(search.Legal, id => Assert.Contains(id, visible));
        observed[actor] = true;
    }

    protected static async Task<EngineResponse[]> SynchronizeBoth(LocalGameClient[] clients, ClientSession[] sessions)
    {
        var responses = new EngineResponse[2];
        for (int seat = 0; seat < responses.Length; seat++)
        {
            ClientSynchronizationResult synchronized = await clients[seat].SynchronizeAsync(sessions[seat], TestContext.Current.CancellationToken);
            Assert.Equal(ClientSessionDisposition.Active, synchronized.SessionDisposition);
            Assert.Null(synchronized.Error);
            responses[seat] = Assert.IsType<EngineResponse>(synchronized.Response);
            Assert.Empty(responses[seat].Events);
        }

        return responses;
    }

    protected static void AssertRestrictedViews(EngineResponse[] views)
    {
        WorldDescriptor owner = Assert.IsType<WorldDescriptor>(views[0].World);
        WorldDescriptor guest = Assert.IsType<WorldDescriptor>(views[1].World);
        AssertPublicAgreement(owner, guest);
        for (int seat = 0; seat < 2; seat++)
        {
            CardDescriptor[] own = HandAndRemoved(views[seat], seat);
            CardDescriptor[] hidden = HandAndRemoved(views[1 - seat], seat);
            Assert.Equal(own.Length, hidden.Length);
            Assert.All(own, card =>
            {
                Assert.NotNull(card.Id);
                Assert.NotNull(card.Face);
            });
            Assert.All(hidden, card =>
            {
                Assert.Null(card.Id);
                Assert.Null(card.Face);
            });
        }

        foreach (WorldDescriptor world in new[]
        {
            owner,
            guest
        }

        )
        {
            Assert.All(world.Areas.Where(area => area.Zone is nameof(DeckType.PlayerDeck) or nameof(DeckType.EncounterDeck)).SelectMany(area => area.Cards.Concat(area.Removed)), card =>
            {
                Assert.Null(card.Id);
                Assert.Null(card.Face);
            });
            Assert.All(world.Areas.Where(area => area.Zone != nameof(DeckType.HandsArea)).SelectMany(area => area.Cards.Concat(area.Removed)).Where(card => card.Back == CardBack.Player && card.FaceUp), card => Assert.NotNull(card.Face));
        }
    }

    protected static void AssertPublicAgreement(WorldDescriptor? expected, WorldDescriptor? actual)
    {
        WorldDescriptor left = HideHands(Assert.IsType<WorldDescriptor>(expected));
        WorldDescriptor right = HideHands(Assert.IsType<WorldDescriptor>(actual));
        var leftResponse = new EngineResponse(EngineProtocol.Version, "public", GameId, null, null, [], left);
        var rightResponse = leftResponse with
        {
            World = right
        };
        Assert.Equal(EngineJson.Write(leftResponse), EngineJson.Write(rightResponse));
    }

    protected static void AssertSamePrompt(Prompt? expected, Prompt? actual)
    {
        var left = new EngineResponse(EngineProtocol.Version, "prompt", GameId, null, expected, []);
        Assert.Equal(EngineJson.Write(left), EngineJson.Write(left with { Prompt = actual }));
    }

    protected static WorldDescriptor HideHands(WorldDescriptor world) => world with
    {
        Areas = world.Areas.Select(area => area.Zone == nameof(DeckType.HandsArea) ? area with { Cards = area.Cards.Select(Hide).ToArray(), Removed = area.Removed.Select(Hide).ToArray(), } : area).ToArray(),
    };
    protected static CardDescriptor Hide(CardDescriptor card) => card with
    {
        Id = null,
        Face = null
    };
    protected static IReadOnlyList<CardDescriptor> Hand(EngineResponse response, int seat) => Assert.Single(Assert.IsType<WorldDescriptor>(response.World).Areas, area => area.Zone == nameof(DeckType.HandsArea) && area.Owner == seat).Cards;
    protected static CardDescriptor[] HandAndRemoved(EngineResponse response, int seat)
    {
        AreaDescriptor hand = Assert.Single(Assert.IsType<WorldDescriptor>(response.World).Areas, area => area.Zone == nameof(DeckType.HandsArea) && area.Owner == seat);
        return hand.Cards.Concat(hand.Removed).ToArray();
    }

    protected static EngineDecision VisibleDecision(Prompt prompt)
    {
        if (prompt.Cancellable)
        {
            return EngineDecision.Decline;
        }

        if (prompt.Affordances.SingleOrDefault(option => string.Equals(option.Verb, Game.ResolveMulligans, StringComparison.Ordinal))is { } mulligan)
        {
            return new EngineDecision(mulligan.Id, []);
        }

        foreach (Affordance offered in prompt.Affordances.Where(option => option.IsLegal).OrderByDescending(option => string.Equals(option.Verb, Game.EndPhaseVerb, StringComparison.Ordinal)))
        {
            var composer = new DecisionComposer(prompt);
            composer.SelectAffordance(offered.Id);
            if (offered.Targets is { } targets)
            {
                composer.SelectTargets(FirstLegalSelection(targets));
            }

            if (composer.TryBuild(out EngineDecision? decision, out _))
            {
                return decision!;
            }
        }

        throw new InvalidOperationException($"no free visible answer can advance prompt '{prompt.Label}'");
    }

    protected static IReadOnlyList<int> FirstLegalSelection(TargetRequest request)
    {
        if (request.IsGrouped)
        {
            return request.Groups![0];
        }

        if (!request.AllowRepeated)
        {
            return request.Legal.Take(request.Min).ToArray();
        }

        var selected = new List<int>();
        foreach (int candidate in request.Legal)
        {
            int maximum = request.MaximumOccurrences?.GetValueOrDefault(candidate) ?? request.Max;
            while (selected.Count < request.Min && selected.Count(chosen => chosen == candidate) < maximum)
            {
                selected.Add(candidate);
            }
        }

        Assert.True(request.Allows(selected));
        return selected;
    }

    protected sealed class DropCommittedResponseTransport(IEngineTransport inner) : IEngineTransport
    {
        private bool dropped;
        public async ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default)
        {
            EngineResponse response = await inner.ExchangeAsync(request, cancellationToken);
            if (!dropped && request.Operation == EngineProtocol.Resolve)
            {
                dropped = true;
                throw new EngineTransportException(requestMayHaveCommitted: true, new IOException("the committed response was disconnected"));
            }

            return response;
        }
    }

    protected sealed class CollectingExporter : ITelemetryExporter
    {
        private readonly ConcurrentQueue<TelemetryEnvelope> envelopes = new();
        public IReadOnlyList<TelemetryEnvelope> Envelopes => [..envelopes];

        public void Export(TelemetryEnvelope envelope) => envelopes.Enqueue(envelope);
    }

    protected sealed class RunningServer : IAsyncDisposable
    {
        private readonly CancellationTokenSource stopping;
        private readonly Task running;
        private bool stopped;
        private RunningServer(IPEndPoint endpoint, CancellationTokenSource stopping, Task running)
        {
            Endpoint = endpoint;
            this.stopping = stopping;
            this.running = running;
        }

        public IPEndPoint Endpoint { get; }

        public static async Task<RunningServer> StartAsync(EngineHost host, int port)
        {
            var stopping = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            stopping.CancelAfter(TimeSpan.FromSeconds(30));
            var listening = new TaskCompletionSource<IPEndPoint>(TaskCreationOptions.RunContinuationsAsynchronously);
            var socket = new SocketEngineServer(host, IPAddress.Loopback, port);
            Task running = Task.Run(() => socket.Run(listening.SetResult, stopping.Token), TestContext.Current.CancellationToken);
            try
            {
                Task completed = await Task.WhenAny(listening.Task, running).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                if (completed == running)
                {
                    await running;
                    throw new InvalidOperationException("the socket server exited before publishing its endpoint");
                }

                return new RunningServer(await listening.Task, stopping, running);
            }
            catch
            {
                stopping.Cancel();
                try
                {
                    await running.WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch
                {
                }

                stopping.Dispose();
                throw;
            }
        }

        public async ValueTask StopAsync()
        {
            if (stopped)
            {
                return;
            }

            stopped = true;
            stopping.Cancel();
            await running.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            stopping.Dispose();
        }

        public ValueTask DisposeAsync() => StopAsync();
    }
}
