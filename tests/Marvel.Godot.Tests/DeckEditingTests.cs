using Marvel.Client;
using Marvel.Protocol;
using Marvel.Server;
using Marvel.Tests;
using Marvel.Transport;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DeckEditingTests
{
    [Theory]
    [InlineData("01084", 1, true)] // Unique Nick Fury.
    [InlineData("01084", 2, false)]
    [InlineData("01088", 2, false)] // Printed one-per-deck Energy.
    [InlineData("01087", 3, true)]
    [InlineData("01005", 3, true)] // Spider-Man's exact signature quantity.
    [InlineData("01005", 2, false)]
    [InlineData("03001a", 1, false)] // Research content is not playable.
    public async Task QuantityButtonsUseHostConstructionAssessment(string face, int quantity, bool available)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        using var client = new LocalGameClient(new InProcessTransport(new EngineHost(factory)));
        var workspace = Workspace(factory);
        var proposal = await DeckEditProposal.CheckAsync(workspace, client, [face], quantity, TestContext.Current.CancellationToken);
        Assert.Equal(available, proposal is not null);
        Assert.Equal(15, workspace.Draft!.Cards.Length);
        Assert.False(workspace.CanUndo);
    }

    [Fact]
    public async Task BulkQuantityAndClearAreAtomicAndRedoIsDiscardedAfterNewEdits()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        using var client = new LocalGameClient(new InProcessTransport(new EngineHost(factory)));
        var workspace = Workspace(factory);
        var original = workspace.Draft!;
        var proposal = await DeckEditProposal.CheckAsync(workspace, client, ["01086", "01087"], 3, TestContext.Current.CancellationToken);
        Assert.True(proposal!.Apply(workspace));
        var bulk = workspace.Draft!;
        Assert.Equal(21, bulk.Cards.Length);
        workspace.Undo();
        Assert.Equal(original.Cards, workspace.Draft!.Cards);
        Assert.Null(workspace.Assessment);
        workspace.Redo();
        Assert.Equal(bulk.Cards, workspace.Draft!.Cards);
        workspace.ClearDeck();
        Assert.Equal(original.Cards, workspace.Draft!.Cards);
        workspace.Undo();
        Assert.Equal(bulk.Cards, workspace.Draft!.Cards);
        workspace.ClearCards(["01086", "01005"]);
        Assert.DoesNotContain("01086", workspace.Draft!.Cards);
        Assert.Equal(3, workspace.Draft.Cards.Count(face => face == "01005"));
        Assert.False(workspace.CanRedo);
        workspace.Undo();
        Assert.Equal(bulk.Cards, workspace.Draft!.Cards);
    }

    [Fact]
    public async Task ChangedIdentityAndUndoInvalidateCapturedProposals()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        using var client = new LocalGameClient(new InProcessTransport(new EngineHost(factory)));
        var workspace = Workspace(factory);
        var original = workspace.Draft!;
        var proposal = await DeckEditProposal.CheckAsync(workspace, client, ["01087"], null, TestContext.Current.CancellationToken);
        workspace.ChooseIdentity("iron_man");
        workspace.Undo();
        Assert.Equal(original, workspace.Draft);
        Assert.False(proposal!.Apply(workspace));
        workspace.Redo();
        Assert.NotEqual(original.HeroCode, workspace.Draft!.HeroCode);
        workspace.Replace(original);
        Assert.False(workspace.CanUndo);
        Assert.False(workspace.CanRedo);
    }

    [Fact]
    public async Task MixedAspectBulkAdditionIsRejectedWithoutPartialApplication()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        using var client = new LocalGameClient(new InProcessTransport(new EngineHost(factory)));
        var workspace = Workspace(factory);
        var before = workspace.Draft;
        Assert.Null(await DeckEditProposal.CheckAsync(workspace, client, ["01050", "01060"], 1, TestContext.Current.CancellationToken));
        Assert.Same(before, workspace.Draft);
        Assert.False(workspace.CanUndo);
    }

    [Fact]
    public void ClearRepairsMissingImportedSignaturesAndUndoRestoresExactImport()
    {
        var workspace = Workspace(DatasetGameFactory.Load(RepositoryPaths.Root));
        workspace.Replace(new("Imported", "01001a", ["unknown", "01005"]));
        var imported = workspace.Draft!;
        workspace.ClearDeck();
        Assert.Equal(15, workspace.Draft!.Cards.Length);
        Assert.DoesNotContain("unknown", workspace.Draft.Cards);
        workspace.Undo();
        Assert.Equal(imported, workspace.Draft);
    }

    [Theory]
    [InlineData(15, true)]
    [InlineData(50, true)]
    [InlineData(51, false)]
    [InlineData(null, false)]
    public void PartialConstructionPermissionDoesNotExcuseMaximumOrUnknownSize(int? size, bool permitted)
    {
        var assessment = new Marvel.Rules.Setup.DeckAssessment(size,
            [new("deck-size", "", "Synthetic size diagnostic")]);
        Assert.Equal(permitted, assessment.CanExtend);
    }

    [Fact]
    public async Task DelayedProposalCannotReplaceAnEditedDraft()
    {
        var workspace = Workspace(DatasetGameFactory.Load(RepositoryPaths.Root));
        var delayed = new DelayedAssessment();
        using var client = new LocalGameClient(delayed);
        var pending = DeckEditProposal.CheckAsync(workspace, client, ["01087"], 1, TestContext.Current.CancellationToken);
        workspace.Describe("New name", workspace.Draft!.HeroCode);
        delayed.Complete();
        Assert.Null(await pending);
        Assert.Equal("New name", workspace.Draft.Name);
        Assert.DoesNotContain("01087", workspace.Draft.Cards);
    }

    private sealed class DelayedAssessment : IEngineTransport
    {
        private readonly TaskCompletionSource<Marvel.Protocol.EngineResponse> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private string requestId = "";
        public ValueTask<Marvel.Protocol.EngineResponse> ExchangeAsync(Marvel.Protocol.EngineRequest request, CancellationToken cancellationToken = default)
        {
            requestId = request.RequestId;
            return new(completion.Task);
        }
        internal void Complete() => completion.SetResult(new(Marvel.Protocol.EngineProtocol.Version, requestId, "", null, null, [])
        {
            Decks = new(null, new(16, []) { CanExtend = true }),
        });
    }

    private static DeckWorkspace Workspace(DatasetGameFactory factory)
    {
        var catalog = factory.DiscoverDecks();
        var hero = catalog.Heroes.Single(item => item.Key == "spider_man");
        var workspace = new DeckWorkspace(catalog);
        workspace.Initialize(new("Build", hero.IdentityFaces[0], hero.Signature));
        return workspace;
    }
}
