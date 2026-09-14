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
        "HeroArea", "IdentityArea", "PlayerDeck", "DiscardPile",
    ];

    internal static void Render(VBoxContainer destination, MulliganTableContext context)
    {
        destination.AddChild(Row(
            "VillainTable", "VILLAIN TABLE  ·  FAR SIDE", Ordered(context.Board.Areas, -1, ScenarioOrder),
            context.Result, context.Scale, context.Art, new MulliganTableRowOptions(false, null)));
        PanelContainer? seatStrip = context.Board.PlayerSummaries.Count > 1
            ? MulliganSeatStripRenderer.Create(context.Board, context.Player, context.SwitchSeat)
            : null;
        BoardAreaPresentation[] own = Ordered(context.Board.Areas, context.Player, PlayerOrder)
            .Where(area => PlayerOrder.Contains(area.Zone, StringComparer.Ordinal))
            .ToArray();
        destination.AddChild(Row(
            "PlayerTable", $"PLAYER {context.Player + 1}  ·  NEAR SIDE", own,
            context.Result, context.Scale, context.Art,
            new MulliganTableRowOptions(own.All(area => area.Zone != "DiscardPile"), seatStrip)));
        RenderHand(context.Hand, context.Board.Areas, context.PromptOwner,
            context.Result, context.Scale, context.Art);
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
        MulliganTableRowOptions options)
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
        if (options.AddEmptyDiscard)
        {
            rail.AddChild(EmptyDiscard(result, scale));
        }
        if (options.Companion is not null)
        {
            rail.AddChild(options.Companion);
        }

        stack.AddChild(rail);
        panel.AddChild(stack);
        return panel;
    }

    private static PanelContainer EmptyDiscard(BoardRenderResult result, InterfaceScale scale)
    {
        var panel = new PanelContainer
        {
            Name = "ExpandedDiscardPile",
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
        if (area.Zone is "HeroArea" or "VillainArea" or "MainSchemesArea")
        {
            BoardCardPresentation? current = area.Cards.FirstOrDefault(card =>
                card.StageRole != BoardStageRole.Upcoming);
            if (current is not null)
            {
                AddCard(cards, current, result, CardDisplaySize.Board, scale, art);
            }
        }
        else
        {
            cards.AddChild(Tile(area));
        }

        if (cards.GetChildCount() == 0)
        {
            cards.AddChild(Label("Empty", GodotThemeVariations.MutedText));
        }

        stack.AddChild(cards);
        panel.AddChild(stack);
        return panel;
    }

    private static PanelContainer Tile(BoardAreaPresentation area)
    {
        int count = area.Cards.Sum(card => card.Count) + area.Removed.Sum(card => card.Count);
        string detail = area.Cards.Count == 0 ? "Empty" : area.Cards[0].Title;
        var tile = new PanelContainer
        {
            Name = $"PileTile{area.Id}",
            CustomMinimumSize = new Vector2(124, 0),
            ThemeTypeVariation = GodotThemeVariations.BoardArea,
        };
        tile.AddChild(Label($"{detail}\n{count} CARD{(count == 1 ? string.Empty : "S")}",
            GodotThemeVariations.Caption, wrap: true));
        return tile;
    }

    private static void RenderHand(
        HBoxContainer hand,
        IReadOnlyList<BoardAreaPresentation> areas,
        int player,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art)
    {
        var destination = new PanelContainer
        {
            Name = "MulliganDiscardPile",
            CustomMinimumSize = new Vector2(124, 0),
            ThemeTypeVariation = GodotThemeVariations.BoardArea,
            TooltipText = $"Player {player + 1} discard destination for this pending opening-hand decision.",
        };
        destination.AddChild(Label($"PLAYER {player + 1}\nDISCARD", GodotThemeVariations.Caption, wrap: true));
        result.RegisterMulliganDiscard(destination);
        hand.AddChild(destination);
        BoardAreaPresentation? handArea = areas.FirstOrDefault(area => area.Seat == player
            && area.Zone == "HandsArea");
        IReadOnlyList<BoardCardPresentation> cards = handArea?.Cards ?? [];
        if (hand.GetParent() is ScrollContainer scroll)
        {
            int required = cards.Count * VisualSystem.Card(CardDisplaySize.Mulligan, scale).Width
                + Math.Max(0, cards.Count - 1) * 8;
            int available = Mathf.RoundToInt(hand.GetViewportRect().Size.X) - 24;
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
                discard.CustomMinimumSize = new Vector2(
                    0,
                    VisualSystem.Controls(scale).MinimumPointerTarget);
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
        // Only the choice shelf compacts card faces at desktop scales. The
        // villain, identity, and scheme cards keep their selected scale so
        // essential current values remain readable.
        InterfaceScale faceScale = size == CardDisplaySize.Mulligan
            && (int)scale > (int)InterfaceScale.Standard
            ? InterfaceScale.Standard
            : scale;
        CardControl control = CardControl.Create(card, size, faceScale, art);
        if (size == CardDisplaySize.Mulligan && faceScale != scale)
        {
            control.CustomMinimumSize = new Vector2(
                VisualSystem.Card(CardDisplaySize.Mulligan, scale).Width,
                control.CustomMinimumSize.Y);
        }
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
