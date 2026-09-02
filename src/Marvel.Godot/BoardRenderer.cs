using Godot;

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
            Name = "AreaFlow",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        foreach (BoardAreaPresentation area in lane.Areas)
        {
            areas.AddChild(Area(area, result, scale, expandedAreas, art));
        }

        section.AddChild(areas);
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
            : cardCount > 0;
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
        disclosure.Pressed += () =>
        {
            expandedAreas[area.Id] = disclosure.ButtonPressed;
            body.Visible = disclosure.ButtonPressed;
            disclosure.Text = $"{(disclosure.ButtonPressed ? "▾" : "▸")}  {area.Title}  ·  {cardCount}";
        };
        body.AddChild(Label(area.Context, GodotThemeVariations.Caption));
        if (area.Depth > 0)
        {
            body.AddChild(Label(
                $"↳ HOSTED BY {area.HostedBy.ToUpperInvariant()}",
                GodotThemeVariations.StatusText,
                wrap: true));
        }
        AddCards(body, area.Cards, "CARDS", result, scale, art);
        if (area.Removed.Count > 0)
        {
            body.AddChild(new HSeparator());
            AddCards(body, area.Removed, "REMOVED", result, scale, art);
        }

        return panel;
    }

    private static void AddCards(
        VBoxContainer destination,
        IReadOnlyList<BoardCardPresentation> cards,
        string section,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art)
    {
        destination.AddChild(Label(
            $"{section}  ·  {cards.Sum(card => card.Count)}",
            GodotThemeVariations.Caption));
        if (cards.Count == 0)
        {
            destination.AddChild(Label("Empty", GodotThemeVariations.MutedText));
            return;
        }

        var scroll = new ScrollContainer
        {
            Name = $"{section}Scroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var rail = new HBoxContainer
        {
            Name = $"{section}Rail",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        scroll.AddChild(rail);
        destination.AddChild(scroll);

        foreach (BoardCardPresentation card in cards)
        {
            CardControl control = CardControl.Create(
                card, CardDisplaySize.Board, scale, art);
            control.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            rail.AddChild(control);
            if (card.TargetId is { } target)
            {
                result.Register(target, control);
            }
            result.TrackHover(control, card);
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
            result.TrackHover(control, card);
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

/// <summary>The controls addressable by prompt anchor and target ids.</summary>
public sealed class BoardRenderResult
{
    private readonly Dictionary<int, List<CardControl>> controls = [];

    /// <summary>Raised with the card under the pointer, or null when it leaves.</summary>
    public event Action<BoardCardPresentation?>? CardHovered;

    internal void Register(int id, CardControl control)
    {
        if (!controls.TryGetValue(id, out List<CardControl>? matches))
        {
            matches = [];
            controls.Add(id, matches);
        }

        matches.Add(control);
    }

    internal void TrackHover(Control control, BoardCardPresentation card)
    {
        control.MouseEntered += () => CardHovered?.Invoke(card);
        control.MouseExited += () => CardHovered?.Invoke(null);
    }

    /// <summary>Highlights every visible control matching server-provided ids.</summary>
    public void Highlight(IEnumerable<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        HashSet<int> highlighted = ids.ToHashSet();
        foreach ((int key, List<CardControl> matches) in controls)
        {
            foreach (CardControl match in matches)
            {
                match.SetHighlighted(highlighted.Contains(key));
                if (highlighted.Contains(key))
                {
                    EnsureVisible(match);
                }
            }
        }
    }

    /// <summary>Marks cards for a transient event cue without disturbing prompt focus.</summary>
    public void Present(IEnumerable<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        HashSet<int> presented = ids.ToHashSet();
        foreach ((int key, List<CardControl> matches) in controls)
        {
            foreach (CardControl match in matches)
            {
                match.SetPresented(presented.Contains(key));
            }
        }
    }

    private static void EnsureVisible(Control control)
    {
        Node? ancestor = control.GetParent();
        while (ancestor is not null)
        {
            if (ancestor is ScrollContainer scroll)
            {
                bool table = scroll.Name == "TableScroll";
                Control target = table
                    ? control.GetNodeOrNull<Control>("CardFace/Title") ?? control
                    : AreaContaining(scroll, control) ?? control;
                if (table)
                {
                    Callable.From(() =>
                    {
                        scroll.EnsureControlVisible(target);
                        Callable.From(() => AlignBoardToTitle(scroll, target, 3)).CallDeferred();
                    }).CallDeferred();
                }
                else
                {
                    scroll.CallDeferred(ScrollContainer.MethodName.EnsureControlVisible, target);
                }
                // The board owns card navigation. Continuing into the outer
                // page would hide the table heading whenever prompt focus
                // highlights a card below the fold.
                if (table)
                {
                    break;
                }
            }

            ancestor = ancestor.GetParent();
        }
    }

    private static void AlignBoardToTitle(
        ScrollContainer board,
        Control title,
        int remainingPasses)
    {
        Rect2 viewport = board.GetGlobalRect();
        Rect2 titleRect = title.GetGlobalRect();
        board.ScrollVertical += Mathf.RoundToInt(titleRect.Position.Y - viewport.Position.Y);
        if (remainingPasses > 0)
        {
            Callable.From(() => AlignBoardToTitle(board, title, remainingPasses - 1)).CallDeferred();
        }
    }

    private static Control? AreaContaining(ScrollContainer scroll, Control control)
    {
        Node? candidate = control;
        Control? area = null;
        while (candidate is not null && candidate != scroll)
        {
            if (candidate is PanelContainer panel
                && panel.Name.ToString().StartsWith("Area", StringComparison.Ordinal))
            {
                area = panel;
            }

            candidate = candidate.GetParent();
        }

        return area;
    }
}
