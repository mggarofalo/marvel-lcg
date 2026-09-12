using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;
using Xunit;

namespace Marvel.View.Tests;
public abstract class EventPresentationTestBase
{
    public static TheoryData<GameEvent> OrdinaryActionResults() => new()
    {
        new CardFormChanged(7, "01001a", "01001b"),
        new FieldSet(9, "k_threat", 5, 3)
        {
            Verb = "Thwart"
        },
        new CardsMoved(AreaRef.Player("HandsArea", 0), AreaRef.Player("SupportsArea", 0), [new Landing(7, 0)])
        {
            Verb = "Play"
        },
    };
    protected static GameEvent[] Events() => [new CardsCreated(AreaRef.Scenario("EncounterDeck"), [new CreatedCard(7, "01001")]), new CardsMoved(AreaRef.Scenario("EncounterDeck"), AreaRef.Scenario("DiscardPileArea"), [new Landing(7, 0)]), new AreaReordered(AreaRef.Scenario("EncounterDeck"), [7]), new CardFormChanged(7, "01001a", "01001b"), new CardsFlipped([7], true), new CardAttached(7, 9), new CardDetached(7, 9), new ControlChanged(7, 0, 1), new FieldSet(7, "health", 1, 2), new PlayAreaJoined(0, 1), new PlayAreaDetached(0, 1), ];
    protected static CardsMoved Move(int card, string from, string to, string verb) => new(AreaRef.Player(from, 0), AreaRef.Player(to, 0), [new Landing(card, 0)])
    {
        Verb = verb,
        Trigger = "WhenPlayerChooseAbility",
    };
    protected static WorldDescriptor NarrativeWorld() => new([new PlayerDescriptor(0, "Spider-Man", false), new PlayerDescriptor(1, "Carol Danvers", false), ], [new AreaDescriptor(1, "DiscardPile", 0, -1, [Readable(7, "Spider-Tracer"), Readable(13, "Aunt May"), Readable(14, "Swinging Web Kick"), ], []), new AreaDescriptor(2, "HandsArea", 0, -1, [Readable(15, "Interrogation Room"), Readable(16, "Nick Fury"), Readable(17, "Helicarrier"), ], []), ], [], Outcome.Unfinished);
    protected static WorldDescriptor World() => new([new PlayerDescriptor(0, "Peter Parker", false), new PlayerDescriptor(1, "Carol Danvers", false), ], [new AreaDescriptor(1, "HandsArea", 0, -1, [Readable(7, "Swinging Web Kick")], []), new AreaDescriptor(2, "VillainArea", -1, -1, [Readable(9, "Rhino", CardKind.EncounterVillain, "1"), Hidden(12)], []), ], [], Outcome.Unfinished);
    protected static CardDescriptor Readable(int id, string title, CardKind kind = CardKind.Hero, string? stage = null) => new(id, CardBack.Player, true, true, -1, new CardFaceDescriptor($"face-{id}", title, string.Empty, kind, new Dictionary<string, long>(StringComparer.Ordinal)) { PrintedStats = stage is null ? new Dictionary<string, string>(StringComparer.Ordinal) : new Dictionary<string, string>(StringComparer.Ordinal) { ["Stage"] = stage }, });
    protected static CardDescriptor Hidden(int id) => new(id, CardBack.Encounter, false, true, -1, null);
}
