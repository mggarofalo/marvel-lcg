namespace Marvel.View;

/// <summary>Presentation-only board lanes derived from the visible area graph.</summary>
/// <remarks>
/// Scenario/player placement and lane order are client choices. They are not
/// tabletop rules, and unknown areas continue to render through the same graph.
/// </remarks>
public static class BoardLayout
{
    /// <summary>Groups every visible area exactly once, preserving allocation order.</summary>
    public static IReadOnlyList<BoardLanePresentation> Arrange(
        IReadOnlyList<BoardAreaPresentation> areas,
        IReadOnlyList<BoardPlayerPresentation> players)
    {
        ArgumentNullException.ThrowIfNull(areas);
        ArgumentNullException.ThrowIfNull(players);

        var byId = areas.ToDictionary(area => area.Id);
        var cardAreas = new Dictionary<int, int>();
        var cardTitles = new Dictionary<int, string>();
        foreach (BoardAreaPresentation area in areas)
        {
            foreach (BoardCardPresentation card in area.Cards.Concat(area.Removed))
            {
                if (card.TargetId is not { } id)
                {
                    continue;
                }

                cardAreas[id] = area.Id;
                cardTitles[id] = card.Title;
            }
        }

        var parents = new Dictionary<int, int>();
        var brokenHosts = new HashSet<int>();
        foreach (BoardAreaPresentation area in areas)
        {
            if (area.Host < 0)
            {
                continue;
            }

            if (cardAreas.TryGetValue(area.Host, out int parent) && parent != area.Id)
            {
                parents[area.Id] = parent;
            }
            else
            {
                brokenHosts.Add(area.Id);
            }
        }

        var seats = players.Select(player => player.Seat).ToHashSet();
        var laneByArea = new Dictionary<int, string>();
        foreach (BoardAreaPresentation area in areas)
        {
            ResolveLane(area.Id, [], laneByArea, parents, brokenHosts, byId, seats);
        }

        var laneOrder = new List<(string Key, string Title, int? Seat)>
        {
            ("scenario", "SCENARIO TABLE", null),
        };
        laneOrder.AddRange(players.Select(player =>
            ($"player-{player.Seat}", $"PLAYER {player.Seat + 1}  ·  {player.Name.ToUpperInvariant()}",
             (int?)player.Seat)));
        laneOrder.Add(("other", "OTHER TABLE AREAS", null));

        var lanes = new List<BoardLanePresentation>();
        foreach ((string key, string title, int? seat) in laneOrder)
        {
            var members = areas.Where(area => laneByArea[area.Id] == key).ToList();
            if (members.Count == 0)
            {
                continue;
            }

            if (key == "other")
            {
                lanes.Add(new BoardLanePresentation(
                    key,
                    title,
                    seat,
                    members.Select(area => area with
                    {
                        HostedBy = area.Host >= 0
                            ? cardTitles.GetValueOrDefault(area.Host, $"CARD {area.Host}")
                            : string.Empty,
                    }).ToArray()));
                continue;
            }

            var memberIds = members.Select(area => area.Id).ToHashSet();
            var children = members
                .Where(area => parents.TryGetValue(area.Id, out int parent)
                    && memberIds.Contains(parent))
                .GroupBy(area => parents[area.Id])
                .ToDictionary(group => group.Key, group => group.ToList());
            var emitted = new HashSet<int>();
            var ordered = new List<BoardAreaPresentation>();

            foreach (BoardAreaPresentation root in members.Where(area =>
                         !parents.TryGetValue(area.Id, out int parent)
                         || !memberIds.Contains(parent)))
            {
                Emit(root, 0, children, emitted, ordered, cardTitles);
            }

            // Defensive fallback for malformed cycles: retain every area once.
            foreach (BoardAreaPresentation remaining in members)
            {
                Emit(remaining, 0, children, emitted, ordered, cardTitles);
            }

            lanes.Add(new BoardLanePresentation(key, title, seat, ordered));
        }

        return lanes;
    }

    private static string ResolveLane(
        int areaId,
        HashSet<int> path,
        Dictionary<int, string> resolved,
        Dictionary<int, int> parents,
        HashSet<int> brokenHosts,
        Dictionary<int, BoardAreaPresentation> areas,
        HashSet<int> seats)
    {
        if (resolved.TryGetValue(areaId, out string? lane))
        {
            return lane;
        }

        if (!path.Add(areaId) || brokenHosts.Contains(areaId))
        {
            return resolved[areaId] = "other";
        }

        BoardAreaPresentation area = areas[areaId];
        lane = parents.TryGetValue(areaId, out int parent)
            ? ResolveLane(parent, path, resolved, parents, brokenHosts, areas, seats)
            : area.Seat < 0
                ? "scenario"
                : seats.Contains(area.Seat) ? $"player-{area.Seat}" : "other";
        path.Remove(areaId);
        return resolved[areaId] = lane;
    }

    private static void Emit(
        BoardAreaPresentation area,
        int depth,
        Dictionary<int, List<BoardAreaPresentation>> children,
        HashSet<int> emitted,
        List<BoardAreaPresentation> ordered,
        Dictionary<int, string> cardTitles)
    {
        if (!emitted.Add(area.Id))
        {
            return;
        }

        ordered.Add(area with
        {
            Depth = depth,
            HostedBy = area.Host >= 0
                ? cardTitles.GetValueOrDefault(area.Host, $"CARD {area.Host}")
                : string.Empty,
        });
        if (!children.TryGetValue(area.Id, out List<BoardAreaPresentation>? nested))
        {
            return;
        }

        foreach (BoardAreaPresentation child in nested)
        {
            Emit(child, depth + 1, children, emitted, ordered, cardTitles);
        }
    }
}

/// <summary>One scenario, player, or fallback board lane.</summary>
public sealed record BoardLanePresentation(
    string Key,
    string Title,
    int? Seat,
    IReadOnlyList<BoardAreaPresentation> Areas);

/// <summary>The seat-order identity needed to arrange player lanes.</summary>
public sealed record BoardPlayerPresentation(int Seat, string Name);
