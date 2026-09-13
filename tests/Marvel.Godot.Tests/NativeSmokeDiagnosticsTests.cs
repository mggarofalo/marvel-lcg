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
    public void NativeSmokeWrappersRejectGodotErrorDiagnostics(string relativePath)
    {
        string path = Path.Combine(RepositoryPaths.Root, relativePath);
        string script = File.ReadAllText(path);

        string filter = Path.GetExtension(relativePath) == ".ps1"
            ? "-match \"ERROR:\""
            : "grep -q 'ERROR:'";
        Assert.Contains(filter, script, StringComparison.Ordinal);
    }
}
