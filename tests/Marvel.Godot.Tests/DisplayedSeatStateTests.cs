using Marvel.Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DisplayedSeatStateTests
{
    [Fact]
    public void UserWorkspaceChoiceRemainsExpandedWhilePromptOwnershipStaysDistinct()
    {
        var state = new DisplayedSeatState();
        DisplayedSeatSnapshot waiting = Snapshot("game-a", [0, 1], promptOwner: null,
            activePlayer: 0, viewedPrivateSeat: 0, publicFocusSeat: 0);
        state.Select(1, waiting);

        DisplayedSeatSelection prompted = state.Update(Snapshot("game-a", [0, 1],
            promptOwner: 0, activePlayer: 0, viewedPrivateSeat: 0, publicFocusSeat: 0));
        DisplayedSeatSelection released = state.Update(waiting);

        Assert.Equal(1, prompted.ExpandedSeat);
        Assert.Equal(0, prompted.PromptOwner);
        Assert.Equal(1, released.ExpandedSeat);
    }

    [Fact]
    public void SeatBoundPromptChoosesItsOwnerBeforeTheUserSelectsAWorkspace()
    {
        var state = new DisplayedSeatState();

        DisplayedSeatSelection prompted = state.Update(Snapshot("game-a", [0, 1],
            promptOwner: 1, activePlayer: 0, viewedPrivateSeat: 0, publicFocusSeat: 0));

        Assert.Equal(1, prompted.ExpandedSeat);
        Assert.Equal(1, prompted.PromptOwner);
    }

    [Fact]
    public void StableRevisionAndResizeSnapshotsPreserveTheSelectedWorkspace()
    {
        var state = new DisplayedSeatState();
        DisplayedSeatSnapshot revisionSeven = Snapshot("game-a", [0, 1], promptOwner: null,
            activePlayer: 0, viewedPrivateSeat: 0, publicFocusSeat: 0);
        state.Select(1, revisionSeven);

        DisplayedSeatSelection revisionEight = state.Update(Snapshot("game-a", [0, 1],
            promptOwner: null, activePlayer: 1, viewedPrivateSeat: 0, publicFocusSeat: 1));
        DisplayedSeatSelection resized = state.Update(Snapshot("game-a", [0, 1],
            promptOwner: null, activePlayer: 0, viewedPrivateSeat: 0, publicFocusSeat: 0));

        Assert.Equal(1, revisionEight.ExpandedSeat);
        Assert.Equal(1, resized.ExpandedSeat);
    }

    [Fact]
    public void SelectionReportsEachAuthorizedSeatRoleWithoutConflatingTheirPurposes()
    {
        var state = new DisplayedSeatState();

        DisplayedSeatSelection selection = state.Update(Snapshot("game-a", [0, 1, 2, 3],
            promptOwner: 3, activePlayer: 2, viewedPrivateSeat: 0, publicFocusSeat: 1));

        Assert.Equal(3, selection.ExpandedSeat);
        Assert.Equal(2, selection.ActivePlayer);
        Assert.Equal(3, selection.PromptOwner);
        Assert.Equal(0, selection.ViewedPrivateSeat);
        Assert.Equal(1, selection.PublicFocusSeat);
    }

    [Fact]
    public void PublicFocusThenActivePlayerProvideTheNonPromptFallback()
    {
        var state = new DisplayedSeatState();

        DisplayedSeatSelection publicFocus = state.Update(Snapshot("game-a", [0, 1, 2],
            promptOwner: null, activePlayer: 2, viewedPrivateSeat: 0, publicFocusSeat: 1));
        DisplayedSeatSelection active = new DisplayedSeatState().Update(Snapshot("game-a", [0, 1, 2],
            promptOwner: null, activePlayer: 2, viewedPrivateSeat: 0, publicFocusSeat: 8));

        Assert.Equal(1, publicFocus.ExpandedSeat);
        Assert.Equal(2, active.ExpandedSeat);
    }

    [Fact]
    public void InvalidSelectedSeatResetsToTheDeterministicAvailableFallback()
    {
        var state = new DisplayedSeatState();
        state.Select(2, Snapshot("game-a", [0, 1, 2], promptOwner: null,
            activePlayer: 0, viewedPrivateSeat: 0, publicFocusSeat: 0));

        DisplayedSeatSelection missing = state.Update(Snapshot("game-a", [1, 0],
            promptOwner: null, activePlayer: 8, viewedPrivateSeat: 9, publicFocusSeat: 7));
        DisplayedSeatSelection returned = state.Update(Snapshot("game-a", [2, 1, 0],
            promptOwner: null, activePlayer: 1, viewedPrivateSeat: 0, publicFocusSeat: 8));

        Assert.Equal(0, missing.ExpandedSeat);
        Assert.Equal(1, returned.ExpandedSeat);
    }

    [Fact]
    public void GameOrSessionChangeResetsThePriorUserWorkspaceChoice()
    {
        var state = new DisplayedSeatState();
        state.Select(1, Snapshot("game-a", [0, 1], promptOwner: null,
            activePlayer: 0, viewedPrivateSeat: 0, publicFocusSeat: 0));

        DisplayedSeatSelection changed = state.Update(Snapshot("game-b", [0, 1],
            promptOwner: null, activePlayer: 0, viewedPrivateSeat: 0, publicFocusSeat: 0));

        Assert.Equal(0, changed.ExpandedSeat);
    }

    private static DisplayedSeatSnapshot Snapshot(
        string sessionKey,
        IReadOnlyList<int> seats,
        int? promptOwner,
        int activePlayer,
        int? viewedPrivateSeat,
        int publicFocusSeat) => new(
            sessionKey,
            seats,
            new DisplayedSeatRoles(
                promptOwner,
                viewedPrivateSeat,
                activePlayer,
                publicFocusSeat));
}
