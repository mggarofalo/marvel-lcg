namespace Marvel.Godot;

/// <summary>Normalizes action copy and treatment across decision entry points.</summary>
internal static class DecisionAffordanceStyle
{
    internal static string Text(
        string action, bool unavailable, bool selected, bool resolving) =>
        resolving
            ? $"✓ {action}  ·  resolving"
            : unavailable
                ? $"— Unavailable  ·  {action}"
                : selected ? $"✓ {action}" : action;

    internal static InteractiveVisualState State(
        bool legal, bool submitting, bool selected, bool resolving) =>
        resolving
            ? InteractiveVisualState.Selected
            : !legal || submitting
                ? InteractiveVisualState.Unavailable
                : selected ? InteractiveVisualState.Selected : InteractiveVisualState.Resting;
}
