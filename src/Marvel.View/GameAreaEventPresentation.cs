using Marvel.Rules.Events;
using Marvel.Rules.Play;

using static Marvel.View.EventSubjectDescriptions;

namespace Marvel.View;

/// <summary>Names game-area topology changes by the seats involved rather than by area number.</summary>
/// <remarks>
/// The wording is our choice. Each event carries the area's membership at the moment it
/// changed (<see cref="PlayAreaJoined.Members"/>), so a later join in the same response
/// cannot rename an earlier one. An event without that evidence names no one.
/// </remarks>
internal static class GameAreaEventPresentation
{
    internal static string Joined(PlayAreaJoined joined, WorldDescriptor world)
    {
        string mover = PlayArea(joined.PlayArea, world);
        if (joined.Members is not { } members) return $"{mover} joined a game area.";
        if (members.Contains(-1) && joined.PlayArea >= 0) return $"{mover} joined the villain's game area.";
        string[] others = Others(members, joined.PlayArea, world);
        return others.Length == 0
            ? $"{mover} formed its own game area."
            : $"{mover} joined {GameAreaFacts.Names(others)}'s game area.";
    }

    internal static string Detached(PlayAreaDetached detached, WorldDescriptor world)
    {
        if (detached.PlayArea < 0)
            return "The villain play area left the shared game area; the players are now in separate game areas.";
        string mover = PlayArea(detached.PlayArea, world);
        string[] others = detached.Members is { } members ? Others(members, detached.PlayArea, world) : [];
        return others.Length == 0
            ? $"{mover} left its game area."
            : $"{mover} left {GameAreaFacts.Names(others)}'s game area.";
    }

    private static string[] Others(IReadOnlyList<int> members, int mover, WorldDescriptor world) =>
        [.. members.Where(seat => seat >= 0 && seat != mover).Select(seat => Player(seat, world))];
}
