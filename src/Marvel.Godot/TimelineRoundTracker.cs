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

    /// <summary>
    /// The round and phase established by the last phase boundary at or before <paramref name="index"/>; unknown when
    /// a restored summary comes first, since the boundaries it stands for are not in the timeline.
    /// </summary>
    internal static (int Round, string Phase)? At(IReadOnlyList<TimelineOccurrence> entries, int index)
    {
        for (int at = Math.Min(index, entries.Count - 1); at >= 0; at--)
        {
            if (entries[at].Restored) return null;
            if (entries[at].Event.Activity is { PhaseBoundary: true, Round: { } round, Phase.Length: > 0 } activity)
                return (round, activity.Phase);
        }
        return null;
    }

    /// <summary>
    /// The timeline position the tracker reads: the selection, or while following, the beat playing now, else the
    /// newest entry. Null when the playing beat is not in the archive, so the shown round stays until playback settles.
    /// </summary>
    internal static int? ShownIndex(IReadOnlyList<TimelineOccurrence> entries, bool following, int selected,
        Marvel.View.EventPresentation? active)
    {
        if (!following) return selected;
        if (active is null) return entries.Count - 1;
        for (int at = entries.Count - 1; at >= 0; at--)
            if (Equals(entries[at].Event, active)) return at;
        return null;
    }

    /// <summary>
    /// Which parts fit in <paramref name="width"/>: the tracker and phase chip, then the tracker alone, then neither.
    /// <paramref name="minimum"/> measures the timeline's minimum width with the phase chip and tracker shown or not.
    /// </summary>
    internal static (bool Phase, bool Tracker) Yield(Func<bool, bool, float> minimum, float width) =>
        minimum(true, true) <= width ? (true, true) : minimum(false, true) <= width ? (false, true) : (false, false);

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
