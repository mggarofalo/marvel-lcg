using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardControlTests
{
    [Fact]
    public void CardGeometryDoesNotMoveWhenAVisibleTitleChanges()
    {
        CardLayoutMetrics layout = VisualSystem.Card(
            CardDisplaySize.Hand, InterfaceScale.Standard);
        BoardCardPresentation shortTitle = Card("EVENT");
        BoardCardPresentation longTitle = shortTitle with
        {
            Title = "A Very Long Opening Hand Card Title That Must Wrap Without Truncation "
                + "Across Enough Lines To Exceed Even The Reserved Portrait Card Height",
        };

        Assert.Equal(
            SpatialCardMetrics.FaceSize(longTitle, CardDisplaySize.Hand, layout, InterfaceScale.Standard),
            SpatialCardMetrics.FaceSize(shortTitle, CardDisplaySize.Hand, layout, InterfaceScale.Standard));
    }

    private static BoardCardPresentation Card(
        string kind,
        IReadOnlyList<BoardFieldPresentation>? fields = null) => new(
            TargetId: 1,
            Count: 1,
            Concealed: false,
            Title: "Test card",
            Subtitle: string.Empty,
            Kind: kind,
            Status: string.Empty,
            Fields: fields ?? []);
}
