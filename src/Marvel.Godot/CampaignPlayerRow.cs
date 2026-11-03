using Godot;
using Marvel.Protocol;
using Marvel.Rules.Campaign;
namespace Marvel.Godot;

/// <summary>One roster seat on a line: its hero, its recorded state as chips, its campaign cards and its deck.</summary>
internal static class CampaignPlayerRow
{
    internal static HFlowContainer Create(CampaignControls ui, CampaignPlayerState player, int seat,
        CampaignDescriptor campaign, DeckCatalog catalog, Action<int> edit, Action<CampaignQuery> choose)
    {
        InterfaceScale scale = ui.Scale;
        var state = campaign.State;
        string name = catalog.Heroes.FirstOrDefault(hero => hero.Key == player.Hero)?.Name ?? player.Hero;
        HFlowContainer row = CampaignBlocks.Row("CampaignPlayer" + seat, scale);
        row.AddChild(Chip.Create("Seat", CampaignWording.Seat(seat), null, scale));
        row.AddChild(CampaignBlocks.Heading("Hero", name, scale));
        CampaignBlocks.Chips(row, "PlayerFact", Facts(player, state), scale);
        foreach (string face in player.Cards.Select(card => card.Face)) row.AddChild(CampaignCardLinks.Link(ui, face, catalog));
        if (campaign.Archived || state.Phase != CampaignPhase.Preparing) return row;
        if (player.RequiresRecovery)
        {
            var participate = new CheckButton { Text = "Joins", ButtonPressed = player.Participating,
                TooltipText = $"Player {seat + 1} joins this attempt by taking a recovery obligation." };
            participate.AddThemeFontSizeOverride("font_size", VisualSystem.Type(scale).Body);
            participate.Toggled += selected => choose(new("participation") { Seat = seat, Participating = selected });
            row.AddChild(participate);
        }
        CampaignBlocks.Action(row, $"Edit deck · {player.Deck.Cards.Count}", () => edit(seat), scale).TooltipText =
            $"Edit {name}'s deck: {player.Deck.Cards.Count} cards.";
        return row;
    }

    /// <summary>The seat's recorded facts: hit points in an expert campaign, recovery owed and sitting out.</summary>
    internal static IEnumerable<Wording> Facts(CampaignPlayerState player, CampaignState state)
    {
        if (state.Expert && player.RecordedHitPoints is { } recorded)
            yield return CampaignWording.HitPoints(recorded, player.BaseHitPoints);
        if (player.RequiresRecovery) yield return CampaignWording.Recovery;
        if (!player.Participating) yield return CampaignWording.SittingOut;
    }
}
