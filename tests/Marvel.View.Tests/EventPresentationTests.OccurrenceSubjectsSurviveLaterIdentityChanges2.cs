using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;
using Xunit;

namespace Marvel.View.Tests;
public sealed class EventPresentationOccurrenceSubjectsSurviveLaterIdentityChangesTests : EventPresentationTestBase
{
    [Fact]
    public void OccurrenceSubjectsSurviveLaterIdentityChangesAndHistoryReconstruction()
    {
        var damage = new FieldSet(7, "health", 1, 0)
        {
            Subjects = new Dictionary<int, string>
            {
                [7] = "Drone"
            },
        };
        var defeated = new CardsMoved(AreaRef.Player("EngagedEnemiesArea", 0), AreaRef.Player("DiscardPile", 0), [new Landing(7, 0)])
        {
            Verb = "Defeat",
            Subjects = new Dictionary<int, string>
            {
                [7] = "Drone"
            },
        };
        var secondDefeated = new CardsMoved(AreaRef.Player("EngagedEnemiesArea", 0), AreaRef.Player("DiscardPile", 0), [new Landing(13, 1)])
        {
            Verb = "Defeat",
            Subjects = new Dictionary<int, string>
            {
                [13] = "Drone"
            },
        };
        WorldDescriptor after = NarrativeWorld();
        var chronology = new EventChronology();
        chronology.Reset([damage, defeated, secondDefeated], after);
        Assert.Equal(["Drone changed health from 1 to 0.", "Drone and Drone were defeated."], chronology.Entries.Select(entry => entry.Summary));
        Assert.Equal("Spider-Tracer changed health from 0 to 1.", EventPresenter.Present(new FieldSet(7, "health", 0, 1), after).Summary);
    }

    [Fact]
    public void MulliganMovementsBecomeTwoNarrativeActions()
    {
        GameEvent[] happened = [Move(7, "HandsArea", "DiscardPile", "Mulligan"), Move(13, "HandsArea", "DiscardPile", "Mulligan"), Move(14, "HandsArea", "DiscardPile", "Mulligan"), Move(15, "PlayerDeck", "HandsArea", "Draw"), Move(16, "PlayerDeck", "HandsArea", "Draw"), Move(17, "PlayerDeck", "HandsArea", "Draw"), ];
        EventBatchPresentation batch = EventCuePlanner.Plan(happened, NarrativeWorld(), Outcome.Unfinished);
        Assert.Equal(["Spider-Man discarded Spider-Tracer, Aunt May, and Swinging Web Kick.", "Spider-Man drew Interrogation Room, Nick Fury, and Helicarrier.", ], batch.History.Select(entry => entry.Summary));
        Assert.Equal(batch.History, batch.Highlights);
        Assert.Equal(6, batch.Cues.Count);
    }

    [Fact]
    public void CrossPlayerMovementNamesBothOwners()
    {
        var moved = new CardsMoved(AreaRef.Player("HandsArea", 0), AreaRef.Player("DiscardPile", 1), [new Landing(7, 0)]);
        EventPresentation presentation = EventPresenter.Present(moved, NarrativeWorld());
        Assert.Equal("Spider-Man discarded Spider-Tracer to Carol Danvers's discard pile.", presentation.Summary);
    }

    [Fact]
    public void AddingADeckCardToHandIsNotDescribedAsDrawingIt()
    {
        CardsMoved moved = Move(15, "PlayerDeck", "HandsArea", "Add_To_Hand");
        EventPresentation presentation = EventPresenter.Present(moved, NarrativeWorld());
        Assert.Equal("Spider-Man added Interrogation Room to their hand from Spider-Man's player deck.", presentation.Summary);
    }

    [Theory]
    [MemberData(nameof(OrdinaryActionResults))]
    public void OrdinaryCommittedActionsReplaceThePersistentResult(GameEvent happened)
    {
        EventBatchPresentation batch = EventCuePlanner.Plan([happened], World(), Outcome.Unfinished);
        Assert.Equal(Assert.Single(batch.Cues), Assert.Single(batch.Highlights));
    }

    [Fact]
    public void StatusGainKeepsHistoryAndFoldsItsAttachmentIntoOneCue()
    {
        GameEvent[] happened = [new CardsCreated(new AreaRef("StatusArea", -1, 9), [new CreatedCard(12, "status-stunned")])
        {
            Verb = "Give_Status",
        }, new CardAttached(12, 9)
        {
            Verb = "Give_Status"
        }, new CardAttached(12, 9)
        {
            Verb = "Give_Status"
        }, ];
        EventBatchPresentation batch = EventCuePlanner.Plan(happened, World(), Outcome.Unfinished);
        Assert.Equal(3, batch.History.Count);
        EventPresentation cue = Assert.Single(batch.Cues);
        Assert.Equal(EventMotionKind.Status, cue.Motion);
        Assert.Equal([9, 12], cue.Anchors.Order());
    }

    [Fact]
    public void SpentStatusKeepsHistoryAndFoldsItsDetachmentIntoOneCue()
    {
        GameEvent[] happened = [new CardsMoved(new AreaRef("StatusArea", -1, 9), AreaRef.Scenario("RemovedArea"), [new Landing(12, 0)])
        {
            Verb = "Discard",
        }, new CardDetached(12, 9)
        {
            Verb = "Discard"
        }, ];
        EventBatchPresentation batch = EventCuePlanner.Plan(happened, World(), Outcome.Unfinished);
        Assert.Equal(2, batch.History.Count);
        Assert.Equal(EventMotionKind.Status, Assert.Single(batch.Cues).Motion);
    }

    [Fact]
    public void OrdinaryCreationAndAttachmentRemainSeparateCues()
    {
        GameEvent[] happened = [new CardsCreated(AreaRef.Player("UpgradesArea", 0), [new CreatedCard(7, "01001")]), new CardAttached(7, 9), ];
        EventBatchPresentation batch = EventCuePlanner.Plan(happened, World(), Outcome.Unfinished);
        Assert.Equal([EventMotionKind.Create, EventMotionKind.Move], batch.Cues.Select(cue => cue.Motion));
    }

    [Fact]
    public void TerminalCueFollowsSemanticEvents()
    {
        WorldDescriptor finished = World()with
        {
            Outcome = Outcome.PlayersWin
        };
        EventBatchPresentation batch = EventCuePlanner.Plan([new FieldSet(9, "health", 1, 0)], finished, Outcome.Unfinished);
        Assert.Equal([EventMotionKind.Damage, EventMotionKind.Terminal], batch.Cues.Select(cue => cue.Motion));
        Assert.Equal(batch.Cues, batch.History);
    }

    [Fact]
    public void ChronologyKeepsOnlyTheMostRecentHundredEntries()
    {
        var chronology = new EventChronology();
        EventPresentation[] entries = Enumerable.Range(1, 105).Select(index => new EventPresentation($"Event {index}", "Test", [], EventMotionKind.Counter)).ToArray();
        chronology.Append(entries);
        Assert.Equal(100, chronology.Entries.Count);
        Assert.Equal("Event 6", chronology.Entries[0].Summary);
        Assert.Equal("Event 105", chronology.Entries[^1].Summary);
    }
}
