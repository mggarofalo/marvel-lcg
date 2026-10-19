using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Xunit;
namespace Marvel.Server.Tests;

public sealed class CampaignHostTests
{
    [Fact]
    public void CampaignCreationAndDifficultySurviveRestartAndLostAcknowledgements()
    {
        var store = new MemoryCampaignStore();
        var factory = new SyntheticCampaignFactory();
        var host = new CampaignHost(factory, store, new MemorySessionStore());
        var request = Create();
        var created = host.Exchange(request);
        Assert.Null(created.Error);
        Assert.Equal(CampaignPhase.Preparing, created.Campaign!.State.Phase);
        var restarted = new CampaignHost(factory, store, new MemorySessionStore());
        var recovered = restarted.Exchange(request with { RequestId = "create-retry" });
        Assert.Null(recovered.Error);
        Assert.Equal(created.Capability, recovered.Capability);
        Assert.Equal(created.Campaign.Id, recovered.Campaign!.Id);
        var mode = Command(created, "mode", 0) with { Campaign = new("mode") { Expert = true } };
        var changed = restarted.Exchange(mode);
        Assert.Null(changed.Error);
        Assert.True(changed.Campaign!.State.ExpertScenario);
        Assert.False(changed.Campaign.State.Expert);
        Assert.Equal(1, changed.Campaign.Revision);
        var replayed = restarted.Exchange(mode);
        Assert.Null(replayed.Error);
        Assert.Equal(1, replayed.Campaign!.Revision);
        Assert.Single(store.Load());
    }

    [Fact]
    public void StaleAndUnauthenticatedChoicesLeaveCampaignUnchanged()
    {
        var store = new MemoryCampaignStore();
        var host = new CampaignHost(new SyntheticCampaignFactory(), store, new MemorySessionStore());
        var created = host.Exchange(Create());
        var request = Command(created, "mode", 9);
        Assert.Equal("revision_conflict", host.Exchange(request).Error!.Code);
        Assert.Equal("campaign_not_found", host.Exchange(request with { Capability = "incorrect" }).Error!.Code);
        Assert.Equal(0, store.Load()[0].Campaign!.Save.Revision);
    }

    [Fact]
    public void AttemptAndCampaignCommitTogetherAndRecoverTheSameGameAuthorityAfterRestart()
    {
        var store = new MemoryCampaignStore();
        var ordinary = new MemorySessionStore();
        var factory = new SyntheticCampaignFactory();
        var host = new CampaignHost(factory, store, ordinary);
        var created = host.Exchange(Create());
        var start = Command(created, "start", 0) with { Entry = EntryClaim.Create() };
        var opened = host.Exchange(start);
        Assert.Null(opened.Error);
        Assert.NotNull(opened.Prompt);
        Assert.Equal(CampaignPhase.Playing, opened.Campaign!.State.Phase);
        Assert.Empty(ordinary.Load());
        Assert.Single(store.Load()[0].Campaign!.Save.Attempts);
        var restored = new CampaignHost(factory, store, ordinary);
        var repeated = restored.Exchange(start);
        Assert.Null(repeated.Error);
        Assert.Equal(opened.Capability, repeated.Capability);
        Assert.Equal(opened.GameId, repeated.GameId);
        Assert.Single(store.Load()[0].Campaign!.Save.Attempts);
        Assert.NotNull(restored.Exchange(EngineRequest.SyncGame("sync", opened.GameId, opened.Capability!)).World);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedPublicationNeverAdvertisesRollbackWhenCommitCouldHaveSucceeded(bool published)
    {
        var store = new FailingCampaignStore();
        var factory = new SyntheticCampaignFactory();
        var host = new CampaignHost(factory, store, new MemorySessionStore());
        var created = host.Exchange(Create());
        store.Fail = true;
        store.PublishThenFail = published;
        var choice = Command(created, "mode", 0) with { Campaign = new("mode") { Expert = true } };
        var response = host.Exchange(choice);
        Assert.Equal(published ? "campaign_recovery_required" : "save_failed", response.Error!.Code);
        Assert.Equal(published ? EngineFailureDisposition.CommitUncertain : EngineFailureDisposition.Rejected,
            response.Error.Disposition);
        Assert.Equal(published, store.Inner.Load()[0].Campaign!.Save.State.ExpertScenario);
        store.Fail = false;
        var subsequent = host.Exchange(choice);
        Assert.Equal(published, subsequent.Error is not null);
        var restored = new CampaignHost(factory, store, new MemorySessionStore());
        var verified = restored.Exchange(Command(created, "sync", 0));
        Assert.True(verified.Campaign!.State.ExpertScenario);
        Assert.Equal(1, verified.Campaign.Revision);
    }

    [Fact]
    public void ClosingTheTablePreservesTheCampaignAttemptAcrossRestart()
    {
        var store = new MemoryCampaignStore();
        var factory = new SyntheticCampaignFactory();
        var host = new CampaignHost(factory, store, new MemorySessionStore());
        var created = host.Exchange(Create());
        var opened = host.Exchange(Command(created, "start", 0) with { Entry = EntryClaim.Create() });
        Assert.Null(opened.Error);
        var closed = host.Exchange(EngineRequest.CloseGame("leave", opened.GameId, opened.Capability!));
        Assert.Null(closed.Error);
        var restarted = new CampaignHost(factory, store, new MemorySessionStore());
        var resumed = restarted.Exchange(EngineRequest.SyncGame("resume", opened.GameId, opened.Capability!));
        Assert.Null(resumed.Error);
        Assert.NotNull(resumed.World);
        Assert.Single(store.Load()[0].Campaign!.Save.Attempts);
    }

    [Fact]
    public void CampaignDeckAssessmentRequiresOwnerAndCurrentEnvelope()
    {
        var host = new CampaignHost(new SyntheticCampaignFactory(), new MemoryCampaignStore(), new MemorySessionStore());
        var created = host.Exchange(Create());
        var query = new DeckQuery("spider_man", []) { Campaign = new(created.Campaign!.Id, created.Capability!) };
        var request = EngineRequest.ReadDecks("assess", query);
        Assert.Null(host.Exchange(request).Error);
        Assert.Equal("unsupported_version", host.Exchange(request with { Version = 1 }).Error!.Code);
        Assert.Equal("campaign_not_found", host.Exchange(request with
            { Deck = query with { Campaign = query.Campaign with { Capability = "incorrect" } } }).Error!.Code);
        Assert.Equal("invalid_request", host.Exchange(request with { Deck = query with { Hero = "iron_man" } }).Error!.Code);
        Assert.Equal("invalid_request", host.Exchange(request with { RequestId = "" }).Error!.Code);
    }

    internal static EngineRequest Create() => new(EngineProtocol.Version, "create", EngineProtocol.Campaign, "campaign",
        Entry: EntryClaim.Create()) { Campaign = new("create") { Product = "synthetic", Seed = 42,
            Heroes = ["spider_man"], Decks = [new([])] } };
    internal static EngineRequest Command(EngineResponse created, string action, long revision) =>
        new(EngineProtocol.Version, action, EngineProtocol.Campaign, created.Campaign!.Id,
            Capability: created.Capability, ExpectedRevision: revision) { Campaign = new(action) };
}
