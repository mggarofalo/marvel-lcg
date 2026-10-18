using Marvel.Client;
using Marvel.Protocol;
using Marvel.Rules.Setup;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DeckLibraryTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "marvel-decks-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void AutomaticPreservationKeepsEditsWithoutRecreatingAnExplicitlyDeletedDeck()
    {
        var store = new LocalDeckStore(directory);
        var workspace = new DeckWorkspace(new([], []));
        workspace.Initialize(new("", "01001a", []));
        workspace.Preserve(store);
        Assert.Empty(store.List());
        workspace.ChangeCard("01005", true);
        workspace.Preserve(store);
        var saved = Assert.Single(store.List());
        Assert.Equal("Untitled deck", saved.Draft.Name);
        Assert.Equal(["01005"], saved.Draft.Cards);
        store.Delete(saved.Id, saved.Revision);
        workspace.ForgetSaved();
        workspace.Preserve(store);
        Assert.Empty(store.List());
        Assert.Equal(["01005"], workspace.Draft!.Cards);
        workspace.ChangeCard("01005", true);
        workspace.Preserve(store);
        Assert.Equal(["01005", "01005"], Assert.Single(store.List()).Draft.Cards);
    }

    [Fact]
    public void InvalidDraftsPreserveOrderAndRevisionsAcrossSaveAndReopen()
    {
        var store = new LocalDeckStore(directory);
        var draft = new DeckDraft("Unfinished", "01040a", ["unknown", "01043d", "01043a", "01043d"]);
        var saved = store.Save(draft);
        var reopened = new LocalDeckStore(directory).Read(saved.Id);
        Assert.Equal(1, reopened.Revision);
        Assert.Equal(draft.Name, reopened.Draft.Name);
        Assert.Equal(draft.Cards, reopened.Draft.Cards);
        var revised = store.Save(new("Revised", draft.HeroCode, draft.Cards), saved.Id, saved.Revision);
        Assert.Equal(2, revised.Revision);
        Assert.Throws<IOException>(() => store.Save(draft, saved.Id, saved.Revision));
        Assert.Equal("Revised", store.Read(saved.Id).Draft.Name);
        Assert.Equal(saved.Id, Assert.Single(store.List()).Id);
        Assert.Throws<IOException>(() => store.Delete(saved.Id, saved.Revision));
        store.Delete(revised.Id, revised.Revision);
        Assert.Empty(store.List());
        Assert.Equal(["unknown", "01043d", "01043a", "01043d"], saved.Draft.Cards);
    }

    [Fact]
    public void CapturedGameRestoresAfterItsLibrarySourceIsEditedAndDeleted()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var hero = factory.DiscoverDecks().Heroes[0];
        var library = new LocalDeckStore(directory);
        var saved = library.Save(new("Captured", hero.IdentityFaces[0], hero.Starter.Reverse().ToArray()));
        var sessions = new MemorySessionStore();
        var host = new EngineHost(factory, store: sessions);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "library-replay",
            new("rhino", [hero.Key], [], 91, PlayerDecks: [new PlayerDeckRecipe(saved.Draft.Cards)])));
        Assert.Null(opened.Error);
        var before = host.Exchange(EngineRequest.SyncGame("sync", "library-replay", opened.Capability!));
        var edited = library.Save(new("Incomplete", saved.Draft.HeroCode, []), saved.Id, saved.Revision);
        library.Delete(edited.Id, edited.Revision);
        Assert.Empty(library.List());
        var restarted = new EngineHost(factory, store: sessions);
        var after = restarted.Exchange(EngineRequest.SyncGame("sync", "library-replay", opened.Capability!));
        Assert.Null(after.Error);
        Assert.Equal(EngineJson.Write(before), EngineJson.Write(after));
        Assert.Equal(saved.Draft.Cards, Assert.Single(sessions.Load()).Save.Setup.PlayerDecks![0].Cards);
    }

    [Fact]
    public void CopyGetsItsOwnIdentityAndInvalidPathsCannotEscapeTheLibrary()
    {
        var store = new LocalDeckStore(directory);
        var draft = new DeckDraft("Draft", "01001a", []);
        var first = store.Save(draft);
        var copy = store.Save(first.Draft);
        Assert.NotEqual(first.Id, copy.Id);
        Assert.Equal(2, store.List().Count);
        Assert.Throws<ArgumentException>(() => store.Read("../outside"));
        Assert.Throws<ArgumentException>(() => store.Save(draft, "../outside"));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
