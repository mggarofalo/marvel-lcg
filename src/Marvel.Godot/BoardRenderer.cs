using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Builds Godot controls from a visibility-safe board presentation.</summary>
public static class BoardRenderer
{
    /// <summary>Replaces the visible board with one authoritative snapshot.</summary>
    public static BoardRenderResult Render(
        VBoxContainer destination,
        BoardPresentation board,
        HBoxContainer hand,
        Label handHeading,
        InterfaceScale scale,
        IDictionary<int, bool> expandedAreas,
        ICardArtProvider? art = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(hand);
        ArgumentNullException.ThrowIfNull(handHeading);
        ArgumentNullException.ThrowIfNull(expandedAreas);
        foreach (Node child in destination.GetChildren())
        {
            destination.RemoveChild(child);
            child.QueueFree();
        }
        foreach (Node child in hand.GetChildren())
        {
            hand.RemoveChild(child);
            child.QueueFree();
        }

        var result = new BoardRenderResult();
        IReadOnlyList<BoardLanePresentation> lanes = board.Lanes.Count > 0
            ? board.Lanes
            : BoardLayout.Arrange(board.Areas, []);
        foreach (BoardLanePresentation lane in lanes)
        {
            BoardAreaPresentation[] visibleAreas =
                [.. lane.Areas.Where(area => area.Zone != "HandsArea")];
            if (visibleAreas.Length > 0)
            {
                destination.AddChild(Lane(
                    lane with { Areas = visibleAreas }, result, scale, expandedAreas, art));
            }
        }

        RenderHand(board, hand, handHeading, result, scale, art);

        return result;
    }

    private static VBoxContainer Lane(
        BoardLanePresentation lane,
        BoardRenderResult result,
        InterfaceScale scale,
        IDictionary<int, bool> expandedAreas,
        ICardArtProvider? art)
    {
        var section = new VBoxContainer
        {
            Name = lane.Key == "scenario"
                ? "ScenarioLane"
                : lane.Seat is { } seat ? $"PlayerLane{seat}" : "OtherLane",
            ThemeTypeVariation = GodotThemeVariations.Stack,
        };
        section.AddChild(Label(lane.Title, GodotThemeVariations.Heading));

        var areas = new HFlowContainer
        {
            Name = "LiveAreaFlow",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        foreach (BoardAreaPresentation area in lane.Areas.Where(area =>
                     area.Prominence == BoardAreaProminence.Live))
        {
            areas.AddChild(Area(area, result, scale, expandedAreas, art));
        }

        if (areas.GetChildCount() > 0)
        {
            section.AddChild(areas);
        }

        BoardAreaPresentation[] secondary = [.. lane.Areas.Where(area =>
            area.Prominence != BoardAreaProminence.Live)];
        if (secondary.Length > 0)
        {
            section.AddChild(SecondaryAreas(
                lane, secondary, result, scale, expandedAreas, art));
        }
        return section;
    }

    private static VBoxContainer SecondaryAreas(
        BoardLanePresentation lane,
        BoardAreaPresentation[] areas,
        BoardRenderResult result,
        InterfaceScale scale,
        IDictionary<int, bool> expandedAreas,
        ICardArtProvider? art)
    {
        int stateKey = lane.Key switch
        {
            "scenario" => -10_001,
            "other" => -10_002,
            _ when lane.Seat is { } seat => -11_000 - seat,
            _ => -10_003,
        };
        bool expanded = expandedAreas.TryGetValue(stateKey, out bool remembered) && remembered;
        int occupied = areas.Count(area => area.Prominence == BoardAreaProminence.Supporting);
        int empty = areas.Length - occupied;
        string Summary(bool open) => $"{(open ? "▾" : "▸")}  More areas"
            + (occupied > 0 ? $"  ·  {occupied} with cards" : string.Empty)
            + (empty > 0 ? $"  ·  {empty} empty" : string.Empty);

        var section = new VBoxContainer
        {
            Name = "SecondaryAreas",
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        var disclosure = new Button
        {
            Name = "SecondaryAreasDisclosure",
            Text = Summary(expanded),
            Alignment = HorizontalAlignment.Left,
            ToggleMode = true,
            ButtonPressed = expanded,
            TooltipText = "Show draw piles, discard piles, and other secondary areas.",
        };
        section.AddChild(disclosure);
        var body = new HFlowContainer
        {
            Name = "SecondaryAreaFlow",
            Visible = expanded,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        section.AddChild(body);
        void SetExpanded(bool value)
        {
            expandedAreas[stateKey] = value;
            body.Visible = value;
            disclosure.Text = Summary(value);
        }
        disclosure.Pressed += () => SetExpanded(disclosure.ButtonPressed);
        result.RegisterArea(body, () =>
        {
            disclosure.SetPressedNoSignal(true);
            SetExpanded(true);
        });
        foreach (BoardAreaPresentation area in areas
                     .Where(area => area.Cards.Count > 0 || area.Removed.Count > 0)
                     .OrderByDescending(area => area.Prominence)
                     .ThenBy(area => area.Title, StringComparer.Ordinal))
        {
            body.AddChild(Area(area, result, scale, expandedAreas, art));
        }

        return section;
    }

    private static PanelContainer Area(
        BoardAreaPresentation area,
        BoardRenderResult result,
        InterfaceScale scale,
        IDictionary<int, bool> expandedAreas,
        ICardArtProvider? art)
    {
        DesktopPlayMetrics layout = VisualSystem.DesktopPlay(
            Math.Max(1, DisplayServer.ScreenGetSize().X),
            Math.Max(1, DisplayServer.ScreenGetSize().Y),
            scale);
        int cardCount = area.Cards.Sum(card => card.Count)
            + area.Removed.Sum(card => card.Count);
        bool expanded = expandedAreas.TryGetValue(area.Id, out bool remembered)
            ? remembered
            : area.Prominence == BoardAreaProminence.Live;
        var panel = new PanelContainer
        {
            Name = $"Area{area.Id}",
            CustomMinimumSize = new Vector2(layout.BoardAreaWidth, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
            TooltipText = $"{area.Title}. {area.Context}",
            ThemeTypeVariation = GodotThemeVariations.BoardArea,
        };

        var content = new VBoxContainer
        {
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        panel.AddChild(content);
        var disclosure = new Button
        {
            Name = $"Area{area.Id}Disclosure",
            Text = $"{(expanded ? "▾" : "▸")}  {area.Title}  ·  {cardCount}",
            Alignment = HorizontalAlignment.Left,
            ToggleMode = true,
            ButtonPressed = expanded,
            TooltipText = $"Show or hide {area.Title.ToLowerInvariant()}.",
        };
        content.AddChild(disclosure);
        var body = new VBoxContainer
        {
            Name = "Body",
            Visible = expanded,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        content.AddChild(body);
        void SetExpanded(bool value)
        {
            expandedAreas[area.Id] = value;
            body.Visible = value;
            disclosure.Text = $"{(value ? "▾" : "▸")}  {area.Title}  ·  {cardCount}";
        }
        disclosure.Pressed += () => SetExpanded(disclosure.ButtonPressed);
        result.RegisterArea(body, () =>
        {
            disclosure.SetPressedNoSignal(true);
            SetExpanded(true);
        });
        if (area.Depth > 0)
        {
            body.AddChild(Label(
                $"↳ HOSTED BY {area.HostedBy.ToUpperInvariant()}",
                GodotThemeVariations.StatusText,
                wrap: true));
        }
        BoardCardPresentation[] primaryCards =
            [.. area.Cards.Where(card => card.StageRole != BoardStageRole.Upcoming)];
        BoardCardPresentation[] upcomingStages =
            [.. area.Cards.Where(card => card.StageRole == BoardStageRole.Upcoming)];
        AddCards(body, primaryCards, "CARDS", area.Zone, result, scale, art);
        AddUpcomingStages(
            body, upcomingStages, area.Id, result, scale, expandedAreas, art);
        if (area.Removed.Count > 0)
        {
            body.AddChild(new HSeparator());
            AddCards(body, area.Removed, "REMOVED", area.Zone, result, scale, art);
        }

        return panel;
    }

    private static void AddUpcomingStages(
        VBoxContainer destination,
        BoardCardPresentation[] cards,
        int areaId,
        BoardRenderResult result,
        InterfaceScale scale,
        IDictionary<int, bool> expandedAreas,
        ICardArtProvider? art)
    {
        if (cards.Length == 0)
        {
            return;
        }

        int count = cards.Sum(card => card.Count);
        int stateKey = UpcomingStagesStateKey(areaId);
        bool expanded = expandedAreas.TryGetValue(stateKey, out bool remembered)
            && remembered;
        var section = new VBoxContainer
        {
            Name = "UpcomingStages",
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        var disclosure = new Button
        {
            Name = "UpcomingStagesDisclosure",
            Text = $"{(expanded ? "▾" : "▸")}  Upcoming stages  ·  {count}",
            Alignment = HorizontalAlignment.Left,
            ToggleMode = true,
            ButtonPressed = expanded,
            TooltipText = "Show or hide the stages that follow the current stage.",
        };
        var list = new VBoxContainer
        {
            Name = "UpcomingStagesList",
            Visible = expanded,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        disclosure.Pressed += () =>
        {
            expandedAreas[stateKey] = disclosure.ButtonPressed;
            list.Visible = disclosure.ButtonPressed;
            disclosure.Text = $"{(disclosure.ButtonPressed ? "▾" : "▸")}  Upcoming stages  ·  {count}";
        };
        section.AddChild(disclosure);
        section.AddChild(list);
        destination.AddChild(section);

        foreach (BoardCardPresentation card in cards)
        {
            CardControl control = CardControl.Create(card, CardDisplaySize.Board, scale, art);
            control.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            list.AddChild(control);
            if (card.TargetId is { } target)
            {
                result.Register(target, control);
            }
            result.TrackCard(control, card);
        }
    }

    internal static int UpcomingStagesStateKey(int areaId) =>
        checked(-1_000_000 - areaId);

    private static void AddCards(
        VBoxContainer destination,
        IReadOnlyList<BoardCardPresentation> cards,
        string section,
        string zone,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art)
    {
        if (cards.Count == 0)
        {
            return;
        }
        if (section != "CARDS")
        {
            destination.AddChild(Label(
                $"{section}  ·  {cards.Sum(card => card.Count)}",
                GodotThemeVariations.Caption));
        }

        bool list = zone is "DiscardPile" or "EncounterDiscardPile";
        var scroll = new ScrollContainer
        {
            Name = $"{section}Scroll",
            HorizontalScrollMode = list
                ? ScrollContainer.ScrollMode.Disabled
                : ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = list
                ? ScrollContainer.ScrollMode.Auto
                : ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        BoxContainer rail = list ? new VBoxContainer() : new HBoxContainer();
        rail.Name = $"{section}Rail";
        rail.ThemeTypeVariation = GodotThemeVariations.CompactRow;
        scroll.AddChild(rail);
        destination.AddChild(scroll);

        foreach (BoardCardPresentation card in cards)
        {
            CardControl control = CardControl.Create(
                card, list ? CardDisplaySize.Hand : CardDisplaySize.Board, scale, art);
            control.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            rail.AddChild(control);
            if (card.TargetId is { } target)
            {
                result.Register(target, control);
            }
            result.TrackCard(control, card);
        }
    }

    private static void RenderHand(
        BoardPresentation board,
        HBoxContainer destination,
        Label heading,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art)
    {
        BoardAreaPresentation? handArea = board.Areas
            .Where(area => area.Zone == "HandsArea")
            .FirstOrDefault(area => area.Cards.Any(card => !card.Concealed));
        IReadOnlyList<BoardCardPresentation> cards = handArea?.Cards ?? [];
        heading.Text = $"HAND  ·  {cards.Sum(card => card.Count)}";
        if (cards.Count == 0)
        {
            destination.AddChild(Label("No visible cards in hand.", GodotThemeVariations.MutedText));
            return;
        }

        foreach (BoardCardPresentation card in cards)
        {
            CardControl control = CardControl.Create(
                card, CardDisplaySize.Hand, scale, art);
            destination.AddChild(control);
            if (card.TargetId is { } target)
            {
                result.Register(target, control);
            }
            result.TrackCard(control, card);
        }
    }

    private static Label Label(string text, string variation, bool wrap = false)
    {
        return new Label
        {
            Text = text,
            AutowrapMode = wrap
                ? TextServer.AutowrapMode.WordSmart
                : TextServer.AutowrapMode.Off,
            ThemeTypeVariation = variation,
        };
    }
}
