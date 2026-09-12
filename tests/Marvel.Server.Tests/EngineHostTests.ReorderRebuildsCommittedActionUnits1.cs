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
public sealed class EngineHostReorderRebuildsCommittedActionUnitsTests : EngineHostTestBase
{
    [Fact]
    public void ReorderRebuildsCommittedActionUnitsAndReplacesTheLiveTrace()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("reorder-owner"), store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "reorder-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse kept = host.Exchange(EngineRequest.ResolveGame("keep", "reorder-table", RequiredCapability(opened), TakeOnly(opened), opened.Revision));
        Affordance changeForm = Assert.Single(kept.Prompt!.Affordances, option => string.Equals(option.Verb, Game.ChangeForm, StringComparison.Ordinal));
        EngineResponse changed = host.Exchange(EngineRequest.ResolveGame("form", "reorder-table", RequiredCapability(opened), new EngineDecision(changeForm.Id, []), kept.Revision));
        EngineResponse first = host.Exchange(EngineRequest.ResolveGame("first", "reorder-table", RequiredCapability(opened), PayFirstPlayableCard(Assert.IsType<Prompt>(changed.Prompt)), changed.Revision));
        Affordance attack = Assert.Single(first.Prompt!.Affordances, option => string.Equals(option.Verb, BasicPowers.AttackVerb, StringComparison.Ordinal) && option.AnchorPlayer == 0);
        IReadOnlyList<int> attackTargets = Assert.IsType<TargetRequest>(attack.Targets).Legal.Take(attack.Targets.Min).ToList();
        EngineResponse second = host.Exchange(EngineRequest.ResolveGame("second", "reorder-table", RequiredCapability(opened), new EngineDecision(attack.Id, attackTargets), first.Revision));
        SessionSave before = Assert.Single(store.Load()).Save;
        int? firstAnchor = before.Units[2].Decisions[0].Decision.Selector.AnchorId;
        int? secondAnchor = before.Units[3].Decisions[0].Decision.Selector.AnchorId;
        EngineResponse reordered = host.Exchange(EngineRequest.ReorderGame("reorder", "reorder-table", RequiredCapability(opened), [3, 2], second.Revision));
        SessionSave after = Assert.Single(store.Load()).Save;
        Assert.Null(reordered.Error);
        Assert.Empty(reordered.Events);
        Assert.Equal(second.Revision + 1, reordered.Revision);
        Assert.Equal(4, after.Cursor);
        Assert.Equal(secondAnchor, after.Units[2].Decisions[0].Decision.Selector.AnchorId);
        Assert.Equal(firstAnchor, after.Units[3].Decisions[0].Decision.Selector.AnchorId);
        Assert.Empty(after.Units.Skip(2).SelectMany(unit => unit.Exposures));
        Assert.NotEqual(before.Units[3].Decisions[0].StateFingerprint, after.Units[2].Decisions[0].StateFingerprint);
        var restarted = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), store: store);
        EngineResponse restored = restarted.Exchange(EngineRequest.SyncGame("restored", "reorder-table", RequiredCapability(opened)));
        Assert.Equal(EngineJson.Write(reordered with { RequestId = "same" }), EngineJson.Write(restored with { RequestId = "same" }));
    }

    [Fact]
    public void ThreeActionRewriteReplaysAttackExcessAndHealingInTheNewOrder()
    {
        var store = new MemorySessionStore();
        var factory = new ExcessHealingFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var host = new EngineHost(factory, new SequenceCapabilities("rewrite-owner"), store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "rewrite-example", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse current = host.Exchange(EngineRequest.ResolveGame("keep", "rewrite-example", RequiredCapability(opened), TakeOnly(opened), opened.Revision));
        current = ResolveAction(host, opened, current, factory.AttackSource.ObjectId, "attack");
        current = ResolveAction(host, opened, current, factory.HealSource.ObjectId, "heal");
        current = ResolveAction(host, opened, current, factory.UpgradeSource.ObjectId, "upgrade");
        Assert.Equal(4, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, factory.ExcessMeter.Damage);
        EngineResponse rewritten = host.Exchange(EngineRequest.ReorderGame("rewrite", "rewrite-example", RequiredCapability(opened), [3, 1, 2], current.Revision));
        EngineResponse synchronized = host.Exchange(EngineRequest.SyncGame("sync", "rewrite-example", RequiredCapability(opened)));
        SessionSave save = Assert.Single(store.Load()).Save;
        Assert.Null(rewritten.Error);
        Assert.Equal(EngineJson.Write(rewritten with { RequestId = "same", Events = [] }), EngineJson.Write(synchronized with { RequestId = "same" }));
        Assert.Equal(2, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(3, factory.ExcessMeter.Damage);
        Assert.False(DeckTypes.IsInPlay(factory.Victim.Area.Type));
        Assert.Equal([factory.UpgradeSource.ObjectId, factory.AttackSource.ObjectId, factory.HealSource.ObjectId], save.Units.Skip(1).Take(3).Select(unit => unit.Decisions[0].Decision.Selector.AnchorId));
    }

    [Fact]
    public void ReorderRejectsATraceWhoseMovedActionNoLongerExists()
    {
        var store = new MemorySessionStore();
        var factory = new InHandActionFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var host = new EngineHost(factory, new SequenceCapabilities("invalid-reorder-owner"), store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "invalid-reorder", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse kept = host.Exchange(EngineRequest.ResolveGame("keep", "invalid-reorder", RequiredCapability(opened), TakeOnly(opened), opened.Revision));
        EngineResponse played = host.Exchange(EngineRequest.ResolveGame("play", "invalid-reorder", RequiredCapability(opened), PayCard(Assert.IsType<Prompt>(kept.Prompt), factory.AuntMay.ObjectId), kept.Revision));
        Affordance action = Assert.Single(played.Prompt!.Affordances, option => option.AnchorId == factory.AuntMay.ObjectId && string.Equals(option.Verb, Game.ActionVerb, StringComparison.Ordinal));
        EngineResponse acted = host.Exchange(EngineRequest.ResolveGame("act", "invalid-reorder", RequiredCapability(opened), new EngineDecision(action.Id, []), played.Revision));
        string before = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        EngineResponse rejected = host.Exchange(EngineRequest.ReorderGame("reorder", "invalid-reorder", RequiredCapability(opened), [2, 1], acted.Revision));
        EngineResponse current = host.Exchange(EngineRequest.SyncGame("current", "invalid-reorder", RequiredCapability(opened)));
        Assert.Equal("reorder_failed", rejected.Error?.Code);
        Assert.Equal(before, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal(EngineJson.Write(acted with { RequestId = "same", Events = [] }), EngineJson.Write(current with { RequestId = "same" }));
    }

    [Fact]
    public void ReorderCannotMoveTurnControlAsThoughItWereAnAction()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("control-reorder-owner"), store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "control-reorder", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse kept = host.Exchange(EngineRequest.ResolveGame("keep", "control-reorder", RequiredCapability(opened), TakeOnly(opened), opened.Revision));
        Affordance changeForm = Assert.Single(kept.Prompt!.Affordances, option => string.Equals(option.Verb, Game.ChangeForm, StringComparison.Ordinal));
        EngineResponse changed = host.Exchange(EngineRequest.ResolveGame("form", "control-reorder", RequiredCapability(opened), new EngineDecision(changeForm.Id, []), kept.Revision));
        EngineResponse played = host.Exchange(EngineRequest.ResolveGame("play", "control-reorder", RequiredCapability(opened), PayFirstPlayableCard(Assert.IsType<Prompt>(changed.Prompt)), changed.Revision));
        EngineResponse rejected = host.Exchange(EngineRequest.ReorderGame("reorder", "control-reorder", RequiredCapability(opened), [2, 1], played.Revision));
        Assert.Equal("reorder_kind", rejected.Error?.Code);
        Assert.Equal(played.Revision, Assert.Single(store.Load()).Save.Revision);
    }

    [Fact]
    public void UndoAndRedoReplaceTheLiveGameByVerifiedReplayAndAdvanceRevision()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("history-owner"), store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "history-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse kept = host.Exchange(EngineRequest.ResolveGame("keep", "history-table", RequiredCapability(opened), TakeOnly(opened), opened.Revision));
        EngineResponse undone = host.Exchange(EngineRequest.UndoGame("undo", "history-table", RequiredCapability(opened), cursor: 0, expectedRevision: kept.Revision));
        SessionSave inactive = Assert.Single(store.Load()).Save;
        Assert.Null(undone.Error);
        Assert.Empty(undone.Events);
        Assert.Equal(kept.Revision + 1, undone.Revision);
        Assert.Equal(0, undone.History?.Cursor);
        Assert.Empty(undone.History!.Undo);
        Assert.Equal([1], undone.History.Redo);
        Assert.Equal(0, inactive.Cursor);
        Assert.Single(inactive.Units);
        Assert.Equal(EngineJson.Write(opened with { RequestId = "same", Capability = null, Invitations = null, Revision = 0, History = null, }), EngineJson.Write(undone with { RequestId = "same", Capability = null, Invitations = null, Revision = 0, History = null, }));
        EngineResponse stale = host.Exchange(EngineRequest.ResolveGame("stale", "history-table", RequiredCapability(opened), new EngineDecision(Assert.IsType<Prompt>(kept.Prompt).Affordances[0].Id, []), kept.Revision));
        Assert.Equal("stale_decision", stale.Error?.Code);
        EngineResponse staleRedo = host.Exchange(EngineRequest.RedoGame("stale-redo", "history-table", RequiredCapability(opened), cursor: 1, expectedRevision: kept.Revision));
        Assert.Equal("stale_history", staleRedo.Error?.Code);
        var restarted = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), store: store);
        EngineResponse redone = restarted.Exchange(EngineRequest.RedoGame("redo", "history-table", RequiredCapability(opened), cursor: 1, expectedRevision: undone.Revision));
        SessionSave active = Assert.Single(store.Load()).Save;
        Assert.Null(redone.Error);
        Assert.Empty(redone.Events);
        Assert.Equal(undone.Revision + 1, redone.Revision);
        Assert.Equal([0], redone.History?.Undo);
        Assert.Empty(redone.History!.Redo);
        Assert.Equal(1, active.Cursor);
        Assert.Equal(EngineJson.Write(kept with { RequestId = "same", Revision = 0, History = null, }), EngineJson.Write(redone with { RequestId = "same", Revision = 0, History = null, }));
    }

    [Fact]
    public void ANewDecisionAfterUndoTruncatesTheRedoSuffix()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("branch-owner"), store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "branch-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse kept = host.Exchange(EngineRequest.ResolveGame("keep", "branch-table", RequiredCapability(opened), TakeOnly(opened), opened.Revision));
        EngineResponse undone = host.Exchange(EngineRequest.UndoGame("undo", "branch-table", RequiredCapability(opened), cursor: 0, expectedRevision: kept.Revision));
        Prompt mulligan = Assert.IsType<Prompt>(undone.Prompt);
        Affordance option = Assert.Single(mulligan.Affordances);
        int card = Assert.IsType<TargetRequest>(option.Targets).Legal[0];
        EngineResponse branched = host.Exchange(EngineRequest.ResolveGame("replace", "branch-table", RequiredCapability(opened), new EngineDecision(option.Id, [card]), undone.Revision));
        SessionSave save = Assert.Single(store.Load()).Save;
        EngineResponse noRedo = host.Exchange(EngineRequest.RedoGame("redo", "branch-table", RequiredCapability(opened), cursor: 1, expectedRevision: branched.Revision));
        Assert.Null(branched.Error);
        Assert.Equal(1, save.Cursor);
        Assert.Single(save.Units);
        Assert.Equal(1, save.EditFrontier);
        Assert.Equal("history_direction", noRedo.Error?.Code);
        Assert.Empty(branched.History!.Redo);
    }

    [Fact]
    public void ACardPlayThatExposesNoNewInformationIsReversible()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("card-play-owner"), store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "card-play-history", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse beforePlay = host.Exchange(EngineRequest.ResolveGame("keep", "card-play-history", RequiredCapability(opened), TakeOnly(opened), opened.Revision));
        EngineDecision play = PayFirstPlayableCard(Assert.IsType<Prompt>(beforePlay.Prompt));
        EngineResponse played = host.Exchange(EngineRequest.ResolveGame("play", "card-play-history", RequiredCapability(opened), play, beforePlay.Revision));
        SessionSave committed = Assert.Single(store.Load()).Save;
        JournalUnit playedUnit = committed.Units[^1];
        EngineResponse undone = host.Exchange(EngineRequest.UndoGame("undo", "card-play-history", RequiredCapability(opened), cursor: 1, expectedRevision: played.Revision));
        Assert.Null(played.Error);
        Assert.Equal("turn_action", playedUnit.Role);
        Assert.Equal("complete", playedUnit.Status);
        Assert.Empty(playedUnit.Exposures);
        Assert.Null(undone.Error);
        Assert.Equal(EngineJson.Write(beforePlay with { RequestId = "same", Revision = 0, History = null, }), EngineJson.Write(undone with { RequestId = "same", Revision = 0, History = null, }));
    }
}
