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

public sealed class PlayedCampaignReplayUpgradeTests
{
    [Theory]
    [InlineData("v19")]
    [InlineData("v20")]
    public void PlayedCampaignUpgradesAllProofsAndRestartsWithExactAuthorityAndCurrentPrompt(string version)
    {
        WithFixture(version, (fixture, factory) =>
        {
            string storage = Path.Combine(fixture, "campaigns");
            StoredCampaign original;
            using (var store = new FileCampaignStore(storage))
            {
                original = Assert.Single(store.Load()).Campaign!;
                AssertPlayedRecords(original.Save);
                Assert.Equal("engine-replay-" + version, original.Save.Compatibility.ReplayContract);
                var host = new CampaignHost(factory, store, new MemorySessionStore());
                AssertResumed(host, fixture);
                var current = Assert.Single(store.Load()).Campaign!;
                AssertCurrentCompatibility(current.Save, factory.Compatibility);
                Assert.Equal(CampaignSaveJson.Canonical(original), CampaignSaveJson.Canonical(current with
                    { Save = Normalize(current.Save, original.Save, remapIdentity: false) }));
                Assert.Equal(CampaignSaveJson.Canonical(original), File.ReadAllText(Assert.Single(
                    Directory.GetFiles(storage, ".upgrade-*.archive", SearchOption.AllDirectories),
                        path => File.ReadAllText(path) == CampaignSaveJson.Canonical(original))));
                Assert.Equal(version == "v19" ? 1 : 2, Directory.GetFiles(storage, ".upgrade-*.manifest", SearchOption.AllDirectories).Length);
            }
            StoredCampaign continued;
            using (var store = new FileCampaignStore(storage))
            {
                var restarted = new CampaignHost(factory, store, new MemorySessionStore());
                AssertResumed(restarted, fixture);
                AssertPlayedRecords(Assert.Single(store.Load()).Campaign!.Save);
                string control = Path.Combine(Path.GetDirectoryName(fixture)!, "control");
                var next = EngineJson.ReadRequest(File.ReadAllBytes(Path.Combine(control, "continuation-request.json")))
                    with { Version = EngineProtocol.Version };
                Equivalent(ReadResponse(control, "continuation-response.json"), restarted.Exchange(next));
                continued = Assert.Single(store.Load()).Campaign!;
                Assert.Equal(version == "v19" ? 1 : 2, Directory.GetFiles(storage, ".upgrade-*.archive", SearchOption.AllDirectories).Length);
            }
            using (var store = new FileCampaignStore(storage))
            {
                var restarted = new CampaignHost(factory, store, new MemorySessionStore());
                string control = Path.Combine(Path.GetDirectoryName(fixture)!, "control");
                var expected = ReadResponse(control, "continuation-response.json");
                var request = EngineJson.ReadRequest(File.ReadAllBytes(Path.Combine(control, "continuation-request.json")));
                var actual = restarted.Exchange(EngineRequest.SyncGame("restart-continued", request.GameId, request.Capability!));
                Assert.Null(actual.Error);
                Assert.Equal(expected.Revision, actual.Revision);
                EqualJson(expected.World, actual.World);
                EqualJson(expected.Prompt, actual.Prompt);
                EqualJson(expected.History, actual.History);
                Assert.Equal(CampaignSaveJson.Canonical(continued), CampaignSaveJson.Canonical(Assert.Single(store.Load()).Campaign));
                Assert.Equal(version == "v19" ? 1 : 2, Directory.GetFiles(storage, ".upgrade-*.archive", SearchOption.AllDirectories).Length);
            }
        });
    }

    [Theory]
    [InlineData("v19")]
    [InlineData("v20")]
    public void PlayedExportImportsWithEveryProofIntactAndFreshPersistentAuthority(string version)
    {
        WithFixture(version, (fixture, factory) =>
        {
            var original = CampaignSaveJson.Read(File.ReadAllBytes(Path.Combine(fixture, "campaign.json")));
            string sourceBytes = CampaignSaveJson.Canonical(original);
            string storage = Path.Combine(fixture, "imported");
            EngineResponse imported;
            using (var store = new FileCampaignStore(storage))
            {
                var host = new CampaignHost(factory, store, new MemorySessionStore());
                imported = host.Exchange(new(EngineProtocol.Version, "import-played-v19", EngineProtocol.Campaign,
                    "Imported predecessor", Entry: EntryClaim.Create())
                { Campaign = new("import") { Transfer = File.ReadAllText(Path.Combine(fixture, "campaign-transfer.txt")) } });
                Assert.Null(imported.Error);
                var copy = Assert.Single(store.Load()).Campaign!.Save;
                AssertCurrentCompatibility(copy, factory.Compatibility);
                AssertPlayedRecords(copy);
                Assert.NotEqual(original.Identity.StorageId, copy.Identity.StorageId);
                Assert.Equal(sourceBytes, CampaignSaveJson.Canonical(Normalize(copy, original, remapIdentity: true)));
                using var access = JsonDocument.Parse(File.ReadAllText(Path.Combine(fixture, "access.json")));
                Assert.NotEqual(access.RootElement.GetProperty("owner").GetString(), imported.Capability);
                Assert.NotNull(host.Exchange(EngineRequest.SyncGame("old-capability", imported.CampaignGame!.GameId,
                    access.RootElement.GetProperty("capability").GetString()!)).Error);
                AssertImportedPrompt(host, imported, fixture);
            }
            using (var store = new FileCampaignStore(storage))
            {
                var restarted = new CampaignHost(factory, store, new MemorySessionStore());
                AssertImportedPrompt(restarted, imported, fixture);
            }
            Assert.Equal(sourceBytes, CampaignSaveJson.Canonical(CampaignSaveJson.Read(
                File.ReadAllBytes(Path.Combine(fixture, "campaign.json")))));
        });
    }

    private static void AssertResumed(CampaignHost host, string fixture)
    {
        using var access = JsonDocument.Parse(File.ReadAllText(Path.Combine(fixture, "access.json")));
        var values = access.RootElement;
        var expected = ReadResponse(fixture, "resumed-wire-response.json");
        Equivalent(expected, host.Exchange(EngineRequest.SyncGame(expected.RequestId,
            values.GetProperty("game").GetString()!, values.GetProperty("capability").GetString()!)));
        expected = ReadResponse(fixture, "owner-wire-response.json");
        Equivalent(expected, host.Exchange(new(EngineProtocol.Version, expected.RequestId, EngineProtocol.Campaign,
            values.GetProperty("id").GetString()!, Capability: values.GetProperty("owner").GetString())
            { Campaign = new("sync") }));
    }

    private static void AssertImportedPrompt(CampaignHost host, EngineResponse imported, string fixture)
    {
        var actual = host.Exchange(EngineRequest.SyncGame("resumed-copy", imported.CampaignGame!.GameId,
            imported.CampaignGame.Capability));
        Assert.Null(actual.Error);
        var expected = ReadResponse(fixture, "resumed-wire-response.json");
        EqualJson(expected.World, actual.World);
        EqualJson(expected.Prompt, actual.Prompt);
    }

    private static void AssertPlayedRecords(CampaignSave save)
    {
        Assert.False(save.State.Expert);
        Assert.Equal(CampaignStage.Taskmaster, save.State.Stage);
        Assert.Equal(13, save.Attempts.Length);
        Assert.Equal(786, save.Attempts.Sum(attempt => attempt.Units.Sum(unit => unit.Decisions.Count)));
        Assert.Contains("04073", save.State.ExperimentalWeapons);
        Assert.Contains("tech:04155", save.State.RemovedCardKeys);
        var player = Assert.Single(save.State.Players);
        Assert.DoesNotContain(player.Cards, card => card.Key == "tech:04155");
        Assert.Contains(player.Cards, card => card.Key == "condition:04159" && card.Face == "04159a");
        Assert.Equal(2, player.ConditionHitPointBonus);
        Assert.NotNull(save.Attempts[^1].CurrentPrompt);
        Assert.Contains(save.Journal, entry => entry.Proof is not null);
    }

    private static void AssertCurrentCompatibility(CampaignSave save, SessionCompatibility target)
    {
        Assert.Equal("engine-replay-v21", target.ReplayContract);
        Assert.Equal(target, save.Compatibility);
        Assert.All(save.Attempts, attempt => Assert.Equal(target, attempt.Compatibility));
        Assert.All(save.Journal.Where(entry => entry.Proof is not null), entry => Assert.Equal(target, entry.Proof!.Compatibility));
    }

    private static CampaignSave Normalize(CampaignSave current, CampaignSave previous, bool remapIdentity)
    {
        Assert.Equal(previous.Attempts.Length, current.Attempts.Length);
        Assert.Equal(previous.Journal.Length, current.Journal.Length);
        return current with
        {
            Identity = remapIdentity ? previous.Identity : current.Identity,
            Compatibility = previous.Compatibility,
            Attempts = [.. current.Attempts.Zip(previous.Attempts, (copy, original) => copy with
                { Session = remapIdentity ? original.Session : copy.Session, Compatibility = original.Compatibility })],
            Journal = [.. current.Journal.Zip(previous.Journal, (copy, original) => copy.Proof is { } proof
                ? copy with { Proof = proof with { Compatibility = original.Proof!.Compatibility,
                    Session = remapIdentity ? original.Proof.Session : proof.Session } } : copy)],
        };
    }

    private static EngineResponse ReadResponse(string root, string name) => EngineJson.ReadResponse(File.ReadAllBytes(Path.Combine(root, name)));

    private static void EqualJson<T>(T expected, T actual) =>
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(CampaignSaveJson.Canonical(expected)),
            JsonNode.Parse(CampaignSaveJson.Canonical(actual))), "JSON values differ");

    private static void Equivalent(EngineResponse expected, EngineResponse actual)
    {
        Assert.Null(actual.Error);
        // Transport version may advance independently; every gameplay, authority and visibility field is compared.
        expected = expected with { Version = EngineProtocol.Version };
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(EngineJson.Write(expected)), JsonNode.Parse(EngineJson.Write(actual))),
            "complete predecessor response differs after upgrade");
    }

    private static void WithFixture(string version, Action<string, DatasetGameFactory> check) =>
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            string archive = Path.Combine(RepositoryPaths.Root, "tests", "Marvel.Server.Tests", "Fixtures", version == "v19" ? "played-campaign-91098022.zip" : "replay-v20-9097da07.zip");
            Assert.Equal(version == "v19"
                ? "1199392F3439FE89B6A21AA03D195DF18BEF1CC70F20DD65A998A2E32B29FFE8"
                : "76ABF6271E0C8B1552A2EA220CA3A7710E9D0EE6765BDFBA0476933062C2E511", Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive))));
            string extraction = Path.Combine(root, "played-predecessor");
            ZipFile.ExtractToDirectory(archive, extraction);
            string fixture = Path.GetDirectoryName(Assert.Single(Directory.GetFiles(version == "v19" ? extraction : Path.Combine(extraction, "played"), "access.json", SearchOption.AllDirectories)))!;
            check(fixture, DatasetGameFactory.Load(root));
        });
}
