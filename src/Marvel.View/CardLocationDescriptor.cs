namespace Marvel.View;

/// <summary>Stable public placement and control information for an addressable card.</summary>
public sealed record CardLocationDescriptor(
    int AreaId,
    string Zone,
    int Controller,
    int EngagedWith);
