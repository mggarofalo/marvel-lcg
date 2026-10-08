using System.Text.Json;
using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Exercises the real bounded inspector with external authorized Core snapshots.</summary>
internal static class CardInspectionSample
{
    internal static bool TryStart(Control owner)
    {
        string? option = OS.GetCmdlineUserArgs().FirstOrDefault(value => value.StartsWith("--marvel-b1-inspection=", StringComparison.Ordinal));
        if (option is null) return false;
        owner.SetProcessInput(false);
        foreach (Node child in owner.GetChildren()) if (child is CanvasItem item) item.Hide();
        var root = new Control { Name = "B1InspectionFixtures", Theme = ClientTheme.Create() };
        owner.AddChild(root);
        BoardCardPresentation[] cards = JsonSerializer.Deserialize<BoardCardPresentation[]>(File.ReadAllText(option["--marvel-b1-inspection=".Length..]))!;
        foreach (BoardCardPresentation card in cards.Where(card => !card.Concealed))
            foreach (Vector2 corner in new[] { new Vector2(12, 12), new Vector2(1700, 12), new Vector2(12, 790), new Vector2(1700, 790) })
                Add(root, card, cards, corner);
        return true;
    }

    private static void Add(Control root, BoardCardPresentation card, BoardCardPresentation[] all, Vector2 position)
    {
        var fixture = new Control { Name = $"Inspection{root.GetChildCount()}", Visible = false };
        root.AddChild(fixture);
        fixture.SetMeta("title", card.Title);
        fixture.SetMeta("rules", card.RulesText);
        fixture.SetMeta("historical", card.TargetId is null);
        CardControl face = CardControl.Create(card, CardDisplaySize.Board,
            SpatialCardMetrics.TableScale(ClientTheme.ConfiguredScale()));
        face.Name = "InspectionSource";
        face.Position = position;
        fixture.AddChild(face);
        face.Size = face.CustomMinimumSize;
        if (SpatialTableZones.IsExhausted(card)) { face.PivotOffset = face.Size / 2; face.Rotation = Mathf.Pi / 2; }
        AddNeighbor(fixture, face);
        var snapshot = new BoardPresentation([new(1, "Authorized cards", "", all, [])]);
        var result = new BoardRenderResult(snapshot) { IsCurrent = () => true };
        var area = new BoardAreaPresentation(1, "Card details", "", [card, .. all.Where(value => value.Concealed)], []);
        result.CardActivated += (selected, source) => TabletopPileInspector.Show(source,
            TabletopAreaObject.From(area), result, ClientTheme.ConfiguredScale(), null, allowActions: false);
        result.TrackCard(face, card);
        face.GuiInput += input =>
        {
            if (input is InputEventMouseButton { Pressed: false }) result.RoutePointer(input);
        };
    }
    private static void AddNeighbor(Control fixture, CardControl face)
    {
        if (face.Position.Y < 100 || face.Rotation != 0) return;
        var neighbor = new Button { Name = "InspectionNeighbor", Text = "?", Size = new Vector2(44, 44) };
        fixture.AddChild(neighbor);
        fixture.MoveChild(neighbor, 0);
        neighbor.Position = face.Position + new Vector2(face.Size.X - 50, -32);
        neighbor.Pressed += () => fixture.SetMeta("neighbor_activated", true);
    }
}
