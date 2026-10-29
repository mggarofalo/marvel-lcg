using System.IO.Compression;
using System.Security.Cryptography;
using Marvel.Protocol;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublishedV28ReplayQuarantineTests
{
    [Theory]
    [InlineData("control", "unit 1 decision 0 prompt", "replay-870-core",
        "e6b755f6a339d6f06f2e3064ab12d8ef3218c38f304b9eaa1e9efdeaee162be5")]
    [InlineData("she-hulk-sync-original", "unit 1 decision 0 events[6]", "she-hulk-taskmaster",
        "5bf1168b3bee4055abf6d26c64278366c53fbcecaac49179745fefc46ed72610")]
    public void PublishedSaveDivergesDiagnosticallyAndProductionPreservesEveryFile(string fixture, string divergence,
        string gameId, string capability)
    {
        string archive = Path.Combine(RepositoryPaths.Root, "tests", "Marvel.Server.Tests", "Fixtures",
            "published-v28-rejection-870.zip");
        Assert.Equal("1A27EB1C902EACC982F8E6DA5CAE4602061269D7143B6712271B7EFCBD64003D",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive))));
        string temp = Path.Combine(Path.GetTempPath(), "marvel-v28-quarantine-" + Guid.NewGuid().ToString("N"));
        try
        {
            ZipFile.ExtractToDirectory(archive, temp);
            string storage = Path.Combine(temp, fixture, "sessions");
            var store = new FileSessionStore(storage);
            var previous = Assert.Single(store.Load());
            Assert.Equal("engine-replay-v28", previous.Save.Compatibility.ReplayContract);
            Assert.NotEmpty(previous.Save.Units.SelectMany(unit => unit.Decisions));
            Assert.Equal(gameId, previous.Save.Session.Label);
            Assert.Contains(previous.Authorities, authority => authority.Owner && !authority.Revoked
                && authority.Verifier == SessionAuthorityRegistry.Verifier(capability));
            var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
            Assert.Equal("engine-replay-v29", factory.Compatibility.ReplayContract);
            Assert.Null(factory.ContentUpgrade(previous.Save.Compatibility));
            // Diagnostic only: this explicit in-memory adapter is not host admission.
            var diagnosticTarget = factory.Compatibility with { ReplayContract = "engine-replay-v28" };
            var failure = Assert.Throws<ReplayDivergenceException>(() =>
                new SessionContentUpgrade(previous.Save.Compatibility, diagnosticTarget)
                    .Verify(previous.Save, setup => ReplayContractUpgradeTests.Open(factory, setup)));
            Assert.Contains(divergence, failure.Message, StringComparison.Ordinal);
            var before = Snapshot(storage);
            for (int restart = 0; restart < 2; restart++)
            {
                var host = new EngineHost(factory, store: new FileSessionStore(storage));
                Assert.Equal("session_not_found", host.Exchange(EngineRequest.SyncGame(
                    "rejected", gameId, capability)).Error?.Code);
                Assert.Equal(before, Snapshot(storage));
                Assert.Empty(Directory.GetFiles(storage, ".upgrade-*", SearchOption.AllDirectories));
            }
        }
        finally { if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true); }
    }

    private static string[] Snapshot(string directory) => Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
        .Where(path => !path.EndsWith(".writer", StringComparison.Ordinal))
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => Path.GetRelativePath(directory, path) + ":" +
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))).ToArray();
}
