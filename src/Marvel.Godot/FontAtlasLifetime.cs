using Godot;

namespace Marvel.Godot;

/// <summary>Releases shared font raster caches before the scene tree's renderer shuts down.</summary>
internal static class FontAtlasLifetime
{
    private static readonly HashSet<FontFile> Fonts = [];
    private static Window? root;

    internal static void Retain(FontFile font)
    {
        if (root is null)
        {
            root = ((SceneTree)Engine.GetMainLoop()).Root;
            root.TreeExited += Release;
        }
        Fonts.Add(font);
    }

    private static void Release()
    {
        root!.TreeExited -= Release;
        root = null;
        // Font bytes remain shared across controls. Their atlas textures belong
        // to this scene tree and must retire while its renderer still exists.
        foreach (FontFile font in Fonts) font.ClearCache();
        Fonts.Clear();
    }
}
