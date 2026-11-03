using Godot;
using Marvel.Client;
using Marvel.Protocol;
namespace Marvel.Godot;

/// <summary>Campaign list navigation using last confirmed summaries and stable local identities.</summary>
internal static class CampaignLibrary
{
    internal static void Render(CampaignControls ui, IReadOnlyList<CampaignConnection> saved,
        CampaignConnection? selected, DeckCatalog? catalog, Action<int> open)
    {
        foreach (Node child in ui.Saved.GetChildren()) { ui.Saved.RemoveChild(child); child.QueueFree(); }
        ui.Selected = saved.ToList().FindIndex(item => item.LocalId == selected?.LocalId);
        int visible = 0;
        foreach (var (item, index) in saved.Select((item, index) => (item, index)))
        {
            if ((item.Snapshot?.Archived ?? false) != ui.ShowArchived.ButtonPressed) continue;
            visible++;
            // The campaign's name opens it; its state stands beneath as chips.
            var button = new Button { Text = item.Label, Alignment = HorizontalAlignment.Left,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, CustomMinimumSize = new(280, 0),
                ToggleMode = true, ButtonPressed = item.LocalId == selected?.LocalId, TooltipText = Heroes(item, catalog) };
            button.AddThemeFontSizeOverride("font_size", VisualSystem.Type(ui.Scale).Body);
            button.Pressed += () => open(index);
            ui.Saved.AddChild(button);
            HFlowContainer facts = CampaignBlocks.Row("CampaignSummary" + index, ui.Scale);
            CampaignBlocks.Chips(facts, "SummaryFact", Summary(item), ui.Scale);
            ui.Saved.AddChild(facts);
        }
        if (visible == 0)
        {
            Label empty = TableBlockStyle.Whole("NoCampaigns", CampaignWording.Empty(ui.ShowArchived.ButtonPressed),
                ui.Scale, VisualSystem.Palette.MutedText);
            empty.TooltipText = ui.ShowArchived.ButtonPressed ? "" : "Create a campaign or import a backup.";
            ui.Saved.AddChild(empty);
        }
    }

    /// <summary>A saved campaign's state as facts: its scenario and phase, or why it cannot be read yet.</summary>
    internal static IEnumerable<Wording> Summary(CampaignConnection item)
    {
        if (item.Pending is not null) { yield return CampaignWording.AwaitingRecovery; yield break; }
        if (item.Refused) { yield return CampaignWording.DeleteOnly; yield break; }
        if (item.Snapshot is not { } summary) { yield return CampaignWording.NotLoaded; yield break; }
        yield return new("", CampaignWording.Stage(summary.State.Stage), CampaignWording.StageTracker(summary.State.Stage).Tooltip);
        yield return CampaignWording.Phase(summary.State.Phase);
        if (summary.State.Expert) yield return CampaignWording.Difficulty(true);
    }

    private static string Heroes(CampaignConnection item, DeckCatalog? catalog) => item.Snapshot is not { } summary ? item.Label
        : item.Label + ": " + string.Join(", ", summary.State.Players.Select(player =>
            catalog?.Heroes.FirstOrDefault(hero => hero.Key == player.Hero)?.Name ?? player.Hero.Replace('_', ' ')));
}
