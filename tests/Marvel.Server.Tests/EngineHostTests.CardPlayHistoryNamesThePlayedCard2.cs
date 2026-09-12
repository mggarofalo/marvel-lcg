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
public sealed class EngineHostCardPlayHistoryNamesThePlayedCardTests : EngineHostTestBase
{
    [Fact]
    public void CardPlayHistoryNamesThePlayedCardAndEveryResourceGenerator()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("history-owner"));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "narrative-history", new GameSpecification("rhino", ["spider_man"], [], Seed: 1)));
        int[] mulligan = Hand(opened, 0).Where(card => card.Face?.Title is "Avengers Mansion" or "Aunt May" or "Swinging Web Kick").Select(card => card.Id!.Value).ToArray();
        EngineResponse turn = host.Exchange(EngineRequest.ResolveGame("mulligan", "narrative-history", RequiredCapability(opened), new EngineDecision(Assert.Single(opened.Prompt!.Affordances).Id, mulligan), opened.Revision));
        CardDescriptor webShooter = Hand(turn, 0).First(card => card.Face?.Title == "Web-Shooter");
        CardDescriptor peter = Assert.Single(turn.World!.Areas.SelectMany(area => area.Cards.Concat(area.Removed)), card => card.Face?.Title == "Peter Parker");
        Affordance play = Assert.Single(turn.Prompt!.Affordances, option => option.Verb == "Play" && option.AnchorId == webShooter.Id);
        string capability = RequiredCapability(opened);
        var composer = new DecisionComposer(turn.Prompt);
        composer.SelectAffordance(play.Id);
        composer.ToggleResource(peter.Id!.Value);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out string? error), error);
        EngineRequest request = EngineRequest.ResolveGame("play", "narrative-history", capability, decision!, turn.Revision);
        EngineResponse played = host.Exchange(request);
        EngineResponse synchronized = host.Exchange(EngineRequest.SyncGame("sync", "narrative-history", RequiredCapability(opened)));
        Assert.Null(played.Error);
        HistoryEntryDescriptor entry = Assert.Single(played.History!.Entries, item => item.Cursor == 1);
        Assert.Equal("Spider-Man played Web-Shooter, generating resources from Scientist.", entry.Summary);
        Assert.Equal(played.History.Entries, synchronized.History!.Entries);
    }

    [Fact]
    public void EventActionHistoryIsACompletedCardPlayRatherThanAnAbilityUse()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("event-history-owner"));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "event-history", new GameSpecification("rhino", ["spider_man"], [], Seed: 1)));
        string capability = RequiredCapability(opened);
        EngineResponse turn = host.Exchange(EngineRequest.ResolveGame("mulligan", "event-history", capability, new EngineDecision(Assert.Single(opened.Prompt!.Affordances).Id, []), opened.Revision));
        Affordance changeForm = Assert.Single(turn.Prompt!.Affordances, option => option.Verb == Game.ChangeForm);
        EngineResponse hero = host.Exchange(EngineRequest.ResolveGame("form", "event-history", capability, new EngineDecision(changeForm.Id, []), turn.Revision));
        CardDescriptor kick = Hand(hero, 0).First(card => card.Face?.Title == "Swinging Web Kick");
        Affordance play = Assert.Single(hero.Prompt!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == kick.Id);
        var composer = new DecisionComposer(hero.Prompt);
        composer.SelectAffordance(play.Id);
        if (play.Targets is { } targets)
        {
            composer.SelectTargets(targets.Legal.Take(targets.Min));
        }

        foreach (ResourceSource source in play.CostOptions.SelectMany(cost => cost.Generators).DistinctBy(source => source.Effect))
        {
            composer.ToggleResource(source.Effect);
            if (composer.TryBuild(out _, out _))
            {
                break;
            }
        }

        Assert.True(composer.TryBuild(out EngineDecision? decision, out string? error), error);
        EngineResponse played = host.Exchange(EngineRequest.ResolveGame("play", "event-history", capability, decision!, hero.Revision));
        Assert.True(played.History!.ActionOpen);
        Assert.DoesNotContain(played.History.Entries, entry => entry.Cursor == 2);
        while (played.History!.ActionOpen)
        {
            EngineDecision continuation = played.Prompt!.Cancellable ? EngineDecision.Decline : TakeOnly(played);
            played = host.Exchange(EngineRequest.ResolveGame("finish-play", "event-history", capability, continuation, played.Revision));
        }

        HistoryEntryDescriptor entry = Assert.Single(played.History!.Entries, item => item.Cursor == 2);
        Assert.StartsWith("Spider-Man played Swinging Web Kick, generating resources from ", entry.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain(" used ", entry.Summary, StringComparison.Ordinal);
        Assert.Empty(entry.Details);
    }

    [Fact]
    public void DecliningTheTurnProducesAnEndTurnHistoryAction()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("end-turn-history-owner"));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "end-turn-history", new GameSpecification("rhino", ["spider_man"], [], Seed: 1)));
        string capability = RequiredCapability(opened);
        EngineResponse turn = host.Exchange(EngineRequest.ResolveGame("mulligan", "end-turn-history", capability, new EngineDecision(Assert.Single(opened.Prompt!.Affordances).Id, []), opened.Revision));
        EngineResponse ended = host.Exchange(EngineRequest.ResolveGame("end", "end-turn-history", capability, EngineDecision.Decline, turn.Revision));
        Assert.Equal("Spider-Man ended their turn.", Assert.Single(ended.History!.Entries, entry => entry.Cursor == 1).Summary);
    }

    [Fact]
    public void NewInformationAndAnotherSeatsHistoryExplainWhyUndoIsUnavailable()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("seat-zero", "invite-one", "seat-one"), new RestrictedVisibilityPolicy(0), store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "bounded-history", new GameSpecification("rhino", ["captain_marvel", "spider_man"], [], Seed: 73)));
        EngineResponse attached = host.Exchange(EngineRequest.AttachGame("attach", "bounded-history", Assert.Single(opened.Invitations!).Invitation));
        EngineResponse afterZero = host.Exchange(EngineRequest.ResolveGame("zero", "bounded-history", RequiredCapability(opened), TakeOnly(opened), opened.Revision));
        EngineResponse onePrompt = host.Exchange(EngineRequest.SyncGame("one-prompt", "bounded-history", RequiredCapability(attached)));
        EngineResponse afterOne = host.Exchange(EngineRequest.ResolveGame("one", "bounded-history", RequiredCapability(attached), TakeOnly(onePrompt), onePrompt.Revision));
        EngineResponse otherSeat = host.Exchange(EngineRequest.UndoGame("other-seat", "bounded-history", RequiredCapability(opened), cursor: 1, expectedRevision: afterOne.Revision));
        EngineResponse undone = host.Exchange(EngineRequest.UndoGame("own", "bounded-history", RequiredCapability(attached), cursor: 1, expectedRevision: afterOne.Revision));
        EngineResponse converged = host.Exchange(EngineRequest.SyncGame("sync", "bounded-history", RequiredCapability(opened)));
        EngineResponse otherRedo = host.Exchange(EngineRequest.RedoGame("other-redo", "bounded-history", RequiredCapability(opened), cursor: 2, expectedRevision: undone.Revision));
        Assert.Null(afterZero.Error);
        Assert.Equal([0], host.Exchange(EngineRequest.SyncGame("zero-history", "bounded-history", RequiredCapability(opened))).History!.Undo);
        Assert.Empty(onePrompt.History!.Undo);
        Assert.Equal([1], afterOne.History?.Undo);
        Assert.Equal("Captain Marvel completed an action.", afterOne.History!.Entries[0].Summary);
        Assert.Empty(afterOne.History.Entries[0].Details);
        Assert.Equal("history_authority", otherSeat.Error?.Code);
        Assert.Null(undone.Error);
        Assert.Equal(undone.Revision, converged.Revision);
        Assert.Equal("history_authority", otherRedo.Error?.Code);
        Prompt current = Assert.IsType<Prompt>(undone.Prompt);
        Affordance draw = Assert.Single(current.Affordances);
        int card = Assert.IsType<TargetRequest>(draw.Targets).Legal[0];
        EngineResponse revealed = host.Exchange(EngineRequest.ResolveGame("draw", "bounded-history", RequiredCapability(attached), new EngineDecision(draw.Id, [card]), undone.Revision));
        EngineResponse beyondFrontier = host.Exchange(EngineRequest.UndoGame("frontier", "bounded-history", RequiredCapability(attached), cursor: 1, expectedRevision: revealed.Revision));
        Assert.Null(revealed.Error);
        Assert.Equal("history_frontier", beyondFrontier.Error?.Code);
    }

    [Fact]
    public void FailedHistoryPersistenceLeavesThePriorLiveGameAuthoritative()
    {
        var memory = new MemorySessionStore();
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("atomic-owner"), store: new FailingSessionStore(memory, failAtCommit: 3));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "atomic-history", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        EngineResponse kept = host.Exchange(EngineRequest.ResolveGame("keep", "atomic-history", RequiredCapability(opened), TakeOnly(opened), opened.Revision));
        EngineResponse failed = host.Exchange(EngineRequest.UndoGame("undo", "atomic-history", RequiredCapability(opened), cursor: 0, expectedRevision: kept.Revision));
        EngineResponse current = host.Exchange(EngineRequest.SyncGame("sync", "atomic-history", RequiredCapability(opened)));
        Assert.Equal("history_failed", failed.Error?.Code);
        Assert.Equal(kept.Revision, current.Revision);
        Assert.Equal(EngineJson.Write(kept with { RequestId = "same" }), EngineJson.Write(current with { RequestId = "same" }));
        Assert.Equal(1, Assert.Single(memory.Load()).Save.Cursor);
    }

    [Fact]
    public void OpenHistoryCannotBeEditedAndForgedCompletionFailsReplay()
    {
        var store = new MemorySessionStore();
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(factory, new SequenceCapabilities("open-unit-owner"), store: store);
        EngineResponse opened = first.Exchange(EngineRequest.OpenGame("open", "open-unit-history", new GameSpecification("rhino", ["captain_marvel"], [], Seed: 73)));
        EngineResponse kept = first.Exchange(EngineRequest.ResolveGame("keep", "open-unit-history", RequiredCapability(opened), TakeOnly(opened), opened.Revision));
        Affordance changeForm = Assert.Single(kept.Prompt!.Affordances, option => string.Equals(option.Verb, Game.ChangeForm, StringComparison.Ordinal));
        EngineResponse changed = first.Exchange(EngineRequest.ResolveGame("form", "open-unit-history", RequiredCapability(opened), new EngineDecision(changeForm.Id, []), kept.Revision));
        Affordance playEighteen = Assert.Single(changed.Prompt!.Affordances, option => string.Equals(option.Verb, "Play", StringComparison.Ordinal) && option.AnchorId == 18);
        EngineResponse firstPlay = first.Exchange(EngineRequest.ResolveGame("first-play", "open-unit-history", RequiredCapability(opened), new EngineDecision(playEighteen.Id, [1], [16], Allocations: [new ResourceAllocation(16, 0, "YY")]), changed.Revision));
        Affordance mariaHill = Assert.Single(firstPlay.Prompt!.Affordances, option => string.Equals(option.Verb, "Play", StringComparison.Ordinal) && option.AnchorId == 24);
        EngineResponse responseWindow = first.Exchange(EngineRequest.ResolveGame("maria-hill", "open-unit-history", RequiredCapability(opened), new EngineDecision(mariaHill.Id, [1], [30, 26], Allocations: [new ResourceAllocation(30, 0, "B"), new ResourceAllocation(26, 0, "R"), ]), firstPlay.Revision));
        StoredSession stored = Assert.Single(store.Load());
        JournalUnit unit = stored.Save.Units[^1];
        EngineResponse refused = first.Exchange(EngineRequest.UndoGame("undo", "open-unit-history", RequiredCapability(opened), cursor: 3, expectedRevision: responseWindow.Revision));
        Assert.Null(responseWindow.Error);
        Assert.Equal("open", unit.Status);
        Assert.Empty(unit.Exposures);
        Assert.Empty(responseWindow.History!.Undo);
        Assert.Equal("history_open", refused.Error?.Code);
        store.Commit(stored with { Save = stored.Save with { Units = [..stored.Save.Units.Take(stored.Save.Units.Count - 1), unit with { Status = "complete" }, ], }, });
        var restarted = new EngineHost(factory, store: store);
        Assert.Equal("session_not_found", restarted.Exchange(EngineRequest.SyncGame("quarantined", "open-unit-history", RequiredCapability(opened))).Error?.Code);
    }

    [Fact]
    public void ADecisionForAnEarlierRevisionIsRejectedWithoutAdvancingTheGame()
    {
        var host = new EngineHost(DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root), new SequenceCapabilities("owner"));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "revision-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 7)));
        EngineDecision mulligan = TakeOnly(opened);
        EngineResponse advanced = host.Exchange(EngineRequest.ResolveGame("mulligan", "revision-table", opened.Capability!, mulligan, opened.Revision));
        EngineResponse beforeStale = host.Exchange(EngineRequest.SyncGame("before-stale", "revision-table", opened.Capability!));
        EngineResponse stale = host.Exchange(EngineRequest.ResolveGame("stale", "revision-table", opened.Capability!, mulligan, opened.Revision));
        EngineResponse afterStale = host.Exchange(EngineRequest.SyncGame("after-stale", "revision-table", opened.Capability!));
        Assert.Null(advanced.Error);
        Assert.Equal(opened.Revision + 1, advanced.Revision);
        Assert.Equal("stale_decision", stale.Error?.Code);
        Assert.Empty(stale.Events);
        Assert.Equal(beforeStale.Revision, afterStale.Revision);
        Assert.Equal(EngineJson.Write(beforeStale with { RequestId = "same" }), EngineJson.Write(afterStale with { RequestId = "same" }));
    }
}
