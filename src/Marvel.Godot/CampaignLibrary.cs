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
            var button = new Button { Text = Summary(item, catalog), Alignment = HorizontalAlignment.Left,
                AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new(280, 100),
                ToggleMode = true, ButtonPressed = item.LocalId == selected?.LocalId };
            button.Pressed += () => open(index);
            ui.Saved.AddChild(button);
        }
        if (visible == 0) ui.Saved.AddChild(CampaignControls.Text(ui.ShowArchived.ButtonPressed
            ? "No archived campaigns." : "No saved campaigns yet.\nCreate one or import a backup."));
    }

    private static string Summary(CampaignConnection item, DeckCatalog? catalog)
    {
        if (item.Pending is not null) return item.Label + "\nChoice awaiting recovery";
        if (item.Snapshot is not { } summary) return item.Label + "\nOpen to load progress";
        string heroes = string.Join(", ", summary.State.Players.Select(player =>
            catalog?.Heroes.FirstOrDefault(hero => hero.Key == player.Hero)?.Name ?? player.Hero.Replace('_', ' ')));
        return item.Label + "\n" + heroes + "\n" + (summary.State.Expert ? "Expert" : "Standard")
            + " campaign · " + CampaignPresentation.Name(summary.State.Stage)
            + "\n" + summary.State.Phase;
    }
}
