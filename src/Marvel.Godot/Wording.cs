namespace Marvel.Godot;

/// <summary>How the table states one fact: an icon, a short label, and a tooltip that explains it.</summary>
/// <param name="Icon">The table icon's asset name, or empty for a fact with no icon.</param>
/// <param name="Label">A short label of one to three words.</param>
/// <param name="Tooltip">The full explanation, shown on hover and read by assistive technology.</param>
internal sealed record Wording(string Icon, string Label, string Tooltip);
