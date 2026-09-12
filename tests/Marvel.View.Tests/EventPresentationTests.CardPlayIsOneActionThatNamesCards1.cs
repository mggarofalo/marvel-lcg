using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;
using Xunit;

namespace Marvel.View.Tests;
public sealed class EventPresentationCardPlayIsOneActionThatNamesCardsTests : EventPresentationTestBase
{
    [Fact]
    public void CardPlayIsOneActionThatNamesCardsAndResourceAbilities()
    {
        string summary = ActionHistoryPresenter.Present(new ActionHistoryFacts(4, "Spider-Man", "turn_action", "PlayerTurn", "Play", "Black Cat", 12, [3, 4], ["Scientist", "First Aid"]));
        Assert.Equal("Spider-Man played Black Cat, generating resources from Scientist and First Aid.", summary);
        Assert.DoesNotContain("Discard", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("·", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void FormChangesAreActionsRatherThanWireDiagnostics()
    {
        string summary = ActionHistoryPresenter.Present(new ActionHistoryFacts(2, "Spider-Man", "turn_control", "PlayerTurn", Game.ChangeForm, "Peter Parker", 1, [], []));
        Assert.Equal("Spider-Man changed form.", summary);
    }

    [Fact]
    public void AutomaticStepsNameTheirPhase()
    {
        string summary = ActionHistoryPresenter.Present(new ActionHistoryFacts(4, "Rhino", "forced_resolution", "VillainPhase", "Resolve", "Rhino activates", null, [], []));
        Assert.Equal("Rhino resolved Rhino activates during the villain phase.", summary);
    }

    [Theory]
    [InlineData(Game.EndPhaseVerb, "Spider-Man", "Spider-Man ended their turn.")]
    [InlineData(BasicPowers.AttackVerb, "Spider-Man", "Spider-Man attacked.")]
    [InlineData(BasicPowers.AttackVerb, "Black Cat", "Spider-Man attacked with Black Cat.")]
    [InlineData(BasicPowers.ThwartVerb, "Spider-Man", "Spider-Man thwarted.")]
    [InlineData(BasicPowers.RecoverVerb, "Peter Parker", "Spider-Man recovered.")]
    public void TurnControlsAndBasicPowersHaveActionNames(string verb, string action, string expected)
    {
        string summary = ActionHistoryPresenter.Present(new ActionHistoryFacts(3, "Spider-Man", "turn_action", "PlayerTurn", verb, action, 1, [], []));
        Assert.Equal(expected, summary);
    }

    [Fact]
    public void TerminalOutcomeCompletesRatherThanReplacesTheAction()
    {
        string summary = ActionHistoryPresenter.Present(new ActionHistoryFacts(8, "Spider-Man", "turn_action", "PlayerTurn", CardPlay.Verb, "Swinging Web Kick", 14, [13], ["First Aid"], Outcome.PlayersWin));
        Assert.Equal("Spider-Man played Swinging Web Kick, generating resources from First Aid. " + "The players won the game.", summary);
    }

    [Fact]
    public void PlayDetailsSuppressPaymentButKeepEffectDrivenDiscards()
    {
        var facts = new ActionHistoryFacts(4, "Spider-Man", "turn_action", "PlayerTurn", CardPlay.Verb, "Black Cat", 14, [13], ["Aunt May"]);
        GameEvent[] events = [Move(13, "HandsArea", "DiscardPile", "Discard")with
        {
            Trigger = CardPlay.Verb,
        }, Move(7, "PlayerDeck", "DiscardPile", "Discard"), ];
        string detail = Assert.Single(ActionHistoryPresenter.PresentDiscardDetails(facts, events, NarrativeWorld()));
        Assert.Contains("discarded", detail, StringComparison.Ordinal);
        Assert.Contains("Spider-Tracer", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Aunt May", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void EffectDiscardDoesNotReplaceABasicPowerHeadline()
    {
        var facts = new ActionHistoryFacts(4, "Spider-Man", "turn_action", "PlayerTurn", BasicPowers.AttackVerb, "Black Cat", 14, [], []);
        GameEvent[] events = [Move(7, "PlayerDeck", "DiscardPile", "Discard")];
        ActionHistoryPresentation entry = ActionHistoryPresenter.PresentEntry(facts, events, NarrativeWorld());
        Assert.Equal("Spider-Man attacked with Black Cat.", entry.Summary);
        Assert.Contains("discarded Spider-Tracer", Assert.Single(entry.Details), StringComparison.Ordinal);
    }

    [Fact]
    public void HistoricalDiscardNeverFallsBackToAConcealedObjectId()
    {
        var facts = new ActionHistoryFacts(4, "Spider-Man", "turn_action", "PlayerTurn", CardPlay.Verb, "Black Cat", 14, [], []);
        GameEvent[] events = [Move(77, "PlayerDeck", "DiscardPile", "Discard")];
        var concealed = new WorldDescriptor([new PlayerDescriptor(0, "Spider-Man", false)], [], [], Outcome.Unfinished);
        string detail = Assert.Single(ActionHistoryPresenter.PresentDiscardDetails(facts, events, concealed));
        Assert.Equal("Spider-Man discarded a player card from Spider-Man's player deck.", detail);
        Assert.DoesNotContain("77", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryWireEventKindHasAPresentation()
    {
        Type[] wireKinds = typeof(GameEvent).GetCustomAttributes(typeof(JsonDerivedTypeAttribute), inherit: false).Cast<JsonDerivedTypeAttribute>().Select(attribute => attribute.DerivedType).OrderBy(type => type.Name, StringComparer.Ordinal).ToArray();
        GameEvent[] examples = Events();
        Assert.Equal(wireKinds, examples.Select(value => value.GetType()).OrderBy(type => type.Name, StringComparer.Ordinal));
        Assert.All(examples, happened => Assert.False(string.IsNullOrWhiteSpace(EventPresenter.Present(happened, World()).Summary)));
    }

    [Fact]
    public void EventsKeepResolutionOrderAndHaveExactHumanSummariesAndCauses()
    {
        GameEvent[] events = [new CardsMoved(AreaRef.Player("HandsArea", 0), AreaRef.Player("DiscardPile", 0), [new Landing(7, 0)])
        {
            Verb = "Pay_Cost",
            Trigger = "WhenPlayerInTurn",
        }, new FieldSet(9, "hitPoints", 15, 11)
        {
            Verb = "Attack",
            Trigger = "WhenDamageDealt",
        }, new CardsFlipped([12], FaceUp: false), ];
        IReadOnlyList<EventPresentation> result = EventPresenter.Present(events, World());
        Assert.Equal(["Peter Parker discarded Swinging Web Kick.", "Rhino changed hit points from 15 to 11.", "Turned face-down encounter card face down.", ], result.Select(entry => entry.Summary));
        Assert.Equal(["Pay Cost · When Player In Turn", "Attack · When Damage Dealt", "Engine resolution"], result.Select(entry => entry.Cause));
        Assert.Equal([7], result[0].Anchors);
        Assert.Equal([EventMotionKind.Move, EventMotionKind.Damage, EventMotionKind.Flip], result.Select(entry => entry.Motion));
    }

    [Fact]
    public void PresentationNeverRestoresAHiddenPrintedIdentityOrForm()
    {
        const string hiddenPrintedId = "secret-printed-face";
        WorldDescriptor world = World();
        GameEvent[] events = [new CardsCreated(AreaRef.Scenario("EncounterDeck"), [new CreatedCard(77, hiddenPrintedId)]), new CardFormChanged(78, hiddenPrintedId, "other-secret-face"), ];
        string text = string.Join(" ", EventPresenter.Present(events, world).SelectMany(entry => new[] { entry.Summary, entry.Cause }));
        Assert.DoesNotContain("secret", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("printed", text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Created card 77 in the scenario's encounter deck.", EventPresenter.Present(events[0], world).Summary);
        Assert.Equal("card 78 changed form.", EventPresenter.Present(events[1], world).Summary);
    }

    [Fact]
    public void ChronologyAppendsResponsesAndResetStartsANewGame()
    {
        var chronology = new EventChronology();
        chronology.Append([new CardAttached(7, 9) { Verb = "Attach" }], World());
        chronology.Append([new CardDetached(7, 9) { Verb = "Discard" }], World());
        Assert.Equal(["Attached Swinging Web Kick to Rhino.", "Detached Swinging Web Kick from Rhino."], chronology.Entries.Select(entry => entry.Summary));
        chronology.Reset([new PlayAreaJoined(0, 4) { Trigger = "BeginGame" }], World());
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
    public void FieldChangesChooseASemanticMotion(string field, long from, long to, EventMotionKind expected)
    {
        EventPresentation presentation = EventPresenter.Present(new FieldSet(7, field, from, to), World());
        Assert.Equal(expected, presentation.Motion);
    }

    [Fact]
    public void RegisteringAFieldIsStateRatherThanDamageOrHealing()
    {
        EventPresentation presentation = EventPresenter.Present(new FieldSet(7, "health", null, 10), World());
        Assert.Equal(EventMotionKind.State, presentation.Motion);
    }

    [Theory]
    [InlineData("k_threat", 3, 1, "Swinging Web Kick changed threat from 3 to 1.")]
    [InlineData("is_exhaust", 0, 1, "Swinging Web Kick became exhausted.")]
    [InlineData("is_exhaust", 1, 0, "Swinging Web Kick became ready.")]
    public void InternalFieldNamesHaveNaturalHistorySummaries(string field, long from, long to, string expected)
    {
        EventPresentation presentation = EventPresenter.Present(new FieldSet(7, field, from, to), World());
        Assert.Equal(expected, presentation.Summary);
    }

    [Fact]
    public void TerminalOutcomesHaveExplicitPresentation()
    {
        EventPresentation presentation = EventPresenter.Terminal(Outcome.PlayersWin);
        Assert.Equal("The players won the game.", presentation.Summary);
        Assert.Equal(EventMotionKind.Terminal, presentation.Motion);
        Assert.Empty(presentation.Anchors);
        Assert.Throws<ArgumentOutOfRangeException>(() => EventPresenter.Terminal(Outcome.Unfinished));
    }

    [Fact]
    public void DefeatMovesNameTheDefeatedCardAndBecomePersistentHighlights()
    {
        var defeated = new CardsMoved(AreaRef.Scenario("VillainArea"), AreaRef.Scenario("RemovedArea"), [new Landing(9, 0)])
        {
            Verb = "Defeat",
            Trigger = "Attack",
        };
        EventBatchPresentation batch = EventCuePlanner.Plan([defeated], World(), Outcome.Unfinished);
        EventPresentation highlight = Assert.Single(batch.Highlights);
        Assert.Equal("Rhino stage 1 was defeated.", highlight.Summary);
        Assert.Equal(EventMotionKind.Defeat, highlight.Motion);
        Assert.Equal(highlight, Assert.Single(batch.History));
    }
}
