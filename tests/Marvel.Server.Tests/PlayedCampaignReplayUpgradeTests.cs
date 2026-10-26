using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Marvel.Protocol;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PlayedCampaignReplayUpgradeTests
{
    [Theory]
    [InlineData("v19")]
    [InlineData("v20")]
    public void NestedRevealDivergenceRejectsTheWholePlayedCampaignWithoutChangingAnySourceFile(string version)
    {
        WithFixture(version, (fixture, factory) =>
        {
            string storage = Path.Combine(fixture, "campaigns");
            var before = Snapshot(storage);
            using var access = JsonDocument.Parse(File.ReadAllText(Path.Combine(fixture, "access.json")));
            for (int restart = 0; restart < 2; restart++)
            {
                using var store = new FileCampaignStore(storage);
                var original = Assert.Single(store.Load()).Campaign!;
                Assert.Equal(13, original.Save.Attempts.Length);
                Assert.Equal(786, original.Save.Attempts.Sum(attempt => attempt.Units.Sum(unit => unit.Decisions.Count)));
                var route = Assert.IsType<SessionContentUpgrade>(factory.ContentUpgrade(original.Save.Compatibility));
                var failure = Assert.Throws<ReplayDivergenceException>(() => route.Verify(original.Save,
                    factory.CampaignDefinition!, setup => ReplayContractUpgradeTests.Open(factory, setup),
                    factory.InspectCampaign));
                Assert.Equal("unit 7 decision 2 events diverged: expected 18, got 14", failure.Message);
                var host = new CampaignHost(factory, store, new MemorySessionStore());
                Assert.Equal("campaign_not_found", host.Exchange(new(EngineProtocol.Version, "owner",
                    EngineProtocol.Campaign, access.RootElement.GetProperty("id").GetString()!,
                    Capability: access.RootElement.GetProperty("owner").GetString())
                    { Campaign = new("sync") }).Error?.Code);
                Assert.Equal("session_not_found", host.Exchange(EngineRequest.SyncGame("game",
                    access.RootElement.GetProperty("game").GetString()!,
                    access.RootElement.GetProperty("capability").GetString()!)).Error?.Code);
                Assert.Equal(before, Snapshot(storage));
                Assert.Equal(CampaignSaveJson.Canonical(original),
                    CampaignSaveJson.Canonical(Assert.Single(store.Load()).Campaign));
            }
        });
    }

    [Theory]
    [InlineData("v19")]
    [InlineData("v20")]
    public void DivergentPlayedExportCannotPublishAnImportedCopy(string version)
    {
        WithFixture(version, (fixture, factory) =>
        {
            var source = Snapshot(fixture);
            using var store = new FileCampaignStore(Path.Combine(Path.GetDirectoryName(fixture)!, "imported"));
            var host = new CampaignHost(factory, store, new MemorySessionStore());
            var result = host.Exchange(new(EngineProtocol.Version, "import", EngineProtocol.Campaign,
                "Imported predecessor", Entry: EntryClaim.Create())
                { Campaign = new("import") { Transfer = File.ReadAllText(Path.Combine(fixture, "campaign-transfer.txt")) } });
            Assert.Equal("campaign_rejected", result.Error?.Code);
            Assert.Empty(store.Load());
            Assert.Equal(source, Snapshot(fixture));
        });
    }

    [Theory]
    [InlineData("v19")]
    [InlineData("v20")]
    public void UnaffectedAuthenticPlayedTaskmasterAttemptStillUpgradesExactly(string version)
    {
        WithFixture(version, (fixture, factory) =>
        {
            var campaign = CampaignSaveJson.Read(File.ReadAllBytes(Path.Combine(fixture, "campaign.json")));
            var original = campaign.Attempts[^1];
            Assert.Equal("taskmaster", original.Setup.Scenario);
            Assert.Equal(5, original.Units.Sum(unit => unit.Decisions.Count));
            string bytes = SessionSaveJson.Write(original);
            var route = Assert.IsType<SessionContentUpgrade>(factory.ContentUpgrade(original.Compatibility));
            var candidate = route.Verify(original, setup => ReplayContractUpgradeTests.Open(factory, setup));
            Assert.Equal("engine-replay-v26", candidate.Compatibility.ReplayContract);
            Assert.Equal(bytes, SessionSaveJson.Write(candidate with { Compatibility = original.Compatibility }));
            Assert.Equal(bytes, SessionSaveJson.Write(original));
            _ = SessionReplay.Verify(candidate, factory.Compatibility,
                setup => ReplayContractUpgradeTests.Open(factory, setup));
            Assert.Throws<SessionCompatibilityException>(() => SessionReplay.Verify(original,
                factory.Compatibility, setup => ReplayContractUpgradeTests.Open(factory, setup)));
        });
    }

    private static string[] Snapshot(string directory) => Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
        .Where(path => !path.EndsWith(".writer", StringComparison.Ordinal))
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => Path.GetRelativePath(directory, path) + ":" +
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))).ToArray();

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
