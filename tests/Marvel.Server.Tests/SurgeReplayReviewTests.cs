using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class SurgeReplayReviewTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:surge.1")]
    [Rule("rr:surge.2")]
    public void FullEncounterQueueReconstructsPendingSurgeAndPreparationReceipt(bool gained, bool declineReceipt)
    {
        // "Deal yourself 1 facedown encounter card." "Complete the process of resolving the original card ...
        // before revealing the additional card." The injected book is test-local, not pack admission.
        var factory = new SurgeReplayReviewFactory(gained);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var specification = new GameSpecification("rhino", ["spider_man"], [], Seed: 8107);
        const string label = "surge-replay-review";
        var opened = host.Exchange(EngineRequest.OpenGame("open", label, specification));
        Assert.Null(opened.Error);
        Assert.Equal(SurgeKeywordOperation.Condition, factory.Game.Pending!.Trigger);
        Assert.True(factory.Game.Pending.Cancellable);
        Assert.Empty(factory.Game.State.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.False(factory.Game.State.Cards[factory.Extra].FaceUp);
        Assert.False(Statuses.Has(factory.Game.State, factory.Game.State.Seats[0].IdentityCard, Statuses.Confused));

        var control = factory.Game;
        var replayed = Replay(factory, store, specification);
        Equivalent(control, replayed);
        string original = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        host = new EngineHost(factory, store: store);
        var restored = host.Exchange(EngineRequest.SyncGame("surge-restart", label, opened.Capability!));
        Assert.Null(restored.Error);
        Assert.NotSame(control.State, factory.Game.State);
        Equivalent(control, factory.Game);
        Assert.Equal(original, SessionSaveJson.Write(Assert.Single(store.Load()).Save));

        var interrupt = Decision.Take(Assert.Single(control.Pending!.Affordances,
            a => a.AnchorId == factory.Preparation).Id);
        var expected = control.Resolve(interrupt);
        Assert.Equal(expected.Events.Select(EventJson.Write), replayed.Resolve(interrupt).Events.Select(EventJson.Write));
        var receipt = host.Exchange(EngineRequest.ResolveGame("espionage", label, opened.Capability!, interrupt, restored.Revision));
        Assert.Null(receipt.Error);
        Equivalent(control, factory.Game);
        Assert.Equal("WhenPreparationAbilityResolved", control.Pending!.Trigger);
        Assert.IsType<ResolvedPreparationAbility>(control.State.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
        Assert.Equal(DeckType.DiscardPile, control.State.Cards[factory.Preparation].Area.Type);
        Assert.Empty(control.State.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.False(control.State.Cards[factory.Extra].FaceUp);
        Assert.False(Statuses.Has(control.State, control.State.Seats[0].IdentityCard, Statuses.Confused));

        replayed = Replay(factory, store, specification);
        Equivalent(control, replayed);
        original = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        host = new EngineHost(factory, store: store);
        restored = host.Exchange(EngineRequest.SyncGame("receipt-restart", label, opened.Capability!));
        Assert.Null(restored.Error);
        Equivalent(control, factory.Game);
        Assert.Equal(original, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Decision answer = declineReceipt ? Decision.Decline : Decision.Take(Assert.Single(control.Pending.Affordances).Id);
        expected = control.Resolve(answer);
        Assert.Equal(expected.Events.Select(EventJson.Write), replayed.Resolve(answer).Events.Select(EventJson.Write));
        var completed = host.Exchange(EngineRequest.ResolveGame("complete", label, opened.Capability!, answer, restored.Revision));
        Assert.Null(completed.Error);
        Equivalent(control, factory.Game);
        Equivalent(control, replayed);
        Assert.True(control.IsRootPrompt);
        Assert.Empty(control.State.Agenda.Outstanding);
        Assert.Empty(control.State.AreaOf(DeckType.RevealingArea).Cards);
        Assert.Empty(control.State.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(DeckType.EngagedEnemiesArea, control.State.Cards[factory.Extra].Area.Type);
        Assert.Equal(gained ? DeckType.EncounterDiscardPile : DeckType.EngagedEnemiesArea,
            control.State.Cards[factory.Encounter].Area.Type);
        Assert.Equal(declineReceipt, control.State.Cards[factory.Suit].Ready);
        Assert.Equal(!declineReceipt, control.State.Seats[0].IdentityCard.Ready);
        Assert.Equal(gained, Statuses.Has(control.State, control.State.Seats[0].IdentityCard, Statuses.Confused));
        Equivalent(control, Replay(factory, store, specification));
    }

    private static Game Replay(SurgeReplayReviewFactory factory, MemorySessionStore store,
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
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(expected.Pending!)), JsonSerializer.Serialize(PromptRecord.From(actual.Pending!)));
        Assert.Equal(expected.State.Agenda.Outstanding.Select(s => (s.What, s.Round, s.Number, s.Index, s.Subject, s.Seat)),
            actual.State.Agenda.Outstanding.Select(s => (s.What, s.Round, s.Number, s.Index, s.Subject, s.Seat)));
        Assert.Equal(expected.State.Agenda.Stage, actual.State.Agenda.Stage);
        Assert.Equal(expected.Phase, actual.Phase);
        Assert.Equal(expected.Round, actual.Round);
    }
}
