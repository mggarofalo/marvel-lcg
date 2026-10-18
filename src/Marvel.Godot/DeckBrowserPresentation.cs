using Godot;
using Marvel.Client;
using Marvel.Protocol;

namespace Marvel.Godot;

/// <summary>Filters public facts and renders exact draft copies without deciding legality.</summary>
internal sealed class DeckBrowserPresentation(DeckEditorControls ui, DeckWorkspace workspace)
{
    private DeckCard[] visible = [];
    private string[] selected = [];

    internal void Initialize()
    {
        ui.Aspect.AddItem("All aspects");
        foreach (string value in workspace.Catalog.Cards.Select(card => card.Aspect).Distinct().Order()) ui.Aspect.AddItem(value);
        ui.Kind.AddItem("All types");
        foreach (string value in workspace.Catalog.Cards.Select(card => card.Kind).Distinct().Order()) ui.Kind.AddItem(value);
        ui.Search.TextChanged += _ => Filter();
        ui.Aspect.ItemSelected += _ => Filter();
        ui.Kind.ItemSelected += _ => Filter();
        ui.Cards.ItemSelected += index => Inspect(visible[(int)index].Face);
        ui.Deck.ItemSelected += index => Inspect(selected[(int)index]);
        Filter();
    }

    internal string? BrowserFace => ui.Cards.GetSelectedItems() is { Length: > 0 } items ? visible[items[0]].Face : null;
    internal string? DeckFace => ui.Deck.GetSelectedItems() is { Length: > 0 } items ? selected[items[0]] : null;

    internal void Render()
    {
        string? retained = DeckFace;
        var draft = workspace.Draft!;
        selected = draft.Cards.Distinct(StringComparer.Ordinal).OrderBy(Title, StringComparer.Ordinal).ToArray();
        ui.Deck.Clear();
        foreach (string face in selected) ui.Deck.AddItem($"{draft.Cards.Count(item => item == face)} × {Title(face)} [{face}]");
        int index = selected.ToList().IndexOf(retained ?? "");
        if (index >= 0) ui.Deck.Select(index);
        ui.Count.Text = $"{draft.Cards.Length} cards in draft · {(workspace.Dirty ? "unsaved changes" : "saved")}";
        ui.SeatOne.Disabled = ui.SeatTwo.Disabled = !workspace.CanUse;
        ui.Diagnostics.Text = workspace.Assessment is not { } assessment ? "Checking this draft with the game service…"
            : assessment.Diagnostics.Count == 0 ? $"Ready to use · {assessment.Size} counted cards. Choosing a seat captures this deck for setup."
            : string.Join("\n", assessment.Diagnostics.Select(item =>
                (item.Face.Length == 0 ? "" : Title(item.Face) + ": ") + item.Message));
    }

    private void Filter()
    {
        string query = ui.Search.Text.Trim();
        visible = workspace.Catalog.Cards.Where(card =>
                (ui.Aspect.Selected <= 0 || card.Aspect == ui.Aspect.GetItemText(ui.Aspect.Selected))
                && (ui.Kind.Selected <= 0 || card.Kind == ui.Kind.GetItemText(ui.Kind.Selected))
                && string.Join(" ", card.Title, card.Subtitle, card.Cost, string.Join(" ", card.Traits))
                    .Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(card => card.Title, StringComparer.Ordinal).ThenBy(card => card.Face, StringComparer.Ordinal).ToArray();
        ui.Cards.Clear();
        foreach (var card in visible) ui.Cards.AddItem($"{card.Title} · {card.Aspect} · {card.Cost ?? "—"} [{card.Face}]");
        if (visible.Length > 0) { ui.Cards.Select(0); Inspect(visible[0].Face); }
        else ui.Inspector.Text = "No supported cards match these filters.";
    }

    private string Title(string face) => workspace.Catalog.Cards.FirstOrDefault(card => card.Face == face)?.Title ?? face;

    private void Inspect(string face)
    {
        var card = workspace.Catalog.Cards.FirstOrDefault(item => item.Face == face);
        ui.Inspector.Text = card is null ? $"{face}\nUnsupported or unknown face. It remains in your draft until you remove it."
            : $"{card.Title} {card.Subtitle}\n{card.Kind} · {card.Aspect} · Cost: {card.Cost ?? "—"}\n{string.Join(" · ", card.Traits)}\n{card.Text}";
    }
}
