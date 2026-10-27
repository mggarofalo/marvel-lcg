using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class HulkAdmissionReviewTests
{
    [Fact]
    public void AnAdditionalAuthoredPackFaceClosesHulkWithoutChangingPredecessorAdmission()
    {
        // Repository policy: reviewed inventory is exact, rather than a minimum.
        // Synthetic catalog extension challenges a superset-only admission check.
        string setupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
        var json = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")))!;
        var cards = CardCatalog.Parse(json.ToJsonString());
        var baseline = RuntimeProductAdmission.Discover(SetupCatalog.Parse(setupJson), cards,
            AuthoredCards.Book.Authored);
        var unexpected = json["cards"]!.AsArray().Single(row =>
            row!["card_id"]!.GetValue<string>() == "10032")!.DeepClone();
        unexpected["card_id"] = "99999";
        json["cards"]!.AsArray().Add(unexpected);
        var altered = CardCatalog.Parse(json.ToJsonString());
        var authored = AuthoredCards.Book.Authored.Append("99999").ToHashSet(StringComparer.Ordinal);

        var rejected = RuntimeProductAdmission.Discover(SetupCatalog.Parse(setupJson), altered, authored);

        Assert.Contains("hulk", baseline.Setup.HeroNames);
        Assert.Equal(33, baseline.Playable.Count(face => cards.Pack(face) == "hlk"));
        Assert.DoesNotContain("hulk", rejected.Setup.HeroNames);
        Assert.DoesNotContain(rejected.Playable, face => altered.Pack(face) == "hlk");
        Assert.Equal(baseline.Playable.Where(face => cards.Pack(face) != "hlk").Order(StringComparer.Ordinal),
            rejected.Playable.Order(StringComparer.Ordinal));
        Assert.Equal(baseline.Setup.HeroNames.Where(name => name != "hulk"), rejected.Setup.HeroNames);
        Assert.Equal(baseline.Setup.CampaignNames, rejected.Setup.CampaignNames);
        Assert.Equal(baseline.Setup.EncounterSetNames, rejected.Setup.EncounterSetNames);
        Assert.NotNull(rejected.Campaign);
    }
}
