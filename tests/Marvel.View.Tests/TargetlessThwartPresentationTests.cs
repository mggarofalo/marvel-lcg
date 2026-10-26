using Marvel.Rules.Events;
using Marvel.Rules.State;
using Xunit;
using static Marvel.View.Tests.VisibilityFixture;

namespace Marvel.View.Tests;

public sealed class TargetlessThwartPresentationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PublicTargetlessThwartRetainsItsCauseWithoutInventingATarget(bool cooperative)
    {
        var world = Board();
        var actor = world.TheCardIn(DeckType.VillainArea)!;
        var scope = cooperative
            ? new PermissiveVisibilityPolicy().Authorize(null, world.Players)
            : new RestrictedVisibilityPolicy(0).Authorize(null, world.Players);
        var result = new FieldSet(actor.ObjectId, "health", 1, 2)
        {
            Cause = new EventCause(17, EventCauseKind.Thwart, actor.ObjectId, -1)
            { SourceTitle = "Black Widow", TargetTitle = "must not survive without a target" },
        };
        var projected = WorldProjection.For(world, null, [result], scope);
        var safe = Assert.Single(projected.Events);
        Assert.Equal(-1, safe.Cause!.Target);
        Assert.Equal(string.Empty, safe.Cause.TargetTitle);
        var presentation = EventPresenter.Present(safe, projected.World);
        Assert.Equal("Black Widow thwarts", presentation.Intention);
        Assert.DoesNotContain(-1, presentation.Anchors);
        Assert.DoesNotContain(presentation.Activity!.Subjects, subject => subject.Card == -1);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void ConcealedCauseParticipantCannotDiscloseItsIdentity(int target)
    {
        var world = Board();
        var visible = world.TheCardIn(DeckType.VillainArea)!;
        var hidden = Assert.Single(world.Seats[1].Hand.Cards);
        int actor = target == -1 ? hidden.ObjectId : visible.ObjectId;
        int recipient = target == -1 ? -1 : hidden.ObjectId;
        var happened = new FieldSet(visible.ObjectId, "health", 1, 2)
        {
            Cause = new EventCause(17, EventCauseKind.Thwart, actor, recipient)
            { SourceTitle = "secret source", TargetTitle = "secret recipient" },
        };
        var scope = new RestrictedVisibilityPolicy(0).Authorize(null, world.Players);
        var projected = WorldProjection.For(world, null, [happened], scope);
        var safe = Assert.Single(projected.Events);
        Assert.Null(safe.Cause);
        Assert.Null(EventPresenter.Present(safe, projected.World).Intention);
    }
}
