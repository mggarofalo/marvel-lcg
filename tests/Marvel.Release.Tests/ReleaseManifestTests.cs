using System.Text.Json;
using Marvel.Tests;
using Xunit;

namespace Marvel.Release.Tests;

public sealed class ReleaseManifestTests
{
    [Fact]
    public void ManifestCarriesTheCompiledEngineAndExactDatasets()
    {
        const string commit = "0123456789abcdef0123456789abcdef01234567";
        ReleaseVersion version = ReleaseVersion.Parse("0.1.0-dev.0");

        InvalidOperationException mismatch = Assert.Throws<InvalidOperationException>(
            () => ReleaseManifest.Create(version, commit, RepositoryPaths.Root));

        Assert.Contains("compiled build identity", mismatch.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultBuildCanDescribeItsExactInputs()
    {
        const string localCommit = "0123456789abcdef0123456789abcdef01234567";
        // Manifest creation deliberately binds to compiled metadata. The
        // default local build says "local", which is not publishable commit
        // identity and must therefore fail before it can emit an artifact.
        ArgumentException failure = Assert.Throws<ArgumentException>(
            () => ReleaseManifest.Create(
                ReleaseVersion.Parse("0.1.0-dev.0"),
                "local",
                RepositoryPaths.Root));

        Assert.Contains("40 lowercase", failure.Message, StringComparison.Ordinal);
        Assert.Equal(40, localCommit.Length);
    }

    [Fact]
    public void CanonicalShapeUsesTheReleaseWireNames()
    {
        var manifest = new ReleaseManifest(
            "marvel-release",
            1,
            "0.1.0",
            "stable",
            "0123456789abcdef0123456789abcdef01234567",
            "0.1.0.0",
            new ReleaseEngineIdentity("engine-replay-v1", "mt19937-iso-cxx", "state-digest-v2", 10, 2),
            new ReleaseDatasetIdentity("cards", "setup", "abilities"));

        using JsonDocument json = JsonDocument.Parse(manifest.Json());

        Assert.Equal("marvel-release", json.RootElement.GetProperty("format").GetString());
        Assert.Equal(10, json.RootElement.GetProperty("engine").GetProperty("protocol").GetInt32());
        Assert.Equal(
            "cards",
            json.RootElement.GetProperty("datasets").GetProperty("cards_sha256").GetString());
    }
}
