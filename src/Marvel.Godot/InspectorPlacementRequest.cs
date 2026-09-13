namespace Marvel.Godot;

/// <summary>Geometry needed to attach one inspector without consulting a Godot control tree.</summary>
public sealed record InspectorPlacementRequest(
    int ViewportWidth,
    int ViewportHeight,
    int SourceX,
    int SourceY,
    int SourceWidth,
    int SourceHeight,
    int PanelWidth,
    int PanelHeight,
    bool HandSource,
    int Margin = 12,
    int Gap = 12);
