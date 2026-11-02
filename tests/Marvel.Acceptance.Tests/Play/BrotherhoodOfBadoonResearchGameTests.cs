using System.Text.Json;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>
/// Research-only complete Brotherhood of Badoon games. The scenario is withheld with Ship
/// Command, so direct setup exercises the executable book without public discovery.
/// </summary>
public sealed class BrotherhoodOfBadoonResearchGameTests(ITestOutputHelper output)
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:setup-keyword.1")]
    public void SetupPutsTheBadoonShipAndTheMilanoIntoPlay(bool multiplayer)
    {
        // 16061a: "Setup: Put the Badoon Ship environment and the Milano support into play."
        var game = Deal(multiplayer, 1);
        var ship = Assert.Single(game.State.Cards, card => card.FaceId == "16063");
        var milano = Assert.Single(game.State.Cards, card => card.FaceId == "16142");

        Assert.Equal(DeckType.EnvironmentArea, ship.Area.Type);
        Assert.Equal(DeckType.SupportsArea, milano.Area.Type);
        Assert.Equal(PlayArea.Of(game.State.FirstPlayer), milano.Area.PlayArea);
        Assert.Equal("16058", game.State.TheCardIn(DeckType.VillainArea)!.FaceId);
        Assert.Equal("16061b", game.State.TheCardIn(DeckType.MainSchemesArea)!.FaceId);
    }

    [Theory]
    [InlineData(false, 1u)]
    [InlineData(false, 2u)]
    [InlineData(false, 3u)]
    [InlineData(true, 4u)]
    [InlineData(true, 5u)]
    [InlineData(true, 6u)]
    public void BrotherhoodGamesReachAnEndingWithExactDecisionReplay(bool multiplayer, uint seed)
    {
        var game = Deal(multiplayer, seed);
        var replay = Deal(multiplayer, seed);
        var policy = new CoreGamePolicy(Cards);
        long charged = 0;
        Assert.Equal(Observe(game), Observe(replay));
        while (game.Pending is { } prompt)
        {
            Assert.True(policy.Answered < 3000, $"seed={seed}: {prompt.Label}");
            var answer = policy.Answer(game);
            Resolution result;
            try { result = game.Resolve(answer); }
            catch (Exception failure)
            {
                throw new InvalidOperationException($"multiplayer={multiplayer}, seed={seed}, "
                    + $"decision={policy.Answered}; prompt={JsonSerializer.Serialize(PromptRecord.From(prompt))}; "
                    + $"answer={JsonSerializer.Serialize(answer)}", failure);
            }
            Assert.Equal(Observe(game, result), Observe(replay, replay.Resolve(answer)));
            charged = Math.Max(charged, game.State.Cards.Single(card => card.FaceId == "16063").Tokens.GetValueOrDefault("c_barrage"));
        }
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Null(replay.Pending);
        Assert.True(charged > 0);
        output.WriteLine($"multiplayer={multiplayer}, seed={seed}: {game.State.Result}; round={game.Round}, "
            + $"decisions={policy.Answered}, rngWords={game.State.Random.Generator.WordsConsumed}, "
            + $"stage={game.State.TheCardIn(DeckType.VillainArea)?.FaceId}, scheme={game.State.TheCardIn(DeckType.MainSchemesArea)?.FaceId}");
    }

    private static Game Deal(bool multiplayer, uint seed)
    {
        string[] heroes = multiplayer ? ["she_hulk", "groot"] : ["she_hulk"];
        var plan = ScenarioDealer.Prepare(Setup, new("brotherhood_of_badoon", heroes, null), Cards);
        var abilities = AuthoredCards.Runner();
        var world = WorldSetup.DealConfigured(Cards, plan.Blueprints,
            [.. heroes.Select(hero => Setup.Hero(hero).Name)], seed, abilities,
            new(Expert: false, EncounterGroups: plan.EncounterGroups));
        return Game.Begin(world, Cards, abilities);
    }

    private static string Observe(Game game, Resolution? result = null) => JsonSerializer.Serialize(new
    {
        State = game.State.Digest().Canonical(),
        RngWords = game.State.Random.Generator.WordsConsumed,
        game.State.Result,
        game.Phase,
        game.Round,
        Prompt = game.Pending is { } prompt ? PromptRecord.From(prompt) : null,
        result?.Events,
        result?.Information,
    });
}
