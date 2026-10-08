using Godot;

namespace Marvel.Godot;

/// <summary>Allocates B1 ink, illustration, and paper around measured text at fixed type sizes.</summary>
internal sealed class CardFaceRegions
{
    private readonly CardFaceMetrics metrics;
    internal Rect2 Title { get; private set; }
    internal Rect2 Cost { get; private set; }
    internal Rect2 Kind { get; private set; }
    internal Rect2 Illustration { get; private set; }
    internal Rect2 Traits { get; private set; }
    internal Rect2 Rules { get; private set; }
    internal Rect2 Stats { get; private set; }
    internal Rect2 Tokens { get; private set; }
    internal Rect2 Resources { get; private set; }
    internal Rect2 Health { get; private set; }
    internal bool Full => metrics.Full;
    internal float Unit => metrics.Unit;
    internal float Density => metrics.Density;
    internal float InkEnd { get; private set; }
    internal float RulesFontSize => metrics.RulesFontSize;
    internal float TitleFontSize => metrics.TitleFontSize;

    internal CardFaceRegions(Vector2 size, CardFaceFeatures features)
    {
        float shortSide = features.Landscape ? size.Y : size.X;
        metrics = new(features.Full, shortSide / 400, shortSide / (features.Full ? 400 : 172));
        ArrangeHeader(size, features);
        ArrangeInk(size, features);
        ArrangePaper(size, features);
        ArrangeFooter(size, features);
    }

    private void ArrangeHeader(Vector2 size, CardFaceFeatures features)
    {
        float pad = metrics.Padding;
        float cost = features.HasPrimary ? metrics.Cost : 0;
        Cost = new Rect2(pad, pad, cost, cost);
        float titleX = pad + (features.HasPrimary ? cost + metrics.CostGap : 0);
        float titleHeight = features.TitleHeight > 0 ? features.TitleHeight : metrics.DefaultTitleHeight;
        Title = new Rect2(titleX, pad, size.X - titleX - pad, titleHeight);
        Kind = new Rect2(pad, Math.Max(Title.End.Y, Cost.End.Y) + 2 * Density,
            size.X - 2 * pad - metrics.AspectWidth, metrics.KindHeight);
    }

    private void ArrangeInk(Vector2 size, CardFaceFeatures features)
    {
        float rail = features.HasStats ? metrics.Rail(features.Landscape, features.HasConsequences) : 0;
        if (features.Landscape) rail = Math.Max(rail, features.PrintedIconRows * metrics.RowHeight);
        float minimum = Kind.End.Y + rail + metrics.Gap;
        float desired = features.HasArt ? size.Y * metrics.ArtFraction : minimum;
        InkEnd = Math.Max(minimum, Math.Min(desired, AvailableInk(size.Y, features)));
        float tokenWidth = features.Landscape && features.PrintedIconRows > 0 ? metrics.TokenWidth : 0;
        Stats = new Rect2(metrics.Padding, InkEnd - rail, size.X - 2 * metrics.Padding - tokenWidth, rail);
        Illustration = features.HasArt
            ? new Rect2(size.X * 0.56f, Kind.End.Y, size.X * 0.44f, Math.Max(0, Stats.Position.Y - Kind.End.Y))
            : new Rect2(metrics.Padding, Kind.End.Y, 0, 0);
    }

    private float PrintedIconHeight(CardFaceFeatures features) =>
        features.Landscape ? 0 : features.PrintedIconRows * metrics.RowHeight;

    private float AvailableInk(float height, CardFaceFeatures features) =>
        height - metrics.Footer(features.Landscape) - (features.HasTraits ? metrics.TraitHeight : 0)
        - PrintedIconHeight(features) - features.RulesHeight - metrics.Gap * (PrintedIconHeight(features) > 0 ? 3 : 2);

    private void ArrangePaper(Vector2 size, CardFaceFeatures features)
    {
        float pad = metrics.Padding, width = size.X - 2 * pad;
        Traits = new Rect2(pad, InkEnd + metrics.Gap, width, features.HasTraits ? metrics.TraitHeight : 0);
        float tokenHeight = features.PrintedIconRows * metrics.RowHeight;
        Tokens = features.Landscape
            ? new Rect2(Stats.End.X, Stats.Position.Y, metrics.TokenWidth, tokenHeight)
            : new Rect2(pad, Traits.End.Y, width, tokenHeight);
        float rulesY = (features.Landscape ? Traits.End.Y : Tokens.End.Y)
            + (PrintedIconHeight(features) > 0 ? metrics.Gap : 0);
        Rules = new Rect2(pad, rulesY, width,
            Math.Max(0, size.Y - metrics.Footer(features.Landscape) - metrics.Gap - rulesY));
    }

    private void ArrangeFooter(Vector2 size, CardFaceFeatures features)
    {
        float footer = metrics.Footer(features.Landscape);
        Health = new Rect2(size.X - metrics.Padding - metrics.HealthWidth, size.Y - footer,
            metrics.HealthWidth, footer - metrics.Gap);
        float resourceWidth = features.HasProgress ? Health.Position.X - metrics.Padding - metrics.Gap
            : size.X - 2 * metrics.Padding;
        Resources = new Rect2(metrics.Padding, size.Y - footer, resourceWidth, footer - metrics.Gap);
    }
}
