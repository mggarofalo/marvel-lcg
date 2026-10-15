using Marvel.Rules.Events;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

public sealed class EventActivityProjectionTests : EventPresentationTestBase
{
    [Fact]
    public void AttackParticipantsRemainDistinctEvenWhenTargetDefendsItself()
    {
        var attack = new AttackCompleted(9, 7, 7) { DamageDealt = 2 };
        EventActivityDescriptor activity = EventPresenter.Present(attack, World()).Activity!;
        Assert.Equal([ActivitySubjectRole.Source, ActivitySubjectRole.Target, ActivitySubjectRole.Defender],
            activity.Subjects.Select(subject => subject.Role));
        Assert.Equal([9, 7, 7], activity.Subjects.Select(subject => subject.Card));
        Assert.Equal(new ActivityQuantity("attack_damage", null, 2), Assert.Single(activity.Quantities));
        Assert.Equal("Rhino", activity.Subjects[0].Name);
    }

    [Fact]
    public void BoostQuantitiesHaveTheirExactEngineSuppliedMeaning()
    {
        var activity = EventPresenter.Present(new BoostResolved(7, 9, 2, false, 5), World()).Activity!;
        Assert.Equal(ActivitySubjectRole.Boost, activity.Subjects[0].Role);
        Assert.Equal(ActivitySubjectRole.Target, activity.Subjects[1].Role);
        Assert.Equal([new ActivityQuantity("boost_icons", null, 2), new ActivityQuantity("SCH", null, 5)], activity.Quantities);
    }

    [Theory]
    [InlineData(null, 3L)]
    [InlineData(0L, 3L)]
    [InlineData(3L, null)]
    public void NumericEvidencePreservesAbsentVersusZero(long? before, long? after)
    {
        var activity = EventPresenter.Present(new FieldSet(9, "k_threat", before, after), World()).Activity!;
        Assert.Equal(new ActivityQuantity("k_threat", before, after), Assert.Single(activity.Quantities));
    }

    [Fact]
    public void HistoricalNameHasNoFaceLookupCapabilityAndMovementRetainsEndpoints()
    {
        var moved = Move(7, "HandsArea", "DiscardPile", "Play") with
            { Subjects = new Dictionary<int, string> { [7] = "Occurrence identity" } };
        var activity = EventPresenter.Present(moved, World()).Activity!;
        Assert.Equal("Occurrence identity", Assert.Single(activity.Subjects).Name);
        Assert.Equal("Play", activity.Operation);
        Assert.Contains("hand", activity.From);
        Assert.Contains("discard", activity.To);
        Assert.Empty(activity.Quantities);
        Assert.Null(Assert.Single(activity.Subjects).Reference);
    }

    [Fact]
    public void StatusAppliedAndConsumedPreserveDirectionAndHost()
    {
        var applied = new CardsCreated(AreaRef.Scenario("StatusArea"), [new CreatedCard(12, "tough")]);
        var attached = new CardAttached(12, 7);
        var batch = EventCuePlanner.Plan([applied, attached], World(), Marvel.Rules.Play.Outcome.Unfinished);
        var cue = Assert.Single(batch.Cues);
        Assert.Equal(ActivityStatusChange.Applied, cue.Activity!.StatusChange);
        Assert.Contains(cue.Activity.Subjects, subject => subject.Card == 7 && subject.Role == ActivitySubjectRole.Target);
        var consumed = new CardsMoved(AreaRef.Scenario("StatusArea"), AreaRef.Scenario("RemovedFromGameArea"), [new Landing(12, 0)]);
        Assert.Equal(ActivityStatusChange.Removed, EventPresenter.Present(consumed, World()).Activity!.StatusChange);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FilteredEventScopeControlsEveryTypedNameAndQuantity(bool cooperative)
    {
        var world = VisibilityFixture.Board();
        int hidden = world.Seats[1].Hand.Cards[0].ObjectId;
        IVisibilityPolicy policy = cooperative ? new PermissiveVisibilityPolicy() : new RestrictedVisibilityPolicy(0);
        var visible = WorldProjection.For(world, null, [new FieldSet(hidden, "health", 1, 2)
            { Subjects = new Dictionary<int, string> { [hidden] = "Private occurrence" } }], policy.Authorize(null, world.Players));
        var entries = EventPresenter.Present(visible.Events, visible.World);
        if (!cooperative) Assert.Empty(entries);
        else
        {
            var activity = Assert.Single(entries).Activity!;
            Assert.Equal("Private occurrence", Assert.Single(activity.Subjects).Name);
            Assert.Equal(new ActivityQuantity("health", 1, 2), Assert.Single(activity.Quantities));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CausalNamesAreRemovedWhenEitherParticipantIsPrivate(bool cooperative)
    {
        var world = VisibilityFixture.Board();
        int privateCard = world.Seats[1].Hand.Cards[0].ObjectId;
        int source = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        IVisibilityPolicy policy = cooperative ? new PermissiveVisibilityPolicy() : new RestrictedVisibilityPolicy(0);
        var changed = new FieldSet(source, "health", 10, 9)
        { Cause = new(15, EventCauseKind.Thwart, source, privateCard) { SourceTitle = "Hero", TargetTitle = "Private identity" } };
        var projected = WorldProjection.For(world, null, [changed], policy.Authorize(null, world.Players));
        var retained = Assert.Single(projected.Events);
        if (cooperative) Assert.Equal("Hero thwarts Private identity", EventPresenter.Present(retained, projected.World).Intention);
        else { Assert.Null(retained.Cause); Assert.Null(EventPresenter.Present(retained, projected.World).Intention); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AttackBoundariesCannotExposeAnUnauthorizedParticipant(bool cooperative)
    {
        var world = VisibilityFixture.Board();
        int privateCard = world.Seats[1].Hand.Cards[0].ObjectId;
        int source = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        IVisibilityPolicy policy = cooperative ? new PermissiveVisibilityPolicy() : new RestrictedVisibilityPolicy(0);
        var boundary = new AttackBoundary(new(17, true), source, privateCard, true)
        { AttackIdentity = new(17, true), Subjects = new Dictionary<int, string> { [privateCard] = "Private participant" } };
        var projected = WorldProjection.For(world, null, [boundary], policy.Authorize(null, world.Players));
        if (!cooperative) Assert.Empty(projected.Events);
        else
        {
            var retained = Assert.IsType<AttackBoundary>(Assert.Single(projected.Events));
            Assert.Equal(boundary.Identity, retained.Identity);
            Assert.Equal(privateCard, retained.Target);
        }
    }
}
