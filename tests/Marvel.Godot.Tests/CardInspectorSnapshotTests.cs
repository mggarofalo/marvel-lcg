using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardInspectorSnapshotTests
{
    [Fact]
    public void AReturnedCopyUsesOnlyTheNewSnapshotsState()
    {
        var old = new BoardCardPresentation(7, 1, false, "Source", "", "UPGRADE", "EXHAUSTED", [])
        { Counters = [new("USES", "4")], Statuses = ["Stunned"] };
        var current = old with { Status = "READY", Counters = [], Statuses = [] };
        BoardPresentation board = new([new(1, "Area", "", [current], [old])]);
        Assert.Same(current, BoardInspectorSequences.Current(board, 7));
        Assert.Empty(BoardInspectorSequences.Current(board, 7)!.Counters);
        Assert.Empty(BoardInspectorSequences.Current(board, 7)!.Statuses);
    }

    [Fact]
    public void RemovedConcealedAndUnlinkedHistoricalSourcesCannotKeepAPinnedLiveInspector()
    {
        var removed = new BoardCardPresentation(7, 1, false, "Old source", "", "", "", []);
        var concealed = removed with { Concealed = true };
        BoardPresentation board = new([new(1, "Area", "", [concealed], [removed])]);
        Assert.Null(BoardInspectorSequences.Current(board, 7));
        Assert.Null(BoardInspectorSequences.Current(board, null));
        Assert.Null(BoardInspectorSequences.Current(board, 8));
    }
}
