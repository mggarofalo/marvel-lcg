using Marvel.Tests;
using Xunit;

namespace Marvel.Release.Tests;

public sealed class ContinuousIntegrationPolicyTests
{
    [Fact]
    public void PullRequestsSeparateManagedAndPlatformAssurance()
    {
        string workflow = Read(".github", "workflows", "ci.yml");

        Assert.Contains("managed:\n    name: Managed correctness", workflow,
            StringComparison.Ordinal);
        Assert.Contains("platform:\n    name: Native integration (${{ matrix.os }})", workflow,
            StringComparison.Ordinal);
        Assert.Contains("github.event_name == 'pull_request' && !inputs.exhaustive", workflow,
            StringComparison.Ordinal);
        Assert.Contains("godot-smoke.sh \"$GODOT_BIN\" --representative", workflow,
            StringComparison.Ordinal);
        Assert.Contains("godot-smoke.ps1 -GodotBin $env:GODOT_BIN -Representative", workflow,
            StringComparison.Ordinal);
        Assert.Contains("The supported Linux server container hosts a restricted game", workflow,
            StringComparison.Ordinal);
        Assert.Contains("retention-days: 7", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void MasterAndReleaseRetainExhaustiveCrossPlatformCertification()
    {
        string workflow = Read(".github", "workflows", "ci.yml");
        string release = Read(".github", "workflows", "release-desktop.yml");

        Assert.Contains("github.event_name != 'pull_request' || inputs.exhaustive", workflow,
            StringComparison.Ordinal);
        Assert.Contains("The local viewport and scale matrix passes (Linux)", workflow,
            StringComparison.Ordinal);
        Assert.Contains("The local viewport and scale matrix passes (Windows)", workflow,
            StringComparison.Ordinal);
        Assert.Contains("uses: ./.github/workflows/ci.yml\n    with:\n      exhaustive: true",
            release, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeSmokeDefaultsToTheExhaustiveProfile()
    {
        string bash = Read("tools", "godot-smoke.sh");
        string powershell = Read("tools", "godot-smoke.ps1");

        Assert.Contains("profile=${2:---exhaustive}", bash, StringComparison.Ordinal);
        Assert.Contains("viewports=(1920x1080)", bash, StringComparison.Ordinal);
        Assert.DoesNotContain("1040x680", bash, StringComparison.Ordinal);
        Assert.DoesNotContain("1280x720", bash, StringComparison.Ordinal);
        Assert.DoesNotContain("1600x900", bash, StringComparison.Ordinal);
        Assert.Contains("[switch]$Representative", powershell, StringComparison.Ordinal);
        Assert.Contains("@(\"1920x1080\")", powershell, StringComparison.Ordinal);
        Assert.DoesNotContain("1040x680", powershell, StringComparison.Ordinal);
        Assert.DoesNotContain("1280x720", powershell, StringComparison.Ordinal);
        Assert.DoesNotContain("1600x900", powershell, StringComparison.Ordinal);
    }

    [Fact]
    public void VisualQaCertifiesOnlyTheSupportedDesktopViewport()
    {
        string bash = Read("tools", "godot-visual-qa.sh");
        string powershell = Read("tools", "godot-visual-qa.ps1");

        Assert.Contains("for viewport in 1920x1080", bash, StringComparison.Ordinal);
        Assert.DoesNotContain("1280x720", bash, StringComparison.Ordinal);
        Assert.Contains("foreach ($viewport in @(\"1920x1080\"))", powershell,
            StringComparison.Ordinal);
        Assert.DoesNotContain("1280x720", powershell, StringComparison.Ordinal);
    }

    private static string Read(params string[] path) =>
        File.ReadAllText(Path.Combine([RepositoryPaths.Root, .. path]))
            .ReplaceLineEndings("\n");
}
