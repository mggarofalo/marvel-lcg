using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders the selected seat's authorized hand through stable bounded pages.</summary>
internal static class BoardHandRenderer
{
    private const int CardsPerPage = 6;

    internal static void Render(
        BoardSeatPresentation seat,
        HFlowContainer destination,
        Label heading,
        BoardRenderContext context)
    {
        IReadOnlyList<BoardCardPresentation> cards = seat.Hand?.Cards ?? [];
        int total = cards.Sum(card => card.Count);
        heading.Text = $"{seat.Name} · AUTHORIZED HAND · {total}";
        if (cards.Count == 0)
        {
            destination.AddChild(Text(
                "No cards are visible in this hand.", GodotThemeVariations.MutedText));
            return;
        }

        int pageCount = (int)Math.Ceiling(cards.Count / (double)CardsPerPage);
        int key = HandKey(seat.Seat);
        int page = context.Pages.Page(key, pageCount);
        if (pageCount > 1)
        {
            destination.AddChild(Pager(cards, key, page, pageCount, context));
        }
        foreach (BoardCardPresentation card in cards
                     .Skip(page * CardsPerPage)
                     .Take(CardsPerPage))
        {
            destination.AddChild(Card(card, context));
        }
    }

    private static VBoxContainer Pager(
        IReadOnlyList<BoardCardPresentation> cards,
        int key,
        int page,
        int pageCount,
        BoardRenderContext context)
    {
        int actions = cards.Sum(card => context.Interaction.ActionsFor(card.TargetId).Count);
        int selected = cards.Count(card => context.Interaction.IsSelected(card.TargetId));
        var stack = new VBoxContainer
        {
            Name = "HandPager",
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        stack.AddChild(Text(
            $"Hand page {page + 1}/{pageCount} · actions {actions} · selected {selected}",
            GodotThemeVariations.Caption));
        var row = new HBoxContainer
        {
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        var previous = PageButton("HandPrevious", "‹ Previous", page > 0, context.Scale);
        var next = PageButton("HandNext", "Next ›", page + 1 < pageCount, context.Scale);
        previous.Pressed += () =>
        {
            context.Pages.SetPage(key, page - 1, pageCount);
            context.Result.RequestRefresh(previous.Name);
        };
        next.Pressed += () =>
        {
            context.Pages.SetPage(key, page + 1, pageCount);
            context.Result.RequestRefresh(next.Name);
        };
        row.AddChild(previous);
        row.AddChild(next);
        stack.AddChild(row);
        return stack;
    }

    private static Control Card(
        BoardCardPresentation card, BoardRenderContext context)
    {
        CardLayoutMetrics geometry = VisualSystem.Card(CardDisplaySize.Hand, context.Scale);
        var wrapper = new Control
        {
            Name = card.TargetId is { } id ? $"HandCardSlot{id}" : "ConcealedHandSlot",
            CustomMinimumSize = new Vector2(geometry.Width, geometry.MinimumHeight),
        };
        CardControl control = CardControl.Create(
            card, CardDisplaySize.Hand, context.Scale, context.Art);
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
        string name, string text, bool enabled, InterfaceScale scale) => new()
    {
        Name = name,
        Text = text,
        Disabled = !enabled,
        CustomMinimumSize = new Vector2(
            VisualSystem.Controls(scale).MinimumButtonWidth,
            VisualSystem.Controls(scale).MinimumHeight),
    };

    private static Label Text(string text, string variation) => new()
    {
        Text = text,
        ThemeTypeVariation = variation,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };

    internal static int HandKey(int seat) => checked(-3_000_000 - seat);
}
