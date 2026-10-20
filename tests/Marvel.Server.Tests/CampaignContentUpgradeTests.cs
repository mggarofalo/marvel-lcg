using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class CampaignContentUpgradeTests
{
    [Fact]
    public void ActualPendingCampaignMigratesItsProofsAndAttemptTogetherAndRetainsOwnerAuthority()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, original) =>
        {
            string directory = Path.Combine(root, "campaigns");
            EngineResponse created;
            EngineResponse opened;
            EngineResponse pending;
            StoredCampaign before;
            using (var store = new FileCampaignStore(directory))
            {
                var host = new CampaignHost(original, store, new MemorySessionStore());
                created = host.Exchange(Create(original));
                Assert.Null(created.Error);
                opened = host.Exchange(Command(created, "start", 0) with { Entry = EntryClaim.Create() });
                Assert.Null(opened.Error);
                var keep = Assert.Single(opened.Prompt!.Affordances, action => action.Verb == Game.ResolveMulligans);
                pending = host.Exchange(EngineRequest.ResolveGame("keep", opened.GameId, opened.Capability!,
                    Decision.Take(keep.Id), opened.Revision));
                Assert.Null(pending.Error);
                before = Assert.Single(store.Load()).Campaign!;
                Assert.NotEmpty(before.Save.Journal);
                Assert.NotNull(Assert.Single(before.Save.Attempts).CurrentPrompt);
            }
            var factory = DatasetGameFactory.Load(root);
            using (var store = new FileCampaignStore(directory))
            {
                var host = new CampaignHost(factory, store, new MemorySessionStore());
                var after = host.Exchange(EngineRequest.SyncGame("after", opened.GameId, opened.Capability!));
                Assert.Null(after.Error);
                Assert.Equal(CampaignSaveJson.Canonical(pending.World), CampaignSaveJson.Canonical(after.World));
                Assert.Equal(CampaignSaveJson.Canonical(pending.Prompt), CampaignSaveJson.Canonical(after.Prompt));
                var campaign = host.Exchange(Command(created, "sync", before.Save.Revision));
                Assert.Null(campaign.Error);
                Assert.Equal(CampaignSaveJson.Canonical(before.Save.State), CampaignSaveJson.Canonical(campaign.Campaign!.State));
                var current = Assert.Single(store.Load()).Campaign!;
                Assert.Equal(factory.Compatibility, current.Save.Compatibility);
                Assert.All(current.Save.Attempts, attempt => Assert.Equal(factory.Compatibility, attempt.Compatibility));
                Assert.All(current.Save.Journal.Where(entry => entry.Proof is not null),
                    entry => Assert.Equal(factory.Compatibility, entry.Proof!.Compatibility));
                Assert.Equal(CampaignSaveJson.Canonical(before), CampaignSaveJson.Canonical(current with
                    { Save = WithCompatibility(current.Save, before.Save.Compatibility) }));
                string backup = Assert.Single(Directory.GetFiles(directory, ".upgrade-*.archive", SearchOption.AllDirectories));
                Assert.Equal(CampaignSaveJson.Canonical(before), File.ReadAllText(backup));
                Assert.Single(Directory.GetFiles(directory, ".upgrade-*.manifest", SearchOption.AllDirectories));
            }
            using (var store = new FileCampaignStore(directory))
            {
                var restarted = new CampaignHost(factory, store, new MemorySessionStore());
                Assert.Null(restarted.Exchange(Command(created, "sync", before.Save.Revision)).Error);
                Assert.Single(Directory.GetFiles(directory, ".upgrade-*.archive", SearchOption.AllDirectories));
            }
        });
    }

    private static CampaignSave WithCompatibility(CampaignSave save, SessionCompatibility compatibility) => save with
    {
        Compatibility = compatibility,
        Attempts = [.. save.Attempts.Select(attempt => attempt with { Compatibility = compatibility })],
        Journal = [.. save.Journal.Select(entry => entry.Proof is { } proof
            ? entry with { Proof = proof with { Compatibility = compatibility } } : entry)],
    };

    internal static EngineRequest Create(DatasetGameFactory factory) =>
        new(EngineProtocol.Version, "create", EngineProtocol.Campaign, "upgrade-campaign", Entry: EntryClaim.Create())
        {
            Campaign = new("create")
            {
                Product = factory.CampaignDefinition!.Product, Seed = 42, Heroes = ["spider_man"],
                Decks = [new PlayerDeckRecipe(factory.DiscoverDecks().Heroes.Single(hero => hero.Key == "spider_man").Starter)],
            },
        };

    internal static EngineRequest Command(EngineResponse created, string action, long revision) =>
        new(EngineProtocol.Version, action, EngineProtocol.Campaign, created.Campaign!.Id,
            Capability: created.Capability, ExpectedRevision: revision) { Campaign = new(action) };
}
