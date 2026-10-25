using Marvel.Protocol;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ReplayUpgradePublicationTests
{
    [Fact]
    public void StaleV19SessionCannotOverwriteSameDatasetV22Publication()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            var target = previous.Save.Compatibility with { ReplayContract = "engine-replay-v22" };
            var route = Assert.IsType<SessionContentUpgrade>(ReplayContractUpgradeTests.Admission(root, target)
                .For(previous.Save.Compatibility));
            var candidate = previous with { Save = route.Verify(previous.Save,
                setup => ReplayContractUpgradeTests.Open(factory, setup)) };
            string storage = Path.Combine(root, "real-v19", "sessions");
            var store = new FileSessionStore(storage);
            string directory = Path.Combine(storage, previous.Save.Session.StorageId);
            string generation = File.ReadAllText(Path.Combine(directory, "current")).Trim();
            string? published = store.CommitContentUpgrade(previous, candidate, generation);
            Assert.Throws<SessionCompatibilityException>(() => store.Commit(previous));
            Assert.Equal(published, File.ReadAllText(Path.Combine(directory, "current")).Trim());
            Assert.Equal(StoredSessionJson.Write(candidate), StoredSessionJson.Write(Assert.Single(store.Load())));
            Assert.Equal(SessionSaveJson.Write(previous.Save),
                File.ReadAllText(Path.Combine(directory, ".upgrade-" + generation + ".session.json")));
        });
    }

    [Fact]
    public void SameStoreStaleCampaignCannotOverwriteVersionOnlyMigration()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, factory) =>
        {
            using var store = new FileCampaignStore(Path.Combine(root, "campaigns"));
            var host = new CampaignHost(factory, store, new MemorySessionStore());
            var opened = host.Exchange(CampaignContentUpgradeTests.Create(factory));
            Assert.Null(opened.Error);
            var previous = Assert.Single(store.Load()).Campaign!;
            // Synthetic storage test: no historical or replay-compatible gameplay claim is made.
            // Changing only a runtime identity exercises the publication boundary independently of content hashes.
            var candidate = previous with { Save = previous.Save with
                { Compatibility = previous.Save.Compatibility with { ReplayContract = "publication-test-next" } } };
            string published = store.CommitContentUpgrade(previous, candidate);
            Assert.Throws<SessionCompatibilityException>(() => store.Commit(previous));
            Assert.Equal(CampaignSaveJson.Canonical(candidate), CampaignSaveJson.Canonical(Assert.Single(store.Load()).Campaign));
            string directory = Path.Combine(root, "campaigns", previous.Save.Identity.StorageId);
            Assert.Contains(published, File.ReadAllText(Path.Combine(directory, "current")), StringComparison.Ordinal);
            Assert.Equal(CampaignSaveJson.Canonical(previous),
                File.ReadAllText(Assert.Single(Directory.GetFiles(directory, ".upgrade-*.archive"))));
        });
    }

    [Fact]
    public void RepeatedDeletionPublishesATombstoneWithoutReadingItAsACampaign()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            using var store = new FileCampaignStore(Path.Combine(root, "campaigns"));
            var deletion = new CampaignDeletion(new string('a', 32), new string('b', 64), "delete",
                new string('c', 64), new string('d', 64), [], []);
            store.Delete(deletion);
            store.Delete(deletion);
            Assert.Equal(CampaignSaveJson.Canonical(deletion), CampaignSaveJson.Canonical(Assert.Single(store.Deletions())));
            Assert.Empty(store.Load());
        });
    }
}
