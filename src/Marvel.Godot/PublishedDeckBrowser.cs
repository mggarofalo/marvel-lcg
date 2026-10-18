using Godot;
using Marvel.Client;
using Marvel.Protocol;

namespace Marvel.Godot;

/// <summary>Public search, selection and import without changing the open draft during discovery.</summary>
internal sealed class PublishedDeckBrowser : IDisposable
{
    private readonly Window window = new() { Name = "PublishedDeckSearch", Title = "Find published MarvelCDB decks", Size = new(1000, 800), Transient = true, Exclusive = true };
    private readonly VBoxContainer stack = new();
    private readonly Dictionary<string, OptionButton> filters = new(StringComparer.Ordinal);
    private readonly LineEdit author = new() { PlaceholderText = "Author name" };
    private readonly LineEdit name = new() { PlaceholderText = "Deck name contains…" };
    private readonly ItemList packs = new() { Name = "AllowedPacks", SelectMode = ItemList.SelectModeEnum.Multi, CustomMinimumSize = new(300, 120) };
    private readonly ItemList cards = new() { SelectMode = ItemList.SelectModeEnum.Multi, CustomMinimumSize = new(300, 120) };
    private readonly ItemList results = new() { Name = "PublishedDeckResults", SizeFlagsVertical = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(0, 200) };
    private readonly Label status = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly Button previous = new() { Text = "Previous page", Disabled = true };
    private readonly Button next = new() { Text = "Next page", Disabled = true };
    private readonly MarvelCdbDeckSearch service = new();
    private readonly Action<string> import;
    private readonly DeckCard[] catalog;
    private DeckCard[] visibleCards = [];
    private readonly HashSet<string> included = new(StringComparer.Ordinal);
    private CancellationTokenSource? pending;
    private PublishedDeckPage? current;
    private KeyValuePair<string, string>[] submitted = [];
    private string[] packKeys = [];
    private int page = 1;
    private bool initialized;
    private bool disposed;

    internal PublishedDeckBrowser(Window parent, DeckCatalog catalog, Action<string> import)
    {
        this.import = import;
        this.catalog = catalog.Cards.OrderBy(card => card.Title, StringComparer.Ordinal).ToArray();
        window.Theme = parent.Theme;
        parent.AddChild(window);
        var margin = new MarginContainer();
        window.AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.AddChild(stack);
        AddFilters();
        stack.AddChild(results);
        stack.AddChild(status);
        var actions = new HBoxContainer();
        stack.AddChild(actions);
        var fetch = new Button { Text = "Import selected deck" };
        var close = new Button { Text = "Back to my deck" };
        foreach (var button in new[] { previous, next, fetch, close }) actions.AddChild(button);
        previous.Pressed += () => Search(page - 1);
        next.Pressed += () => Search(page + 1);
        fetch.Pressed += Import;
        close.Pressed += () => window.QueueFree();
        window.CloseRequested += () => window.QueueFree();
        window.TreeExiting += Dispose;
        window.PopupCenteredClamped(new(1000, 800), 0.95f);
        Search(1);
    }

    private void AddFilters()
    {
        stack.AddChild(new Label { Text = "Published decks · online MarvelCDB search" });
        var row = new GridContainer { Columns = 2 };
        stack.AddChild(row);
        foreach (string key in new[] { "hero", "aspect", "tag", "sort" })
        {
            var select = new OptionButton { Name = "Search_" + key, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            select.AddItem("Loading " + key + "…");
            filters[key] = select;
            row.AddChild(select);
        }
        stack.AddChild(author);
        stack.AddChild(name);
        var choices = new HBoxContainer();
        stack.AddChild(choices);
        var packColumn = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var cardColumn = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        choices.AddChild(packColumn);
        choices.AddChild(cardColumn);
        packColumn.AddChild(new Label { Text = "Allowed packs (Ctrl-click to select several)" });
        packColumn.AddChild(packs);
        var all = new Button { Text = "Allow all packs" };
        all.Pressed += () => { for (int index = 0; index < packs.ItemCount; index++) packs.Select(index, false); };
        packColumn.AddChild(all);
        cardColumn.AddChild(new Label { Text = "Must include cards (Ctrl-click to select several)" });
        var cardQuery = new LineEdit { PlaceholderText = "Find an included card by name…" };
        cardColumn.AddChild(cardQuery);
        cardColumn.AddChild(cards);
        cardQuery.TextChanged += FilterCards;
        cards.MultiSelected += (index, selected) =>
        {
            string face = visibleCards[(int)index].Face;
            if (selected) included.Add(face); else included.Remove(face);
        };
        FilterCards("");
        var clear = new Button { Text = "Clear included cards" };
        clear.Pressed += () => { included.Clear(); cards.DeselectAll(); };
        cardColumn.AddChild(clear);
        var search = new Button { Text = "Search published decks" };
        search.Pressed += () =>
        {
            if (!initialized) { status.Text = "Wait for search options to load."; return; }
            if (packs.GetSelectedItems().Length == 0) { status.Text = "Select at least one allowed pack."; return; }
            submitted = Query().ToArray();
            Search(1);
        };
        stack.AddChild(search);
    }

    private void FilterCards(string query)
    {
        visibleCards = catalog.Where(card => card.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        cards.Clear();
        for (int index = 0; index < visibleCards.Length; index++)
        {
            var card = visibleCards[index];
            cards.AddItem($"{card.Title} · {card.Subtitle} · {card.SearchFields.GetValueOrDefault("en", "")}");
            if (included.Contains(card.Face)) cards.Select(index, false);
        }
    }
    private IEnumerable<KeyValuePair<string, string>> Query()
    {
        foreach (var (key, select) in filters)
            if (select.Selected >= 0 && initialized) yield return new(key, select.GetItemMetadata(select.Selected).AsString());
        yield return new("author", author.Text);
        yield return new("name", name.Text);
        foreach (int index in packs.GetSelectedItems()) yield return new("packs[]", packKeys[index]);
        foreach (string face in included.Order(StringComparer.Ordinal)) yield return new("cards[]", face);
    }

    private async void Search(int requestedPage)
    {
        pending?.Cancel();
        pending?.Dispose();
        pending = new();
        var attempt = pending;
        previous.Disabled = next.Disabled = true;
        status.Text = "Searching MarvelCDB…";
        try
        {
            var found = await service.SearchAsync(submitted, requestedPage, attempt.Token);
            if (attempt.IsCancellationRequested || disposed) return;
            ShowResults(found, requestedPage);
        }
        catch (OperationCanceledException) { if (Current(attempt)) status.Text = "Search cancelled or timed out. Search again to retry."; }
        catch (Exception error) { if (Current(attempt)) status.Text = "Search failed: " + error.Message; }
    }

    private bool Current(CancellationTokenSource attempt) => !disposed && attempt == pending;
    private void ShowResults(PublishedDeckPage found, int requestedPage)
    {
        current = found;
        page = requestedPage;
        if (!initialized) InitializeFilters(found);
        results.Clear();
        foreach (var deck in found.Decks) results.AddItem($"{deck.Name} · {deck.Hero} · by {deck.Author}");
        if (results.ItemCount > 0) results.Select(0);
        status.Text = $"Page {page} · {found.Decks.Count} published decks. Import creates a local draft; the game checks Core compatibility.";
        previous.Disabled = page <= 1;
        next.Disabled = !found.HasNext;
    }

    private void InitializeFilters(PublishedDeckPage found)
    {
        foreach (var (key, select) in filters)
        {
            select.Clear();
            foreach (var (value, label) in found.Filters[key])
            {
                select.AddItem(label);
                select.SetItemMetadata(select.ItemCount - 1, value);
            }
        }
        packKeys = found.Packs.Keys.ToArray();
        foreach (string label in found.Packs.Values) packs.AddItem(label);
        for (int index = 0; index < packs.ItemCount; index++) packs.Select(index, false);
        initialized = true;
    }

    private void Import()
    {
        if (current is null || results.GetSelectedItems() is not { Length: > 0 } selected) return;
        string url = current.Decks[selected[0]].Url;
        window.QueueFree();
        import(url);
    }

    public void Dispose()
    {
        disposed = true;
        pending?.Cancel();
        pending?.Dispose();
        service.Dispose();
    }
}
