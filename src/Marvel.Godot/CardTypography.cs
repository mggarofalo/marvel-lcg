using Godot;

namespace Marvel.Godot;

/// <summary>Pinned card fonts loaded from the assembly without an import or network dependency.</summary>
internal static class CardTypography
{
    private static readonly Dictionary<string, FontFile> Fonts = [];
    internal static Font Title => Load("BarlowCondensed-Bold");
    internal static Font Body => Load("Barlow-Regular");
    internal static Font Bold => Load("Barlow-Bold");
    internal static Font Italic => Load("Barlow-Italic");

    private static FontFile Load(string name)
    {
        if (Fonts.TryGetValue(name, out FontFile? cached)) return cached;
        using Stream stream = typeof(CardTypography).Assembly.GetManifestResourceStream(
            $"Marvel.Godot.Assets.{name}.ttf") ?? throw new InvalidOperationException($"Missing card font {name}");
        var data = new byte[stream.Length];
        stream.ReadExactly(data);
        var font = new FontFile { Data = data };
        Fonts.Add(name, font);
        return font;
    }
}
