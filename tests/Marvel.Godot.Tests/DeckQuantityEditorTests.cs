using Marvel.Client;
using Marvel.Protocol;
using Marvel.Server;
using Marvel.Tests;
using Marvel.Transport;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DeckQuantityEditorTests
{
    [Fact]
    public async Task ZeroSupersedesAnUncompletedAddition()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var transport = new HeldAssessment(new InProcessTransport(new EngineHost(factory)));
        using var client = new LocalGameClient(transport);
        var workspace = Workspace(factory);
        int changes = 0;
        using var editor = new DeckQuantityEditor(workspace, client, () => changes++);
        var addition = editor.SetAsync("01087", 1);
        var removal = editor.SetAsync("01087", 0);
        transport.Release();
        Assert.Null(await addition);
        Assert.Null(await removal);
        Assert.DoesNotContain("01087", workspace.Draft!.Cards);
        Assert.Equal(0, changes);
        Assert.False(workspace.CanUndo);
    }

    [Fact]
    public async Task IndependentRowsAreRetainedAndLatestQuantityWins()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var transport = new HeldAssessment(new InProcessTransport(new EngineHost(factory)));
        using var client = new LocalGameClient(transport);
        var workspace = Workspace(factory);
        int changes = 0;
        using var editor = new DeckQuantityEditor(workspace, client, () => changes++);
        var first = editor.SetAsync("01087", 1);
        var second = editor.SetAsync("01086", 2);
        var latest = editor.SetAsync("01087", 3);
        transport.Release();
        Assert.All(await Task.WhenAll(first, second, latest), Assert.Null);
        Assert.Equal(3, workspace.Draft!.Cards.Count(face => face == "01087"));
        Assert.Equal(2, workspace.Draft.Cards.Count(face => face == "01086"));
        Assert.Equal(2, changes);
        workspace.Undo();
        Assert.DoesNotContain("01087", workspace.Draft!.Cards);
        Assert.Equal(2, workspace.Draft.Cards.Count(face => face == "01086"));
    }

    [Fact]
    public async Task CancellationDiscardsEveryQueuedIntention()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var transport = new HeldAssessment(new InProcessTransport(new EngineHost(factory)));
        using var client = new LocalGameClient(transport);
        var workspace = Workspace(factory);
        using var editor = new DeckQuantityEditor(workspace, client, () => { });
        var first = editor.SetAsync("01087", 1);
        var second = editor.SetAsync("01086", 2);
        editor.Cancel();
        transport.Release();
        Assert.All(await Task.WhenAll(first, second), Assert.Null);
        Assert.Equal(15, workspace.Draft!.Cards.Length);
        Assert.False(workspace.CanUndo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NewEditAfterBulkOrReplacementDoesNotJoinStaleQueue(bool replace)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var transport = new HeldAssessment(new InProcessTransport(new EngineHost(factory)));
        using var client = new LocalGameClient(transport);
        var workspace = Workspace(factory);
        workspace.Replace(new("Seeded", workspace.Draft!.HeroCode, [.. workspace.Draft.Cards, "01088"]));
        using var editor = new DeckQuantityEditor(workspace, client, () => { });
        var old = editor.SetAsync("01087", 1);
        if (replace) workspace.Replace(new("Replacement", workspace.Draft!.HeroCode, workspace.Draft.Cards));
        else { editor.Cancel(); workspace.ClearDeck(); }
        var current = editor.SetAsync("01086", 2);
        transport.Release();
        Assert.Null(await old);
        Assert.Null(await current);
        Assert.DoesNotContain("01087", workspace.Draft!.Cards);
        Assert.Equal(2, workspace.Draft.Cards.Count(face => face == "01086"));
    }

    private static DeckWorkspace Workspace(DatasetGameFactory factory)
    {
        var catalog = factory.DiscoverDecks();
        var hero = catalog.Heroes.Single(item => item.Key == "spider_man");
        var workspace = new DeckWorkspace(catalog);
        workspace.Initialize(new("Build", hero.IdentityFaces[0], hero.Signature));
        return workspace;
    }

    private sealed class HeldAssessment(IEngineTransport inner) : IEngineTransport
    {
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default)
        {
            await release.Task;
            return await inner.ExchangeAsync(request, cancellationToken);
        }
        internal void Release() => release.TrySetResult();
    }
}
