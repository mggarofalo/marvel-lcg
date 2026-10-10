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
        AstraTableGeometry geometry = Geometry(main, selection, prompt);
        Control surface = CreateSurface(main, geometry);
        var result = new BoardRenderResult(main.boardPresentation);
        AddMats(surface, result, geometry, selection);
        RenderObjects(main, result, surface, geometry, selection, prompt);
        AddSeats(surface, geometry, main.boardPresentation!, selection, switchSeat);
        surface.SetMeta("spatial_table_grammar", "astra-far-to-near-v1");
        surface.SetMeta("expanded_seat", selection.ExpandedSeat);
        return result;
    }

    private static void Prepare(Main main)
    {
        TabletopPileInspector.Close();
        BoardActionChoiceSurface.Close();
        BoardRenderCleanup.Clear(main.boardAreas);
        BoardRenderCleanup.Clear(main.handRail);
        main.GetNode<PanelContainer>("Margin/Shell/Content/Play/Board/HandShelf").Visible = false;
    }

    private static AstraTableGeometry Geometry(Main main, DisplayedSeatSelection selection, Prompt? prompt)
    {
        Vector2 viewport = main.GetViewportRect().Size;
        bool expanded = TableHistoryDrawer.IsExpanded(main);
        float reserved = CardPaymentWorkspaceLayout.Active(main)
            ? Math.Max(CardPaymentWorkspaceLayout.Width(viewport),
                expanded ? TableHistoryDrawer.ExpandedWidth(main) : 0) + 40
            : expanded ? TableHistoryDrawer.ExpandedWidth(main) + 40 : 250;
        float width = Math.Max(expanded ? 920 : 1060, viewport.X - reserved);
        float height = Math.Min(AstraTableGeometry.ReferenceHeight, Math.Max(820, viewport.Y - 118));
        return new AstraTableGeometry(
            width, height, main.interfaceScale >= InterfaceScale.Percent130,
            HasRevealingCard: main.boardPresentation!.Areas.Any(area =>
                SpatialTableZones.IsResolving(area) && SpatialTableZones.Current(area).Length > 0)
                || Context(main, prompt).Count > 0,
            PhysicalCardSize: PhysicalCardSize(main.boardPresentation!, main.interfaceScale, height, Context(main, prompt)),
            HasSeatSummaries: main.boardPresentation.PlayerSummaries.Count > 1,
            HasSourceTableau: CardSourceGroups.Controlled(main.boardPresentation.Areas, selection.ExpandedSeat).Length > 0)
        {
            HasAllies = main.boardPresentation.Areas.Any(area => area.Zone == "AlliesArea"
                && area.Seat == selection.ExpandedSeat && area.Cards.Count > 0),
        };
    }

    private static IReadOnlyList<BoardCardPresentation> Context(Main main, Prompt? prompt) =>
        DecisionContextCards.MissingFromTable(prompt is null || main.CurrentGame?.World is null ? null
            : PromptPresentation.From(prompt, main.CurrentGame.World), main.boardPresentation!);

    private static Vector2 PhysicalCardSize(BoardPresentation board, InterfaceScale scale, float height, IReadOnlyList<BoardCardPresentation> context)
    {
        IEnumerable<BoardCardPresentation> cards = board.Areas
            .Where(area => area.Zone != "HandsArea")
            .SelectMany(SpatialTableZones.Current);
        InterfaceScale cardScale = SpatialCardMetrics.TableScale(scale, height);
        Vector2 installed = SpatialCardMetrics.Envelope(cards, CardDisplaySize.Board, cardScale);
        Vector2 revealing = SpatialCardMetrics.Envelope(board.Areas
            .Where(SpatialTableZones.IsResolving).SelectMany(SpatialTableZones.Current).Concat(context),
            CardDisplaySize.Hand, cardScale);
        return context.Count > 0 || board.Areas.Any(area => SpatialTableZones.IsResolving(area) && SpatialTableZones.Current(area).Length > 0)
            ? new Vector2(Math.Max(installed.X, revealing.X), Math.Max(installed.Y, revealing.Y)) : installed;
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
            GodotThemeVariations.SpatialVillainMat, "Villain's play area");
        PanelContainer playerMat = AddMat(surface, "PlayerTable", geometry.PlayerMat,
            GodotThemeVariations.SpatialPlayerMat,
            $"Player {selection.ExpandedSeat + 1}");
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
        InterfaceScale cardScale = SpatialCardMetrics.TableScale(main.interfaceScale, geometry.Height);
        var objects = new SpatialTableObjectRenderer(
            surface, result, geometry, cardScale, main.art, MulliganPrompt.IsOpening(prompt));
        objects.RenderScenario(scenario);
        objects.RenderRevealing(board.Areas, Context(main, prompt));
        objects.RenderPlayer(player);
        PendingEncounterIndicator.Add(surface, geometry.PendingEncounters,
            board.PendingEncounterCount(selection.ExpandedSeat), selection.ExpandedSeat);
        objects.RenderHosted([.. scenario.Concat(player)]);
        int handSeat = prompt?.Player ?? selection.ExpandedSeat;
        objects.RenderHand(board.Areas.FirstOrDefault(area =>
            area.Zone == "HandsArea" && area.Seat == handSeat));
        objects.RenderOverflow([.. scenario.Concat(player).Concat(Lane(board, "other"))]);

        AddHandCaption(surface, geometry, board, handSeat, prompt);
        SpatialTableContextRenderer.Add(surface, result, geometry, main.CurrentGame?.World, prompt);
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
            Text = $"Engaged with player {expandedSeat + 1}",
            Position = geometry.EngagedEnemies.Position - new Vector2(0, 24),
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
        string heading = MulliganPrompt.IsOpening(prompt)
            ? $"Player {seat + 1} opening hand"
            : $"Player {seat + 1} hand";
        float left = geometry.PlayerMat.Position.X + 22;
        float top = geometry.PlayerDiscard.End.Y + 12;
        surface.AddChild(new Label
        {
            Name = "SpatialHandHeading",
            Text = $"{heading} · {visible} cards"
                + (concealed > 0 ? $" · {concealed} concealed" : string.Empty),
            Position = new Vector2(left, top),
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
