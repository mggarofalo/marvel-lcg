using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>A reusable procedural rendering of one visibility-safe card descriptor.</summary>
public sealed partial class CardControl : PanelContainer
{
    private string baseVariation = GodotThemeVariations.BoardCard;
    private bool highlighted;
    private bool presented;

    private CardControl()
    {
    }

    /// <summary>The engine handle used by prompt highlighting, when visible.</summary>
    public int? TargetId { get; private set; }

    /// <summary>Builds a card without consulting content or inferring hidden face data.</summary>
    public static CardControl Create(
        BoardCardPresentation card,
        CardDisplaySize size = CardDisplaySize.Board,
        InterfaceScale scale = InterfaceScale.Standard,
        ICardArtProvider? art = null)
    {
        ArgumentNullException.ThrowIfNull(card);
        CardLayoutMetrics layout = LayoutFor(card, size, scale);
        string variation = VariationFor(card, size);
        var control = new CardControl
        {
            Name = "ProceduralCard",
            TargetId = card.TargetId,
            CustomMinimumSize = new Vector2(
                layout.Width,
                card.Concealed
                    ? layout.Width * 0.72f
                    : size == CardDisplaySize.Full
                        ? layout.MinimumHeight
                        : CompactHeight(card, layout, size)),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            TooltipText = card.Title,
            FocusMode = card.Concealed ? FocusModeEnum.None : FocusModeEnum.All,
            MouseDefaultCursorShape = card.Concealed
                ? CursorShape.Arrow
                : CursorShape.PointingHand,
            baseVariation = variation,
            ThemeTypeVariation = variation,
        };
        Control body = CardFaceRendering.CreateBody(card, size, layout, scale, art);
        body.CustomMinimumSize = new Vector2(
            Math.Max(1, layout.Width - 32),
            Math.Max(1, layout.MinimumHeight - 32));
        control.AddChild(body);
        return control;
    }

    private static CardLayoutMetrics LayoutFor(
        BoardCardPresentation card,
        CardDisplaySize size,
        InterfaceScale scale)
    {
        CardLayoutMetrics layout = VisualSystem.Card(size, scale);
        return size == CardDisplaySize.Full
            && VisualSystem.CardFrame(card.Kind).Family == CardFrameFamily.Scheme
                ? layout with
                {
                    Width = layout.MinimumHeight,
                    MinimumHeight = layout.Width,
                }
                : layout;
    }

    private static string VariationFor(BoardCardPresentation card, CardDisplaySize size) =>
        card.Concealed
            ? GodotThemeVariations.ConcealedCard
            : size == CardDisplaySize.Full
                ? VisualSystem.CardFrame(card.Kind).ThemeVariation
                : GodotThemeVariations.BoardCard;

    internal static float CompactHeight(
        BoardCardPresentation card,
        CardLayoutMetrics layout,
        CardDisplaySize size)
    {
        IReadOnlyList<BoardFieldPresentation> values = CompactValues(card, size);
        int progressRows = values.Count(IsCompactProgressValue);
        int resourcesRows = values.Count(value => value.Name == "RES");
        int badgeCount = values.Count - progressRows - values.Count(value => value.Name == "RES");
        int valueRows = (badgeCount + 2) / 3 + progressRows + resourcesRows;
        int titleCharactersPerLine = size == CardDisplaySize.Hand ? 18 : 24;
        int titleRows = Math.Max(
            1,
            (int)Math.Ceiling(card.Title.Length / (double)titleCharactersPerLine));
        int textRows = (size == CardDisplaySize.Hand ? 1 : 0)
            + titleRows
            + (CompactState(card, size) is null ? 0 : 1)
            + valueRows;
        float scale = layout.Width / (size == CardDisplaySize.Hand ? 172.0f : 210.0f);
        return Math.Max(layout.MinimumHeight, textRows * 22 * scale + 20 * scale);
    }

    /// <summary>Applies or clears the prompt-anchor focus treatment.</summary>
    public void SetHighlighted(bool value)
    {
        highlighted = value;
        RefreshTreatment();
    }

    /// <summary>Applies or clears a transient event cue independently of prompt focus.</summary>
    public void SetPresented(bool value)
    {
        presented = value;
        RefreshTreatment();
    }

    private void RefreshTreatment() =>
        ThemeTypeVariation = highlighted || presented
            ? GodotThemeVariations.FocusedCard
            : baseVariation;


    internal static IReadOnlyList<BoardFieldPresentation> CompactValues(
        BoardCardPresentation card, CardDisplaySize size) =>
        CardValueRendering.CompactValues(card, size);

    internal static bool IsCompactProgressValue(BoardFieldPresentation value) =>
        CardValueRendering.IsCompactProgressValue(value);

    internal static string? CompactState(
        BoardCardPresentation card, CardDisplaySize size) =>
        CardValueRendering.CompactState(card, size);

}
