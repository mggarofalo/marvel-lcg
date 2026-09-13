using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class NativeSmokeDiagnosticsTests
{
    [Theory]
    [InlineData("tools/godot-smoke.ps1")]
    [InlineData("tools/godot-smoke.sh")]
    [InlineData("tools/godot-hosted-multiplayer-smoke.ps1")]
    [InlineData("tools/godot-hosted-multiplayer-smoke.sh")]
    [InlineData("tools/godot-visual-qa.ps1")]
    [InlineData("tools/godot-visual-qa.sh")]
    public void NativeSmokeWrappersUseTheSharedErrorContract(string relativePath)
    {
        string path = Path.Combine(RepositoryPaths.Root, relativePath);
        string script = File.ReadAllText(path);

        string contract = Path.GetExtension(relativePath) == ".ps1"
            ? "godot-smoke-diagnostics.ps1"
            : "godot-smoke-diagnostics.sh";
        Assert.Contains(contract, script, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("tools/godot-smoke.ps1")]
    [InlineData("tools/godot-smoke.sh")]
    [InlineData("tools/godot-hosted-multiplayer-smoke.ps1")]
    [InlineData("tools/godot-hosted-multiplayer-smoke.sh")]
    [InlineData("tools/godot-visual-qa.ps1")]
    [InlineData("tools/godot-visual-qa.sh")]
    [InlineData("tools/windows-portable-install-smoke.ps1")]
    [InlineData("tools/windows-community-install-smoke.ps1")]
    [InlineData("tools/macos-community-install-smoke.sh")]
    public void NativeSmokeWrappersForceGodotsDummyAudioDriver(string relativePath)
    {
        string script = File.ReadAllText(Path.Combine(RepositoryPaths.Root, relativePath));

        Assert.Contains("--audio-driver", script, StringComparison.Ordinal);
        Assert.Contains("Dummy", script, StringComparison.Ordinal);
    }

    [Fact]
    public void HostedSmokeUsesUntransformedViewportCoordinatesAtWindowsDpi()
    {
        string script = File.ReadAllText(Path.Combine(
            RepositoryPaths.Root, "src", "Marvel.Godot", "smoke",
            "hosted_multiplayer_smoke_support.gd"));

        Assert.Contains("func _embedder_point(viewport: Viewport, local_point: Vector2)", script, StringComparison.Ordinal);
        Assert.Contains("return viewport.get_final_transform() * local_point", script, StringComparison.Ordinal);
        Assert.Contains("control.grab_focus()", script, StringComparison.Ordinal);
        Assert.Contains("scroll.ensure_control_visible(control)", script, StringComparison.Ordinal);
        Assert.Contains("viewport.push_input(move)", script, StringComparison.Ordinal);
        Assert.Contains("viewport.push_input(press)", script, StringComparison.Ordinal);
        Assert.Contains("viewport.push_input(release)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("push_input(move, true)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("push_input(press, true)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("push_input(release, true)", script, StringComparison.Ordinal);
    }

    [Fact]
    public void PowerShellErrorContractMakesGodotErrorAProcessFailure()
    {
        string helper = Path.Combine(RepositoryPaths.Root, "tools/godot-smoke-diagnostics.ps1");
        var start = new System.Diagnostics.ProcessStartInfo(
            "pwsh",
            $"-NoProfile -Command \". '{helper}'; if (Test-GodotSmokeDiagnostics @('INFO', 'ERROR: stale control')) {{ exit 1 }}; exit 0\"")
        {
            UseShellExecute = false,
        };
        using var process = System.Diagnostics.Process.Start(start)!;
        process.WaitForExit();

        Assert.Equal(1, process.ExitCode);
    }
}
