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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CorruptCurrentGenerationIsQuarantinedWithoutResurrectingPreviousState(bool manifestOnly)
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
            if (manifestOnly)
            {
                manifest["sha256"] = new string('0', 64);
                File.WriteAllText(Path.Combine(directory, "current"), manifest.ToJsonString());
            }
            else
            {
                payload[payload.Length / 2] ^= 1;
                File.WriteAllBytes(path, payload);
            }
            var rejected = Assert.Single(store.Load());
            Assert.Null(rejected.Campaign);
            Assert.Equal("campaign_restore_failed", rejected.Error);
            Assert.Equal(2, Directory.GetFiles(directory, "*.archive").Length);
        }
        finally { Directory.Delete(root, recursive: true); }
    }
    [Theory]
    [InlineData("authority")]
    [InlineData("receipt")]
    public void StructurallyForgedArchivesWithValidChecksumsAreQuarantined(string alteration)
    {
        string root = Path.Combine(Path.GetTempPath(), "marvel-campaign-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var store = new FileCampaignStore(root);
            var host = new CampaignHost(new SyntheticCampaignFactory(), store, new MemorySessionStore());
            var created = host.Exchange(CampaignHostTests.Create());
            var start = CampaignHostTests.Command(created, "start", 0) with { Entry = Marvel.Protocol.EntryClaim.Create() };
            Assert.Null(host.Exchange(start).Error);
            string directory = Path.Combine(root, created.Campaign!.Id);
            string manifestPath = Path.Combine(directory, "current");
            var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))!;
            string path = Path.Combine(directory, manifest["generation"]!.GetValue<string>() + ".archive");
            var payload = JsonNode.Parse(File.ReadAllBytes(path))!;
            string field = alteration == "authority" ? "authorities" : "receipts";
            payload[field]!.AsArray().Add(payload[field]![0]!.DeepClone());
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(payload.ToJsonString());
            File.WriteAllBytes(path, bytes);
            manifest["length"] = bytes.Length;
            manifest["sha256"] = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
            File.WriteAllText(manifestPath, manifest.ToJsonString());
            var result = Assert.Single(store.Load());
            Assert.Null(result.Campaign);
            Assert.Equal("campaign_restore_failed", result.Error);
            Assert.Equal(bytes, File.ReadAllBytes(path));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

}
