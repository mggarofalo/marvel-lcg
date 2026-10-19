using Godot;
using Marvel.Client;

namespace Marvel.Godot;

/// <summary>Coordinates reversible edits, persistence and revision-bound host assessment.</summary>
internal sealed class DeckEditorController : IDisposable
{
    private readonly DeckEditorControls ui;
    private readonly LocalGameClient client;
    private readonly DeckWorkspace workspace;
    private readonly LocalDeckStore store;
    private readonly Action<DeckDraft, int> assign;
    private readonly DeckBrowserPresentation browser;
    private readonly DeckEditingActions editing;
    private readonly CancellationTokenSource lifetime = new();
    private readonly MarvelCdbDeckImporter importer = new();
    private CancellationTokenSource? validation;
    private CancellationTokenSource? acquisition;
    private IReadOnlyList<SavedDeck> saved = [];
    private Action? confirmed;
    private bool rendering;
    private bool disposed;

    internal DeckEditorController(DeckEditorControls ui, LocalGameClient client, DeckWorkspace workspace,
        LocalDeckStore store, Action<DeckDraft, int> assign)
    {
        this.ui = ui;
        this.client = client;
        this.workspace = workspace;
        this.store = store;
        this.assign = assign;
        browser = new(ui, workspace);
        editing = new(ui, workspace, client, browser, () => DisplayDraft(false));
        Bind();
    }

    internal void Show()
    {
        ui.Hero.AddItem("Choose a Core identity");
        foreach (var hero in workspace.Catalog.Heroes) ui.Hero.AddItem(hero.Name);
        browser.Initialize();
        ReloadLibrary();
        if (workspace.Draft is null)
        {
            var hero = workspace.Catalog.Heroes[0];
            workspace.Initialize(new(hero.Name + " starter", hero.IdentityFaces[0], hero.Starter));
        }
        DisplayDraft();
        DeckEditorLayout.ShowCentered(ui.Window, ui.Window.GetParent().GetWindow(), new(1720, 940));
        ui.Hero.GrabFocus();
    }

    private void Bind()
    {
        ui.Name.TextChanged += _ => Describe();
        ui.Hero.ItemSelected += _ => ChooseIdentity();
        ui.InspectIdentity.Pressed += browser.InspectIdentity;
        ui.Starter.Pressed += () => PreserveThen(() => New(starter: true));
        ui.Empty.Pressed += () => PreserveThen(() => New(starter: false));
        ui.Copy.Pressed += () => PreserveThen(() => Replace(new(workspace.Draft!.Name + " copy", workspace.Draft.HeroCode, workspace.Draft.Cards)));
        ui.Save.Pressed += Save;
        ui.Load.Pressed += () => PreserveThen(Load);
        ui.Delete.Pressed += Delete;
        ui.UseDeck.Pressed += () => Use(ui.Seat.Selected);
        ui.Close.Pressed += Close;
        ui.Window.CloseRequested += Close;
        ui.Confirm.Confirmed += () => { var action = confirmed; confirmed = null; action?.Invoke(); };
        ui.Import.Pressed += () => PickFile(false);
        ui.Export.Pressed += () => PickFile(true);
        ui.File.FileSelected += FileSelected;
        ui.Fetch.Pressed += ImportUrl;
        ui.Published.Pressed += () => _ = new PublishedDeckBrowser(ui.Window, workspace.Catalog, url => { ui.Url.Text = url; ImportUrl(); });
        ui.CancelFetch.Pressed += () => acquisition?.Cancel();
        ui.Window.TreeExiting += Dispose;
    }

    private void New(bool starter)
    {
        int index = Math.Max(0, ui.Hero.Selected - 1);
        var hero = workspace.Catalog.Heroes[index];
        Replace(new(hero.Name + (starter ? " starter" : " deck"), hero.IdentityFaces[0], starter ? hero.Starter : hero.Signature));
    }

    private void Replace(DeckDraft draft, SavedDeck? from = null)
    {
        workspace.Replace(draft, from);
        DisplayDraft();
    }

    private void DisplayDraft(bool inspectIdentity = true)
    {
        var draft = workspace.Draft!;
        rendering = true;
        ui.Name.Text = draft.Name;
        int hero = workspace.Catalog.Heroes.ToList().FindIndex(item => item.IdentityFaces.Contains(draft.HeroCode, StringComparer.Ordinal));
        ui.Hero.Select(hero + 1);
        rendering = false;
        ui.Status.Text = hero < 0 ? "This imported identity is outside the playable Core set. All cards have been retained." : "";
        Changed();
        if (inspectIdentity) browser.InspectIdentity();
    }

    private void ChooseIdentity()
    {
        if (rendering || ui.Hero.Selected <= 0) return;
        workspace.ChooseIdentity(workspace.Catalog.Heroes[ui.Hero.Selected - 1].Key);
        Changed();
        browser.InspectIdentity();
    }

    private void Describe()
    {
        if (workspace.Draft is null || rendering) return;
        string hero = ui.Hero.Selected > 0 ? workspace.Catalog.Heroes[ui.Hero.Selected - 1].IdentityFaces[0] : workspace.Draft.HeroCode;
        workspace.Describe(ui.Name.Text, hero);
        Changed();
    }

    private async void Changed()
    {
        browser.Render();
        editing.Refresh();
        validation?.Cancel();
        validation?.Dispose();
        validation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        var attempt = validation;
        try
        {
            string? error = await workspace.AssessAsync(client, attempt.Token);
            if (attempt.IsCancellationRequested || !GodotObject.IsInstanceValid(ui.Window)) return;
            browser.Render();
            if (error is not null) ui.Diagnostics.Text = error;
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!lifetime.IsCancellationRequested) ui.Diagnostics.Text = "Validation could not finish. Edit again to retry; your draft is retained."; }
    }

    private void Save() => Try(() =>
    {
        workspace.Save(store);
        ReloadLibrary();
        browser.Render();
        ui.Status.Text = "Draft saved locally. Invalid drafts can be saved and corrected later.";
    });

    private void ReloadLibrary() => Try(() =>
    {
        string? selectedId = workspace.Saved?.Id;
        saved = store.List();
        ui.Library.Clear();
        foreach (var item in saved) ui.Library.AddItem(item.Draft.Name);
        int selectedIndex = saved.ToList().FindIndex(item => item.Id == selectedId);
        if (selectedIndex >= 0) ui.Library.Select(selectedIndex);
        ui.Load.Disabled = ui.Delete.Disabled = saved.Count == 0;
    });

    private void Load() => Try(() =>
    {
        if (ui.Library.Selected < 0) return;
        var item = store.Read(saved[ui.Library.Selected].Id);
        Replace(item.Draft, item);
    });

    private void Delete()
    {
        if (ui.Library.Selected < 0) return;
        var item = saved[ui.Library.Selected];
        confirmed = () => Try(() =>
        {
            store.Delete(item.Id, item.Revision);
            if (workspace.Saved?.Id == item.Id) workspace.ForgetSaved();
            ReloadLibrary();
            browser.Render();
            ui.Status.Text = "Saved deck deleted. The open copy stays in memory; edit or Save draft to keep it again.";
        });
        ui.Confirm.DialogText = $"Delete saved deck \"{item.Draft.Name}\"? Running games retain their captured decks.";
        ui.Confirm.OkButtonText = "Delete saved deck";
        ui.Confirm.PopupCentered();
    }

    private void Use(int seat) => Try(() =>
    {
        if (!workspace.CanUse) return;
        assign(workspace.Draft!, seat);
        ui.Status.Text = $"Deck captured for seat {seat + 1}. Further edits will not change that selection until you choose Use again.";
    });

    private void PickFile(bool export)
    {
        ui.File.FileMode = export ? FileDialog.FileModeEnum.SaveFile : FileDialog.FileModeEnum.OpenFile;
        ui.File.Title = export ? "Export deck JSON" : "Import MarvelCDB JSON";
        ui.File.PopupCentered();
    }

    private void FileSelected(string path) => Try(() =>
    {
        if (ui.File.FileMode == FileDialog.FileModeEnum.SaveFile)
        {
            System.IO.File.WriteAllBytes(path, MarvelCdbDeckJson.Write(workspace.Draft!));
            ui.Status.Text = "JSON exported. The local draft is unchanged.";
            return;
        }
        if (new FileInfo(path).Length > MarvelCdbDeckJson.MaximumBytes) throw new InvalidDataException("The deck file exceeds 1 MiB.");
        var draft = MarvelCdbDeckJson.Read(System.IO.File.ReadAllBytes(path));
        PreserveThen(() => Replace(draft));
    });

    private async void ImportUrl()
    {
        acquisition?.Cancel();
        acquisition?.Dispose();
        acquisition = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        var attempt = acquisition;
        long revision = workspace.Revision;
        ui.ImportStatus.Text = "Downloading and checking the deck…";
        ui.Fetch.Disabled = true;
        ui.CancelFetch.Disabled = false;
        ui.Status.Text = "Downloading a deck snapshot… Your current draft remains open.";
        try
        {
            var draft = await importer.ImportAsync(ui.Url.Text, attempt.Token);
            if (attempt.IsCancellationRequested) return;
            if (revision != workspace.Revision) { ui.ImportStatus.Text = ui.Status.Text = "Your draft changed during import. Import again when ready to replace it."; return; }
            PreserveThen(() => Replace(draft));
            ui.ImportDialog?.Hide();
        }
        catch (OperationCanceledException) { ReportImportFailure("Import cancelled or timed out. Your draft is unchanged."); }
        catch (Exception error) { ReportImportFailure("Import failed: " + error.Message); }
        finally
        {
            if (!lifetime.IsCancellationRequested) { ui.Fetch.Disabled = false; ui.CancelFetch.Disabled = true; }
        }
    }

    private void ReportImportFailure(string message)
    {
        if (!lifetime.IsCancellationRequested) ui.ImportStatus.Text = ui.Status.Text = message;
    }

    private void PreserveThen(Action action) => Try(() =>
    {
        workspace.Preserve(store);
        action();
        ReloadLibrary();
    });

    private void Close() => PreserveThen(() => ui.Window.QueueFree());

    private void Try(Action action)
    {
        try { action(); }
        catch (Exception error) { ui.Status.Text = error.Message; }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        editing.Dispose();
        lifetime.Cancel();
        validation?.Cancel();
        acquisition?.Cancel();
        client.Dispose();
        importer.Dispose();
        validation?.Dispose();
        acquisition?.Dispose();
        lifetime.Dispose();
    }
}
