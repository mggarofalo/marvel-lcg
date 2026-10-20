using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Unarranged games exercise both expansion heroes across all five scenarios and modes.</summary>
public sealed class RiseOfRedSkullGameMatrixTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    public static TheoryData<string, string, uint> Matchups
    {
        get
        {
            var cases = new TheoryData<string, string, uint>();
            uint seed = 700;
            foreach (string hero in new[] { "hawkeye", "spider_woman" })
                foreach (string scenario in new[] { "crossbones", "absorbing_man", "taskmaster", "zola", "red_skull" })
                    foreach (string suffix in new[] { "", "_expert" })
                        cases.Add(hero, scenario + suffix, seed++);
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(Matchups))]
    public void EveryNewHeroScenarioAndModeReachesAnEnding(string hero, string scenario, uint seed)
    {
        var played = Play(hero, scenario, seed);
        Assert.NotEqual(Outcome.Unfinished, played.World.Result);
        Assert.True(played.Policy.CardsPlayed > 0, "No cards were played");
        Assert.True(played.Policy.Payments > 0, "No costs were paid");
        Assert.True(played.Policy.PlayerAttacks > 0, "No player attacks were made");
    }

    [Theory]
    [InlineData("hawkeye", "crossbones", 700u)]
    [InlineData("spider_woman", "red_skull_expert", 719u)]
    public void FullGamesReproduceTheSameDecisionCountOutcomeAndCardState(string hero, string scenario, uint seed)
    {
        var first = Play(hero, scenario, seed);
        var replay = Play(hero, scenario, seed);
        Assert.Equal(first.Policy.Answered, replay.Policy.Answered);
        Assert.Equal(first.World.Result, replay.World.Result);
        Assert.Equal(first.World.Digest().Canonical(), replay.World.Digest().Canonical());
    }

    private static Played Play(string hero, string scenario, uint seed)
    {
        var abilities = AuthoredCards.Runner();
        var world = WorldSetup.Deal(Cards,
            Blueprints.From(Dealer.DealOrder(Setup, scenario, [hero]), Cards),
            [Setup.Hero(hero).Name], seed, abilities, expert: Setup.Campaign(scenario).Expert);
        var game = Game.Begin(world, Cards, abilities);
        var policy = new CoreGamePolicy(Cards);
        for (int count = 0; game.Pending is not null; count++)
        {
            Assert.True(count < 600,
                $"{hero}/{scenario}/{seed} still playing at {game.Pending.Label} after {count} decisions");
            game.Resolve(policy.Answer(game));
        }
        return new(world, policy);
    }

    private sealed record Played(World World, CoreGamePolicy Policy);
}
