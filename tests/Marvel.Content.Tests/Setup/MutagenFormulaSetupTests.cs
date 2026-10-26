using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

// Direct dealer evidence complements the public factory acceptance tests.
public sealed class MutagenFormulaSetupTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly string[] Heroes = ["spider_man", "captain_marvel", "she_hulk", "iron_man"];
    private static readonly string[] Encounters =
    [
        "02019", "02020", "02021", "02022", "02023", "02023", "02023", "02023",
        "02024", "02024", "02024", "02024", "02024", "02024", "02025", "02026",
        "02026", "02027", "02028", "02028", "02029", "02029", "02030", "02031", "02032", "02032",
    ];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecipesMatchPrintedContentsAndPinnedPhysicalQuantities(bool expert)
    {
        // 02017a Contents selects I/II or II/III, Standard, and one modular set.
        // Pinned gob_encounter quantities supply all 26 encounter cards.
        var recipe = Setup.Campaign(expert ? "mutagen_formula_expert" : "mutagen_formula");
        Assert.Equal(expert, recipe.Expert);
        Assert.Equal(expert ? ["02015", "02016"] : ["02014", "02015"], recipe.Villain);
        Assert.Equal(["02017a,02017b", "02018a,02018b"], recipe.Schemes);
        Assert.Empty(recipe.SetAside);
        Assert.Equal(Encounters, recipe.Encounters);
        Assert.Equal(expert ? ["standard", "expert"] : ["standard"], recipe.EncounterSets);
        Assert.Equal(["goblin_gimmicks"], recipe.ModularSets);
        Assert.Equal(EncounterSetRole.Modular, Setup.EncounterRole("goblin_gimmicks"));
        Assert.Equal(["02033", "02033", "02034", "02034", "02035", "02035", "02036", "02036"],
            Setup.EncounterSet("goblin_gimmicks"));
        var order = Dealer.DealOrder(Setup, expert ? "mutagen_formula_expert" : "mutagen_formula", ["spider_man"], facts: Cards);
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
    public void AuthoredSetupEngagesOneThrallPerPlayerAndAdvancesOnce(bool expert, int players)
    {
        // 02017a: "Put a Goblin Thrall minion into play engaged with each player."
        // Appendix II: "Flip the main scheme to side 1B."
        string scenario = expert ? "mutagen_formula_expert" : "mutagen_formula";
        var heroes = Heroes.Take(players).ToArray();
        var plan = ScenarioDealer.Prepare(Setup, new DealRequest(scenario, heroes, ["bomb_scare"]), Cards);
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        var world = WorldSetup.Deal(Cards, plan.Blueprints, heroes, 690, runner, events, expert);
        var game = Game.Begin(world, Cards, runner);
        CompleteSetupOrders(game);
        Assert.All(world.Seats, seat => Assert.Equal(expert ? 2 : 0,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat.Index)).Cards.Count));

        Assert.Equal(GamePhase.Mulligan, game.Phase);
        Assert.Equal(expert ? "02015" : "02014", world.TheCardIn(DeckType.VillainArea)!.FaceId);
        Assert.Equal(expert ? "02016" : "02015", world.TheCardIn(DeckType.VillainDeck)!.FaceId);
        Assert.All(world.Seats, seat => Assert.Equal("02024", Assert.Single(
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat.Index)).Cards).FaceId));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.Equal("02017b", scheme.FaceId);
        Assert.Equal(2L * players, scheme.Tokens["k_threat"]);
        Assert.Equal("02018a", world.TheCardIn(DeckType.MainSchemesDeck)!.FaceId);
        Assert.Equal(26 + 7 + 6 + (expert ? 3 : 0) - (expert ? 2 * players : 0), world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(6 - players, world.Cards.Count(card => card.FaceId == "02024"
            && card.Area.Type is DeckType.EncounterDeck or DeckType.DealtEncounterCardsDeck));
        Assert.DoesNotContain(world.Cards, card => card.FaceId is "02033" or "02034" or "02035" or "02036");
        Assert.All(world.Seats, seat => Assert.Equal(6, seat.Hand.Cards.Count));
        Assert.False(world.Agenda.IsBusy);
        if (!expert) AssertSetupShuffles(world, players);
    }

    private static void CompleteSetupOrders(Game game)
    {
        for (int i = 0; game.Phase == GamePhase.ScenarioSetup && i < 4; i++)
        {
            var prompt = game.Pending!;
            Assert.Equal(Question.Order, prompt.Asking);
            var order = Assert.Single(prompt.Affordances);
            game.Resolve(Decision.Take(order.Id, order.Targets!.Legal, []));
        }
    }

    private static void AssertSetupShuffles(World world, int players)
    {
        // Every starter has forty cards. Appendix II shuffles player decks and
        // the assembled encounter deck; printed 02017a then recruits Thralls and shuffles it again.
        // Compare the whole stream state to distinguish a missing or extra shuffle.
        var expected = new Marvel.Core.Random.EngineRandom(690);
        for (int player = 0; player < players; player++)
            expected.Shuffle(Enumerable.Range(0, 40).ToArray());
        int count = world.AreaOf(DeckType.EncounterDeck).Cards.Count;
        expected.Shuffle(Enumerable.Range(0, count + players).ToArray());
        expected.Shuffle(Enumerable.Range(0, count).ToArray());
        Assert.Equal(expected.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal(expected.Generator.GetState().Words, world.Random.Generator.GetState().Words);
    }

    [Fact]
    public void IncompleteProductRecipesRemainExcluded()
    {
        // Complete-product acceptance is repository policy, independent of authoring.
        var admitted = RuntimeProductAdmission.Discover(Setup, Cards, Cards.Faces.Where(face => face != "02049").ToHashSet(StringComparer.Ordinal));
        Assert.DoesNotContain("mutagen_formula", admitted.Setup.CampaignNames);
        Assert.DoesNotContain("mutagen_formula_expert", admitted.Setup.CampaignNames);
        Assert.DoesNotContain("goblin_gimmicks", admitted.Setup.EncounterSetNames);
        Assert.DoesNotContain("02017b", admitted.Playable);
        Assert.Contains("02033", AuthoredCards.Book.Authored);
        Assert.Contains("rhino", admitted.Setup.CampaignNames);
    }
}
