using System.Runtime.CompilerServices;

namespace Marvel.Godot.Tests;

/// <summary>Isolates embedded composition before any test can initialize its process host.</summary>
internal static class DesktopTestStorage
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Repository choice: every test process owns temporary saves, regardless of inherited developer configuration.
        Environment.SetEnvironmentVariable("MARVEL_SAVE_DIRECTORY",
            Path.Combine(Path.GetTempPath(), "marvel-desktop-tests", Guid.NewGuid().ToString("N")));
    }
}
