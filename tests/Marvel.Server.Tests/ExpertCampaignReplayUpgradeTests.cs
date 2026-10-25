using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ExpertCampaignReplayUpgradeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenuineExpertPredecessorPreservesEveryProofAndCanCommitItsInitialRecovery(bool recover)
    {
        WithFixture((published, factory) =>
        {
            string storage = Path.Combine(published, "campaigns");
            StoredCampaign original;
            StoredCampaign continued;
            var request = EngineJson.ReadRequest(File.ReadAllBytes(Path.Combine(published,
                recover ? "recovery-request.json" : "decline-request.json"))) with { Version = EngineProtocol.Version };
            EngineResponse resolved;
            using (var store = new FileCampaignStore(storage))
            {
                original = Assert.Single(store.Load()).Campaign!;
                Assert.True(original.Save.State.Expert);
                Assert.Equal(CampaignStage.AbsorbingMan, original.Save.State.Stage);
                Assert.Equal(7, original.Save.Attempts.Length);
                Assert.Equal(395, original.Save.Attempts.Sum(attempt => attempt.Units.Sum(unit => unit.Decisions.Count)));
                Assert.Equal(7, Assert.Single(original.Save.State.Players).RecordedHitPoints);
                Assert.Contains("04072", original.Save.State.ExperimentalWeapons);
                Assert.Contains(original.Save.State.Players[0].Cards, card => card.Face == "04155");
                Assert.Equal("engine-replay-v19", original.Save.Compatibility.ReplayContract);
                Assert.Empty(original.Save.Attempts[^1].Units);
                var setup = original.Save.Attempts[^1].Setup;
                var control = factory.Create(new(setup.Scenario, setup.Heroes, setup.ModularSets,
                    setup.Seed, setup.PlayerDecks, setup.Campaign)).Game;
                var pending = control.Pending!;
                var answer = control.Resolve(request.Decision!);
                var expectedStep = JournalStep.From(pending.Player, pending, request.Decision!, answer.Events,
                    control.State.Random.Generator.WordsConsumed, SessionReplay.Fingerprint(control), SessionReplay.Result(control));
                var host = new CampaignHost(factory, store, new MemorySessionStore());
                var resumed = host.Exchange(EngineRequest.SyncGame("resume", request.GameId, request.Capability!));
                Assert.Null(resumed.Error);
                var oldResponse = EngineJson.ReadResponse(File.ReadAllBytes(Path.Combine(published, "resumed-wire-response.json")));
                Equal(oldResponse.Prompt, resumed.Prompt);
                Equal(oldResponse.World, resumed.World);
                var upgraded = Assert.Single(store.Load()).Campaign!;
                AssertCompatibility(upgraded.Save, factory.Compatibility);
                Equal(original, upgraded with { Save = Normalize(upgraded.Save, original.Save) });
                AssertOriginalBackup(storage, original);
                resolved = host.Exchange(request);
                Assert.Null(resolved.Error);
                Assert.Equal(1, resolved.Revision);
                continued = Assert.Single(store.Load()).Campaign!;
                Assert.Single(continued.Save.Attempts[^1].Units);
                Assert.Single(continued.Save.Attempts[^1].Units[0].Decisions);
                Equal(expectedStep, continued.Save.Attempts[^1].Units[0].Decisions[0]);
                AssertOriginalBackup(storage, original);
            }
            using (var store = new FileCampaignStore(storage))
            {
                var restarted = new CampaignHost(factory, store, new MemorySessionStore());
                var resumed = restarted.Exchange(EngineRequest.SyncGame("restart", request.GameId, request.Capability!));
                Assert.Null(resumed.Error);
                Assert.Equal(resolved.Revision, resumed.Revision);
                Equal(resolved.Prompt, resumed.Prompt);
                Equal(resolved.World, resumed.World);
                Equal(continued, Assert.Single(store.Load()).Campaign);
                AssertOriginalBackup(storage, original);
            }
        });
    }

    [Fact]
    public void ExpertTransferImportsWithFreshAuthorityAndRetainsEveryRecordedAttempt()
    {
        WithFixture((published, factory) =>
        {
            var original = CampaignSaveJson.Read(File.ReadAllBytes(Path.Combine(published, "campaign.json")));
            string storage = Path.Combine(published, "imported");
            EngineResponse imported;
            using (var store = new FileCampaignStore(storage))
            {
                var host = new CampaignHost(factory, store, new MemorySessionStore());
                imported = host.Exchange(new(EngineProtocol.Version, "import", EngineProtocol.Campaign,
                    "Expert import", Entry: EntryClaim.Create())
                { Campaign = new("import") { Transfer = File.ReadAllText(Path.Combine(published, "campaign-transfer.txt")) } });
                Assert.Null(imported.Error);
                var copy = Assert.Single(store.Load()).Campaign!.Save;
                AssertCompatibility(copy, factory.Compatibility);
                Assert.NotEqual(original.Identity.StorageId, copy.Identity.StorageId);
                Equal(original, Normalize(copy, original, remapIdentity: true));
                using var access = JsonDocument.Parse(File.ReadAllText(Path.Combine(published, "access.json")));
                Assert.NotEqual(access.RootElement.GetProperty("owner").GetString(), imported.Capability);
                Assert.Equal("session_not_found", host.Exchange(EngineRequest.SyncGame("old-authority",
                    imported.CampaignGame!.GameId, access.RootElement.GetProperty("capability").GetString()!)).Error!.Code);
            }
            using (var store = new FileCampaignStore(storage))
            {
                var host = new CampaignHost(factory, store, new MemorySessionStore());
                var resumed = host.Exchange(EngineRequest.SyncGame("resume", imported.CampaignGame!.GameId,
                    imported.CampaignGame.Capability));
                Assert.Null(resumed.Error);
                var expected = EngineJson.ReadResponse(File.ReadAllBytes(Path.Combine(published, "resumed-wire-response.json")));
                Equal(expected.Prompt, resumed.Prompt);
                Equal(expected.World, resumed.World);
            }
        });
    }

    private static void AssertCompatibility(CampaignSave save, SessionCompatibility target)
    {
        Assert.Equal(target, save.Compatibility);
        Assert.All(save.Attempts, attempt => Assert.Equal(target, attempt.Compatibility));
        Assert.All(save.Journal.Where(entry => entry.Proof is not null), entry => Assert.Equal(target, entry.Proof!.Compatibility));
    }

    private static CampaignSave Normalize(CampaignSave copy, CampaignSave original, bool remapIdentity = false)
    {
        Assert.Equal(original.Attempts.Length, copy.Attempts.Length);
        Assert.Equal(original.Journal.Length, copy.Journal.Length);
        return copy with
        {
            Identity = remapIdentity ? original.Identity : copy.Identity,
            Compatibility = original.Compatibility,
            Attempts = [.. copy.Attempts.Zip(original.Attempts, (current, old) => current with
                { Compatibility = old.Compatibility, Session = remapIdentity ? old.Session : current.Session })],
            Journal = [.. copy.Journal.Zip(original.Journal, (current, old) => current.Proof is { } proof
                ? current with { Proof = proof with { Compatibility = old.Proof!.Compatibility,
                    Session = remapIdentity ? old.Proof.Session : proof.Session } } : current)],
        };
    }

    private static void AssertOriginalBackup(string storage, StoredCampaign original) =>
        Assert.Equal(CampaignSaveJson.Canonical(original), File.ReadAllText(Assert.Single(
            Directory.GetFiles(storage, ".upgrade-*.archive", SearchOption.AllDirectories))));

    private static void Equal<T>(T expected, T actual) =>
        Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(expected), JsonSerializer.SerializeToNode(actual)),
            "JSON values differ");

    private static void WithFixture(Action<string, DatasetGameFactory> check) =>
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            string archive = Path.Combine(RepositoryPaths.Root, "tests", "Marvel.Server.Tests", "Fixtures",
                "expert-campaign-91098022.zip");
            Assert.Equal("1A7B953709EE3E390863BFD28A6C920EE00B53F7F24853B197013AA72903538B",
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive))));
            string extraction = Path.Combine(root, "expert-predecessor");
            ZipFile.ExtractToDirectory(archive, extraction);
            check(Path.Combine(extraction, "published"), DatasetGameFactory.Load(root));
        });
}


