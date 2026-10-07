using Godot;
using Marvel.Client;
using Marvel.Server;

namespace Marvel.Godot;

/// <summary>Keeps undo admission and its visible explanation together.</summary>
internal static class HistoryUndoControl
{
    internal static void Refresh(Main main)
    {
        HistoryDescriptor? history = main.CurrentGame?.History;
        int last = (history?.Cursor ?? 0) - 1;
        main.undoLast.Disabled = main.decisions.PaymentModalOpen || !main.lifecycle.CanUndo(last);
        main.undoLast.TooltipText = main.decisions.PaymentModalOpen
            ? "Finish or cancel the current payment selection before undoing an action."
            : HistoryUndoPresentation.Describe(!main.undoLast.Disabled, history, main.lifecycle.Progress);
        RefreshReason(main, TableHistoryDrawer.IsExpanded(main));
    }

    internal static void RefreshReason(Main main, bool expanded)
    {
        Control history = (Control)main.eventLog.GetParent();
        Label reason = history.GetNodeOrNull<Label>("UndoReason") ?? new Label
        {
            Name = "UndoReason",
            ThemeTypeVariation = GodotThemeVariations.BodyMuted,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        if (reason.GetParent() is null) history.AddChild(reason);
        Node header = history.GetNodeOrNull<Node>("HistoryActions") ?? history.GetNode("EventHeader");
        history.MoveChild(reason, header.GetIndex() + 1);
        reason.Text = main.undoLast.Disabled ? main.undoLast.TooltipText : string.Empty;
        reason.Visible = expanded && reason.Text.Length > 0;
    }
}
