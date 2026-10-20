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
    private readonly SpatialCardFactory faces;
    private readonly bool openingMulligan;
    private readonly SpatialTablePileRenderer piles;
    private readonly Dictionary<int, (CardControl Control, Vector2 Position)> hosts = [];
    private int renderedCardCount;


    internal SpatialTableObjectRenderer(
        Control surface,
        BoardRenderResult result,
        AstraTableGeometry geometry,
        SpatialCardFactory faces,
        bool openingMulligan)
    {
        this.surface = surface;
        this.result = result;
        this.geometry = geometry;
        scale = faces.Scale;
        art = faces.Art;
        this.faces = faces;
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
        RenderCards(areas, "SideSchemesArea", geometry.SideSchemes, CardDisplaySize.Board);
    }

    internal void RenderRevealing(IReadOnlyList<BoardAreaPresentation> areas,
        IReadOnlyList<BoardCardPresentation>? context = null)
    {
        BoardAreaPresentation[] resolving = SpatialTableZones.Resolving(areas);
        if (context is { Count: > 0 })
            resolving = [new BoardAreaPresentation(-1, "Current decision", "", context, []), .. resolving];
        CausalCardReferences.Add(surface, geometry.Revealing, resolving.SelectMany(SpatialTableZones.Current), result, scale);
    }

    internal void RenderPlayer(IReadOnlyList<BoardAreaPresentation> areas)
    {
        int lowerStart = surface.GetChildCount();
        if (!piles.Render(areas, "DiscardPile", geometry.PlayerDiscard))
        {
            piles.RenderEmpty("PlayerDiscard", "Discard\nEmpty", geometry.PlayerDiscard, openingMulligan);
        }
        piles.Render(areas, "PlayerDeck", geometry.PlayerDeck);
        TableCanvasReflow.MoveAddedMiddle(surface, lowerStart);
        RenderCards(areas, "EngagedEnemiesArea", geometry.EngagedEnemies, CardDisplaySize.Board);
        lowerStart = surface.GetChildCount();
        RenderCards(areas, "HeroArea", geometry.Identity, CardDisplaySize.Board);
        RenderCards(areas, "AlliesArea", geometry.Allies, CardDisplaySize.Board);
        RenderCards(areas, "SupportsArea", geometry.Assets, CardDisplaySize.Board);
        RenderCards(areas, "UpgradesArea", geometry.Upgrades, CardDisplaySize.Board, includeHosted: true);
        TableCanvasReflow.MoveAddedMiddle(surface, lowerStart);
    }

    internal void RenderHosted(IReadOnlyList<BoardAreaPresentation> areas)
    {
        int seat = areas.FirstOrDefault(area => area.Zone == "HeroArea")?.Seat ?? -1;
        BoardCardPresentation[] controlled = CardSourceGroups.Controlled(areas, seat);
        HashSet<int?> controlledIds = [.. controlled.Select(card => card.TargetId)];
        foreach (var host in hosts)
        {
            SpatialTableHostAttachments.Add(host.Value.Control,
                [.. CardSourceGroups.Attached(areas, host.Key).Where(card => !controlledIds.Contains(card.TargetId))], result, scale, art);
        }
        foreach (BoardAreaPresentation area in areas.Where(area => area.Host >= 0))
            if (hosts.TryGetValue(area.Host, out var host))
                SpatialTableHostAttachments.AddOther(host.Control, area, result, scale, art);
        if (controlled.Length == 0) return;
        Control sources = SourceTableauRenderer.Create(controlled, geometry.SourceTableau, result, faces.ReadingScale, seat,
            areas.SelectMany(area => area.Cards).Where(card => card.TargetId.HasValue && !card.Concealed)
                .DistinctBy(card => card.TargetId).ToDictionary(card => card.TargetId!.Value, card => card.Title));
        surface.AddChild(sources);
        TableCanvasReflow.Grow(surface, sources);
        TableCanvasReflow.MoveMiddle(surface, sources);
        result.RegisterDropTarget(seat, sources);
    }

    internal void RenderHand(BoardAreaPresentation? hand)
    {
        BoardCardPresentation[] cards = hand is null ? [] : HandPresentationOrder.Arrange(hand.Seat, SpatialTableZones.Current(hand));
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
        bool includeHosted = false)
    {
        BoardAreaPresentation[] matching = [.. areas.Where(area => area.Zone == zone && (includeHosted || area.Host < 0))];
        RenderCardRegion(matching, zone, region, size);
    }

    private void RenderCardRegion(BoardAreaPresentation[] matching, string zone,
        Rect2 region, CardDisplaySize size)
    {
        if (zone != "RevealingArea") matching = [.. matching.Select(area => area with
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
        if (zone is "SideSchemesArea" or "EngagedEnemiesArea" or "AlliesArea")
        {
            IReadOnlyList<CardControl> collection = zone == "SideSchemesArea"
                ? SchemeStackCollection.Render(surface, region, cards, result, faces)
                : SpatialCardCollection.Render(surface, region, zone, cards, result, faces);
            foreach (CardControl card in collection)
                if (card.TargetId is { } id) hosts[id] = (card, card.Position);
            return;
        }
        for (int index = 0; index < cards.Length; index++)
        {
            CardControl control = AddCard(cards[index], region.Position + new Vector2(index * 24, 0), size, 8 + index);
            if (SpatialTableZones.IsExhausted(cards[index]))
            {
                Exhaust(control, cards[index], control.Position);
                result.UpdateRestingPose(control);
            }
        }
    }
    private CardControl AddCard(
        BoardCardPresentation card,
        Vector2 position,
        CardDisplaySize size,
        int z,
        bool isHand = false)
    {
        CardControl control = faces.Create(card, size);
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
        control.PivotOffset = Vector2.Zero;
        control.Rotation = Mathf.Pi / 2;
        control.Position = position + new Vector2(control.Size.Y, 0);
        control.SetMeta("spatial_exhausted", true);
        SpatialCardSidecar.Place(control);
    }

}
