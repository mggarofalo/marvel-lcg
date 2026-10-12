using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Keeps the unpaid card beside payment and overlays exact source previews.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "The workspace's scene tree owns and frees the native controls.")]
internal sealed class CardPaymentPreview
{
    private readonly Main main;
    private readonly DecisionPanel decision;
    private readonly Control workspace;
    private readonly PanelContainer frame;
    private readonly VBoxContainer column;
    private readonly Button toggle;
    private readonly PanelContainer sourceFrame;
    private readonly VBoxContainer sourceColumn;
    private readonly int played;
    private Control? face;
    private int? source;

    internal CardPaymentPreview(Main main, DecisionPanel decision, Control workspace, int played)
    {
        this.main = main;
        this.decision = decision;
        this.workspace = workspace;
        this.played = played;
        var layer = new CanvasLayer { Name = "PaymentPreviewLayer", Layer = 75 };
        workspace.AddChild(layer);
        frame = new PanelContainer { Name = "PaymentCardPreview", Theme = decision.Theme,
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel };
        layer.AddChild(frame);
        sourceFrame = new PanelContainer { Name = "PaymentSourcePreview", Theme = decision.Theme,
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel, Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(sourceFrame);
        sourceColumn = new VBoxContainer();
        sourceFrame.AddChild(sourceColumn);
        column = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        frame.AddChild(column);
        toggle = new Button { Name = "TogglePlayedCardPreview", FocusMode = Control.FocusModeEnum.All };
        column.AddChild(toggle);
        toggle.Pressed += () => { decision.PaymentPreviewExpanded = !decision.PaymentPreviewExpanded; Render(); };
        frame.Resized += () => Callable.From(Position).CallDeferred();
        sourceFrame.Resized += () => Callable.From(Position).CallDeferred();
        workspace.ItemRectChanged += () => Callable.From(Position).CallDeferred();
        Render();
    }

    internal static BoardCardPresentation? Card(BoardPresentation? board, int id) =>
        board?.Areas.SelectMany(area => area.Cards)
            .FirstOrDefault(card => card.TargetId == id && !card.Concealed);

    internal void ShowSource(int? id)
    {
        if (!InteractionControl.IsUsable(frame) || source == id) return;
        source = id;
        foreach (Node child in sourceColumn.GetChildren()) { sourceColumn.RemoveChild(child); child.QueueFree(); }
        BoardCardPresentation? card = id is { } exact ? Card(main.boardPresentation, exact) : null;
        sourceFrame.Visible = card is not null;
        frame.SetMeta("previewed_card_anchor", card?.TargetId ?? played);
        if (card is not null)
        {
            Control detail = CreateFace(card);
            sourceColumn.AddChild(detail);
        }
        sourceFrame.ResetSize();
        Callable.From(Position).CallDeferred();
    }

    private void Render()
    {
        if (face is not null) { column.RemoveChild(face); face.QueueFree(); face = null; }
        BoardCardPresentation? playing = Card(main.boardPresentation, played);
        toggle.Text = $"{(decision.PaymentPreviewExpanded ? "▾" : "▸")} Playing {playing?.Title ?? "card"}";
        toggle.AccessibilityName = $"{(decision.PaymentPreviewExpanded ? "Collapse" : "Expand")} played card preview";
        frame.SetMeta("previewed_card_anchor", played);
        if (playing is not null && decision.PaymentPreviewExpanded) AddFace(playing);
        frame.ResetSize();
        Callable.From(Position).CallDeferred();
    }

    private void AddFace(BoardCardPresentation card)
    {
        face = CreateFace(card);
        column.AddChild(face);
    }

    private Control CreateFace(BoardCardPresentation card)
    {
        InterfaceScale scale = CardInspectorFocus.FittedScale(card, InterfaceScale.Standard,
            main.GetViewportRect().Size.Y - PreviewTop() - 90);
        Control detail = CardControl.Create(card, CardDisplaySize.Full, scale, main.art);
        CardInspectorFocus.IgnoreMouseRecursively(detail, interactiveRules: false);
        return detail;
    }

    private float PreviewTop() =>
        main.FindChild("ToggleHistory", true, false) is Control history && history.IsVisibleInTree()
            ? Math.Max(8, history.GetGlobalRect().End.Y + 12) : 8;

    private void Position()
    {
        if (!InteractionControl.IsUsable(frame) || !workspace.IsInsideTree()) return;
        Rect2 area = workspace.GetGlobalRect();
        Vector2 viewport = workspace.GetViewportRect().Size;
        float top = PreviewTop();
        frame.Position = new Vector2(Math.Max(8, area.Position.X - frame.Size.X - 12),
            Math.Clamp(area.Position.Y, top, Math.Max(top, viewport.Y - frame.Size.Y - 8)));
        sourceFrame.Position = new Vector2(Math.Max(8, frame.Position.X - sourceFrame.Size.X - 12),
            Math.Clamp(area.Position.Y, top, Math.Max(top, viewport.Y - sourceFrame.Size.Y - 8)));
    }
}
