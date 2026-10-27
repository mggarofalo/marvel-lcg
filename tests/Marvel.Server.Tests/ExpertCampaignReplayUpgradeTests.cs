using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ExpertCampaignReplayUpgradeTests
{
    [Fact]
    [Rule("rr:status-cards.2")]
    [Rule("rr:confuse-confused.5")]
    [Rule("rr:confuse-confused.7")]
    public void ConfusedThwartDivergenceQuarantinesTheExpertCampaignWithoutChangingSourceFiles()
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
                if (restart == 0) AssertConfusedThwartCause(original.Save, factory);
                Assert.Equal("unit 25 decision 0 events diverged: expected 3, got 6", failure.Message);
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

    private static void AssertConfusedThwartCause(CampaignSave campaign, DatasetGameFactory factory)
    {
        var proof = campaign.Journal[4].Proof!;
        Assert.Equal("crossbones", proof.Setup.Scenario);
        Assert.Equal(1, proof.Setup.Campaign!.Attempt);
        var game = FrozenReplayPrefixAssertions.Before(proof, factory, 25, 0);
        var step = proof.Units[25].Decisions[0];
        Assert.Equal("For Justice!", step.Decision.Selector.Label);
        Assert.Equal(25, step.Decision.Selector.AnchorId);
        Assert.Empty(step.Decision.Targets);
        Assert.Equal(42, Assert.Single(step.Decision.Resources));
        var world = game.State;
        Assert.Equal("01060", world.Cards[25].FaceId);
        Assert.Equal("01088", world.Cards[42].FaceId);
        var confused = Assert.Single(Statuses.On(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(99, confused.ObjectId);
        var threats = world.Cards.Select(card => card.Tokens.GetValueOrDefault("k_threat")).ToArray();
        long rng = world.Random.Generator.WordsConsumed;
        // "Status card abilities have timing priority over all conflicting triggered abilities."
        // "Costs associated with the thwart attempt ... must still be paid."
        // The engine chooses to settle this replacement before an effect-target prompt.
        var result = game.Resolve(step.Decision.Resolve(game.Pending!));
        var events = result.Events.Select(JournalJson.Event).ToArray();
        Assert.Equal(6, events.Length);
        Assert.Equal(step.Events.Select(item => item.GetRawText()), events.Take(3).Select(item => item.GetRawText()));
        Assert.Equal("StatusArea", events[3].GetProperty("from").GetProperty("zone").GetString());
        Assert.Equal("RemovedArea", events[3].GetProperty("to").GetProperty("zone").GetString());
        Assert.Equal(99, Assert.Single(events[3].GetProperty("cards").EnumerateArray()).GetProperty("card").GetInt32());
        Assert.Equal("confused", events[3].GetProperty("trigger").GetString());
        Assert.Equal("CardDetached", events[4].GetProperty("kind").GetString());
        Assert.Equal(99, events[4].GetProperty("card").GetInt32());
        Assert.Equal(1, events[4].GetProperty("host").GetInt32());
        Assert.Equal(25, Assert.Single(events[5].GetProperty("cards").EnumerateArray()).GetProperty("card").GetInt32());
        Assert.Equal("DiscardPile", events[5].GetProperty("to").GetProperty("zone").GetString());
        Assert.Equal(DeckType.DiscardPile, world.Cards[25].Area.Type);
        Assert.Equal(DeckType.DiscardPile, world.Cards[42].Area.Type);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        // "That character is not considered to have thwarted or schemed."
        Assert.Equal(threats, world.Cards.Select(card => card.Tokens.GetValueOrDefault("k_threat")));
        Assert.Equal(rng, world.Random.Generator.WordsConsumed);
        Assert.Equal("WhenPlayerInTurn", game.Pending!.Trigger);

        // Synthetic causal control: remove only confusion from a second verified prefix.
        // This is not a replay upgrade and does not advance the rejected historical choice.
        var control = FrozenReplayPrefixAssertions.Before(proof, factory, 25, 0);
        var status = Assert.Single(Statuses.On(control.State, control.State.Seats[0].IdentityCard, Statuses.Confused));
        World.MoveToTop(status, control.State.AreaOf(DeckType.RemovedArea));
        var continued = control.Resolve(step.Decision.Resolve(control.Pending!));
        JournalReplay.RequireEvents(step.Events, continued.Events, "unconfused control payment");
        JournalReplay.RequirePrompt(proof.Units[25].Decisions[1].Prompt, control.Pending!, "unconfused control target");
        Assert.Equal(DeckType.RevealingArea, control.State.Cards[25].Area.Type);
        Assert.Equal(DeckType.DiscardPile, control.State.Cards[42].Area.Type);
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
