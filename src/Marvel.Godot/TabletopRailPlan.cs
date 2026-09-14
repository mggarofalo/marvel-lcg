using Marvel.View;

namespace Marvel.Godot;

/// <summary>Assigns every projected non-hand area to one stable desktop tabletop rail.</summary>
internal sealed record TabletopRailPlan(
    IReadOnlyList<BoardAreaPresentation> FarLive,
    IReadOnlyList<BoardAreaPresentation> FarShelf,
    IReadOnlyList<BoardAreaPresentation> NearLive,
    IReadOnlyList<BoardAreaPresentation> NearShelf)
{
    internal static TabletopRailPlan Create(
        IReadOnlyList<BoardLanePresentation> lanes,
        int expandedSeat)
    {
        ArgumentNullException.ThrowIfNull(lanes);
        BoardLanePresentation? scenario = lanes.FirstOrDefault(lane => lane.Key == "scenario");
        BoardLanePresentation? player = lanes.FirstOrDefault(lane => lane.Seat == expandedSeat);
        BoardAreaPresentation[] far = [.. (scenario?.Areas ?? [])
            .Where(area => area.Zone != "HandsArea")];
        BoardAreaPresentation[] other = [.. lanes.Where(lane => lane.Key == "other")
            .SelectMany(lane => lane.Areas).Where(area => area.Zone != "HandsArea")];
        BoardAreaPresentation[] near = [.. (player?.Areas ?? [])
            .Where(area => area.Zone != "HandsArea")];
        return new TabletopRailPlan(
            Live(far), [.. Shelf(far).Concat(other)], Live(near), Shelf(near));
    }

    private static BoardAreaPresentation[] Live(IEnumerable<BoardAreaPresentation> areas) => [.. areas
        .Where(area => area.Prominence == BoardAreaProminence.Live)];

    private static BoardAreaPresentation[] Shelf(IEnumerable<BoardAreaPresentation> areas) => [.. areas
        .Where(area => area.Prominence != BoardAreaProminence.Live)];
}
