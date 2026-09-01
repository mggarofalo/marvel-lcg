using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

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
                AreaRef.Player("DiscardPileArea", 0),
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
                "Moved Swinging Web Kick from Peter Parker's hands to Peter Parker's discard pile.",
                "Rhino changed hit points from 15 to 11.",
                "Turned face-down encounter card face down.",
            ],
            result.Select(entry => entry.Summary));
        Assert.Equal(
            ["Pay Cost · When Player In Turn", "Attack · When Damage Dealt", "Engine resolution"],
            result.Select(entry => entry.Cause));
        Assert.Equal([7], result[0].Anchors);
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
                    [Readable(9, "Rhino"), Hidden(12)],
                    []),
            ],
            [],
            Outcome.Unfinished);

    private static CardDescriptor Readable(int id, string title) =>
        new(
            id,
            CardBack.Player,
            true,
            true,
            -1,
            new CardFaceDescriptor(
                $"face-{id}", title, string.Empty, CardKind.Hero,
                new Dictionary<string, long>(StringComparer.Ordinal)));

    private static CardDescriptor Hidden(int id) =>
        new(id, CardBack.Encounter, false, true, -1, null);
}
