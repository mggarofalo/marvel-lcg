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
        Assert.Equal(Steps.DamageWouldBeDealt, result.Prompt!.Trigger);
        Assert.Contains(result.Prompt.Affordances, item => item.AnchorId == factory.Backflip);
        Assert.Equal(accept ? 1 : 4, factory.Game.State.Attack!.CalculatedDamage);
        Assert.Equal(0, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.NotNull(factory.Game.State.Agenda.Current);
        // The containing attack still has dependent decisions. History edits must
        // reject without changing the persisted unit or replayed state.
        string after = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var undone = restarted.Exchange(EngineRequest.UndoGame("undo", "defense", opened.Capability!, 0, result.Revision));
        Assert.Equal("history_open", undone.Error?.Code);
        Assert.Equal(after, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal(control.State.Digest().Canonical(), factory.Game.State.Digest().Canonical());
        var recovered = new EngineHost(factory, store: store);
        var reopened = recovered.Exchange(
            EngineRequest.SyncGame("resync", "defense", opened.Capability!));
        Assert.Null(reopened.Error);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(result.Prompt!)),
            JsonSerializer.Serialize(PromptRecord.From(reopened.Prompt!)));
        Assert.Contains(reopened.Prompt!.Affordances, item => item.AnchorId == factory.Backflip);
        var damaged = control.Resolve(Decision.Decline);
        var finished = recovered.Exchange(EngineRequest.ResolveGame("backflip", "defense", opened.Capability!,
            Decision.Decline, reopened.Revision));
        Assert.Null(finished.Error);
        Assert.Null(factory.Game.State.Attack);
        Assert.Equal(accept ? 1 : 4, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.HandsArea, factory.Game.State.Cards[factory.Backflip].Area.Type);
        var completed = Assert.Single(store.Load()).Save.Units.SelectMany(unit => unit.Decisions).ToArray();
        Assert.Equal(2, completed.Length);
        Assert.Equal(damaged.Events.Select(JournalJson.Event).Select(value => value.GetRawText()),
            completed[^1].Events.Select(value => value.GetRawText()));
        Assert.Equal(control.State.Digest().Canonical(), factory.Game.State.Digest().Canonical());
        string finalSave = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var finalSync = new EngineHost(factory, store: store).Exchange(
            EngineRequest.SyncGame("final", "defense", opened.Capability!));
        Assert.Null(finalSync.Error);
        Assert.Equal(finalSave, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal(control.State.Digest().Canonical(), factory.Game.State.Digest().Canonical());
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ZeroResidualDamageCompletesWithoutOfferingBackflip(bool accept)
    {
        var factory = new ExpertDefenseReplayFactory(DatasetGameFactory.Load(RepositoryPaths.Root), charged: false);
        var game = factory.Create(new GameSpecification("rhino", ["spider_man", "captain_marvel"], [],
            Seed: 12345)).Game;
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == factory.Expert);
        var result = game.Resolve(accept ? Decision.Take(offer.Id) : Decision.Decline);
        Assert.Equal("WhenPlayerInTurn", result.Prompt!.Trigger);
        Assert.Null(game.State.Attack);
        Assert.Null(game.State.Agenda.Current);
        Assert.Equal(0, game.State.FinishedAttack!.CalculatedDamage);
        Assert.False(game.State.FinishedAttack.Damaged);
        Assert.Equal(0, game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.HandsArea, game.State.Cards[factory.Backflip].Area.Type);
        Assert.Equal(accept ? DeckType.DiscardPile : DeckType.HandsArea,
            game.State.Cards[factory.Expert].Area.Type);
    }
}
