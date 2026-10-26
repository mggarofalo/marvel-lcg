using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicGreenGoblinSetupTests
{
    private static readonly string[] Scenarios = ["risky_business", "risky_business_expert", "mutagen_formula", "mutagen_formula_expert"];
    private static readonly string[] Modulars = ["goblin_gimmicks", "a_mess_of_things", "power_drain", "running_interference"];
    private static readonly string[] Heroes = ["spider_man", "captain_marvel", "she_hulk", "iron_man"];

    [Fact]
    public void SessionDiscoveryOffersEveryModeAndModularSetWithoutMsMarvel()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var response = new EngineHost(factory).Exchange(EngineRequest.ReadSetup("setup"));
        Assert.Null(response.Error);
        var choices = EngineJson.ReadResponse(EngineJson.Write(response)).Setup!;
        foreach (string key in Scenarios)
        {
            var choice = Assert.Single(choices.Scenarios, choice => choice.Key == key);
            Assert.Equal(key.EndsWith("_expert", StringComparison.Ordinal), choice.Expert);
            Assert.True(choice.AllowsModularSets);
            Assert.Equal(["goblin_gimmicks"], choice.RecommendedModularSets);
        }
        Assert.All(Modulars, key => Assert.Contains(choices.ModularSets, choice => choice.Key == key));
        Assert.DoesNotContain(choices.Heroes, choice => choice.Key == "ms_marvel");
    }

    [Fact]
    public void ProductionFactoryDealsEveryModeAtOneThroughFourPlayers()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        foreach (string scenario in Scenarios)
        foreach (int players in Enumerable.Range(1, 4))
        {
            var game = factory.Create(new GameSpecification(scenario, Heroes.Take(players).ToArray(), null, 693)).Game;
            CompleteSetup(game);
            bool risky = scenario.StartsWith("risky", StringComparison.Ordinal);
            bool expert = scenario.EndsWith("_expert", StringComparison.Ordinal);
            Assert.Equal(GamePhase.Mulligan, game.Phase);
            Assert.Equal(players, game.State.Seats.Count);
            Assert.Equal(risky ? expert ? "02002a" : "02001a" : expert ? "02015" : "02014",
                game.State.TheCardIn(DeckType.VillainArea)!.FaceId);
            Assert.Equal(risky ? "02004b" : "02017b", game.State.TheCardIn(DeckType.MainSchemesArea)!.FaceId);
            Assert.Equal(8, game.State.Cards.Count(card => card.FaceId is "02033" or "02034" or "02035" or "02036"));
            if (!risky) Assert.All(game.State.Seats, seat => Assert.Equal("02024", Assert.Single(
                game.State.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat.Index)).Cards).FaceId));
        }
    }

    [Fact]
    public void ValidCustomDeckAndEveryGoblinModularRemainAvailableInCoreRedSkullAndBothGoblinScenarios()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var hero = factory.DiscoverDecks().Heroes.Single(hero => hero.Key == "spider_man");
        string[] deck = [.. hero.Starter.SkipLast(1), "01086"];
        Assert.NotEqual(hero.Starter.ToArray(), deck);
        Assert.True(factory.AssessDeck(hero.Key, deck).IsValid);
        foreach (string scenario in new[] { "rhino", "absorbing_man", "risky_business", "mutagen_formula" })
        foreach (string modular in Modulars)
        {
            var game = factory.Create(new GameSpecification(scenario, [hero.Key], [modular], 693, [new(deck)])).Game;
            CompleteSetup(game);
            string[] faces = modular switch
            {
                "goblin_gimmicks" => ["02033", "02034", "02035", "02036"],
                "a_mess_of_things" => ["02037", "02038", "02039", "02040"],
                "power_drain" => ["02041", "02042", "02043", "02044", "02045"],
                _ => ["02046", "02047", "02048", "02049"],
            };
            Assert.Equal(modular == "goblin_gimmicks" ? 8 : 5, game.State.Cards.Count(card => faces.Contains(card.FaceId)));
            if (modular != "goblin_gimmicks") Assert.DoesNotContain(game.State.Cards,
                card => card.FaceId is "02033" or "02034" or "02035" or "02036");
        }
    }

    private static void CompleteSetup(Game game)
    {
        for (int index = 0; game.Phase == GamePhase.ScenarioSetup && index < 8; index++)
        {
            var prompt = game.Pending!;
            Assert.Equal(Question.Order, prompt.Asking);
            var order = Assert.Single(prompt.Affordances);
            game.Resolve(Decision.Take(order.Id, order.Targets!.Legal, []));
        }
    }
}
