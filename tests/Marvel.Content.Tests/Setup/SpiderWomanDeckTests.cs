using Marvel.Content.Setup;
using Marvel.Rules.Setup;
using Marvel.Tests;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Marvel.Content.Tests.Setup;

/// <summary>Printed construction checks using synthetic setup metadata, without admitting a playable expansion.</summary>
public sealed class SpiderWomanDeckTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly string[] Signature = ["04032", "04033", "04033", "04034", "04035", "04035",
        "04036", "04036", "04037", "04037", "04038", "04038", "04039", "04039", "04039"];
    private static readonly string[] Customization = ["01052", "01052", "01052", "01053", "01053", "01053",
        "01054", "01054", "01054", "01056", "01060", "01060", "01060", "01061", "01061", "01061",
        "01063", "01063", "01063", "01064", "01085", "01085", "01085", "01086", "01086"];
    private static HeroSetup Hero => new("Spider-Woman", ["04031a,04031b"], Signature, [], [], Customization, "spider_woman_nemesis")
    { AspectRule = DeckAspectRule.EqualTwoAspects };

    [Fact]
    public void EqualChosenAspectsAdmitDespiteFourAspectsInTheRequiredSignature()
    {
        // 04031b Double Agent: "Choose two aspects instead of one ... an equal number of cards from those aspects".
        string[] deck = [.. Signature, .. Customization];
        var assessment = Assess(deck);
        Assert.True(assessment.IsValid, string.Join("; ", assessment.Diagnostics));
        Assert.Equal(40, assessment.Size);
        Assert.Equal(deck, PlayerDeckAdmission.Admit(new(deck), Signature, 0, Cards, "04031a,04031b",
            DeckAspectRule.EqualTwoAspects).Select(card => card.Spec));
        Assert.Contains(PlayerDeckAssessment.Assess(new(deck), Signature, "04031a,04031b", Cards).Diagnostics,
            item => item.Code == "multiple-aspects");
    }

    [Fact]
    public void ImbalanceBlocksSavingButCanBeCorrectedThroughQuantityOffers()
    {
        string[] deck = [.. Signature, .. Customization, "01056"];
        var assessment = Assess(deck);
        Assert.False(assessment.IsValid);
        Assert.True(assessment.CanExtend);
        Assert.Contains(assessment.Diagnostics, item => item.Code == "aspect-balance" && item.Actual == 1);
        Assert.Throws<ArgumentException>(() => PlayerDeckAdmission.Admit(new(deck), Signature, 0, Cards,
            "04031a,04031b", DeckAspectRule.EqualTwoAspects));
        var quantities = CoreDeckQuantities.Assess(deck, Hero, Cards, deck.Append("01064").ToHashSet());
        Assert.Contains(2, quantities["01064"]);
        Assert.True(Assess([.. deck, "01064"]).IsValid);
    }

    [Fact]
    public void ThirdCustomizationAspectIsRejectedAndImbalanceCannotExceedRemainingSpace()
    {
        string[] third = [.. Signature, .. Customization, "01070"];
        Assert.Contains(Assess(third).Diagnostics, item => item.Code == "multiple-aspects");
        Assert.False(Assess(third).CanExtend);
        string[] full = [.. Signature, .. Customization, "01056", "01086", "01087", "01087", "01087",
            "01088", "01089", "01090", "01091", "01092"];
        var assessment = Assess(full);
        Assert.Equal(50, assessment.Size);
        Assert.False(assessment.CanExtend);
        Assert.Equal("aspect-balance", Assert.Single(assessment.Diagnostics).Code);
        Assert.True(Assess(full[..^1]).CanExtend);
    }

    [Fact]
    public void EmptyAspectSelectionAllowsBuildingAndSignatureCopiesRemainMandatory()
    {
        Assert.True(Assess(Signature).CanExtend);
        Assert.True(Assess([.. Signature, "01052"]).CanExtend);
        Assert.False(Assess([.. Signature, "01052"]).IsValid);
        var missing = Signature.Skip(1).Concat(Customization).ToArray();
        Assert.Contains(Assess(missing).Diagnostics, item => item.Code == "signature-quantity");
        Assert.False(Assess(missing).CanExtend);
    }

    private static DeckAssessment Assess(string[] deck) => CoreDeckAssessment.Assess(deck, Hero, Cards, deck.ToHashSet());

    [Fact]
    public void SetupMetadataAppliesToStarterSubstitutionsAsWellAsExplicitRecipes()
    {
        var root = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")))!;
        root["heroes"]!["synthetic_spider_woman"] = JsonSerializer.SerializeToNode(new
        {
            name = Hero.Name, hero = Hero.Hero, hero_deck = Signature, obligations = Array.Empty<string>(),
            nemesis_set = Array.Empty<string>(), nemesis_set_icon = Hero.NemesisSetIcon, player_deck = Customization,
            aspect_rule = "equal_two_aspects",
        });
        var catalog = SetupCatalog.Parse(root.ToJsonString());
        Assert.Equal(DeckAspectRule.EqualTwoAspects, catalog.Hero("synthetic_spider_woman").AspectRule);
        Assert.NotEmpty(Dealer.DealOrder(catalog, "rhino", ["synthetic_spider_woman"], facts: Cards));
        Assert.Throws<ArgumentException>(() => Dealer.DealOrder(catalog, "rhino", ["synthetic_spider_woman"],
            playerDeckNames: ["spider_man"], facts: Cards));
        Assert.Throws<ArgumentException>(() => Dealer.DealOrder(catalog, "rhino", ["spider_man"],
            playerDeckNames: ["synthetic_spider_woman"], facts: Cards));
        root["heroes"]!["synthetic_spider_woman"]!["aspect_rule"] = "unknown";
        Assert.Throws<JsonException>(() => SetupCatalog.Parse(root.ToJsonString()));
    }
}
