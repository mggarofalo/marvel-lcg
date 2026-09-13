using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders the opening hand as one far-to-near tabletop decision surface.</summary>
/// <remarks>
/// This is a product layout, not an alternate game model. Candidate legality and
/// selection remain in the prompt's shared <c>DecisionComposer</c> draft.
/// </remarks>
internal static class MulliganTableRenderer
{
    private static readonly string[] ScenarioOrder =
    [
        "EncounterDeck", "EncounterDiscardPile", "VillainArea", "MainSchemesArea",
    ];

    private static readonly string[] PlayerOrder =
    [
        "IdentityArea", "PlayerDeck", "DiscardPile",
    ];

    internal static void Render(
        VBoxContainer destination,
        BoardPresentation board,
        HBoxContainer hand,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art,
        int player)
    {
        destination.AddChild(Row(
            "VillainTable", "VILLAIN TABLE  ·  FAR SIDE", Ordered(board.Areas, -1, ScenarioOrder),
            result, scale, art));
        BoardAreaPresentation[] own = Ordered(board.Areas, player, PlayerOrder)
            .Where(area => PlayerOrder.Contains(area.Zone, StringComparer.Ordinal))
            .ToArray();
        destination.AddChild(Row(
            "PlayerTable", $"PLAYER {player + 1}  ·  NEAR SIDE", own, result, scale, art,
            addEmptyDiscard: own.All(area => area.Zone != "DiscardPile")));
        AddOtherPlayers(destination, board, player);
        RenderHand(hand, board.Areas, player, result, scale, art);
    }

    private static BoardAreaPresentation[] Ordered(
        IReadOnlyList<BoardAreaPresentation> areas, int seat, IReadOnlyList<string> order)
    {
        return areas.Where(area => area.Seat == seat)
            .Where(area => area.Zone != "HandsArea")
            .OrderBy(area => OrderOf(area.Zone, order))
            .ThenBy(area => area.Id)
            .ToArray();
    }

    private static int OrderOf(string zone, IReadOnlyList<string> order)
    {
        for (int index = 0; index < order.Count; index++)
        {
            if (string.Equals(order[index], zone, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return order.Count;
    }

    private static PanelContainer Row(
        string name,
        string title,
        IReadOnlyList<BoardAreaPresentation> areas,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art,
        bool addEmptyDiscard = false)
    {
        var panel = new PanelContainer
        {
            Name = name,
            ThemeTypeVariation = GodotThemeVariations.BoardArea,
        };
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        stack.AddChild(Label(title, GodotThemeVariations.Eyebrow));
        var rail = new HFlowContainer { Name = "Areas", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (BoardAreaPresentation area in areas)
        {
            rail.AddChild(Area(area, result, scale, art));
        }
        if (addEmptyDiscard)
        {
            rail.AddChild(EmptyDiscard(result, scale));
        }

        stack.AddChild(rail);
        panel.AddChild(stack);
        return panel;
    }

    private static PanelContainer EmptyDiscard(BoardRenderResult result, InterfaceScale scale)
    {
        var panel = new PanelContainer
        {
            Name = "MulliganDiscardPile",
            CustomMinimumSize = new Vector2(VisualSystem.Card(CardDisplaySize.Board, scale).Width + 28, 0),
            ThemeTypeVariation = GodotThemeVariations.BoardArea,
            TooltipText = "Discard pile. Drop an opening-hand card here to select it for replacement.",
        };
        panel.AddChild(Label("DISCARD PILE\nEmpty", GodotThemeVariations.Caption, wrap: true));
        result.RegisterMulliganDiscard(panel);
        return panel;
    }

    private static PanelContainer Area(
        BoardAreaPresentation area,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art)
    {
        var panel = new PanelContainer
        {
            Name = $"Area{area.Id}",
            CustomMinimumSize = new Vector2(VisualSystem.Card(CardDisplaySize.Board, scale).Width + 28, 0),
            ThemeTypeVariation = GodotThemeVariations.BoardArea,
            TooltipText = area.Context,
        };
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        stack.AddChild(Label(area.Title, GodotThemeVariations.Caption, wrap: true));
        var cards = new HBoxContainer { ThemeTypeVariation = GodotThemeVariations.CompactRow };
        foreach (BoardCardPresentation card in area.Cards.Concat(area.Removed))
        {
            AddCard(cards, card, result, CardDisplaySize.Board, scale, art);
        }

        if (cards.GetChildCount() == 0)
        {
            cards.AddChild(Label("Empty", GodotThemeVariations.MutedText));
        }

        stack.AddChild(cards);
        panel.AddChild(stack);
        if (area.Zone == "DiscardPile")
        {
            result.RegisterMulliganDiscard(panel);
        }

        return panel;
    }

    private static void AddOtherPlayers(
        VBoxContainer destination, BoardPresentation board, int expandedPlayer)
    {
        foreach (BoardLanePresentation lane in board.Lanes.Where(lane => lane.Seat is not null
                     && lane.Seat != expandedPlayer))
        {
            // No private area is rendered here. This compact public label keeps
            // cooperation legible without making a second hand or tableau.
            destination.AddChild(Label(
                $"{lane.Title}  ·  PUBLIC SUMMARY", GodotThemeVariations.Caption, wrap: true));
        }
    }

    private static void RenderHand(
        HBoxContainer hand,
        IReadOnlyList<BoardAreaPresentation> areas,
        int player,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art)
    {
        BoardAreaPresentation? handArea = areas.FirstOrDefault(area => area.Seat == player
            && area.Zone == "HandsArea");
        IReadOnlyList<BoardCardPresentation> cards = handArea?.Cards ?? [];
        if (hand.GetParent() is ScrollContainer scroll)
        {
            int required = cards.Count * VisualSystem.Card(CardDisplaySize.Mulligan, scale).Width
                + Math.Max(0, cards.Count - 1) * 8;
            int available = Mathf.RoundToInt(hand.GetViewportRect().Size.X) - 360;
            // The 1920 desktop profile fits every opening choice at once. A
            // smaller diagnostic viewport keeps overflow local to this hand.
            scroll.HorizontalScrollMode = available >= required
                ? ScrollContainer.ScrollMode.Disabled
                : ScrollContainer.ScrollMode.Auto;
        }

        foreach (BoardCardPresentation card in cards)
        {
            var choice = new VBoxContainer
            {
                Name = $"MulliganCard{card.TargetId}",
                ThemeTypeVariation = GodotThemeVariations.TightStack,
            };
            AddCard(choice, card, result, CardDisplaySize.Mulligan, scale, art);
            if (card.TargetId is { } id && !card.Concealed)
            {
                var discard = new Button
                {
                    Name = $"MulliganDiscard{id}",
                    Text = "□ DISCARD",
                    ToggleMode = true,
                    TooltipText = "Select this card for replacement. Space toggles this checkbox.",
                };
                discard.CustomMinimumSize = new Vector2(0, VisualSystem.Controls(scale).MinimumPointerTarget);
                discard.Pressed += () => result.RequestMulliganTarget(id);
                choice.AddChild(discard);
                result.RegisterMulliganToggle(id, discard);
            }

            hand.AddChild(choice);
        }
    }

    private static void AddCard(
        Container destination,
        BoardCardPresentation card,
        BoardRenderResult result,
        CardDisplaySize size,
        InterfaceScale scale,
        ICardArtProvider? art)
    {
        CardControl control = CardControl.Create(card, size, scale, art);
        destination.AddChild(control);
        if (card.TargetId is { } id)
        {
            result.Register(id, control);
            if (size == CardDisplaySize.Mulligan)
            {
                result.RegisterMulliganCard(id, control);
            }
        }

        result.TrackCard(control, card);
    }

    private static Label Label(string text, string variation, bool wrap = false) => new()
    {
        Text = text,
        ThemeTypeVariation = variation,
        AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
    };
}
