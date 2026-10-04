namespace Marvel.Godot;

/// <summary>One compact, explicit control attached to a visible card.</summary>
internal sealed record CardInteractionControlDescriptor(
    int CardId,
    CardInteractionIntent Intent,
    string Text,
    CardInteractionCue Cue,
    int? Option = null,
    string? Description = null);
