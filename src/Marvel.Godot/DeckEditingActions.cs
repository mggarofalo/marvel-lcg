using Godot;
using Marvel.Client;

namespace Marvel.Godot;

/// <summary>Selection, checked bulk edits and local history share one draft lifetime.</summary>
internal sealed class DeckEditingActions : IDisposable
{
    private readonly DeckEditorControls ui;
    private readonly DeckWorkspace workspace;
    private readonly LocalGameClient client;
    private readonly DeckBrowserPresentation browser;
    private readonly Action changed;
    private CancellationTokenSource? checking;
    private readonly DeckEditProposal?[] quantities = new DeckEditProposal?[3];
    private DeckEditProposal? addition;
    private DeckEditProposal? dragging;
    private string? dragToken;
    private long dragRevision;
    private bool fromBrowser = true;

    internal DeckEditingActions(DeckEditorControls ui, DeckWorkspace workspace, LocalGameClient client,
        DeckBrowserPresentation browser, Action changed)
    {
        this.ui = ui;
        this.workspace = workspace;
        this.client = client;
        this.browser = browser;
        this.changed = changed;
        browser.SelectionChanged = browse => { fromBrowser = browse; Refresh(); };
        for (int index = 0; index < 3; index++)
        {
            int captured = index;
            ui.Quantities[index].Pressed += () => Apply(quantities[captured]);
        }
        ui.Undo.Shortcut = new() { Events = [new InputEventKey { Keycode = Key.Z, CtrlPressed = true }] };
        ui.Redo.Shortcut = new() { Events = [new InputEventKey { Keycode = Key.Y, CtrlPressed = true }] };
        ui.Undo.TooltipText = "Undo deck edit · Ctrl+Z";
        ui.Redo.TooltipText = "Redo deck edit · Ctrl+Y";
        ui.Add.Pressed += () => Apply(addition);
        ui.Remove.Pressed += () => { workspace.ClearCards(browser.SelectedFaces(false)); changed(); };
        ui.Clear.Pressed += () => { workspace.ClearDeck(); changed(); };
        ui.Undo.Pressed += () => { workspace.Undo(); changed(); };
        ui.Redo.Pressed += () => { workspace.Redo(); changed(); };
        ui.SelectAll.Pressed += () => SelectAll(true);
        ui.SelectDeck.Pressed += () => SelectAll(false);
        ui.Deselect.Pressed += () => { ui.Cards.DeselectAll(); ui.Deck.DeselectAll(); Refresh(); };
        ui.Cards.BeginTransfer = BeginDrag;
        ui.Cards.EndTransfer = EndDrag;
        ui.Deck.AcceptTransfer = CanDrop;
        ui.Deck.CompleteTransfer = data => { if (CanDrop(data)) Apply(dragging); EndDrag(); };
    }

    private void SelectAll(bool browse)
    {
        var list = browse ? ui.Cards : ui.Deck;
        for (int index = 0; index < list.ItemCount; index++) list.Select(index, false);
        fromBrowser = browse;
        Refresh();
    }

    internal async void Refresh()
    {
        ResetChecks();
        var attempt = checking!;
        if (workspace.Draft is null) return;
        var selected = browser.SelectedFaces(fromBrowser);
        ui.Selection.Text = $"{selected.Length} selected in {(fromBrowser ? "results" : "deck")} · checking available quantities…";
        try
        {
            var checkedAddition = await DeckEditProposal.CheckAsync(workspace, client, browser.SelectedFaces(true), null, attempt.Token);
            if (attempt.IsCancellationRequested) return;
            addition = checkedAddition;
            ui.Add.Disabled = addition is null;
            for (int index = 0; index < 3; index++)
            {
                var proposal = await DeckEditProposal.CheckAsync(workspace, client, selected, index + 1, attempt.Token);
                if (attempt.IsCancellationRequested) return;
                quantities[index] = proposal;
                ui.Quantities[index].Disabled = proposal is null;
                ui.Quantities[index].TooltipText = proposal is null ? "The game service does not accept this quantity for the selected cards in this draft." : $"Set every selected card to {index + 1} copies in one undoable edit.";
            }
            ui.Selection.Text = $"{selected.Length} selected in {(fromBrowser ? "results" : "deck")} · enabled quantities pass construction checks. Required signature cards are kept when clearing.";
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { if (!attempt.IsCancellationRequested) ui.Selection.Text = error.Message; }
    }

    private void ResetChecks()
    {
        checking?.Cancel();
        checking?.Dispose();
        checking = new();
        addition = null;
        Array.Clear(quantities);
        ui.Add.Disabled = true;
        foreach (var button in ui.Quantities) button.Disabled = true;
        ui.Undo.Disabled = !workspace.CanUndo;
        ui.Redo.Disabled = !workspace.CanRedo;
        ui.Remove.Disabled = browser.SelectedFaces(false).Length == 0;
    }

    private void Apply(DeckEditProposal? proposal)
    {
        if (proposal?.Apply(workspace) == true) changed();
        else ui.Status.Text = "Selection or draft changed. Choose an available quantity again.";
    }

    private Variant BeginDrag()
    {
        if (addition is null) return default;
        dragging = addition;
        dragRevision = workspace.Revision;
        dragToken = Guid.NewGuid().ToString("N");
        var faces = browser.SelectedFaces(true);
        string names = string.Join("\n", faces.Take(3).Select(face => workspace.Catalog.Cards.Single(card => card.Face == face).Title));
        var preview = new Label { Text = $"Add 1 copy each · {faces.Length} selected cards\n{names}", Modulate = Colors.White };
        ui.Cards.SetDragPreview(preview);
        ui.Deck.Modulate = new Color(0.7f, 1f, 0.8f);
        ui.Status.Text = "Drop into the deck to add one copy of each selected card. Escape cancels.";
        return dragToken;
    }

    private bool CanDrop(Variant data) => dragging is not null && dragRevision == workspace.Revision
        && data.VariantType == Variant.Type.String && data.AsString() == dragToken;

    private void EndDrag()
    {
        if (dragging is not null)
            ui.Status.Text = workspace.Revision == dragRevision ? "Drag ended without adding cards." : "Deck updated. Undo restores the previous draft.";
        dragging = null;
        dragToken = null;
        ui.Deck.Modulate = Colors.White;
    }

    public void Dispose() { checking?.Cancel(); checking?.Dispose(); }
}
