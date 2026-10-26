using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class VariableEventReplayReviewTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void SuspendedCombinedPaymentRestartsAndRetainsChosenX(int x)
    {
        // Synthetic abilities isolate payment. The supported IDurableGameFactory seam supplies
        // the scene; EngineHost, SessionSaveJson and SessionReplay execute their production paths.
        var factory = new VariableEventReplayReviewFactory();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var specification = new GameSpecification("rhino", ["spider_man"], [], Seed: 12345);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "variable-review", specification));
        Assert.Null(opened.Error);
        var action = Assert.Single(opened.Prompt!.Affordances, offer => offer.AnchorId == factory.Source);
        var decision = Decision.Take(action.Id, [], [factory.Generator], new Dictionary<string, long> { ["X"] = x },
            [new(factory.Generator, 0, "Y"), new(factory.Generator, 1, new string('Y', x))]);
        var suspended = host.Exchange(EngineRequest.ResolveGame("pay", "variable-review", opened.Capability!, decision, opened.Revision));
        Assert.Null(suspended.Error);
        Assert.Equal(Question.Opportunity, suspended.Prompt!.Asking);
        var uninterrupted = factory.Game;
        Assert.Equal(DeckType.RevealingArea, uninterrupted.State.Cards[factory.Source].Area.Type);
        Assert.Equal(0, uninterrupted.State.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));
        var save = Assert.Single(store.Load()).Save;
        Assert.Equal("open", save.Units[^1].Status);
        string serialized = SessionSaveJson.Write(save);
        var serializedReplay = SessionReplay.Verify(SessionSaveJson.Read(serialized), factory.Compatibility, _ =>
        {
            var fresh = factory.Create(specification);
            return new ReplayOpenedGame(fresh.Game, fresh.SetupEvents);
        });
        Equivalent(uninterrupted, serializedReplay);
        var restarted = new EngineHost(factory, store: store);
        var restored = restarted.Exchange(EngineRequest.SyncGame("restart", "variable-review", opened.Capability!));
        Assert.Null(restored.Error);
        Assert.Equal(serialized, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Equivalent(uninterrupted, factory.Game);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(suspended.Prompt)),
            JsonSerializer.Serialize(PromptRecord.From(restored.Prompt!)));
        var expected = uninterrupted.Resolve(Decision.Decline);
        var completed = restarted.Exchange(EngineRequest.ResolveGame("finish", "variable-review", opened.Capability!, Decision.Decline, restored.Revision));
        Assert.Null(completed.Error);
        Assert.Equal(expected.Events.Select(EventJson.Write), completed.Events!.Select(EventJson.Write));
        Equivalent(uninterrupted, factory.Game);
        Assert.Equal(x, factory.Game.State.Seats[0].IdentityCard.Tokens["c_test"]);
        Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[factory.Source].Area.Type);
        Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[factory.Generator].Area.Type);
        Assert.False(factory.Game.State.Agenda.IsBusy);
        var completedSave = SessionSaveJson.Read(SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        var replayed = SessionReplay.Verify(completedSave, factory.Compatibility, _ =>
        {
            var fresh = factory.Create(specification);
            return new ReplayOpenedGame(fresh.Game, fresh.SetupEvents);
        });
        Equivalent(uninterrupted, replayed);
        Assert.Equal(x, replayed.State.Seats[0].IdentityCard.Tokens["c_test"]);
    }

    private static void Equivalent(Game expected, Game actual)
    {
        Assert.Equal(expected.State.Digest().Canonical(), actual.State.Digest().Canonical());
        Assert.Equal(expected.State.Random.Generator.GetState().Index, actual.State.Random.Generator.GetState().Index);
        Assert.Equal(expected.State.Random.Generator.GetState().Words, actual.State.Random.Generator.GetState().Words);
        Assert.Equal(expected.State.Random.Generator.WordsConsumed, actual.State.Random.Generator.WordsConsumed);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(expected.Pending!)),
            JsonSerializer.Serialize(PromptRecord.From(actual.Pending!)));
        Assert.Equal(expected.Phase, actual.Phase);
    }
}
