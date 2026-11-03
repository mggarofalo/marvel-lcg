using Godot;

namespace Marvel.Godot;

/// <summary>
/// The round and phase at a point in the timeline, as a tracker: the round, one pip per phase of the round (player,
/// then villain) filled through the current one, and the phase as a chip. Read from the engine's phase-boundary
/// events; unknown, and hidden, when the timeline holds no boundary at or before that point.
/// </summary>
internal static class TimelineRoundTracker
{
    // rr:game-round — a round is a player phase and then a villain phase.
    private static readonly string[] Phases = ["Player", "Villain"];

    /// <summary>The round and phase established by the last phase boundary at or before <paramref name="index"/>.</summary>
    internal static (int Round, string Phase)? At(IReadOnlyList<TimelineOccurrence> entries, int index)
    {
        for (int at = Math.Min(index, entries.Count - 1); at >= 0; at--)
            if (entries[at].Event.Activity is { PhaseBoundary: true, Round: { } round, Phase.Length: > 0 } activity)
                return (round, activity.Phase);
        return null;
    }

    /// <summary>The tracker's state: "Round N" with the phase's pips filled, worded on hover.</summary>
    internal static TrackerState State(int round, string phase)
    {
        int position = Array.IndexOf(Phases, phase) + 1;
        return new TrackerState($"Round {round}", position, Phases.Length, null, 0,
            $"Round {round}, {phase.ToLowerInvariant()} phase.")
        {
            Numbered = false,
        };
    }

    /// <summary>Replaces <paramref name="host"/>'s contents with the tracker and phase chip, or hides it when unknown.</summary>
    internal static void Present(HBoxContainer host, (int Round, string Phase)? point, InterfaceScale scale)
    {
        foreach (Node child in host.GetChildren())
        {
            host.RemoveChild(child);
            child.QueueFree();
        }
        host.Visible = point is not null;
        if (point is not { } known) return;
        TrackerState state = State(known.Round, known.Phase);
        host.AddChild(Tracker.Create("RoundTracker", state, scale));
        host.AddChild(Chip.Create("PhaseChip", new Wording("", $"{known.Phase} phase", state.Tooltip), null, scale));
        host.SetMeta("round", known.Round);
        host.SetMeta("phase", known.Phase);
    }
}
