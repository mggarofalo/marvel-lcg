namespace Marvel.Godot;

/// <summary>Content-presence facts used only to arrange one card face.</summary>
internal sealed record CardFaceFeatures(bool Landscape, bool HasPrimary, bool HasArt,
    bool HasStats, bool HasTraits, bool Full, int PrintedIconRows)
{
    internal float RulesHeight { get; init; }
    internal float TitleHeight { get; init; }
    internal bool HasProgress { get; init; }
    internal bool HasConsequences { get; init; }
}
