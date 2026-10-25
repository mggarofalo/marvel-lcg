using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ExpertDefenseReplayTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CommittedDefenseRestartsAndResolvesExactlyOnce(bool accept)
    {
        var factory = new ExpertDefenseReplayFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "defense",
            new GameSpecification("rhino", ["spider_man", "captain_marvel"], [], Seed: 12345)));
        Assert.Null(opened.Error);
        Assert.Equal(HeroDefenseTiming.Defends, factory.Game.Pending!.Trigger);
        var control = factory.Game;
        string saved = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var restarted = new EngineHost(factory, store: store);
        var restored = restarted.Exchange(EngineRequest.SyncGame("sync", "defense", opened.Capability!));
        Assert.Null(restored.Error);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(opened.Prompt!)),
            JsonSerializer.Serialize(PromptRecord.From(restored.Prompt!)));
        Assert.Equal(saved, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal(control.State.Digest().Canonical(), factory.Game.State.Digest().Canonical());
        var offer = Assert.Single(restored.Prompt!.Affordances, item => item.AnchorId == factory.Expert);
        var answer = accept ? Decision.Take(offer.Id) : Decision.Decline;
        var expected = control.Resolve(answer);
        var result = restarted.Exchange(EngineRequest.ResolveGame("answer", "defense", opened.Capability!,
            answer, restored.Revision));
        Assert.Null(result.Error);
        var recorded = Assert.Single(store.Load()).Save.Units.SelectMany(unit => unit.Decisions).Last();
        Assert.Equal(expected.Events.Select(JournalJson.Event).Select(value => value.GetRawText()),
            recorded.Events.Select(value => value.GetRawText()));
        Assert.Equal(control.State.Random.Generator.WordsConsumed, recorded.RngWords);
        Assert.Equal(control.State.Digest().Canonical(), factory.Game.State.Digest().Canonical());
        Assert.Equal(accept ? DeckType.DiscardPile : DeckType.HandsArea, factory.Game.State.Cards[factory.Expert].Area.Type);
        // The containing attack still has dependent decisions. History edits must
        // reject without changing the persisted unit or replayed state.
        string after = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var undone = restarted.Exchange(EngineRequest.UndoGame("undo", "defense", opened.Capability!, 0, result.Revision));
        Assert.Equal("history_open", undone.Error?.Code);
        Assert.Equal(after, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal(control.State.Digest().Canonical(), factory.Game.State.Digest().Canonical());
        var reopened = new EngineHost(factory, store: store).Exchange(
            EngineRequest.SyncGame("resync", "defense", opened.Capability!));
        Assert.Null(reopened.Error);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(result.Prompt!)),
            JsonSerializer.Serialize(PromptRecord.From(reopened.Prompt!)));
    }
}
