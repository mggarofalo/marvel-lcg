namespace Marvel.Godot;

/// <summary>One generous, explicit control attached to a visible card.</summary>
internal sealed record CardInteractionControlDescriptor(
    int CardId,
    CardInteractionIntent Intent,
    string Text,
    CardInteractionCue Cue);
