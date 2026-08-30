using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Whole games driven toward the core box's two normal outcomes.</summary>
public sealed class GameOutcomeTests
{
    private const string RhinoOne = "01094";
    private const string RhinoTwo = "01095";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void ACompleteGameIsDeterministicAndTraversesBothVillainStages()
    {
        var first = Play(2794);
        var replay = Play(2794);

        Assert.Equal(Outcome.VillainWins, first.World.Result);
        Assert.Contains(RhinoOne, first.Policy.VillainStages);
        Assert.Contains(RhinoTwo, first.Policy.VillainStages);
        Assert.True(first.Policy.CardsPlayed > 0, "the player played no cards");
        Assert.True(first.Policy.Payments > 0, "the player paid no costs");
        Assert.True(
            first.Policy.ResourceAbilitiesUsed > 0,
            "the player used no resource abilities");
        Assert.True(first.Policy.PlayerAttacks > 0, "the player made no attacks");

        Assert.Equal(first.World.Result, replay.World.Result);
        Assert.Equal(first.Game.Round, replay.Game.Round);
        Assert.Equal(first.Policy.Answered, replay.Policy.Answered);
        Assert.Equal(first.World.Digest().Canonical(), replay.World.Digest().Canonical());
    }

    [Rule("rr:main-scheme-main-scheme-deck.2.1")]
    [Rule("rr:winning-the-game")]
    [Fact]
    public void TheSamePolicyCanLoseToTheFinalMainScheme()
    {
        // "If the final stage of the main scheme deck is completed, the
        // villain wins the game."
        var played = Play(1);

        Assert.Equal(Outcome.VillainWins, played.World.Result);
        Assert.True(played.Policy.CardsPlayed > 0, "the player played no cards");
        Assert.True(played.Policy.PlayerAttacks > 0, "the player made no attacks");
    }

    private static Played Play(uint seed)
    {
        var abilities = AuthoredCards.Runner();
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            seed,
            abilities);
        var game = Game.Begin(world, Cards, abilities);
        var policy = new CoreGamePolicy(Cards);

        for (int decisions = 0; game.Pending is not null; decisions++)
        {
            Assert.True(decisions < 600, $"still playing after {decisions} decisions");
            game.Resolve(policy.Answer(game));
        }

        return new Played(game, world, policy);
    }

    private sealed record Played(Game Game, World World, CoreGamePolicy Policy);
}
