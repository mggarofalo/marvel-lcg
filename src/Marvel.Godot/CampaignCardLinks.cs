using Godot;
using Marvel.Protocol;
namespace Marvel.Godot;

/// <summary>Compact inspectable printed references for the authoritative campaign log.</summary>
internal static class CampaignCardLinks
{
    internal static void Section(CampaignControls ui, string label, IEnumerable<string> faces, DeckCatalog catalog)
    {
        var cards = faces.ToArray();
        if (cards.Length == 0) return;
        ui.Body.AddChild(CampaignControls.Text(label));
        var links = new HFlowContainer();
        links.AddThemeConstantOverride("h_separation", 16);
        ui.Body.AddChild(links);
        foreach (string face in cards)
        {
            var card = catalog.Cards.FirstOrDefault(candidate => candidate.Face == face);
            var link = new LinkButton { Text = card?.Title ?? "Unavailable card", FocusMode = Control.FocusModeEnum.All };
            links.AddChild(link);
            if (card is null) continue;
            Bind(ui, link, card);
            link.Pressed += () => ui.Inspect(card);
        }
    }

    internal static void Bind(CampaignControls ui, Control control, DeckCard card)
    {
        control.MouseEntered += () => ui.Inspect(card);
        control.FocusEntered += () => ui.Inspect(card);
    }
}
