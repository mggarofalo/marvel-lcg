using Godot;

namespace Marvel.Godot;

/// <summary>Builds Godot controls from a visibility-safe board presentation.</summary>
public static class BoardRenderer
{
    /// <summary>Replaces the visible board with one authoritative snapshot.</summary>
    public static BoardRenderResult Render(VBoxContainer destination, BoardPresentation board)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(board);
        foreach (Node child in destination.GetChildren())
        {
            destination.RemoveChild(child);
            child.QueueFree();
        }

        var result = new BoardRenderResult();
        IReadOnlyList<BoardLanePresentation> lanes = board.Lanes.Count > 0
            ? board.Lanes
            : BoardLayout.Arrange(board.Areas, []);
        foreach (BoardLanePresentation lane in lanes)
        {
            destination.AddChild(Lane(lane, result));
        }

        return result;
    }

    private static VBoxContainer Lane(
        BoardLanePresentation lane,
        BoardRenderResult result)
    {
        var section = new VBoxContainer
        {
            Name = lane.Key == "scenario"
                ? "ScenarioLane"
                : lane.Seat is { } seat ? $"PlayerLane{seat}" : "OtherLane",
            ThemeTypeVariation = GodotThemeVariations.Stack,
        };
        section.AddChild(Label(lane.Title, GodotThemeVariations.Heading));

        var scroll = new ScrollContainer
        {
            Name = "AreaScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var areas = new HBoxContainer
        {
            Name = "AreaRail",
            ThemeTypeVariation = GodotThemeVariations.WideRow,
        };
        scroll.AddChild(areas);
        foreach (BoardAreaPresentation area in lane.Areas)
        {
            areas.AddChild(Area(area, result));
        }

        section.AddChild(scroll);
        return section;
    }

    private static PanelContainer Area(
        BoardAreaPresentation area,
        BoardRenderResult result)
    {
        var panel = new PanelContainer
        {
            Name = $"Area{area.Id}",
            CustomMinimumSize = new Vector2(380, 120),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            TooltipText = $"{area.Title}. {area.Context}",
            ThemeTypeVariation = GodotThemeVariations.BoardArea,
        };

        var content = new VBoxContainer
        {
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        panel.AddChild(content);
        content.AddChild(Label(area.Title, GodotThemeVariations.Heading));
        content.AddChild(Label(area.Context, GodotThemeVariations.Eyebrow));
        if (area.Depth > 0)
        {
            content.AddChild(Label(
                $"↳ HOSTED BY {area.HostedBy.ToUpperInvariant()}",
                GodotThemeVariations.StatusText,
                wrap: true));
        }
        content.AddChild(new HSeparator());
        AddCards(content, area.Cards, "CARDS", result);
        if (area.Removed.Count > 0)
        {
            content.AddChild(new HSeparator());
            AddCards(content, area.Removed, "REMOVED", result);
        }

        return panel;
    }

    private static void AddCards(
        VBoxContainer destination,
        IReadOnlyList<BoardCardPresentation> cards,
        string section,
        BoardRenderResult result)
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
                card, CardDisplaySize.Board, ClientTheme.ConfiguredScale());
            control.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            rail.AddChild(control);
            if (card.TargetId is { } target)
            {
                result.Register(target, control);
            }
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

    internal void Register(int id, CardControl control)
    {
        if (!controls.TryGetValue(id, out List<CardControl>? matches))
        {
            matches = [];
            controls.Add(id, matches);
        }

        matches.Add(control);
    }

    /// <summary>Highlights every visible control matching one server-provided id.</summary>
    public void Highlight(int? id)
    {
        foreach ((int key, List<CardControl> matches) in controls)
        {
            foreach (CardControl match in matches)
            {
                match.SetHighlighted(id == key);
                if (id == key)
                {
                    EnsureVisible(match);
                }
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
                scroll.CallDeferred(ScrollContainer.MethodName.EnsureControlVisible, control);
            }

            ancestor = ancestor.GetParent();
        }
    }
}
