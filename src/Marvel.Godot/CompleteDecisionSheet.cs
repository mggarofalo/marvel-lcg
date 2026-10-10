using Godot;
using Marvel.Decisions;
using Marvel.Client;

namespace Marvel.Godot;

/// <summary>Shows the existing prompt editor without making a second draft or commitment.</summary>
internal sealed class CompleteDecisionSheet : IDisposable
{
    private readonly Control overlay = new() { Name = "CompleteDecisionSheetOverlay" };
    private DecisionPanel panel = null!;
    private Node originalParent = null!;
    private int originalIndex;
    private Control opener = null!;
    private CanvasLayer layer = null!;
    private PanelContainer frame = null!;
    private DecisionComposer? draft;
    private int focusGeneration;
    private Label notice = null!;

    internal static CompleteDecisionSheet Open(DecisionPanel panel, Control opener)
    {
        var sheet = new CompleteDecisionSheet
        {
            panel = panel, opener = opener,
            originalParent = panel.GetParent(), originalIndex = panel.GetIndex(),
            draft = panel.composer, focusGeneration = panel.GetRenderGeneration(),
        };
        sheet.overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        Node host = panel;
        while (host.GetParent() is Control parent) host = parent;
        sheet.layer = new CanvasLayer { Name = "CompleteChoicesLayer", Layer = 25 };
        host.AddChild(sheet.layer);
        sheet.layer.AddChild(sheet.overlay);
        sheet.overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        sheet.Build();
        return sheet;
    }

    private void Build()
    {
        AddBackdrop();
        AddFrame();
        overlay.Resized += Fit;
        Fit();
        Callable.From(FocusFirstChoice).CallDeferred();
    }

    private void AddBackdrop()
    {
        var backdrop = new ColorRect
        {
            Color = new Color(0.02f, 0.04f, 0.06f, 0.4f), MouseFilter = Control.MouseFilterEnum.Stop,
        };
        overlay.AddChild(backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        backdrop.GuiInput += input =>
        {
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            {
                overlay.GetViewport().SetInputAsHandled();
                Close();
            }
        };
    }

    private void AddFrame()
    {
        frame = new PanelContainer
        {
            Name = "CompleteChoicesFrame", ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
            Theme = panel.Theme,
        };
        overlay.AddChild(frame);
        frame.MinimumSizeChanged += () => Callable.From(Fit).CallDeferred();
        // Container layout can clamp a resize against a transient child minimum.
        // Refit after its actual size settles as well as when the minimum changes.
        frame.Resized += () => Callable.From(Fit).CallDeferred();
        var content = new VBoxContainer
        {
            Name = "CompleteChoicesContent", ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        frame.AddChild(content);
        var header = new HBoxContainer();
        header.AddChild(new Label
        {
            Text = DecisionCardChoices.Heading(panel.composer?.Prompt),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.Heading,
        });
        var close = new Button { Name = "CloseChoiceSheet", Text = "Return to table" };
        openerFallback = close;
        close.Pressed += Close;
        header.AddChild(close);
        content.AddChild(header);
        notice = DecisionPanel.Text(string.Empty, GodotThemeVariations.Body, wrap: true);
        notice.Name = "DecisionRecoveryNotice";
        notice.Visible = false;
        content.AddChild(notice);
        if (panel.composer is { } composer && panel.world is { } world)
        {
            Marvel.View.PromptPresentation prompt = Marvel.View.PromptPresentation.From(composer.Prompt, world);
            content.AddChild(DecisionPanel.Text(
                $"{prompt.Heading}\n{prompt.Context}\n{prompt.Resolution}",
                GodotThemeVariations.Caption, wrap: true));
        }
        panel.Reparent(content);
        panel.Visible = true;
        panel.CustomMinimumSize = Vector2.Zero;
        panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        panel.SetCompleteChoicesVisibility(true);
    }

    private Control openerFallback = null!;

    internal void PresentNotice(string copy)
    {
        notice.Text = copy;
        notice.Visible = notice.Text.Length > 0;
    }

    internal static string RecoveryCopy(GameProgressPresentation progress) => progress.Kind is
        GameProgressKind.DecisionNotSent or GameProgressKind.DecisionRejected
        or GameProgressKind.Unconfirmed or GameProgressKind.SynchronizationUnavailable
        or GameProgressKind.ServiceUnavailable or GameProgressKind.VersionMismatch
        or GameProgressKind.SessionUnavailable or GameProgressKind.StorageFailure
        or GameProgressKind.Unavailable
            ? $"{progress.Title}\n{progress.Description}" : string.Empty;

    private void FocusFirstChoice()
    {
        if (!InteractionControl.IsUsable(overlay) || !panel.CompleteChoicesOpen
            || !ReferenceEquals(panel.composer, draft)
            || panel.GetRenderGeneration() != focusGeneration) return;
        Button? first = panel.FindChildren("*", "Button", true, false)
            .OfType<Button>().FirstOrDefault(button => button.IsVisibleInTree() && !button.Disabled);
        Control focus = first as Control ?? openerFallback;
        if (InteractionControl.IsUsable(focus)) focus.GrabFocus();
    }

    private Node? FindInHost(string name)
    {
        Node host = layer.GetParent();
        return host.FindChild(name, recursive: true, owned: false);
    }

    private void Fit()
    {
        if (!InteractionControl.IsUsable(frame)) return;
        Vector2 viewport = overlay.GetViewportRect().Size;
        if (DecisionCardChoices.IsChoice(panel.composer?.Prompt))
        {
            Rect2 bounds = CardChoiceLayout.Frame(viewport);
            frame.Size = bounds.Size;
            frame.Position = bounds.Position;
            panel.CardChoices.RefreshLayout();
            return;
        }
        frame.Size = new Vector2(Math.Min(560, viewport.X - 32), Math.Min(560, Math.Max(220, viewport.Y * 0.52f)));
        frame.Position = new Vector2(viewport.X - frame.Size.X - 16, 72);
    }

    internal void Input(InputEvent input)
    {
        if (input.IsActionPressed("ui_cancel"))
        {
            overlay.GetViewport().SetInputAsHandled();
            Close();
        }
        else if (input is InputEventKey { Keycode: Key.Tab, Pressed: true } key)
        {
            Control[] controls = [.. frame.FindChildren("*", "Control", true, false)
                .OfType<Control>().Where(control => control.IsVisibleInTree()
                    && control.FocusMode == Control.FocusModeEnum.All
                    && control is not BaseButton { Disabled: true })];
            if (controls.Length == 0) return;
            int index = Array.IndexOf(controls, overlay.GetViewport().GuiGetFocusOwner());
            controls[(index + (key.ShiftPressed ? controls.Length - 1 : 1)) % controls.Length].GrabFocus();
            overlay.GetViewport().SetInputAsHandled();
        }
    }

    internal void Close()
    {
        if (overlay.IsQueuedForDeletion() || layer.IsQueuedForDeletion()) return;
        panel.Reparent(originalParent);
        originalParent.MoveChild(panel, Math.Min(originalIndex, originalParent.GetChildCount() - 1));
        panel.Visible = false;
        panel.CompleteChoicesClosed();
        panel.SetCompleteChoicesVisibility(false);
        RestoreOpenerFocus();
        Dispose();
    }

    public void Dispose()
    {
        if (GodotObject.IsInstanceValid(layer) && !layer.IsQueuedForDeletion()) layer.QueueFree();
    }

    private void RestoreOpenerFocus()
    {
        Control? restore = CanRestoreFocus(opener) ? opener
            : FindInHost("CompleteChoiceSheet") as Control;
        int generation = panel.GetRenderGeneration();
        DecisionComposer? current = panel.composer;
        if (restore is not null) Callable.From(() =>
        {
            if (InteractionControl.IsUsable(restore) && !panel.CompleteChoicesOpen
                && ReferenceEquals(panel.composer, current)
                && panel.GetRenderGeneration() == generation) restore.GrabFocus();
        }).CallDeferred();
    }

    private static bool CanRestoreFocus(Control control) =>
        InteractionControl.IsUsable(control) && control.IsVisibleInTree()
        && control.FocusMode == Control.FocusModeEnum.All;
}
