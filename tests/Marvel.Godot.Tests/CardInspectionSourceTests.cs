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
        var sequences = new BoardInspectorSequences(board);
        Assert.Empty(sequences.For(full.TargetId));
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

    [Fact]
    public void ANewSnapshotDoesNotRetainRemovedConcealedOrHistoricalSourceTargets()
    {
        var full = new BoardCardPresentation(7, 1, false, "Visible source", "", "UPGRADE", "", [])
        { Cost = "3", RulesText = "Public current rules." };
        var reference = full with { Kind = "SOURCE", Cost = null };
        var prior = new BoardInspectorSequences(new([new(1, "Area", "", [full], [])]));
        Assert.Same(full, prior.Source(reference));
        foreach (BoardPresentation next in new BoardPresentation[]
        {
            new([]),
            new([new(1, "Area", "", [], [full])]),
            new([new(1, "Area", "", [full with { Concealed = true, Title = "Secret", RulesText = "Secret rules" }], [])]),
        })
        {
            var current = new BoardInspectorSequences(next);
            // A region cache must never authorize a source missing from the current snapshot.
            current.Register([full]);
            BoardCardPresentation detail = current.Source(reference);
            Assert.Null(detail.TargetId);
            Assert.Null(detail.Cost);
            Assert.DoesNotContain("Secret", detail.Title + detail.RulesText);
        }
        Assert.Null(prior.Source(reference with { TargetId = null }).TargetId);
        Assert.Null(new BoardInspectorSequences().Source(reference).TargetId);
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
