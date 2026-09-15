using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Adds an explicit current-threat marker to compact active schemes.</summary>
internal static class SchemeThreatBadge
{
    internal static BoardFieldPresentation? Add(
        VBoxContainer content,
        BoardCardPresentation card,
        IReadOnlyList<BoardFieldPresentation> values)
    {
        BoardFieldPresentation? threat =
            VisualSystem.CardFrame(card.Kind).Family == CardFrameFamily.Scheme
                ? values.FirstOrDefault(value => value.Name == "THREAT")
                : null;
        if (threat is not null)
        {
            content.AddChild(CardValueRendering.Label(
                LabelText(threat),
                GodotThemeVariations.DangerText,
                "SchemeThreatBadge"));
        }
        return threat;
    }

    internal static string LabelText(BoardFieldPresentation threat) =>
        $"THREAT  ◆ {threat.Value}";
}
