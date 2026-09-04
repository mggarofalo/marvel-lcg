using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;
using Xunit;

namespace Marvel.View.Tests;

public sealed class EventPresentationTests
{
    [Fact]
    public void EveryWireEventKindHasAPresentation()
    {
        Type[] wireKinds = typeof(GameEvent)
            .GetCustomAttributes(typeof(JsonDerivedTypeAttribute), inherit: false)
            .Cast<JsonDerivedTypeAttribute>()
            .Select(attribute => attribute.DerivedType)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
        GameEvent[] examples = Events();

        Assert.Equal(wireKinds, examples.Select(value => value.GetType())
            .OrderBy(type => type.Name, StringComparer.Ordinal));
        Assert.All(examples, happened =>
            Assert.False(string.IsNullOrWhiteSpace(EventPresenter.Present(happened, World()).Summary)));
    }

    [Fact]
    public void EventsKeepResolutionOrderAndHaveExactHumanSummariesAndCauses()
    {
        GameEvent[] events =
        [
            new CardsMoved(
                AreaRef.Player("HandsArea", 0),
                AreaRef.Player("DiscardPile", 0),
                [new Landing(7, 0)])
            {
                Verb = "Pay_Cost",
                Trigger = "WhenPlayerInTurn",
            },
            new FieldSet(9, "hitPoints", 15, 11)
            {
                Verb = "Attack",
                Trigger = "WhenDamageDealt",
            },
            new CardsFlipped([12], FaceUp: false),
        ];

        IReadOnlyList<EventPresentation> result = EventPresenter.Present(events, World());

        Assert.Equal(
            [
                "Peter Parker discarded Swinging Web Kick.",
                "Rhino changed hit points from 15 to 11.",
                "Turned face-down encounter card face down.",
            ],
            result.Select(entry => entry.Summary));
        Assert.Equal(
            ["Pay Cost · When Player In Turn", "Attack · When Damage Dealt", "Engine resolution"],
            result.Select(entry => entry.Cause));
        Assert.Equal([7], result[0].Anchors);
        Assert.Equal(
            [EventMotionKind.Move, EventMotionKind.Damage, EventMotionKind.Flip],
            result.Select(entry => entry.Motion));
    }

    [Fact]
    public void PresentationNeverRestoresAHiddenPrintedIdentityOrForm()
    {
        const string hiddenPrintedId = "secret-printed-face";
        WorldDescriptor world = World();
        GameEvent[] events =
        [
            new CardsCreated(AreaRef.Scenario("EncounterDeck"),
                [new CreatedCard(77, hiddenPrintedId)]),
            new CardFormChanged(78, hiddenPrintedId, "other-secret-face"),
        ];

        string text = string.Join(" ", EventPresenter.Present(events, world)
            .SelectMany(entry => new[] { entry.Summary, entry.Cause }));

        Assert.DoesNotContain("secret", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("printed", text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Created card 77 in the scenario's encounter deck.",
            EventPresenter.Present(events[0], world).Summary);
        Assert.Equal("card 78 changed form.", EventPresenter.Present(events[1], world).Summary);
    }

    [Fact]
    public void ChronologyAppendsResponsesAndResetStartsANewGame()
    {
        var chronology = new EventChronology();
        chronology.Append(
            [new CardAttached(7, 9) { Verb = "Attach" }], World());
        chronology.Append(
            [new CardDetached(7, 9) { Verb = "Discard" }], World());

        Assert.Equal(
            ["Attached Swinging Web Kick to Rhino.", "Detached Swinging Web Kick from Rhino."],
            chronology.Entries.Select(entry => entry.Summary));

        chronology.Reset(
            [new PlayAreaJoined(0, 4) { Trigger = "BeginGame" }], World());

        EventPresentation only = Assert.Single(chronology.Entries);
        Assert.Equal("Peter Parker's play area joined game area 4.", only.Summary);
        Assert.Equal("Begin Game", only.Cause);
    }

    [Theory]
    [InlineData("health", 10, 8, EventMotionKind.Damage)]
    [InlineData("health", 8, 10, EventMotionKind.Heal)]
    [InlineData("k_damage", 1, 2, EventMotionKind.Damage)]
    [InlineData("k_damage", 2, 1, EventMotionKind.Heal)]
    [InlineData("c_energy", 1, 2, EventMotionKind.Counter)]
    [InlineData("k_acceleration", 0, 1, EventMotionKind.Counter)]
    [InlineData("k_threat", 1, 2, EventMotionKind.State)]
    [InlineData("is_exhaust", 0, 1, EventMotionKind.State)]
    [InlineData("attack", 2, 3, EventMotionKind.State)]
    public void FieldChangesChooseASemanticMotion(
        string field,
        long from,
        long to,
        EventMotionKind expected)
    {
        EventPresentation presentation = EventPresenter.Present(
            new FieldSet(7, field, from, to), World());

        Assert.Equal(expected, presentation.Motion);
    }

    [Fact]
    public void RegisteringAFieldIsStateRatherThanDamageOrHealing()
    {
        EventPresentation presentation = EventPresenter.Present(
            new FieldSet(7, "health", null, 10), World());

        Assert.Equal(EventMotionKind.State, presentation.Motion);
    }

    [Theory]
    [InlineData("k_threat", 3, 1, "Swinging Web Kick changed threat from 3 to 1.")]
    [InlineData("is_exhaust", 0, 1, "Swinging Web Kick became exhausted.")]
    [InlineData("is_exhaust", 1, 0, "Swinging Web Kick became ready.")]
    public void InternalFieldNamesHaveNaturalHistorySummaries(
        string field, long from, long to, string expected)
    {
        EventPresentation presentation = EventPresenter.Present(
            new FieldSet(7, field, from, to), World());

        Assert.Equal(expected, presentation.Summary);
    }

    [Fact]
    public void TerminalOutcomesHaveExplicitPresentation()
    {
        EventPresentation presentation = EventPresenter.Terminal(Outcome.PlayersWin);

        Assert.Equal("The players won the game.", presentation.Summary);
        Assert.Equal(EventMotionKind.Terminal, presentation.Motion);
        Assert.Empty(presentation.Anchors);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EventPresenter.Terminal(Outcome.Unfinished));
    }

    [Fact]
    public void DefeatMovesNameTheDefeatedCardAndBecomePersistentHighlights()
    {
        var defeated = new CardsMoved(
            AreaRef.Scenario("VillainArea"),
            AreaRef.Scenario("RemovedArea"),
            [new Landing(9, 0)])
        {
            Verb = "Defeat",
            Trigger = "Attack",
        };

        EventBatchPresentation batch = EventCuePlanner.Plan(
            [defeated], World(), Outcome.Unfinished);

        EventPresentation highlight = Assert.Single(batch.Highlights);
        Assert.Equal("Rhino stage 1 was defeated.", highlight.Summary);
        Assert.Equal(EventMotionKind.Defeat, highlight.Motion);
        Assert.Equal(highlight, Assert.Single(batch.History));
    }

    [Fact]
    public void MulliganMovementsBecomeTwoNarrativeActions()
    {
        GameEvent[] happened =
        [
            Move(7, "HandsArea", "DiscardPile", "Mulligan"),
            Move(13, "HandsArea", "DiscardPile", "Mulligan"),
            Move(14, "HandsArea", "DiscardPile", "Mulligan"),
            Move(15, "PlayerDeck", "HandsArea", "Draw"),
            Move(16, "PlayerDeck", "HandsArea", "Draw"),
            Move(17, "PlayerDeck", "HandsArea", "Draw"),
        ];

        EventBatchPresentation batch = EventCuePlanner.Plan(
            happened, NarrativeWorld(), Outcome.Unfinished);

        Assert.Equal(
            [
                "Spider-Man discarded Spider-Tracer, Aunt May, and Swinging Web Kick.",
                "Spider-Man drew Interrogation Room, Nick Fury, and Helicarrier.",
            ],
            batch.History.Select(entry => entry.Summary));
        Assert.Equal(batch.History, batch.Highlights);
        Assert.Equal(6, batch.Cues.Count);
    }

    [Fact]
    public void CrossPlayerMovementNamesBothOwners()
    {
        var moved = new CardsMoved(
            AreaRef.Player("HandsArea", 0),
            AreaRef.Player("DiscardPile", 1),
            [new Landing(7, 0)]);

        EventPresentation presentation = EventPresenter.Present(moved, NarrativeWorld());

        Assert.Equal(
            "Spider-Man discarded Spider-Tracer to Carol Danvers's discard pile.",
            presentation.Summary);
    }

    [Fact]
    public void AddingADeckCardToHandIsNotDescribedAsDrawingIt()
    {
        CardsMoved moved = Move(15, "PlayerDeck", "HandsArea", "Add_To_Hand");

        EventPresentation presentation = EventPresenter.Present(moved, NarrativeWorld());

        Assert.Equal(
            "Spider-Man added Interrogation Room to their hand from Spider-Man's player deck.",
            presentation.Summary);
    }

    [Theory]
    [MemberData(nameof(OrdinaryActionResults))]
    public void OrdinaryCommittedActionsReplaceThePersistentResult(GameEvent happened)
    {
        EventBatchPresentation batch = EventCuePlanner.Plan(
            [happened], World(), Outcome.Unfinished);

        Assert.Equal(Assert.Single(batch.Cues), Assert.Single(batch.Highlights));
    }

    public static TheoryData<GameEvent> OrdinaryActionResults() => new()
    {
        new CardFormChanged(7, "01001a", "01001b"),
        new FieldSet(9, "k_threat", 5, 3) { Verb = "Thwart" },
        new CardsMoved(
            AreaRef.Player("HandsArea", 0),
            AreaRef.Player("SupportsArea", 0),
            [new Landing(7, 0)]) { Verb = "Play" },
    };

    [Fact]
    public void StatusGainKeepsHistoryAndFoldsItsAttachmentIntoOneCue()
    {
        GameEvent[] happened =
        [
            new CardsCreated(
                new AreaRef("StatusArea", -1, 9),
                [new CreatedCard(12, "status-stunned")])
            {
                Verb = "Give_Status",
            },
            new CardAttached(12, 9) { Verb = "Give_Status" },
            new CardAttached(12, 9) { Verb = "Give_Status" },
        ];

        EventBatchPresentation batch = EventCuePlanner.Plan(
            happened, World(), Outcome.Unfinished);

        Assert.Equal(3, batch.History.Count);
        EventPresentation cue = Assert.Single(batch.Cues);
        Assert.Equal(EventMotionKind.Status, cue.Motion);
        Assert.Equal([9, 12], cue.Anchors.Order());
    }

    [Fact]
    public void SpentStatusKeepsHistoryAndFoldsItsDetachmentIntoOneCue()
    {
        GameEvent[] happened =
        [
            new CardsMoved(
                new AreaRef("StatusArea", -1, 9),
                AreaRef.Scenario("RemovedArea"),
                [new Landing(12, 0)])
            {
                Verb = "Discard",
            },
            new CardDetached(12, 9) { Verb = "Discard" },
        ];

        EventBatchPresentation batch = EventCuePlanner.Plan(
            happened, World(), Outcome.Unfinished);

        Assert.Equal(2, batch.History.Count);
        Assert.Equal(EventMotionKind.Status, Assert.Single(batch.Cues).Motion);
    }

    [Fact]
    public void OrdinaryCreationAndAttachmentRemainSeparateCues()
    {
        GameEvent[] happened =
        [
            new CardsCreated(
                AreaRef.Player("UpgradesArea", 0),
                [new CreatedCard(7, "01001")]),
            new CardAttached(7, 9),
        ];

        EventBatchPresentation batch = EventCuePlanner.Plan(
            happened, World(), Outcome.Unfinished);

        Assert.Equal(
            [EventMotionKind.Create, EventMotionKind.Move],
            batch.Cues.Select(cue => cue.Motion));
    }

    [Fact]
    public void TerminalCueFollowsSemanticEvents()
    {
        WorldDescriptor finished = World() with { Outcome = Outcome.PlayersWin };

        EventBatchPresentation batch = EventCuePlanner.Plan(
            [new FieldSet(9, "health", 1, 0)],
            finished,
            Outcome.Unfinished);

        Assert.Equal(
            [EventMotionKind.Damage, EventMotionKind.Terminal],
            batch.Cues.Select(cue => cue.Motion));
        Assert.Equal(batch.Cues, batch.History);
    }

    [Fact]
    public void ChronologyKeepsOnlyTheMostRecentHundredEntries()
    {
        var chronology = new EventChronology();
        EventPresentation[] entries = Enumerable.Range(1, 105)
            .Select(index => new EventPresentation(
                $"Event {index}", "Test", [], EventMotionKind.Counter))
            .ToArray();

        chronology.Append(entries);

        Assert.Equal(100, chronology.Entries.Count);
        Assert.Equal("Event 6", chronology.Entries[0].Summary);
        Assert.Equal("Event 105", chronology.Entries[^1].Summary);
    }

    private static GameEvent[] Events() =>
    [
        new CardsCreated(AreaRef.Scenario("EncounterDeck"), [new CreatedCard(7, "01001")]),
        new CardsMoved(AreaRef.Scenario("EncounterDeck"), AreaRef.Scenario("DiscardPileArea"),
            [new Landing(7, 0)]),
        new AreaReordered(AreaRef.Scenario("EncounterDeck"), [7]),
        new CardFormChanged(7, "01001a", "01001b"),
        new CardsFlipped([7], true),
        new CardAttached(7, 9),
        new CardDetached(7, 9),
        new ControlChanged(7, 0, 1),
        new FieldSet(7, "health", 1, 2),
        new PlayAreaJoined(0, 1),
        new PlayAreaDetached(0, 1),
    ];

    private static CardsMoved Move(int card, string from, string to, string verb) =>
        new(
            AreaRef.Player(from, 0),
            AreaRef.Player(to, 0),
            [new Landing(card, 0)])
        {
            Verb = verb,
            Trigger = "WhenPlayerChooseAbility",
        };

    private static WorldDescriptor NarrativeWorld() =>
        new(
            [
                new PlayerDescriptor(0, "Spider-Man", false),
                new PlayerDescriptor(1, "Carol Danvers", false),
            ],
            [
                new AreaDescriptor(
                    1,
                    "DiscardPile",
                    0,
                    -1,
                    [
                        Readable(7, "Spider-Tracer"),
                        Readable(13, "Aunt May"),
                        Readable(14, "Swinging Web Kick"),
                    ],
                    []),
                new AreaDescriptor(
                    2,
                    "HandsArea",
                    0,
                    -1,
                    [
                        Readable(15, "Interrogation Room"),
                        Readable(16, "Nick Fury"),
                        Readable(17, "Helicarrier"),
                    ],
                    []),
            ],
            [],
            Outcome.Unfinished);

    private static WorldDescriptor World() =>
        new(
            [
                new PlayerDescriptor(0, "Peter Parker", false),
                new PlayerDescriptor(1, "Carol Danvers", false),
            ],
            [
                new AreaDescriptor(
                    1,
                    "HandsArea",
                    0,
                    -1,
                    [Readable(7, "Swinging Web Kick")],
                    []),
                new AreaDescriptor(
                    2,
                    "VillainArea",
                    -1,
                    -1,
                    [Readable(9, "Rhino", CardKind.EncounterVillain, "1"), Hidden(12)],
                    []),
            ],
            [],
            Outcome.Unfinished);

    private static CardDescriptor Readable(
        int id, string title, CardKind kind = CardKind.Hero, string? stage = null) =>
        new(
            id,
            CardBack.Player,
            true,
            true,
            -1,
            new CardFaceDescriptor(
                $"face-{id}", title, string.Empty, kind,
                new Dictionary<string, long>(StringComparer.Ordinal))
            {
                PrintedStats = stage is null
                    ? new Dictionary<string, string>(StringComparer.Ordinal)
                    : new Dictionary<string, string>(StringComparer.Ordinal)
                        { ["Stage"] = stage },
            });

    private static CardDescriptor Hidden(int id) =>
        new(id, CardBack.Encounter, false, true, -1, null);
}
