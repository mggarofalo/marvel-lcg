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
    private Control? tableDraft;

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
        HideTableDraft();
        overlay.Resized += Fit;
        Fit();
        Callable.From(FocusFirstChoice).CallDeferred();
    }

    private void AddBackdrop()
    {
        var backdrop = new ColorRect
        {
            Color = new Color(0.02f, 0.04f, 0.06f, 0.72f), MouseFilter = Control.MouseFilterEnum.Stop,
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
        var close = new Button { Name = "CloseChoiceSheet", Text = "Return to table" };
        openerFallback = close;
        close.Pressed += Close;
        content.AddChild(DecisionSheetHead.Create(panel, main, close));
        notice = DecisionPanel.Text(string.Empty, GodotThemeVariations.Body, wrap: true);
        notice.Name = "DecisionRecoveryNotice";
        notice.Visible = false;
        content.AddChild(notice);
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

    // The head already states the engine's instruction, so the draft summary leaves it out.
    private void RefreshSummary(DecisionProgressPresentation? _)
    {
        var prompt = panel.composer is { } composer && panel.world is { } world
            ? Marvel.View.PromptPresentation.From(composer.Prompt, world) : null;
        selectedSummary.Text = TableDraftSummary.From(panel.composer, prompt, panel.world,
            shownInstruction: prompt?.Frame.Instruction) ?? string.Empty;
        selectedSummary.Visible = selectedSummary.Text.Length > 0;
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

    // The sheet shows the draft in full, so the table's one-line copy of it steps aside until the sheet closes.
    private void HideTableDraft()
    {
        if (FindInHost("ContextualDecision") is not Control draftRow || !draftRow.Visible) return;
        tableDraft = draftRow;
        tableDraft.Visible = false;
    }

    private void RestoreTableDraft()
    {
        if (tableDraft is not null && GodotObject.IsInstanceValid(tableDraft) && !tableDraft.IsQueuedForDeletion())
            tableDraft.Visible = true;
        tableDraft = null;
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
        RestoreTableDraft();
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
