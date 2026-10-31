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
        AssertRefused(restored.Exchange(EngineRequest.AttachGame("recover", "another-game", "unknown")
            with { Entry = entry }).Error);
        Assert.Equal("session_not_found", restored.Exchange(EngineRequest.SyncGame("stranger", opened.GameId,
            "not-a-seat")).Error?.Code);
        Assert.Equal("campaign_not_found", restored.Exchange(CampaignHostTests.Command(created, "sync", 0)
            with { Capability = "not-the-owner" }).Error?.Code);
        Assert.Equal(before, CampaignSaveJson.Write(Assert.Single(store.Load()).Campaign!.Save));
    }

    [Fact]
    public void OnlyTheOwnerCanPermanentlyDeleteARefusedCampaign()
    {
        string root = Path.Combine(Path.GetTempPath(), "marvel-refused-campaign-" + Guid.NewGuid().ToString("N"));
        try
        {
            var factory = new SyntheticCampaignFactory();
            EngineResponse created;
            using (var store = new FileCampaignStore(root))
            {
                created = new CampaignHost(factory, store, new MemorySessionStore()).Exchange(CampaignHostTests.Create());
                var stored = Assert.Single(store.Load()).Campaign!;
                store.Commit(stored with { Save = stored.Save with
                {
                    Compatibility = stored.Save.Compatibility with { ReplayContract = "engine-replay-v18" },
                } });
            }
            string directory = Path.Combine(root, created.Campaign!.Id);
            using (var store = new FileCampaignStore(root))
            {
                var host = new CampaignHost(factory, store, new MemorySessionStore());
                Assert.Equal("campaign_not_found", host.Exchange(CampaignHostTests.Command(created, "delete", 0)
                    with { Capability = "not-the-owner" }).Error?.Code);
                Assert.True(Directory.Exists(directory));
                var deleted = host.Exchange(CampaignHostTests.Command(created, "delete", 0) with { ExpectedRevision = null });
                Assert.Null(deleted.Error);
                Assert.True(deleted.CampaignDeleted);
                Assert.False(Directory.Exists(directory));
                Assert.Equal("campaign_deleted", host.Exchange(CampaignHostTests.Command(created, "sync", 0)).Error?.Code);
            }
            using var reopened = new FileCampaignStore(root);
            Assert.Empty(reopened.Load());
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void EveryCampaignCommitStampsTheRuntimeAndAnOlderRuntimeRefusesIt()
    {
        var store = new MemoryCampaignStore();
        var created = new CampaignHost(new SyntheticCampaignFactory { Application = "0.1.0" }, store,
            new MemorySessionStore()).Exchange(CampaignHostTests.Create());
        Assert.Equal("0.1.0", Assert.Single(store.Load()).Campaign!.Save.Compatibility.Application);
        var upgraded = new CampaignHost(new SyntheticCampaignFactory { Application = "0.2.0" }, store, new MemorySessionStore());
        Assert.Null(upgraded.Exchange(CampaignHostTests.Command(created, "mode", 0)).Error);
        Assert.Equal("0.2.0", Assert.Single(store.Load()).Campaign!.Save.Compatibility.Application);

        var older = new CampaignHost(new SyntheticCampaignFactory { Application = "0.1.5" }, store, new MemorySessionStore());

        EngineError refused = Assert.IsType<EngineError>(older.Exchange(CampaignHostTests.Command(created, "sync", 1)).Error);
        Assert.Equal(RefusedSaves.Code, refused.Code);
        Assert.Contains("newer version", refused.Message, StringComparison.Ordinal);
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
