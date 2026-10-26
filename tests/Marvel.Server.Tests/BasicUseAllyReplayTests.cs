using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class BasicUseAllyReplayTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void PendingSelectionAndPaidEffectRestartWithTheSameMatchingPower(int player, bool thwart)
    {
        // Synthetic abilities isolate the receipt without admitting an expansion.
        // The production host, JSON save and replay paths reconstruct pending work.
        var factory = new BasicUseAllyReplayFactory(player);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var specification = new GameSpecification("rhino", ["spider_man", "captain_marvel"], [], Seed: 12345);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "basic-use", specification));
        Assert.Null(opened.Error);
        var action = Assert.Single(opened.Prompt!.Affordances, offer =>
            offer.AnchorId == factory.Game.State.Seats[player].IdentityCard.ObjectId
                && offer.Verb == (thwart ? BasicPowers.ThwartVerb : BasicPowers.AttackVerb));
        var pending = host.Exchange(EngineRequest.ResolveGame("use", "basic-use", opened.Capability!,
            new Decision(action.Id, [action.Targets!.Legal[0]]), opened.Revision));
        Assert.Null(pending.Error);
        Assert.Equal(player, pending.Prompt!.Player);
        Assert.True(factory.Game.State.Cards[factory.Ally].Ready);
        VerifySerialized(store, factory, specification);
        var restarted = new EngineHost(factory, store: store);
        var restored = restarted.Exchange(EngineRequest.SyncGame("cost-restart", "basic-use", opened.Capability!));
        Assert.Null(restored.Error);
        var pay = Assert.Single(restored.Prompt!.Affordances);
        var suspended = restarted.Exchange(EngineRequest.ResolveGame("pay", "basic-use", opened.Capability!,
            new Decision(pay.Id, [factory.Ally]), restored.Revision));
        Assert.Null(suspended.Error);
        var uninterrupted = factory.Game;
        Assert.False(uninterrupted.State.Cards[factory.Ally].Ready);
        Assert.Equal(DeckType.RevealingArea, uninterrupted.State.Cards[factory.Source].Area.Type);
        VerifySerialized(store, factory, specification);
        var effectRestart = new EngineHost(factory, store: store);
        var effect = effectRestart.Exchange(EngineRequest.SyncGame("effect-restart", "basic-use", opened.Capability!));
        Assert.Null(effect.Error);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(suspended.Prompt!)),
            JsonSerializer.Serialize(PromptRecord.From(effect.Prompt!)));
        var choice = Decision.Take(effect.Prompt!.Affordances[0].Id);
        var expected = uninterrupted.Resolve(choice);
        var completed = effectRestart.Exchange(EngineRequest.ResolveGame("finish", "basic-use", opened.Capability!, choice, effect.Revision));
        Assert.Null(completed.Error);
        Assert.Equal(expected.Events.Select(EventJson.Write), completed.Events!.Select(EventJson.Write));
        Equivalent(uninterrupted, factory.Game);
        var world = factory.Game.State;
        long heroPower = StateFields.Modified(world, world.Seats[player].IdentityCard,
            thwart ? "thwart" : "attack", world.Facts, world.Players);
        if (thwart) Assert.Equal(10 - heroPower - 3, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        else Assert.Equal(heroPower + 4, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, world.Cards[factory.Ally].Damage);
        Assert.Equal(DeckType.DiscardPile, world.Cards[factory.Source].Area.Type);
        Assert.False(world.Agenda.IsBusy);
        VerifySerialized(store, factory, specification);
    }

    private static void VerifySerialized(MemorySessionStore store, BasicUseAllyReplayFactory factory, GameSpecification specification)
    {
        var expected = factory.Game;
        string json = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var replayed = SessionReplay.Verify(SessionSaveJson.Read(json), factory.Compatibility, _ =>
        {
            var fresh = factory.Create(specification);
            return new ReplayOpenedGame(fresh.Game, fresh.SetupEvents);
        });
        Equivalent(expected, replayed);
        Assert.Equal(json, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
    }

    private static void Equivalent(Game expected, Game actual)
    {
        Assert.Equal(expected.State.Digest().Canonical(), actual.State.Digest().Canonical());
        Assert.Equal(expected.State.Random.Generator.GetState().Index, actual.State.Random.Generator.GetState().Index);
        Assert.Equal(expected.State.Random.Generator.GetState().Words, actual.State.Random.Generator.GetState().Words);
        Assert.Equal(expected.State.Random.Generator.WordsConsumed, actual.State.Random.Generator.WordsConsumed);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(expected.Pending!)),
            JsonSerializer.Serialize(PromptRecord.From(actual.Pending!)));
    }
}

