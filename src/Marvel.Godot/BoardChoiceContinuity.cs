using Godot;
using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Rebinds an open source choice to replacement controls within the same prompt revision.</summary>
internal sealed class BoardChoiceContinuity(DecisionPanel panel)
{
    private Action<BoardRenderResult>? restoration;
    private int[]? expectedOffers;
    private bool expectedRecipients;

    internal bool IsRestoring => expectedOffers is not null;
    internal bool Matches(IReadOnlyList<AffordancePresentation> offers) =>
        expectedOffers is null || offers.Select(offer => offer.Id).SequenceEqual(expectedOffers);

    internal Action<BoardRenderResult>? Capture(bool layout = true) =>
        layout && BoardActionChoiceSurface.IsOwnedBy(this) ? restoration : null;

    internal static void Restore(Action<BoardRenderResult>? captured, BoardRenderResult board) => captured?.Invoke(board);

    internal void Show(Control source, DecisionComposer composer, int sourceId,
        IReadOnlyList<AffordancePresentation> offers, Action<int> choose,
        Action<Control> reopen, bool recipients = false)
    {
        int[] ids = [.. offers.Select(offer => offer.Id)];
        if (expectedOffers is { } expected && (!ids.SequenceEqual(expected) || recipients != expectedRecipients)) return;
        int generation = panel.GetRenderGeneration();
        long revision = panel.CurrentRevision;
        bool actionEntry = source is Button || source.GetViewport().GuiGetFocusOwner()?.Name == $"Card{sourceId}Action";
        restoration = board =>
        {
            if (panel.CurrentRevision != revision || !panel.IsCurrentDraft(composer, panel.GetRenderGeneration())) return;
            Control? replacement = Source(board, sourceId, actionEntry);
            if (replacement is null) return;
            expectedOffers = ids;
            expectedRecipients = recipients;
            try { reopen(replacement); }
            finally { expectedOffers = null; }
        };
        BoardActionChoiceSurface.Show(source, offers,
            () => panel.IsCurrentDraft(composer, generation), choose, recipients, this);
    }

    private static Control? Source(BoardRenderResult board, int id, bool actionEntry)
    {
        CardControl? card = board.VisibleCardControls().FirstOrDefault(control =>
            control.TargetId == id && control.IsVisibleInTree());
        if (card is null || !actionEntry) return card;
        return card.FindChild($"Card{id}Action", true, false) as Control ?? card;
    }
}
