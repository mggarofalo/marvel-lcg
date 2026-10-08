using Godot;
using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Composes one unpaid card play beside an inspectable table.</summary>
internal sealed class CardPaymentWorkspace : IDisposable
{
    private readonly DecisionPanel panel;
    private readonly PanelContainer frame;
    private readonly Main main;
    private readonly DecisionComposer draft;
    private readonly int generation;
    internal VBoxContainer Content { get; }

    internal CardPaymentWorkspace(DecisionPanel panel, PromptPresentation prompt)
    {
        this.panel = panel;
        draft = panel.composer!;
        generation = panel.GetRenderGeneration();
        main = ClientSceneHost.MainFor(panel);
        frame = new PanelContainer
        {
            Name = "PaymentWorkspace", Theme = panel.Theme,
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CardPaymentWorkspaceLayout.Install(main, frame);
        Content = new VBoxContainer { Name = "PaymentContent", ThemeTypeVariation = GodotThemeVariations.TightStack };
        frame.AddChild(Content);
        CardPaymentWorkspaceHeader.Add(Content, panel, main, draft, prompt);
        Callable.From(() => CardPaymentWorkspaceLayout.Refresh(main)).CallDeferred();
    }

    internal int? ScrollPosition(DecisionComposer? current) =>
        ReferenceEquals(draft, current)
            ? Content.GetNodeOrNull<ScrollContainer>("DecisionBodyScroll")?.ScrollVertical
            : null;

    internal void AddCancel()
    {
        var cancel = new Button
        {
            Name = "CancelCardPlay", Text = "Cancel card play", Disabled = panel.submitting,
        };
        panel.StyleButton(cancel, InteractiveVisualState.Resting);
        cancel.Pressed += Cancel;
        panel.AddCommit(cancel);
    }

    public void Dispose()
    {
        if (InteractionControl.IsUsable(frame))
        {
            frame.GetParent().RemoveChild(frame);
            frame.QueueFree();
        }
        Callable.From(() => CardPaymentWorkspaceLayout.Refresh(main)).CallDeferred();
    }

    internal void Input(InputEvent input)
    {
        if (input.IsActionPressed("ui_cancel")
            && CardPaymentWorkspaceFocus.Contains(frame, panel.GetViewport().GuiGetFocusOwner()))
        {
            Cancel();
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
