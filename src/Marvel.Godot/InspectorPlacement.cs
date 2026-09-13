namespace Marvel.Godot;

/// <summary>A viewport-safe inspector origin and its explicit source relationship.</summary>
public sealed record InspectorPlacement(int X, int Y, InspectorAttachment Attachment)
{
    /// <summary>Whether the inspector remains directly attached to its source.</summary>
    public bool IsAttached => Attachment != InspectorAttachment.ViewportFallback;
}
