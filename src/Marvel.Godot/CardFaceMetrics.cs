namespace Marvel.Godot;

/// <summary>One density profile for native card regions, independent of content and nodes.</summary>
internal sealed record CardFaceMetrics(bool Full, float Unit, float Density)
{
    internal float Padding => (Full ? 22 : 7) * Density;
    internal float Cost => (Full ? 48 : 27) * Density;
    internal float CostGap => (Full ? 12 : 4) * Density;
    internal float DefaultTitleHeight => (Full ? 80 : 42) * Density;
    internal float KindHeight => (Full ? 20 : 12) * Density;
    internal float AspectWidth => (Full ? 26 : 14) * Density;
    internal float TraitHeight => (Full ? 22 : 13) * Density;
    internal float RowHeight => (Full ? 32 : 44) * Unit;
    internal float Gap => (Full ? 12 : 4) * Density;
    internal float TokenWidth => (Full ? 120 : 66) * Density;
    internal float HealthWidth => (Full ? 150 : 74) * Density;
    internal float ArtFraction => Full ? 0.59f : 0.52f;
    internal float RulesFontSize => (Full ? CardVisualTokens.FullBodySize : CardVisualTokens.CompactBodySize) * Density;
    internal float TitleFontSize => (Full ? CardVisualTokens.FullTitleSize : CardVisualTokens.CompactTitleSize) * Density;

    internal float Rail(bool landscape, bool consequences) =>
        (Full ? (landscape ? 44 : 66) : landscape ? 24 : consequences ? 29 : 25) * Density;

    internal float Footer(bool landscape) =>
        (landscape ? (Full ? 48 : 23) : (Full ? 56 : 26)) * Density;
}
