using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ManOutOfTimeReplayTests
{
    [Fact]
    public void ChosenHandDiscardRestartsPrivatelyAndResolvesOnce()
    {
        var factory = new ManOutOfTimeReplayFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "cost",
            new GameSpecification("rhino", ["spider_man", "captain_marvel"], [], Seed: 12345)));
        Assert.Null(opened.Error);
        var options = host.Exchange(EngineRequest.ResolveGame("remain", "cost", opened.Capability!,
            Decision.Take(1), opened.Revision));
        Assert.Null(options.Error);
        var pending = host.Exchange(EngineRequest.ResolveGame("discard-half", "cost", opened.Capability!,
            Decision.Take(1), options.Revision));
        Assert.Null(pending.Error);
        var uninterrupted = factory.Game;
        var hidden = WorldProjection.For(uninterrupted.State, uninterrupted.Pending, [],
            new RestrictedVisibilityPolicy(1).Authorize(null, 2));
        Assert.Null(hidden.Prompt);
        Assert.All(hidden.World.Areas.Single(area => area.Id == uninterrupted.State.Seats[0].Hand.Id).Cards,
            card => { Assert.Null(card.Id); Assert.Null(card.Face); });
        string saved = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var restarted = new EngineHost(factory, store: store);
        var restored = restarted.Exchange(EngineRequest.SyncGame("sync", "cost", opened.Capability!));
        Assert.Null(restored.Error);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(pending.Prompt!)),
            JsonSerializer.Serialize(PromptRecord.From(restored.Prompt!)));
        Assert.Equal(saved, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal(uninterrupted.State.Digest().Canonical(), factory.Game.State.Digest().Canonical());
        var choice = Assert.Single(restored.Prompt!.Affordances);
        int[] payment = choice.Targets!.Legal.TakeLast(2).ToArray();
        var answer = new Decision(choice.Id, payment);
        var expectedEvents = uninterrupted.Resolve(answer).Events;
        var result = restarted.Exchange(EngineRequest.ResolveGame("pay", "cost", opened.Capability!, answer, restored.Revision));
        Assert.Null(result.Error);
        var recorded = Assert.Single(store.Load()).Save.Units.SelectMany(unit => unit.Decisions).Last();
        Assert.Equal(expectedEvents.Select(JournalJson.Event).Select(value => value.GetRawText()),
            recorded.Events.Select(value => value.GetRawText()));
        Assert.Equal(uninterrupted.State.Random.Generator.WordsConsumed, recorded.RngWords);
        Assert.Equal(uninterrupted.State.Digest().Canonical(), factory.Game.State.Digest().Canonical());
        Assert.All(payment, id => Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[id].Area.Type));
        Assert.Equal(DeckType.EncounterDiscardPile, factory.Game.State.Cards[factory.Obligation].Area.Type);
        Assert.Equal(3, factory.Game.State.Seats[0].Hand.Cards.Count);
    }
}
