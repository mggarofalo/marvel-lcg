using Godot;
using Marvel.Client;
using Marvel.Desktop;
using Marvel.Protocol;

namespace Marvel.Godot;

/// <summary>Connects the deck workspace to the setup sheet and captures chosen per-seat recipes.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "The setup scene tree owns both native controls.")]
internal sealed class DeckSetupController
{
    private readonly Main main;
    private readonly Button open = new() { Text = "Deck library · browse, build, import" };
    private readonly Label summary = new() { Text = "Published starter decks", AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly Dictionary<string, DeckDraft> chosen = new(StringComparer.Ordinal);
    private DeckCatalog? catalog;
    private DeckWorkspace? workspace;

    internal DeckSetupController(Main main)
    {
        this.main = main;
        var parent = main.start.GetParent();
        parent.AddChild(open);
        parent.AddChild(summary);
        parent.MoveChild(open, main.start.GetIndex());
        parent.MoveChild(summary, main.start.GetIndex());
        open.Pressed += Open;
        main.endpoint.TextChanged += _ => { chosen.Clear(); catalog = null; workspace = null; Refresh(); };
        main.hero.ItemSelected += _ => Refresh();
        main.secondHero.ItemSelected += _ => Refresh();
        foreach (var control in main.setupController.AdditionalHeroes) control.ItemSelected += _ => Refresh();
    }

    internal GameSetupSelection Apply(GameSetupSelection selection)
    {
        if (chosen.Count == 0 || catalog is null) return selection;
        return selection with
        {
            Decks = selection.HeroKeys.Select(key => chosen.TryGetValue(key, out var draft) ? draft
                : Starter(catalog.Heroes.Single(hero => hero.Key == key))).ToArray(),
        };
    }

    private async void Open()
    {
        if (!CanOpen()) return;
        open.Disabled = true;
        summary.Text = "Loading card library…";
        string endpoint = main.endpoint.Text;
        LocalGameClient? client = null;
        try
        {
            var connection = ClientComposition.Connect(DesktopDataRoot.Current(), endpoint);
            if (!connection.Succeeded) { summary.Text = connection.Error!.Message; return; }
            client = connection.Client!;
            catalog = await ReadCatalog(client, endpoint);
            if (catalog is null) return;
            var controls = new DeckEditorControls(main);
            var store = new LocalDeckStore(System.Environment.GetEnvironmentVariable("MARVEL_DECK_LIBRARY_DIRECTORY")
                ?? ProjectSettings.GlobalizePath("user://decks"));
            workspace ??= new(catalog);
            var controller = new DeckEditorController(controls, client, workspace, store, Assign);
            client = null; // The window owns this independent read-only connection.
            controller.Show();
            Refresh();
        }
        catch (Exception) { summary.Text = "The deck library could not open. Your selected decks have not changed."; }
        finally
        {
            client?.Dispose();
            if (GodotObject.IsInstanceValid(open)) open.Disabled = false;
        }
    }

    private async Task<DeckCatalog?> ReadCatalog(LocalGameClient client, string endpoint)
    {
        var result = await client.ReadDecksAsync();
        if (!Current(endpoint)) return null;
        if (!result.Succeeded) { summary.Text = result.Error!.Message; return null; }
        var loaded = await DeckResearchCatalog.LoadAsync(client, result.Decks!.Catalog!);
        return Current(endpoint) ? loaded : null;
    }

    private bool Current(string endpoint) => GodotObject.IsInstanceValid(main)
        && main.setupPanel.Visible && main.endpoint.Text == endpoint;

    private bool CanOpen() => !main.lifecycle.EntryPending && main.setupChoices is not null;

    private void Assign(DeckDraft draft, int seat)
    {
        var hero = catalog!.Heroes.Single(item => item.IdentityFaces.Contains(draft.HeroCode, StringComparer.Ordinal));
        main.setupController.AssignHero(hero.Key, seat);
        chosen[hero.Key] = draft;
        main.setupController.RefreshBriefing();
        Refresh();
    }

    private void Refresh() => summary.Text = main.setupChoices is null ? "Reload setup choices"
        : string.Join(" · ", main.SelectedSetup().HeroKeys.Select((key, seat) =>
            $"Seat {seat + 1}: {(chosen.TryGetValue(key, out var deck) ? deck.Name : "published starter")}"));

    private static DeckDraft Starter(DeckHero hero) => new(hero.Name + " starter", hero.IdentityFaces[0], hero.Starter);
}
