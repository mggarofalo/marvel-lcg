using Marvel.Protocol;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class CampaignContentUpgradeRejectionTests
{
    [Theory]
    [InlineData("proof")]
    [InlineData("attempt")]
    [InlineData("unknown-content")]
    [InlineData("unsupported-replay")]
    public void AnInvalidMemberPreventsTheWholeAggregateUpgradeAndPreservesItsArchive(string change)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, original) =>
        {
            using var store = new FileCampaignStore(Path.Combine(root, "campaigns"));
            var host = new CampaignHost(original, store, new MemorySessionStore());
            var created = host.Exchange(CampaignContentUpgradeTests.Create(original));
            Assert.Null(created.Error);
            var opened = host.Exchange(CampaignContentUpgradeTests.Command(created, "start", 0)
                with { Entry = EntryClaim.Create() });
            Assert.Null(opened.Error);
            var previous = Assert.Single(store.Load()).Campaign!;
            var changed = previous with { Save = Alter(previous.Save, change) };
            store.Commit(changed);
            string before = CampaignSaveJson.Canonical(changed);
            var upgraded = new CampaignHost(DatasetGameFactory.Load(root), store, new MemorySessionStore());
            Assert.Equal("campaign_not_found", upgraded.Exchange(
                CampaignContentUpgradeTests.Command(created, "sync", previous.Save.Revision)).Error?.Code);
            Assert.Equal("session_not_found", upgraded.Exchange(
                EngineRequest.SyncGame("attempt", opened.GameId, opened.Capability!)).Error?.Code);
            Assert.Equal(before, CampaignSaveJson.Canonical(Assert.Single(store.Load()).Campaign));
            Assert.Empty(Directory.GetFiles(Path.Combine(root, "campaigns"), ".upgrade-*", SearchOption.AllDirectories));
        });
    }

    private static CampaignSave Alter(CampaignSave save, string change) => change switch
    {
        "proof" => save with { Journal = [.. save.Journal.Select(entry => entry.Proof is { } proof
            ? entry with { Proof = proof with { Initial = proof.Initial with { StateDigest = "altered proof" } } } : entry)] },
        "attempt" => save with { Attempts = save.Attempts.SetItem(0, save.Attempts[0] with
            { Initial = save.Attempts[0].Initial with { StateDigest = "altered attempt" } }) },
        "unknown-content" => save with { Compatibility = save.Compatibility with { SetupSha256 = new string('a', 64) } },
        "unsupported-replay" => save with { Compatibility = save.Compatibility with { ReplayContract = "unsupported-replay" } },
        _ => throw new ArgumentException("unknown mutation", nameof(change)),
    };
}
