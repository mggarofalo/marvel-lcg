using Marvel.Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class HistoryVisibleRangeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4000000)]
    [InlineData(7999520)]
    public void OneHundredThousandOccurrencesNeedOnlyViewportSizedNativeRows(int offset)
    {
        (int first, int count) = HistoryVisibleRange.At(100000, offset, 480);
        Assert.Equal(offset / 80, first);
        Assert.InRange(count, 1, 7);
        Assert.True(first + count <= 100000);
    }

    [Fact]
    public void UndoPastTheCurrentScrollPositionClampsWithoutInventingRows()
    {
        Assert.Equal((2, 1), HistoryVisibleRange.At(3, 4000000, 480));
        Assert.Equal((0, 0), HistoryVisibleRange.At(0, 4000000, 480));
    }
}
