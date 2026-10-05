using Godot;
using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Contains card-play composition, keyboard focus and reversible cancellation.</summary>
internal sealed class CardPaymentModal : IDisposable
{
    private readonly DecisionPanel panel;
    private readonly PanelContainer frame;
    private readonly Control overlay;
    private readonly CardPlayStaging staging;
    private readonly DecisionComposer draft;
    private readonly int generation;
    internal VBoxContainer Content { get; }

    internal CardPaymentModal(DecisionPanel panel, PromptPresentation prompt)
    {
        this.panel = panel;
        draft = panel.composer!;
        generation = panel.GetRenderGeneration();
        var layer = new CanvasLayer { Name = "PaymentLayer", Layer = 30 };
        panel.AddChild(layer);
        overlay = new Control { Name = "PaymentModal", MouseFilter = Control.MouseFilterEnum.Stop };
        layer.AddChild(overlay);
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var backdrop = new ColorRect { Color = new Color(0.02f, 0.04f, 0.06f, 0.8f) };
        overlay.AddChild(backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        backdrop.GuiInput += input =>
        {
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) Cancel();
        };
        staging = new CardPlayStaging(panel, overlay);
        frame = new PanelContainer
        {
            Name = "PaymentFrame",
            Theme = panel.Theme,
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
        };
        overlay.AddChild(frame);
        Content = new VBoxContainer { Name = "PaymentContent", ThemeTypeVariation = GodotThemeVariations.TightStack };
        frame.AddChild(Content);
        string title = PromptPresentation.Describe(panel.composer!.Selected!.AnchorId, panel.world!);
        Content.AddChild(DecisionPanel.Text($"Play {title}", GodotThemeVariations.Heading, wrap: true));
        if (!string.IsNullOrWhiteSpace(prompt.Resolution))
            Content.AddChild(DecisionPanel.Text(prompt.Resolution, GodotThemeVariations.Caption, wrap: true));
        if (draft.Selected!.DeferredTargetSelection)
            Content.AddChild(DecisionPanel.Text(
                "Payment commits now. Choose a target as the effect resolves; cancelling a later draft does not refund payment.",
                GodotThemeVariations.Caption, wrap: true));
        overlay.Resized += Fit;
        frame.MinimumSizeChanged += () => Callable.From(Fit).CallDeferred();
        Fit();
    }

    internal void AddCancel()
    {
        var cancel = new Button
        {
            Name = "CancelCardPlay", Text = "Cancel — return card to hand", Disabled = panel.submitting,
        };
        panel.StyleButton(cancel, InteractiveVisualState.Resting);
        cancel.Pressed += Cancel;
        panel.AddCommit(cancel);
    }

    private void Fit()
    {
        if (!InteractionControl.IsUsable(frame)) return;
        Vector2 viewport = panel.GetViewportRect().Size;
        frame.Size = new Vector2(Math.Min(680, viewport.X - 48), Math.Min(660, viewport.Y - 64));
        frame.Position = (viewport - frame.Size) / 2;
        bool wide = viewport.X >= 1200;
        if (wide) frame.Position += new Vector2(140, 0);
        staging.Fit(new Rect2(frame.Position, frame.Size), wide);
    }

    public void Dispose() => staging.Dispose();

    internal void Input(InputEvent input)
    {
        if (input.IsActionPressed("ui_cancel"))
        {
            Cancel();
            panel.GetViewport().SetInputAsHandled();
        }
        else if (input is InputEventKey { Keycode: Key.Tab, Pressed: true } key)
        {
            Control[] choices = [.. overlay.FindChildren("*", "Control", true, false)
                .OfType<Control>().Where(control => control.IsVisibleInTree()
                    && control.FocusMode == Control.FocusModeEnum.All
                    && control is not BaseButton { Disabled: true })];
            if (choices.Length > 0)
            {
                int index = Array.IndexOf(choices, panel.GetViewport().GuiGetFocusOwner());
                choices[(index + (key.ShiftPressed ? choices.Length - 1 : 1)) % choices.Length].GrabFocus();
            }
            panel.GetViewport().SetInputAsHandled();
        }
    }

    private void Cancel()
    {
        if (panel.submitting || !panel.IsCurrentDraft(draft, generation)) return;
        var fresh = new DecisionComposer(draft.Prompt);
        int anchor = draft.Selected!.AnchorId;
        panel.composer = fresh;
        panel.Rebuild();
        int refreshed = panel.GetRenderGeneration();
        Callable.From(() => BoardInteractionBinder.RestoreCardFocus(panel, fresh, refreshed, anchor)).CallDeferred();
    }
}
