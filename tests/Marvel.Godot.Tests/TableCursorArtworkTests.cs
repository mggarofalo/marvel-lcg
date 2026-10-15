using System.Xml.Linq;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TableCursorArtworkTests
{
    [Fact]
    public void EveryPointerHasOriginalPackagedVectorArtAndAnInBoundsClickAnchor()
    {
        Assert.Equal(7, TableCursorArtwork.Names.Count);
        foreach (string name in TableCursorArtwork.Names)
        {
            XElement svg = XElement.Parse(TableCursorArtwork.Svg(name));
            Assert.Equal("0 0 32 32", svg.Attribute("viewBox")!.Value);
            Assert.NotEmpty(svg.Elements());
            foreach (InterfaceScale scale in VisualSystem.SupportedScales)
            {
                int size = TableCursorArtwork.PixelSize(scale);
                (float x, float y) = TableCursorArtwork.Hotspot(name, size);
                Assert.InRange(size, 32, 48);
                Assert.InRange(x, 0, size - 1);
                Assert.InRange(y, 0, size - 1);
            }
        }
    }

    [Theory]
    [InlineData(InterfaceScale.Percent50, 32)]
    [InlineData(InterfaceScale.Percent100, 32)]
    [InlineData(InterfaceScale.Percent120, 40)]
    [InlineData(InterfaceScale.Percent150, 48)]
    public void PointerArtworkIsRasterizedAtAnEvenNativeSize(InterfaceScale scale, int size) =>
        Assert.Equal(size, TableCursorArtwork.PixelSize(scale));

    [Fact]
    public void DragValidityChangesBadgeWithoutMovingTheGrabAnchor()
    {
        foreach (int size in new[] { 32, 40, 48 })
        {
            var anchor = TableCursorArtwork.Hotspot("grab", size);
            Assert.Equal(anchor, TableCursorArtwork.Hotspot("drag", size));
            Assert.Equal(anchor, TableCursorArtwork.Hotspot("drop", size));
            Assert.Equal(anchor, TableCursorArtwork.Hotspot("forbidden", size));
        }
        Assert.NotEqual(TableCursorArtwork.Svg("drag"), TableCursorArtwork.Svg("drop"));
        Assert.NotEqual(TableCursorArtwork.Svg("drop"), TableCursorArtwork.Svg("forbidden"));
    }
}
