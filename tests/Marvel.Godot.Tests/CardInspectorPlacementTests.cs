using Godot;
using Marvel.Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardInspectorPlacementTests
{
    public static IEnumerable<object[]> Scales => VisualSystem.SupportedScales.Select(scale => new object[] { scale });

    [Theory]
    [MemberData(nameof(Scales))]
    public void TopRowPreviewUsesAvailableSpaceWithoutCroppingItsMeasuredContent(InterfaceScale scale)
    {
        float factor = (int)scale / 100f;
        Vector2 viewport = new(1920, 1080);
        Rect2 source = new(700, 45, 160, 180);
        Vector2 content = new(380 * factor, 550 * factor);

        Rect2 preview = CardInspectorPlacement.Fit(viewport, source, content);

        Assert.Equal(content, preview.Size);
        Assert.True(new Rect2(Vector2.Zero, viewport).Encloses(preview));
        Assert.False(preview.Intersects(source));
    }

    [Theory]
    [InlineData(12, 12)]
    [InlineData(1100, 12)]
    [InlineData(12, 650)]
    [InlineData(1100, 650)]
    public void EdgeSourcesKeepThePreviewInsideTheViewport(float x, float y)
    {
        Vector2 viewport = new(1280, 800);
        Rect2 source = new(x, y, 120, 130);

        Rect2 preview = CardInspectorPlacement.Fit(viewport, source, new Vector2(360, 650));

        Assert.Equal(new Vector2(360, 650), preview.Size);
        Assert.True(new Rect2(Vector2.Zero, viewport).Encloses(preview));
        Assert.False(preview.Intersects(source));
    }

    [Fact]
    public void MiddleHandPreviewLeavesNeighboringCardBodiesExposed()
    {
        Rect2 source = new(800, 650, 160, 220);
        Rect2[] neighbors = [new(630, 650, 160, 220), new(970, 650, 160, 220)];

        Rect2 preview = CardInspectorPlacement.Fit(new Vector2(1920, 1080),
            source, new Vector2(360, 550), neighbors);

        Assert.False(preview.Intersects(source));
        Assert.All(neighbors, card => Assert.False(preview.Intersects(card)));
        Assert.Equal(new Vector2(360, 550), preview.Size);
    }

    [Fact]
    public void PreviewProtectsDecisionButtonsWhenAvoidingAnotherCardWouldCoverThem()
    {
        Rect2 source = new(800, 650, 160, 220);
        Rect2[] cards = [new(630, 650, 160, 220), new(970, 650, 160, 220), new(800, 400, 160, 180)];
        Rect2[] actions = [new(760, 900, 260, 44)];

        Rect2 preview = CardInspectorPlacement.Fit(new Vector2(1920, 1080),
            source, new Vector2(360, 550), cards, actions);

        Assert.False(preview.Intersects(source));
        Assert.All(actions, action => Assert.False(preview.Intersects(action)));
    }

    [Fact]
    public void PreviewCanMoveBeyondSourceActionsIntoTheNextFreeColumn()
    {
        Rect2 source = new(710, 382, 176, 203);
        Rect2[] actions = [new(894, 382, 88, 44), new(894, 434, 88, 48),
            new(241, 164, 121, 126), new(1738, 96, 140, 44), new(1738, 298, 140, 44),
            new(1738, 466, 140, 44), new(1242, 899, 400, 44)];

        Rect2 preview = CardInspectorPlacement.Fit(new Vector2(1920, 1080),
            source, new Vector2(420, 573), actions: actions);

        Assert.False(preview.Intersects(source));
        Assert.All(actions, action => Assert.False(preview.Intersects(action)));
    }

    [Fact]
    public void CrowdedTableKeepsACompleteFaceInsteadOfCroppingForAnotherCard()
    {
        Vector2 content = new(420, 624);
        Rect2 source = new(700, 380, 240, 305);
        Rect2[] cards = [new(500, 700, 850, 300)];
        Rect2[] actions = [new(950, 400, 130, 60), new(1650, 80, 250, 800)];

        Rect2 preview = CardInspectorPlacement.Fit(new Vector2(1920, 1080), source, content, cards, actions);

        Assert.Equal(content, preview.Size);
        Assert.False(preview.Intersects(source));
        Assert.True(new Rect2(0, 0, 1920, 1080).Encloses(preview));
    }
}
