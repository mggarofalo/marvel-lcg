using System.Net;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Session;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ShuffleReceiptTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EndpointWithoutShuffleReceiptVocabularyIsRejected(bool socket)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        EngineRequest request = EngineRequest.OpenGame("old", "old-protocol",
            new GameSpecification("rhino", ["black_panther"], null, 4)) with { Version = 24 };
        EngineResponse response = socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal("unsupported_version", response.Error?.Code);
        Assert.Null(response.World);
    }

    [Fact]
    public void AConcealedSourceFaceContributesOnlyToTheReturnedCount()
    {
        Game game = DatasetGameFactory.Load(RepositoryPaths.Root)
            .Create(new GameSpecification("rhino", ["black_panther"], null, 4)).Game;
        game.Resolve(Decision.Take(game.Pending!.Affordances[0].Id, [13, 31, 16, 17, 38], []));
        game.Resolve(Decision.Take(game.Pending!.Affordances.Single(a => a.Label == "01048").Id));
        game.Resolve(Decision.Take(game.Pending!.Affordances.Single(a => a.AnchorId == 9).Id, [], [11]));
        // Synthetic privacy variation on the legal opening: a concealed discard
        // must not acquire a public name merely because it is being returned.
        game.State.Cards[16].TurnFaceDown();
        var resolved = game.Resolve(Decision.Take(game.Pending!.Affordances[0].Id, [16, 31], []));
        CardsShuffledIntoDeck receipt = Assert.Single(resolved.Events.OfType<CardsShuffledIntoDeck>());
        Assert.Equal(2, receipt.Count);
        Assert.Equal(["Med Team"], receipt.PublicTitles);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletedAncestralShuffleRetainsPublicReceiptAcrossTransportAndReplay(bool socket)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        async Task<EngineResponse> Exchange(EngineRequest request) => socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        const string game = "shuffle-receipt";
        EngineResponse current = await Exchange(EngineRequest.OpenGame("open", game,
            new GameSpecification("rhino", ["black_panther"], null, 4)));
        string capability = current.Capability!;
        async Task<EngineResponse> Choose(string request, EngineDecision answer) =>
            await Exchange(EngineRequest.ResolveGame(request, game, capability, answer, current.Revision));
        current = await Choose("mulligan", new EngineDecision(current.Prompt!.Affordances[0].Id, [13, 31, 16, 17, 38]));
        current = await Choose("setup", new EngineDecision(current.Prompt!.Affordances.Single(a => a.Label == "01048").Id, []));
        var draft = new DecisionComposer(current.Prompt!);
        draft.SelectAffordance(current.Prompt!.Affordances.Single(a => a.AnchorId == 9).Id);
        draft.ToggleResource(11);
        Assert.True(draft.TryBuild(out EngineDecision? payment, out string? error), error);
        current = await Choose("play", payment!);
        Assert.Null(current.Error);
        Assert.DoesNotContain(current.Events!, item => item is CardsShuffledIntoDeck);
        current = await Choose("shuffle", new EngineDecision(current.Prompt!.Affordances[0].Id, [16, 13, 31]));
        Assert.Null(current.Error);
        CardsShuffledIntoDeck receipt = Assert.Single(current.Events!.OfType<CardsShuffledIntoDeck>());
        Assert.Equal(0, receipt.Player);
        Assert.Equal(3, receipt.Count);
        Assert.Equal(["Med Team", "Vibranium", "Wakanda Forever!"], receipt.PublicTitles);
        Assert.Null(receipt.Subjects);
        Assert.IsType<CardsShuffledIntoDeck>(current.Events![0]);
        Assert.Contains(current.Events.Skip(1), item => item is CardsMoved { Verb: "Discard" });
        AreaDescriptor deck = Assert.Single(current.World!.Areas, area => area.Zone == "PlayerDeck" && area.Owner == 0);
        Assert.All(deck.Cards, card => { Assert.Null(card.Id); Assert.Null(card.Face); });
        EventBatchPresentation batch = EventCuePlanner.Plan(current.Events, current.World, Outcome.Unfinished);
        EventPresentation settled = batch.Cues[EventCuePlanner.SettledCueIndex(batch.Cues)];
        Assert.Equal("Black Panther shuffled 3 cards (Med Team, Vibranium, Wakanda Forever!) into their deck.", settled.Summary);
        Assert.Empty(settled.Anchors);
        Assert.Empty(settled.Relationships);
        Assert.Contains(ResponseReceiptPresenter.Present(current.Events, current.World, batch), item => item == settled);
        host = new EngineHost(factory, store: store);
        EngineResponse restored = await Exchange(EngineRequest.SyncGame("restore", game, capability));
        Assert.Null(restored.Error);
        Assert.Equal(current.Revision, restored.Revision);

        // v8 recordings lack this completed receipt, and replay compares exact events.
        StoredSession saved = Assert.Single(store.Load());
        var old = saved.Save with { Compatibility = saved.Save.Compatibility with { ReplayContract = "engine-replay-v8" } };
        Assert.Throws<SessionCompatibilityException>(() => SessionReplay.Verify(old, factory.Compatibility,
            _ => throw new InvalidOperationException("incompatible recordings must not execute")));
    }
}
