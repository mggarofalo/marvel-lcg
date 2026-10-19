using System.Collections.Immutable;
using Godot;
using Marvel.Client;
using Marvel.Protocol;
using Marvel.Rules.Setup;
namespace Marvel.Godot;

/// <summary>Collects a stable one-to-four-player roster before creating campaign authority.</summary>
internal static class CampaignCreationForm
{
    internal static void Render(CampaignControls ui, DeckCatalog catalog, string product, Action<string, CampaignQuery> create)
    {
        ui.Clear();
        ui.Body.AddChild(CampaignControls.Text("Rise of Red Skull"));
        ui.Body.AddChild(CampaignControls.Text("Choose your heroes for the whole campaign. You can edit their decks between scenarios."));
        var name = new LineEdit { Text = "Rise of Red Skull", PlaceholderText = "Campaign name" };
        ui.Body.AddChild(name);
        var expert = new CheckButton { Text = "Expert campaign", TooltipText = "Persistent HP, recovery obligations, and a final defeat against Red Skull ends the campaign." };
        ui.Body.AddChild(expert);
        var players = new List<OptionButton>();
        for (int seat = 0; seat < 4; seat++)
        {
            var row = new HBoxContainer();
            row.AddChild(CampaignControls.Text($"Player {seat + 1}"));
            var hero = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            if (seat > 0) hero.AddItem("Not playing");
            foreach (var choice in catalog.Heroes) hero.AddItem(choice.Name);
            row.AddChild(hero); ui.Body.AddChild(row); players.Add(hero);
        }
        CampaignControls.Action(ui.Body, "Create campaign", () =>
        {
            var roster = players.Select((control, seat) => seat == 0 ? control.Selected : control.Selected - 1)
                .Where(index => index >= 0).Select(index => catalog.Heroes[index]).ToArray();
            create(name.Text, new("create") { Product = product, Expert = expert.ButtonPressed, Seed = GameSeed.Create(),
                Heroes = [.. roster.Select(hero => hero.Key)], Decks = [.. roster.Select(hero => new PlayerDeckRecipe(hero.Starter))] });
        });
    }
}
