using Marvel.Decisions;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Tests;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Session;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;
public sealed class EngineHostRestrictedSeatsReceiveOnlyTheirTurnOptionsTests : EngineHostTestBase
{
    [Fact]
    public void RestrictedSeatsReceiveOnlyTheirTurnOptionsAndMaySubmitTheirOwnOffTurnAction()
    {
        var store = new MemorySessionStore();
        var factory = new OffTurnActionFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var host = new EngineHost(factory, new SequenceCapabilities("seat-zero", "invite-one", "seat-one"), new RestrictedVisibilityPolicy(0), store);
        var specification = new GameSpecification("rhino", ["captain_marvel", "spider_man"], [], Seed: 7);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "actions", specification));
        EngineResponse attached = host.Exchange(EngineRequest.AttachGame("attach", "actions", Assert.Single(opened.Invitations!).Invitation));
        EngineResponse afterZero = host.Exchange(EngineRequest.ResolveGame("zero-mulligan", "actions", RequiredCapability(opened), TakeOnly(opened)));
        EngineResponse oneMulligan = host.Exchange(EngineRequest.SyncGame("one-mulligan-menu", "actions", RequiredCapability(attached)));
        EngineResponse afterOne = host.Exchange(EngineRequest.ResolveGame("one-mulligan", "actions", RequiredCapability(attached), TakeOnly(oneMulligan), oneMulligan.Revision));
        int frontierBeforeAction = Assert.Single(store.Load()).Save.EditFrontier;
        Assert.Null(afterZero.Prompt);
        Assert.Equal(1, afterOne.Prompt?.Player);
        EngineResponse active = host.Exchange(EngineRequest.SyncGame("active", "actions", RequiredCapability(opened)));
        EngineResponse other = host.Exchange(EngineRequest.SyncGame("other", "actions", RequiredCapability(attached)));
        Prompt activeMenu = Assert.IsType<Prompt>(active.Prompt);
        Prompt otherMenu = Assert.IsType<Prompt>(other.Prompt);
        Assert.Equal(0, activeMenu.Player);
        Assert.DoesNotContain(activeMenu.Affordances, option => option.AnchorPlayer == 1);
        Assert.Equal(1, otherMenu.Player);
        Assert.False(otherMenu.Cancellable);
        Assert.All(otherMenu.Affordances, option =>
        {
            Assert.Equal(Game.ActionVerb, option.Verb);
            Assert.Equal(1, option.AnchorPlayer);
        });
        Affordance auntMay = Assert.Single(otherMenu.Affordances, option => option.AnchorId == factory.AuntMay.ObjectId);
        Affordance activeOnly = Assert.Single(activeMenu.Affordances, option => option.Verb == Game.ChangeForm);
        EngineResponse forgedTurnOption = host.Exchange(EngineRequest.ResolveGame("forged-basic", "actions", RequiredCapability(attached), new EngineDecision(activeOnly.Id, []), active.Revision));
        EngineResponse stolenAction = host.Exchange(EngineRequest.ResolveGame("stolen-action", "actions", RequiredCapability(opened), new EngineDecision(auntMay.Id, []), active.Revision));
        Assert.Equal("not_your_turn", forgedTurnOption.Error?.Code);
        Assert.Equal("not_your_turn", stolenAction.Error?.Code);
        EngineResponse acted = host.Exchange(EngineRequest.ResolveGame("act", "actions", RequiredCapability(attached), new EngineDecision(auntMay.Id, []), other.Revision));
        Assert.Null(acted.Error);
        Assert.False(factory.AuntMay.Ready);
        Assert.Equal(1, factory.Game.State.Seats[1].IdentityCard.Damage);
        SessionSave save = Assert.Single(store.Load()).Save;
        Assert.Equal(frontierBeforeAction, save.EditFrontier);
        Assert.Empty(save.Units[^1].Exposures);
        EngineResponse staleActive = host.Exchange(EngineRequest.ResolveGame("stale-active", "actions", RequiredCapability(opened), new EngineDecision(activeOnly.Id, []), active.Revision));
        Assert.Equal("stale_decision", staleActive.Error?.Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ACompetingOffTurnActionIsStaleEvenWhenTheWinningCommandRemovesIt(bool winningCommandEndsGame)
    {
        var store = new MemorySessionStore();
        var factory = new VanishingOffTurnActionFactory(DatasetGameFactory.Load(RepositoryPaths.Root), winningCommandEndsGame);
        var host = new EngineHost(factory, new SequenceCapabilities("seat-zero", "invite-one", "seat-one"), new RestrictedVisibilityPolicy(0), store);
        var specification = new GameSpecification("rhino", ["captain_marvel", "spider_man"], [], Seed: 7);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "race", specification));
        EngineResponse attached = host.Exchange(EngineRequest.AttachGame("attach", "race", Assert.Single(opened.Invitations!).Invitation));
        _ = host.Exchange(EngineRequest.ResolveGame("zero-mulligan", "race", RequiredCapability(opened), TakeOnly(opened)));
        EngineResponse oneMulligan = host.Exchange(EngineRequest.SyncGame("one-mulligan-menu", "race", RequiredCapability(attached)));
        _ = host.Exchange(EngineRequest.ResolveGame("one-mulligan", "race", RequiredCapability(attached), TakeOnly(oneMulligan), oneMulligan.Revision));
        EngineResponse active = host.Exchange(EngineRequest.SyncGame("active", "race", RequiredCapability(opened)));
        EngineResponse offTurn = host.Exchange(EngineRequest.SyncGame("off-turn", "race", RequiredCapability(attached)));
        Affordance healing = Assert.Single(active.Prompt!.Affordances, option => option.AnchorId == factory.ActiveSource.ObjectId);
        Affordance disappearing = Assert.Single(offTurn.Prompt!.Affordances, option => option.AnchorId == factory.OffTurnSource.ObjectId);
        Assert.Equal(active.Revision, offTurn.Revision);
        EngineResponse won = host.Exchange(EngineRequest.ResolveGame("winner", "race", RequiredCapability(opened), new EngineDecision(healing.Id, []), active.Revision));
        EngineResponse lost = host.Exchange(EngineRequest.ResolveGame("loser", "race", RequiredCapability(attached), new EngineDecision(disappearing.Id, []), offTurn.Revision));
        Assert.Null(won.Error);
        Assert.Equal(0, factory.Game.State.Seats[1].IdentityCard.Damage);
        Assert.Equal(winningCommandEndsGame, won.Prompt is null);
        Assert.Equal("stale_decision", lost.Error?.Code);
        EngineResponse current = host.Exchange(EngineRequest.SyncGame("current", "race", RequiredCapability(attached)));
        Assert.Equal(won.Revision, current.Revision);
        if (winningCommandEndsGame)
        {
            var restarted = new EngineHost(new VanishingOffTurnActionFactory(DatasetGameFactory.Load(RepositoryPaths.Root), winningCommandEndsGame: true), visibility: new RestrictedVisibilityPolicy(0), store: store);
            EngineResponse restored = restarted.Exchange(EngineRequest.SyncGame("restored", "race", RequiredCapability(attached)));
            Assert.Null(restored.Error);
            Assert.Null(restored.Prompt);
            Assert.Equal(Outcome.PlayersWin, restored.World?.Outcome);
        }
    }

    [Fact]
    public void ACommittedGameRestartsFromItsSaveWithTheSameCapabilityAndRevision()
    {
        var store = new MemorySessionStore();
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(factory, new SequenceCapabilities("restart-owner"), store: store);
        EngineResponse opened = first.Exchange(EngineRequest.OpenGame("open", "restart-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse advanced = first.Exchange(EngineRequest.ResolveGame("resolve", "restart-table", opened.Capability!, TakeOnly(opened), opened.Revision));
        var restarted = new EngineHost(factory, store: store);
        EngineResponse restored = restarted.Exchange(EngineRequest.SyncGame("sync", "restart-table", opened.Capability!));
        Assert.Null(advanced.Error);
        Assert.Null(restored.Error);
        Assert.Equal(advanced.Revision, restored.Revision);
        Assert.Equal(EngineJson.Write(advanced with { RequestId = "same", Events = [] }), EngineJson.Write(restored with { RequestId = "same" }));
        StoredSession persisted = Assert.Single(store.Load());
        string save = SessionSaveJson.Write(persisted.Save);
        Assert.DoesNotContain("restart-owner", save, StringComparison.Ordinal);
        Assert.DoesNotContain("capability", save, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(64, persisted.Save.Compatibility.CardsSha256.Length);
        Assert.Equal(1, persisted.Save.Revision);
        Assert.Single(persisted.Save.Units);
    }

    [Fact]
    public void AnOpenNestedContinuationReplaysAcrossRestartUndoAndRedo()
    {
        var store = new MemorySessionStore();
        var factory = new NestedContinuationFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var first = new EngineHost(factory, new SequenceCapabilities("continuation-owner"), store: store);
        EngineResponse opened = first.Exchange(EngineRequest.OpenGame("open", "continuation-replay", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse kept = first.Exchange(EngineRequest.ResolveGame("keep", "continuation-replay", RequiredCapability(opened), TakeOnly(opened), opened.Revision));
        Affordance action = Assert.Single(kept.Prompt!.Affordances, option => option.AnchorId == factory.Source.ObjectId && string.Equals(option.Verb, Game.ActionVerb, StringComparison.Ordinal));
        EngineResponse waiting = first.Exchange(EngineRequest.ResolveGame("act", "continuation-replay", RequiredCapability(opened), new EngineDecision(action.Id, []), kept.Revision));
        StoredSession open = Assert.Single(store.Load());
        JournalUnit openUnit = open.Save.Units[^1];
        JournalStep beforeRestart = Assert.Single(openUnit.Decisions);
        string waitingDigest = factory.Game.State.Digest().Canonical();
        long waitingRng = factory.Game.State.Random.Generator.WordsConsumed;
        Assert.Equal("open", openUnit.Status);
        Assert.Equal(Question.Option, waiting.Prompt?.Asking);
        Assert.Equal(1, factory.Game.State.Seats[0].IdentityCard.Damage);
        var restarted = new EngineHost(factory, store: store);
        EngineResponse restored = restarted.Exchange(EngineRequest.SyncGame("sync", "continuation-replay", RequiredCapability(opened)));
        JournalStep afterRestart = Assert.Single(Assert.Single(store.Load()).Save.Units[^1].Decisions);
        Assert.Equal(EngineJson.Write(waiting with { RequestId = "same", Events = [] }), EngineJson.Write(restored with { RequestId = "same", Events = [] }));
        Assert.Equal(waitingDigest, factory.Game.State.Digest().Canonical());
        Assert.Equal(waitingRng, factory.Game.State.Random.Generator.WordsConsumed);
        Assert.Equal(beforeRestart.Events.Select(item => item.GetRawText()), afterRestart.Events.Select(item => item.GetRawText()));
        Assert.Equal(beforeRestart.RngWords, afterRestart.RngWords);
        Assert.Equal(beforeRestart.StateFingerprint, afterRestart.StateFingerprint);
        Affordance choice = restored.Prompt!.Affordances[0];
        EngineResponse answered = restarted.Exchange(EngineRequest.ResolveGame("answer", "continuation-replay", RequiredCapability(opened), new EngineDecision(choice.Id, []), restored.Revision));
        StoredSession completed = Assert.Single(store.Load());
        JournalUnit completedUnit = completed.Save.Units[^1];
        JournalStep answeredStep = completedUnit.Decisions[^1];
        string completedDigest = factory.Game.State.Digest().Canonical();
        long completedRng = factory.Game.State.Random.Generator.WordsConsumed;
        Assert.Null(answered.Error);
        Assert.Equal("complete", completedUnit.Status);
        Assert.Equal(6, factory.Game.State.Seats[0].IdentityCard.Damage);
        EngineResponse undone = restarted.Exchange(EngineRequest.UndoGame("undo", "continuation-replay", RequiredCapability(opened), cursor: completed.Save.Cursor - 1, expectedRevision: answered.Revision));
        EngineResponse redone = restarted.Exchange(EngineRequest.RedoGame("redo", "continuation-replay", RequiredCapability(opened), cursor: completed.Save.Cursor, expectedRevision: undone.Revision));
        JournalStep replayedAnswer = Assert.Single(store.Load()).Save.Units[^1].Decisions[^1];
        Assert.Null(undone.Error);
        Assert.Null(redone.Error);
        Assert.Equal(EngineJson.Write(answered with { RequestId = "same", Events = [], Revision = 0, History = null }), EngineJson.Write(redone with { RequestId = "same", Events = [], Revision = 0, History = null }));
        Assert.Equal(completedDigest, factory.Game.State.Digest().Canonical());
        Assert.Equal(completedRng, factory.Game.State.Random.Generator.WordsConsumed);
        Assert.Equal(answeredStep.Events.Select(item => item.GetRawText()), replayedAnswer.Events.Select(item => item.GetRawText()));
        Assert.Equal(answeredStep.RngWords, replayedAnswer.RngWords);
        Assert.Equal(answeredStep.StateFingerprint, replayedAnswer.StateFingerprint);
        Assert.Equal(6, factory.Game.State.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void AFailedSaveCommitDoesNotAdvanceOrInvalidateTheLiveGame()
    {
        var inner = new MemorySessionStore();
        var store = new FailingSessionStore(inner, failAtCommit: 2);
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("atomic-owner"), store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "atomic-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse failed = host.Exchange(EngineRequest.ResolveGame("resolve", "atomic-table", opened.Capability!, TakeOnly(opened), opened.Revision));
        EngineResponse current = host.Exchange(EngineRequest.SyncGame("sync", "atomic-table", opened.Capability!));
        Assert.Equal("save_failed", failed.Error?.Code);
        Assert.Null(current.Error);
        Assert.Equal(opened.Revision, current.Revision);
        Assert.Equal(EngineJson.Write(opened with { RequestId = "same", Capability = null, Invitations = null, Events = [], }), EngineJson.Write(current with { RequestId = "same" }));
        Assert.Equal(0, Assert.Single(inner.Load()).Save.Revision);
    }

    [Fact]
    public void AFailedOpenCommitReturnsABoundedStorageFailureWithoutPublishingAGame()
    {
        var inner = new MemorySessionStore();
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("unpublished-owner"), store: new FailingSessionStore(inner, failAtCommit: 1));
        EngineResponse failed = host.Exchange(EngineRequest.OpenGame("open", "unpublished-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse absent = host.Exchange(EngineRequest.SyncGame("sync", "unpublished-table", "unpublished-owner"));
        Assert.Equal("save_failed", failed.Error?.Code);
        Assert.Null(failed.Capability);
        Assert.Equal("session_not_found", absent.Error?.Code);
        Assert.Empty(inner.Load());
    }
}
