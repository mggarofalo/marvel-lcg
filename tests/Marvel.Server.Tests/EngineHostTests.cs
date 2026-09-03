using Marvel.Decisions;
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

public sealed class EngineHostTests
{
    [Fact]
    public void ReorderRebuildsCommittedActionUnitsAndReplacesTheLiveTrace()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("reorder-owner"),
            store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", "reorder-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse kept = host.Exchange(EngineRequest.ResolveGame(
            "keep", "reorder-table", RequiredCapability(opened),
            TakeOnly(opened), opened.Revision));
        Affordance changeForm = Assert.Single(kept.Prompt!.Affordances, option =>
            string.Equals(option.Verb, Game.ChangeForm, StringComparison.Ordinal));
        EngineResponse changed = host.Exchange(EngineRequest.ResolveGame(
            "form", "reorder-table", RequiredCapability(opened),
            new EngineDecision(changeForm.Id, []), kept.Revision));
        EngineResponse first = host.Exchange(EngineRequest.ResolveGame(
            "first", "reorder-table", RequiredCapability(opened),
            PayFirstPlayableCard(Assert.IsType<Prompt>(changed.Prompt)), changed.Revision));
        Affordance attack = Assert.Single(first.Prompt!.Affordances, option =>
            string.Equals(option.Verb, BasicPowers.AttackVerb, StringComparison.Ordinal)
            && option.AnchorPlayer == 0);
        IReadOnlyList<int> attackTargets = Assert.IsType<TargetRequest>(attack.Targets)
            .Legal.Take(attack.Targets.Min).ToList();
        EngineResponse second = host.Exchange(EngineRequest.ResolveGame(
            "second", "reorder-table", RequiredCapability(opened),
            new EngineDecision(attack.Id, attackTargets), first.Revision));
        SessionSave before = Assert.Single(store.Load()).Save;
        int? firstAnchor = before.Units[2].Decisions[0].Decision.Selector.AnchorId;
        int? secondAnchor = before.Units[3].Decisions[0].Decision.Selector.AnchorId;

        EngineResponse reordered = host.Exchange(EngineRequest.ReorderGame(
            "reorder", "reorder-table", RequiredCapability(opened),
            [3, 2], second.Revision));
        SessionSave after = Assert.Single(store.Load()).Save;

        Assert.Null(reordered.Error);
        Assert.Empty(reordered.Events);
        Assert.Equal(second.Revision + 1, reordered.Revision);
        Assert.Equal(4, after.Cursor);
        Assert.Equal(secondAnchor, after.Units[2].Decisions[0].Decision.Selector.AnchorId);
        Assert.Equal(firstAnchor, after.Units[3].Decisions[0].Decision.Selector.AnchorId);
        Assert.Empty(after.Units.Skip(2).SelectMany(unit => unit.Exposures));
        Assert.NotEqual(
            before.Units[3].Decisions[0].StateFingerprint,
            after.Units[2].Decisions[0].StateFingerprint);

        var restarted = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            store: store);
        EngineResponse restored = restarted.Exchange(EngineRequest.SyncGame(
            "restored", "reorder-table", RequiredCapability(opened)));
        Assert.Equal(
            EngineJson.Write(reordered with { RequestId = "same" }),
            EngineJson.Write(restored with { RequestId = "same" }));
    }

    [Fact]
    public void ThreeActionRewriteReplaysAttackExcessAndHealingInTheNewOrder()
    {
        var store = new MemorySessionStore();
        var factory = new ExcessHealingFactory(
            DatasetGameFactory.Load(RepositoryPaths.Root));
        var host = new EngineHost(
            factory,
            new SequenceCapabilities("rewrite-owner"),
            store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", "rewrite-example",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse current = host.Exchange(EngineRequest.ResolveGame(
            "keep", "rewrite-example", RequiredCapability(opened),
            TakeOnly(opened), opened.Revision));

        current = ResolveAction(host, opened, current, factory.AttackSource.ObjectId, "attack");
        current = ResolveAction(host, opened, current, factory.HealSource.ObjectId, "heal");
        current = ResolveAction(host, opened, current, factory.UpgradeSource.ObjectId, "upgrade");
        Assert.Equal(4, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, factory.ExcessMeter.Damage);

        EngineResponse rewritten = host.Exchange(EngineRequest.ReorderGame(
            "rewrite", "rewrite-example", RequiredCapability(opened),
            [3, 1, 2], current.Revision));
        EngineResponse synchronized = host.Exchange(EngineRequest.SyncGame(
            "sync", "rewrite-example", RequiredCapability(opened)));
        SessionSave save = Assert.Single(store.Load()).Save;

        Assert.Null(rewritten.Error);
        Assert.Equal(
            EngineJson.Write(rewritten with { RequestId = "same", Events = [] }),
            EngineJson.Write(synchronized with { RequestId = "same" }));
        Assert.Equal(2, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(3, factory.ExcessMeter.Damage);
        Assert.False(DeckTypes.IsInPlay(factory.Victim.Area.Type));
        Assert.Equal(
            [factory.UpgradeSource.ObjectId,
             factory.AttackSource.ObjectId,
             factory.HealSource.ObjectId],
            save.Units.Skip(1).Take(3)
                .Select(unit => unit.Decisions[0].Decision.Selector.AnchorId));
    }

    private static EngineResponse ResolveAction(
        EngineHost host,
        EngineResponse opened,
        EngineResponse current,
        int source,
        string requestId)
    {
        Affordance action = Assert.Single(
            Assert.IsType<Prompt>(current.Prompt).Affordances,
            option => option.AnchorId == source);
        EngineResponse resolved = host.Exchange(EngineRequest.ResolveGame(
            requestId,
            "rewrite-example",
            RequiredCapability(opened),
            new EngineDecision(action.Id, []),
            current.Revision));
        Assert.Null(resolved.Error);
        return resolved;
    }

    [Fact]
    public void ReorderRejectsATraceWhoseMovedActionNoLongerExists()
    {
        var store = new MemorySessionStore();
        var factory = new InHandActionFactory(
            DatasetGameFactory.Load(RepositoryPaths.Root));
        var host = new EngineHost(
            factory,
            new SequenceCapabilities("invalid-reorder-owner"),
            store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", "invalid-reorder",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse kept = host.Exchange(EngineRequest.ResolveGame(
            "keep", "invalid-reorder", RequiredCapability(opened),
            TakeOnly(opened), opened.Revision));
        EngineResponse played = host.Exchange(EngineRequest.ResolveGame(
            "play", "invalid-reorder", RequiredCapability(opened),
            PayCard(Assert.IsType<Prompt>(kept.Prompt), factory.AuntMay.ObjectId),
            kept.Revision));
        Affordance action = Assert.Single(played.Prompt!.Affordances, option =>
            option.AnchorId == factory.AuntMay.ObjectId
            && string.Equals(option.Verb, Game.ActionVerb, StringComparison.Ordinal));
        EngineResponse acted = host.Exchange(EngineRequest.ResolveGame(
            "act", "invalid-reorder", RequiredCapability(opened),
            new EngineDecision(action.Id, []), played.Revision));
        string before = SessionSaveJson.Write(Assert.Single(store.Load()).Save);

        EngineResponse rejected = host.Exchange(EngineRequest.ReorderGame(
            "reorder", "invalid-reorder", RequiredCapability(opened),
            [2, 1], acted.Revision));
        EngineResponse current = host.Exchange(EngineRequest.SyncGame(
            "current", "invalid-reorder", RequiredCapability(opened)));

        Assert.Equal("reorder_failed", rejected.Error?.Code);
        Assert.Equal(before, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal(
            EngineJson.Write(acted with { RequestId = "same", Events = [] }),
            EngineJson.Write(current with { RequestId = "same" }));
    }

    [Fact]
    public void ReorderCannotMoveTurnControlAsThoughItWereAnAction()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("control-reorder-owner"),
            store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", "control-reorder",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse kept = host.Exchange(EngineRequest.ResolveGame(
            "keep", "control-reorder", RequiredCapability(opened),
            TakeOnly(opened), opened.Revision));
        Affordance changeForm = Assert.Single(kept.Prompt!.Affordances, option =>
            string.Equals(option.Verb, Game.ChangeForm, StringComparison.Ordinal));
        EngineResponse changed = host.Exchange(EngineRequest.ResolveGame(
            "form", "control-reorder", RequiredCapability(opened),
            new EngineDecision(changeForm.Id, []), kept.Revision));
        EngineResponse played = host.Exchange(EngineRequest.ResolveGame(
            "play", "control-reorder", RequiredCapability(opened),
            PayFirstPlayableCard(Assert.IsType<Prompt>(changed.Prompt)), changed.Revision));

        EngineResponse rejected = host.Exchange(EngineRequest.ReorderGame(
            "reorder", "control-reorder", RequiredCapability(opened),
            [2, 1], played.Revision));

        Assert.Equal("reorder_kind", rejected.Error?.Code);
        Assert.Equal(played.Revision, Assert.Single(store.Load()).Save.Revision);
    }

    [Fact]
    public void UndoAndRedoReplaceTheLiveGameByVerifiedReplayAndAdvanceRevision()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("history-owner"),
            store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open",
            "history-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse kept = host.Exchange(EngineRequest.ResolveGame(
            "keep",
            "history-table",
            RequiredCapability(opened),
            TakeOnly(opened),
            opened.Revision));

        EngineResponse undone = host.Exchange(EngineRequest.UndoGame(
            "undo", "history-table", RequiredCapability(opened),
            cursor: 0, expectedRevision: kept.Revision));
        SessionSave inactive = Assert.Single(store.Load()).Save;

        Assert.Null(undone.Error);
        Assert.Empty(undone.Events);
        Assert.Equal(kept.Revision + 1, undone.Revision);
        Assert.Equal(0, undone.History?.Cursor);
        Assert.Empty(undone.History!.Undo);
        Assert.Equal([1], undone.History.Redo);
        Assert.Equal(0, inactive.Cursor);
        Assert.Single(inactive.Units);
        Assert.Equal(
            EngineJson.Write(opened with
            {
                RequestId = "same",
                Capability = null,
                Invitations = null,
                Revision = 0,
                History = null,
            }),
            EngineJson.Write(undone with
            {
                RequestId = "same",
                Capability = null,
                Invitations = null,
                Revision = 0,
                History = null,
            }));

        EngineResponse stale = host.Exchange(EngineRequest.ResolveGame(
            "stale", "history-table", RequiredCapability(opened),
            new EngineDecision(Assert.IsType<Prompt>(kept.Prompt).Affordances[0].Id, []),
            kept.Revision));
        Assert.Equal("stale_decision", stale.Error?.Code);
        EngineResponse staleRedo = host.Exchange(EngineRequest.RedoGame(
            "stale-redo", "history-table", RequiredCapability(opened),
            cursor: 1, expectedRevision: kept.Revision));
        Assert.Equal("stale_history", staleRedo.Error?.Code);

        var restarted = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            store: store);
        EngineResponse redone = restarted.Exchange(EngineRequest.RedoGame(
            "redo", "history-table", RequiredCapability(opened),
            cursor: 1, expectedRevision: undone.Revision));
        SessionSave active = Assert.Single(store.Load()).Save;

        Assert.Null(redone.Error);
        Assert.Empty(redone.Events);
        Assert.Equal(undone.Revision + 1, redone.Revision);
        Assert.Equal([0], redone.History?.Undo);
        Assert.Empty(redone.History!.Redo);
        Assert.Equal(1, active.Cursor);
        Assert.Equal(
            EngineJson.Write(kept with
            {
                RequestId = "same", Revision = 0, History = null,
            }),
            EngineJson.Write(redone with
            {
                RequestId = "same", Revision = 0, History = null,
            }));
    }

    [Fact]
    public void ANewDecisionAfterUndoTruncatesTheRedoSuffix()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("branch-owner"),
            store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", "branch-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse kept = host.Exchange(EngineRequest.ResolveGame(
            "keep", "branch-table", RequiredCapability(opened),
            TakeOnly(opened), opened.Revision));
        EngineResponse undone = host.Exchange(EngineRequest.UndoGame(
            "undo", "branch-table", RequiredCapability(opened),
            cursor: 0, expectedRevision: kept.Revision));
        Prompt mulligan = Assert.IsType<Prompt>(undone.Prompt);
        Affordance option = Assert.Single(mulligan.Affordances);
        int card = Assert.IsType<TargetRequest>(option.Targets).Legal[0];

        EngineResponse branched = host.Exchange(EngineRequest.ResolveGame(
            "replace", "branch-table", RequiredCapability(opened),
            new EngineDecision(option.Id, [card]), undone.Revision));
        SessionSave save = Assert.Single(store.Load()).Save;
        EngineResponse noRedo = host.Exchange(EngineRequest.RedoGame(
            "redo", "branch-table", RequiredCapability(opened),
            cursor: 1, expectedRevision: branched.Revision));

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
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("card-play-owner"),
            store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", "card-play-history",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse beforePlay = host.Exchange(EngineRequest.ResolveGame(
            "keep", "card-play-history", RequiredCapability(opened),
            TakeOnly(opened), opened.Revision));
        EngineDecision play = PayFirstPlayableCard(
            Assert.IsType<Prompt>(beforePlay.Prompt));

        EngineResponse played = host.Exchange(EngineRequest.ResolveGame(
            "play", "card-play-history", RequiredCapability(opened),
            play, beforePlay.Revision));
        SessionSave committed = Assert.Single(store.Load()).Save;
        JournalUnit playedUnit = committed.Units[^1];
        EngineResponse undone = host.Exchange(EngineRequest.UndoGame(
            "undo", "card-play-history", RequiredCapability(opened),
            cursor: 1, expectedRevision: played.Revision));

        Assert.Null(played.Error);
        Assert.Equal("turn_action", playedUnit.Role);
        Assert.Equal("complete", playedUnit.Status);
        Assert.Empty(playedUnit.Exposures);
        Assert.Null(undone.Error);
        Assert.Equal(
            EngineJson.Write(beforePlay with
            {
                RequestId = "same", Revision = 0, History = null,
            }),
            EngineJson.Write(undone with
            {
                RequestId = "same", Revision = 0, History = null,
            }));
    }

    [Fact]
    public void NewInformationAndAnotherSeatsHistoryExplainWhyUndoIsUnavailable()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("seat-zero", "invite-one", "seat-one"),
            new RestrictedVisibilityPolicy(0),
            store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", "bounded-history",
            new GameSpecification(
                "rhino", ["captain_marvel", "spider_man"], [], Seed: 73)));
        EngineResponse attached = host.Exchange(EngineRequest.AttachGame(
            "attach", "bounded-history", Assert.Single(opened.Invitations!).Invitation));
        EngineResponse afterZero = host.Exchange(EngineRequest.ResolveGame(
            "zero", "bounded-history", RequiredCapability(opened),
            TakeOnly(opened), opened.Revision));
        EngineResponse onePrompt = host.Exchange(EngineRequest.SyncGame(
            "one-prompt", "bounded-history", RequiredCapability(attached)));
        EngineResponse afterOne = host.Exchange(EngineRequest.ResolveGame(
            "one", "bounded-history", RequiredCapability(attached),
            TakeOnly(onePrompt), onePrompt.Revision));

        EngineResponse otherSeat = host.Exchange(EngineRequest.UndoGame(
            "other-seat", "bounded-history", RequiredCapability(opened),
            cursor: 1, expectedRevision: afterOne.Revision));
        EngineResponse undone = host.Exchange(EngineRequest.UndoGame(
            "own", "bounded-history", RequiredCapability(attached),
            cursor: 1, expectedRevision: afterOne.Revision));
        EngineResponse converged = host.Exchange(EngineRequest.SyncGame(
            "sync", "bounded-history", RequiredCapability(opened)));
        EngineResponse otherRedo = host.Exchange(EngineRequest.RedoGame(
            "other-redo", "bounded-history", RequiredCapability(opened),
            cursor: 2, expectedRevision: undone.Revision));

        Assert.Null(afterZero.Error);
        Assert.Equal([0], host.Exchange(EngineRequest.SyncGame(
            "zero-history", "bounded-history", RequiredCapability(opened)))
            .History!.Undo);
        Assert.Empty(onePrompt.History!.Undo);
        Assert.Equal([1], afterOne.History?.Undo);
        Assert.Equal("history_authority", otherSeat.Error?.Code);
        Assert.Null(undone.Error);
        Assert.Equal(undone.Revision, converged.Revision);
        Assert.Equal("history_authority", otherRedo.Error?.Code);

        Prompt current = Assert.IsType<Prompt>(undone.Prompt);
        Affordance draw = Assert.Single(current.Affordances);
        int card = Assert.IsType<TargetRequest>(draw.Targets).Legal[0];
        EngineResponse revealed = host.Exchange(EngineRequest.ResolveGame(
            "draw", "bounded-history", RequiredCapability(attached),
            new EngineDecision(draw.Id, [card]), undone.Revision));
        EngineResponse beyondFrontier = host.Exchange(EngineRequest.UndoGame(
            "frontier", "bounded-history", RequiredCapability(attached),
            cursor: 1, expectedRevision: revealed.Revision));

        Assert.Null(revealed.Error);
        Assert.Equal("history_frontier", beyondFrontier.Error?.Code);
    }

    [Fact]
    public void FailedHistoryPersistenceLeavesThePriorLiveGameAuthoritative()
    {
        var memory = new MemorySessionStore();
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("atomic-owner"),
            store: new FailingSessionStore(memory, failAtCommit: 3));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", "atomic-history",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse kept = host.Exchange(EngineRequest.ResolveGame(
            "keep", "atomic-history", RequiredCapability(opened),
            TakeOnly(opened), opened.Revision));

        EngineResponse failed = host.Exchange(EngineRequest.UndoGame(
            "undo", "atomic-history", RequiredCapability(opened),
            cursor: 0, expectedRevision: kept.Revision));
        EngineResponse current = host.Exchange(EngineRequest.SyncGame(
            "sync", "atomic-history", RequiredCapability(opened)));

        Assert.Equal("history_failed", failed.Error?.Code);
        Assert.Equal(kept.Revision, current.Revision);
        Assert.Equal(
            EngineJson.Write(kept with { RequestId = "same" }),
            EngineJson.Write(current with { RequestId = "same" }));
        Assert.Equal(1, Assert.Single(memory.Load()).Save.Cursor);
    }

    [Fact]
    public void OpenHistoryCannotBeEditedAndForgedCompletionFailsReplay()
    {
        var store = new MemorySessionStore();
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(
            factory,
            new SequenceCapabilities("open-unit-owner"),
            store: store);
        EngineResponse opened = first.Exchange(EngineRequest.OpenGame(
            "open", "open-unit-history",
            new GameSpecification("rhino", ["captain_marvel"], [], Seed: 73)));
        EngineResponse kept = first.Exchange(EngineRequest.ResolveGame(
            "keep", "open-unit-history", RequiredCapability(opened),
            TakeOnly(opened), opened.Revision));
        Affordance changeForm = Assert.Single(kept.Prompt!.Affordances, option =>
            string.Equals(option.Verb, Game.ChangeForm, StringComparison.Ordinal));
        EngineResponse changed = first.Exchange(EngineRequest.ResolveGame(
            "form", "open-unit-history", RequiredCapability(opened),
            new EngineDecision(changeForm.Id, []), kept.Revision));
        Affordance playEighteen = Assert.Single(changed.Prompt!.Affordances, option =>
            string.Equals(option.Verb, "Play", StringComparison.Ordinal)
            && option.AnchorId == 18);
        EngineResponse firstPlay = first.Exchange(EngineRequest.ResolveGame(
            "first-play", "open-unit-history", RequiredCapability(opened),
            new EngineDecision(
                playEighteen.Id,
                [1],
                [16],
                Allocations: [new ResourceAllocation(16, 0, "YY")]),
            changed.Revision));
        Affordance mariaHill = Assert.Single(firstPlay.Prompt!.Affordances, option =>
            string.Equals(option.Verb, "Play", StringComparison.Ordinal)
            && option.AnchorId == 24);
        EngineResponse responseWindow = first.Exchange(EngineRequest.ResolveGame(
            "maria-hill", "open-unit-history", RequiredCapability(opened),
            new EngineDecision(
                mariaHill.Id,
                [1],
                [30, 26],
                Allocations:
                [
                    new ResourceAllocation(30, 0, "B"),
                    new ResourceAllocation(26, 0, "R"),
                ]),
            firstPlay.Revision));

        StoredSession stored = Assert.Single(store.Load());
        JournalUnit unit = stored.Save.Units[^1];
        EngineResponse refused = first.Exchange(EngineRequest.UndoGame(
            "undo", "open-unit-history", RequiredCapability(opened),
            cursor: 3, expectedRevision: responseWindow.Revision));

        Assert.Null(responseWindow.Error);
        Assert.Equal("open", unit.Status);
        Assert.Empty(unit.Exposures);
        Assert.Empty(responseWindow.History!.Undo);
        Assert.Equal("history_open", refused.Error?.Code);

        store.Commit(stored with
        {
            Save = stored.Save with
            {
                Units =
                [
                    .. stored.Save.Units.Take(stored.Save.Units.Count - 1),
                    unit with { Status = "complete" },
                ],
            },
        });
        var restarted = new EngineHost(factory, store: store);
        Assert.Equal(
            "session_not_found",
            restarted.Exchange(EngineRequest.SyncGame(
                "quarantined", "open-unit-history", RequiredCapability(opened))).Error?.Code);
    }

    [Fact]
    public void ADecisionForAnEarlierRevisionIsRejectedWithoutAdvancingTheGame()
    {
        var host = new EngineHost(
            DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root),
            new SequenceCapabilities("owner"));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open",
            "revision-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7)));
        EngineDecision mulligan = TakeOnly(opened);

        EngineResponse advanced = host.Exchange(EngineRequest.ResolveGame(
            "mulligan",
            "revision-table",
            opened.Capability!,
            mulligan,
            opened.Revision));
        EngineResponse beforeStale = host.Exchange(EngineRequest.SyncGame(
            "before-stale", "revision-table", opened.Capability!));
        EngineResponse stale = host.Exchange(EngineRequest.ResolveGame(
            "stale",
            "revision-table",
            opened.Capability!,
            mulligan,
            opened.Revision));
        EngineResponse afterStale = host.Exchange(EngineRequest.SyncGame(
            "after-stale", "revision-table", opened.Capability!));

        Assert.Null(advanced.Error);
        Assert.Equal(opened.Revision + 1, advanced.Revision);
        Assert.Equal("stale_decision", stale.Error?.Code);
        Assert.Empty(stale.Events);
        Assert.Equal(beforeStale.Revision, afterStale.Revision);
        Assert.Equal(
            EngineJson.Write(beforeStale with { RequestId = "same" }),
            EngineJson.Write(afterStale with { RequestId = "same" }));
    }

    [Fact]
    public void AuthoredSetupChoicesAreAvailableBeforeAGameExists()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));

        EngineResponse response = host.Exchange(EngineRequest.ReadSetup("choices"));

        Assert.Null(response.Error);
        Assert.Equal(string.Empty, response.GameId);
        Assert.Null(response.World);
        Assert.Empty(response.Events);
        SetupChoices choices = Assert.IsType<SetupChoices>(response.Setup);
        Assert.Equal(
            ["spider_man", "captain_marvel", "she_hulk", "iron_man", "black_panther"],
            choices.Heroes.Select(choice => choice.Key));
        Assert.Equal("Spider-Man", choices.Heroes[0].Name);
        Assert.Equal(
            ["rhino", "rhino_expert", "klaw", "klaw_expert", "ultron", "ultron_expert"],
            choices.Scenarios.Select(choice => choice.Key));
        Assert.False(choices.Scenarios[0].Expert);
        Assert.True(choices.Scenarios[1].Expert);
        Assert.Equal(["bomb_scare"], choices.Scenarios[0].RecommendedModularSets);
        Assert.Equal(
            [
                "bomb_scare", "masters_of_evil", "under_attack",
                "legions_of_hydra", "the_doomsday_chair",
            ],
            choices.ModularSets.Select(choice => choice.Key));
        Assert.Equal("The Doomsday Chair", choices.ModularSets[^1].Name);
        Assert.Equal(EngineBuildIdentity.ProductVersion, choices.Runtime.ProductVersion);
        Assert.Equal(EngineBuildIdentity.Commit, choices.Runtime.Commit);
        Assert.Equal(EngineProtocol.Version, choices.Runtime.Protocol);
        Assert.Equal(SessionSave.CurrentSchema, choices.Runtime.SaveSchema);
        Assert.Equal(64, choices.Runtime.CardsSha256.Length);
        Assert.Equal(64, choices.Runtime.SetupSha256.Length);
        Assert.Equal(64, choices.Runtime.AbilitiesSha256.Length);
    }

    [Fact]
    public void InProcessSetupResponsesCannotMutateTheAuthoredCatalog()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        SetupChoices first = Assert.IsType<SetupChoices>(
            host.Exchange(EngineRequest.ReadSetup("first")).Setup);
        IList<string> exposed = Assert.IsAssignableFrom<IList<string>>(
            first.Scenarios.Single(choice => choice.Key == "rhino")
                .RecommendedModularSets);

        Assert.Throws<NotSupportedException>(exposed.Clear);

        SetupChoices second = Assert.IsType<SetupChoices>(
            host.Exchange(EngineRequest.ReadSetup("second")).Setup);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open",
            "catalog-isolation",
            new GameSpecification(
                "rhino", ["spider_man"], ModularSets: null, Seed: 7)));
        Assert.Equal(
            ["bomb_scare"],
            second.Scenarios.Single(choice => choice.Key == "rhino")
                .RecommendedModularSets);
        Assert.Null(opened.Error);
    }

    [Fact]
    public void SetupDiscoveryRejectsFieldsThatCouldNameOrMutateAGame()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        EngineRequest valid = EngineRequest.ReadSetup("bad");
        EngineRequest[] malformed =
        {
            valid with { GameId = "game" },
            valid with { Capability = "capability" },
            valid with
            {
                Game = new GameSpecification("rhino", ["spider_man"], [], 7),
            },
            valid with { Decision = EngineDecision.Decline },
            valid with { Viewer = new ViewerClaim(Watch: true) },
            valid with { ExpectedRevision = 0 },
        };

        foreach (EngineRequest request in malformed)
        {
            EngineResponse response = host.Exchange(request);
            Assert.Equal("invalid_request", response.Error?.Code);
            Assert.Null(response.Setup);
        }
    }

    [Fact]
    public void HostsWithoutSetupDiscoveryReportItAsUnavailable()
    {
        var factory = new UnusedFactory();
        var host = new EngineHost(factory);

        EngineResponse response = host.Exchange(EngineRequest.ReadSetup("choices"));

        Assert.Equal("setup_unavailable", response.Error?.Code);
        Assert.Equal(0, factory.Calls);
    }

    [Theory]
    [InlineData("unus", "spider_man", null, "no campaign named 'unus'")]
    [InlineData("rhino", "sp_dr", null, "no hero named 'sp_dr'")]
    [InlineData("rhino", "spider_man", "sinister_syndicate",
        "no encounter set named 'sinister_syndicate'")]
    public void ExpansionProductsAreRejectedAtTheDatasetBoundary(
        string scenario, string hero, string? modular, string message)
    {
        // Product names resolve before WorldSetup creates a board or seeds its
        // RNG. Keeping the complete printed card catalog therefore cannot make
        // an unsupported expansion game partly start.
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var specification = new GameSpecification(
            scenario, [hero], modular is null ? null : [modular], Seed: 7);

        var refused = Assert.Throws<KeyNotFoundException>(() => factory.Create(specification));

        Assert.Contains(message, refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CanonicalContentOpensAndResolvesThroughTheHost()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var opened = host.Exchange(EngineRequest.OpenGame(
            "request-1",
            "game-1",
            new GameSpecification("rhino", ["spider_man"], ModularSets: null, Seed: 7)));

        Assert.Null(opened.Error);
        Assert.NotNull(opened.Prompt);
        Assert.Equal("request-1", opened.RequestId);
        Assert.Equal("game-1", opened.GameId);

        var resolved = host.Exchange(EngineRequest.ResolveGame(
            "request-2", "game-1", RequiredCapability(opened), TakeOnly(opened)));

        Assert.Null(resolved.Error);
        Assert.NotNull(resolved.Prompt);
        Assert.Equal("request-2", resolved.RequestId);
    }

    [Fact]
    public void SeparateCapabilitiesMayUseTheSameClientChosenGameId()
    {
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("first-capability", "second-capability"));
        var request = EngineRequest.OpenGame(
            "first",
            "chosen-id",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7));

        var first = host.Exchange(request);
        var duplicate = host.Exchange(request with { RequestId = "again" });

        Assert.Null(first.Error);
        Assert.Null(duplicate.Error);
        Assert.NotEqual(first.Capability, duplicate.Capability);
    }

    [Fact]
    public void InvalidCommandsFailBeforeTheyReachAFactory()
    {
        var factory = new UnusedFactory();
        var host = new EngineHost(factory);

        var wrongVersion = host.Exchange(new EngineRequest(
            99, "version", EngineProtocol.Open, "game",
            Game: new GameSpecification("rhino", ["spider_man"], null, 1)));
        var unknownGame = host.Exchange(EngineRequest.ResolveGame(
            "resolve", "missing", "missing-capability", EngineDecision.Decline));

        Assert.Equal("unsupported_version", wrongVersion.Error?.Code);
        Assert.Equal("session_not_found", unknownGame.Error?.Code);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public void InvalidAdditionalSeatGrantsFailBeforeTheyReachAFactory()
    {
        Func<int, ViewScope> seat = index =>
            new RestrictedVisibilityPolicy(index).Authorize(null, players: 2);
        ViewScope everySeat = new PermissiveVisibilityPolicy().Authorize(
            new ViewerClaim(HotSeat: true), players: 2);
        (string Name, IReadOnlyList<SeatScope>? Grants)[] hostile =
        {
            ("missing collection", null),
            ("empty grant", new SeatScope[] { null! }),
            ("negative seat", [new SeatScope(-1, seat(0))]),
            ("seat past player count", [new SeatScope(2, seat(0))]),
            ("duplicate seat", [new SeatScope(1, seat(1)), new SeatScope(1, seat(1))]),
            ("primary seat repeated", [new SeatScope(0, seat(0))]),
            ("different seat in scope", [new SeatScope(1, seat(0))]),
            ("more than one seat in scope", [new SeatScope(1, everySeat)]),
            ("empty scope", [new SeatScope(1, ViewScope.None)]),
        };

        foreach ((string name, IReadOnlyList<SeatScope>? grants) in hostile)
        {
            var factory = new UnusedFactory();
            var host = new EngineHost(
                factory,
                visibility: new HostileVisibilityPolicy(grants));

            EngineResponse response = host.Exchange(EngineRequest.OpenGame(
                name,
                "game",
                new GameSpecification(
                    "rhino", ["spider_man", "captain_marvel"], [], Seed: 7)));

            Assert.Equal("invalid_request", response.Error?.Code);
            Assert.Equal(0, factory.Calls);
        }
    }

    [Fact]
    public void MissingPrimaryScopeFailsBeforeItReachesAFactory()
    {
        var factory = new UnusedFactory();
        var host = new EngineHost(factory, visibility: new NullPrimaryVisibilityPolicy());

        EngineResponse response = host.Exchange(EngineRequest.OpenGame(
            "missing-primary",
            "game",
            new GameSpecification(
                "rhino", ["spider_man", "captain_marvel"], [], Seed: 7)));

        Assert.Equal("invalid_request", response.Error?.Code);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public void AdditionalSeatGrantsAreSnapshottedBeforeValidationAndIssuance()
    {
        ViewScope zero = new RestrictedVisibilityPolicy(0).Authorize(null, players: 2);
        ViewScope one = new RestrictedVisibilityPolicy(1).Authorize(null, players: 2);
        var grants = new ChangingSeatGrants(
            [new SeatScope(1, one)],
            [new SeatScope(0, zero)]);
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("owner", "invitation"),
            new HostileVisibilityPolicy(grants));

        EngineResponse response = host.Exchange(EngineRequest.OpenGame(
            "snapshot",
            "game",
            new GameSpecification(
                "rhino", ["spider_man", "captain_marvel"], [], Seed: 7)));

        Assert.Null(response.Error);
        Assert.Equal(1, Assert.Single(response.Invitations!).Seat);
        Assert.Equal(1, grants.Enumerations);
    }

    [Fact]
    public void EngineFailuresDoNotReturnInternalDiagnostics()
    {
        var host = new EngineHost(new FailingFactory("secret-card-identity"));

        EngineResponse failed = host.Exchange(EngineRequest.OpenGame(
            "open",
            "game",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7)));

        Assert.Equal("engine_error", failed.Error?.Code);
        Assert.DoesNotContain(
            "secret-card-identity", failed.Error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VersionOneIsRejectedBeforeItCanOpenAGame()
    {
        // The current protocol adds play-area topology kinds to the event
        // union and per-target allocation capacities. A version 1 client
        // cannot deserialize those responses, so it is rejected before the
        // factory can create mutable game state.
        var factory = new UnusedFactory();
        var host = new EngineHost(factory);

        var rejected = host.Exchange(new EngineRequest(
            1, "old-client", EngineProtocol.Open, "game",
            Game: new GameSpecification("rhino", ["spider_man"], null, 1)));

        Assert.Equal(11, EngineProtocol.Version);
        Assert.Equal(EngineProtocol.Version, rejected.Version);
        Assert.Equal("unsupported_version", rejected.Error?.Code);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public void ClosingAGameReleasesItsClientChosenId()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var specification = new GameSpecification(
            "rhino", ["spider_man"], ModularSets: [], Seed: 7);
        var opened = host.Exchange(
            EngineRequest.OpenGame("open", "reusable", specification));
        Assert.Null(opened.Error);

        Assert.Null(host.Exchange(
            EngineRequest.CloseGame(
                "close", "reusable", RequiredCapability(opened))).Error);
        Assert.Null(host.Exchange(
            EngineRequest.OpenGame("reopen", "reusable", specification)).Error);
    }

    [Fact]
    public void AnInvalidDecisionIsRejectedBeforeMutationAndTheSessionRemainsAvailable()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var opened = host.Exchange(EngineRequest.OpenGame(
            "open",
            "fail-closed",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7)));
        Assert.Null(opened.Error);
        string capability = RequiredCapability(opened);

        var rejected = host.Exchange(EngineRequest.ResolveGame(
            "bad", "fail-closed", capability, new EngineDecision(999, [])));
        var after = host.Exchange(EngineRequest.ResolveGame(
            "after", "fail-closed", capability, TakeOnly(opened)));

        Assert.Equal("invalid_decision", rejected.Error?.Code);
        Assert.Null(after.Error);
    }

    [Fact]
    public void RestrictedHostsDoNotTrustWatchOrHotSeatClaims()
    {
        var specification = new GameSpecification(
            "rhino", ["spider_man", "captain_marvel"], [], Seed: 7);
        var seatZeroHost = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            visibility: new RestrictedVisibilityPolicy(0));
        var seatOneHost = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            visibility: new RestrictedVisibilityPolicy(1));

        EngineResponse zero = seatZeroHost.Exchange(EngineRequest.OpenGame(
            "zero", "game", specification, new ViewerClaim(Watch: true)));
        EngineResponse one = seatOneHost.Exchange(EngineRequest.OpenGame(
            "one", "game", specification, new ViewerClaim(HotSeat: true)));

        Assert.Null(zero.Error);
        Assert.Null(one.Error);
        Assert.All(Hand(zero, 0), card => Assert.NotNull(card.Face));
        Assert.All(Hand(zero, 1), card =>
        {
            Assert.Null(card.Face);
            Assert.Null(card.Id);
        });
        Assert.All(Hand(one, 0), card =>
        {
            Assert.Null(card.Face);
            Assert.Null(card.Id);
        });
        Assert.All(Hand(one, 1), card => Assert.NotNull(card.Face));
    }

    [Fact]
    public void RestrictedSeatsReceiveOnlyTheirTurnOptionsAndMaySubmitTheirOwnOffTurnAction()
    {
        var store = new MemorySessionStore();
        var factory = new OffTurnActionFactory(
            DatasetGameFactory.Load(RepositoryPaths.Root));
        var host = new EngineHost(
            factory,
            new SequenceCapabilities("seat-zero", "invite-one", "seat-one"),
            new RestrictedVisibilityPolicy(0),
            store);
        var specification = new GameSpecification(
            "rhino", ["captain_marvel", "spider_man"], [], Seed: 7);

        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", "actions", specification));
        EngineResponse attached = host.Exchange(EngineRequest.AttachGame(
            "attach", "actions", Assert.Single(opened.Invitations!).Invitation));
        EngineResponse afterZero = host.Exchange(EngineRequest.ResolveGame(
            "zero-mulligan", "actions", RequiredCapability(opened), TakeOnly(opened)));
        EngineResponse oneMulligan = host.Exchange(EngineRequest.SyncGame(
            "one-mulligan-menu", "actions", RequiredCapability(attached)));
        EngineResponse afterOne = host.Exchange(EngineRequest.ResolveGame(
            "one-mulligan", "actions", RequiredCapability(attached),
            TakeOnly(oneMulligan), oneMulligan.Revision));
        int frontierBeforeAction = Assert.Single(store.Load()).Save.EditFrontier;

        Assert.Null(afterZero.Prompt);
        Assert.Equal(1, afterOne.Prompt?.Player);
        EngineResponse active = host.Exchange(EngineRequest.SyncGame(
            "active", "actions", RequiredCapability(opened)));
        EngineResponse other = host.Exchange(EngineRequest.SyncGame(
            "other", "actions", RequiredCapability(attached)));

        Prompt activeMenu = Assert.IsType<Prompt>(active.Prompt);
        Prompt otherMenu = Assert.IsType<Prompt>(other.Prompt);
        Assert.Equal(0, activeMenu.Player);
        Assert.DoesNotContain(
            activeMenu.Affordances, option => option.AnchorPlayer == 1);
        Assert.Equal(1, otherMenu.Player);
        Assert.False(otherMenu.Cancellable);
        Assert.All(otherMenu.Affordances, option =>
        {
            Assert.Equal(Game.ActionVerb, option.Verb);
            Assert.Equal(1, option.AnchorPlayer);
        });
        Affordance auntMay = Assert.Single(
            otherMenu.Affordances,
            option => option.AnchorId == factory.AuntMay.ObjectId);

        Affordance activeOnly = Assert.Single(
            activeMenu.Affordances, option => option.Verb == Game.ChangeForm);
        EngineResponse forgedTurnOption = host.Exchange(EngineRequest.ResolveGame(
            "forged-basic", "actions", RequiredCapability(attached),
            new EngineDecision(activeOnly.Id, []), active.Revision));
        EngineResponse stolenAction = host.Exchange(EngineRequest.ResolveGame(
            "stolen-action", "actions", RequiredCapability(opened),
            new EngineDecision(auntMay.Id, []), active.Revision));
        Assert.Equal("not_your_turn", forgedTurnOption.Error?.Code);
        Assert.Equal("not_your_turn", stolenAction.Error?.Code);

        EngineResponse acted = host.Exchange(EngineRequest.ResolveGame(
            "act", "actions", RequiredCapability(attached),
            new EngineDecision(auntMay.Id, []), other.Revision));
        Assert.Null(acted.Error);
        Assert.False(factory.AuntMay.Ready);
        Assert.Equal(1, factory.Game.State.Seats[1].IdentityCard.Damage);
        SessionSave save = Assert.Single(store.Load()).Save;
        Assert.Equal(frontierBeforeAction, save.EditFrontier);
        Assert.Empty(save.Units[^1].Exposures);

        EngineResponse staleActive = host.Exchange(EngineRequest.ResolveGame(
            "stale-active", "actions", RequiredCapability(opened),
            new EngineDecision(activeOnly.Id, []), active.Revision));
        Assert.Equal("stale_decision", staleActive.Error?.Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ACompetingOffTurnActionIsStaleEvenWhenTheWinningCommandRemovesIt(
        bool winningCommandEndsGame)
    {
        var store = new MemorySessionStore();
        var factory = new VanishingOffTurnActionFactory(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            winningCommandEndsGame);
        var host = new EngineHost(
            factory,
            new SequenceCapabilities("seat-zero", "invite-one", "seat-one"),
            new RestrictedVisibilityPolicy(0),
            store);
        var specification = new GameSpecification(
            "rhino", ["captain_marvel", "spider_man"], [], Seed: 7);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", "race", specification));
        EngineResponse attached = host.Exchange(EngineRequest.AttachGame(
            "attach", "race", Assert.Single(opened.Invitations!).Invitation));
        _ = host.Exchange(EngineRequest.ResolveGame(
            "zero-mulligan", "race", RequiredCapability(opened), TakeOnly(opened)));
        EngineResponse oneMulligan = host.Exchange(EngineRequest.SyncGame(
            "one-mulligan-menu", "race", RequiredCapability(attached)));
        _ = host.Exchange(EngineRequest.ResolveGame(
            "one-mulligan", "race", RequiredCapability(attached),
            TakeOnly(oneMulligan), oneMulligan.Revision));

        EngineResponse active = host.Exchange(EngineRequest.SyncGame(
            "active", "race", RequiredCapability(opened)));
        EngineResponse offTurn = host.Exchange(EngineRequest.SyncGame(
            "off-turn", "race", RequiredCapability(attached)));
        Affordance healing = Assert.Single(
            active.Prompt!.Affordances,
            option => option.AnchorId == factory.ActiveSource.ObjectId);
        Affordance disappearing = Assert.Single(
            offTurn.Prompt!.Affordances,
            option => option.AnchorId == factory.OffTurnSource.ObjectId);
        Assert.Equal(active.Revision, offTurn.Revision);

        EngineResponse won = host.Exchange(EngineRequest.ResolveGame(
            "winner", "race", RequiredCapability(opened),
            new EngineDecision(healing.Id, []), active.Revision));
        EngineResponse lost = host.Exchange(EngineRequest.ResolveGame(
            "loser", "race", RequiredCapability(attached),
            new EngineDecision(disappearing.Id, []), offTurn.Revision));

        Assert.Null(won.Error);
        Assert.Equal(0, factory.Game.State.Seats[1].IdentityCard.Damage);
        Assert.Equal(winningCommandEndsGame, won.Prompt is null);
        Assert.Equal("stale_decision", lost.Error?.Code);
        EngineResponse current = host.Exchange(EngineRequest.SyncGame(
            "current", "race", RequiredCapability(attached)));
        Assert.Equal(won.Revision, current.Revision);

        if (winningCommandEndsGame)
        {
            var restarted = new EngineHost(
                new VanishingOffTurnActionFactory(
                    DatasetGameFactory.Load(RepositoryPaths.Root),
                    winningCommandEndsGame: true),
                visibility: new RestrictedVisibilityPolicy(0),
                store: store);
            EngineResponse restored = restarted.Exchange(EngineRequest.SyncGame(
                "restored", "race", RequiredCapability(attached)));
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
        var first = new EngineHost(
            factory,
            new SequenceCapabilities("restart-owner"),
            store: store);
        EngineResponse opened = first.Exchange(EngineRequest.OpenGame(
            "open",
            "restart-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse advanced = first.Exchange(EngineRequest.ResolveGame(
            "resolve",
            "restart-table",
            opened.Capability!,
            TakeOnly(opened),
            opened.Revision));

        var restarted = new EngineHost(factory, store: store);
        EngineResponse restored = restarted.Exchange(EngineRequest.SyncGame(
            "sync", "restart-table", opened.Capability!));

        Assert.Null(advanced.Error);
        Assert.Null(restored.Error);
        Assert.Equal(advanced.Revision, restored.Revision);
        Assert.Equal(
            EngineJson.Write(advanced with { RequestId = "same", Events = [] }),
            EngineJson.Write(restored with { RequestId = "same" }));
        StoredSession persisted = Assert.Single(store.Load());
        string save = SessionSaveJson.Write(persisted.Save);
        Assert.DoesNotContain("restart-owner", save, StringComparison.Ordinal);
        Assert.DoesNotContain("capability", save, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(64, persisted.Save.Compatibility.CardsSha256.Length);
        Assert.Equal(1, persisted.Save.Revision);
        Assert.Single(persisted.Save.Units);
    }

    [Fact]
    public void AFailedSaveCommitDoesNotAdvanceOrInvalidateTheLiveGame()
    {
        var inner = new MemorySessionStore();
        var store = new FailingSessionStore(inner, failAtCommit: 2);
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("atomic-owner"),
            store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open",
            "atomic-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));

        EngineResponse failed = host.Exchange(EngineRequest.ResolveGame(
            "resolve",
            "atomic-table",
            opened.Capability!,
            TakeOnly(opened),
            opened.Revision));
        EngineResponse current = host.Exchange(EngineRequest.SyncGame(
            "sync", "atomic-table", opened.Capability!));

        Assert.Equal("save_failed", failed.Error?.Code);
        Assert.Null(current.Error);
        Assert.Equal(opened.Revision, current.Revision);
        Assert.Equal(
            EngineJson.Write(opened with
            {
                RequestId = "same",
                Capability = null,
                Invitations = null,
                Events = [],
            }),
            EngineJson.Write(current with { RequestId = "same" }));
        Assert.Equal(0, Assert.Single(inner.Load()).Save.Revision);
    }

    [Fact]
    public void AFailedOpenCommitReturnsABoundedStorageFailureWithoutPublishingAGame()
    {
        var inner = new MemorySessionStore();
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("unpublished-owner"),
            store: new FailingSessionStore(inner, failAtCommit: 1));

        EngineResponse failed = host.Exchange(EngineRequest.OpenGame(
            "open",
            "unpublished-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse absent = host.Exchange(EngineRequest.SyncGame(
            "sync", "unpublished-table", "unpublished-owner"));

        Assert.Equal("save_failed", failed.Error?.Code);
        Assert.Null(failed.Capability);
        Assert.Equal("session_not_found", absent.Error?.Code);
        Assert.Empty(inner.Load());
    }

    [Theory]
    [InlineData("rng")]
    [InlineData("digest")]
    [InlineData("prompt")]
    [InlineData("compatibility")]
    public void RestartQuarantinesTheFirstDivergentAuthorityRecord(string field)
    {
        var store = new MemorySessionStore();
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var host = new EngineHost(
            factory,
            new SequenceCapabilities("divergence-owner"),
            store: store);
        _ = host.Exchange(EngineRequest.OpenGame(
            "open",
            "divergence-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        StoredSession stored = Assert.Single(store.Load());
        SessionSave changed = field switch
        {
            "rng" => stored.Save with
            {
                Initial = stored.Save.Initial with
                {
                    RngWords = stored.Save.Initial.RngWords + 1,
                },
            },
            "digest" => stored.Save with
            {
                Initial = stored.Save.Initial with { StateDigest = "changed" },
            },
            "prompt" => stored.Save with { CurrentPrompt = null },
            "compatibility" => stored.Save with
            {
                Compatibility = stored.Save.Compatibility with
                {
                    ReplayContract = "future-contract",
                },
            },
            _ => throw new InvalidOperationException(field),
        };

        AssertQuarantined(
            factory,
            stored with { Save = changed },
            "divergence-table",
            "divergence-owner");
    }

    [Theory]
    [InlineData("event")]
    [InlineData("rng")]
    [InlineData("state")]
    public void RestartQuarantinesTheFirstDivergentCommittedDecision(string field)
    {
        var store = new MemorySessionStore();
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var host = new EngineHost(
            factory,
            new SequenceCapabilities("step-owner"),
            store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open",
            "step-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        _ = host.Exchange(EngineRequest.ResolveGame(
            "resolve",
            "step-table",
            opened.Capability!,
            TakeOnly(opened),
            opened.Revision));
        StoredSession stored = Assert.Single(store.Load());
        JournalUnit unit = Assert.Single(stored.Save.Units);
        JournalStep step = Assert.Single(unit.Decisions);
        JournalStep changedStep = field switch
        {
            "event" => step with
            {
                Events = [JournalJson.Event(new FieldSet(0, "damage", 0, 1))],
            },
            "rng" => step with { RngWords = step.RngWords + 1 },
            "state" => step with { StateFingerprint = "changed" },
            _ => throw new InvalidOperationException(field),
        };
        SessionSave changed = stored.Save with
        {
            Units = [unit with { Decisions = [changedStep] }],
        };

        AssertQuarantined(
            factory,
            stored with { Save = changed },
            "step-table",
            "step-owner");
    }

    [Fact]
    public void DrawingDuringAUnitAdvancesItsPersistedInformationFrontier()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("frontier-owner"),
            store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open",
            "frontier-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        Prompt prompt = Assert.IsType<Prompt>(opened.Prompt);
        Affordance mulligan = Assert.Single(prompt.Affordances);
        int card = Assert.IsType<TargetRequest>(mulligan.Targets).Legal[0];

        EngineResponse resolved = host.Exchange(EngineRequest.ResolveGame(
            "mulligan",
            "frontier-table",
            opened.Capability!,
            new EngineDecision(mulligan.Id, [card]),
            opened.Revision));

        Assert.Null(resolved.Error);
        SessionSave save = Assert.Single(store.Load()).Save;
        Assert.Equal(1, save.EditFrontier);
        JournalUnit unit = Assert.Single(save.Units);
        InformationExposure draw = Assert.Single(
            unit.Exposures,
            exposure => exposure.Reason == InformationFrontier.Draw);
        Assert.Equal([0], draw.Seats);
        StoredSession stored = Assert.Single(store.Load());
        IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        AssertQuarantined(
            factory,
            new StoredSession(save with { EditFrontier = 0 }, stored.Authorities),
            "frontier-table",
            "frontier-owner");
        AssertQuarantined(
            factory,
            stored with
            {
                Save = save with
                {
                    Units =
                    [
                        unit with
                        {
                            Exposures =
                            [
                                new InformationExposure(
                                    InformationFrontier.Search,
                                    [0]),
                            ],
                        },
                    ],
                },
            },
            "frontier-table",
            "frontier-owner");

        var migrationStore = new MigrationSessionStore(stored with
        {
            Save = save with
            {
                Schema = 1,
                EditFrontier = 0,
                Units = [unit with { Exposures = [] }],
            },
        });
        var restarted = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            store: migrationStore);

        StoredSession migrated = Assert.Single(migrationStore.Load());
        Assert.Equal(1, migrationStore.Commits);
        Assert.Equal(SessionSave.CurrentSchema, migrated.Save.Schema);
        Assert.Equal(1, migrated.Save.EditFrontier);
        Assert.Equal(
            InformationFrontier.Draw,
            Assert.Single(Assert.Single(migrated.Save.Units).Exposures).Reason);
        Assert.Null(restarted.Exchange(EngineRequest.SyncGame(
            "sync", "frontier-table", opened.Capability!)).Error);
    }

    [Fact]
    public void ChoosingNoMulliganCardsBeforeAnotherPlayersPromptRevealsNothingNew()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("seat-zero", "invite-one", "seat-one"),
            store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open",
            "zero-mulligan-frontier",
            new GameSpecification(
                "rhino", ["captain_marvel", "spider_man"], [], Seed: 73)));

        EngineResponse resolved = host.Exchange(EngineRequest.ResolveGame(
            "keep",
            "zero-mulligan-frontier",
            RequiredCapability(opened),
            TakeOnly(opened),
            opened.Revision));

        Assert.Null(resolved.Error);
        SessionSave save = Assert.Single(store.Load()).Save;
        Assert.Equal(0, save.EditFrontier);
        Assert.Empty(Assert.Single(save.Units).Exposures);
        Assert.Equal(1, resolved.Prompt?.Player);
        Assert.True(Assert.Single(resolved.Prompt!.Affordances).Targets?.IsSearch);
    }

    [Fact]
    public void OneIncompatibleSessionIsQuarantinedWithoutHidingAHealthySession()
    {
        var store = new MemorySessionStore();
        IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(
            factory,
            new SequenceCapabilities("bad-owner", "healthy-owner"),
            store: store);
        _ = first.Exchange(EngineRequest.OpenGame(
            "bad-open",
            "bad-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        _ = first.Exchange(EngineRequest.OpenGame(
            "healthy-open",
            "healthy-table",
            new GameSpecification("rhino", ["captain_marvel"], [], Seed: 74)));
        StoredSession[] saved = [.. store.Load()];
        StoredSession bad = saved.Single(session => session.Save.Session.Label == "bad-table");
        StoredSession healthy = saved.Single(
            session => session.Save.Session.Label == "healthy-table");
        bad = bad with
        {
            Save = bad.Save with
            {
                Compatibility = bad.Save.Compatibility with
                {
                    ReplayContract = "future-contract",
                },
            },
        };

        var restarted = new EngineHost(
            factory,
            store: new FixedSessionStore(bad, healthy));

        Assert.Equal(
            "session_not_found",
            restarted.Exchange(EngineRequest.SyncGame(
                "bad", "bad-table", "bad-owner")).Error?.Code);
        Assert.Null(restarted.Exchange(EngineRequest.SyncGame(
            "healthy", "healthy-table", "healthy-owner")).Error);
    }

    [Fact]
    public void APartlyReadAuthorityListPublishesNoneOfTheQuarantinedSession()
    {
        var store = new MemorySessionStore();
        IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(
            factory,
            new SequenceCapabilities("partial-owner"),
            store: store);
        EngineResponse opened = first.Exchange(EngineRequest.OpenGame(
            "open",
            "partial-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        StoredSession stored = Assert.Single(store.Load());
        stored = stored with
        {
            Authorities =
            [
                .. stored.Authorities,
                new StoredAuthority(new string('f', 64), [1], Owner: false, Invitation: false),
            ],
        };

        var restarted = new EngineHost(
            factory,
            store: new FixedSessionStore(stored));

        Assert.Equal(
            "session_not_found",
            restarted.Exchange(EngineRequest.SyncGame(
                "sync", "partial-table", RequiredCapability(opened))).Error?.Code);
    }

    [Fact]
    public void EveryGameplayCommitStampsTheCurrentVersionAndBlocksADowngrade()
    {
        var store = new MemorySessionStore();
        IDurableGameFactory inner = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(
            new VersionedFactory(inner, "0.1.0"),
            new SequenceCapabilities("version-owner"),
            store: store);
        EngineResponse opened = first.Exchange(EngineRequest.OpenGame(
            "open",
            "version-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));

        var upgraded = new EngineHost(
            new VersionedFactory(inner, "0.1.1-preview.2"),
            store: store);
        EngineResponse current = upgraded.Exchange(EngineRequest.SyncGame(
            "sync", "version-table", "version-owner"));
        EngineResponse resolved = upgraded.Exchange(EngineRequest.ResolveGame(
            "resolve",
            "version-table",
            "version-owner",
            TakeOnly(current),
            current.Revision));

        Assert.Null(resolved.Error);
        Assert.Equal(
            "0.1.1-preview.2",
            Assert.Single(store.Load()).Save.Compatibility.Application);
        var downgraded = new EngineHost(
            new VersionedFactory(inner, "0.1.1-preview.1"),
            store: store);
        Assert.Equal(
            "session_not_found",
            downgraded.Exchange(EngineRequest.SyncGame(
                "downgrade", "version-table", "version-owner")).Error?.Code);
    }

    [Fact]
    public void FileReadingAndCheatOperationsHaveNoServedSurface()
    {
        var factory = new UnusedFactory();
        var host = new EngineHost(factory);

        EngineResponse read = host.Exchange(new EngineRequest(
            EngineProtocol.Version, "read", "read_file", "game"));
        EngineResponse cheat = host.Exchange(new EngineRequest(
            EngineProtocol.Version, "cheat", "cheat", "game"));

        Assert.Equal("invalid_request", read.Error?.Code);
        Assert.Equal("invalid_request", cheat.Error?.Code);
        Assert.Equal(0, factory.Calls);
        Assert.DoesNotContain(
            typeof(EngineRequest).GetProperties(),
            property => property.Name.Contains("File", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Contains("Cheat", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<Marvel.View.CardDescriptor> Hand(
        EngineResponse response, int seat) =>
        Assert.Single(
            Assert.IsType<WorldDescriptor>(response.World).Areas,
            area => area.Zone == "HandsArea" && area.Owner == seat).Cards;

    private static string RequiredCapability(EngineResponse response) =>
        Assert.IsType<string>(response.Capability);

    private static EngineDecision TakeOnly(EngineResponse response) =>
        new(Assert.Single(Assert.IsType<Marvel.Rules.Prompts.Prompt>(response.Prompt)
            .Affordances).Id, []);

    private static void AssertQuarantined(
        IDurableGameFactory factory,
        StoredSession stored,
        string gameId,
        string capability)
    {
        var restarted = new EngineHost(
            factory,
            store: new FixedSessionStore(stored));
        Assert.Equal(
            "session_not_found",
            restarted.Exchange(EngineRequest.SyncGame(
                "quarantined", gameId, capability)).Error?.Code);
    }

    private static EngineDecision PayFirstPlayableCard(Prompt prompt)
    {
        foreach (Affordance option in prompt.Affordances.Where(option =>
                     string.Equals(option.Verb, "Play", StringComparison.Ordinal)))
        {
            IReadOnlyList<int> targets = option.Targets is null
                ? []
                : option.Targets.Legal.Take(option.Targets.Min).ToList();
            foreach (CostOption cost in option.CostOptions.Where(cost =>
                         cost.Target == 0
                         || cost.Target == option.AnchorId
                         || targets.Contains(cost.Target)))
            {
                var values = cost.VariableRequests.ToDictionary(
                    request => request.Name,
                    request => request.Min,
                    StringComparer.Ordinal);
                int[] generators = cost.Generators
                    .Select(generator => generator.Effect)
                    .Distinct()
                    .ToArray();
                IReadOnlyList<ResourceAllocation>? allocations =
                    ResourcePayment.Allocate(cost, generators, values);
                if (allocations is not null
                    && ResourcePayment.Allows(cost, generators, values, allocations))
                {
                    return new EngineDecision(
                        option.Id, targets, generators, values, allocations);
                }
            }
        }

        throw new Xunit.Sdk.XunitException("the deterministic hand has no payable card play");
    }

    private static EngineDecision PayCard(Prompt prompt, int anchor)
    {
        Affordance option = Assert.Single(prompt.Affordances, option =>
            string.Equals(option.Verb, "Play", StringComparison.Ordinal)
            && option.AnchorId == anchor);
        IReadOnlyList<int> targets = option.Targets is null
            ? []
            : option.Targets.Legal.Take(option.Targets.Min).ToList();
        foreach (CostOption cost in option.CostOptions.Where(cost =>
                     cost.Target == 0
                     || cost.Target == option.AnchorId
                     || targets.Contains(cost.Target)))
        {
            var values = cost.VariableRequests.ToDictionary(
                request => request.Name,
                request => request.Min,
                StringComparer.Ordinal);
            int[] generators = cost.Generators
                .Select(generator => generator.Effect)
                .Distinct()
                .ToArray();
            IReadOnlyList<ResourceAllocation>? allocations =
                ResourcePayment.Allocate(cost, generators, values);
            if (allocations is not null
                && ResourcePayment.Allows(cost, generators, values, allocations))
            {
                return new EngineDecision(
                    option.Id, targets, generators, values, allocations);
            }
        }

        throw new Xunit.Sdk.XunitException("the requested card has no legal payment");
    }

    private sealed class UnusedFactory : IGameFactory
    {
        public int Calls { get; private set; }

        public OpenedGame Create(GameSpecification specification)
        {
            Calls++;
            throw new InvalidOperationException("should not be called");
        }
    }

    private sealed class FailingSessionStore(ISessionStore inner, int failAtCommit)
        : ISessionStore
    {
        private int commits;

        public IReadOnlyList<StoredSession> Load() => inner.Load();

        public void Commit(StoredSession session)
        {
            commits++;
            if (commits == failAtCommit)
            {
                throw new IOException("simulated interrupted write");
            }

            inner.Commit(session);
        }
    }

    private sealed class FixedSessionStore(params StoredSession[] sessions) : ISessionStore
    {
        public IReadOnlyList<StoredSession> Load() => sessions;

        public void Commit(StoredSession session) =>
            throw new InvalidOperationException("not used");
    }

    private sealed class VersionedFactory(IDurableGameFactory inner, string application)
        : IDurableGameFactory
    {
        public SessionCompatibility Compatibility => inner.Compatibility with
        {
            Application = application,
        };

        public OpenedGame Create(GameSpecification specification) => inner.Create(specification);
    }

    private sealed class MigrationSessionStore(StoredSession session) : ISessionStore
    {
        private StoredSession session = session;

        public int Commits { get; private set; }

        public IReadOnlyList<StoredSession> Load() => [session];

        public void Commit(StoredSession replacement)
        {
            SessionSaveJson.Validate(replacement.Save);
            session = replacement;
            Commits++;
        }
    }

    private sealed class SequenceCapabilities(params string[] capabilities)
        : ISessionCapabilityIssuer
    {
        private readonly Queue<string> capabilities = new(capabilities);

        public string Issue() => capabilities.Dequeue();
    }

    private sealed class FailingFactory(string message) : IGameFactory
    {
        public OpenedGame Create(GameSpecification specification) =>
            throw new InvalidOperationException(message);
    }

    private sealed class OffTurnActionFactory(IDurableGameFactory inner) : IDurableGameFactory
    {
        public SessionCompatibility Compatibility => inner.Compatibility;

        public Card AuntMay { get; private set; } = null!;

        public Game Game { get; private set; } = null!;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            Game = opened.Game;
            var world = Game.State;
            AuntMay = world.CreateCard(
                "01006",
                world.AreaOf(
                    DeckType.SupportsArea,
                    PlayArea.Of(1),
                    cardOwner: 1));
            world.Seats[1].IdentityCard.TakeDamage(5);
            return opened;
        }
    }

    private sealed class InHandActionFactory(IDurableGameFactory inner) : IDurableGameFactory
    {
        public SessionCompatibility Compatibility => inner.Compatibility;

        public Card AuntMay { get; private set; } = null!;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            World world = opened.Game.State;
            AuntMay = world.CreateCard("01006", world.Seats[0].Hand);
            world.Seats[0].IdentityCard.TakeDamage(5);
            return opened;
        }
    }

    private sealed class VanishingOffTurnActionFactory(
        IDurableGameFactory inner,
        bool winningCommandEndsGame) : IDurableGameFactory
    {
        public SessionCompatibility Compatibility => inner.Compatibility;

        public Card ActiveSource { get; private set; } = null!;

        public Card OffTurnSource { get; private set; } = null!;

        public Game Game { get; private set; } = null!;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            World world = opened.Game.State;
            ActiveSource = world.CreateCard(
                "01006",
                world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            OffTurnSource = world.CreateCard(
                "01006",
                world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            world.Seats[1].IdentityCard.TakeDamage(1);
            Game = Game.Begin(
                world,
                world.Facts,
                new CrossSeatHealingActions(
                    ActiveSource,
                    OffTurnSource,
                    winningCommandEndsGame));
            return opened with { Game = Game };
        }
    }

    private sealed class ExcessHealingFactory(IDurableGameFactory inner)
        : IDurableGameFactory
    {
        public SessionCompatibility Compatibility => inner.Compatibility;

        public Card AttackSource { get; private set; } = null!;

        public Card HealSource { get; private set; } = null!;

        public Card UpgradeSource { get; private set; } = null!;

        public Card Victim { get; private set; } = null!;

        public Card ExcessMeter { get; private set; } = null!;

        public Game Game { get; private set; } = null!;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            World world = opened.Game.State;
            Area supports = world.AreaOf(
                DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0);
            AttackSource = world.CreateCard("01006", supports);
            HealSource = world.CreateCard("01006", supports);
            UpgradeSource = world.CreateCard("01006", supports);
            Area minions = world.AreaOf(
                DeckType.EngagedEnemiesArea, PlayArea.Of(0), cardOwner: World.Scenario);
            Victim = world.CreateCard("01101", minions);
            ExcessMeter = world.CreateCard("01184", minions);
            world.Seats[0].IdentityCard.TakeDamage(5);
            Game = Game.Begin(
                world,
                world.Facts,
                new ExcessHealingActions(
                    AttackSource,
                    HealSource,
                    UpgradeSource,
                    Victim,
                    ExcessMeter));
            return opened with { Game = Game };
        }
    }

    private sealed class ExcessHealingActions(
        Card attack,
        Card heal,
        Card upgrade,
        Card victim,
        Card excessMeter) : NoCardAbilities
    {
        public override IReadOnlyList<PendingAbility> Actions(World world, int player)
        {
            if (player != 0)
            {
                return [];
            }

            var actions = new List<PendingAbility>();
            if (attack.Ready && DeckTypes.IsInPlay(victim.Area.Type))
            {
                actions.Add(new PendingAbility(attack.ObjectId, AbilityType.Action, player));
            }

            if (heal.Ready && excessMeter.Damage > 0)
            {
                actions.Add(new PendingAbility(heal.ObjectId, AbilityType.Action, player));
            }

            if (upgrade.Ready)
            {
                actions.Add(new PendingAbility(upgrade.ObjectId, AbilityType.Action, player));
            }

            return actions;
        }

        public override Affordance Describe(World world, PendingAbility ability) =>
            new(
                ability.Card,
                Game.ActionVerb,
                ability.Card,
                ability.Player,
                ability.Card == attack.ObjectId
                    ? "Attack and record excess"
                    : ability.Card == heal.ObjectId
                        ? "Heal for recorded excess"
                        : "Increase the later attack");

        public override IReadOnlyList<GameEvent> Act(
            World world,
            PendingAbility ability,
            IReadOnlyList<int> paying,
            IReadOnlyList<int> chosen,
            IReadOnlyDictionary<string, long>? values = null,
            IReadOnlyList<ResourceAllocation>? allocations = null)
        {
            Card source = world.Cards[ability.Card];
            source.Exhaust();
            var events = new List<GameEvent>
            {
                new FieldSet(source.ObjectId, "is_exhaust", 0, 1),
            };
            if (source == upgrade)
            {
                return events;
            }

            if (source == attack)
            {
                long amount = upgrade.Ready ? 4 : 6;
                Damage.AttackResult result = Damage.Attack(
                    world,
                    world.Facts,
                    world.Seats[0].IdentityCard,
                    victim,
                    amount,
                    "rewrite-example",
                    Game.ActionVerb,
                    events,
                    retaliate: false);
                long before = Damage.Health(world, world.Facts, excessMeter)
                    - excessMeter.Damage;
                excessMeter.TakeDamage(result.Excess);
                events.Add(new FieldSet(
                    excessMeter.ObjectId,
                    "health",
                    before,
                    before - result.Excess));
                return events;
            }

            Damage.Heal(
                world,
                world.Facts,
                world.Seats[0].IdentityCard,
                excessMeter.Damage,
                "rewrite-example",
                Game.ActionVerb,
                events);
            return events;
        }
    }

    private sealed class CrossSeatHealingActions(
        Card active,
        Card offTurn,
        bool winningCommandEndsGame)
        : NoCardAbilities
    {
        public override IReadOnlyList<PendingAbility> Actions(World world, int player) =>
            player switch
            {
                0 => [new PendingAbility(active.ObjectId, AbilityType.Action, player)],
                1 when world.Seats[1].IdentityCard.Damage > 0 =>
                    [new PendingAbility(offTurn.ObjectId, AbilityType.Action, player)],
                _ => [],
            };

        public override Affordance Describe(World world, PendingAbility ability) =>
            new(
                ability.Card,
                Game.ActionVerb,
                ability.Card,
                ability.Player,
                ability.Player == 0 ? "Heal player 2" : "Use player 2 action");

        public override IReadOnlyList<GameEvent> Act(
            World world,
            PendingAbility ability,
            IReadOnlyList<int> paying,
            IReadOnlyList<int> chosen,
            IReadOnlyDictionary<string, long>? values = null,
            IReadOnlyList<ResourceAllocation>? allocations = null)
        {
            var events = new List<GameEvent>();
            if (ability.Player == 0)
            {
                Damage.Heal(
                    world,
                    world.Facts,
                    world.Seats[1].IdentityCard,
                    amount: 1,
                    trigger: "test",
                    verb: Game.ActionVerb,
                    events);
                if (winningCommandEndsGame)
                {
                    world.Finish(Outcome.PlayersWin);
                }
            }

            return events;
        }
    }

    private sealed class HostileVisibilityPolicy(IReadOnlyList<SeatScope>? grants)
        : IVisibilityPolicy
    {
        public ViewScope Authorize(ViewerClaim? claim, int players) =>
            new RestrictedVisibilityPolicy(0).Authorize(null, players);

        public IReadOnlyList<SeatScope> AdditionalScopes(
            ViewerClaim? claim, int players) => grants!;
    }

    private sealed class NullPrimaryVisibilityPolicy : IVisibilityPolicy
    {
        public ViewScope Authorize(ViewerClaim? claim, int players) => null!;

        public IReadOnlyList<SeatScope> AdditionalScopes(
            ViewerClaim? claim, int players) => [];
    }

    private sealed class ChangingSeatGrants(
        IReadOnlyList<SeatScope> first,
        IReadOnlyList<SeatScope> later) : IReadOnlyList<SeatScope>
    {
        public int Enumerations { get; private set; }

        public int Count => first.Count;

        public SeatScope this[int index] => first[index];

        public IEnumerator<SeatScope> GetEnumerator()
        {
            Enumerations++;
            return (Enumerations == 1 ? first : later).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
