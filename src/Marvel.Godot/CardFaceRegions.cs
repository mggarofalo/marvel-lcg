using Godot;

namespace Marvel.Godot;

/// <summary>Stable printed-card slots shared by table cards, searches and inspection.</summary>
internal sealed class CardFaceRegions
{
    internal Rect2 Title { get; }
    internal Rect2 Cost { get; }
    internal Rect2 Kind { get; }
    internal Rect2 Illustration { get; }
    internal Rect2 Traits { get; }
    internal Rect2 Rules { get; }
    internal Rect2 Stats { get; }
    internal Rect2 Tokens { get; }
    internal Rect2 Retaliate { get; }
    internal Rect2 Resources { get; }
    internal bool Full { get; }
    internal float RulesFontSize => (Full ? CardVisualTokens.FullBodySize : CardVisualTokens.CompactBodySize) * Unit;
    internal Rect2 Health { get; }
    internal float Unit { get; }

    internal CardFaceRegions(Vector2 size, CardFaceFeatures features)
    {
        float w = size.X, h = size.Y;
        bool landscape = features.Landscape, full = features.Full;
        Full = full;
        Unit = landscape ? h / 400f : w / 400f;
        float inset = 8 * Unit;
        float header = (full ? 72 : 92) * Unit;
        float costWidth = features.HasPrimary ? 56 * Unit : 0;
        Cost = new Rect2(0, 0, costWidth, header);
        Title = new Rect2(costWidth + inset, 0, w - costWidth - inset * 2, header);
        Kind = new Rect2(inset, header, w - inset * 2, 26 * Unit);
        float bodyY = header + 28 * Unit;
        float railWidth = (full ? 58 : 72) * Unit;
        Stats = new Rect2(inset, bodyY + 32 * Unit, railWidth, h - bodyY - 87 * Unit);
        Illustration = IllustrationBounds(size, features, inset, bodyY);
        Vector2 text = TextOrigin(size, features, inset, bodyY, railWidth);
        float textX = text.X, textY = text.Y;
        float textWidth = w - textX - inset;
        Traits = new Rect2(textX, textY, textWidth, features.HasTraits ? 28 * Unit : 0);
        Tokens = new Rect2(textX, Traits.End.Y, textWidth, features.TokenRows * LiveRowHeight(features));
        Retaliate = new Rect2(textX, Tokens.End.Y, textWidth,
            features.HasRetaliate ? LiveRowHeight(features) : 0);
        float rulesY = Retaliate.End.Y + (features.TokenRows > 0 || features.HasRetaliate ? 4 * Unit : 0);
        Rules = new Rect2(textX, rulesY, textWidth, h - rulesY - 55 * Unit);
        Resources = new Rect2(inset, h - 44 * Unit, w * 0.55f, 36 * Unit);
        Health = new Rect2(w - 114 * Unit, h - 50 * Unit, 106 * Unit, 46 * Unit);
    }

    private Rect2 IllustrationBounds(Vector2 size, CardFaceFeatures features, float inset, float bodyY) =>
        !features.HasArt ? new Rect2(inset, bodyY, 0, 0) : features.Landscape
            ? new Rect2(inset, bodyY, size.X * 0.34f - inset, size.Y - bodyY - 55 * Unit)
            : new Rect2(inset, bodyY, size.X - inset * 2, PortraitIllustrationHeight(size, features));

    private float LiveRowHeight(CardFaceFeatures features) => (features.Full ? 32 : 44) * Unit;

    private float PortraitIllustrationHeight(Vector2 size, CardFaceFeatures features)
    {
        float rowHeight = LiveRowHeight(features);
        float reserved = (features.TokenRows + (features.HasRetaliate ? 1 : 0)) * rowHeight;
        return Math.Max(0, size.Y * 0.32f - reserved);
    }

    private Vector2 TextOrigin(Vector2 size, CardFaceFeatures features, float inset, float bodyY, float railWidth)
    {
        float x = features.HasArt && features.Landscape ? size.X * 0.36f : features.HasStats
            ? inset + railWidth + 8 * Unit : inset;
        float y = features.HasArt && !features.Landscape ? Illustration.End.Y + 8 * Unit : bodyY;
        return new Vector2(x, y);
    }
}
