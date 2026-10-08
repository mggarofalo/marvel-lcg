using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardInspectionSourceTests
{
    [Fact]
    public void ACurrentSourceOpensItsAuthorizedFullFaceIncludingCostAndCurrentCounters()
    {
        var full = new BoardCardPresentation(7, 1, false, "Current source", "", "UPGRADE", "EXHAUSTED", [])
        { Cost = "3", RulesText = "Complete source rules.", Counters = [new("USES", "2")] };
        var reference = new BoardCardPresentation(7, 1, false, "Current source", "", "SOURCE", "", [])
        { RulesText = "Complete source rules." };
        BoardPresentation board = new([new(1, "Area", "", [full], [])]);
        Assert.Same(full, BoardInspectorSequences.Source(board, reference));
        var sequences = new BoardInspectorSequences();
        sequences.Register([full]);
        Assert.Same(full, sequences.Source(reference));
    }

    [Fact]
    public void PublicHistoryDoesNotAcquireALiveTargetOrConcealedCurrentFace()
    {
        var hidden = new BoardCardPresentation(7, 1, true, "Hidden title", "", "CONCEALED", "", [])
        { RulesText = "Hidden rules." };
        var known = new BoardCardPresentation(7, 1, false, "Known earlier source", "", "SOURCE", "", [])
        { RulesText = "Public earlier rules." };
        BoardPresentation board = new([new(1, "Area", "", [hidden], [])]);
        BoardCardPresentation detail = BoardInspectorSequences.Source(board, known);
        Assert.Null(detail.TargetId);
        Assert.Equal("Public earlier rules.", detail.RulesText);
        Assert.Equal("Known earlier source", detail.Title);
        Assert.Null(BoardInspectorSequences.Source(board, known with { TargetId = null }).TargetId);
    }

    [Theory]
    [InlineData("Printed", "Base 28")]
    [InlineData("Replacement", "Base 28 · replacement")]
    [InlineData("Defined", "Base 28 · defined")]
    public void BaselineMeaningUsesTheSuppliedBaseWithoutReconstructingPrintedArithmetic(string kind, string expected)
    {
        Assert.Equal(expected, CardValueSourceInspection.BaseDescription(new(28, 31, kind, true, [])));
    }
}
