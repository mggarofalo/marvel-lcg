using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Places independent encounter groups from explicit published relationships.</summary>
internal sealed class EncounterGroupTableRenderer(
    Control surface, BoardRenderResult result, SpatialCardFactory faces, SpatialTablePileRenderer piles)
{
    internal IReadOnlyList<CardControl> Render(IReadOnlyList<BoardAreaPresentation> areas,
        IReadOnlyList<EncounterGroupDescriptor> groups, AstraTableGeometry geometry)
    {
        var cards = new List<CardControl>();
        float left = geometry.MainScheme.Position.X;
        float right = geometry.EngagedEnemies.Position.X - 12;
        float width = (right - left) / groups.Count;
        for (int index = 0; index < groups.Count; index++)
            RenderGroup(areas, groups[index], new Rect2(left + index * width, 12, width - 8, 334), cards);
        var main = areas.Where(area => area.Zone == "MainSchemesArea")
            .SelectMany(SpatialTableZones.Current).ToArray();
        foreach (var card in main) AddFace(card, new Rect2(22, 38, left - 40, 132), 8, cards);
        HashSet<int?> paired = [.. groups.Select(group => group.Scheme)];
        var extra = areas.Where(SpatialTableZones.IsEncounterSupport)
            .SelectMany(SpatialTableZones.Current).Where(card => !paired.Contains(card.TargetId)).ToArray();
        if (extra.Length > 0)
            cards.AddRange(SchemeStackCollection.Render(surface, new Rect2(22, 184, left - 40, 160), extra, result, faces));
        return cards;
    }

    private void RenderGroup(IReadOnlyList<BoardAreaPresentation> areas, EncounterGroupDescriptor group,
        Rect2 bounds, List<CardControl> controls)
    {
        var visible = areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.TargetId is not null).DistinctBy(card => card.TargetId)
            .ToDictionary(card => card.TargetId!.Value);
        var title = new Label
        {
            Name = $"EncounterGroup{group.Group}", Position = bounds.Position,
            Size = new Vector2(bounds.Size.X, 44),
            Text = $"{group.Title}\n{(group.Active ? "ACTIVE VILLAIN" : group.Defeated ? "DEFEATED" : "Inactive villain")}",
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = GodotThemeVariations.Caption,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        surface.AddChild(title);
        result.Inspector.Register(visible.Values.ToArray());
        if (group.Villain is { } villain && visible.TryGetValue(villain, out var current))
            AddFace(current, new Rect2(bounds.Position.X, 62, bounds.Size.X, 156), 10, controls);
        bool hasUpcoming = group.Stages.Where(visible.ContainsKey)
            .Any(id => visible[id].StageRole == BoardStageRole.Upcoming);
        if (group.Scheme is { } scheme && visible.TryGetValue(scheme, out var paired) && !group.Defeated)
            AddFace(paired, new Rect2(bounds.Position.X, hasUpcoming ? 248 : 224, bounds.Size.X, hasUpcoming ? 68 : 92), 10, controls);
        RenderPiles(areas, group, bounds);
        var upcoming = group.Stages.Where(visible.ContainsKey).Select(id => visible[id])
            .Where(card => card.StageRole == BoardStageRole.Upcoming).ToArray();
        if (upcoming.Length > 0) AddUpcoming(group, upcoming, bounds);
    }

    private void RenderPiles(IReadOnlyList<BoardAreaPresentation> areas, EncounterGroupDescriptor group, Rect2 bounds)
    {
        float width = (bounds.Size.X - 4) / 2;
        if (areas.FirstOrDefault(area => area.Id == group.Deck) is { } deck)
            piles.RenderNamed(deck, new Rect2(bounds.Position.X, 320, width, 40), $"{group.Title} encounter deck");
        if (areas.FirstOrDefault(area => area.Id == group.Discard) is { } discard)
            piles.RenderNamed(discard, new Rect2(bounds.Position.X + width + 4, 320, width, 40),
                $"{group.Title} encounter discard");
    }

    private void AddUpcoming(EncounterGroupDescriptor group, BoardCardPresentation[] cards, Rect2 bounds)
    {
        var button = new Button
        {
            Name = $"EncounterUpcoming{group.Group}", Text = "Next stage",
            Position = new Vector2(bounds.Position.X, 220), Size = new Vector2(bounds.Size.X, 26),
            TooltipText = $"Inspect {group.Title}'s next stage", ZIndex = 20,
        };
        var area = new BoardAreaPresentation(group.Group, $"{group.Title} upcoming stages", "Out of play", cards, [])
            { Zone = "VillainDeck" };
        button.Pressed += () => TabletopPileInspector.Show(button, TabletopAreaObject.From(area), result, faces.Scale, faces.Art, allowActions: false);
        surface.AddChild(button);
        TableCompactButtonStyle.Apply(button);
    }

    private void AddFace(BoardCardPresentation card, Rect2 bounds, int z, List<CardControl> controls)
    {
        var layout = CardControl.LayoutFor(card, CardDisplaySize.Board, faces.Scale);
        float factor = Math.Min(1, Math.Min(bounds.Size.X / layout.Width, bounds.Size.Y / layout.MinimumHeight));
        var control = faces.CreateFitted(card, factor);
        control.Name = $"ProceduralCard{card.TargetId}";
        control.Position = bounds.Position + new Vector2((bounds.Size.X - control.CustomMinimumSize.X) / 2, 0);
        control.Size = control.CustomMinimumSize;
        control.ZIndex = z;
        control.SetMeta("spatial_resting_z", z);
        surface.AddChild(control);
        SpatialCardSidecar.State(control, card);
        if (card.TargetId is { } id) result.Register(id, control);
        result.TrackCard(control, card);
        controls.Add(control);
    }
}
