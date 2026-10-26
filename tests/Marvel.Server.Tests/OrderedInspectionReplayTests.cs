using System.Net;
using System.Text.Json;
using Marvel.Decisions;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Transport;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>Synthetic mechanism evidence, not actual Heimdall or Thor admission.</summary>
public sealed class OrderedInspectionReplayTests : TransportTestBase
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(false, 4)]
    [InlineData(true, 3)]
    public async Task PendingDiscardAndOrderSurviveWireSaveAndRestart(bool socket, int count)
    {
        var factory = new OrderedInspectionReplayFactory(count);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        async Task<EngineResponse> Exchange(EngineRequest request) => socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        const string name = "inspection-review";
        var specification = new GameSpecification("rhino", ["spider_man"], [], Seed: 19);
        var current = await Exchange(EngineRequest.OpenGame("open", name, specification));
        Assert.Null(current.Error);
        string capability = current.Capability!;
        var action = Assert.Single(current.Prompt!.Affordances, offer => offer.AnchorId == factory.Source && offer.Label == "Synthetic inspection");
        current = await Exchange(EngineRequest.ResolveGame("start", name, capability, Decision.Take(action.Id), current.Revision));
        Assert.Null(current.Error);
        Assert.Empty(current.Prompt!.ContextCardIds);
        var look = Assert.Single(current.Prompt.Affordances);
        long rng = factory.Game.State.Random.Generator.WordsConsumed;
        current = await Exchange(EngineRequest.ResolveGame("look", name, capability, Decision.Take(look.Id), current.Revision));
        Assert.Null(current.Error);
        Assert.Equal(Math.Min(3, count), current.Prompt!.ContextCardIds.Count);
        int discarded = current.Prompt.ContextCardIds[0];
        int[] survivors = current.Prompt.ContextCardIds.Skip(1).Reverse().ToArray();
        var beforeRestart = factory.Game;
        VerifySave(store, factory, specification, beforeRestart);
        host = new EngineHost(factory, store: store);
        current = await Exchange(EngineRequest.SyncGame("discard-restart", name, capability));
        Assert.Null(current.Error);
        Equivalent(beforeRestart, factory.Game);
        var discard = Assert.Single(current.Prompt!.Affordances, offer => offer.AnchorId == discarded);
        current = await Exchange(EngineRequest.ResolveGame("discard", name, capability, Decision.Take(discard.Id), current.Revision));
        Assert.Null(current.Error);
        if (survivors.Length > 0)
        {
            Assert.Equal(Question.Order, current.Prompt!.Asking);
            Assert.Contains("first selected card will be the next encounter card", current.Prompt.Description);
            beforeRestart = factory.Game;
            VerifySave(store, factory, specification, beforeRestart);
            host = new EngineHost(factory, store: store);
            current = await Exchange(EngineRequest.SyncGame("order-restart", name, capability));
            Assert.Null(current.Error);
            Equivalent(beforeRestart, factory.Game);
            var composer = new DecisionComposer(current.Prompt!);
            composer.SelectAffordance(Assert.Single(current.Prompt!.Affordances).Id);
            if (survivors.Length > 1)
            {
                composer.SelectTargets([survivors[0]]);
                Assert.False(composer.TryBuild(out _, out _));
            }
            composer.SelectTargets(survivors);
            Assert.True(composer.TryBuild(out Decision? answer, out string? error), error);
            current = await Exchange(EngineRequest.ResolveGame("order", name, capability, answer!, current.Revision));
            Assert.Null(current.Error);
            Assert.Equal(survivors, factory.Game.State.AreaOf(DeckType.EncounterDeck).Cards.Reverse().Take(survivors.Length).Select(card => card.ObjectId));
            Assert.Equal(rng, factory.Game.State.Random.Generator.WordsConsumed);
        }
        VerifySave(store, factory, specification, factory.Game);
    }

    private static void VerifySave(MemorySessionStore store, OrderedInspectionReplayFactory factory,
        GameSpecification specification, Game expected)
    {
        var save = SessionSaveJson.Read(SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        var replayed = SessionReplay.Verify(save, factory.Compatibility, _ =>
        {
            var fresh = factory.Create(specification);
            return new ReplayOpenedGame(fresh.Game, fresh.SetupEvents);
        });
        Equivalent(expected, replayed);
    }

    private static void Equivalent(Game expected, Game actual)
    {
        Assert.Equal(expected.State.Digest().Canonical(), actual.State.Digest().Canonical());
        Assert.Equal(expected.State.Random.Generator.GetState().Words, actual.State.Random.Generator.GetState().Words);
        Assert.Equal(expected.State.Random.Generator.GetState().Index, actual.State.Random.Generator.GetState().Index);
        Assert.Equal(expected.State.Random.Generator.WordsConsumed, actual.State.Random.Generator.WordsConsumed);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(expected.Pending!)), JsonSerializer.Serialize(PromptRecord.From(actual.Pending!)));
    }
}
