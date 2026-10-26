using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class ThorRecipeIndependentTests
{
    [Fact]
    public void EqualLengthDividerMisprintCannotAdmitTheNemesisSet()
    {
        // The numbered mc06 components identify Frost Giant as 3/5 and 4/5,
        // and Trickster as 5/5: the divider's swapped quantities are not setup.
        var json = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")))!;
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var correct = RuntimeProductAdmission.Discover(SetupCatalog.Parse(json.ToJsonString()), cards,
            AuthoredCards.Book.Authored);
        Assert.Contains("thor", correct.Setup.HeroNames);
        var nemesis = json["heroes"]!["thor"]!["nemesis_set"]!.AsArray();
        Assert.Equal(5, nemesis.Count);
        nemesis[3] = "06030";

        var wrong = RuntimeProductAdmission.Discover(SetupCatalog.Parse(json.ToJsonString()), cards,
            AuthoredCards.Book.Authored);
        Assert.DoesNotContain("thor", wrong.Setup.HeroNames);
        Assert.DoesNotContain(wrong.Playable, face => cards.Pack(face) == "thor");
        Assert.Equal(correct.Setup.HeroNames.Where(key => key != "thor"), wrong.Setup.HeroNames);
        Assert.Equal(correct.Setup.CampaignNames, wrong.Setup.CampaignNames);
        Assert.Equal(correct.Setup.EncounterSetNames, wrong.Setup.EncounterSetNames);
    }
}
