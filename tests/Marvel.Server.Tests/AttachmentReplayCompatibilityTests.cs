using System.Security.Cryptography;
using Marvel.Protocol;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class AttachmentReplayCompatibilityTests
{
    [Theory]
    [InlineData("engine-replay-v22", SessionReplayUpgrade.CoreRorsV19ToV22)]
    [InlineData("engine-replay-v23", SessionReplayUpgrade.CoreRorsV19ToV23)]
    [InlineData("engine-replay-v24", SessionReplayUpgrade.CoreRorsV19ToV24)]
    [InlineData("engine-replay-v25", SessionReplayUpgrade.CoreRorsV19ToV25)]
    public void UnsupportedPredecessorRemainsQuarantinedWithoutChangingAnySourceFile(
        string replay, SessionReplayUpgrade historicalRoute)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            // Derived intermediate evidence verifies every fact of the authentic v19 trace.
            // This is a compatibility control, not a historical recording of the derived version.
            var source = previous.Save.Compatibility with { ReplayContract = replay };
            var original = new SessionContentUpgrade(previous.Save.Compatibility, source, historicalRoute)
                .Verify(previous.Save, setup => ReplayContractUpgradeTests.Open(factory, setup));
            string bytes = SessionSaveJson.Write(original);
            Assert.Null(factory.ContentUpgrade(source));
            bool opened = false;
            var failure = Assert.Throws<SessionCompatibilityException>(() =>
                SessionReplay.Verify(original, factory.Compatibility, setup =>
                {
                    opened = true;
                    return ReplayContractUpgradeTests.Open(factory, setup);
                }));
            Assert.Equal("replay_identity_mismatch", failure.Category);
            Assert.False(opened);
            string storage = Path.Combine(root, "quarantined");
            var store = new FileSessionStore(storage);
            store.Commit(previous with { Save = original });
            var before = Snapshot(storage);
            for (int restart = 0; restart < 2; restart++)
            {
                var host = new EngineHost(factory, store: store);
                Assert.Equal("session_not_found", host.Exchange(EngineRequest.SyncGame(
                    "rejected", original.Session.Label, "unpublished")).Error?.Code);
                Assert.Equal(before, Snapshot(storage));
                Assert.Equal(bytes, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
            }
        });
    }

    private static string[] Snapshot(string directory) => Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
        .Where(path => !path.EndsWith(".writer", StringComparison.Ordinal))
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => Path.GetRelativePath(directory, path) + ":" +
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))).ToArray();
}
