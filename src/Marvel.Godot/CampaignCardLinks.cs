using Godot;
using Marvel.Protocol;
namespace Marvel.Godot;

/// <summary>Compact inspectable printed references for the authoritative campaign log.</summary>
internal static class CampaignCardLinks
{
    /// <summary>A row of one section's cards behind its short label chip; nothing when the section is empty.</summary>
    internal static HFlowContainer? Section(CampaignControls ui, CampaignSection section, IEnumerable<string> faces,
        DeckCatalog catalog)
    {
        var cards = faces.ToArray();
        if (cards.Length == 0) return null;
        var links = CampaignBlocks.Row("Section" + section, ui.Scale);
        links.AddChild(Chip.Create("SectionLabel", CampaignWording.Section(section), null, ui.Scale));
        foreach (string face in cards) links.AddChild(Link(ui, face, catalog));
        return links;
    }

    /// <summary>One printed card's name, which previews the card on hover or focus.</summary>
    internal static LinkButton Link(CampaignControls ui, string face, DeckCatalog catalog)
    {
        var card = catalog.Cards.FirstOrDefault(candidate => candidate.Face == face);
        var link = new LinkButton { Text = card?.Title ?? "Unavailable card", FocusMode = Control.FocusModeEnum.All,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        link.AddThemeFontSizeOverride("font_size", VisualSystem.Type(ui.Scale).Body);
        if (card is null) return link;
        Bind(ui, link, card);
        link.Pressed += () => ui.Inspect(card);
        return link;
    }

    internal static void Bind(CampaignControls ui, Control control, DeckCard card)
    {
        control.MouseEntered += () => ui.Inspect(card);
        control.FocusEntered += () => ui.Inspect(card);
    }
}
