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

    internal DeckSetupController(Main main)
    {
        this.main = main;
        var parent = main.start.GetParent();
        parent.AddChild(open);
        parent.AddChild(summary);
        parent.MoveChild(open, main.start.GetIndex());
        parent.MoveChild(summary, main.start.GetIndex());
        open.Pressed += Open;
        main.endpoint.TextChanged += _ => { chosen.Clear(); catalog = null; Refresh(); };
        main.hero.ItemSelected += _ => Refresh();
        main.secondHero.ItemSelected += _ => Refresh();
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
        string endpoint = main.endpoint.Text;
        LocalGameClient? client = null;
        try
        {
            var connection = ClientComposition.Connect(DesktopDataRoot.Current(), endpoint);
            if (!connection.Succeeded) { summary.Text = connection.Error!.Message; return; }
            client = connection.Client!;
            var result = await client.ReadDecksAsync();
            if (!Current(endpoint)) return;
            if (!result.Succeeded) { summary.Text = result.Error!.Message; return; }
            catalog = result.Decks!.Catalog!;
            var controls = new DeckEditorControls(main);
            var store = new LocalDeckStore(System.Environment.GetEnvironmentVariable("MARVEL_DECK_LIBRARY_DIRECTORY")
                ?? ProjectSettings.GlobalizePath("user://decks"));
            var controller = new DeckEditorController(controls, client, new(catalog), store, Assign);
            client = null; // The window owns this independent read-only connection.
            controller.Show();
        }
        catch (Exception) { summary.Text = "The deck library could not open. Your selected decks have not changed."; }
        finally
        {
            client?.Dispose();
            if (GodotObject.IsInstanceValid(open)) open.Disabled = false;
        }
    }

    private bool Current(string endpoint) => GodotObject.IsInstanceValid(main)
        && main.setupPanel.Visible && main.endpoint.Text == endpoint;

    private bool CanOpen() => !main.lifecycle.EntryPending && main.setupChoices is not null;

    private void Assign(DeckDraft draft, int seat)
    {
        var hero = catalog!.Heroes.Single(item => item.IdentityFaces.Contains(draft.HeroCode, StringComparer.Ordinal));
        if (seat == 0)
        {
            int index = main.setupChoices!.Heroes.ToList().FindIndex(item => item.Key == hero.Key);
            if (main.hero.Selected != index)
            {
                main.hero.Select(index);
                main.setupController.PopulateSecondHeroChoices();
            }
        }
        else
        {
            int index = Enumerable.Range(1, main.secondHero.ItemCount - 1)
                .FirstOrDefault(item => main.secondHero.GetItemMetadata(item).AsString() == hero.Key, -1);
            if (index < 0) throw new InvalidOperationException("Choose a different hero for seat 2.");
            main.secondHero.Select(index);
        }
        chosen[hero.Key] = draft;
        main.setupController.RefreshBriefing();
        Refresh();
    }

    private void Refresh() => summary.Text = main.setupChoices is null ? "Reload setup choices"
        : string.Join(" · ", main.SelectedSetup().HeroKeys.Select((key, seat) =>
            $"Seat {seat + 1}: {(chosen.TryGetValue(key, out var deck) ? deck.Name : "published starter")}"));

    private static DeckDraft Starter(DeckHero hero) => new(hero.Name + " starter", hero.IdentityFaces[0], hero.Starter);
}
