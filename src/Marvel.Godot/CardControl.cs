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
            CustomMinimumSize = SpatialCardMetrics.FaceSize(card, size, layout, scale),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            TooltipText = card.Title,
            FocusMode = card.Concealed ? FocusModeEnum.None : FocusModeEnum.All,
            MouseFilter = MouseFilterEnum.Pass,
            MouseDefaultCursorShape = card.Concealed
                ? CursorShape.Arrow
                : CursorShape.PointingHand,
            baseVariation = variation,
            ThemeTypeVariation = variation,
            Theme = ClientTheme.Create(scale),
        };
        control.FocusEntered += control.QueueRedraw;
        control.FocusExited += control.QueueRedraw;
        control.AddThemeStyleboxOverride("panel", CardFaceStyle.Frame(card));
        var surface = new Control
        {
            Name = "CardSurface", MouseFilter = MouseFilterEnum.Pass,
            CustomMinimumSize = control.CustomMinimumSize - new Vector2(8, 8),
        };
        control.AddChild(surface);
        Control body = CardFaceRendering.CreateBody(card, size, layout, scale, art);
        surface.AddChild(body);
        if (body.FindChild("ResourceIcons", true, false) is Control resources)
            control.SetMeta("card_resource_rect", new Rect2(resources.Position + new Vector2(4, 4), resources.Size));
        if (card.Concealed)
        {
            body.Size = surface.CustomMinimumSize;
            body.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        }
        control.interactionLabel = new Label
        {
            Name = "InteractionCue", Position = new Vector2(layout.Width - 36, 4),
            Size = new Vector2(28, 28), HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore, Visible = !card.Concealed,
        };
        control.interactionLabel.AddThemeColorOverride("font_color", CardFaceStyle.Ink);
        control.interactionLabel.AddThemeColorOverride("font_outline_color", CardFaceStyle.Paper);
        control.interactionLabel.AddThemeConstantOverride("outline_size", 4);
        control.interactionLabel.AddThemeFontSizeOverride("font_size", 18);
        surface.AddChild(control.interactionLabel);
        control.interactionControls = new GridContainer
        {
            Name = "DirectControls", Columns = 2, MouseFilter = MouseFilterEnum.Pass,
            Position = new Vector2(layout.Width - 52, -28),
            Size = new Vector2(44, 44),
        };
        surface.AddChild(control.interactionControls);
        control.Size = control.CustomMinimumSize;
        return control;
    }

    /// <summary>Keeps keyboard focus distinct from prompt selection on the same face.</summary>
    public override void _Draw()
    {
        CardInteractionDrawing.Draw(this, interactionCue, highlighted || presented);
        if (HasFocus()) DrawRect(new Rect2(new Vector2(-4, -4), Size + new Vector2(8, 8)),
            Colors.White, filled: false, width: 1.5f);
    }

    internal static CardLayoutMetrics LayoutFor(
        BoardCardPresentation card,
        CardDisplaySize size,
        InterfaceScale scale)
    {
        CardLayoutMetrics layout = VisualSystem.Card(size, scale);
        return size is CardDisplaySize.Full or CardDisplaySize.Board
            && VisualSystem.CardFrame(card.Kind).Family == CardFrameFamily.Scheme
                ? layout with
                {
                    Width = layout.MinimumHeight,
                    MinimumHeight = layout.Width,
                }
                : layout;
    }

    internal static string VariationFor(BoardCardPresentation card, CardDisplaySize size) =>
        card.Concealed
            ? GodotThemeVariations.ConcealedCard
            : VisualSystem.CardFrame(card.Kind).ThemeVariation;

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
        cue.AccessibilityName = CueDescription(value);
        cue.TooltipText = cue.AccessibilityName;
        bool resource = (value & (CardInteractionCue.LegalGenerator | CardInteractionCue.SelectedGenerator)) != 0;
        cue.Position = resource && HasMeta("card_resource_rect")
            ? GetMeta("card_resource_rect").AsRect2().End - new Vector2(24, 28)
            : new Vector2(Size.X - 36, 4);

        RefreshTreatment();
    }

    /// <summary>Keeps the local highlight while an adjacent control carries the same text.</summary>
    internal void HideRedundantActionCueLabel()
    {
        if (interactionCue == CardInteractionCue.OfferedAction
            && InteractionControl.IsUsable(interactionLabel))
        {
            interactionLabel!.Text = string.Empty;
        }
    }

    /// <summary>Reserves the cue strip for its explicit hand control.</summary>
    internal void HideInteractionCue()
    {
        if (interactionLabel is not null) interactionLabel.Visible = false;
    }

    /// <summary>Adds a prompt-authorized symbol at the card edge.</summary>
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
        cue.Visible = false;

        CardSymbolButtonStyle.Apply(control);
        if (GetParent() is Container parent)
        {
            parent.QueueSort();
        }
        directControls.Columns = directControls.GetChildCount() == 0 ? 1 : 2;
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
        if (interactionLabel is not null)
            interactionLabel.Visible = baseVariation != GodotThemeVariations.ConcealedCard;

        if (GetParent() is Container parent)
        {
            parent.QueueSort();
        }
    }

    private void RefreshTreatment()
    {
        ThemeTypeVariation = baseVariation;
        QueueRedraw();
    }

    private static string CueText(CardInteractionCue cue) =>
        (cue & (CardInteractionCue.SelectedTarget | CardInteractionCue.SelectedGenerator
            | CardInteractionCue.SelectedDestructiveChoice)) != 0 ? "✓"
        : cue.HasFlag(CardInteractionCue.LegalTarget) ? "◎"
        : cue.HasFlag(CardInteractionCue.OfferedAction) ? "↗"
        : cue.HasFlag(CardInteractionCue.Unavailable) ? "×" : "";

    private static string CueDescription(CardInteractionCue cue) =>
        cue.HasFlag(CardInteractionCue.SelectedTarget) ? "Selected target"
        : cue.HasFlag(CardInteractionCue.SelectedGenerator) ? "Selected payment source"
        : cue.HasFlag(CardInteractionCue.SelectedDestructiveChoice) ? "Staged discard"
        : cue.HasFlag(CardInteractionCue.LegalTarget) ? "Available target"
        : cue.HasFlag(CardInteractionCue.OfferedAction) ? "Available action"
        : cue.HasFlag(CardInteractionCue.Unavailable) ? "Unavailable" : "";
}
