using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class LocalDecisionContinuityTests
{
    [Fact]
    public void OnlySubmittedSequentialFollowupRetainsSourceWithoutRetainingAnAnswer()
    {
        var state = Started();
        state.Submit();
        state.Accept(8);
        Assert.True(state.Refresh(0, PublicDecisionKind.Choice, 8));
        Assert.True(state.Committed);
        Assert.False(state.Awaiting);
        Assert.Equal(42, state.Source);
        Assert.Equal("Play event", state.Origin);
        state.Submit();
        state.Accept(9);
        Assert.True(state.Refresh(0, PublicDecisionKind.Response, 9));
        Assert.False(state.Refresh(0, PublicDecisionKind.PlayerAction, 10));
    }

    [Theory]
    [InlineData(0, PublicDecisionKind.Choice, 9)]
    [InlineData(1, PublicDecisionKind.Choice, 8)]
    [InlineData(0, PublicDecisionKind.PlayerAction, 8)]
    [InlineData(null, null, 8)]
    public void UnrelatedRevisionSeatOrTurnCannotInheritTheMenu(int? seat, PublicDecisionKind? kind, long revision)
    {
        var state = Started();
        state.Submit();
        state.Accept(8);
        Assert.False(state.Refresh(seat, kind, revision));
        Assert.False(state.Active);
        Assert.False(state.Awaiting);
    }

    [Fact]
    public void ExternalRevisionCannotTurnAnUnsubmittedDraftIntoAContinuation()
    {
        var state = Started();
        Assert.False(state.Refresh(0, PublicDecisionKind.Choice, 8));
        Assert.False(state.Active);
    }

    [Fact]
    public void SameRevisionRefreshPreservesContextButNeverCommitsIt()
    {
        var state = Started();
        Assert.True(state.Refresh(0, PublicDecisionKind.PlayerAction, 7));
        Assert.False(state.Committed);
        Assert.False(state.Awaiting);
        state.Clear();
        Assert.False(state.Refresh(0, PublicDecisionKind.PlayerAction, 7));
    }

    [Fact]
    public void RejectedSubmissionCannotClaimALaterExternalDecisionAsItsFollowup()
    {
        var state = Started();
        state.Submit();
        state.Reject();
        Assert.True(state.Refresh(0, PublicDecisionKind.PlayerAction, 7));
        Assert.False(state.Awaiting);
        Assert.False(state.Refresh(0, PublicDecisionKind.Choice, 8));
    }

    [Fact]
    public void UnconfirmedMutationDoesNotClaimSynchronizedRevisionAsItsOwn()
    {
        var state = Started();
        state.Submit();
        Assert.False(state.Refresh(0, PublicDecisionKind.Choice, 8));
        Assert.False(state.Committed);
    }

    private static LocalDecisionContinuity Started()
    {
        var state = new LocalDecisionContinuity();
        state.Begin(42, 0, 7, "Play event", PublicDecisionKind.PlayerAction);
        return state;
    }

    [Fact]
    public void FreshTableActionLeavesThePreviousLocalIntention()
    {
        var state = Started();
        Assert.True(state.LeaveForTableAction(PublicDecisionKind.PlayerAction));
        Assert.False(state.Active);
        Assert.False(state.Awaiting);
        Assert.False(state.Refresh(0, PublicDecisionKind.PlayerAction, 7));
    }

    [Theory]
    [InlineData(PublicDecisionKind.Choice)]
    [InlineData(PublicDecisionKind.Response)]
    public void ChoosingWithinAcceptedEffectRetainsItsLocalOrigin(PublicDecisionKind kind)
    {
        var state = Started();
        state.Submit();
        state.Accept(8);
        Assert.True(state.Refresh(0, kind, 8));
        Assert.False(state.LeaveForTableAction(kind));
        Assert.True(state.Active);
        Assert.True(state.Committed);
        Assert.Equal("Play event", state.Origin);
    }

    [Theory]
    [InlineData(PublicDecisionKind.Choice)]
    [InlineData(PublicDecisionKind.Response)]
    public void ReanchoringNestedMenuOrDragRetainsTheAcceptedSource(PublicDecisionKind kind)
    {
        var state = Started();
        state.Submit();
        state.Accept(8);
        Assert.True(state.Refresh(0, kind, 8));
        state.Begin(99, 0, 8, "Select target", kind);
        Assert.True(state.Active);
        Assert.True(state.Committed);
        Assert.Equal(42, state.Source);
        Assert.Equal("Play event", state.Origin);
    }
}
