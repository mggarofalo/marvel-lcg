using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>Campaign composition boundaries; synthetic metadata does not claim playable expansion coverage.</summary>
public sealed class CampaignFactoryTests
{
    [Fact]
    public void NewlyCreatedCampaignCanAdmitItsFirstAttempt()
    {
        var definition = RiseOfRedSkullCampaign.Definition;
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var recipe = new PlayerDeckRecipe(["01002"]);
        var state = CampaignRules.Create(definition, [new("spider_man", recipe, 10)], true, 7);
        var specification = new GameSpecification("crossbones", ["spider_man"], [], 7,
            [recipe], CampaignRules.Setup(state));
        CampaignSetupAdmission.Validate(specification, definition, cards,
            cards.Faces.ToHashSet(StringComparer.Ordinal));
        Assert.Equal(0, specification.Campaign!.Attempt);
        Assert.Throws<ArgumentException>(() => CampaignSetupAdmission.Validate(
            specification with { Campaign = specification.Campaign with { Attempt = -1 } },
            definition, cards, cards.Faces.ToHashSet(StringComparer.Ordinal)));
    }

    [Fact]
    public void CampaignAssessmentAndQuantityOffersPermitOnlyAuthorizedSignatureRemoval()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var hero = setup.Hero("spider_man");
        string[] draft = hero.HeroDeck.Concat(hero.PlayerDeck).Where(face => face != "01002").Append("01093").ToArray();
        Assert.False(factory.AssessDeck("spider_man", draft).IsValid);
        Assert.True(factory.AssessCampaignDeck("spider_man", draft, ["01002"]).IsValid);
        Assert.Equal([0], factory.CampaignDeckQuantities("spider_man", draft, ["01002"])["01002"]);
        Assert.Contains(factory.AssessCampaignDeck("spider_man", [.. draft, "01002"], ["01002"]).Diagnostics,
            diagnostic => diagnostic.Code == "campaign-removed");
    }

    [Fact]
    public void APartialProductCannotAdvertiseTheCampaign()
    {
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var missingFace = cards.Faces.Where(face => cards.Pack(face) == "trors").Skip(1).ToHashSet(StringComparer.Ordinal);
        Assert.Null(RiseOfRedSkullAdmission.Discover(setup, cards, missingFace));
    }

    [Fact]
    public void CampaignOverlaysAllocateAfterOrdinaryCardsWithoutDuplicatingScenarioCaptives()
    {
        var definition = RiseOfRedSkullCampaign.Definition;
        CardBlueprint[] ordinary = [new("01001b,01001a", SetupSlot.Identity, 0),
            new("04097", SetupSlot.SetAside, -1)];
        var tech = definition.Tech[0];
        var obligation = new CampaignCard("obligation:0:0", "04163", CampaignCardRole.Obligation);
        var input = new CampaignGameSetup(definition.Product, true, CampaignStage.Taskmaster, 3,
            [new(0, [tech], 5, false, [obligation], false)], ["04072"], 0);
        var plan = CampaignBlueprints.From(ordinary, input, definition);
        Assert.Equal(ordinary, plan.Blueprints.Take(ordinary.Length));
        Assert.Equal(5, plan.Blueprints.Count);
        Assert.Equal(tech, plan.Bindings.Single(binding => binding.ObjectId == 2).Card);
        Assert.Equal(SetupSlot.PlayerDeck, plan.Blueprints[2].Slot);
        Assert.Equal(SetupSlot.SetAside, plan.Blueprints[3].Slot);
        Assert.Equal("captive:04097", plan.Bindings.Single(binding => binding.ObjectId == 1).Card.Key);
        Assert.Equal([4], plan.ExperimentalWeapons);
    }
}
