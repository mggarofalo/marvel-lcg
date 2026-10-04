using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

/// <summary>Synthetic presentation contracts using the verbs emitted by Core effects.</summary>
public sealed class EventResultMeaningTests : EventPresentationTestBase
{
    [Fact]
    public void BoostFlipDoesNotPresentOrdinaryWhenRevealedTextAsAnEffect()
    {
        CardDescriptor card = Readable(9, "Hard to Keep Down", CardKind.Treachery);
        card = card with { Face = card.Face! with { RulesText = "When Revealed: Rhino heals 4 damage." } };
        WorldDescriptor world = World() with
        {
            Areas = [new AreaDescriptor(2, "RevealingArea", -1, -1, [card], [])],
        };
        var boost = new CardsFlipped([9], true) { Verb = "Boost", Trigger = "AttackInitiated" };

        EventPresentation result = EventPresenter.Present(boost, world);

        Assert.Equal("Turned Hard to Keep Down face up for a boost.", result.Summary);
        Assert.DoesNotContain("heals", result.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("When Revealed", result.Summary, StringComparison.Ordinal);
        Assert.Equal("Boost · Attack Initiated", result.Cause);
        Assert.Equal("Revealed Hard to Keep Down: When Revealed: Rhino heals 4 damage.",
            EventPresenter.Present(boost with { Verb = "Reveal" }, world).Summary);
        Assert.Equal("Turned Hard to Keep Down face down.",
            EventPresenter.Present(boost with { FaceUp = false }, world).Summary);
    }

    [Fact]
    public void EffectHandGainRemainsInTheImmediateRecapAlongsideDamage()
    {
        GameEvent[] events = [new FieldSet(9, "hitPoints", 14, 6),
            Move(7, "DiscardPile", "HandsArea", "Add_To_Hand")];

        EventBatchPresentation result = EventCuePlanner.Plan(events, World(), Outcome.Unfinished);

        Assert.Equal([EventMotionKind.Damage, EventMotionKind.HandGain],
            result.Highlights.Select(cue => cue.Motion));
        Assert.Equal("Peter Parker added Swinging Web Kick to their hand from Peter Parker's discard pile.",
            result.Highlights[1].Summary);
        Assert.Equal([7], result.Highlights[1].Anchors);
    }

    [Fact]
    public void OrdinaryDrawDoesNotDisplaceEffectHighlights()
    {
        GameEvent[] events = [new FieldSet(9, "hitPoints", 14, 6),
            Move(7, "PlayerDeck", "HandsArea", "Draw")];

        EventBatchPresentation result = EventCuePlanner.Plan(events, World(), Outcome.Unfinished);

        Assert.Equal(EventMotionKind.Damage, Assert.Single(result.Highlights).Motion);
        Assert.Equal("Peter Parker drew Swinging Web Kick.", result.History[1].Summary);
    }

    [Fact]
    public void ReturnToHandNamesTheCompletedEffect()
    {
        EventPresentation result = EventPresenter.Present(
            Move(7, "UpgradesArea", "HandsArea", "Return"), World());

        Assert.Equal("Peter Parker returned Swinging Web Kick to their hand from Peter Parker's upgrades.",
            result.Summary);
        Assert.Equal(EventMotionKind.HandGain, result.Motion);
    }

    [Fact]
    public void BlackCatHistoryKeepsDiscardThenRecoveryAndOmitsPayment()
    {
        var action = new ActionHistoryFacts(1, "Spider-Man", "turn_action", "PlayerTurn",
            CardPlay.Verb, "Black Cat", 99, [13], ["Aunt May"]);
        GameEvent[] events = [Move(13, "HandsArea", "DiscardPile", "Discard"),
            Move(14, "PlayerDeck", "DiscardPile", "Discard"),
            Move(14, "DiscardPile", "HandsArea", "Add_To_Hand")];

        ActionHistoryPresentation result = ActionHistoryPresenter.PresentEntry(action, events, NarrativeWorld());

        Assert.Equal("Spider-Man played Black Cat, generating resources from Aunt May.", result.Summary);
        Assert.Equal(["Spider-Man discarded Swinging Web Kick from Spider-Man's player deck.",
            "Spider-Man added Swinging Web Kick to their hand from Spider-Man's discard pile."], result.Details);
    }

    [Fact]
    public void RecoveryOfAPaymentCardIsNotSuppressedAsPayment()
    {
        var action = new ActionHistoryFacts(1, "Spider-Man", "turn_action", "PlayerTurn",
            CardPlay.Verb, "Black Cat", 99, [14], ["Swinging Web Kick"]);

        ActionHistoryPresentation result = ActionHistoryPresenter.PresentEntry(action,
            [Move(14, "DiscardPile", "HandsArea", "Add_To_Hand")], NarrativeWorld());

        Assert.Contains("added Swinging Web Kick", Assert.Single(result.Details), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Return", "returned")]
    [InlineData("Add_To_Hand", "added")]
    public void HandGainHistoryNeverRestoresConcealedOccurrenceIdentity(string verb, string expectedVerb)
    {
        var action = new ActionHistoryFacts(1, "Spider-Man", "turn_action", "PlayerTurn",
            CardPlay.Verb, "Black Cat", 99, [], []);
        var moved = Move(77, "DiscardPile", "HandsArea", verb) with
        {
            Subjects = new Dictionary<int, string> { [77] = "Private Swinging Web Kick" },
        };
        var hidden = new WorldDescriptor([new PlayerDescriptor(0, "Spider-Man", false)], [], [], Outcome.Unfinished);

        string detail = Assert.Single(ActionHistoryPresenter.PresentEntry(action, [moved], hidden).Details);

        Assert.Equal($"Spider-Man {expectedVerb} a player card to their hand from Spider-Man's discard pile.", detail);
        Assert.DoesNotContain("77", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Swinging Web Kick", detail, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("c_web", 2, 1, "Rhino web counters: 2 → 1 (1 removed).")]
    [InlineData("c_energy", 1, 3, "Rhino energy counters: 1 → 3 (2 added).")]
    [InlineData("c_web", null, 2, "Rhino gained 2 web counters.")]
    [InlineData("c_web", 2, null, "Rhino lost 2 web counters.")]
    [InlineData("k_acceleration", 1, 2, "Rhino acceleration tokens: 1 → 2 (1 added).")]
    public void CounterResultsUseHumanNamesAndAuthoritativeQuantities(
        string field, int? before, int? after, string expected)
    {
        EventPresentation result = EventPresenter.Present(new FieldSet(9, field, before, after), World());

        Assert.Equal(expected, result.Summary);
        Assert.DoesNotContain("c_", result.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("k_", result.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void CounterRemovalRemainsInTheImmediateRecapAlongsideDamage()
    {
        EventBatchPresentation result = EventCuePlanner.Plan(
            [new FieldSet(9, "c_web", 1, 0), new FieldSet(9, "hitPoints", 14, 6)], World(), Outcome.Unfinished);

        Assert.Equal([EventMotionKind.Counter, EventMotionKind.Damage], result.Highlights.Select(cue => cue.Motion));
        Assert.Equal("Rhino web counters: 1 → 0 (1 removed).", result.Highlights[0].Summary);
    }
}
