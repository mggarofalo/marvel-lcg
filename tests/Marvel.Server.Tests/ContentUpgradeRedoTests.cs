using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ContentUpgradeRedoTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UpgradeVerifiesInactiveHistoryBeforePublishingAndRetainsLegalRedo(bool corruptRedo)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, original) =>
        {
            var store = new FileSessionStore(Path.Combine(root, "sessions"));
            var host = new EngineHost(original, store: store);
            var opened = host.Exchange(EngineRequest.OpenGame("open", "redo-core",
                new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
            Assert.Null(opened.Error);
            var turn = Take(host, opened, opened, Game.ResolveMulligans);
            var changed = Take(host, opened, turn, Game.ChangeForm);
            var complete = Assert.Single(store.Load()).Save;
            var undone = host.Exchange(EngineRequest.UndoGame("undo", opened.GameId, opened.Capability!,
                complete.Cursor - 1, changed.Revision));
            Assert.Null(undone.Error);
            var previous = Assert.Single(store.Load());
            Assert.True(previous.Save.Cursor < previous.Save.Units.Count);
            var controlStore = new MemorySessionStore();
            controlStore.Commit(previous);
            var control = new EngineHost(original, store: controlStore);
            var expectedRedo = control.Exchange(EngineRequest.RedoGame("control-redo", opened.GameId,
                opened.Capability!, complete.Cursor, undone.Revision));
            Assert.Null(expectedRedo.Error);
            if (corruptRedo)
            {
                var units = previous.Save.Units.ToArray();
                var decisions = units[^1].Decisions.ToArray();
                decisions[0] = decisions[0] with { StateFingerprint = new string('a', 64) };
                units[^1] = units[^1] with { Decisions = decisions };
                previous = previous with { Save = previous.Save with { Units = units } };
                store.Commit(previous);
            }
            string before = StoredSessionJson.Write(previous);
            var upgraded = new EngineHost(DatasetGameFactory.Load(root), store: store);
            var restored = upgraded.Exchange(EngineRequest.SyncGame("sync", opened.GameId, opened.Capability!));
            if (corruptRedo)
            {
                Assert.Equal("session_not_found", restored.Error?.Code);
                Assert.Equal(before, StoredSessionJson.Write(Assert.Single(store.Load())));
                Assert.Empty(Directory.GetFiles(Path.Combine(root, "sessions"), ".upgrade-*", SearchOption.AllDirectories));
                return;
            }
            Assert.Null(restored.Error);
            Assert.Equal(CampaignSaveJson.Canonical(undone.Prompt), CampaignSaveJson.Canonical(restored.Prompt));
            var redone = upgraded.Exchange(EngineRequest.RedoGame("redo", opened.GameId, opened.Capability!,
                complete.Cursor, restored.Revision));
            Assert.Null(redone.Error);
            Assert.Equal(CampaignSaveJson.Canonical(expectedRedo.World), CampaignSaveJson.Canonical(redone.World));
            Assert.Equal(CampaignSaveJson.Canonical(expectedRedo.Prompt), CampaignSaveJson.Canonical(redone.Prompt));
            Assert.Equal(CampaignSaveJson.Canonical(complete.Units),
                CampaignSaveJson.Canonical(Assert.Single(store.Load()).Save.Units));
        });
    }

    private static EngineResponse Take(EngineHost host, EngineResponse owner, EngineResponse current, string verb)
    {
        var offered = Assert.Single(current.Prompt!.Affordances, action => action.Verb == verb);
        var response = host.Exchange(EngineRequest.ResolveGame(verb, owner.GameId, owner.Capability!,
            Decision.Take(offered.Id), current.Revision));
        Assert.Null(response.Error);
        return response;
    }
}
