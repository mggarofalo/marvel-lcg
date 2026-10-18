using Godot;
using Marvel.Client;
using Marvel.Protocol;

namespace Marvel.Godot;

/// <summary>Filters public facts and renders exact draft copies without deciding legality.</summary>
internal sealed class DeckBrowserPresentation(DeckEditorControls ui, DeckWorkspace workspace)
{
    internal Action<bool>? SelectionChanged { get; set; }
    private DeckCard[] visible = [];
    private string[] selected = [];

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
        ui.Cards.MultiSelected += (index, _) => { Inspect(visible[(int)index].Face); SelectionChanged?.Invoke(true); };
        ui.Deck.MultiSelected += (index, _) => { Inspect(selected[(int)index]); SelectionChanged?.Invoke(false); };
        ui.ClearSearch.Pressed += () => { ui.Search.Text = ""; ui.CardScope.Select(0); ui.Aspect.Select(0); ui.Kind.Select(0); Filter(); };
        Filter();
    }

    internal string[] SelectedFaces(bool browse) => browse
        ? ui.Cards.GetSelectedItems().Select(index => visible[index].Face).ToArray()
        : ui.Deck.GetSelectedItems().Select(index => selected[index]).ToArray();

    internal string? BrowserFace => ui.Cards.GetSelectedItems() is { Length: > 0 } items ? visible[items[0]].Face : null;
    internal string? DeckFace => ui.Deck.GetSelectedItems() is { Length: > 0 } items ? selected[items[0]] : null;

    internal void Render()
    {
        var retained = SelectedFaces(false);
        var draft = workspace.Draft!;
        selected = draft.Cards.Distinct(StringComparer.Ordinal).OrderBy(Title, StringComparer.Ordinal).ToArray();
        ui.Deck.Clear();
        foreach (string face in selected) ui.Deck.AddItem($"{draft.Cards.Count(item => item == face)} × {Title(face)}");
        for (int index = 0; index < selected.Length; index++)
            if (retained.Contains(selected[index], StringComparer.Ordinal)) ui.Deck.Select(index, false);
        ui.Count.Text = $"{draft.Cards.Length} cards in draft · {(workspace.Dirty ? "unsaved changes" : workspace.Saved is null ? "draft" : "saved")}";
        ui.SeatOne.Disabled = ui.SeatTwo.Disabled = !workspace.CanUse;
        ui.Diagnostics.Text = workspace.Assessment is not { } assessment ? "Checking this draft with the game service…"
            : assessment.Diagnostics.Count == 0 ? $"Ready to use · {assessment.Size} counted cards. Choosing a seat captures this deck for setup."
            : string.Join("\n", assessment.Diagnostics.Select(item =>
                (item.Face.Length == 0 ? "" : Title(item.Face) + ": ") + item.Message));
    }

    private void Filter()
    {
        string query = ui.Search.Text.Trim();
        try
        {
            visible = Sorted(CardSearch.Find(workspace.Catalog.Cards, query).Where(InScope).Where(card =>
                    (ui.Aspect.Selected <= 0 || card.Aspect == ui.Aspect.GetItemText(ui.Aspect.Selected))
                    && (ui.Kind.Selected <= 0 || card.Kind == ui.Kind.GetItemText(ui.Kind.Selected)))
                ).ThenBy(card => card.Title, StringComparer.Ordinal).ThenBy(card => card.Face, StringComparer.Ordinal).ToArray();
            ui.SearchError.Text = $"{visible.Length} cards match";
        }
        catch (FormatException error) { ui.SearchError.Text = error.Message; return; }
        ui.Cards.Clear();
        foreach (var card in visible) ui.Cards.AddItem($"{card.Title} · {card.Aspect} · {card.Cost ?? "—"}");
        if (visible.Length > 0) { ui.Cards.Select(0); Inspect(visible[0].Face); }
        else ShowPreview(null, "No cards match these filters.");
        SelectionChanged?.Invoke(true);
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

    private string Title(string face) => workspace.Catalog.Cards.FirstOrDefault(card => card.Face == face)?.Title ?? face;

    internal void InspectIdentity()
    {
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
        var card = workspace.Catalog.Cards.FirstOrDefault(item => item.Face == face);
        ShowPreview(card?.Preview, card is null ? $"{face}\nUnsupported or unknown face. It remains in your draft until you remove it."
            : $"{card.Title} {card.Subtitle}\n{card.Kind} · {card.Aspect} · Cost: {card.Cost ?? "—"}\n{string.Join(" · ", card.Traits)}\n{card.Text}");
    }
}
