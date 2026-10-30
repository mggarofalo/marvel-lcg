using Marvel.Protocol;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class CampaignReplayCompatibilityTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PreviousReplayIdentityIsRefusedToTheCampaignOwnerAndItsAttempt(bool aggregate)
    {
        // Synthetic content isolates the persistence contract; this is not RoRS play evidence.
        var factory = new SyntheticCampaignFactory();
        var store = new MemoryCampaignStore();
        var host = new CampaignHost(factory, store, new MemorySessionStore());
        var created = host.Exchange(CampaignHostTests.Create());
        Assert.Null(created.Error);
        var entry = EntryClaim.Create();
        var opened = host.Exchange(CampaignHostTests.Command(created, "start", 0) with { Entry = entry });
        Assert.Null(opened.Error);
        var stored = Assert.Single(store.Load()).Campaign!;
        var save = stored.Save;
        var old = save.Compatibility with { ReplayContract = "engine-replay-v18" };
        save = aggregate ? save with { Compatibility = old }
            : save with { Attempts = save.Attempts.SetItem(0, save.Attempts[0] with { Compatibility = old }) };
        store.Commit(stored with { Save = save });
        byte[] before = CampaignSaveJson.Write(save);
        var restored = new CampaignHost(factory, store, new MemorySessionStore());
        AssertRefused(restored.Exchange(CampaignHostTests.Command(created, "sync", 0)).Error);
        AssertRefused(restored.Exchange(EngineRequest.SyncGame("attempt", opened.GameId,
            opened.Capability!)).Error);
        AssertRefused(restored.Exchange(EngineRequest.AttachGame("recover", opened.GameId, "unknown")
            with { Entry = entry }).Error);
        Assert.Equal("campaign_not_found", restored.Exchange(CampaignHostTests.Command(created, "sync", 0)
            with { Capability = "not-the-owner" }).Error?.Code);
        Assert.Equal(before, CampaignSaveJson.Write(Assert.Single(store.Load()).Campaign!.Save));
    }

    [Fact]
    public void ImportingACampaignFromAnotherReplayIdentityIsRefusedAndPublishesNothing()
    {
        var store = new MemoryCampaignStore();
        var host = new CampaignHost(new SyntheticCampaignFactory(), store, new MemorySessionStore());
        var owner = host.Exchange(CampaignHostTests.Create());
        var original = CampaignTransferCodec.Import(
            host.Exchange(CampaignHostTests.Command(owner, "export", 0)).CampaignTransfer!);
        string older = CampaignTransferCodec.Export(original with
        {
            Compatibility = original.Compatibility with { ReplayContract = "engine-replay-v18" },
        });
        var imported = host.Exchange(new EngineRequest(EngineProtocol.Version, "import",
            EngineProtocol.Campaign, "copy", Entry: EntryClaim.Create())
            { Campaign = new("import") { Transfer = older } });
        Assert.Equal(RefusedSaves.Code, imported.Error?.Code);
        Assert.Single(store.Load());
    }

    private static void AssertRefused(EngineError? error)
    {
        Assert.Equal(RefusedSaves.Code, error?.Code);
        Assert.Equal(EngineFailureDisposition.SessionUnavailable, error!.Disposition);
        Assert.Contains("Start a new game", error.Message, StringComparison.Ordinal);
    }
}
