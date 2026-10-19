using Godot;
using Marvel.Client;

namespace Marvel.Godot;

/// <summary>Captures drag intent immediately and commits only after host validation of that exact draft.</summary>
internal sealed class DeckDragTransfer : IDisposable
{
    private readonly DeckEditorControls ui;
    private readonly DeckWorkspace workspace;
    private readonly LocalGameClient client;
    private readonly DeckBrowserPresentation browser;
    private readonly Action<DeckEditProposal, string[]> apply;
    private readonly CancellationTokenSource lifetime = new();
    private DeckDragAttempt? current;

    internal DeckDragTransfer(DeckEditorControls ui, DeckWorkspace workspace, LocalGameClient client,
        DeckBrowserPresentation browser, Action<DeckEditProposal, string[]> apply)
    {
        this.ui = ui;
        this.workspace = workspace;
        this.client = client;
        this.browser = browser;
        this.apply = apply;
        ui.Cards.BeginTransfer = Begin;
        ui.Cards.EndTransfer = End;
        ui.Deck.AcceptTransfer = Accepts;
        ui.Deck.CompleteTransfer = Drop;
    }

    private Variant Begin(Control source)
    {
        string[] faces = browser.SelectedFaces(true);
        if (faces.Length == 0) return default;
        var attempt = new DeckDragAttempt(Guid.NewGuid().ToString("N"), workspace.Revision, faces,
            DeckEditProposal.CheckAsync(workspace, client, faces, null, lifetime.Token));
        current = attempt;
        ui.DropHint.Visible = true;
        string changes = string.Join("\n", faces.Take(6).Select(face =>
        {
            int before = workspace.Draft!.Cards.Count(item => item == face);
            return $"{browser.CardTitle(face)}: {before} → {before + 1}";
        }));
        if (faces.Length > 6) changes += $"\n…and {faces.Length - 6} more cards (+1 each)";
        var preview = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        preview.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.07f, 0.13f, 0.18f), ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8 });
        var label = new Label { Text = $"Checking +1 each · {faces.Length} cards\n{changes}", MouseFilter = Control.MouseFilterEnum.Ignore };
        preview.AddChild(label);
        source.SetDragPreview(preview);
        ui.DropHint.Text = "Drop here · checking construction before adding";
        ui.Status.Text = "Proposed copies shown as current → result. Drop in the deck; Escape cancels.";
        ShowResult(attempt, label, changes);
        return attempt.Token;
    }

    private async void ShowResult(DeckDragAttempt attempt, Label label, string changes)
    {
        try
        {
            var checkedProposal = await attempt.Check;
            if (lifetime.IsCancellationRequested || current != attempt || !GodotObject.IsInstanceValid(label)) return;
            ShowCheckedFeedback(label, changes, attempt.Faces.Length, checkedProposal is not null);
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { if (!lifetime.IsCancellationRequested && current == attempt) ui.DropHint.Text = error.Message; }
    }

    private void ShowCheckedFeedback(Label label, string changes, int count, bool accepted)
    {
        label.Text = (accepted ? $"Add {count} cards · +1 each" : "Cannot add this selection") + "\n" + changes;
        ui.DropHint.Text = accepted ? $"Release here: add {count} cards (+1 each)" : "Selection cannot be added to this draft";
        ui.Deck.Modulate = accepted ? new Color(0.7f, 1f, 0.8f) : Colors.White;
    }

    private bool Accepts(Variant data) => current is { } attempt && attempt.Revision == workspace.Revision
        && data.VariantType == Variant.Type.String && data.AsString() == attempt.Token;

    private async void Drop(Variant data)
    {
        if (!Accepts(data)) return;
        var attempt = current!;
        attempt.Dropped = true;
        ui.Status.Text = "Checking the dropped cards before adding…";
        try
        {
            var checkedProposal = await attempt.Check;
            if (lifetime.IsCancellationRequested) return;
            if (checkedProposal is null || attempt.Revision != workspace.Revision)
            {
                ui.Status.Text = "No cards added. This selection is not accepted for the current draft; use the available row quantities or change the selection.";
                return;
            }
            apply(checkedProposal, attempt.Faces);
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { if (!lifetime.IsCancellationRequested) ui.Status.Text = "No cards added. " + error.Message; }
    }

    private void End()
    {
        if (current is null) return;
        if (!current.Dropped) ui.Status.Text = "No cards added. The drag was cancelled or released outside the deck.";
        current = null;
        ui.DropHint.Visible = false;
        ui.DropHint.Text = "Drop cards here to add +1 each";
        ui.Deck.Modulate = Colors.White;
    }

    public void Dispose() { lifetime.Cancel(); End(); lifetime.Dispose(); }
}
