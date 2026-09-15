namespace Marvel.Godot;

/// <summary>Prompt-authorized interaction marks rendered independently of card color.</summary>
[Flags]
internal enum CardInteractionCue
{
    None = 0,
    OfferedAction = 1,
    LegalTarget = 2,
    SelectedTarget = 4,
    LegalGenerator = 8,
    SelectedGenerator = 16,
    Unavailable = 32,
    DestructiveChoice = 64,
    SelectedDestructiveChoice = 128,
}
