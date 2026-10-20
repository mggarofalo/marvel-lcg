using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>Actual admitted RoRS setup and durable host acceptance; no synthetic factory.</summary>
public sealed class RiseOfRedSkullPersistenceTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ActualTwoPlayerAttemptRetainsModesPromptAndAuthoritiesAcrossDiskRestart(
        bool campaignExpert, bool scenarioExpert)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        Assert.NotNull(factory.CampaignDefinition);
        string directory = Path.Combine(Path.GetTempPath(), "marvel-rors-acceptance-" + Guid.NewGuid().ToString("N"));
        EngineResponse created;
        EngineResponse opened;
        EngineResponse guest;
        EngineResponse before;
        EngineRequest start;
        try
        {
            using (var store = new FileCampaignStore(directory))
            {
                var host = new CampaignHost(factory, store, new MemorySessionStore(), new Marvel.View.RestrictedVisibilityPolicy(0));
                created = host.Exchange(Create(factory, campaignExpert));
                Assert.Null(created.Error);
                var mode = host.Exchange(Command(created, "mode", 0) with
                    { Campaign = new("mode") { Expert = scenarioExpert } });
                Assert.Null(mode.Error);
                start = Command(created, "start", mode.Campaign!.Revision) with { Entry = EntryClaim.Create() };
                opened = host.Exchange(start);
                Assert.Null(opened.Error);
                Assert.Equal(CampaignPhase.Playing, opened.Campaign!.State.Phase);
                Assert.Equal(campaignExpert, opened.Campaign.State.Expert);
                Assert.Equal(scenarioExpert, opened.Campaign.State.ExpertScenario);
                guest = host.Exchange(EngineRequest.AttachGame("guest", opened.GameId,
                    Assert.Single(opened.Invitations!).Invitation));
                Assert.Null(guest.Error);
                Assert.Equal(0, opened.Prompt!.Player);
                var keep = Assert.Single(opened.Prompt.Affordances, offer => offer.Verb == Game.ResolveMulligans);
                var kept = host.Exchange(EngineRequest.ResolveGame("keep", opened.GameId,
                    opened.Capability!, Decision.Take(keep.Id), opened.Revision));
                Assert.Null(kept.Error);
                before = host.Exchange(EngineRequest.SyncGame("guest-prompt", opened.GameId, guest.Capability!));
                Assert.Null(before.Error);
                Assert.Equal(1, before.Prompt!.Player);
                var saved = Assert.Single(store.Load()).Campaign!.Save;
                var attempt = Assert.Single(saved.Attempts);
                Assert.Equal(scenarioExpert ? "crossbones_expert" : "crossbones", attempt.Setup.Scenario);
                Assert.Equal(campaignExpert, attempt.Setup.Campaign!.ExpertCampaign);
                Assert.Equal([0, 1], attempt.Setup.Campaign.Seats.Select(seat => seat.CampaignSeat ?? seat.Seat));
            }
            using (var store = new FileCampaignStore(directory))
            {
                // Fresh factory and host replay the complete attempt before publishing it.
                var restored = new CampaignHost(DatasetGameFactory.Load(RepositoryPaths.Root), store, new MemorySessionStore(), new Marvel.View.RestrictedVisibilityPolicy(0));
                var after = restored.Exchange(EngineRequest.SyncGame("restored-guest", opened.GameId, guest.Capability!));
                Assert.Null(after.Error);
                Assert.Equal(before.Revision, after.Revision);
                Assert.Equal(CampaignSaveJson.Canonical(before.Prompt), CampaignSaveJson.Canonical(after.Prompt));
                Assert.Equal(CampaignSaveJson.Canonical(before.World), CampaignSaveJson.Canonical(after.World));
                var retry = restored.Exchange(start);
                Assert.Null(retry.Error);
                Assert.Equal(opened.Capability, retry.Capability);
                Assert.Equal(opened.GameId, retry.GameId);
                Assert.Single(Assert.Single(store.Load()).Campaign!.Save.Attempts);
                var keep = Assert.Single(after.Prompt!.Affordances, offer => offer.Verb == Game.ResolveMulligans);
                var wrongSeat = restored.Exchange(EngineRequest.ResolveGame("wrong-seat", opened.GameId,
                    opened.Capability!, Decision.Take(keep.Id), after.Revision));
                Assert.NotNull(wrongSeat.Error);
                var accepted = restored.Exchange(EngineRequest.ResolveGame("guest-keep", opened.GameId,
                    guest.Capability!, Decision.Take(keep.Id), after.Revision));
                Assert.Null(accepted.Error);
                Assert.True(accepted.Revision > after.Revision);
                var campaign = restored.Exchange(Command(created, "sync", 0));
                Assert.Null(campaign.Error);
                Assert.Equal(CampaignPhase.Playing, campaign.Campaign!.State.Phase);
                Assert.Equal(campaignExpert, campaign.Campaign.State.Expert);
                Assert.Equal(scenarioExpert, campaign.Campaign.State.ExpertScenario);
            }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
    }

    private static EngineRequest Create(DatasetGameFactory factory, bool expert)
    {
        string[] heroes = ["spider_man", "captain_marvel"];
        var catalog = factory.DiscoverDecks();
        return new(EngineProtocol.Version, "create", EngineProtocol.Campaign, "actual-rors",
            Entry: EntryClaim.Create())
        {
            Campaign = new("create")
            {
                Product = factory.CampaignDefinition!.Product, Expert = expert, Seed = 42,
                Heroes = [.. heroes],
                Decks = [.. heroes.Select(hero => new PlayerDeckRecipe(
                    catalog.Heroes.Single(candidate => candidate.Key == hero).Starter))],
            },
        };
    }

    private static EngineRequest Command(EngineResponse created, string action, long revision) =>
        new(EngineProtocol.Version, action, EngineProtocol.Campaign, created.Campaign!.Id,
            Capability: created.Capability, ExpectedRevision: revision) { Campaign = new(action) };
}
