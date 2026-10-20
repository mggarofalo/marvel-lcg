using Marvel.Protocol;
using Marvel.Session;
using Xunit;
namespace Marvel.Server.Tests;

public sealed class CampaignManagementTests
{
    [Fact]
    public void RenameArchiveAndRestorePreserveTheAttemptAcrossRestart()
    {
        var store = new MemoryCampaignStore();
        var host = Host(store);
        var owner = host.Exchange(CampaignHostTests.Create());
        var started = host.Exchange(Command(owner, "start", 0) with { Entry = EntryClaim.Create() });
        Assert.Null(started.Error);
        var before = Assert.Single(store.Load()).Campaign!.Save;
        var rename = Command(owner, "rename", started.Campaign!.Revision) with { Campaign = new("rename") { Name = "Friday heroes" } };
        var named = host.Exchange(rename);
        Assert.Equal("Friday heroes", named.Campaign!.Name);
        Assert.Equal(before.Attempts, Assert.Single(store.Load()).Campaign!.Save.Attempts);
        Assert.Equal(before.Journal, Assert.Single(store.Load()).Campaign!.Save.Journal);
        var archive = Command(owner, "archive", named.Campaign.Revision);
        Assert.True(host.Exchange(archive).Campaign!.Archived);
        Assert.Equal("campaign_archived", host.Exchange(EngineRequest.SyncGame("game", started.GameId, started.Capability!)).Error!.Code);
        host = Host(store);
        var repeated = host.Exchange(archive);
        Assert.True(repeated.Campaign!.Archived);
        Assert.Equal(named.Campaign.Revision + 1, repeated.Campaign.Revision);
        var restored = host.Exchange(Command(owner, "restore", repeated.Campaign.Revision));
        Assert.False(restored.Campaign!.Archived);
        Assert.NotNull(host.Exchange(EngineRequest.SyncGame("resume", started.GameId, started.Capability!)).World);
        Assert.Equal("revision_conflict", host.Exchange(rename with { RequestId = "stale" }).Error!.Code);
    }

    [Fact]
    public void PortableCopyPreservesTheLivePromptButNeverCredentialsOrIdentities()
    {
        var store = new MemoryCampaignStore();
        var host = Host(store);
        var owner = host.Exchange(CampaignHostTests.Create());
        var started = host.Exchange(Command(owner, "start", 0) with { Entry = EntryClaim.Create() });
        var export = host.Exchange(Command(owner, "export", started.Campaign!.Revision));
        Assert.NotNull(export.CampaignTransfer);
        Assert.DoesNotContain(owner.Capability!, export.CampaignTransfer);
        var import = Import(export.CampaignTransfer);
        var copied = host.Exchange(import);
        Assert.Null(copied.Error);
        Assert.NotEqual(owner.Campaign!.Id, copied.Campaign!.Id);
        Assert.Equal(owner.Campaign.Name + " (copy)", copied.Campaign.Name);
        Assert.NotEqual(owner.Capability, copied.Capability);
        Assert.NotEqual(started.GameId, copied.CampaignGame!.GameId);
        var resumed = host.Exchange(EngineRequest.SyncGame("copy", copied.CampaignGame.GameId, copied.CampaignGame.Capability));
        Assert.Null(resumed.Error);
        Assert.Equal(CampaignSaveJson.Canonical(started.World), CampaignSaveJson.Canonical(resumed.World));
        Assert.Equal(CampaignSaveJson.Canonical(started.Prompt), CampaignSaveJson.Canonical(resumed.Prompt));
        Assert.NotNull(host.Exchange(EngineRequest.SyncGame("cross", started.GameId, copied.CampaignGame.Capability)).Error);
        Assert.NotNull(host.Exchange(Command(copied, "sync", 0) with { Capability = owner.Capability }).Error);
        host = Host(store);
        var repeated = host.Exchange(import);
        Assert.Equal(copied.Campaign.Id, repeated.Campaign!.Id);
        Assert.Equal(copied.CampaignGame, repeated.CampaignGame);
        Assert.Equal(2, store.Load().Count);
        Assert.Null(host.Exchange(EngineRequest.SyncGame("restart", copied.CampaignGame.GameId, copied.CampaignGame.Capability)).Error);
    }

    [Fact]
    public void DeleteRevokesGamesAndEntriesAndRecoversOnlyTheExactDeletion()
    {
        var store = new MemoryCampaignStore();
        var host = Host(store);
        var creation = CampaignHostTests.Create();
        var owner = host.Exchange(creation);
        var start = Command(owner, "start", 0) with { Entry = EntryClaim.Create() };
        var started = host.Exchange(start);
        var delete = Command(owner, "delete", started.Campaign!.Revision);
        Assert.True(host.Exchange(delete).CampaignDeleted);
        Assert.Empty(store.Load());
        Assert.Single(store.Deletions());
        Assert.Equal("campaign_deleted", host.Exchange(EngineRequest.SyncGame("old", started.GameId, started.Capability!)).Error!.Code);
        host = Host(store);
        Assert.True(host.Exchange(delete).CampaignDeleted);
        Assert.Equal("campaign_not_found", host.Exchange(delete with { Capability = "wrong" }).Error!.Code);
        Assert.Equal("campaign_deleted", host.Exchange(delete with { RequestId = "different" }).Error!.Code);
        Assert.Equal("campaign_deleted", host.Exchange(creation).Error!.Code);
        Assert.Equal("campaign_deleted", host.Exchange(start).Error!.Code);
        Assert.Empty(store.Load());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UncertainDeleteRequiresRestartAndNeverPublishesAFalseRollback(bool published)
    {
        var store = new FailingCampaignStore();
        var host = Host(store);
        var owner = host.Exchange(CampaignHostTests.Create());
        var delete = Command(owner, "delete", 0);
        store.Fail = true; store.PublishThenFail = published;
        var failed = host.Exchange(delete);
        Assert.Equal(published ? "campaign_recovery_required" : "save_failed", failed.Error!.Code);
        store.Fail = false;
        host = Host(store);
        Assert.True(host.Exchange(delete).CampaignDeleted);
        Assert.Empty(store.Load());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("line\nbreak")]
    public void InvalidNamesDoNotChangeTheCampaign(string name)
    {
        var store = new MemoryCampaignStore();
        var host = Host(store);
        var owner = host.Exchange(CampaignHostTests.Create());
        Assert.NotNull(host.Exchange(Command(owner, "rename", 0) with { Campaign = new("rename") { Name = name } }).Error);
        Assert.Equal(0, Assert.Single(store.Load()).Campaign!.Save.Revision);
    }

    [Fact]
    public void DamagedTransferAndUnsupportedFormatDoNotPublishAnything()
    {
        var store = new MemoryCampaignStore();
        var host = Host(store);
        var owner = host.Exchange(CampaignHostTests.Create());
        string transfer = host.Exchange(Command(owner, "export", 0)).CampaignTransfer!;
        string[] parts = transfer.Split('\n');
        parts[1] = new string('0', 64);
        Assert.NotNull(host.Exchange(Import(string.Join('\n', parts))).Error);
        Assert.NotNull(host.Exchange(Import("unsupported")).Error);
        Assert.Single(store.Load());
    }

    private static CampaignHost Host(ICampaignStore store) =>
        new(new SyntheticCampaignFactory(), store, new MemorySessionStore());
    private static EngineRequest Command(EngineResponse owner, string action, long revision) =>
        CampaignHostTests.Command(owner, action, revision);
    private static EngineRequest Import(string transfer) =>
        new(EngineProtocol.Version, "import", EngineProtocol.Campaign, "copy", Entry: EntryClaim.Create())
        { Campaign = new("import") { Transfer = transfer } };
}
