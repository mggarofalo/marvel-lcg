using Godot;
using Marvel.Decisions;

namespace Marvel.Godot;

/// <summary>
/// Payment progress as a tracker: one pip per resource the cost needs, filled by the resources selected, and an
/// excess chip when the selection pays more than the cost. The words stay on hover.
/// </summary>
internal static class PaymentProgressTracker
{
    /// <summary>
    /// The pips: the cost's required resources, filled by the selected resources up to that cost, or null when the
    /// engine's assessment does not know the requirement. Resources beyond the cost are the excess chip, not pips.
    /// </summary>
    internal static (int Filled, int Total)? Pips(PaymentProgress progress) => progress.Required is { } required
        ? (Math.Min(progress.GeneratedIcons, required), required)
        : null;

    internal static Control Create(PaymentProgress progress, string summary, InterfaceScale scale)
    {
        Control row = Pips(progress) is { } pips && pips.Total > 0
            ? Tracker.Create("PaymentProgress", new TrackerState(DecisionWording.DraftPaid, pips.Filled, pips.Total,
                progress.ExcessIcons > 0 ? DecisionWording.Excess : null, progress.ExcessIcons, summary), scale)
            : Chip.Create("PaymentProgress", new Wording("", DecisionWording.Selected(progress.GeneratedIcons), summary), null, scale);
        row.TooltipText = summary;
        row.AccessibilityDescription = summary;
        row.SetMeta("progress_text", summary);
        return row;
    }
}
