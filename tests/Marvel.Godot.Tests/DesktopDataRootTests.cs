using Marvel.Server;
using Marvel.Tests;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
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
            "v0.1.0-dev.0 · engine engine-replay-v2 · protocol 16 · save 4",
            EngineBuildIdentity.Display);
    }

    [Fact]
    public void ScenePinsTheCompiledBuildIdentity()
    {
        string scene = File.ReadAllText(Path.Combine(
            RepositoryPaths.Root, "src", "Marvel.Godot", "Main.tscn"));

        Assert.Contains(EngineBuildIdentity.Display, scene, StringComparison.Ordinal);
    }

    [Fact]
    public void NativePromptHeaderUsesTheStructuredQuestionAndAuthorizedSeat()
    {
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "Mulligan", "Spider-Man resolves mulligans", false, [])
        {
            DisplayQuestion = "Opening hand",
        };
        var world = new WorldDescriptor([new PlayerDescriptor(0, "Spider-Man", false)],
            [], [], Outcome.Unfinished);

        PromptPresentation header = PromptPresentation.From(prompt, world);

        Assert.Equal("Opening hand", header.Heading);
        Assert.Contains("Spider-Man", header.Context, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeMulliganSmokeWaitsForPlayerActionsInsteadOfPromptProse()
    {
        string smoke = string.Join(Environment.NewLine, Directory.EnumerateFiles(Path.Combine(
            RepositoryPaths.Root, "src", "Marvel.Godot", "smoke"), "local_game_smoke*.gd")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText));

        Assert.Contains("Change Form", smoke, StringComparison.Ordinal);
        Assert.Contains("Play Web-Shooter", smoke, StringComparison.Ordinal);
        Assert.DoesNotContain("the seeded mulligan did not reach the player turn", smoke,
            StringComparison.Ordinal);
    }
}
