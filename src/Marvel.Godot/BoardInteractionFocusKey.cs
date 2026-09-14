namespace Marvel.Godot;

/// <summary>Identifies one prompt-authorized card control across a visual refresh.</summary>
internal readonly record struct BoardInteractionFocusKey(
    int CardId,
    CardInteractionIntent Intent);
