namespace Marvel.Godot;

/// <summary>The measured heights of a column's text, in pixels at the current reading scale.</summary>
/// <param name="Heading">The wrapped heading.</param>
/// <param name="Markers">The wrapped marker words, or zero when there are none.</param>
/// <param name="Details">The wrapped explanation, or zero when there is none.</param>
/// <param name="Line">One caption line.</param>
internal sealed record GameAreaColumnText(float Heading, float Markers, float Details, float Line);
