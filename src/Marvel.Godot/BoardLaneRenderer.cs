using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders one fixed-height scenario or player lane.</summary>
internal static class BoardLaneRenderer
{
    internal static VBoxContainer Render(
        BoardLanePresentation lane, BoardRenderContext context)
    {
        var section = new VBoxContainer
        {
            Name = lane.Key == "scenario"
                ? "ScenarioLane"
                : lane.Seat is { } seat ? $"PlayerLane{seat}" : "OtherLane",
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        section.AddChild(Text(lane.Title, GodotThemeVariations.Heading));

        var areas = new HFlowContainer
        {
            Name = "LiveAreaFlow",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        foreach (BoardAreaPresentation area in lane.Areas.Where(area =>
                     area.Zone != "HandsArea" && area.Prominence == BoardAreaProminence.Live))
        {
            areas.AddChild(BoardAreaRenderer.Render(area, context));
        }
        if (areas.GetChildCount() > 0)
        {
            section.AddChild(areas);
        }

        BoardAreaPresentation[] secondary = [.. lane.Areas.Where(area =>
            area.Zone != "HandsArea" && area.Prominence != BoardAreaProminence.Live)];
        if (secondary.Length > 0)
        {
            section.AddChild(SecondaryAreas(lane, secondary, context));
        }
        return section;
    }

    private static VBoxContainer SecondaryAreas(
        BoardLanePresentation lane,
        BoardAreaPresentation[] areas,
        BoardRenderContext context)
    {
        int stateKey = lane.Key switch
        {
            "scenario" => -10_001,
            "other" => -10_002,
            _ when lane.Seat is { } seat => -11_000 - seat,
            _ => -10_003,
        };
        bool expanded = context.ExpandedAreas.TryGetValue(stateKey, out bool remembered)
            && remembered;
        int occupied = areas.Count(area => area.Prominence == BoardAreaProminence.Supporting);
        int empty = areas.Length - occupied;
        string Summary(bool open) => $"{(open ? "▾" : "▸")} More areas"
            + (occupied > 0 ? $" · {occupied} with cards" : string.Empty)
            + (empty > 0 ? $" · {empty} empty" : string.Empty);

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
            TooltipText = "Show secondary, empty, hosted, and unknown areas.",
        };
        var menu = new PopupPanel
        {
            Name = "SecondaryAreaMenu",
            Title = $"{lane.Title} · More areas",
        };
        var browser = new ScrollContainer
        {
            Name = "SecondaryAreaBrowser",
            CustomMinimumSize = new Vector2(1600, 600),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
        };
        var body = new HFlowContainer
        {
            Name = "SecondaryAreaFlow",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        void SetExpanded(bool value)
        {
            context.ExpandedAreas[stateKey] = value;
            if (value)
            {
                menu.PopupCenteredClamped(new Vector2I(1600, 600), 0.8f);
            }
            else
            {
                menu.Hide();
            }
            disclosure.Text = Summary(value);
        }
        disclosure.Pressed += () => SetExpanded(disclosure.ButtonPressed);
        menu.PopupHide += () =>
        {
            disclosure.SetPressedNoSignal(false);
            context.ExpandedAreas[stateKey] = false;
            disclosure.Text = Summary(false);
        };
        context.Result.RegisterArea(body, () =>
        {
            disclosure.SetPressedNoSignal(true);
            SetExpanded(true);
        });
        section.AddChild(disclosure);
        section.AddChild(menu);
        menu.AddChild(browser);
        browser.AddChild(body);
        foreach (BoardAreaPresentation area in areas
                     .OrderByDescending(area => area.Prominence)
                     .ThenBy(area => area.Title, StringComparer.Ordinal))
        {
            body.AddChild(BoardAreaRenderer.Render(area, context));
        }
        if (expanded)
        {
            Callable.From(() => SetExpanded(true)).CallDeferred();
        }
        return section;
    }

    private static Label Text(string text, string variation) => new()
    {
        Text = text,
        ThemeTypeVariation = variation,
    };
}
