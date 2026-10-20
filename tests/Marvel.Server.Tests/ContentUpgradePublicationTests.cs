using Marvel.Protocol;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ContentUpgradePublicationTests
{
    [Fact]
    public void ConcurrentPublishersCannotReplaceTheSamePredecessorTwice()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, original) =>
        {
            var (store, previous, candidate, generation, directory) = Prepare(root, original);
            using var start = new ManualResetEventSlim();
            var writers = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
            {
                start.Wait();
                return Record.Exception(() => store.CommitContentUpgrade(previous, candidate, generation));
            })).ToArray();
            start.Set();
            Task.WaitAll(writers);
            Assert.Single(writers, writer => writer.Result is null);
            var failure = Assert.Single(writers, writer => writer.Result is not null).Result;
            Assert.True(failure is IOException or SessionSaveException);
            Assert.Equal(StoredSessionJson.Write(candidate), StoredSessionJson.Write(Assert.Single(store.Load())));
            Assert.NotEqual(generation, File.ReadAllText(Path.Combine(directory, "current")).Trim());
            Assert.Equal(SessionSaveJson.Write(previous.Save),
                File.ReadAllText(Path.Combine(directory, ".upgrade-" + generation + ".session.json")));
        });
    }

    [Fact]
    public void FailedBackupLeavesOriginalPublishedAndRetryPublishesExactlyOnce()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, original) =>
        {
            var (store, previous, candidate, generation, directory) = Prepare(root, original);
            string authorityBackup = Path.Combine(directory, ".upgrade-" + generation + ".authority.json");
            File.WriteAllText(authorityBackup, "conflicting backup");
            Assert.Throws<SessionSaveException>(() => store.CommitContentUpgrade(previous, candidate, generation));
            Assert.Equal(generation, File.ReadAllText(Path.Combine(directory, "current")).Trim());
            Assert.Equal(StoredSessionJson.Write(previous), StoredSessionJson.Write(Assert.Single(store.Load())));
            Assert.Equal("conflicting backup", File.ReadAllText(authorityBackup));
            File.Delete(authorityBackup);
            string? published = store.CommitContentUpgrade(previous, candidate, generation);
            Assert.NotNull(published);
            Assert.Equal(published, File.ReadAllText(Path.Combine(directory, "current")).Trim());
            Assert.Equal(StoredSessionJson.Write(candidate), StoredSessionJson.Write(Assert.Single(store.Load())));
            Assert.Throws<SessionSaveException>(() => store.CommitContentUpgrade(previous, candidate, generation));
            Assert.Equal(published, File.ReadAllText(Path.Combine(directory, "current")).Trim());
        });
    }

    private static (FileSessionStore Store, StoredSession Previous, StoredSession Candidate,
        string Generation, string Directory) Prepare(string root, DatasetGameFactory original)
    {
        string storage = Path.Combine(root, "sessions");
        var store = new FileSessionStore(storage);
        var host = new EngineHost(original, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "publication",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        Assert.Null(opened.Error);
        var previous = Assert.Single(store.Load());
        string directory = Path.Combine(storage, previous.Save.Session.StorageId);
        string generation = File.ReadAllText(Path.Combine(directory, "current")).Trim();
        var upgraded = DatasetGameFactory.Load(root);
        var route = Assert.IsType<SessionContentUpgrade>(upgraded.ContentUpgrade(previous.Save.Compatibility));
        var save = route.Verify(previous.Save, setup =>
        {
            var game = upgraded.Create(new GameSpecification(setup.Scenario, setup.Heroes,
                setup.ModularSets, setup.Seed, setup.PlayerDecks) { Campaign = setup.Campaign });
            return new ReplayOpenedGame(game.Game, game.SetupEvents);
        });
        return (store, previous, previous with { Save = save }, generation, directory);
    }
}
