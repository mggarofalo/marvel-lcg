using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Marvel.Protocol;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class RetaliationReplayUpgradeRejectionTests
{
    [Theory]
    [InlineData("v19")]
    [InlineData("v20")]
    public void ActualPredecessorBlackPantherHistoryCannotPublishChangedRetaliationOutcomes(string version)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            string archive = Path.Combine(RepositoryPaths.Root, "tests", "Marvel.Server.Tests",
                "Fixtures", version == "v19" ? "retaliation-v19-91098022.zip" : "replay-v20-9097da07.zip");
            Assert.Equal(version == "v19" ? "2077DDA898D36A1C55EC9285557E59AA7D0F3A2F7669E3324DF9013F6D09547E"
                : "76ABF6271E0C8B1552A2EA220CA3A7710E9D0EE6765BDFBA0476933062C2E511",
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive))));
            string extraction = Path.Combine(root, "actual-v20");
            ZipFile.ExtractToDirectory(archive, extraction);
            string fixture = Path.Combine(extraction, "black-panther");
            string storage = Path.Combine(fixture, "sessions");
            var store = new FileSessionStore(storage);
            var original = Assert.Single(store.Load());
            Assert.Equal("engine-replay-" + version, original.Save.Compatibility.ReplayContract);
            Assert.Equal(30, original.Save.Units.Sum(unit => unit.Decisions.Count));
            var factory = DatasetGameFactory.Load(root);
            var route = Assert.IsType<SessionContentUpgrade>(factory.ContentUpgrade(original.Save.Compatibility));
            var failure = Assert.Throws<ReplayDivergenceException>(() =>
                route.Verify(original.Save, setup => ReplayContractUpgradeTests.Open(factory, setup)));
            Assert.Contains("event", failure.Message, StringComparison.OrdinalIgnoreCase);
            var before = Snapshot(storage);
            using var access = JsonDocument.Parse(File.ReadAllText(Path.Combine(fixture, "access.json")));
            for (int restart = 0; restart < 2; restart++)
            {
                var host = new EngineHost(factory, store: store);
                var refused = host.Exchange(EngineRequest.SyncGame("refused-" + restart,
                    access.RootElement.GetProperty("game").GetString()!,
                    access.RootElement.GetProperty("capability").GetString()!));
                Assert.Equal("session_not_found", refused.Error?.Code);
                Assert.Equal(before, Snapshot(storage));
                Assert.Equal(StoredSessionJson.Write(original), StoredSessionJson.Write(Assert.Single(store.Load())));
            }
            Assert.Empty(Directory.GetFiles(storage, ".upgrade-*", SearchOption.AllDirectories));
        });
    }

    private static string[] Snapshot(string directory) => Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
        .Where(path => !path.EndsWith(".writer", StringComparison.Ordinal))
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => Path.GetRelativePath(directory, path) + ":" +
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))).ToArray();
}
