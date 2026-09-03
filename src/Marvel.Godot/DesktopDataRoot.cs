using Godot;

namespace Marvel.Godot;

/// <summary>Locates packaged runtime datasets without making them Godot resources.</summary>
internal static class DesktopDataRoot
{
    /// <summary>Returns the data root for the current editor or exported process.</summary>
    public static string Current()
    {
        bool macOS = OS.HasFeature("macos");
        string executablePath = OS.GetExecutablePath();
        string editorDataRoot = ProjectSettings.GlobalizePath("res://../..");
        string packagedDataRoot = Resolve(false, macOS, executablePath, editorDataRoot);
        bool developerProcess = Engine.IsEditorHint()
            || !File.Exists(Path.Combine(packagedDataRoot, "release-manifest.json"));
        return Resolve(developerProcess, macOS, executablePath, editorDataRoot);
    }

    internal static string Resolve(
        bool editor,
        bool macOS,
        string executablePath,
        string editorDataRoot)
    {
        if (editor)
        {
            return Path.GetFullPath(editorDataRoot);
        }

        string executableDirectory = Path.GetDirectoryName(
            Path.GetFullPath(executablePath))
            ?? throw new InvalidOperationException("desktop executable has no directory");
        return macOS
            ? Path.GetFullPath(Path.Combine(executableDirectory, "..", "Resources"))
            : executableDirectory;
    }
}
