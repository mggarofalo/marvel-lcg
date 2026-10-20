using Marvel.Protocol;
using Xunit;
namespace Marvel.Server.Tests;

public sealed class CampaignDeletionTests
{
    [Theory]
    [InlineData(2, 64)]
    [InlineData(1, 63)]
    public void InvalidRevocationCannotDeleteCampaignData(int schema, int verifierLength)
    {
        string root = Path.Combine(Path.GetTempPath(), "campaign-delete-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var store = new FileCampaignStore(root);
            var host = new CampaignHost(new SyntheticCampaignFactory(), store, new MemorySessionStore());
            var owner = host.Exchange(CampaignHostTests.Create());
            var deletion = new CampaignDeletion(owner.Campaign!.Id, new('a', verifierLength), "delete",
                new('b', 64), new('c', 64), [], []) { Schema = schema };
            Assert.Throws<Marvel.Session.SessionSaveException>(() => store.Delete(deletion));
            Assert.Single(store.Load());
            Assert.Empty(store.Deletions());
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void PermanentDeletePurgesEveryGenerationAndKeepsOnlyRetryMetadata()
    {
        string root = Path.Combine(Path.GetTempPath(), "campaign-delete-" + Guid.NewGuid().ToString("N"));
        try
        {
            EngineRequest deletion;
            string id;
            using (var store = new FileCampaignStore(root))
            {
                var host = new CampaignHost(new SyntheticCampaignFactory(), store, new MemorySessionStore());
                var owner = host.Exchange(CampaignHostTests.Create());
                id = owner.Campaign!.Id;
                var changed = host.Exchange(CampaignHostTests.Command(owner, "rename", 0)
                    with { Campaign = new("rename") { Name = "Private Friday campaign" } });
                Assert.Equal(2, Directory.GetFiles(Path.Combine(root, id), "*.archive").Length);
                deletion = CampaignHostTests.Command(owner, "delete", changed.Campaign!.Revision);
                Assert.True(host.Exchange(deletion).CampaignDeleted);
                Assert.False(Directory.Exists(Path.Combine(root, id)));
                Assert.Empty(store.Load());
            }
            using var restarted = new FileCampaignStore(root);
            var restored = new CampaignHost(new SyntheticCampaignFactory(), restarted, new MemorySessionStore());
            Assert.True(restored.Exchange(deletion).CampaignDeleted);
            Assert.Empty(restarted.Load());
            foreach (string file in Directory.GetFiles(root, "*.archive", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                Assert.DoesNotContain("Private Friday campaign", text);
                Assert.DoesNotContain("spider_man", text);
                Assert.DoesNotContain("journal", text);
            }
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
