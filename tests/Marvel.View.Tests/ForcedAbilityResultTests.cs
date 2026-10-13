using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.View.Tests;

public sealed class ForcedAbilityResultTests
{
    [Fact]
    public void ActualDiscardAndReturnReplacePrintedInstructionsAndKeepOrderedDetail()
    {
        var world = new WorldDescriptor([], [], [], Outcome.Unfinished);
        var discard = Movement("PlayerDeck", "DiscardPile", "Discard", [new(10, 0), new(11, 1)]);
        var returned = Movement("DiscardPile", "HandsArea", "Add_To_Hand", [new(11, 0)]);
        AbilityWindowResult receipt = Receipt();
        EventBatchPresentation batch = EventCuePlanner.Plan([discard, returned, receipt], world, Outcome.Unfinished);
        EventPresentation result = Assert.Single(batch.Cues);
        Assert.Contains("discarded Energy and Genius", result.Summary);
        Assert.Contains("added Genius to their hand", result.Summary);
        Assert.DoesNotContain("printed instructions", result.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("printed instructions", result.ForcedAbilityNotification!.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, result.Details.Count);
        Assert.Equal([10, 11], result.Details[0].Anchors);
        Assert.Equal([11], result.Details[1].Anchors);
    }

    [Fact]
    public void SimilarAdjacentMovementAndDifferentOccurrencesAreNotAttributedToTheAbility()
    {
        var world = new WorldDescriptor([], [], [], Outcome.Unfinished);
        CardsMoved own = Movement("PlayerDeck", "DiscardPile", "Discard", [new(10, 0)]);
        CardsMoved unrelated = own with { AbilityOccurrence = 44 };
        EventBatchPresentation batch = EventCuePlanner.Plan([unrelated, own, Receipt()], world, Outcome.Unfinished);
        Assert.Equal(2, batch.Cues.Count);
        Assert.Null(batch.Cues[0].ForcedAbilityNotification);
        Assert.Single(batch.Cues[1].Details);
    }

    [Fact]
    public void DifferentAbilitiesOnTheSameCardAndOccurrenceKeepTheirOwnOutcomes()
    {
        var world = new WorldDescriptor([], [], [], Outcome.Unfinished);
        CardsMoved first = Movement("PlayerDeck", "DiscardPile", "Discard", [new(10, 0)]);
        CardsMoved second = first with { AbilityOrdinal = 1, Cards = [new(11, 0)] };
        EventBatchPresentation batch = EventCuePlanner.Plan([first, Receipt(), second, Receipt() with { Ordinal = 1 }],
            world, Outcome.Unfinished);
        Assert.Equal(2, batch.Cues.Count);
        Assert.Contains("Energy", batch.Cues[0].Summary);
        Assert.DoesNotContain("Genius", batch.Cues[0].Summary);
        Assert.Contains("Genius", batch.Cues[1].Summary);
        Assert.DoesNotContain("Energy", batch.Cues[1].Summary);
    }

    [Fact]
    public void PrivateResultsCannotLeakThroughAPublicForcedReceipt()
    {
        World world = VisibilityFixture.Board();
        Card source = world.TheCardIn(DeckType.VillainArea)!;
        Card hidden = world.Seats[1].Hand.Cards[0];
        var receipt = Receipt() with { Source = source.ObjectId };
        var secret = new FieldSet(hidden.ObjectId, "health", 2, 1)
        { AbilitySource = source.ObjectId, AbilityOccurrence = receipt.Occurrence };
        VisibleResult visible = WorldProjection.For(world, null, [secret, receipt],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players));
        EventPresentation result = Assert.Single(EventCuePlanner.Plan(visible.Events, visible.World, Outcome.Unfinished).Cues);
        Assert.Empty(result.Details);
        Assert.DoesNotContain("one-hand", result.Summary);
    }

    [Fact]
    public void PublicResultDoesNotRevealItsPrivateAbilitySourceIdentity()
    {
        World world = VisibilityFixture.Board();
        Card source = world.Seats[1].Hand.Cards[0];
        Card target = world.TheCardIn(DeckType.VillainArea)!;
        var happened = new FieldSet(target.ObjectId, "health", 2, 1)
        { AbilitySource = source.ObjectId, AbilityOccurrence = 42, AbilityTiming = AbilityType.ForcedResponse, AbilityOrdinal = 1 };
        VisibleResult visible = WorldProjection.For(world, null, [happened],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players));
        GameEvent safe = Assert.Single(visible.Events);
        Assert.Null(safe.AbilitySource);
        Assert.Null(safe.AbilityOccurrence);
        Assert.Null(safe.AbilityTiming);
        Assert.Null(safe.AbilityOrdinal);
    }

    [Fact]
    public void ForcedNotificationRetainsOnlyTheExplicitOccurrenceSourceFace()
    {
        World world = VisibilityFixture.Board();
        Card source = world.TheCardIn(DeckType.VillainArea)!;
        var receipt = Receipt() with { Source = source.ObjectId, SubjectFaces = OccurrenceFaces.For(source) };
        VisibleResult visible = WorldProjection.For(world, null, [receipt],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players));
        var notification = Assert.Single(EventPresenter.Present(visible.Events, visible.World)).ForcedAbilityNotification!;
        Assert.Equal(source.FaceId, notification.SourceCard!.FaceId);
        Assert.Null(notification.SourceCard.TargetId);
        Assert.Empty(notification.SourceCard.Fields);
        var withoutGrant = receipt with { SubjectFaces = null };
        Assert.Null(EventPresenter.Present(withoutGrant, visible.World).ForcedAbilityNotification!.SourceCard);
    }

    private static AbilityWindowResult Receipt() => new(2, AbilityType.ForcedResponse, 0, 42,
        "Printed instructions: discard the top 2 cards and add each printed mental resource", true)
    { Subjects = new Dictionary<int, string> { [2] = "Black Cat" }, Verb = "Ability_Resolved" };

    private static CardsMoved Movement(string from, string to, string verb, Landing[] cards) =>
        new(new AreaRef(from, 0, -1), new AreaRef(to, 0, -1), cards)
        {
            Verb = verb, AbilitySource = 2, AbilityOccurrence = 42,
            AbilityTiming = AbilityType.ForcedResponse, AbilityOrdinal = 0,
            Subjects = new Dictionary<int, string> { [10] = "Energy", [11] = "Genius" },
        };
}
