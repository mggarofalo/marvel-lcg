using Godot;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class SearchChoiceLayoutTests
{
    [Theory]
    [InlineData(PublicDecisionKind.CardSearch, false, true)]
    [InlineData(PublicDecisionKind.CardLook, false, true)]
    [InlineData(PublicDecisionKind.Choice, true, false)]
    [InlineData(PublicDecisionKind.PlayerAction, false, false)]
    public void GalleryAdmissionUsesPublicPurposeWithoutPrivateLedgerMetadata(
        PublicDecisionKind kind, bool exposure, bool expected)
    {
        var prompt = new Prompt(0, Question.Option, TimingPriority.Untimed,
            "fixture", "fixture", false, [])
        {
            PublicKind = kind, ExposesConcealedCandidates = exposure,
        };
        Assert.Equal(expected, SearchChoiceGallery.IsChoice(prompt));
    }

    [Theory]
    [InlineData(1920, 1080, 4)]
    [InlineData(1280, 720, 3)]
    [InlineData(1060, 720, 2)]
    public void SearchOffersSeveralReadableWholeCardsWithoutViewportOverflow(int width, int height, int count)
    {
        var viewport = new Vector2(width, height);
        Rect2 frame = SearchChoiceLayout.Frame(viewport);
        CardLayoutMetrics card = VisualSystem.Card(CardDisplaySize.Full, SearchChoiceLayout.CardScale(viewport));
        Assert.Equal(count, SearchChoiceLayout.Capacity(viewport));
        Assert.True(new Rect2(Vector2.Zero, viewport).Encloses(frame));
        Assert.True(count * (card.Width + 24) + 64 <= frame.Size.X);
        Assert.True(card.MinimumHeight + 200 <= frame.Size.Y);
        Assert.True(card.Width >= 320);
    }
}
