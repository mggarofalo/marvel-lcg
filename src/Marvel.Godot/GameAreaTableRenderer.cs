using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Places the separated table: shared piles, the central column and one column per game area.</summary>
/// <remarks>
/// Column contents and markers come from <see cref="GameAreaTableProjection"/>. Positions, sizes
/// and the dimming of out-of-reach cards are presentation choices. Text is measured at the
/// current reading scale and placed first; cards take the room that remains.
/// </remarks>
internal sealed class GameAreaTableRenderer(
    Control surface, BoardRenderResult result, SpatialCardFactory faces, SpatialTablePileRenderer piles)
{
    private const float HeaderTop = 10;
    private const float OutOfReachAlpha = 0.45f;
    private const float AttachmentRow = 30;

    internal IReadOnlyList<CardControl> Render(BoardPresentation board, GameAreaTable table, AstraTableGeometry geometry)
    {
        var visible = board.Areas.SelectMany(SpatialTableZones.Current)
            .Where(card => card.TargetId is not null).DistinctBy(card => card.TargetId)
            .ToDictionary(card => card.TargetId!.Value);
        RenderSharedPiles(board, table, geometry);
        float left = geometry.MainScheme.Position.X;
        float right = geometry.EngagedEnemies.Position.X - 12;
        float header = GameAreaTableChrome.AddHeader(surface, new Vector2(left, HeaderTop), right - left, table);
        float top = HeaderTop + header + 6;
        float bottom = geometry.VillainMat.End.Y - 4;
        var cards = new List<CardControl>();
        float width = (right - left) / table.Columns.Count;
        for (int index = 0; index < table.Columns.Count; index++)
            RenderColumn(board, table, table.Columns[index], visible,
                new Rect2(left + index * width, top, width - 8, bottom - top), cards);
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
            GameAreaTableChrome.AddSharedCaption(surface, new Vector2(geometry.EncounterDiscard.Position.X, HeaderTop),
                geometry.EncounterDeck.End.X - geometry.EncounterDiscard.Position.X, note);
    }

    private void RenderColumn(BoardPresentation board, GameAreaTable table, GameAreaColumn column,
        Dictionary<int, BoardCardPresentation> visible, Rect2 bounds, List<CardControl> controls)
    {
        string name = column.GameArea is { } id ? $"GameArea{id}" : "GameAreaCentral";
        GameAreaTableChrome.AddFrame(surface, name, bounds, column);
        float width = bounds.Size.X - 12;
        Label heading = GameAreaTableChrome.AddMeasured(surface, name + "Heading", bounds.Position + new Vector2(6, 4), width, column.Heading);
        heading.TooltipText = string.Join("\n", [column.Heading, .. column.Details]);
        float headingHeight = heading.Size.Y;
        float markers = column.Markers.Count == 0 ? 0 : GameAreaTableChrome.AddMeasured(surface, name + "Markers",
            bounds.Position + new Vector2(6, 6 + headingHeight), width, column.MarkerText).Size.Y;
        BoardCardPresentation[] objects = [.. column.Villains.Concat(column.Schemes)
            .Where(visible.ContainsKey).Select(id => visible[id])];
        BoardCardPresentation[] encounter = [.. column.Encounter.Where(visible.ContainsKey).Select(id => visible[id])];
        var details = GameAreaTableChrome.AddDetails(surface, name + "Details", width, column.Details);
        var layout = GameAreaColumnLayout.Arrange(bounds.Size.Y,
            new GameAreaColumnText(headingHeight, markers, details?.Measured ?? 0, details?.Line ?? 0),
            Attachments(board, objects));
        details?.Place(bounds.Position + new Vector2(6, layout.DetailsTop), layout.DetailsHeight);
        RenderCards(table, objects, encounter,
            new Rect2(bounds.Position.X + 4, bounds.Position.Y + layout.CardsTop, bounds.Size.X - 8, layout.CardsHeight), controls);
    }

    private void RenderCards(GameAreaTable table, BoardCardPresentation[] objects, BoardCardPresentation[] encounter,
        Rect2 row, List<CardControl> controls)
    {
        int slots = Math.Max(1, objects.Length + (encounter.Length > 0 ? 1 : 0));
        result.Inspector.Register([.. objects, .. encounter]);
        // A face takes no more width than its height allows, so a stack beside it gets the rest;
        // without a stack the faces are centred.
        float[] widths = [.. objects.Select(card => CardControl.LayoutFor(card, CardDisplaySize.Board, faces.Scale))
            .Select(layout => Math.Min(row.Size.X / slots, row.Size.Y * layout.Width / layout.MinimumHeight + 4))];
        float x = row.Position.X + (encounter.Length == 0 ? (row.Size.X - widths.Sum()) / 2 : 0);
        for (int index = 0; index < objects.Length; index++)
        {
            AddFace(objects[index], new Rect2(x, row.Position.Y, widths[index] - 4, row.Size.Y), table, controls);
            x += widths[index];
        }
        if (encounter.Length == 0) return;
        // The stack's caption and rows can want more width than one slot; the slot clips them.
        var slotClip = new Control
        {
            Name = "GameAreaEncounterSlot", Position = new Vector2(x, row.Position.Y),
            Size = new Vector2(row.End.X - x - 4, row.Size.Y), ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        slotClip.SetMeta("game_area_stack", true);
        surface.AddChild(slotClip);
        var stack = SchemeStackCollection.Render(slotClip, new Rect2(Vector2.Zero, slotClip.Size), encounter, result, faces);
        foreach (CardControl control in stack) Dim(control, table);
        controls.AddRange(stack);
    }

    // RenderHosted hangs each placed card's attachments and hosted piles below it, in a strip
    // SpatialCardFootprint.PlaceSidecar starts 4 pixels under the face and makes at least 44 tall.
    private float Attachments(BoardPresentation board, BoardCardPresentation[] objects)
    {
        int rows = objects.Where(card => card.TargetId is not null)
            .Max(card => (int?)SpatialTableHostAttachments.Rows(board.Areas, card.TargetId!.Value)) ?? 0;
        return rows == 0 ? 0 : 4 + Math.Max(44, rows * AttachmentRow * Math.Max(1, (int)faces.ReadingScale / 100f));
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
        control.SetMeta("game_area_card", true);
        surface.AddChild(control);
        SpatialCardSidecar.State(control, card);
        if (card.TargetId is { } id) result.Register(id, control);
        result.TrackCard(control, card);
        Dim(control, table);
        controls.Add(control);
    }

    // Attachments and hosted piles hang inside the card's own overlay, so they inherit its dimming.
    private static void Dim(CardControl control, GameAreaTable table)
    {
        bool outOfReach = control.TargetId is { } id && table.OutOfReach.Contains(id);
        control.SetMeta("out_of_reach", outOfReach);
        if (outOfReach) control.Modulate = new Color(1, 1, 1, OutOfReachAlpha);
    }
}
