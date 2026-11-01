using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Places the separated table: shared piles, the central column and one column per game area.</summary>
/// <remarks>
/// Column contents and markers come from <see cref="GameAreaTableProjection"/>. Positions, sizes
/// and the dimming of out-of-reach cards are presentation choices.
/// </remarks>
internal sealed class GameAreaTableRenderer(
    Control surface, BoardRenderResult result, SpatialCardFactory faces, SpatialTablePileRenderer piles)
{
    private const float HeaderTop = 10;
    private const float ColumnBottom = 342;
    private const float OutOfReachAlpha = 0.45f;
    private const float DetailsHeight = 72;

    internal IReadOnlyList<CardControl> Render(BoardPresentation board, GameAreaTable table, AstraTableGeometry geometry)
    {
        var visible = board.Areas.SelectMany(SpatialTableZones.Current)
            .Where(card => card.TargetId is not null).DistinctBy(card => card.TargetId)
            .ToDictionary(card => card.TargetId!.Value);
        RenderSharedPiles(board, table, geometry);
        float left = geometry.MainScheme.Position.X;
        float right = geometry.EngagedEnemies.Position.X - 12;
        // Three caption lines of progress and acting text, growing with the reading scale.
        float header = 54 * Math.Max(1, (int)faces.ReadingScale / 100f);
        GameAreaTableChrome.AddHeader(surface, new Rect2(left, HeaderTop, right - left, header), table);
        float top = HeaderTop + header + 6;
        var cards = new List<CardControl>();
        float width = (right - left) / table.Columns.Count;
        for (int index = 0; index < table.Columns.Count; index++)
            RenderColumn(table, table.Columns[index], visible,
                new Rect2(left + index * width, top, width - 8, ColumnBottom - top), cards);
        return cards;
    }

    private void RenderSharedPiles(BoardPresentation board, GameAreaTable table, AstraTableGeometry geometry)
    {
        if (board.Kang is not { } kang) return;
        if (board.Areas.FirstOrDefault(area => area.Id == kang.EncounterDiscard) is { } discard)
            piles.RenderNamed(discard, geometry.EncounterDiscard, "Shared encounter discard pile");
        if (board.Areas.FirstOrDefault(area => area.Id == kang.EncounterDeck) is { } deck)
            piles.RenderNamed(deck, geometry.EncounterDeck, "Shared encounter deck");
        if (table.SharedPiles is { } note)
            GameAreaTableChrome.AddSharedCaption(surface, new Rect2(geometry.EncounterDiscard.Position.X, HeaderTop + 4,
                geometry.EncounterDeck.End.X - geometry.EncounterDiscard.Position.X,
                geometry.EncounterDeck.Position.Y - HeaderTop - 8), note);
    }

    private void RenderColumn(GameAreaTable table, GameAreaColumn column,
        Dictionary<int, BoardCardPresentation> visible, Rect2 bounds, List<CardControl> controls)
    {
        string name = column.GameArea is { } id ? $"GameArea{id}" : "GameAreaCentral";
        // Two caption lines each for the heading and markers, growing with the reading scale.
        float lines = 34 * Math.Max(1, (int)faces.ReadingScale / 100f);
        GameAreaTableChrome.AddFrame(surface, name, bounds, column);
        GameAreaTableChrome.AddText(surface, name + "Heading",
            new Rect2(bounds.Position + new Vector2(6, 4), new Vector2(bounds.Size.X - 12, lines)), column.Heading);
        GameAreaTableChrome.AddText(surface, name + "Markers",
            new Rect2(bounds.Position + new Vector2(6, 6 + lines), new Vector2(bounds.Size.X - 12, lines)), column.MarkerText);
        float top = 8 + 2 * lines;
        BoardCardPresentation[] objects = [.. column.Villains.Concat(column.Schemes)
            .Where(visible.ContainsKey).Select(id => visible[id])];
        BoardCardPresentation[] encounter = [.. column.Encounter.Where(visible.ContainsKey).Select(id => visible[id])];
        int slots = Math.Max(1, objects.Length + (encounter.Length > 0 ? 1 : 0));
        float slot = (bounds.Size.X - 8) / slots;
        var row = new Rect2(bounds.Position.X + 4, bounds.Position.Y + top, bounds.Size.X - 8, bounds.Size.Y - top - DetailsHeight - 4);
        result.Inspector.Register([.. objects, .. encounter]);
        for (int index = 0; index < objects.Length; index++)
            AddFace(objects[index], new Rect2(row.Position.X + index * slot, row.Position.Y, slot - 4, row.Size.Y),
                table, controls);
        if (encounter.Length > 0)
        {
            var stack = SchemeStackCollection.Render(surface,
                new Rect2(row.Position.X + objects.Length * slot, row.Position.Y, slot - 4, row.Size.Y), encounter, result, faces);
            foreach (CardControl control in stack) Dim(control, table);
            controls.AddRange(stack);
        }
        GameAreaTableChrome.AddDetails(surface, name + "Details",
            new Rect2(bounds.Position + new Vector2(6, bounds.Size.Y - DetailsHeight - 2), new Vector2(bounds.Size.X - 12, DetailsHeight)),
            column.Details);
    }

    private void AddFace(BoardCardPresentation card, Rect2 bounds, GameAreaTable table, List<CardControl> controls)
    {
        var layout = CardControl.LayoutFor(card, CardDisplaySize.Board, faces.Scale);
        float factor = Math.Min(1, Math.Min(bounds.Size.X / layout.Width, bounds.Size.Y / layout.MinimumHeight));
        var control = faces.CreateFitted(card, factor);
        control.Name = $"ProceduralCard{card.TargetId}";
        control.Position = bounds.Position + new Vector2((bounds.Size.X - control.CustomMinimumSize.X) / 2, 0);
        control.Size = control.CustomMinimumSize;
        control.ZIndex = 10;
        control.SetMeta("spatial_resting_z", 10);
        surface.AddChild(control);
        SpatialCardSidecar.State(control, card);
        if (card.TargetId is { } id) result.Register(id, control);
        result.TrackCard(control, card);
        Dim(control, table);
        controls.Add(control);
    }

    private static void Dim(CardControl control, GameAreaTable table)
    {
        bool outOfReach = control.TargetId is { } id && table.OutOfReach.Contains(id);
        control.SetMeta("out_of_reach", outOfReach);
        if (outOfReach) control.Modulate = new Color(1, 1, 1, OutOfReachAlpha);
    }
}
