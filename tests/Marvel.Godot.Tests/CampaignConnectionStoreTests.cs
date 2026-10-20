using Marvel.Client;
using Marvel.Desktop;
using Marvel.Protocol;
using Xunit;
namespace Marvel.Godot.Tests;

public sealed class CampaignConnectionStoreTests
{
    [Fact]
    public void RestartRetainsExactPendingEntryBeforeAnyServerAcknowledgement()
    {
        string root = Path.Combine(Path.GetTempPath(), "marvel-campaign-access-" + Guid.NewGuid().ToString("N"));
        try
        {
            var request = new EngineRequest(EngineProtocol.Version, "creation", EngineProtocol.Campaign, "my-campaign",
                Entry: EntryClaim.Create()) { Campaign = new("create") };
            var pending = new CampaignConnection(Guid.NewGuid().ToString("N"), "", "my-campaign", null, null, null, request);
            using (var store = new CampaignConnectionStore(root)) store.Save(pending);
            using var restored = new CampaignConnectionStore(root);
            var loaded = Assert.Single(restored.Load());
            Assert.Equal(pending.LocalId, loaded.LocalId);
            Assert.Null(loaded.Capability);
            Assert.Equal(EngineJson.Write(request), EngineJson.Write(loaded.Pending!));
            var accepted = loaded with { CampaignId = new('a', 32), Capability = "owner", Game = new("attempt", "game-owner"), Pending = null };
            restored.Save(accepted);
            Assert.Equal(accepted, Assert.Single(restored.Load()));
            restored.Delete(accepted.LocalId);
            Assert.Empty(restored.Load());
            Assert.False(Directory.Exists(Path.Combine(root, accepted.LocalId)));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
