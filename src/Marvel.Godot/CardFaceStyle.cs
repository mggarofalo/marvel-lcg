using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Printed card identity colors; interaction cues are a separate visual layer.</summary>
internal static class CardFaceStyle
{
    internal static Color Paper => ClientTheme.ToGodot(CardVisualTokens.Paper);
    internal static Color Ink => ClientTheme.ToGodot(CardVisualTokens.Ink);
    internal static Color Accent(BoardCardPresentation card) => ClientTheme.ToGodot(
        CardVisualTokens.Aspect(card.Classification, VisualSystem.CardFrame(card.Kind).Family));

    internal static StyleBoxFlat Frame(BoardCardPresentation card) => new()
    {
        BgColor = card.Concealed ? Ink : Accent(card), BorderColor = Ink,
        BorderWidthLeft = CardVisualTokens.FrameStroke,
        BorderWidthRight = CardVisualTokens.FrameStroke,
        BorderWidthTop = CardVisualTokens.FrameStroke,
        BorderWidthBottom = CardVisualTokens.FrameStroke,
        CornerRadiusTopLeft = CardVisualTokens.FrameRadius,
        CornerRadiusTopRight = CardVisualTokens.FrameRadius,
        CornerRadiusBottomLeft = CardVisualTokens.FrameRadius,
        CornerRadiusBottomRight = CardVisualTokens.FrameRadius,
        ContentMarginLeft = CardVisualTokens.FrameInset,
        ContentMarginRight = CardVisualTokens.FrameInset,
        ContentMarginTop = CardVisualTokens.FrameInset,
        ContentMarginBottom = CardVisualTokens.FrameInset,
    };
}
