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
    private CardControl? revealedCard;

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
            piles.RenderEmpty("EncounterDiscard", "Discard\nEmpty", geometry.EncounterDiscard, false);
        }
        piles.Render(areas, "EncounterDeck", geometry.EncounterDeck);
        RenderCards(areas, "MainSchemesArea", geometry.MainScheme, CardDisplaySize.Board);
        RenderCards(areas, "VillainArea", geometry.Villain, CardDisplaySize.Board);
        RenderCards(areas, "SideSchemesArea", geometry.SideSchemes, CardDisplaySize.Board,
            geometry.LargeText ? 1 : 2);
    }

    internal void RenderRevealing(IReadOnlyList<BoardAreaPresentation> areas) =>
        RenderCardRegion(SpatialTableZones.Resolving(areas), "RevealingArea",
            geometry.Revealing, CardDisplaySize.Hand, int.MaxValue);

    internal void RenderPlayer(IReadOnlyList<BoardAreaPresentation> areas)
    {
        if (!piles.Render(areas, "DiscardPile", geometry.PlayerDiscard))
        {
            piles.RenderEmpty("PlayerDiscard", "Discard\nEmpty", geometry.PlayerDiscard, openingMulligan);
        }
        piles.Render(areas, "PlayerDeck", geometry.PlayerDeck);
        RenderCards(areas, "EngagedEnemiesArea", geometry.EngagedEnemies, CardDisplaySize.Board, 1);
        RenderCards(areas, "HeroArea", geometry.Identity, CardDisplaySize.Board);
        RenderCards(areas, "AlliesArea", geometry.Allies, CardDisplaySize.Board,
            geometry.LargeText ? 2 : 3);
        RenderCards(areas, "SupportsArea", geometry.Assets, CardDisplaySize.Board,
            geometry.HasRevealingCard && !geometry.HasSeparateRevealSlot ? 0 : 1);
        RenderCards(areas, "UpgradesArea", geometry.Upgrades, CardDisplaySize.Board, 1, includeHosted: true);
    }

    internal void RenderHosted(IReadOnlyList<BoardAreaPresentation> areas)
    {
        foreach (var host in hosts)
        {
            SpatialTableHostAttachments.Add(host.Value.Control, CardSourceGroups.Attached(areas, host.Key), result, scale, art);
        }
        foreach (BoardAreaPresentation area in areas.Where(area => area.Host >= 0))
            if (hosts.TryGetValue(area.Host, out var host))
                SpatialTableHostAttachments.AddOther(host.Control, area, result, scale, art);
        int seat = areas.FirstOrDefault(area => area.Zone == "HeroArea")?.Seat ?? -1;
        BoardCardPresentation[] controlled = CardSourceGroups.Controlled(areas, seat);
        if (controlled.Length == 0) return;
        VBoxContainer ledger = CardSourceCollection.Create(controlled, CardSourceStrip.LedgerWidth, result, scale, "Controlled upgrades");
        ledger.Name = "ControlledSourceLedger";
        ledger.Position = geometry.Upgrades.Position;
        surface.AddChild(ledger);
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
            control.SetMeta("spatial_hand_exposed_width", geometry.HandExposedWidth(index, cards.Length, width));
            result.UpdateRestingPose(control);
            if (openingMulligan && cards[index].TargetId is { } id)
            {
                result.RegisterMulliganCard(id, control);
                AddMulliganToggle(id, control);
            }
        }
    }

    internal static IReadOnlyList<BoardAreaPresentation> Unplaced(
        IReadOnlyList<BoardAreaPresentation> areas, IReadOnlyCollection<int>? visibleHosts = null) => [.. areas.Where(area =>
        area.Prominence != BoardAreaProminence.Empty
        && area.Zone != "HandsArea"
        && (area.Host < 0 && !SpatialTableZones.Known.Contains(area.Zone)
            || area.Host >= 0 && visibleHosts is not null && !visibleHosts.Contains(area.Host)))];

    internal void RenderOverflow(IReadOnlyList<BoardAreaPresentation> areas)
        => piles.RenderOverflow(Unplaced(areas, hosts.Keys), geometry.Overflow);

    private void RenderCards(
        IReadOnlyList<BoardAreaPresentation> areas,
        string zone,
        Rect2 region,
        CardDisplaySize size,
        int maximumVisible = int.MaxValue,
        bool includeHosted = false)
    {
        BoardAreaPresentation[] matching = [.. areas.Where(area => area.Zone == zone && (includeHosted || area.Host < 0))];
        RenderCardRegion(matching, zone, region, size, maximumVisible);
    }

    private void RenderCardRegion(BoardAreaPresentation[] matching, string zone,
        Rect2 region, CardDisplaySize size, int maximumVisible)
    {
        matching = [.. matching.Select(area => area with
        { Cards = [.. area.Cards.Where(card => !CardSourceGroups.IsLocalSource(card))] })];
        BoardCardPresentation[] cards = [.. matching.SelectMany(SpatialTableZones.Current)];
        if (cards.Length == 0)
        {
            return;
        }
        foreach (BoardAreaPresentation area in matching)
        {
            result.Inspector.Register(area.Cards);
        }
        Vector2 objectSize = SpatialCardFootprint.OccupiedSize(SpatialCardMetrics.Envelope(cards, size, scale));
        int capacity = (int)Math.Floor((region.Size.X + 14) / (objectSize.X + 14));
        BoardCardPresentation[] visible = [.. cards.Take(Math.Min(maximumVisible, capacity))];
        CardControl? drawerHost = zone == "SupportsArea" && maximumVisible == 0 ? revealedCard : null;
        bool hasDrawer = cards.Length > visible.Length;
        drawerHost = RenderVisible(visible, zone, region, size, matching, hasDrawer) ?? drawerHost;

        if (hasDrawer)
        {
            piles.RenderRegion(matching, zone, SpatialRegionDrawerLayout.Bounds(region), drawerHost);
        }
    }

    private CardControl? RenderVisible(BoardCardPresentation[] visible, string zone, Rect2 region,
        CardDisplaySize size, BoardAreaPresentation[] matching, bool hasDrawer)
    {
        CardControl? first = null;
        Vector2 objectSize = SpatialCardFootprint.OccupiedSize(SpatialCardMetrics.Envelope(visible, size, scale));
        for (int index = 0; index < visible.Length; index++)
        {
            Vector2 position = geometry.Slot(region, index, visible.Length, objectSize);
            CardControl control = AddCard(visible[index], position, size, 8 + index);
            first ??= control;
            if (zone == "RevealingArea") revealedCard ??= control;
            SpatialTableLabel.Persistent(control, zone, matching, visible[index], hasDrawer);
            if (SpatialTableZones.IsExhausted(visible[index]))
            {
                Exhaust(control, visible[index], position);
                result.UpdateRestingPose(control);
            }
        }
        return first;
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
        if (!isHand) SpatialCardSidecar.State(control, card);
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

    private static void Exhaust(CardControl control, BoardCardPresentation card, Vector2 position)
    {
        control.PivotOffset = control.CustomMinimumSize / 2;
        control.Rotation = Mathf.Pi / 2;
        control.SetMeta("spatial_exhausted", true);
        SpatialCardSidecar.Place(control);
    }

    private void AddMulliganToggle(int id, CardControl card)
    {
        var toggle = new Button
        {
            Name = $"MulliganDiscard{id}",
            Text = "↻",
            AccessibilityName = "Select for replacement",
            ToggleMode = true,
            ZIndex = 0,
            ZAsRelative = true,
            TooltipText = "Select this card for replacement. The card body remains inspection.",
        };
        toggle.Pressed += () => result.RequestMulliganTarget(id);
        SpatialHandActionStrip.Place(card, toggle);
        result.RegisterMulliganToggle(id, toggle);
    }

}
