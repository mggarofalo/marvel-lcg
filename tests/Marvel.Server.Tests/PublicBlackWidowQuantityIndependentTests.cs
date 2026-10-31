using Marvel.Protocol;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicBlackWidowQuantityIndependentTests
{
    [Fact]
    public void IdentityAndForeignSignatureCanOnlyBeRemovedWhileExtraPoolRemainsEditable()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var hero = factory.DiscoverDecks().Heroes.Single(item => item.Key == "black_widow");
        string[] draft = [.. hero.Signature, "08001a", "08001b", "01002", "08033"];
        var response = new EngineHost(factory).Exchange(EngineRequest.ReadDecks("widow-quantities",
            new(hero.Key, draft) { IncludeQuantities = true }));
        Assert.Null(response.Error);
        var assessment = response.Decks!.Assessment!;
        Assert.False(assessment.CanExtend);
        var quantities = assessment.Quantities;
        Assert.Equal([0], quantities["08001a"]);
        Assert.Equal([0], quantities["08001b"]);
        Assert.Equal([0], quantities["01002"]);
        // The invalid draft must remove forbidden cards before any additions.
        Assert.Equal([0], quantities["12024"]);
        Assert.False(quantities.ContainsKey("14001a"));
        Assert.All(hero.Signature, face => Assert.False(quantities.ContainsKey(face)));

        // Espionage is a Basic extra, outside the pinned printed starter.
        string[] repaired = [.. hero.Signature, "08033"];
        var editable = factory.DeckQuantities(hero.Key, repaired);
        Assert.Equal([0, 1, 2, 3], editable["08033"]);
        Assert.Equal([0, 1, 2, 3], editable["12024"]);
        Assert.True(factory.AssessDeck(hero.Key, repaired).CanExtend);
        Assert.DoesNotContain("08033", hero.Starter);
        string[] legal = [.. hero.Starter.SkipLast(1), "08033"];
        Assert.True(factory.AssessDeck(hero.Key, legal).IsValid);
        Assert.False(factory.AssessDeck(hero.Key, [.. legal.Skip(1), "08033"]).CanExtend);
    }
}
