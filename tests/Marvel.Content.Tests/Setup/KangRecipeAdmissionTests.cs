using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class KangRecipeAdmissionTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void EveryPrintedFaceOfThePackIsAuthoredAndEveryRecipeFaceIsInThePack()
    {
        // The scenario pack prints 51 cards; the seven main schemes are double-sided,
        // so 58 faces. Each one is an executable row, so a recipe never deals a face
        // the engine would meet unauthored.
        var pack = Cards.Faces.Where(face => Cards.Pack(face) == "toafk").ToArray();
        Assert.Equal(58, pack.Length);
        Assert.All(pack, face => Assert.Contains(face, AuthoredCards.Program.Authored));
        foreach (string scenario in new[] { "kang", "kang_expert" })
        {
            var recipe = Setup.Campaign(scenario);
            Assert.All(recipe.Villain.Concat(recipe.Schemes).Concat(recipe.SetAside).Concat(recipe.Encounters)
                .SelectMany(spec => spec.Split(',')), face => Assert.Contains(face, pack));
        }
    }

    [Fact]
    public void PublicAdmissionStaysClosedEvenWhenEveryFaceIsAuthored()
    {
        // Our product decision: separated game areas have no client presentation yet, so
        // the scenario and its modular sets stay outside the accepted product boundary.
        var admitted = RuntimeProductAdmission.Discover(Setup, Cards,
            Cards.Faces.ToHashSet(StringComparer.Ordinal));

        Assert.Contains("kang", Setup.CampaignNames);
        Assert.Contains("kang_expert", Setup.CampaignNames);
        Assert.DoesNotContain("kang", admitted.Setup.CampaignNames);
        Assert.DoesNotContain("kang_expert", admitted.Setup.CampaignNames);
        Assert.Throws<KeyNotFoundException>(() => Dealer.DealOrder(admitted.Setup, "kang", ["spider_man"], facts: Cards));
        Assert.DoesNotContain(admitted.Playable, face => Cards.Pack(face) == "toafk");
        Assert.Contains("rhino", admitted.Setup.CampaignNames);
    }
}
