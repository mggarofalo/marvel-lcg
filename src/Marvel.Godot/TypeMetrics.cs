namespace Marvel.Godot;

/// <summary>Font sizes in logical pixels for one supported interface scale.</summary>
public sealed record TypeMetrics(
    int DisplayTitle,
    int Heading,
    int Body,
    int Caption,
    int Eyebrow);
