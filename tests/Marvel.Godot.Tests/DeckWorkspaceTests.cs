using Marvel.Client;
using Marvel.Protocol;
using Marvel.Server;
using Marvel.Tests;
using Marvel.Transport;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DeckWorkspaceTests
{
    [Fact]
    public async Task EditingInvalidatesAssessmentAndIdentityChangesPreserveTheDraft()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        using var client = new LocalGameClient(new InProcessTransport(new EngineHost(factory)));
        var workspace = new DeckWorkspace(factory.DiscoverDecks());
        var hero = workspace.Catalog.Heroes[0];
        workspace.Replace(new("Draft", hero.IdentityFaces[0], hero.Starter));
        Assert.Null(await workspace.AssessAsync(client, TestContext.Current.CancellationToken));
        Assert.True(workspace.CanUse);
        workspace.ChangeCard(hero.Starter[0], false);
        Assert.False(workspace.CanUse);
        Assert.Null(await workspace.AssessAsync(client, TestContext.Current.CancellationToken));
        Assert.Contains(workspace.Assessment!.Diagnostics, item => item.Code == "signature-quantity");
        var before = workspace.Draft!.Cards;
        workspace.Describe("Changed hero", workspace.Catalog.Heroes[^1].IdentityFaces[0]);
        Assert.Equal(before, workspace.Draft.Cards);
        Assert.Null(workspace.Assessment);
        Assert.True(workspace.Dirty);
    }

    [Fact]
    public async Task AssessmentForAnOldDraftCannotEnableUseAfterAnEdit()
    {
        var catalog = DatasetGameFactory.Load(RepositoryPaths.Root).DiscoverDecks();
        var workspace = new DeckWorkspace(catalog);
        var hero = catalog.Heroes[0];
        workspace.Replace(new("Draft", hero.IdentityFaces[0], hero.Starter));
        var delayed = new DelayedTransport();
        using var client = new LocalGameClient(delayed);
        Task<string?> pending = workspace.AssessAsync(client, TestContext.Current.CancellationToken);
        workspace.ChangeCard("unknown", true);
        delayed.Complete();
        Assert.Null(await pending);
        Assert.Null(workspace.Assessment);
        Assert.False(workspace.CanUse);
        Assert.Contains("unknown", workspace.Draft!.Cards);
    }

    [Fact]
    public void IdentitySelectionReplacesOnlySuppliedSignatureCards()
    {
        var catalog = DatasetGameFactory.Load(RepositoryPaths.Root).DiscoverDecks();
        var workspace = new DeckWorkspace(catalog);
        var first = catalog.Heroes[0];
        var next = catalog.Heroes[1];
        workspace.Initialize(new("My deck", first.IdentityFaces[0], first.Starter));
        Assert.False(workspace.Dirty);
        workspace.ChangeCard("unknown-import", true);
        workspace.ChooseIdentity(next.Key);
        Assert.Equal(next.IdentityFaces[0], workspace.Draft!.HeroCode);
        Assert.Equal("My deck", workspace.Draft.Name);
        Assert.Equal(next.Signature.Concat(first.Starter.Where(face => !first.Signature.Contains(face))).Append("unknown-import"), workspace.Draft.Cards);
        Assert.True(workspace.Dirty);
        long revision = workspace.Revision;
        workspace.ChooseIdentity(next.Key);
        Assert.Equal(revision, workspace.Revision);
    }

    [Fact]
    public void CatalogPreviewsCarryFullPrintedTextWithoutLiveTargets()
    {
        var catalog = DatasetGameFactory.Load(RepositoryPaths.Root).DiscoverDecks();
        var kick = catalog.Cards.Single(card => card.Face == "01005");
        Assert.Contains("8 damage", kick.Preview!.RulesText);
        Assert.Null(kick.Preview.TargetId);
        Assert.Equal(kick.Face, kick.Preview.FaceId);
        Assert.All(catalog.Heroes, hero => Assert.Equal(hero.IdentityFaces, hero.Previews.Select(face => face.FaceId)));
        Assert.All(catalog.Heroes.SelectMany(hero => hero.Previews), face => Assert.Null(face.TargetId));
    }

    private sealed class DelayedTransport : IEngineTransport
    {
        private readonly TaskCompletionSource<EngineResponse> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private EngineRequest request = null!;

        public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default)
        {
            this.request = request;
            return new(completion.Task);
        }

        internal void Complete() => completion.SetResult(new(EngineProtocol.Version, request.RequestId, "", null, null, [])
        {
            Decks = new(null, new(40, [])),
        });
    }
}
