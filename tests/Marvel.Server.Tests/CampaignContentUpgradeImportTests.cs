using Marvel.Protocol;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class CampaignContentUpgradeImportTests
{
    [Fact]
    public void PredecessorExportMigratesOnceWithoutChangingItsTraceOrRetainingOldAuthority()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, original) =>
        {
            var originals = new MemoryCampaignStore();
            var oldHost = new CampaignHost(original, originals, new MemorySessionStore());
            var owner = oldHost.Exchange(CampaignContentUpgradeTests.Create(original));
            var started = oldHost.Exchange(CampaignContentUpgradeTests.Command(owner, "start", 0)
                with { Entry = EntryClaim.Create() });
            Assert.Null(started.Error);
            var before = Assert.Single(originals.Load()).Campaign!.Save;
            string transfer = oldHost.Exchange(CampaignContentUpgradeTests.Command(owner, "export",
                before.Revision)).CampaignTransfer!;
            string originalBytes = CampaignSaveJson.Canonical(before);
            var target = DatasetGameFactory.Load(root);
            var copies = new MemoryCampaignStore();
            var host = new CampaignHost(target, copies, new MemorySessionStore());
            var request = Import(transfer);
            var imported = host.Exchange(request);
            Assert.Null(imported.Error);
            var copy = Assert.Single(copies.Load()).Campaign!.Save;
            Assert.Equal(target.Compatibility, copy.Compatibility);
            Assert.NotEqual(before.Identity.StorageId, copy.Identity.StorageId);
            Assert.NotEqual(owner.Capability, imported.Capability);
            Assert.Equal(before.Attempts.Length, copy.Attempts.Length);
            Assert.Equal(before.Journal.Length, copy.Journal.Length);
            Assert.Equal(originalBytes, CampaignSaveJson.Canonical(NormalizeIdentity(copy, before)));
            var resumed = host.Exchange(EngineRequest.SyncGame("resume", imported.CampaignGame!.GameId,
                imported.CampaignGame.Capability));
            Assert.Null(resumed.Error);
            Assert.Equal(CampaignSaveJson.Canonical(started.World), CampaignSaveJson.Canonical(resumed.World));
            Assert.Equal(CampaignSaveJson.Canonical(started.Prompt), CampaignSaveJson.Canonical(resumed.Prompt));
            Assert.NotNull(host.Exchange(EngineRequest.SyncGame("old-authority", imported.CampaignGame.GameId,
                started.Capability!)).Error);
            Assert.Equal(originalBytes, CampaignSaveJson.Canonical(Assert.Single(originals.Load()).Campaign!.Save));
            host = new CampaignHost(target, copies, new MemorySessionStore());
            var retried = host.Exchange(request);
            Assert.Null(retried.Error);
            Assert.Equal(imported.Campaign!.Id, retried.Campaign!.Id);
            Assert.Equal(imported.CampaignGame, retried.CampaignGame);
            Assert.Single(copies.Load());
        });
    }

    [Fact]
    public void CorruptedPredecessorTransferPublishesNeitherCampaignNorAttempt()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, original) =>
        {
            var oldStore = new MemoryCampaignStore();
            var oldHost = new CampaignHost(original, oldStore, new MemorySessionStore());
            var owner = oldHost.Exchange(CampaignContentUpgradeTests.Create(original));
            Assert.Null(oldHost.Exchange(CampaignContentUpgradeTests.Command(owner, "start", 0)
                with { Entry = EntryClaim.Create() }).Error);
            var before = Assert.Single(oldStore.Load()).Campaign!.Save;
            string transfer = oldHost.Exchange(CampaignContentUpgradeTests.Command(owner, "export",
                before.Revision)).CampaignTransfer!;
            string[] envelope = transfer.Split('\n');
            envelope[1] = new string('0', 64);
            var campaigns = new MemoryCampaignStore();
            var sessions = new MemorySessionStore();
            var host = new CampaignHost(DatasetGameFactory.Load(root), campaigns, sessions);
            Assert.NotNull(host.Exchange(Import(string.Join('\n', envelope))).Error);
            Assert.Empty(campaigns.Load());
            Assert.Empty(sessions.Load());
            Assert.Equal(CampaignSaveJson.Canonical(before),
                CampaignSaveJson.Canonical(Assert.Single(oldStore.Load()).Campaign!.Save));
        });
    }

    private static CampaignSave NormalizeIdentity(CampaignSave copy, CampaignSave original) => copy with
    {
        Identity = original.Identity,
        Compatibility = original.Compatibility,
        Attempts = [.. copy.Attempts.Zip(original.Attempts, (current, previous) =>
            current with { Session = previous.Session, Compatibility = previous.Compatibility })],
        Journal = [.. copy.Journal.Zip(original.Journal, (current, previous) => current.Proof is { } proof
            ? current with { Proof = proof with
                { Session = previous.Proof!.Session, Compatibility = previous.Proof.Compatibility } }
            : current)],
    };

    private static EngineRequest Import(string transfer) =>
        new(EngineProtocol.Version, "import-upgrade", EngineProtocol.Campaign, "copy", Entry: EntryClaim.Create())
        { Campaign = new("import") { Transfer = transfer } };
}
