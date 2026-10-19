using System.Text.Json.Nodes;
using Xunit;
namespace Marvel.Server.Tests;

public sealed class FileCampaignStoreTests
{
    [Fact]
    public void ArchivesRoundTripUnderAnExclusiveWriterLease()
    {
        string root = Path.Combine(Path.GetTempPath(), "marvel-campaign-" + Guid.NewGuid().ToString("N"));
        try
        {
            string id;
            using (var store = new FileCampaignStore(root))
            {
                Assert.Throws<IOException>(() => new FileCampaignStore(root));
                var host = new CampaignHost(new SyntheticCampaignFactory(), store, new MemorySessionStore());
                var response = host.Exchange(CampaignHostTests.Create());
                Assert.Null(response.Error);
                id = response.Campaign!.Id;
                Assert.Equal(id, Assert.Single(store.Load()).Campaign!.Save.Identity.StorageId);
            }
            using var restored = new FileCampaignStore(root);
            Assert.Equal(id, Assert.Single(restored.Load()).Campaign!.Save.Identity.StorageId);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void CorruptCurrentGenerationIsQuarantinedWithoutResurrectingPreviousState()
    {
        string root = Path.Combine(Path.GetTempPath(), "marvel-campaign-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var store = new FileCampaignStore(root);
            var host = new CampaignHost(new SyntheticCampaignFactory(), store, new MemorySessionStore());
            var response = host.Exchange(CampaignHostTests.Create());
            Assert.Null(response.Error);
            Assert.Null(host.Exchange(CampaignHostTests.Command(response, "mode", 0)).Error);
            string directory = Path.Combine(root, response.Campaign!.Id);
            var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "current")))!;
            string generation = manifest["generation"]!.GetValue<string>();
            string path = Path.Combine(directory, generation + ".archive");
            byte[] payload = File.ReadAllBytes(path);
            payload[payload.Length / 2] ^= 1;
            File.WriteAllBytes(path, payload);
            var rejected = Assert.Single(store.Load());
            Assert.Null(rejected.Campaign);
            Assert.Equal("campaign_restore_failed", rejected.Error);
            Assert.Equal(2, Directory.GetFiles(directory, "*.archive").Length);
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
