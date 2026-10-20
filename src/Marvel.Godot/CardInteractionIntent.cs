namespace Marvel.Godot;

/// <summary>The deliberate card-attached control a player invoked.</summary>
internal enum CardInteractionIntent
{
    Action,
    ContextMenu,
    Target,
    Generator,
    Cost,
    Submit,
    Decline,
}
