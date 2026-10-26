using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class DeckAssessmentTests
{
    [Fact]
    public void RejectedDeckCreatesNoDurableSessionAndReportsEveryKnownCorrection()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var draft = Starter().Skip(1).Concat(["01050", "01083"]).ToArray();
        var preview = factory.AssessDeck("spider_man", draft);
        Assert.False(preview.IsValid);
        Assert.Contains(preview.Diagnostics, item => item.Code == "signature-quantity");
        Assert.Contains(preview.Diagnostics, item => item.Code == "multiple-aspects");
        Assert.Contains(preview.Diagnostics, item => item.Code == "unique-match");
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var rejected = host.Exchange(EngineRequest.OpenGame("open", "invalid-deck",
            new GameSpecification("rhino", ["spider_man"], [], 17, PlayerDecks: [new(draft)])));
        Assert.NotNull(rejected.Error);
        Assert.Null(rejected.Capability);
        Assert.Empty(store.Load());
    }

    [Fact]
    public void RepeatedAssessmentDoesNotChangeTheGameCreatedFromTheSeed()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var recipe = new PlayerDeckRecipe(Starter());
        var specification = new GameSpecification("rhino", ["spider_man"], [], 17, PlayerDecks: [recipe]);
        var before = factory.Create(specification).Game.State.Digest().Canonical();
        for (int attempt = 0; attempt < 3; attempt++)
        {
            Assert.True(factory.AssessDeck("spider_man", recipe.Cards).IsValid);
            Assert.False(factory.AssessDeck("spider_man", ["unknown"]).IsValid);
        }
        Assert.Equal(before, factory.Create(specification).Game.State.Digest().Canonical());
    }

    [Fact]
    public void ResearchCardsCannotBeAdmittedByPreviewOrGameCreation()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        string[] draft = [.. Starter(), "05001a"];
        var preview = factory.AssessDeck("spider_man", draft);
        Assert.False(preview.IsValid);
        Assert.Null(preview.Size);
        Assert.Contains(preview.Diagnostics, item => item.Code == "unsupported-face" && item.Face == "05001a");
        Assert.Throws<ArgumentException>(() => factory.Create(
            new GameSpecification("rhino", ["spider_man"], [], 17, PlayerDecks: [new(draft)])));
    }

    private static string[] Starter()
    {
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var hero = setup.Hero("spider_man");
        return [.. hero.HeroDeck, .. hero.PlayerDeck];
    }
}
