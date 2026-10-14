namespace Marvel.Godot;

/// <summary>Original vector pointer artwork and its physical click anchors.</summary>
internal static class TableCursorArtwork
{
    internal static IReadOnlyList<string> Names { get; } =
        ["arrow", "point", "grab", "drag", "drop", "forbidden", "text"];

    internal static int PixelSize(InterfaceScale scale)
    {
        _ = VisualSystem.ScalePercent(scale);
        // Product choice: preserve a precise 32px pointer below standard scale.
        return Math.Max(32, (int)Math.Ceiling((int)scale * 0.32 / 2) * 2);
    }

    internal static (float X, float Y) Hotspot(string name, int pixels)
    {
        (float x, float y) = name switch
        {
            "arrow" => (3, 2),
            "point" => (12, 3),
            "grab" or "drag" or "drop" or "forbidden" => (13, 12),
            "text" => (16, 16),
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
        return (x * pixels / 32, y * pixels / 32);
    }

    internal static string Svg(string name)
    {
        if (!Names.Contains(name)) throw new ArgumentOutOfRangeException(nameof(name));
        using Stream stream = typeof(TableCursorArtwork).Assembly.GetManifestResourceStream(
            $"Marvel.Godot.Assets.Cursors.{name}.svg")
            ?? throw new InvalidOperationException($"Missing cursor artwork: {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
