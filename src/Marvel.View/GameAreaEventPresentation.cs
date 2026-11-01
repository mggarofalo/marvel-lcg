using Marvel.Rules.Events;

using static Marvel.View.EventSubjectDescriptions;

namespace Marvel.View;

/// <summary>Names game-area topology changes by the seats involved rather than by area number.</summary>
/// <remarks>
/// The wording is our choice. The response snapshot supplies the seats in each named
/// game area; an area the snapshot does not list is described without naming anyone.
/// </remarks>
internal static class GameAreaEventPresentation
{
    internal static string Joined(PlayAreaJoined joined, WorldDescriptor world)
    {
        string mover = PlayArea(joined.PlayArea, world);
        var area = world.GameAreas.FirstOrDefault(item => item.Id == joined.GameArea);
        if (area is null) return $"{mover} joined a game area.";
        if (area.Central) return $"{mover} joined the central game area.";
        string[] others = Others(area, joined.PlayArea, world);
        return others.Length == 0
            ? $"{mover} formed its own game area."
            : $"{mover} joined {Names(others)}'s game area.";
    }

    internal static string Detached(PlayAreaDetached detached, WorldDescriptor world)
    {
        if (detached.PlayArea < 0)
            return "The villain play area left the shared game area; the players are now in separate game areas.";
        string mover = PlayArea(detached.PlayArea, world);
        var area = world.GameAreas.FirstOrDefault(item => item.Id == detached.GameArea);
        string[] others = area is null ? [] : Others(area, detached.PlayArea, world);
        return others.Length == 0
            ? $"{mover} left its game area."
            : $"{mover} left {Names(others)}'s game area.";
    }

    private static string[] Others(GameAreaDescriptor area, int mover, WorldDescriptor world) =>
        [.. area.PlayAreas.Where(seat => seat >= 0 && seat != mover).Select(seat => Player(seat, world))];

    private static string Names(string[] names) => names.Length == 1
        ? names[0]
        : $"{string.Join(", ", names.Take(names.Length - 1))} and {names[^1]}";
}
