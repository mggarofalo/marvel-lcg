using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ContentUpgradePersistenceTests
{
    [Fact]
    public void DiskUpgradeRetainsPendingSeatAuthorityTraceAndPermanentOriginalBackup()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, original) =>
        {
            string directory = Path.Combine(root, "sessions");
            var store = new FileSessionStore(directory);
            var host = new EngineHost(original, visibility: new RestrictedVisibilityPolicy(0), store: store);
            var opened = host.Exchange(EngineRequest.OpenGame("old-open", "old-core",
                new GameSpecification("rhino", ["spider_man", "captain_marvel"], [], Seed: 73)));
            Assert.Null(opened.Error);
            var guest = host.Exchange(EngineRequest.AttachGame("guest", opened.GameId,
                Assert.Single(opened.Invitations!).Invitation));
            Assert.Null(guest.Error);
            var keep = Assert.Single(opened.Prompt!.Affordances, action => action.Verb == Game.ResolveMulligans);
            Assert.Null(host.Exchange(EngineRequest.ResolveGame("keep", opened.GameId, opened.Capability!,
                Decision.Take(keep.Id), opened.Revision)).Error);
            var before = host.Exchange(EngineRequest.SyncGame("before", opened.GameId, guest.Capability!));
            var saved = Assert.Single(store.Load());
            string path = Path.Combine(directory, saved.Save.Session.StorageId);
            string oldGeneration = File.ReadAllText(Path.Combine(path, "current")).Trim();
            byte[] oldArchive = File.ReadAllBytes(Path.Combine(path, oldGeneration + ".session.json"));
            byte[] oldAuthority = File.ReadAllBytes(Path.Combine(path, oldGeneration + ".authority.json"));
            File.WriteAllText(Path.Combine(path, ".upgrade-" + oldGeneration + ".session.json.tmp"), "interrupted copy");
            var upgraded = DatasetGameFactory.Load(root);
            var restored = new EngineHost(upgraded, visibility: new RestrictedVisibilityPolicy(0), store: store);
            var after = restored.Exchange(EngineRequest.SyncGame("after", opened.GameId, guest.Capability!));
            Assert.Null(after.Error);
            Assert.Equal(before.Revision, after.Revision);
            Assert.Equal(CampaignSaveJson.Canonical(before.Prompt), CampaignSaveJson.Canonical(after.Prompt));
            Assert.Equal(CampaignSaveJson.Canonical(before.World), CampaignSaveJson.Canonical(after.World));
            var current = Assert.Single(store.Load());
            Assert.Equal(upgraded.Compatibility, current.Save.Compatibility);
            Assert.Equal(StoredSessionJson.Write(saved), StoredSessionJson.Write(current with
                { Save = current.Save with { Compatibility = saved.Save.Compatibility } }));
            Assert.Equal(oldArchive, File.ReadAllBytes(Path.Combine(path, ".upgrade-" + oldGeneration + ".session.json")));
            Assert.Equal(oldAuthority, File.ReadAllBytes(Path.Combine(path, ".upgrade-" + oldGeneration + ".authority.json")));
            string generation = File.ReadAllText(Path.Combine(path, "current"));
            _ = new EngineHost(upgraded, visibility: new RestrictedVisibilityPolicy(0), store: store);
            Assert.Equal(generation, File.ReadAllText(Path.Combine(path, "current")));
            Assert.Throws<SessionSaveException>(() => store.CommitContentUpgrade(saved, current, oldGeneration));
            Assert.Equal(generation, File.ReadAllText(Path.Combine(path, "current")));
            keep = Assert.Single(after.Prompt!.Affordances, action => action.Verb == Game.ResolveMulligans);
            var wrong = restored.Exchange(EngineRequest.ResolveGame("wrong", opened.GameId, opened.Capability!,
                Decision.Take(keep.Id), after.Revision));
            Assert.NotNull(wrong.Error);
            var continued = restored.Exchange(EngineRequest.ResolveGame("continue", opened.GameId, guest.Capability!,
                Decision.Take(keep.Id), after.Revision));
            Assert.Null(continued.Error);
            Assert.True(continued.Revision > after.Revision);
            Assert.Equal(oldArchive, File.ReadAllBytes(Path.Combine(path, ".upgrade-" + oldGeneration + ".session.json")));
            VerifyRollback(root, path, oldGeneration, original, opened, guest, before);
        });
    }

    private static void VerifyRollback(
        string root, string archive, string generation, DatasetGameFactory original,
        EngineResponse opened, EngineResponse guest, EngineResponse expected)
    {
        string restoredRoot = Path.Combine(root, "rollback");
        string restoredSession = Path.Combine(restoredRoot, Path.GetFileName(archive));
        Directory.CreateDirectory(restoredSession);
        foreach (string suffix in new[] { ".session.json", ".authority.json" })
            File.Copy(Path.Combine(archive, ".upgrade-" + generation + suffix),
                Path.Combine(restoredSession, generation + suffix));
        File.WriteAllText(Path.Combine(restoredSession, "current"), generation + "\n");
        var rollback = new EngineHost(original, visibility: new RestrictedVisibilityPolicy(0),
            store: new FileSessionStore(restoredRoot));
        var response = rollback.Exchange(EngineRequest.SyncGame("rollback", opened.GameId, guest.Capability!));
        Assert.Null(response.Error);
        Assert.Equal(expected.Revision, response.Revision);
        Assert.Equal(CampaignSaveJson.Canonical(expected.Prompt), CampaignSaveJson.Canonical(response.Prompt));
        Assert.Equal(CampaignSaveJson.Canonical(expected.World), CampaignSaveJson.Canonical(response.World));
    }

    [Fact]
    public void DivergentArchiveIsNotMigratedAndRemainsAvailableForReadOnlyInspection()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, original) =>
        {
            var store = new FileSessionStore(Path.Combine(root, "sessions"));
            var host = new EngineHost(original, store: store);
            var opened = host.Exchange(EngineRequest.OpenGame("open", "tampered-core",
                new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
            Assert.Null(opened.Error);
            var saved = Assert.Single(store.Load());
            var tampered = saved with { Save = saved.Save with
                { Initial = saved.Save.Initial with { StateDigest = "not the dealt state" } } };
            store.Commit(tampered);
            string before = StoredSessionJson.Write(Assert.Single(store.Load()));
            var upgraded = new EngineHost(DatasetGameFactory.Load(root), store: store);
            Assert.Equal("session_not_found", upgraded.Exchange(
                EngineRequest.SyncGame("sync", opened.GameId, opened.Capability!)).Error?.Code);
            Assert.Equal(before, StoredSessionJson.Write(Assert.Single(store.Load())));
            Assert.Empty(Directory.GetFiles(Path.Combine(root, "sessions"), ".upgrade-*", SearchOption.AllDirectories));
        });
    }
}
