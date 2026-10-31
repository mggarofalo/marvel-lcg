using System.IO.Compression;
using System.Security.Cryptography;
using Marvel.Protocol;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublishedV28SaveRefusalTests
{
    [Theory]
    [InlineData("control", "replay-870-core",
        "e6b755f6a339d6f06f2e3064ab12d8ef3218c38f304b9eaa1e9efdeaee162be5")]
    [InlineData("she-hulk-sync-original", "she-hulk-taskmaster",
        "5bf1168b3bee4055abf6d26c64278366c53fbcecaac49179745fefc46ed72610")]
    public void APublishedOlderSaveIsRefusedToItsHolderAndLeftUntouched(
        string fixture, string gameId, string capability)
    {
        string archive = Path.Combine(RepositoryPaths.Root, "tests", "Marvel.Server.Tests", "Fixtures",
            "published-v28-rejection-870.zip");
        Assert.Equal("1A27EB1C902EACC982F8E6DA5CAE4602061269D7143B6712271B7EFCBD64003D",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive))));
        string temp = Path.Combine(Path.GetTempPath(), "marvel-v28-refusal-" + Guid.NewGuid().ToString("N"));
        try
        {
            ZipFile.ExtractToDirectory(archive, temp);
            string storage = Path.Combine(temp, fixture, "sessions");
            var previous = Assert.Single(new FileSessionStore(storage).Load());
            Assert.Equal("engine-replay-v28", previous.Save.Compatibility.ReplayContract);
            Assert.Equal(gameId, previous.Save.Session.Label);
            var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
            Assert.NotEqual(previous.Save.Compatibility.ReplayContract, factory.Compatibility.ReplayContract);
            var before = Snapshot(storage);
            for (int restart = 0; restart < 2; restart++)
            {
                var host = new EngineHost(factory, store: new FileSessionStore(storage));
                EngineError refused = Assert.IsType<EngineError>(host.Exchange(
                    EngineRequest.SyncGame("refused", gameId, capability)).Error);
                Assert.Equal("save_incompatible", refused.Code);
                Assert.Equal(EngineFailureDisposition.SessionUnavailable, refused.Disposition);
                Assert.Contains("Start a new game", refused.Message, StringComparison.Ordinal);
                Assert.Equal("session_not_found", host.Exchange(
                    EngineRequest.SyncGame("stranger", gameId, "not-" + capability)).Error?.Code);
                Assert.Equal(before, Snapshot(storage));
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
