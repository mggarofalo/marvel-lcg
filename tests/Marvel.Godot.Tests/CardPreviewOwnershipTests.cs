using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardPreviewOwnershipTests
{
    [Fact]
    public void StalePreviewSourceCannotDismissTheCurrentPreview()
    {
        var ownership = new CardPreviewOwnership();
        object firstSource = new();
        object currentSource = new();
        var changes = new List<int?>();
        ownership.Changed += changes.Add;

        ownership.Show(firstSource, 1);
        ownership.Show(currentSource, 2);

        ownership.Dismiss(firstSource);
        ownership.Dismiss(currentSource);

        Assert.Equal([1, 2, null], changes);
    }

    [Fact]
    public void ClearingThePanelRevokesTheCurrentPreviewSource()
    {
        var ownership = new CardPreviewOwnership();
        object source = new();
        var changes = new List<int?>();
        ownership.Changed += changes.Add;

        ownership.Show(source, 3);

        ownership.Clear();
        ownership.Dismiss(source);
        ownership.Clear();

        Assert.Equal([3, null], changes);
    }
}
