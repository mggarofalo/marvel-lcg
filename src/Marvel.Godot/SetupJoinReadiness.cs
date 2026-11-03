using Godot;

namespace Marvel.Godot;

/// <summary>The join form's readiness: one pip per input the join needs, the missing ones named on hover.</summary>
internal static class SetupJoinReadiness
{
    private const string Name = "JoinReadiness";

    /// <summary>Which of the join's inputs hold a value, in <see cref="SetupWording.JoinInputs"/> order.</summary>
    internal static bool[] Provided(string endpoint, string gameLabel, string invitation) =>
        [endpoint.Length > 0, gameLabel.Length > 0, invitation.Length > 0];

    /// <summary>What the tracker shows for the join's inputs.</summary>
    internal static TrackerState State(IReadOnlyList<bool> provided) =>
        new("Join", provided.Count(value => value), provided.Count, null, 0, SetupWording.JoinTooltip(provided));

    /// <summary>Replaces the tracker above the Join button with one for the current inputs.</summary>
    internal static void Refresh(Main main, Control join)
    {
        if (join.GetNodeOrNull<Control>(Name) is { } prior)
        {
            join.RemoveChild(prior);
            prior.QueueFree();
        }
        HBoxContainer tracker = Tracker.Create(Name,
            State(Provided(main.endpoint.Text, main.gameId.Text, main.invitation.Text)), main.interfaceScale);
        join.AddChild(tracker);
        join.MoveChild(tracker, main.join.GetIndex());
    }
}
