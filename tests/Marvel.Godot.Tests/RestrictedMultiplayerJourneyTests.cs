using System.Net;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class RestrictedMultiplayerJourneyTests
{
    private const string GameId = "restricted-rhino-journey";

    [Fact]
    public async Task TwoRestrictedSocketClientsCompleteOneSeededCoreGame()
    {
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        stopping.CancelAfter(TimeSpan.FromSeconds(30));
        var listening = new TaskCompletionSource<IPEndPoint>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var server = new SocketEngineServer(
            new EngineHost(
                DatasetGameFactory.Load(RepositoryPaths.Root),
                visibility: new RestrictedVisibilityPolicy(0)),
            IPAddress.Loopback,
            port: 0);
        Task running = Task.Run(
            () => server.Run(listening.SetResult, stopping.Token),
            TestContext.Current.CancellationToken);

        try
        {
            IPEndPoint endpoint = await listening.Task.WaitAsync(
                TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            LocalGameClient NewClient() => new(new SocketTransport(
                endpoint.Address.ToString(), endpoint.Port));
            LocalGameClient[] clients = [NewClient(), NewClient()];

            ClientEntryResult owner = await clients[0].OpenSessionAsync(
                GameId,
                new GameSpecification(
                    "rhino", ["spider_man", "captain_marvel"], [], Seed: 7),
                TestContext.Current.CancellationToken);
            Assert.True(owner.Succeeded, owner.Error?.Message);
            SeatInvitation invitation = Assert.Single(owner.Invitations);
            Assert.Equal(1, invitation.Seat);

            ClientEntryResult guessed = await clients[1].AttachAsync(
                GameId, "guessed-invitation", TestContext.Current.CancellationToken);
            ClientEntryResult wrongLabel = await clients[1].AttachAsync(
                "wrong-table", invitation.Invitation, TestContext.Current.CancellationToken);
            Assert.Equal("invitation_unavailable", guessed.Error?.Code);
            Assert.Equal("invitation_unavailable", wrongLabel.Error?.Code);

            ClientEntryResult guest = await clients[1].AttachAsync(
                GameId, invitation.Invitation, TestContext.Current.CancellationToken);
            ClientEntryResult replay = await clients[1].AttachAsync(
                GameId, invitation.Invitation, TestContext.Current.CancellationToken);
            Assert.True(guest.Succeeded, guest.Error?.Message);
            Assert.Equal("invitation_unavailable", replay.Error?.Code);
            ClientSession[] sessions = [owner.Session!, guest.Session!];
            Assert.NotEqual(sessions[0].Capability, sessions[1].Capability);

            // This second owner client retains revision zero while the primary
            // owner advances. It later proves that a syntactically reusable
            // old answer cannot cross a prompt revision.
            var staleOwner = NewClient();
            ClientSynchronizationResult staleBaseline = await staleOwner.SynchronizeAsync(
                sessions[0], TestContext.Current.CancellationToken);
            Assert.Null(staleBaseline.Error);

            EngineResponse[] views =
            [
                owner.Response!,
                guest.Response!,
            ];
            bool[] testedWrongSeat = [false, false];
            bool[] sawPrivateSearch = [false, false];
            bool testedStale = false;
            bool testedDroppedResponse = false;
            bool reconnected = false;
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

                int[] prompted = Enumerable.Range(0, views.Length)
                    .Where(seat => views[seat].Prompt is not null)
                    .ToArray();
                int actor = prompted.Length == 2
                    ? Assert.Single(prompted, seat => views[seat].Prompt!.Cancellable)
                    : Assert.Single(prompted);
                int peer = 1 - actor;
                Prompt prompt = Assert.IsType<Prompt>(views[actor].Prompt);
                Assert.Equal(actor, prompt.Player);
                if (views[peer].Prompt is { } offTurn)
                {
                    Assert.Equal(peer, offTurn.Player);
                    Assert.False(offTurn.Cancellable);
                    Assert.All(offTurn.Affordances, option =>
                    {
                        Assert.Equal(Game.ActionVerb, option.Verb);
                        Assert.Equal(peer, option.AnchorPlayer);
                    });
                }
                TargetRequest? search = prompt.Affordances
                    .Select(option => option.Targets)
                    .FirstOrDefault(targets => targets?.IsSearch == true);
                if (search is not null)
                {
                    HashSet<int> visibleHandIds = Hand(views[actor], actor)
                        .Select(card => card.Id!.Value)
                        .ToHashSet();
                    Assert.All(search.Legal, id => Assert.Contains(id, visibleHandIds));
                    sawPrivateSearch[actor] = true;
                }
                EngineDecision decision = VisibleDecision(prompt);

                if (!testedWrongSeat[actor])
                {
                    ClientResolutionResult denied = await clients[peer].ResolveAsync(
                        sessions[peer], decision, TestContext.Current.CancellationToken);
                    Assert.Equal(ClientMutationDisposition.Rejected, denied.MutationDisposition);
                    Assert.Equal("not_your_turn", denied.Error?.Code);
                    Assert.NotNull(denied.Response);
                    testedWrongSeat[actor] = true;
                    EngineResponse[] unchanged = await SynchronizeBoth(clients, sessions);
                    Assert.Equal(views[actor].Revision, unchanged[actor].Revision);
                    AssertSamePrompt(views[actor].Prompt, unchanged[actor].Prompt);
                    views = unchanged;
                }

                if (actor == 0 && ownerRevisionZeroDecision is null)
                {
                    ownerRevisionZeroDecision = decision;
                }
                else if (actor == 0 && !testedStale)
                {
                    ClientResolutionResult stale = await staleOwner.ResolveAsync(
                        sessions[0], ownerRevisionZeroDecision!,
                        TestContext.Current.CancellationToken);
                    Assert.Equal(ClientMutationDisposition.Rejected, stale.MutationDisposition);
                    Assert.Equal("stale_decision", stale.Error?.Code);
                    Assert.Equal(views[0].Revision, stale.Response?.Revision);
                    AssertSamePrompt(views[0].Prompt, stale.Response?.Prompt);
                    testedStale = true;
                }

                if (!testedDroppedResponse && decisions >= 2)
                {
                    var disconnected = new LocalGameClient(
                        new DropCommittedResponseTransport(new SocketTransport(
                            endpoint.Address.ToString(), endpoint.Port)));
                    ClientSynchronizationResult connected = await disconnected.SynchronizeAsync(
                        sessions[actor], TestContext.Current.CancellationToken);
                    Assert.Null(connected.Error);
                    ClientResolutionResult recovered = await disconnected.ResolveAsync(
                        sessions[actor], decision, TestContext.Current.CancellationToken);
                    Assert.Equal(
                        ClientMutationDisposition.Uncertain, recovered.MutationDisposition);
                    Assert.Equal("transport_unavailable", recovered.Error?.Code);
                    Assert.Equal(views[actor].Revision + 1, recovered.Response?.Revision);
                    testedDroppedResponse = true;
                    decisions++;
                    continue;
                }

                ClientResolutionResult resolved = await clients[actor].ResolveAsync(
                    sessions[actor], decision, TestContext.Current.CancellationToken);
                Assert.Equal(ClientMutationDisposition.Accepted, resolved.MutationDisposition);
                Assert.Equal(ClientSessionDisposition.Active, resolved.SessionDisposition);
                Assert.Null(resolved.Error);
                Assert.NotNull(resolved.Response);
                decisions++;

                if (!reconnected && decisions >= 2)
                {
                    clients = [NewClient(), NewClient()];
                    views = await SynchronizeBoth(clients, sessions);
                    AssertRestrictedViews(views);
                    reconnected = true;
                }
            }

            views = await SynchronizeBoth(clients, sessions);
            AssertRestrictedViews(views);
            WorldDescriptor ownerTerminal = Assert.IsType<WorldDescriptor>(views[0].World);
            WorldDescriptor guestTerminal = Assert.IsType<WorldDescriptor>(views[1].World);
            Assert.NotEqual(Outcome.Unfinished, ownerTerminal.Outcome);
            Assert.Equal(ownerTerminal.Outcome, guestTerminal.Outcome);
            Assert.Null(views[0].Prompt);
            Assert.Null(views[1].Prompt);
            Assert.All(testedWrongSeat, Assert.True);
            Assert.All(sawPrivateSearch, Assert.True);
            Assert.True(testedStale);
            Assert.True(testedDroppedResponse);
            Assert.True(reconnected);
            Assert.Equal(10, decisions);
            Assert.Equal(Outcome.VillainWins, ownerTerminal.Outcome);

            EngineResponse[] terminalAgain = await SynchronizeBoth(clients, sessions);
            Assert.Equal(views[0].Revision, terminalAgain[0].Revision);
            Assert.Equal(views[1].Revision, terminalAgain[1].Revision);
            AssertPublicAgreement(views[0].World, terminalAgain[0].World);
            AssertPublicAgreement(views[1].World, terminalAgain[1].World);
        }
        finally
        {
            stopping.Cancel();
            await running.WaitAsync(
                TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
    }

    private static async Task<EngineResponse[]> SynchronizeBoth(
        LocalGameClient[] clients,
        ClientSession[] sessions)
    {
        var responses = new EngineResponse[2];
        for (int seat = 0; seat < responses.Length; seat++)
        {
            ClientSynchronizationResult synchronized = await clients[seat].SynchronizeAsync(
                sessions[seat], TestContext.Current.CancellationToken);
            Assert.Equal(ClientSessionDisposition.Active, synchronized.SessionDisposition);
            Assert.Null(synchronized.Error);
            responses[seat] = Assert.IsType<EngineResponse>(synchronized.Response);
            Assert.Empty(responses[seat].Events);
        }

        return responses;
    }

    private static void AssertRestrictedViews(EngineResponse[] views)
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

        foreach (WorldDescriptor world in new[] { owner, guest })
        {
            Assert.All(
                world.Areas.Where(area => area.Zone is nameof(DeckType.PlayerDeck)
                    or nameof(DeckType.EncounterDeck))
                    .SelectMany(area => area.Cards.Concat(area.Removed)),
                card =>
                {
                    Assert.Null(card.Id);
                    Assert.Null(card.Face);
                });
            Assert.All(
                world.Areas.Where(area => area.Zone != nameof(DeckType.HandsArea))
                    .SelectMany(area => area.Cards.Concat(area.Removed))
                    .Where(card => card.Back == CardBack.Player && card.FaceUp),
                card => Assert.NotNull(card.Face));
        }
    }

    private static void AssertPublicAgreement(WorldDescriptor? expected, WorldDescriptor? actual)
    {
        WorldDescriptor left = HideHands(Assert.IsType<WorldDescriptor>(expected));
        WorldDescriptor right = HideHands(Assert.IsType<WorldDescriptor>(actual));
        var leftResponse = new EngineResponse(
            EngineProtocol.Version, "public", GameId, null, null, [], left);
        var rightResponse = leftResponse with { World = right };
        Assert.Equal(EngineJson.Write(leftResponse), EngineJson.Write(rightResponse));
    }

    private static void AssertSamePrompt(Prompt? expected, Prompt? actual)
    {
        var left = new EngineResponse(
            EngineProtocol.Version, "prompt", GameId, null, expected, []);
        Assert.Equal(EngineJson.Write(left), EngineJson.Write(left with { Prompt = actual }));
    }

    private static WorldDescriptor HideHands(WorldDescriptor world) => world with
    {
        Areas = world.Areas.Select(area => area.Zone == nameof(DeckType.HandsArea)
            ? area with
            {
                Cards = area.Cards.Select(Hide).ToArray(),
                Removed = area.Removed.Select(Hide).ToArray(),
            }
            : area).ToArray(),
    };

    private static CardDescriptor Hide(CardDescriptor card) =>
        card with { Id = null, Face = null };

    private static IReadOnlyList<CardDescriptor> Hand(EngineResponse response, int seat) =>
        Assert.Single(
            Assert.IsType<WorldDescriptor>(response.World).Areas,
            area => area.Zone == nameof(DeckType.HandsArea) && area.Owner == seat).Cards;

    private static CardDescriptor[] HandAndRemoved(
        EngineResponse response,
        int seat)
    {
        AreaDescriptor hand = Assert.Single(
            Assert.IsType<WorldDescriptor>(response.World).Areas,
            area => area.Zone == nameof(DeckType.HandsArea) && area.Owner == seat);
        return hand.Cards.Concat(hand.Removed).ToArray();
    }

    private static EngineDecision VisibleDecision(Prompt prompt)
    {
        if (prompt.Cancellable)
        {
            return EngineDecision.Decline;
        }

        if (prompt.Affordances.SingleOrDefault(option => string.Equals(
                option.Verb, Game.ResolveMulligans, StringComparison.Ordinal)) is { } mulligan)
        {
            return new EngineDecision(mulligan.Id, []);
        }

        foreach (Affordance offered in prompt.Affordances
                     .Where(option => option.IsLegal)
                     .OrderByDescending(option => string.Equals(
                         option.Verb, Game.EndPhaseVerb, StringComparison.Ordinal)))
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

        throw new InvalidOperationException(
            $"no free visible answer can advance prompt '{prompt.Label}'");
    }

    private static IReadOnlyList<int> FirstLegalSelection(TargetRequest request)
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
            int maximum = request.MaximumOccurrences?.GetValueOrDefault(candidate)
                ?? request.Max;
            while (selected.Count < request.Min && selected.Count(chosen => chosen == candidate) < maximum)
            {
                selected.Add(candidate);
            }
        }

        Assert.True(request.Allows(selected));
        return selected;
    }

    private sealed class DropCommittedResponseTransport(IEngineTransport inner)
        : IEngineTransport
    {
        private bool dropped;

        public async ValueTask<EngineResponse> ExchangeAsync(
            EngineRequest request,
            CancellationToken cancellationToken = default)
        {
            EngineResponse response = await inner.ExchangeAsync(request, cancellationToken);
            if (!dropped && request.Operation == EngineProtocol.Resolve)
            {
                dropped = true;
                throw new EngineTransportException(
                    requestMayHaveCommitted: true,
                    new IOException("the committed response was disconnected"));
            }

            return response;
        }
    }
}
