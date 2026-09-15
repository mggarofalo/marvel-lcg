using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>A reusable procedural rendering of one visibility-safe card descriptor.</summary>
public sealed partial class CardControl : PanelContainer
{
    private string baseVariation = GodotThemeVariations.BoardCard;
    private bool highlighted;
    private bool presented;
    private CardInteractionCue interactionCue;
    private Label? interactionLabel;
    private GridContainer? interactionControls;
    private InterfaceScale interactionScale;

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
            MouseFilter = MouseFilterEnum.Pass,
            MouseDefaultCursorShape = card.Concealed
                ? CursorShape.Arrow
                : CursorShape.PointingHand,
            baseVariation = variation,
            ThemeTypeVariation = variation,
            interactionScale = scale,
        };
        var content = new VBoxContainer
        {
            Name = "CardContent",
            ThemeTypeVariation = GodotThemeVariations.TightStack,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        Control body = CardFaceRendering.CreateBody(card, size, layout, scale, art);
        body.CustomMinimumSize = new Vector2(
            Math.Max(1, layout.Width - 32),
            Math.Max(1, control.CustomMinimumSize.Y - 32));
        content.AddChild(body);
        control.interactionLabel = new Label
        {
            Name = "InteractionCue",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = MouseFilterEnum.Pass,
            ThemeTypeVariation = GodotThemeVariations.Eyebrow,
            Visible = false,
        };
        content.AddChild(control.interactionLabel);
        control.interactionControls = new GridContainer
        {
            Name = "DirectControls",
            Columns = 2,
            MouseFilter = MouseFilterEnum.Pass,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        var surface = new VBoxContainer
        {
            Name = "CardSurface",
            MouseFilter = MouseFilterEnum.Pass,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        control.AddChild(surface);
        surface.AddChild(content);
        surface.AddChild(control.interactionControls);
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
        int titleCharactersPerLine = size is CardDisplaySize.Hand or CardDisplaySize.Mulligan ? 14 : 18;
        int titleRows = Math.Max(
            1,
            (int)Math.Ceiling(card.Title.Length / (double)titleCharactersPerLine));
        int textRows = (size is CardDisplaySize.Hand or CardDisplaySize.Mulligan ? 1 : 0)
            + titleRows
            + (CompactState(card, size) is null ? 0 : 1)
            + valueRows;
        float scale = layout.Width / (size is CardDisplaySize.Hand or CardDisplaySize.Mulligan ? 144.0f : 156.0f);
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

    /// <summary>Shows prompt-authorized interaction state with a readable marker as well as color.</summary>
    internal void SetInteractionCue(CardInteractionCue value)
    {
        Label? cue = interactionLabel;
        if (cue is null
            || !InteractionControl.IsUsable(this)
            || !InteractionControl.IsUsable(cue))
        {
            return;
        }

        interactionCue = value;
        cue.Text = CueText(value);
        cue.Visible = value != CardInteractionCue.None;
        RefreshTreatment();
    }

    /// <summary>Adds a prompt-authorized control in this card's reserved action strip.</summary>
    internal bool AddInteractionControl(Button control)
    {
        ArgumentNullException.ThrowIfNull(control);
        Label? cue = interactionLabel;
        GridContainer? directControls = interactionControls;
        if (cue is null
            || directControls is null
            || !InteractionControl.IsUsable(this)
            || !InteractionControl.IsUsable(cue)
            || !InteractionControl.IsUsable(directControls))
        {
            control.QueueFree();
            return false;
        }

        interactionCue &= ~CardInteractionCue.OfferedAction;
        cue.Text = CueText(interactionCue);
        cue.Visible = interactionCue != CardInteractionCue.None;
        control.CustomMinimumSize = new Vector2(
            0, VisualSystem.Controls(interactionScale).MinimumPointerTarget);
        control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        if (GetParent() is Container parent)
        {
            parent.QueueSort();
        }
        directControls.AddChild(control);
        return true;
    }

    /// <summary>Removes a prior prompt's controls and restores this card's base surface height.</summary>
    internal void ClearInteractionControls()
    {
        if (interactionControls is not null)
        {
            foreach (Node child in interactionControls.GetChildren())
            {
                interactionControls.RemoveChild(child);
                child.QueueFree();
            }
        }

        if (GetParent() is Container parent)
        {
            parent.QueueSort();
        }
    }

    private void RefreshTreatment() =>
        ThemeTypeVariation = highlighted || presented || interactionCue != CardInteractionCue.None
            ? GodotThemeVariations.FocusedCard
            : baseVariation;

    private static string CueText(CardInteractionCue cue)
    {
        var labels = new List<string>();
        if (cue.HasFlag(CardInteractionCue.Unavailable)) labels.Add("— UNAVAILABLE");
        if (cue.HasFlag(CardInteractionCue.SelectedDestructiveChoice)) labels.Add("✓ DISCARD");
        else if (cue.HasFlag(CardInteractionCue.DestructiveChoice)) labels.Add("◇ DISCARD");
        if (cue.HasFlag(CardInteractionCue.OfferedAction)) labels.Add("◇ ACTION");
        if (cue.HasFlag(CardInteractionCue.SelectedTarget)) labels.Add("✓ TARGET");
        else if (cue.HasFlag(CardInteractionCue.LegalTarget)) labels.Add("◇ TARGET");
        if (cue.HasFlag(CardInteractionCue.SelectedGenerator)) labels.Add("✓ RESOURCE");
        else if (cue.HasFlag(CardInteractionCue.LegalGenerator)) labels.Add("◇ RESOURCE");
        return string.Join("  ", labels);
    }


    internal static IReadOnlyList<BoardFieldPresentation> CompactValues(
        BoardCardPresentation card, CardDisplaySize size) =>
        CardValueRendering.CompactValues(card, size);

    internal static bool IsCompactProgressValue(BoardFieldPresentation value) =>
        CardValueRendering.IsCompactProgressValue(value);

    internal static string? CompactState(
        BoardCardPresentation card, CardDisplaySize size) =>
        CardValueRendering.CompactState(card, size);

}
