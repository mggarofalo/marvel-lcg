using Marvel.Godot;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardPaymentPreviewTests
{
    [Fact]
    public void DuplicateTitlesKeepExactPaymentIdentityAndConcealedCardsHaveNoPreview()
    {
        var first = new BoardCardPresentation(10, 1, false, "Same title", "", "UPGRADE", "READY", []);
        var second = first with { TargetId = 11, Status = "EXHAUSTED" };
        var hidden = first with { TargetId = 12, Concealed = true };
        var board = new BoardPresentation([new BoardAreaPresentation(0, "Hand", "", [first, second, hidden], [])]);
        Assert.Same(second, CardPaymentPreview.Card(board, 11));
        Assert.Equal("EXHAUSTED", CardPaymentPreview.Card(board, 11)!.Status);
        Assert.Null(CardPaymentPreview.Card(board, 12));
        Assert.Null(CardPaymentPreview.Card(board, 99));
    }
}
