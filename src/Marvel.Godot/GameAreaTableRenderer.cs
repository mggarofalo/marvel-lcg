using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Places the separated table: the tracker line, then one column per game area with the shared column between them.</summary>
/// <remarks>
/// Column contents and facts come from <see cref="GameAreaTableProjection"/>. The band is grid columns 2–9;
/// a game area weighs a fifth more than the shared column. Positions, sizes and the dimming of
/// out-of-reach cards are presentation choices. No sentence is placed on the table: each column says
/// what it is with a one-line header and icons, and explains itself on hover.
/// </remarks>
internal sealed class GameAreaTableRenderer(
    Control surface, BoardRenderResult result, SpatialCardFactory faces, SpatialTablePileRenderer piles)
{
    private const float AreaWeight = 1.2f;
    private const float AttachmentRow = 30;

    internal IReadOnlyList<CardControl> Render(BoardPresentation board, GameAreaTable table, AstraTableGeometry geometry,
        IReadOnlyList<BoardAreaPresentation> hosted)
    {
        var visible = board.Areas.SelectMany(SpatialTableZones.Current)
            .Where(card => card.TargetId is not null).DistinctBy(card => card.TargetId)
            .ToDictionary(card => card.TargetId!.Value);
        Rect2 band = geometry.GameAreaBand;
        InterfaceScale scale = geometry.ReadingScale;
        float line = GameAreaTableChrome.AddTrackerLine(surface, band, table, scale);
        float top = band.Position.Y + line + VisualSystem.Spacing(scale).ExtraSmall;
        var columns = TableGrid.Divide(band.Position.X, band.Size.X,
            [.. table.Columns.Select(column => column.Central ? 1f : AreaWeight)], geometry.Grid.Gutter / 2);
        var pass = new Pass(board, hosted, table, visible, scale, []);
        for (int index = 0; index < table.Columns.Count; index++)
            RenderColumn(pass, table.Columns[index],
                new Rect2(columns[index].X, top, columns[index].Width, band.End.Y - top));
        return pass.Controls;
    }

    // One render's inputs, shared by every column.
    private sealed record Pass(BoardPresentation Board, IReadOnlyList<BoardAreaPresentation> Hosted, GameAreaTable Table,
        Dictionary<int, BoardCardPresentation> Visible, InterfaceScale Scale, List<CardControl> Controls);

    private void RenderColumn(Pass pass, GameAreaColumn column, Rect2 bounds)
    {
        var (board, hosted, table, visible, scale, controls) = pass;
        string name = column.GameArea is { } id ? $"GameArea{id}" : "GameAreaCentral";
        GameAreaTableChrome.AddFrame(surface, name, bounds, column);
        GameAreaTableChrome.AddHeader(surface, name, bounds, column, scale);
        BoardCardPresentation[] objects = [.. column.Villains.Concat(column.Schemes)
            .Where(visible.ContainsKey).Select(card => visible[card])];
        BoardCardPresentation[] encounter = [.. column.Encounter.Where(visible.ContainsKey).Select(card => visible[card])];
        var layout = GameAreaColumnLayout.Arrange(bounds.Size.Y, ColumnHeader.Height(scale), Attachments(hosted, objects));
        var room = new Rect2(bounds.Position.X + GameAreaColumnLayout.Padding, bounds.Position.Y + layout.CardsTop,
            bounds.Size.X - 2 * GameAreaColumnLayout.Padding, layout.CardsHeight);
        if (column.Central) room = RenderSharedPiles(board, room, scale);
        RenderCards(table, objects, encounter, room, controls);
    }

    // The shared encounter deck and discard stand in the central column, between the game areas.
    private Rect2 RenderSharedPiles(BoardPresentation board, Rect2 room, InterfaceScale scale)
    {
        if (board.Kang is not { } kang) return room;
        (Rect2 stage, Rect2 discard, Rect2 deck) = GameAreaColumnLayout.Central(room, VisualSystem.Spacing(scale).Small);
        if (board.Areas.FirstOrDefault(area => area.Id == kang.EncounterDiscard) is { } discardPile)
            piles.RenderNamed(discardPile, discard, "Shared encounter discard pile", stacked: true);
        if (board.Areas.FirstOrDefault(area => area.Id == kang.EncounterDeck) is { } deckPile)
            piles.RenderNamed(deckPile, deck, "Shared encounter deck", stacked: true);
        return stage;
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
    private float Attachments(IReadOnlyList<BoardAreaPresentation> hosted, BoardCardPresentation[] objects)
    {
        int rows = objects.Where(card => card.TargetId is not null)
            .Max(card => (int?)SpatialTableHostAttachments.Rows(hosted, card.TargetId!.Value)) ?? 0;
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
        if (outOfReach) control.Modulate = new Color(1, 1, 1, GameAreaTableChrome.DimmedAlpha);
    }
}
