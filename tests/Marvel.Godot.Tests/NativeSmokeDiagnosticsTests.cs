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
