using Marvel.Godot;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class HandPresentationOrderTests
{
    [Fact]
    public void LocalOrderReconcilesDrawsAndRemovalsWithoutChangingOtherSeats()
    {
        HandPresentationOrder.Store(901, [3, 1, 2]);
        HandPresentationOrder.Store(902, [8, 9]);
        Assert.Equal([3, 1, 4], HandPresentationOrder.Arrange(901, [Card(1), Card(3), Card(4)]).Select(card => card.TargetId));
        Assert.Equal([8, 9], HandPresentationOrder.Arrange(902, [Card(9), Card(8)]).Select(card => card.TargetId));
    }

    [Fact]
    public void RepeatedDropStagesOnceAndDropBackRestoresTheSameDraft()
    {
        var draft = new PointerMulliganDraft();
        var chosen = new List<int>();
        draft.Bind([1, 2], [], id => chosen.Add(id));
        Assert.True(draft.Drop(1, true, false));
        draft.SetSelected([1]);
        Assert.True(draft.Drop(1, true, false));
        Assert.Equal([1], chosen);
        Assert.True(draft.Drop(1, false, true));
        Assert.Equal([1, 1], chosen);
        Assert.False(draft.Drop(9, true, false));
    }

    private static BoardCardPresentation Card(int id) => new(id, 1, false, $"Card {id}", "", "", "", []);
}
