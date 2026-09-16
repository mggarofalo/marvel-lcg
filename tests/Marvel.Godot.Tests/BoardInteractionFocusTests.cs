using Marvel.Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class BoardInteractionFocusTests
{
    [Fact]
    public void OfferedStableCardAndIntentRestoresTheSameAttachedControl()
    {
        var requested = new BoardInteractionFocusKey(19, CardInteractionIntent.Generator);

        BoardInteractionFocusKey? restored = BoardInteractionFocus.Restore(requested,
        [
            new BoardInteractionFocusKey(19, CardInteractionIntent.Action),
            requested,
            new BoardInteractionFocusKey(10, CardInteractionIntent.Target),
        ]);

        Assert.Equal(requested, restored);
    }

    [Fact]
    public void MissingIntentKeepsFocusOnTheSameRepresentedCard()
    {
        BoardInteractionFocusKey? restored = BoardInteractionFocus.Restore(
            new BoardInteractionFocusKey(19, CardInteractionIntent.Target),
        [
            new BoardInteractionFocusKey(19, CardInteractionIntent.Generator),
            new BoardInteractionFocusKey(10, CardInteractionIntent.Target),
            new BoardInteractionFocusKey(10, CardInteractionIntent.Action),
        ]);

        Assert.Equal(new BoardInteractionFocusKey(19, CardInteractionIntent.Generator), restored);
    }

    [Fact]
    public void MissingAttachedControlDoesNotFocusWhenTheRefreshOffersNothing()
    {
        BoardInteractionFocusKey? restored = BoardInteractionFocus.Restore(
            new BoardInteractionFocusKey(19, CardInteractionIntent.Action), []);

        Assert.Null(restored);
    }

    [Fact]
    public void MissingSourceCardUsesTheFirstStableOfferedFallback()
    {
        BoardInteractionFocusKey? restored = BoardInteractionFocus.Restore(
            new BoardInteractionFocusKey(19, CardInteractionIntent.Action),
        [
            new BoardInteractionFocusKey(20, CardInteractionIntent.Generator),
            new BoardInteractionFocusKey(10, CardInteractionIntent.Target),
        ]);

        Assert.Equal(new BoardInteractionFocusKey(10, CardInteractionIntent.Target), restored);
    }
}
