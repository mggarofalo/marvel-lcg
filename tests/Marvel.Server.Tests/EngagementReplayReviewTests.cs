using System.Security.Cryptography;
using Marvel.Protocol;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EngagementReplayReviewTests
{
    [Theory]
    [InlineData("downgrade", "unsupported_downgrade")]
    [InlineData("rng", "rng_identity_mismatch")]
    [InlineData("digest", "digest_identity_mismatch")]
    public void NamedV26RouteRetainsIndependentRuntimeGuards(string contract, string category)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            var target = contract switch
            {
                "downgrade" => factory.Compatibility with { Application = "0.0.0" },
                "rng" => factory.Compatibility with { RngContract = "unknown" },
                _ => factory.Compatibility with { StateDigest = "unknown" },
            };
            bool opened = false;
            var failure = Assert.Throws<SessionCompatibilityException>(() =>
                new SessionContentUpgrade(previous.Save.Compatibility, target, SessionReplayUpgrade.CoreRorsV19ToV26)
                    .Verify(previous.Save, setup =>
                    {
                        opened = true;
                        return ReplayContractUpgradeTests.Open(factory, setup);
                    }));
            Assert.Equal(category, failure.Category);
            Assert.False(opened);
        });
    }

    [Fact]
    public void PendingPromptMismatchAfterCompleteReplayCannotPublishOrChangeDurablePredecessor()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            var original = previous with { Save = previous.Save with
                { CurrentPrompt = previous.Save.CurrentPrompt! with { Label = "changed pending prompt" } } };
            string storage = Path.Combine(root, "late-rejection");
            var store = new FileSessionStore(storage);
            store.Commit(original);
            var before = Snapshot(storage);
            var route = Assert.IsType<SessionContentUpgrade>(factory.ContentUpgrade(original.Save.Compatibility));
            bool opened = false;
            var failure = Assert.Throws<ReplayDivergenceException>(() => route.Verify(original.Save, setup =>
            {
                opened = true;
                return ReplayContractUpgradeTests.Open(factory, setup);
            }));
            Assert.True(opened);
            Assert.Contains("current prompt", failure.Message, StringComparison.Ordinal);
            for (int restart = 0; restart < 2; restart++)
            {
                var host = new EngineHost(factory, store: new FileSessionStore(storage));
                Assert.Equal("session_not_found", host.Exchange(EngineRequest.SyncGame(
                    "late-rejection", original.Save.Session.Label, "unpublished")).Error?.Code);
                Assert.Equal(before, Snapshot(storage));
                Assert.Equal(StoredSessionJson.Write(original),
                    StoredSessionJson.Write(Assert.Single(new FileSessionStore(storage).Load())));
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
