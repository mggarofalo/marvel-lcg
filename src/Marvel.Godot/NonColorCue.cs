namespace Marvel.Godot;

/// <summary>Cues that continue to communicate state without color perception.</summary>
[Flags]
public enum NonColorCue
{
    None = 0,
    Raised = 1 << 0,
    FocusRing = 1 << 1,
    LegalMarker = 1 << 2,
    Checkmark = 1 << 3,
    Pressed = 1 << 4,
    Disabled = 1 << 5,
    WarningIcon = 1 << 6,
}
