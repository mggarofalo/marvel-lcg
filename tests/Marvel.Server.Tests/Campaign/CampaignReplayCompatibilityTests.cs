using Marvel.Protocol;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class CampaignReplayCompatibilityTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PreviousReplayIdentityCannotRestoreCampaignOrItsAttempt(bool aggregate)
    {
        // Synthetic content isolates the persistence contract; this is not RoRS play evidence.
        var factory = new SyntheticCampaignFactory();
        var store = new MemoryCampaignStore();
        var host = new CampaignHost(factory, store, new MemorySessionStore());
        var created = host.Exchange(CampaignHostTests.Create());
        Assert.Null(created.Error);
        var opened = host.Exchange(CampaignHostTests.Command(created, "start", 0)
            with { Entry = EntryClaim.Create() });
        Assert.Null(opened.Error);
        var stored = Assert.Single(store.Load()).Campaign!;
        var save = stored.Save;
        var old = save.Compatibility with { ReplayContract = "engine-replay-v18" };
        save = aggregate ? save with { Compatibility = old }
            : save with { Attempts = save.Attempts.SetItem(0, save.Attempts[0] with { Compatibility = old }) };
        store.Commit(stored with { Save = save });
        byte[] before = CampaignSaveJson.Write(save);
        var restored = new CampaignHost(factory, store, new MemorySessionStore());
        Assert.Equal("campaign_not_found", restored.Exchange(CampaignHostTests.Command(created, "sync", 0)).Error?.Code);
        Assert.Equal("session_not_found", restored.Exchange(EngineRequest.SyncGame("attempt", opened.GameId,
            opened.Capability!)).Error?.Code);
        Assert.Equal(before, CampaignSaveJson.Write(Assert.Single(store.Load()).Campaign!.Save));
    }
}
