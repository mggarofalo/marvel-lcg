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
    private Label selectedSummary = null!;
    private Main main = null!;
    private Node inspectorParent = null!;

    internal static CompleteDecisionSheet Open(DecisionPanel panel, Control opener)
    {
        var sheet = new CompleteDecisionSheet
        {
            panel = panel, opener = opener,
            main = ClientSceneHost.MainFor(panel),
            originalParent = panel.GetParent(), originalIndex = panel.GetIndex(),
            draft = panel.composer, focusGeneration = panel.GetRenderGeneration(),
        };
        sheet.overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        Node host = panel;
        while (host.GetParent() is Control parent) host = parent;
        sheet.layer = new CanvasLayer { Name = "CompleteChoicesLayer", Layer = 25 };
        host.AddChild(sheet.layer);
        sheet.layer.AddChild(sheet.overlay);
        sheet.inspectorParent = sheet.main.cardInspector.GetParent();
        sheet.main.cardInspector.Reparent(sheet.layer);
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
        header.AddChild(Heading());
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
                $"{prompt.Context} · {prompt.Requirement}",
                GodotThemeVariations.Caption, wrap: true));
            if (!DecisionCardChoices.IsChoice(composer.Prompt))
                content.AddChild(DecisionPanel.Text(prompt.Heading, GodotThemeVariations.Heading, wrap: true));
            if (!string.IsNullOrWhiteSpace(prompt.Resolution))
                content.AddChild(DecisionPanel.Text(prompt.Resolution, GodotThemeVariations.Body, wrap: true));
        }
        selectedSummary = DecisionPanel.Text(string.Empty, GodotThemeVariations.Body, wrap: true);
        selectedSummary.Name = "CompleteChoiceSummary";
        content.AddChild(selectedSummary);
        panel.ProgressChanged += RefreshSummary;
        RefreshSummary(null);
        panel.Reparent(content);
        panel.Visible = true;
        panel.CustomMinimumSize = Vector2.Zero;
        panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        panel.SetCompleteChoicesVisibility(true);
    }

    private void RefreshSummary(DecisionProgressPresentation? _)
    {
        var prompt = panel.composer is { } composer && panel.world is { } world
            ? Marvel.View.PromptPresentation.From(composer.Prompt, world) : null;
        selectedSummary.Text = TableDraftSummary.From(panel.composer, prompt, panel.world) ?? string.Empty;
        selectedSummary.Visible = selectedSummary.Text.Length > 0;
    }

    private Control openerFallback = null!;

    private Control Heading()
    {
        if (panel.composer is { } composer && panel.world is { } world)
        {
            var prompt = Marvel.View.PromptPresentation.From(composer.Prompt, world);
            if (prompt.ContextCards.Count == 1)
            {
                var title = new Button { Name = "DecisionSource", Text = prompt.ContextCards[0].Title,
                    Flat = true, Alignment = HorizontalAlignment.Left, AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, TooltipText = "Inspect card" };
                title.Pressed += () => main.boardController.ShowCardInspector(prompt.ContextCards[0], title, pinned: true);
                return title;
            }
        }
        return new Label { Text = Title(), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.Heading, AutowrapMode = TextServer.AutowrapMode.WordSmart };
    }

    private string Title()
    {
        if (panel.composer is not { } composer || panel.world is not { } world) return "Current choice";
        var prompt = Marvel.View.PromptPresentation.From(composer.Prompt, world);
        if (prompt.ContextCards.Count == 1) return prompt.ContextCards[0].Title;
        return DecisionCardChoices.IsChoice(composer.Prompt) ? DecisionCardChoices.Heading(composer.Prompt) : "Current choice";
    }

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
        Rect2 dialog = DecisionDialogLayout.Frame(viewport, panel.ReadingScale, panel.composer?.Prompt.Affordances.Count ?? 0);
        frame.Size = dialog.Size;
        frame.Position = dialog.Position;
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
        main.HideCardInspector();
        main.cardInspector.Reparent(inspectorParent);
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
        panel.ProgressChanged -= RefreshSummary;
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
