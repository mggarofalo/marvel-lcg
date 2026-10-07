using Godot;
using Marvel.Client;

namespace Marvel.Godot;

/// <summary>Retains the decision sheet's operational explanation across dismissal and reopening.</summary>
internal sealed class CompleteDecisionSheetController(DecisionPanel panel)
{
    private CompleteDecisionSheet? sheet;
    internal string CurrentNotice { get; private set; } = string.Empty;
    internal bool IsOpen => sheet is not null;

    internal void Open(Control source)
    {
        if (sheet is not null || panel.submitting || panel.composer is null || panel.PaymentModalOpen) return;
        sheet = CompleteDecisionSheet.Open(panel, source);
        sheet.PresentNotice(CurrentNotice);
        panel.Rebuild(focusFirst: true);
    }

    internal void Closed() => sheet = null;
    internal void OpenCardChoiceAfterRender()
    {
        if (!DecisionCardChoices.IsChoice(panel.composer?.Prompt)) return;
        var draft = panel.composer;
        Callable.From(() =>
        {
            if (InteractionControl.IsUsable(panel) && ReferenceEquals(panel.composer, draft)) Open(panel);
        }).CallDeferred();
    }
    internal void Close() => sheet?.Close();
    internal void Input(InputEvent input) => sheet?.Input(input);
    internal void PresentProgress(GameProgressPresentation current)
    {
        CurrentNotice = CompleteDecisionSheet.RecoveryCopy(current);
        sheet?.PresentNotice(CurrentNotice);
    }
}
