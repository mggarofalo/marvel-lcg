using Godot;

namespace Marvel.Godot;

/// <summary>Owns one immutable theme per desktop scale while table scenes are active.</summary>
internal static class ClientThemePalette
{
    private static readonly Dictionary<InterfaceScale, Theme> themes = [];
    private static Window? lifetime;
    private static readonly HashSet<Main> owners = [];

    internal static void Retain(Main owner) => owners.Add(owner);

    internal static void ReleaseFor(Main owner)
    {
        if (owners.Remove(owner) && owners.Count == 0) Release();
    }

    internal static Theme For(InterfaceScale scale)
    {
        if (lifetime is null && Engine.GetMainLoop() is SceneTree tree)
        {
            lifetime = tree.Root;
            lifetime.TreeExiting += Release;
        }
        if (!themes.TryGetValue(scale, out Theme? theme))
        {
            theme = ClientTheme.Create(scale);
            themes.Add(scale, theme);
        }
        return theme;
    }

    private static void Release()
    {
        if (lifetime is not null) lifetime.TreeExiting -= Release;
        lifetime = null;
        foreach (Theme theme in themes.Values) theme.Dispose();
        themes.Clear();
    }
}
