using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders one area as a compact card and a stable bounded pager.</summary>
internal static class BoardAreaRenderer
{
    internal static PanelContainer Render(
        BoardAreaPresentation area, BoardRenderContext context)
    {
        int cardCount = area.Cards.Sum(card => card.Count)
            + area.Removed.Sum(card => card.Count);
        bool live = area.Prominence == BoardAreaProminence.Live;
        bool expanded = context.ExpandedAreas.TryGetValue(area.Id, out bool remembered)
            ? remembered
            : live;
        int width = VisualSystem.DesktopPlay(1920, 1080, context.Scale).BoardAreaWidth;
        var panel = new PanelContainer
        {
            Name = $"Area{area.Id}",
            CustomMinimumSize = new Vector2(width, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
            TooltipText = $"{area.Title}. {area.Context}",
            ThemeTypeVariation = GodotThemeVariations.BoardArea,
        };
        var content = new VBoxContainer
        {
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        var body = new VBoxContainer
        {
            Name = "Body",
            Visible = live || expanded,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        panel.AddChild(content);
        if (live)
        {
            Label heading = Text($"{area.Title} · {cardCount}", GodotThemeVariations.Caption);
            heading.Name = "Heading";
            content.AddChild(heading);
        }
        else
        {
            AddDisclosure(content, body, area, cardCount, expanded, context);
        }
        content.AddChild(body);
        if (area.Depth > 0)
        {
            body.AddChild(Text(
                $"↳ HOSTED BY {area.HostedBy.ToUpperInvariant()}",
                GodotThemeVariations.StatusText));
        }

        BoardCardPresentation[] cards =
            [.. area.Cards.Where(card => card.StageRole != BoardStageRole.Upcoming)];
        AddCollection(body, cards, area.Id, context);
        AddUpcoming(body, area, context);
        if (area.Removed.Count > 0)
        {
            body.AddChild(Text(
                $"REMOVED · {area.Removed.Sum(card => card.Count)}",
                GodotThemeVariations.Caption));
            AddCollection(body, area.Removed, RemovedKey(area.Id), context);
        }
        return panel;
    }

    private static void AddDisclosure(
        VBoxContainer content,
        VBoxContainer body,
        BoardAreaPresentation area,
        int cardCount,
        bool expanded,
        BoardRenderContext context)
    {
        var disclosure = new Button
        {
            Name = $"Area{area.Id}Disclosure",
            Text = AreaHeading(area, cardCount, expanded),
            Alignment = HorizontalAlignment.Left,
            ToggleMode = true,
            ButtonPressed = expanded,
            TooltipText = $"Show or hide {area.Title.ToLowerInvariant()}.",
        };
        void SetExpanded(bool value)
        {
            context.ExpandedAreas[area.Id] = value;
            body.Visible = value;
            disclosure.Text = AreaHeading(area, cardCount, value);
        }
        disclosure.Pressed += () => SetExpanded(disclosure.ButtonPressed);
        context.Result.RegisterArea(body, () =>
        {
            disclosure.SetPressedNoSignal(true);
            SetExpanded(true);
        });
        content.AddChild(disclosure);
    }

    private static void AddUpcoming(
        VBoxContainer destination,
        BoardAreaPresentation area,
        BoardRenderContext context)
    {
        BoardCardPresentation[] upcoming =
            [.. area.Cards.Where(card => card.StageRole == BoardStageRole.Upcoming)];
        if (upcoming.Length == 0)
        {
            return;
        }
        int stateKey = UpcomingKey(area.Id);
        bool expanded = context.ExpandedAreas.TryGetValue(stateKey, out bool remembered)
            && remembered;
        int count = upcoming.Sum(card => card.Count);
        var section = new VBoxContainer
        {
            Name = "UpcomingStages",
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        var disclosure = new Button
        {
            Name = "UpcomingStagesDisclosure",
            Text = $"{(expanded ? "▾" : "▸")} Upcoming stages · {count}",
            Alignment = HorizontalAlignment.Left,
            ToggleMode = true,
            ButtonPressed = expanded,
        };
        var body = new VBoxContainer
        {
            Name = "UpcomingStagesList",
            Visible = expanded,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        disclosure.Pressed += () =>
        {
            context.ExpandedAreas[stateKey] = disclosure.ButtonPressed;
            body.Visible = disclosure.ButtonPressed;
            disclosure.Text = $"{(disclosure.ButtonPressed ? "▾" : "▸")} Upcoming stages · {count}";
        };
        section.AddChild(disclosure);
        section.AddChild(body);
        destination.AddChild(section);
        AddCollection(body, upcoming, stateKey, context);
    }

    private static void AddCollection(
        VBoxContainer destination,
        IReadOnlyList<BoardCardPresentation> cards,
        int key,
        BoardRenderContext context)
    {
        if (cards.Count == 0)
        {
            return;
        }

        int pages = cards.Count;
        int page = context.Pages.Page(key, pages);
        if (pages > 1)
        {
            destination.AddChild(Pager(cards, key, page, context));
        }
        destination.AddChild(Card(cards[page], context));
    }

    private static HBoxContainer Pager(
        IReadOnlyList<BoardCardPresentation> cards,
        int key,
        int page,
        BoardRenderContext context)
    {
        int actionCount = cards.Sum(card => context.Interaction.ActionsFor(card.TargetId).Count);
        int selectedCount = cards.Count(card => context.Interaction.IsSelected(card.TargetId));
        string state = actionCount > 0 ? $" · actions {actionCount}" : string.Empty;
        state += selectedCount > 0 ? $" · selected {selectedCount}" : string.Empty;
        var row = new HBoxContainer
        {
            Name = "CollectionPager",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        var previous = PageButton(key, "Previous", "‹", page > 0, context.Scale);
        var label = Text($"{page + 1}/{cards.Count}{state}", GodotThemeVariations.Caption);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        var next = PageButton(key, "Next", "›", page + 1 < cards.Count, context.Scale);
        previous.Pressed += () =>
        {
            context.Pages.SetPage(key, page - 1, cards.Count);
            context.Result.RequestRefresh(previous.Name);
        };
        next.Pressed += () =>
        {
            context.Pages.SetPage(key, page + 1, cards.Count);
            context.Result.RequestRefresh(next.Name);
        };
        row.AddChild(previous);
        row.AddChild(label);
        row.AddChild(next);
        return row;
    }

    private static Control Card(
        BoardCardPresentation card, BoardRenderContext context)
    {
        CardLayoutMetrics geometry = VisualSystem.Card(CardDisplaySize.Board, context.Scale);
        var wrapper = new Control
        {
            Name = card.TargetId is { } id ? $"CardSlot{id}" : "ConcealedCardSlot",
            CustomMinimumSize = new Vector2(geometry.Width, geometry.MinimumHeight),
        };
        CardControl control = CardControl.Create(
            card, CardDisplaySize.Board, context.Scale, context.Art);
        control.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        wrapper.AddChild(control);
        if (card.TargetId is { } target)
        {
            context.Result.Register(target, control);
        }
        context.Result.TrackCard(control, card);
        BoardLocalInteractionRenderer.Add(wrapper, card, context, geometry);
        return wrapper;
    }

    private static Button PageButton(
        int key, string name, string text, bool enabled, InterfaceScale scale) => new()
    {
        Name = $"Collection{key}{name}",
        Text = text,
        Disabled = !enabled,
        TooltipText = $"{name} card in this collection",
        CustomMinimumSize = new Vector2(
            VisualSystem.Controls(scale).MinimumPointerTarget,
            VisualSystem.Controls(scale).MinimumHeight),
    };

    private static string AreaHeading(
        BoardAreaPresentation area, int count, bool expanded) =>
        $"{(expanded ? "▾" : "▸")} {area.Title} · {count}";

    internal static int UpcomingKey(int areaId) => checked(-1_000_000 - areaId);

    internal static int RemovedKey(int areaId) => checked(-2_000_000 - areaId);

    private static Label Text(string text, string variation) => new()
    {
        Text = text,
        ThemeTypeVariation = variation,
    };
}
