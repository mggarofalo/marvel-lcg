using System.Text.Json;
using Marvel.Decisions;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class AllyPaymentReplayReviewTests
{
    [Theory]
    [InlineData("YY", "01003", 3)]
    [InlineData("RR", "01088", 2)]
    public void PendingEntryResponseReconstructsDeclaredOriginalPriceAcrossJsonAndHostRestart(
        string declaration, string responseFace, int expectedCounters)
    {
        // Synthetic abilities prove the engine persistence contract, without admitting a pack.
        var factory = new AllyPaymentReplayReviewFactory(responseFace);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var specification = new GameSpecification("rhino", ["spider_man"], [], Seed: 7301);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "ally-price-review", specification));
        Assert.Null(opened.Error);
        var play = Assert.Single(opened.Prompt!.Affordances, offer => offer.AnchorId == factory.Ally);
        var pending = host.Exchange(EngineRequest.ResolveGame("play", "ally-price-review", opened.Capability!,
            Decision.Take(play.Id, [factory.Game.State.Seats[0].IdentityCard.ObjectId],
                [factory.Wild, factory.Physical], new Dictionary<string, long>(),
                allocations: [new(factory.Wild, 0, declaration), new(factory.Physical, 0, "R")]), opened.Revision));
        Assert.Null(pending.Error);
        var response = Assert.Single(pending.Prompt!.Affordances, offer => offer.AnchorId == factory.Ally);
        var uninterrupted = factory.Game;
        Assert.Equal(DeckType.AlliesArea, uninterrupted.State.Cards[factory.Ally].Area.Type);
        Assert.False(uninterrupted.IsRootPrompt);
        Assert.Equal(0, uninterrupted.State.Cards[factory.Ally].Tokens.GetValueOrDefault("c_test"));
        var save = Assert.Single(store.Load()).Save;
        Assert.Equal("open", save.Units[^1].Status);
        string serialized = SessionSaveJson.Write(save);
        Assert.DoesNotContain("CardPlayPayment", serialized);
        Assert.DoesNotContain("EntryPayment", serialized);
        var replayed = SessionReplay.Verify(SessionSaveJson.Read(serialized), factory.Compatibility, _ =>
        {
            var fresh = factory.Create(specification);
            return new ReplayOpenedGame(fresh.Game, fresh.SetupEvents);
        });
        Equivalent(uninterrupted, replayed);
        EntryPaymentEqual(uninterrupted, replayed, factory.Ally);
        var restarted = new EngineHost(factory, store: store);
        var restored = restarted.Exchange(EngineRequest.SyncGame("restart", "ally-price-review", opened.Capability!));
        Assert.Null(restored.Error);
        var restartedGame = factory.Game;
        Assert.NotSame(uninterrupted.State, restartedGame.State);
        Equivalent(uninterrupted, restartedGame);
        EntryPaymentEqual(uninterrupted, restartedGame, factory.Ally);
        Assert.Equal(serialized, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        var draft = new DecisionComposer(pending.Prompt);
        draft.SelectAffordance(response.Id);
        draft.ToggleResource(factory.ResponsePayment);
        Assert.True(draft.TryBuild(out Decision? answer, out string? error), error);
        var expected = uninterrupted.Resolve(answer!);
        var replayResult = replayed.Resolve(answer!);
        var completed = restarted.Exchange(EngineRequest.ResolveGame("respond", "ally-price-review",
            opened.Capability!, answer!, restored.Revision));
        Assert.Null(completed.Error);
        restartedGame = factory.Game;
        Assert.Equal(expected.Events.Select(EventJson.Write), replayResult.Events.Select(EventJson.Write));
        Assert.Equal(expected.Events.Select(EventJson.Write), completed.Events!.Select(EventJson.Write));
        Equivalent(uninterrupted, replayed);
        Equivalent(uninterrupted, restartedGame);
        Assert.Equal(expectedCounters, restartedGame.State.Cards[factory.Ally].Tokens["c_test"]);
        Assert.Equal(DeckType.DiscardPile, restartedGame.State.Cards[factory.ResponsePayment].Area.Type);
        Assert.True(restartedGame.IsRootPrompt);
        var completedSave = SessionSaveJson.Read(SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        var finalReplay = SessionReplay.Verify(completedSave, factory.Compatibility, _ =>
        {
            var fresh = factory.Create(specification);
            return new ReplayOpenedGame(fresh.Game, fresh.SetupEvents);
        });
        Equivalent(uninterrupted, finalReplay);
        Assert.Equal(expectedCounters, finalReplay.State.Cards[factory.Ally].Tokens["c_test"]);
    }

    private static void EntryPaymentEqual(Game expected, Game actual, int ally)
    {
        var original = Assert.IsType<EntryPayment>(expected.State.Agenda.Occurrence!.EntryPayment);
        var restored = Assert.IsType<EntryPayment>(actual.State.Agenda.Occurrence!.EntryPayment);
        Assert.Equal(original.Generated, restored.Generated);
        Assert.Equal(original.Paid, restored.Paid);
        Assert.Equal(original.Excess, restored.Excess);
        Assert.True(original.MatchesEntered(expected.State.Cards[ally], 0));
        Assert.True(restored.MatchesEntered(actual.State.Cards[ally], 0));
        Assert.False(restored.MatchesEntered(expected.State.Cards[ally], 0));
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
        Assert.Equal(expected.Round, actual.Round);
    }
}
