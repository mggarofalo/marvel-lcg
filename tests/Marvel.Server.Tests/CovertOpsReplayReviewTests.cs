using System.Text.Json;
using Marvel.Decisions;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class CovertOpsReplayReviewTests
{
    [Fact]
    public void TargetlessThwartPreservesItsCauseAndResponseAcrossJsonAndFreshHost()
    {
        var factory = new CovertOpsReplayReviewFactory();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var specification = new GameSpecification("rhino", ["spider_man"], [], Seed: 7301);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "covert-review", specification));
        Assert.Null(opened.Error);
        var play = Assert.Single(opened.Prompt!.Affordances, offer => offer.AnchorId == factory.Source);
        var composer = new DecisionComposer(opened.Prompt);
        composer.SelectAffordance(play.Id);
        foreach (int payment in factory.Payment) composer.ToggleResource(payment);
        Assert.True(composer.TryBuild(out var playAnswer, out var error), error);
        var pending = host.Exchange(EngineRequest.ResolveGame("play", "covert-review", opened.Capability!,
            playAnswer!, opened.Revision));
        Assert.Null(pending.Error);
        var uninterrupted = factory.Game;
        Assert.False(uninterrupted.IsRootPrompt);
        Assert.Equal(-1, uninterrupted.State.Agenda.Occurrence!.Target);
        Assert.Null(uninterrupted.State.Agenda.Occurrence.TargetFacts);
        Assert.True(Statuses.Has(uninterrupted.State, uninterrupted.State.TheCardIn(DeckType.VillainArea)!, Statuses.Confused));
        var causalEvents = pending.Events!.Where(happened => happened.Cause?.Kind == EventCauseKind.Thwart).ToArray();
        Assert.NotEmpty(causalEvents);
        foreach (var happened in causalEvents)
        {
            Assert.Equal(-1, happened.Cause!.Target);
            Assert.Equal(string.Empty, happened.Cause.TargetTitle);
            var safe = EventJson.Read(EventJson.Write(happened));
            var world = WorldProjection.For(uninterrupted.State, uninterrupted.Pending, [],
                new RestrictedVisibilityPolicy(0).Authorize(null, 1)).World;
            var presentation = EventPresenter.Present(safe, world);
            Assert.Equal("Spider-Man thwarts", presentation.Intention);
            Assert.DoesNotContain(-1, presentation.Anchors);
            Assert.DoesNotContain(presentation.Activity!.Subjects, subject => subject.Card == -1);
        }
        string saved = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var replayed = SessionReplay.Verify(SessionSaveJson.Read(saved), factory.Compatibility, _ =>
        {
            var fresh = factory.Create(specification);
            return new ReplayOpenedGame(fresh.Game, fresh.SetupEvents);
        });
        Equivalent(uninterrupted, replayed);
        var restarted = new EngineHost(factory, store: store);
        var restored = restarted.Exchange(EngineRequest.SyncGame("sync", "covert-review", opened.Capability!));
        Assert.Null(restored.Error);
        Assert.NotSame(uninterrupted.State, factory.Game.State);
        Equivalent(uninterrupted, factory.Game);
        Assert.Equal(saved, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        var response = Assert.Single(pending.Prompt!.Affordances);
        var answer = Decision.Take(response.Id);
        int handBefore = uninterrupted.State.Seats[0].Hand.Cards.Count;
        var expected = uninterrupted.Resolve(answer);
        var replayResult = replayed.Resolve(answer);
        var completed = restarted.Exchange(EngineRequest.ResolveGame("response", "covert-review", opened.Capability!, answer, restored.Revision));
        Assert.Null(completed.Error);
        Assert.Equal(expected.Events.Select(EventJson.Write), replayResult.Events.Select(EventJson.Write));
        Assert.Equal(expected.Events.Select(EventJson.Write), completed.Events!.Select(EventJson.Write));
        Equivalent(uninterrupted, replayed);
        Equivalent(uninterrupted, factory.Game);
        Assert.Equal(handBefore + 1, factory.Game.State.Seats[0].Hand.Cards.Count);
        Assert.True(factory.Game.IsRootPrompt);
        Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[factory.Source].Area.Type);
    }

    private static void Equivalent(Game expected, Game actual)
    {
        Assert.Equal(expected.State.Digest().Canonical(), actual.State.Digest().Canonical());
        Assert.Equal(expected.State.Random.Generator.GetState().Index, actual.State.Random.Generator.GetState().Index);
        Assert.Equal(expected.State.Random.Generator.GetState().Words, actual.State.Random.Generator.GetState().Words);
        Assert.Equal(expected.State.Random.Generator.WordsConsumed, actual.State.Random.Generator.WordsConsumed);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(expected.Pending!)), JsonSerializer.Serialize(PromptRecord.From(actual.Pending!)));
    }
}
