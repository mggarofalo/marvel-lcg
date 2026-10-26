using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PreparationReplayReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SuspendedDefeatAndCompletionReceiptReconstructAcrossJsonAndHostRestart(bool decline)
    {
        // Synthetic Action entry uses the supported IDurableGameFactory seam;
        // it proves persistence without public admission of incomplete content.
        var factory = new PreparationReplayReviewFactory();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var specification = new GameSpecification("rhino", ["spider_man"], [], Seed: 7301);
        const string label = "preparation-replay-review";
        var opened = host.Exchange(EngineRequest.OpenGame("open", label, specification));
        Assert.Null(opened.Error);
        var bite = Assert.Single(opened.Prompt!.Affordances, a => a.AnchorId == factory.Preparation);
        var pending = host.Exchange(EngineRequest.ResolveGame("bite", label, opened.Capability!,
            Decision.Take(bite.Id), opened.Revision));
        Assert.Null(pending.Error);
        var observer = Assert.Single(pending.Prompt!.Affordances, a => a.AnchorId == factory.Observer);
        pending = host.Exchange(EngineRequest.ResolveGame("interrupt", label, opened.Capability!,
            Decision.Take(observer.Id), pending.Revision));
        Assert.Null(pending.Error);
        Assert.Equal(Question.Option, factory.Game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[factory.Preparation].Area.Type);
        Assert.DoesNotContain(factory.Game.State.Agenda.Outstanding,
            step => step.Operation is PreparationAbilityResolvedOperation);

        var control = factory.Game;
        var replayed = Replay(factory, store, specification);
        Equivalent(control, replayed);
        string serialized = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var restarted = new EngineHost(factory, store: store);
        var restored = restarted.Exchange(EngineRequest.SyncGame("restart", label, opened.Capability!));
        Assert.Null(restored.Error);
        Assert.NotSame(control.State, factory.Game.State);
        Equivalent(control, factory.Game);
        Assert.Equal(serialized, SessionSaveJson.Write(Assert.Single(store.Load()).Save));

        var choice = Decision.Take(control.Pending!.Affordances[0].Id);
        var expected = control.Resolve(choice);
        Assert.Equal(expected.Events.Select(EventJson.Write), replayed.Resolve(choice).Events.Select(EventJson.Write));
        var completed = restarted.Exchange(EngineRequest.ResolveGame("settle", label,
            opened.Capability!, choice, restored.Revision));
        Assert.Null(completed.Error);
        Assert.NotEmpty(completed.Events!);
        Equivalent(control, factory.Game);
        Assert.Equal(DeckType.EncounterDiscardPile, factory.Game.State.Cards[factory.Minion].Area.Type);
        Assert.False(Statuses.Has(factory.Game.State, factory.Game.State.Cards[factory.Minion], Statuses.Stunned));
        Assert.Equal(2, control.Pending!.Affordances.Count);
        Assert.IsType<ResolvedPreparationAbility>(control.State.Agenda.Occurrence!.ActorFacts!.PreparationAbility);

        // Restart at the receipt itself as well as at the preceding damage choice.
        var receiptControl = factory.Game;
        replayed = Replay(factory, store, specification);
        Equivalent(receiptControl, replayed);
        restarted = new EngineHost(factory, store: store);
        restored = restarted.Exchange(EngineRequest.SyncGame("receipt-restart", label, opened.Capability!));
        Assert.Null(restored.Error);
        Equivalent(receiptControl, factory.Game);
        for (int i = 0; i < 4 && !receiptControl.IsRootPrompt; i++)
        {
            var prompt = receiptControl.Pending!;
            Decision answer = decline ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id);
            expected = receiptControl.Resolve(answer);
            var replayResult = replayed.Resolve(answer);
            completed = restarted.Exchange(EngineRequest.ResolveGame("response-" + i, label,
                opened.Capability!, answer, restored.Revision));
            Assert.Null(completed.Error);
            Assert.Equal(expected.Events.Select(EventJson.Write), replayResult.Events.Select(EventJson.Write));
            Equivalent(receiptControl, replayed);
            Equivalent(receiptControl, factory.Game);
            restored = completed;
        }
        Assert.True(receiptControl.IsRootPrompt);
        Assert.Equal(decline ? 0 : 1, factory.Game.State.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(decline, factory.Game.State.Cards[factory.Suit].Ready);
        Equivalent(receiptControl, Replay(factory, store, specification));
    }

    private static Game Replay(PreparationReplayReviewFactory factory, MemorySessionStore store,
        GameSpecification specification) => SessionReplay.Verify(
            SessionSaveJson.Read(SessionSaveJson.Write(Assert.Single(store.Load()).Save)), factory.Compatibility, _ =>
            {
                var fresh = factory.Create(specification);
                return new ReplayOpenedGame(fresh.Game, fresh.SetupEvents);
            });

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
