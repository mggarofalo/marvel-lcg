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
        InterfaceScale scale = ui.Scale;
        ui.Body.AddChild(CampaignBlocks.Title("NewCampaign", "New campaign", scale,
            "Choose your heroes for the whole campaign. You can edit their decks between scenarios."));
        var name = new LineEdit { Text = CampaignWording.Product, PlaceholderText = "Campaign name", TooltipText = "Campaign name" };
        name.AddThemeFontSizeOverride("font_size", VisualSystem.Type(scale).Body);
        ui.Body.AddChild(name);
        var expert = new CheckButton { Text = "Expert campaign", TooltipText = CampaignWording.Difficulty(true).Tooltip };
        expert.AddThemeFontSizeOverride("font_size", VisualSystem.Type(scale).Body);
        ui.Body.AddChild(expert);
        var players = new List<OptionButton>();
        for (int seat = 0; seat < 4; seat++)
        {
            HFlowContainer row = CampaignBlocks.Row("RosterSeat" + seat, scale);
            row.AddChild(Chip.Create("Seat", CampaignWording.Seat(seat), null, scale));
            var hero = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(320, 0) };
            hero.AddThemeFontSizeOverride("font_size", VisualSystem.Type(scale).Body);
            if (seat > 0) hero.AddItem("Not playing");
            foreach (var choice in catalog.Heroes) hero.AddItem(choice.Name);
            row.AddChild(hero); ui.Body.AddChild(row); players.Add(hero);
        }
        CampaignBlocks.Action(ui.Body, "Create campaign", () =>
        {
            var roster = players.Select((control, seat) => seat == 0 ? control.Selected : control.Selected - 1)
                .Where(index => index >= 0).Select(index => catalog.Heroes[index]).ToArray();
            create(name.Text, new("create") { Product = product, Expert = expert.ButtonPressed, Seed = GameSeed.Create(),
                Heroes = [.. roster.Select(hero => hero.Key)], Decks = [.. roster.Select(hero => new PlayerDeckRecipe(hero.Starter))] });
        }, scale);
    }
}
