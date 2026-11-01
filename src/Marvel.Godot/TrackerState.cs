namespace Marvel.Godot;

/// <summary>What a <see cref="Tracker"/> shows: a label, pips and a counted fact.</summary>
/// <param name="Label">What is tracked, in a few words.</param>
/// <param name="Filled">Pips drawn filled; they come first.</param>
/// <param name="Total">All pips, filled and hollow.</param>
/// <param name="Counter">The counted fact beside the pips, or null for none.</param>
/// <param name="Count">The counted fact's number.</param>
/// <param name="Tooltip">The full explanation of the tracked progress.</param>
internal sealed record TrackerState(string Label, int Filled, int Total, Wording? Counter, int Count, string Tooltip);
