using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Printed card identity colors; interaction cues are a separate visual layer.</summary>
internal static class CardFaceStyle
{
    internal static Color Paper => new("faf8f0");
    internal static Color Ink => new("191c20");
    internal static Color Accent(BoardCardPresentation card) => card.Classification.ToUpperInvariant() switch
    {
        "AGGRESSION" => new("be3c37"), "JUSTICE" => new("d4a725"),
        "LEADERSHIP" => new("2d73b9"), "PROTECTION" => new("4f983b"),
        "BASIC" => new("80878a"), "HERO" => new("6a3c85"),
        _ => VisualSystem.CardFrame(card.Kind).Family is CardFrameFamily.Enemy or CardFrameFamily.Scheme
            or CardFrameFamily.Environment ? new("b93635") : new("6a3c85"),
    };

    internal static StyleBoxFlat Frame(BoardCardPresentation card) => new()
    {
        BgColor = card.Concealed ? new Color("19323e") : Accent(card), BorderColor = Ink,
        BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
        CornerRadiusTopLeft = 7, CornerRadiusTopRight = 7, CornerRadiusBottomLeft = 7, CornerRadiusBottomRight = 7,
        ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 4, ContentMarginBottom = 4,
    };
}
