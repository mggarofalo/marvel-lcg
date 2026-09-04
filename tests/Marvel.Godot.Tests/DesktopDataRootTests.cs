using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DesktopDataRootTests
{
    [Fact]
    public void EditorUsesTheRepositoryDataRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "marvel", "repository");

        string actual = DesktopDataRoot.Resolve(
            editor: true,
            macOS: true,
            executablePath: "/Applications/Marvel Champions.app/Contents/MacOS/Marvel Champions",
            editorDataRoot: root);

        Assert.Equal(Path.GetFullPath(root), actual);
    }

    [Fact]
    public void ExportedMacUsesTheBundleResourcesDirectory()
    {
        string bundle = Path.Combine(
            Path.GetTempPath(),
            "Marvel Champions.app",
            "Contents");
        string executable = Path.Combine(bundle, "MacOS", "Marvel Champions");

        string actual = DesktopDataRoot.Resolve(
            editor: false,
            macOS: true,
            executablePath: executable,
            editorDataRoot: "/ignored");

        Assert.Equal(Path.Combine(bundle, "Resources"), actual);
    }

    [Fact]
    public void ExportedWindowsUsesTheExecutableDirectory()
    {
        string executable = Path.Combine("C:\\", "Program Files", "Marvel", "Marvel Champions.exe");

        string actual = DesktopDataRoot.Resolve(
            editor: false,
            macOS: false,
            executablePath: executable,
            editorDataRoot: "/ignored");

        Assert.Equal(Path.GetDirectoryName(Path.GetFullPath(executable)), actual);
    }

    [Fact]
    public void DesktopBuildIdentityIsVisibleAndBounded()
    {
        Assert.Equal("0.1.0-dev.0", EngineBuildIdentity.ProductVersion);
        Assert.Equal("local", EngineBuildIdentity.Commit);
        Assert.Equal(
            "v0.1.0-dev.0 · engine engine-replay-v1 · protocol 12 · save 2",
            EngineBuildIdentity.Display);
    }
}
