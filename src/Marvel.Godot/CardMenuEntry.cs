namespace Marvel.Godot;

/// <summary>A presentation branch or leaf; opening branches never changes a draft.</summary>
internal sealed record CardMenuEntry(string Label, string Detail, CardMenuChoice? Choice,
    IReadOnlyList<CardMenuEntry> Children, bool Disabled = false, int? Preview = null);
