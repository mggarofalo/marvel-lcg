using Godot;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Builds the supported desktop route as one stable physical-table scene graph.</summary>
internal static class SpatialTableSurfaceRenderer
{
    internal static BoardRenderResult Render(
        Main main,
        DisplayedSeatSelection selection,
        Action<int> switchSeat,
        Prompt? prompt)
    {
        Prepare(main);
        AstraTableGeometry geometry = Geometry(main);
        Control surface = CreateSurface(main, geometry);
        var result = new BoardRenderResult();
        AddMats(surface, result, geometry, selection);
        RenderObjects(main, result, surface, geometry, selection, prompt);
        AddSeats(surface, geometry, main.boardPresentation!, selection, switchSeat);
        surface.SetMeta("spatial_table_grammar", "astra-far-to-near-v1");
        surface.SetMeta("expanded_seat", selection.ExpandedSeat);
        return result;
    }

    private static void Prepare(Main main)
    {
        BoardRenderCleanup.Clear(main.boardAreas);
        BoardRenderCleanup.Clear(main.handRail);
        main.GetNode<PanelContainer>("Margin/Shell/Content/Play/Board/HandShelf").Visible = false;
    }

    private static AstraTableGeometry Geometry(Main main)
    {
        Vector2 viewport = main.GetViewportRect().Size;
        bool expanded = TableHistoryDrawer.IsExpanded(main);
        float reserved = expanded ? TableHistoryDrawer.ExpandedWidth(main) + 40 : 250;
        float width = Math.Max(expanded ? 920 : 1060, viewport.X - reserved);
        float height = Math.Min(AstraTableGeometry.ReferenceHeight, Math.Max(820, viewport.Y - 118));
        return new AstraTableGeometry(
            width, height, main.interfaceScale >= InterfaceScale.Percent130);
    }

    private static Control CreateSurface(Main main, AstraTableGeometry geometry)
    {
        var surface = new Control
        {
            Name = "AstraTableSurface",
            Size = new Vector2(geometry.Width, geometry.Height),
            CustomMinimumSize = new Vector2(geometry.Width, geometry.Height),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            ClipContents = true,
            MouseFilter = Control.MouseFilterEnum.Pass,
        };
        main.boardAreas.AddChild(surface);
        return surface;
    }

    private static void AddMats(
        Control surface,
        BoardRenderResult result,
        AstraTableGeometry geometry,
        DisplayedSeatSelection selection)
    {
        AddMat(surface, "VillainTable", geometry.VillainMat,
            GodotThemeVariations.SpatialVillainMat, "VILLAIN'S PLAY AREA  ·  FAR SIDE");
        PanelContainer playerMat = AddMat(surface, "PlayerTable", geometry.PlayerMat,
            GodotThemeVariations.SpatialPlayerMat,
            $"PLAYER {selection.ExpandedSeat + 1}  ·  NEAR SIDE");
        result.RegisterDropTarget(selection.ExpandedSeat, playerMat);
        AddConfrontationSeam(surface, geometry, selection.ExpandedSeat);
    }

    private static void RenderObjects(
        Main main,
        BoardRenderResult result,
        Control surface,
        AstraTableGeometry geometry,
        DisplayedSeatSelection selection,
        Prompt? prompt)
    {
        BoardPresentation board = main.boardPresentation!;
        BoardAreaPresentation[] scenario = Lane(board, "scenario");
        BoardAreaPresentation[] player = Lane(board, $"player-{selection.ExpandedSeat}");
        InterfaceScale cardScale = main.interfaceScale > InterfaceScale.Percent110
            ? InterfaceScale.Percent110
            : main.interfaceScale;
        var objects = new SpatialTableObjectRenderer(
            surface, result, geometry, cardScale, main.art, MulliganPrompt.IsOpening(prompt));
        objects.RenderScenario(scenario);
        objects.RenderPlayer(player);
        objects.RenderHosted([.. scenario.Concat(player)]);
        int handSeat = prompt?.Player ?? selection.ExpandedSeat;
        objects.RenderHand(board.Areas.FirstOrDefault(area =>
            area.Zone == "HandsArea" && area.Seat == handSeat));
        RenderDecisionAnchors(main, objects, board, prompt);
        objects.RenderOverflow(SpatialTableObjectRenderer.Unplaced([.. scenario.Concat(player)
            .Concat(Lane(board, "other"))]));

        AddHandCaption(surface, geometry, board, handSeat, prompt);
        SpatialTableContextRenderer.Add(surface, result, geometry, main.CurrentGame?.World, prompt);
    }

    private static void RenderDecisionAnchors(
        Main main,
        SpatialTableObjectRenderer objects,
        BoardPresentation board,
        Prompt? prompt)
    {
        if (prompt is null || main.CurrentGame?.World is not { } world) return;
        PromptPresentation decision = PromptPresentation.From(prompt, world);
        objects.RenderDecisionAnchors(
            board.Areas,
            [.. decision.Affordances
                .Where(affordance => affordance.Illegal is null)
                .Select(affordance => affordance.Source?.CardId)
                .OfType<int>()
                .Distinct()]);
    }

    private static PanelContainer AddMat(
        Control surface,
        string name,
        Rect2 rect,
        string variation,
        string caption)
    {
        var mat = new PanelContainer
        {
            Name = name,
            Position = rect.Position,
            Size = rect.Size,
            CustomMinimumSize = rect.Size,
            ThemeTypeVariation = variation,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 0,
        };
        surface.AddChild(mat);
        surface.AddChild(new Label
        {
            Name = $"{name}Caption",
            Text = caption,
            Position = rect.Position + new Vector2(22, 15),
            ThemeTypeVariation = GodotThemeVariations.Eyebrow,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 2,
        });
        return mat;
    }

    private static void AddConfrontationSeam(
        Control surface, AstraTableGeometry geometry, int expandedSeat)
    {
        Rect2 villain = geometry.VillainMat;
        Rect2 player = geometry.PlayerMat;
        var seam = new HSeparator
        {
            Name = "ConfrontationAxis",
            Position = new Vector2(villain.Position.X + villain.Size.X * 0.36f, villain.End.Y + 10),
            Size = new Vector2(villain.Size.X * 0.42f, 1),
            ZIndex = 3,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        surface.AddChild(seam);
        surface.AddChild(new Label
        {
            Name = "EngagementCaption",
            Text = $"ENGAGED WITH PLAYER {expandedSeat + 1}",
            Position = seam.Position + new Vector2(154, -12),
            ThemeTypeVariation = GodotThemeVariations.Caption,
            ZIndex = 3,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
    }

    private static void AddHandCaption(
        Control surface,
        AstraTableGeometry geometry,
        BoardPresentation board,
        int seat,
        Prompt? prompt)
    {
        BoardAreaPresentation? hand = board.Areas.FirstOrDefault(area =>
            area.Zone == "HandsArea" && area.Seat == seat);
        int visible = hand?.Cards.Where(card => !card.Concealed).Sum(card => card.Count) ?? 0;
        int concealed = hand?.Cards.Where(card => card.Concealed).Sum(card => card.Count) ?? 0;
        string instruction = MulliganPrompt.IsOpening(prompt)
            ? "DRAG TO DISCARD\nOR SELECT A CARD"
            : "DRAG AN OFFERED CARD\nTOWARD YOUR PLAY AREA";
        string heading = MulliganPrompt.IsOpening(prompt)
            ? $"PLAYER {seat + 1} OPENING HAND"
            : $"PLAYER {seat + 1} PRIVATE HAND";
        float left = geometry.PlayerMat.Position.X + 22;
        float top = geometry.PlayerDiscard.End.Y + 24;
        surface.AddChild(new Label
        {
            Name = "SpatialHandHeading",
            Text = $"{heading}\n{visible} VISIBLE"
                + (concealed > 0 ? $" · {concealed} CONCEALED" : string.Empty),
            Position = new Vector2(left, top),
            ThemeTypeVariation = GodotThemeVariations.Eyebrow,
            ZIndex = 60,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        surface.AddChild(new Label
        {
            Name = "SpatialHandInstruction",
            Text = instruction,
            Position = new Vector2(left, top + 52),
            ThemeTypeVariation = GodotThemeVariations.Caption,
            ZIndex = 60,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
    }

    private static void AddSeats(
        Control surface,
        AstraTableGeometry geometry,
        BoardPresentation board,
        DisplayedSeatSelection selection,
        Action<int> switchSeat)
    {
        if (board.PlayerSummaries.Count < 2)
        {
            return;
        }
        Control seats = MulliganSeatStripRenderer.CreateCompact(board, selection, switchSeat);
        seats.Name = "SpatialSeatSummaries";
        seats.Position = geometry.SeatStrip.Position;
        seats.Size = geometry.SeatStrip.Size;
        seats.CustomMinimumSize = geometry.SeatStrip.Size;
        seats.ZIndex = 24;
        surface.AddChild(seats);
    }

    private static BoardAreaPresentation[] Lane(BoardPresentation board, string key) => [.. board.Lanes
        .Where(lane => lane.Key == key)
        .SelectMany(lane => lane.Areas)];

}
