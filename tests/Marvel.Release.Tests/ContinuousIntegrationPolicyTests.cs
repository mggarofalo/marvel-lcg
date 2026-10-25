using Marvel.Tests;
using Xunit;

namespace Marvel.Release.Tests;

public sealed class ContinuousIntegrationPolicyTests
{
    [Fact]
    public void RoutineRunsSeparateManagedAndPlatformAssurance()
    {
        string workflow = Read(".github", "workflows", "ci.yml");

        Assert.Contains("managed:\n    name: Managed correctness", workflow,
            StringComparison.Ordinal);
        Assert.Contains("platform:\n    name: Native integration (${{ matrix.os }})", workflow,
            StringComparison.Ordinal);
        Assert.Contains("if: ${{ !inputs.exhaustive && needs.changes.outputs.run == 'true' }}", workflow,
            StringComparison.Ordinal);
        Assert.Contains("platform-gates:\n    name: Platform gates (${{ matrix.os }})", workflow,
            StringComparison.Ordinal);
        Assert.Contains("NATIVE_PROFILE: ${{ inputs.exhaustive && 'exhaustive' || 'representative' }}", workflow,
            StringComparison.Ordinal);
        Assert.Contains("The supported Linux server container hosts a restricted game", workflow,
            StringComparison.Ordinal);
        Assert.Contains("retention-days: 7", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitCertificationAndReleaseRetainExhaustiveCrossPlatformChecks()
    {
        string workflow = Read(".github", "workflows", "ci.yml");
        string release = Read(".github", "workflows", "release-desktop.yml");

        Assert.Contains("if: ${{ inputs.exhaustive }}", workflow,
            StringComparison.Ordinal);
        Assert.Contains("The selected native profile completes its exact shard", workflow,
            StringComparison.Ordinal);
        Assert.Contains("uses: ./.github/workflows/ci.yml\n    with:\n      exhaustive: true",
            release, StringComparison.Ordinal);
    }

    [Fact]
    public void RequiredNativeChecksDependOnEveryPlatformGateAndIsolatedShard()
    {
        string workflow = Read(".github", "workflows", "ci.yml");
        string journeys = workflow.Split("  native-journeys:\n", StringSplitOptions.None)[1]
            .Split("  platform:\n", StringSplitOptions.None)[0];
        string required = workflow.Split("  platform:\n", StringSplitOptions.None)[1]
            .Split("  desktop-package:\n", StringSplitOptions.None)[0];

        Assert.Contains("os: [windows-latest, ubuntu-latest]\n        shard: [0, 1]", journeys, StringComparison.Ordinal);
        Assert.Contains("-ShardCount 2 -ShardIndex ${{ matrix.shard }}", journeys, StringComparison.Ordinal);
        Assert.Contains("native-evidence-${{ matrix.os }}-shard-${{ matrix.shard }}", journeys, StringComparison.Ordinal);
        Assert.DoesNotContain("Marvel.UnitTests", journeys, StringComparison.Ordinal);
        Assert.DoesNotContain("Marvel.Acceptance.Tests", journeys, StringComparison.Ordinal);
        Assert.Contains("if: ${{ always() }}",
            required, StringComparison.Ordinal);
        Assert.Contains("needs: [changes, platform-gates, native-journeys]", required, StringComparison.Ordinal);
        Assert.Contains("needs.changes.result != 'success' || (needs.changes.outputs.run != 'true' && needs.changes.outputs.run != 'false')",
            required, StringComparison.Ordinal);
        Assert.Contains("if: ${{ needs.changes.result == 'success' && needs.changes.outputs.run == 'false' }}",
            required, StringComparison.Ordinal);
        Assert.Contains("- name: Require every platform gate and native shard to succeed\n        if: ${{ needs.changes.outputs.run == 'true' }}",
            required, StringComparison.Ordinal);
        Assert.Contains("PLATFORM_GATES_RESULT: ${{ needs.platform-gates.result }}", required, StringComparison.Ordinal);
        Assert.Contains("NATIVE_JOURNEYS_RESULT: ${{ needs.native-journeys.result }}", required, StringComparison.Ordinal);
        Assert.Contains("./tools/native-integration-result.ps1 -PlatformGates $env:PLATFORM_GATES_RESULT -NativeJourneys $env:NATIVE_JOURNEYS_RESULT",
            required, StringComparison.Ordinal);
        Assert.Contains("run: ./tools/native-shard-tests.ps1", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeSmokeDefaultsToTheExhaustiveProfile()
    {
        string bash = Read("tools", "godot-smoke.sh");
        string powershell = Read("tools", "godot-smoke.ps1");

        Assert.Contains("profile=${2:---exhaustive}", bash, StringComparison.Ordinal);
        Assert.Contains("native-cases.ps1", bash, StringComparison.Ordinal);
        Assert.Contains("[switch]$Representative", powershell, StringComparison.Ordinal);
        Assert.Contains("else { 'exhaustive' }", powershell, StringComparison.Ordinal);
        Assert.Contains("native-cases.ps1", powershell, StringComparison.Ordinal);
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
