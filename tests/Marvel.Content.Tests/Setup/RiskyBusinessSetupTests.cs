using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

// Research uses the production dealer and authored abilities without public pack admission.
public sealed class RiskyBusinessSetupTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly string[] Heroes = ["spider_man", "captain_marvel", "she_hulk", "iron_man"];
    private static readonly string[] Encounters =
    [
        "02007", "02007", "02007", "02008", "02008", "02008", "02008", "02009",
        "02010", "02010", "02011", "02011", "02012", "02012", "02012", "02012", "02013", "02013",
    ];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecipesMatchPrintedContentsAndPinnedPhysicalQuantities(bool expert)
    {
        // 02004a Contents: Norman I/II (II/III expert), Risky Business and Standard,
        // and one modular set (recommended Goblin Gimmicks). MarvelSDB gob_encounter
        // pins eighteen encounter copies, three villain, two scheme and one environment cards.
        var recipe = Setup.Campaign(expert ? "risky_business_expert" : "risky_business");
        Assert.Equal(expert, recipe.Expert);
        Assert.Equal(expert ? ["02002a,02002b", "02003a,02003b"] : ["02001a,02001b", "02002a,02002b"], recipe.Villain);
        Assert.Equal(["02004a,02004b", "02005a,02005b"], recipe.Schemes);
        Assert.Equal(["02006a,02006b"], recipe.SetAside);
        Assert.Equal(Encounters, recipe.Encounters);
        Assert.Equal(expert ? ["standard", "expert"] : ["standard"], recipe.EncounterSets);
        Assert.Equal(["goblin_gimmicks"], recipe.ModularSets);
        Assert.Equal(EncounterSetRole.Modular, Setup.EncounterRole("goblin_gimmicks"));
        Assert.Equal(["02033", "02033", "02034", "02034", "02035", "02035", "02036", "02036"],
            Setup.EncounterSet("goblin_gimmicks"));
        var order = Dealer.DealOrder(Setup, expert ? "risky_business_expert" : "risky_business", ["spider_man"], facts: Cards);
        Assert.Equal(8, order.Count(card => Setup.EncounterSet("goblin_gimmicks").Contains(card.Spec)));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(false, 4)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [InlineData(true, 4)]
    [Rule("rr:appendix-ii-setup.step.12.b")]
    public void AuthoredSetupEntersCriminalEnterpriseAndAdvancesOnce(bool expert, int players)
    {
        // 02004a: "Put the Criminal Enterprise environment into play. Shuffle the
        // encounter deck. Advance to stage 1B." Appendix II: "Flip the main scheme
        // to side 1B." A single chosen Core modular set is the normal printed setup.
        string scenario = expert ? "risky_business_expert" : "risky_business";
        var heroes = Heroes.Take(players).ToArray();
        var plan = ScenarioDealer.Prepare(Setup, new DealRequest(scenario, heroes, ["bomb_scare"]), Cards);
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        var world = WorldSetup.Deal(Cards, plan.Blueprints, heroes, 690, runner, events, expert);
        var game = Game.Begin(world, Cards, runner);

        Assert.Equal(GamePhase.Mulligan, game.Phase);
        Assert.Equal(expert ? "02002a" : "02001a", world.TheCardIn(DeckType.VillainArea)!.FaceId);
        Assert.Equal(expert ? "02003a" : "02002a", world.TheCardIn(DeckType.VillainDeck)!.FaceId);
        var environment = Assert.Single(world.Cards, card => card.FaceId == "02006a");
        Assert.Equal(DeckType.EnvironmentArea, environment.Area.Type);
        Assert.Equal(2L * players, environment.Tokens["c_infamy"]);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.Equal("02004b", scheme.FaceId);
        Assert.Equal(2L * players, scheme.Tokens["k_threat"]);
        Assert.Equal("02005a", world.TheCardIn(DeckType.MainSchemesDeck)!.FaceId);
        Assert.Equal(18 + 7 + 6 + players + (expert ? 3 : 0), world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(Encounters.Order(), world.AreaOf(DeckType.EncounterDeck).Cards
            .Where(card => Encounters.Contains(card.FaceId)).Select(card => card.FaceId).Order());
        Assert.DoesNotContain(world.Cards, card => card.FaceId is "02033" or "02034" or "02035" or "02036");
        Assert.All(world.Seats, seat => Assert.Equal(6, seat.Hand.Cards.Count));
        Assert.False(world.Agenda.IsBusy);
        AssertSetupShuffles(world, players);
    }

    private static void AssertSetupShuffles(World world, int players)
    {
        // Every starter has forty cards. Appendix II shuffles player decks and
        // the assembled encounter deck; printed 02004a then shuffles it again.
        // Compare the whole stream state to distinguish a missing or extra shuffle.
        var expected = new Marvel.Core.Random.EngineRandom(690);
        for (int player = 0; player < players; player++)
            expected.Shuffle(Enumerable.Range(0, 40).ToArray());
        int count = world.AreaOf(DeckType.EncounterDeck).Cards.Count;
        expected.Shuffle(Enumerable.Range(0, count).ToArray());
        expected.Shuffle(Enumerable.Range(0, count).ToArray());
        Assert.Equal(expected.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal(expected.Generator.GetState().Words, world.Random.Generator.GetState().Words);
    }

    [Fact]
    public void ResearchRecipesRemainExcludedEvenWhenEveryFaceIsSupplied()
    {
        // Complete-product acceptance is repository policy, independent of authoring.
        var admitted = RuntimeProductAdmission.Discover(Setup, Cards, Cards.Faces.ToHashSet(StringComparer.Ordinal));
        Assert.DoesNotContain("risky_business", admitted.Setup.CampaignNames);
        Assert.DoesNotContain("risky_business_expert", admitted.Setup.CampaignNames);
        Assert.DoesNotContain("goblin_gimmicks", admitted.Setup.EncounterSetNames);
        Assert.DoesNotContain("02004b", admitted.Playable);
        Assert.False(AuthoredCards.Book.Authored.Contains("02033"));
        Assert.Contains("rhino", admitted.Setup.CampaignNames);
    }
}
