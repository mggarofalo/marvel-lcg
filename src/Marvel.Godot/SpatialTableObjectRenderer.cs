using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Places visibility-safe cards and piles as physical objects on an Astra surface.</summary>
internal sealed class SpatialTableObjectRenderer
{
    private readonly Control surface;
    private readonly BoardRenderResult result;
    private readonly AstraTableGeometry geometry;
    private readonly InterfaceScale scale;
    private readonly ICardArtProvider? art;
    private readonly bool openingMulligan;
    private readonly SpatialTablePileRenderer piles;
    private readonly Dictionary<int, (CardControl Control, Vector2 Position)> hosts = [];
    private int renderedCardCount;

    internal SpatialTableObjectRenderer(
        Control surface,
        BoardRenderResult result,
        AstraTableGeometry geometry,
        InterfaceScale scale,
        ICardArtProvider? art,
        bool openingMulligan)
    {
        this.surface = surface;
        this.result = result;
        this.geometry = geometry;
        this.scale = scale;
        this.art = art;
        this.openingMulligan = openingMulligan;
        piles = new SpatialTablePileRenderer(surface, result, scale, art, openingMulligan);
    }

    internal void RenderScenario(IReadOnlyList<BoardAreaPresentation> areas)
    {
        if (!piles.Render(areas, "EncounterDiscardPile", geometry.EncounterDiscard))
        {
            piles.RenderEmpty("EncounterDiscard", "DISCARD\nEMPTY", geometry.EncounterDiscard, false);
        }
        piles.Render(areas, "EncounterDeck", geometry.EncounterDeck);
        RenderCards(areas, "MainSchemesArea", geometry.MainScheme, CardDisplaySize.Board);
        RenderCards(areas, "VillainArea", geometry.Villain, CardDisplaySize.Board);
        RenderCards(areas, "SideSchemesArea", geometry.SideSchemes, CardDisplaySize.Board,
            geometry.LargeText ? 1 : 2);
        RenderCards(areas, "RevealingArea", geometry.Context, CardDisplaySize.Hand);
    }

    internal void RenderPlayer(IReadOnlyList<BoardAreaPresentation> areas)
    {
        if (!piles.Render(areas, "DiscardPile", geometry.PlayerDiscard))
        {
            piles.RenderEmpty("PlayerDiscard", "DISCARD\nEMPTY", geometry.PlayerDiscard, openingMulligan);
        }
        piles.Render(areas, "PlayerDeck", geometry.PlayerDeck);
        RenderCards(areas, "EngagedEnemiesArea", geometry.EngagedEnemies, CardDisplaySize.Hand,
            geometry.LargeText ? 2 : 3);
        RenderCards(areas, "HeroArea", geometry.Identity, CardDisplaySize.Board);
        RenderCards(areas, "AlliesArea", geometry.Allies, CardDisplaySize.Hand,
            geometry.LargeText ? 2 : 3);
        RenderCards(areas, "SupportsArea", geometry.Assets with
        {
            Size = new Vector2(geometry.Assets.Size.X * 0.31f, geometry.Assets.Size.Y),
        }, CardDisplaySize.Hand, geometry.LargeText ? 1 : 2);
        RenderCards(areas, "UpgradesArea", geometry.Assets with
        {
            Position = geometry.Assets.Position + new Vector2(geometry.Assets.Size.X * 0.59f, 0),
            Size = new Vector2(geometry.Assets.Size.X * 0.41f, geometry.Assets.Size.Y),
        }, CardDisplaySize.Hand, geometry.LargeText ? 1 : 2);
    }

    internal void RenderHosted(IReadOnlyList<BoardAreaPresentation> areas)
    {
        foreach (BoardAreaPresentation area in areas.Where(candidate => candidate.Host >= 0))
        {
            if (!hosts.TryGetValue(area.Host, out var host))
            {
                continue;
            }
            BoardCardPresentation[] cards = SpatialTableZones.Current(area);
            for (int index = 0; index < cards.Length; index++)
            {
                Vector2 position = host.Position + new Vector2(
                    host.Control.CustomMinimumSize.X - 34 + index * 24,
                    70 + index * 18);
                CardControl control = AddCard(cards[index], position, CardDisplaySize.Hand, 4 + index);
                var viewport = new Control
                {
                    Name = $"AttachmentViewport{area.Host}-{index}",
                    Position = position,
                    Size = new Vector2(132, 170),
                    CustomMinimumSize = new Vector2(132, 170),
                    ClipContents = true,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    ZIndex = 4 + index,
                };
                surface.AddChild(viewport);
                control.Reparent(viewport);
                control.Position = Vector2.Zero;
                control.Scale = Vector2.One * 0.70f;
                control.ZIndex = 0;
                control.ZAsRelative = true;
                result.UpdateRestingPose(control);
                control.TooltipText = $"Attached to {area.HostedBy}. {control.TooltipText}";
                SpatialTableLabel.Add(surface,
                    $"AttachmentHost{area.Host}-{index}",
                    "ATTACHED",
                    position + new Vector2(8, -20), 100);
            }
        }
    }

    internal void RenderHand(BoardAreaPresentation? hand)
    {
        BoardCardPresentation[] cards = hand is null ? [] : SpatialTableZones.Current(hand);
        float width = VisualSystem.Card(CardDisplaySize.Hand, scale).Width;
        for (int index = 0; index < cards.Length; index++)
        {
            SpatialCardPlacement placement = geometry.HandCard(index, cards.Length, width);
            CardControl control = AddCard(
                cards[index], placement.Position, CardDisplaySize.Hand, placement.ZIndex, isHand: true);
            control.Rotation = placement.Rotation;
            control.PivotOffset = control.CustomMinimumSize / 2;
            control.SetMeta("spatial_hand_index", index);
            control.SetMeta("spatial_hand_overlap", placement.Overlaps);
            result.UpdateRestingPose(control);
            if (openingMulligan && cards[index].TargetId is { } id)
            {
                result.RegisterMulliganCard(id, control);
                AddMulliganToggle(id, control);
            }
        }
    }

    internal void RenderDecisionAnchors(
        IReadOnlyList<BoardAreaPresentation> areas,
        IReadOnlyCollection<int> sourceCards)
    {
        int index = 0;
        foreach (int id in sourceCards.OrderBy(value => value))
        {
            if (hosts.ContainsKey(id))
            {
                continue;
            }
            BoardCardPresentation? card = areas.SelectMany(SpatialTableZones.Current)
                .FirstOrDefault(candidate => candidate.TargetId == id);
            if (card is null)
            {
                continue;
            }
            Vector2 position = geometry.Allies.Position + new Vector2(index * 150, 0);
            CardControl control = AddCard(card, position, CardDisplaySize.Hand, 32 + index);
            control.TooltipText = $"Decision source from another player's public tableau. {control.TooltipText}";
            SpatialTableLabel.Add(surface, $"DecisionAnchor{id}", "DECISION SOURCE",
                position + new Vector2(0, -20));
            index++;
        }
    }

    internal static IReadOnlyList<BoardAreaPresentation> Unplaced(
        IReadOnlyList<BoardAreaPresentation> areas) => [.. areas.Where(area =>
        area.Prominence != BoardAreaProminence.Empty
        && area.Zone != "HandsArea"
        && area.Host < 0
        && !SpatialTableZones.Known.Contains(area.Zone))];

    internal void RenderOverflow(IReadOnlyList<BoardAreaPresentation> areas)
        => piles.RenderOverflow(areas, geometry.Overflow);

    private void RenderCards(
        IReadOnlyList<BoardAreaPresentation> areas,
        string zone,
        Rect2 region,
        CardDisplaySize size,
        int maximumVisible = int.MaxValue)
    {
        BoardAreaPresentation[] matching = [.. areas.Where(area => area.Zone == zone && area.Host < 0)];
        BoardCardPresentation[] cards = [.. matching.SelectMany(SpatialTableZones.Current)];
        if (cards.Length == 0)
        {
            return;
        }
        foreach (BoardAreaPresentation area in matching)
        {
            result.Inspector.Register(area.Cards);
        }
        Vector2 objectSize = new(
            VisualSystem.Card(size, scale).Width,
            VisualSystem.Card(size, scale).MinimumHeight);
        BoardCardPresentation[] visible = [.. cards.Take(maximumVisible)];
        for (int index = 0; index < visible.Length; index++)
        {
            Vector2 position = geometry.Slot(region, index, visible.Length, objectSize);
            CardControl control = AddCard(visible[index], position, size, 8 + index);
            if (zone is "SupportsArea" or "UpgradesArea")
            {
                SpatialTableLabel.Add(surface, $"Persistent{visible[index].TargetId}",
                    visible[index].Title.ToUpperInvariant(),
                    position + new Vector2(0, -18));
            }
            if (SpatialTableZones.IsExhausted(visible[index]))
            {
                Exhaust(control, visible[index], position);
                result.UpdateRestingPose(control);
            }
        }
        if (cards.Length > visible.Length)
        {
            piles.RenderOverflow(matching, new Rect2(
                region.End.X - Math.Min(164, region.Size.X),
                region.End.Y - 44,
                Math.Min(164, region.Size.X),
                40));
        }
    }

    private CardControl AddCard(
        BoardCardPresentation card,
        Vector2 position,
        CardDisplaySize size,
        int z,
        bool isHand = false)
    {
        CardControl control = CardControl.Create(card, size, scale, art);
        control.Name = card.TargetId is { } id
            ? $"ProceduralCard{id}"
            : $"ProceduralCardHidden{renderedCardCount}";
        renderedCardCount++;
        control.Position = position;
        control.ZIndex = z;
        control.SetMeta("spatial_resting_z", z);
        control.MouseFilter = Control.MouseFilterEnum.Pass;
        surface.AddChild(control);
        control.Size = control.CustomMinimumSize;
        Callable.From(() =>
        {
            if (InteractionControl.IsUsable(control))
            {
                // Wrapped labels establish their final minimum after entering
                // the tree. Collapse the temporary pre-layout height back to
                // the authored physical-card bounds once that pass settles.
                control.Size = control.CustomMinimumSize;
            }
        }).CallDeferred();
        if (card.TargetId is { } target)
        {
            result.Register(target, control);
            hosts[target] = (control, position);
        }
        result.TrackCard(control, card, isHand);
        return control;
    }

    private void Exhaust(CardControl control, BoardCardPresentation card, Vector2 position)
    {
        control.PivotOffset = control.CustomMinimumSize / 2;
        control.Rotation = Mathf.Pi / 2;
        control.SetMeta("spatial_exhausted", true);
        SpatialTableLabel.Add(surface,
            $"ExhaustedCaption{card.TargetId}",
            $"{card.Title.ToUpperInvariant()}  ·  EXHAUSTED",
            position + new Vector2(-18, -28),
            width: 260,
            z: 180);
    }

    private void AddMulliganToggle(int id, CardControl card)
    {
        Vector2 cardSize = card.CustomMinimumSize;
        var overlay = new Control
        {
            Name = $"MulliganOverlay{id}",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        var toggle = new Button
        {
            Name = $"MulliganDiscard{id}",
            Text = "□ DISCARD",
            ToggleMode = true,
            Position = new Vector2(20, cardSize.Y - 48),
            Size = new Vector2(Math.Max(92, cardSize.X - 40), 44),
            CustomMinimumSize = new Vector2(Math.Max(92, cardSize.X - 40), 44),
            ZIndex = 50,
            ZAsRelative = false,
            TooltipText = "Select this card for replacement. The card body remains inspection.",
        };
        toggle.Pressed += () => result.RequestMulliganTarget(id);
        overlay.AddChild(toggle);
        card.AddChild(overlay);
        result.RegisterMulliganToggle(id, toggle);
    }

}
