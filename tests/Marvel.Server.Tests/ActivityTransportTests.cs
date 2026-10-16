using Marvel.Transport;
using Marvel.Protocol;
using System.Net;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ActivityTransportTests : TransportTestBase
{
    [Fact]
    public void LegalCoreGameEmitsRoundAndPhaseAtTheirOccurrenceBoundaries()
    {
        // Marker shape is an engine choice; rr:game-round orders player then villain phase.
        Game game = DatasetGameFactory.Load(RepositoryPaths.Root)
            .Create(new GameSpecification("rhino", ["spider_man"], [], Seed: 1)).Game;
        Resolution opening = game.Resolve(new Decision(Assert.Single(game.Pending!.Affordances).Id, []));
        PhaseStarted player = Assert.Single(opening.Events.OfType<PhaseStarted>());
        Assert.Equal("Player", player.Phase);
        Assert.Equal(1, player.Round);
        game.Resolve(Decision.Decline);
        Resolution ending = game.Resolve(new Decision(Assert.Single(game.Pending!.Affordances).Id, []));
        PhaseStarted villain = Assert.Single(ending.Events.OfType<PhaseStarted>(), phase => phase.Phase == "Villain");
        Assert.Equal(1, villain.Round);
        Assert.All(ending.Events.OfType<ActivityCompleted>(), activity => Assert.Equal(1, activity.Round));
    }

    [Fact]
    public async Task BothTransportsRetainExplicitBatchAndOccurrenceContext()
    {
        // Synthetic authorized contract: a concealed participant contributes only public quantity.
        var request = EngineRequest.SyncGame("activity", "table", "capability");
        List<GameEvent> events =
        [
            new CardsMoved(AreaRef.Player("PlayerDeck", 0), AreaRef.Player("HandsArea", 0), [new Landing(7, 0)])
                { ActivityBatch = 0, Round = 2, Verb = "Draw" },
            new ActivityCompleted(0, [new ActivityParticipant(0, 1, [7]), new ActivityParticipant(1, 3, [])],
                ActivityPhaseStep.DrawToHandSize) { Round = 2, Verb = "Draw" },
            new PhaseStarted("Villain") { Round = 2, Verb = "Phase_Started" },
        ];
        ActivityBatches.Append(events,
        [
            new CardsMoved(AreaRef.Player("HandsArea", 0), AreaRef.Player("DiscardPile", 0), [new Landing(9, 0)])
                { ActivityBatch = 0, Round = 2, Verb = "Discard" },
            new ActivityCompleted(0, [new ActivityParticipant(0, 1, [9])]) { Round = 2, Verb = "Discard" },
        ]);
        var response = new EngineResponse(EngineProtocol.Version, request.RequestId,
            request.GameId, Capability: null, Prompt: null, Events: events);
        var endpoint = new EchoEndpoint(request, response);
        EngineResponse local = await new InProcessTransport(endpoint).ExchangeAsync(request,
            TestContext.Current.CancellationToken);
        EngineResponse socket = await ExchangeOverSocket(new SocketEngineServer(endpoint, IPAddress.Loopback, 0), request);
        Assert.Equal(EngineJson.Write(local), EngineJson.Write(socket));
        foreach (EngineResponse restored in new[] { local, socket })
        {
            Assert.Equal(0, restored.Events![0].ActivityBatch);
            ActivityCompleted receipt = Assert.IsType<ActivityCompleted>(restored.Events[1]);
            Assert.Equal(ActivityPhaseStep.DrawToHandSize, receipt.PhaseStep);
            Assert.Equal(3, receipt.Participants[1].Count);
            Assert.Empty(receipt.Participants[1].Cards);
            PhaseStarted phase = Assert.IsType<PhaseStarted>(restored.Events[2]);
            Assert.Equal(2, phase.Round);
            Assert.Equal("Villain", phase.Phase);
            ActivityCompleted discard = Assert.IsType<ActivityCompleted>(restored.Events[4]);
            Assert.NotEqual(receipt.Batch, discard.Batch);
            Assert.Equal(discard.Batch, restored.Events[3].ActivityBatch);
            Assert.Equal([9], discard.Participants[0].Cards);
        }
    }
}
