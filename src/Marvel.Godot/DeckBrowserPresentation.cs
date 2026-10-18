using Godot;
using Marvel.Client;
using Marvel.Protocol;

namespace Marvel.Godot;

/// <summary>Filters public facts and renders exact draft copies without deciding legality.</summary>
internal sealed class DeckBrowserPresentation(DeckEditorControls ui, DeckWorkspace workspace)
{
    internal Action<bool>? SelectionChanged { get; set; }
    private readonly Dictionary<string, DeckCard> cards = workspace.Catalog.Cards.ToDictionary(card => card.Face, StringComparer.Ordinal);
    private DeckCard[] visible = [];
    private long generation = -1;
    private string identity = "";
    private string? inspected;
    private int page;
    private IReadOnlyDictionary<string, int[]> quantities = new Dictionary<string, int[]>();

    internal void Initialize()
    {
        foreach (string field in CardSearch.Fields.Values) ui.SearchField.AddItem(field);
        foreach (string operation in new[] { ":", "!", "<", ">" }) ui.SearchOperator.AddItem(operation);
        ui.AddFilter.Pressed += () =>
        {
            string key = CardSearch.Fields.Keys.ElementAt(ui.SearchField.Selected);
            string value = ui.SearchValue.Text.Replace("\"", "", StringComparison.Ordinal);
            string prefix = key.Length == 0 ? "" : key + ui.SearchOperator.GetItemText(ui.SearchOperator.Selected);
            ui.Search.Text = (ui.Search.Text + " " + prefix + "\"" + value + "\"").Trim();
            Filter();
        };
        foreach (string scope in new[] { "Playable Core", "All research cards", "Player cards", "Encounter cards" }) ui.CardScope.AddItem(scope);
        ui.CardScope.ItemSelected += _ => Filter();
        foreach (string sort in new[] { "Sort: Name", "Sort: Pack", "Sort: Aspect", "Sort: Type", "Sort: Cost" }) ui.Sort.AddItem(sort);
        ui.Sort.ItemSelected += _ => Filter();
        ui.Aspect.AddItem("All aspects");
        foreach (string value in workspace.Catalog.Cards.Select(card => card.Aspect).Distinct().Order()) ui.Aspect.AddItem(value);
        ui.Kind.AddItem("All types");
        foreach (string value in workspace.Catalog.Cards.Select(card => card.Kind).Distinct().Order()) ui.Kind.AddItem(value);
        ui.Search.TextChanged += _ => Filter();
        ui.Aspect.ItemSelected += _ => Filter();
        ui.Kind.ItemSelected += _ => Filter();
        ui.Cards.Inspected = Inspect;
        ui.Deck.Inspected = Inspect;
        ui.Cards.SelectionChanged = () => SelectionChanged?.Invoke(true);
        ui.Deck.SelectionChanged = () => SelectionChanged?.Invoke(false);
        ui.ClearSearch.Pressed += () => { ui.Search.Text = ""; ui.CardScope.Select(0); ui.Aspect.Select(0); ui.Kind.Select(0); Filter(); };
        ui.PreviousPage.Pressed += () => { page--; RenderPool(); SelectionChanged?.Invoke(true); };
        ui.NextPage.Pressed += () => { page++; RenderPool(); SelectionChanged?.Invoke(true); };
        Filter();
    }

    internal string[] SelectedFaces(bool browse) => browse ? ui.Cards.SelectedFaces : ui.Deck.SelectedFaces;

    internal bool Required(string face) => workspace.Catalog.Heroes.FirstOrDefault(hero => hero.IdentityFaces.Contains(workspace.Draft?.HeroCode ?? ""))?.Signature.Contains(face) == true;
    internal string CardTitle(string face) => Title(face);

    internal void Render()
    {
        var draft = workspace.Draft!;
        if (generation != workspace.Generation || identity != draft.HeroCode)
        {
            quantities = new Dictionary<string, int[]>();
            generation = workspace.Generation;
            identity = draft.HeroCode;
        }
        if (workspace.Assessment is { } assessed) quantities = assessed.Quantities;
        ui.Deck.Render(draft.Cards.Distinct(StringComparer.Ordinal).OrderBy(Title, StringComparer.Ordinal).Select(Entry).ToArray());
        RenderPool();
        ui.Count.Text = $"{draft.Cards.Length} cards in draft · {(workspace.Dirty ? "unsaved changes" : workspace.Saved is null ? "draft" : "saved")}";
        ui.SeatOne.Disabled = ui.SeatTwo.Disabled = !workspace.CanUse;
        ui.Diagnostics.Text = workspace.Assessment is not { } assessment ? ui.Diagnostics.Text
            : assessment.Diagnostics.Count == 0 ? $"Ready to use · {assessment.Size} counted cards. Choosing a seat captures this deck for setup."
            : string.Join("\n", assessment.Diagnostics.Select(item =>
                (item.Face.Length == 0 ? "" : Title(item.Face) + ": ") + item.Message));
    }

    private void Filter()
    {
        page = 0;
        string query = ui.Search.Text.Trim();
        try
        {
            visible = Sorted(CardSearch.Find(workspace.Catalog.Cards, query).Where(card => card.SearchFields.GetValueOrDefault("f") != "hero").Where(InScope).Where(card =>
                    (ui.Aspect.Selected <= 0 || card.Aspect == ui.Aspect.GetItemText(ui.Aspect.Selected))
                    && (ui.Kind.Selected <= 0 || card.Kind == ui.Kind.GetItemText(ui.Kind.Selected)))
                ).ThenBy(card => card.Title, StringComparer.Ordinal).ThenBy(card => card.Face, StringComparer.Ordinal).ToArray();
            ui.SearchError.Text = $"{visible.Length} cards match";
        }
        catch (FormatException error) { ui.SearchError.Text = error.Message; return; }
        RenderPool();
        if (visible.Length > 0 && ui.Cards.SelectedFaces.Length == 0) ui.Cards.SelectRow(0, false, false);
        else if (visible.Length == 0) { inspected = null; ShowPreview(null, "No cards match these filters."); }
        SelectionChanged?.Invoke(true);
    }

    private DeckTableEntry Entry(string face)
    {
        var card = cards.GetValueOrDefault(face);
        int count = workspace.Draft?.Cards.Count(item => item == face) ?? 0;
        return new(face, Title(face), card is null ? "Imported card" : $"{card.Aspect} · {card.Kind} · Cost {card.Cost ?? "—"}",
            count, Required(face), quantities.GetValueOrDefault(face) ?? []);
    }

    private void RenderPool()
    {
        const int pageSize = 50;
        ui.Cards.Render(visible.Skip(page * pageSize).Take(pageSize).Select(card => Entry(card.Face)).ToArray());
        ui.PreviousPage.Disabled = page == 0;
        ui.NextPage.Disabled = (page + 1) * pageSize >= visible.Length;
        ui.Page.Text = $"{page + 1} / {Math.Max(1, (visible.Length + pageSize - 1) / pageSize)}";
    }

    private IOrderedEnumerable<DeckCard> Sorted(IEnumerable<DeckCard> cards) => ui.Sort.Selected switch
    {
        1 => cards.OrderBy(card => int.TryParse(card.SearchFields.GetValueOrDefault("ep"), out int value) ? value : int.MaxValue),
        2 => cards.OrderBy(card => card.Aspect, StringComparer.Ordinal),
        3 => cards.OrderBy(card => card.Kind, StringComparer.Ordinal),
        4 => cards.OrderBy(card => int.TryParse(card.Cost, out int value) ? value : int.MaxValue),
        _ => cards.OrderBy(card => card.Title, StringComparer.Ordinal),
    };

    private bool InScope(DeckCard card) => ui.CardScope.Selected switch
    {
        0 => card.RuntimeSupported,
        2 => card.SearchFields.GetValueOrDefault("f") is not ("encounter" or "campaign") && card.SearchFields.ContainsKey("f"),
        3 => card.SearchFields.GetValueOrDefault("f") == "encounter",
        _ => true,
    };

    private string Title(string face) => cards.GetValueOrDefault(face)?.Title ?? "Unrecognized card";

    internal void InspectIdentity()
    {
        string key = "identity:" + workspace.Draft!.HeroCode;
        if (inspected == key) return;
        inspected = key;
        var hero = workspace.Catalog.Heroes.FirstOrDefault(item => item.IdentityFaces.Contains(workspace.Draft!.HeroCode));
        if (hero is null)
        {
            Inspect(workspace.Draft!.HeroCode);
            return;
        }
        ClearPreview();
        ui.Inspector.Visible = false;
        foreach (var face in hero?.Previews ?? []) ui.Preview.AddChild(DeckCardPreview.Create(face));
    }

    private void ClearPreview()
    {
        foreach (var child in ui.Preview.GetChildren().Where(child => child != ui.Inspector))
        {
            ui.Preview.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void ShowPreview(Marvel.View.BoardCardPresentation? preview, string fallback)
    {
        ClearPreview();
        ui.Inspector.Visible = preview is null;
        ui.Inspector.Text = fallback;
        if (preview is not null) ui.Preview.AddChild(DeckCardPreview.Create(preview));
    }

    private void Inspect(string face)
    {
        if (inspected == face) return;
        inspected = face;
        var card = cards.GetValueOrDefault(face);
        ShowPreview(card?.Preview, card is null ? $"{face}\nUnsupported or unknown face. It remains in your draft until you remove it."
            : $"{card.Title} {card.Subtitle}\n{card.Kind} · {card.Aspect} · Cost: {card.Cost ?? "—"}\n{string.Join(" · ", card.Traits)}\n{card.Text}");
    }
}
