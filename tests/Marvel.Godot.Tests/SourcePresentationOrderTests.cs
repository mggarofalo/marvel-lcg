using Marvel.Godot;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class SourcePresentationOrderTests
{
    [Fact]
    public void LocalOrderReconcilesDrawsAndRemovalsWithoutChangingOtherSeats()
    {
        SourcePresentationOrder.Store(901, [3, 1, 2]);
        SourcePresentationOrder.Store(902, [8, 9]);
        Assert.Equal([3, 1, 4], SourcePresentationOrder.Arrange(901, [Card(1), Card(3), Card(4)]).Select(card => card.TargetId));
        Assert.Equal([8, 9], SourcePresentationOrder.Arrange(902, [Card(9), Card(8)]).Select(card => card.TargetId));
    }

    private static BoardCardPresentation Card(int id) => new(id, 1, false, $"Card {id}", "", "", "", []);
}
