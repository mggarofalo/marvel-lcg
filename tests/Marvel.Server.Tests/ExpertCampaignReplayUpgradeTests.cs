using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ExpertCampaignReplayUpgradeTests
{
    [Fact]
    public void StackedPreventionDivergenceQuarantinesTheExpertCampaignWithoutChangingSourceFiles()
    {
        WithFixture((published, factory) =>
        {
            string storage = Path.Combine(published, "campaigns");
            var before = Snapshot(storage);
            using var access = JsonDocument.Parse(File.ReadAllText(Path.Combine(published, "access.json")));
            for (int restart = 0; restart < 2; restart++)
            {
                using var store = new FileCampaignStore(storage);
                var original = Assert.Single(store.Load()).Campaign!;
                Assert.True(original.Save.State.Expert);
                Assert.Equal(CampaignStage.AbsorbingMan, original.Save.State.Stage);
                Assert.Equal(7, original.Save.Attempts.Length);
                Assert.Equal(395, original.Save.Attempts.Sum(attempt => attempt.Units.Sum(unit => unit.Decisions.Count)));
                Assert.Equal(7, Assert.Single(original.Save.State.Players).RecordedHitPoints);
                Assert.Contains("04072", original.Save.State.ExperimentalWeapons);
                Assert.Contains(original.Save.State.Players[0].Cards, card => card.Face == "04155");
                Assert.Equal("engine-replay-v19", original.Save.Compatibility.ReplayContract);
                var route = Assert.IsType<SessionContentUpgrade>(factory.ContentUpgrade(original.Save.Compatibility));
                var failure = Assert.Throws<ReplayDivergenceException>(() => route.Verify(original.Save,
                    factory.CampaignDefinition!, setup => ReplayContractUpgradeTests.Open(factory, setup),
                    factory.InspectCampaign));
                Assert.Equal("unit 31 decision 3 events diverged: expected 10, got 11", failure.Message);
                if (restart == 0) AssertPreventionCause(original.Save, factory);
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

    [Fact]
    public void DivergentExpertTransferCannotPublishAnImportedCopy()
    {
        WithFixture((published, factory) =>
        {
            var before = Snapshot(published);
            using var store = new FileCampaignStore(Path.Combine(Path.GetDirectoryName(published)!, "imported"));
            var host = new CampaignHost(factory, store, new MemorySessionStore());
            var imported = host.Exchange(new(EngineProtocol.Version, "import", EngineProtocol.Campaign,
                "Expert import", Entry: EntryClaim.Create())
                { Campaign = new("import") { Transfer = File.ReadAllText(Path.Combine(published, "campaign-transfer.txt")) } });
            Assert.Equal("campaign_rejected", imported.Error?.Code);
            Assert.Empty(store.Load());
            Assert.Equal(before, Snapshot(published));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnaffectedAbsorbingManAttemptUpgradesAndCanResolveItsInitialRecovery(bool recover)
    {
        WithFixture((published, factory) =>
        {
            var campaign = CampaignSaveJson.Read(File.ReadAllBytes(Path.Combine(published, "campaign.json")));
            var original = campaign.Attempts[^1];
            Assert.Equal("absorbing_man", original.Setup.Scenario);
            Assert.Empty(original.Units);
            string bytes = SessionSaveJson.Write(original);
            var route = Assert.IsType<SessionContentUpgrade>(factory.ContentUpgrade(original.Compatibility));
            var candidate = route.Verify(original, setup => ReplayContractUpgradeTests.Open(factory, setup));
            Assert.Equal(factory.Compatibility, candidate.Compatibility);
            Assert.Equal(bytes, SessionSaveJson.Write(candidate with { Compatibility = original.Compatibility }));
            Assert.Equal(bytes, SessionSaveJson.Write(original));
            var game = SessionReplay.Verify(candidate, factory.Compatibility,
                setup => ReplayContractUpgradeTests.Open(factory, setup));
            var request = EngineJson.ReadRequest(File.ReadAllBytes(Path.Combine(published,
                recover ? "recovery-request.json" : "decline-request.json")));
            Assert.Equal("CampaignRecovery", game.Pending!.Trigger);
            long damageBefore = game.State.Seats[0].IdentityCard.Damage;
            long rngBefore = game.State.Random.Generator.WordsConsumed;
            _ = game.Resolve(request.Decision!);
            Assert.NotNull(game.Pending);
            Assert.NotEqual("CampaignRecovery", game.Pending.Trigger);
            Assert.Equal(recover ? 0 : damageBefore, game.State.Seats[0].IdentityCard.Damage);
            Assert.Equal(recover, game.State.Random.Generator.WordsConsumed > rngBefore);
        });
    }

    private static void AssertPreventionCause(CampaignSave campaign, DatasetGameFactory factory)
    {
        var proof = campaign.Journal[13].Proof!;
        Assert.Equal("crossbones", proof.Setup.Scenario);
        Assert.Equal(4, proof.Setup.Campaign!.Attempt);
        var game = ReplayContractUpgradeTests.Open(factory, proof.Setup).Game;
        for (int unit = 0; unit <= 31; unit++)
        {
            int decisions = unit == 31 ? 2 : proof.Units[unit].Decisions.Count;
            for (int decision = 0; decision < decisions; decision++)
            {
                var step = proof.Units[unit].Decisions[decision];
                _ = game.Resolve(step.Decision.Resolve(game.Pending!));
            }
        }
        // Both Backflips (unit 28) commit prevention before the defended Crossbones attack.
        // Each registration belongs to that damage; neither can protect a later minion attack.
        var preventions = game.State.Effects.Active().Where(effect => effect.Kind == "preventDamage").ToArray();
        Assert.Equal(new int?[] { 9, 10 }, preventions.Select(effect => effect.Card));
        Assert.All(preventions, effect => Assert.Equal(1, effect.Lasts!.Uses));
        var defended = proof.Units[31].Decisions[2];
        _ = game.Resolve(defended.Decision.Resolve(game.Pending!));
        Assert.DoesNotContain(game.State.Effects.Active(), effect => effect.Kind == "preventDamage");
        var mercenary = proof.Units[31].Decisions[3];
        Assert.Equal("01182", mercenary.Decision.Selector.Label);
        Assert.DoesNotContain(mercenary.Events, happened => happened.GetProperty("kind").GetString() == "FieldSet"
            && happened.GetProperty("verb").GetString() == "Deal_Damage"
            && happened.GetProperty("attack_identity").GetProperty("id").GetInt32() == 10);
        var result = game.Resolve(mercenary.Decision.Resolve(game.Pending!));
        var damage = result.Events.Select(JournalJson.Event).First(happened =>
            happened.GetProperty("kind").GetString() == "FieldSet"
            && happened.GetProperty("verb").GetString() == "Deal_Damage");
        Assert.Equal(7, damage.GetProperty("from").GetInt64());
        Assert.Equal(5, damage.GetProperty("to").GetInt64());
    }

    private static string[] Snapshot(string directory) => Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
        .Where(path => !path.EndsWith(".writer", StringComparison.Ordinal))
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => Path.GetRelativePath(directory, path) + ":" +
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))).ToArray();

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
