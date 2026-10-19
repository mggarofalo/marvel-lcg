using Godot;
using Marvel.Client;

namespace Marvel.Godot;

/// <summary>Row edits and explicitly labelled bulk actions share local history and host checks.</summary>
internal sealed class DeckEditingActions : IDisposable
{
    private readonly DeckEditorControls ui;
    private readonly DeckWorkspace workspace;
    private readonly LocalGameClient client;
    private readonly DeckBrowserPresentation browser;
    private readonly Action changed;
    private readonly DeckDragTransfer transfer;
    private CancellationTokenSource? checking;
    private readonly DeckQuantityEditor quantities;
    private DeckEditProposal? addition;

    internal DeckEditingActions(DeckEditorControls ui, DeckWorkspace workspace, LocalGameClient client,
        DeckBrowserPresentation browser, Action changed)
    {
        this.ui = ui;
        this.workspace = workspace;
        this.client = client;
        this.browser = browser;
        this.changed = changed;
        quantities = new(workspace, client, changed);
        transfer = new(ui, workspace, client, browser, ApplyAddition);
        browser.SelectionChanged = _ => Refresh();
        ui.Cards.QuantityRequested = SetQuantity;
        ui.Deck.QuantityRequested = SetQuantity;
        ui.Undo.Shortcut = new() { Events = [new InputEventKey { Keycode = Key.Z, CtrlPressed = true }] };
        ui.Redo.Shortcut = new() { Events = [new InputEventKey { Keycode = Key.Y, CtrlPressed = true }] };
        ui.Undo.TooltipText = "Undo deck edit · Ctrl+Z";
        ui.Redo.TooltipText = "Redo deck edit · Ctrl+Y";
        ui.Add.Pressed += () => { if (addition is not null) ApplyAddition(addition, browser.SelectedFaces(true)); };
        ui.Remove.Pressed += () => { CancelEdit(); workspace.ClearCards(RemovableSelection()); changed(); };
        ui.RestoreRequired.Pressed += () => { CancelEdit(); workspace.RestoreRequiredCards(); changed(); };
        ui.Clear.Pressed += () => { CancelEdit(); workspace.ClearDeck(); changed(); };
        ui.Undo.Pressed += () => History(false);
        ui.Redo.Pressed += () => History(true);
        ui.SelectAll.Pressed += ui.Cards.SelectAll;
        ui.SelectDeck.Pressed += ui.Deck.SelectAll;
        ui.Deselect.Pressed += () => { ui.Cards.DeselectAll(); ui.Deck.DeselectAll(); };
    }

    private string[] RemovableSelection() => browser.SelectedFaces(false).Where(face => !browser.Required(face)).ToArray();

    private void History(bool redo)
    {
        CancelEdit();
        string? identity = workspace.Draft?.HeroCode;
        if (redo) workspace.Redo(); else workspace.Undo();
        changed();
        if (identity != workspace.Draft?.HeroCode) browser.InspectIdentity();
    }

    internal async void Refresh()
    {
        checking?.Cancel();
        checking?.Dispose();
        checking = new();
        var attempt = checking;
        addition = null;
        ui.Add.Disabled = true;
        ui.Undo.Disabled = !workspace.CanUndo;
        ui.Redo.Disabled = !workspace.CanRedo;
        ui.Remove.Disabled = RemovableSelection().Length == 0;
        var faces = browser.SelectedFaces(true);
        ui.Selection.Text = string.Join(" · ", new[]
        {
            faces.Length > 0 ? $"{faces.Length} pool cards selected" : null,
            browser.SelectedFaces(false).Length > 0 ? $"{browser.SelectedFaces(false).Length} deck cards selected" : null,
        }.Where(text => text is not null));
        ui.Selection.Visible = ui.Deselect.Visible = faces.Length + browser.SelectedFaces(false).Length > 0;
        ui.Add.Text = $"Add {faces.Length} cards +1";
        ui.Add.Visible = faces.Length > 1;
        ui.Remove.Visible = browser.SelectedFaces(false).Length > 0;
        if (workspace.Draft is null) return;
        try
        {
            var proposal = await DeckEditProposal.CheckAsync(workspace, client, faces, null, attempt.Token);
            if (attempt.IsCancellationRequested) return;
            addition = proposal;
            ui.Add.Disabled = proposal is null;
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { if (!attempt.IsCancellationRequested) ui.Status.Text = error.Message; }
    }

    private async void SetQuantity(string face, int quantity)
    {
        if (browser.Required(face)) return;
        string? error = await quantities.SetAsync(face, quantity);
        if (error is not null && GodotObject.IsInstanceValid(ui.Window)) ui.Status.Text = $"{browser.CardTitle(face)}: {error}";
    }

    private void ApplyAddition(DeckEditProposal proposal, string[] faces)
    {
        CancelEdit();
        if (!proposal.Apply(workspace)) { ui.Status.Text = "The draft changed. Select cards and add again."; return; }
        changed();
        ui.Deck.Reveal(faces);
        string result = string.Join("; ", faces.Select(face => $"{browser.CardTitle(face)}: {workspace.Draft!.Cards.Count(item => item == face)} total"));
        ui.Status.Text = $"Added {faces.Length} cards (+1 each). {result}. Undo reverses this addition.";
    }

    private void CancelEdit() => quantities.Cancel();
    public void Dispose() { quantities.Dispose(); checking?.Cancel(); checking?.Dispose(); transfer.Dispose(); }
}
