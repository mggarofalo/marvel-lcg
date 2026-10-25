using System.Text.Json;
using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Synthetic topology layout fixture; it neither admits content nor creates a game.</summary>
public sealed partial class EncounterGroupsNativeProbe : Node
{
    private BoardRenderResult? result;
    public int InspectorTarget() => GetTree().Root.FindChild("PileInspector", true, false)?
        .FindChildren("*", "", true, false).OfType<CardControl>().FirstOrDefault()?.TargetId ?? -1;

    public void Show(string json)
    {
        var board = JsonSerializer.Deserialize<BoardPresentation>(json)!;
        var surface = new Control { Name = "EncounterGroupFixture", Position = new Vector2(20, 40),
            Size = new Vector2(1320, 962), MouseFilter = Control.MouseFilterEnum.Ignore };
        ClientThemeInstallation.Apply(surface, ClientTheme.ConfiguredScale());
        AddChild(surface);
        result = new BoardRenderResult(board) { IsCurrent = () => true };
        result.CardPreviewEntered += (card, _) =>
        {
            surface.SetMeta("preview_id", card.TargetId ?? -1);
            surface.SetMeta("preview_text", card.RulesText);
        };
        var geometry = new AstraTableGeometry(1320, 962, ClientTheme.ConfiguredScale() >= InterfaceScale.Percent130)
            { HasEncounterGroups = true };
        var objects = new SpatialTableObjectRenderer(surface, result, geometry,
            new SpatialCardFactory(InterfaceScale.Percent100, null, null, ClientTheme.ConfiguredScale()), false);
        objects.RenderScenario(board.Areas, board.EncounterGroups);
        objects.RenderPlayer(board.Areas.Where(area => area.Seat == 0).ToArray());
        objects.RenderHand(board.Areas.FirstOrDefault(area => area.Zone == "HandsArea"));
        surface.AddChild(new Label { Name = "FixtureScope", Position = new Vector2(22, 720),
            Text = "SYNTHETIC LAYOUT ONLY — no scenario is running; no pack admission", MouseFilter = Control.MouseFilterEnum.Ignore });
    }
}
