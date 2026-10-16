using Godot;
using Marvel.Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardInspectorFreeSpaceTests
{
    [Fact]
    public void AThinFreeSliverRequiresExplicitReadingModeInsteadOfAnUnreadableScrollWindow()
    {
        Assert.False(CardInspectorFreeSpace.Fit(new(1280, 800), new(340, 600),
            [new(0, 0, 1200, 800)]).HasArea());
    }

    [Fact]
    public void CompletelyOccupiedViewportUsesAnExplicitNoticeThenDeliberateReadingMode()
    {
        Vector2 viewport = new(1280, 800);
        Rect2 source = new(1150, 700, 100, 80);
        Vector2 content = new(340, 600);
        Assert.False(CardInspectorFreeSpace.Fit(viewport, content, [new(Vector2.Zero, viewport)]).HasArea());
        Rect2 notice = CardInspectorOverlayPlacement.Fit(viewport, source, content, pinned: false);
        Rect2 reading = CardInspectorOverlayPlacement.Fit(viewport, source, content, pinned: true);
        Assert.Equal(48, notice.Size.Y);
        Assert.True(notice.Intersects(source));
        Assert.Equal(content, reading.Size);
        Assert.True(new Rect2(Vector2.Zero, viewport).Encloses(notice));
        Assert.True(new Rect2(Vector2.Zero, viewport).Encloses(reading));
    }

    [Fact]
    public void SpaceForOnlyPartOfTheCardDoesNotBecomeAClippedPreview()
    {
        Rect2[] protectedRegions = [new(0, 0, 200, 800), new(600, 0, 680, 800),
            new(200, 500, 400, 300), new(200, 0, 400, 100)];
        Rect2 frame = CardInspectorFreeSpace.Fit(new(1280, 800), new(340, 600), protectedRegions);
        Assert.False(frame.HasArea());
    }

    [Fact]
    public void CompleteDetailFitsWithoutCoveringParticipants()
    {
        Rect2[] protectedRegions = [new(0, 0, 200, 800), new(600, 0, 680, 800),
            new(200, 500, 400, 300), new(200, 0, 400, 100)];
        Rect2 frame = CardInspectorFreeSpace.Fit(new(1280, 800), new(280, 350), protectedRegions);
        Assert.Equal(new Vector2(280, 350), frame.Size);
        Assert.All(protectedRegions, region => Assert.False(frame.Intersects(region)));
    }

    [Fact]
    public void SelectedRhinoIsAHardExclusionBesideThePaymentWorkspace()
    {
        Rect2 rhino = new(700, 100, 180, 240);
        Rect2 hand = new(180, 750, 1100, 280);
        Rect2 payment = new(1360, 88, 544, 960);
        Rect2 frame = CardInspectorPlacement.Fit(new(1920, 1080), new(1410, 300, 430, 60),
            new(400, 560), situation: [rhino, hand, payment]);
        Assert.Equal(new Vector2(400, 560), frame.Size);
        Assert.All(new[] { rhino, hand, payment }, region => Assert.False(frame.Intersects(region)));
    }
}
