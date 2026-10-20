using System.IO.Compression;
using System.Security.Cryptography;
using Marvel.Session;
using Marvel.Sim;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class RetaliationReplayBoundaryTests
{
    [Fact]
    public void StrictReplayRejectsOriginalGameAtTheChangedRetaliationOutcome()
    {
        // Replay acceptance is an engine contract: a corrected outcome cannot
        // silently replace the history that the player actually committed.
        string path = RepositoryPaths.Repository("tests", "Marvel.Acceptance.Tests",
            "Fixtures", "retaliate-predecessor-91098022.jsonl.gz");
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        Assert.Equal("F9107037687344F2CFDE00005E3ACC2A5494894B1F905C0EF8E94AAE1B0E7BE1",
            Convert.ToHexString(SHA256.HashData(gzip)));
        var failure = Assert.Throws<ReplayDivergenceException>(() =>
            SimulationReplayHarness.Replay(new ReplayConfig(path, RepositoryPaths.Root), TextWriter.Null));
        Assert.Contains("step 9 events diverged: expected 18, got 19", failure.Message);
    }
}
