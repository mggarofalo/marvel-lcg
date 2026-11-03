namespace Marvel.Godot;

/// <summary>One part of a draft's summary: its words, and the tracker that shows it on the table when it has one.</summary>
/// <param name="Text">The part in words, for the summary's accessible text and the tracker's tooltip.</param>
/// <param name="Label">The tracker's short label, or null when the part shows as a chip of its words.</param>
/// <param name="Filled">Pips filled: what is staged, selected or paid.</param>
/// <param name="Total">All pips: what can be staged or selected, or what the cost needs.</param>
internal sealed record DraftPart(string Text, string? Label = null, int Filled = 0, int Total = 0);
