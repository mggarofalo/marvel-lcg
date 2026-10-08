using Godot;

namespace Marvel.Godot;

/// <summary>Loads cached font bytes independently of their semantic typography role.</summary>
internal static class PackagedFonts
{
    private static readonly Dictionary<string, FontFile> Fonts = [];

    internal static FontFile Load(string name)
    {
        if (Fonts.TryGetValue(name, out FontFile? cached))
        {
            FontAtlasLifetime.Retain(cached);
            return cached;
        }
        using Stream stream = typeof(PackagedFonts).Assembly.GetManifestResourceStream(
            $"Marvel.Godot.Assets.{name}.ttf") ?? throw new InvalidOperationException($"Missing packaged font {name}");
        var data = new byte[stream.Length];
        stream.ReadExactly(data);
        var font = new FontFile { Data = data, AllowSystemFallback = false };
        Fonts.Add(name, font);
        FontAtlasLifetime.Retain(font);
        return font;
    }
}
