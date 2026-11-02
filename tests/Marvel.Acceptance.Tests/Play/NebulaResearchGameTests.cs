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
/// Research-only complete Nebula games. The scenario is withheld with Ship Command, so direct
/// setup exercises the executable book without public discovery.
/// </summary>
public sealed class NebulaResearchGameTests(ITestOutputHelper output)
{
    private const string Ship = "16093";
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(false, false, 1u)]
    [InlineData(true, false, 2u)]
    [InlineData(false, true, 3u)]
    [InlineData(true, true, 4u)]
    [Rule("rr:appendix-ii-setup.step.12.a")]
    public void SetupPutsTheShipsIntoPlayAndAttachesTheDiscardedTechniquesToNebula(bool multiplayer, bool expert, uint seed)
    {
        // 16091a: "Setup: Put the Nebula's Ship environment and the Milano support into play.
        // Attach the Power Stone to Nebula. Discard the top 2 per hero cards of the encounter
        // deck, then attach each Technique attachment discarded this way to Nebula."
        var game = Deal(multiplayer, seed, expert);
        var world = game.State;
        var nebula = world.TheCardIn(DeckType.VillainArea)!;
        var ship = Assert.Single(world.Cards, card => card.FaceId == Ship);
        var milano = Assert.Single(world.Cards, card => card.FaceId == "16142");
        var stone = Assert.Single(world.Cards, card => card.FaceId == "16149");
        var techniques = world.Cards.Where(card => Traits.Has(world, card, "TECHNIQUE", world.Facts)
            && card.Area.Host == nebula.ObjectId).ToList();
        var discarded = world.AreaOf(DeckType.EncounterDiscardPile).Cards;

        Assert.Equal(DeckType.EnvironmentArea, ship.Area.Type);
        Assert.Equal(PlayArea.Of(world.FirstPlayer), milano.Area.PlayArea);
        Assert.Equal(nebula.ObjectId, stone.Area.Host);
        Assert.Equal(expert ? "16089" : "16088", nebula.FaceId);
        Assert.Equal("16091b", world.TheCardIn(DeckType.MainSchemesArea)!.FaceId);
        Assert.DoesNotContain(discarded, card => Traits.Has(world, card, "TECHNIQUE", world.Facts));
        Assert.Equal(multiplayer ? 4 : 2, discarded.Count + techniques.Count);
        output.WriteLine($"seed={seed}: techniques attached={techniques.Count}");
    }

    [Theory]
    [InlineData(false, false, 1u)]
    [InlineData(false, false, 2u)]
    [InlineData(false, true, 3u)]
    [InlineData(true, false, 4u)]
    [InlineData(true, false, 5u)]
    [InlineData(true, true, 6u)]
    public void NebulaGamesReachAnEndingWithExactDecisionReplay(bool multiplayer, bool expert, uint seed)
    {
        var game = Deal(multiplayer, seed, expert);
        var replay = Deal(multiplayer, seed, expert);
        var policy = new CoreGamePolicy(Cards);
        long evasion = 0;
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
            evasion = Math.Max(evasion, game.State.Cards.Single(card => card.FaceId == Ship).Tokens.GetValueOrDefault("c_evasion"));
        }
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Null(replay.Pending);
        output.WriteLine($"multiplayer={multiplayer}, expert={expert}, seed={seed}: {game.State.Result}; round={game.Round}, "
            + $"decisions={policy.Answered}, rngWords={game.State.Random.Generator.WordsConsumed}, maxEvasion={evasion}, "
            + $"stage={game.State.TheCardIn(DeckType.VillainArea)?.FaceId}, scheme={game.State.TheCardIn(DeckType.MainSchemesArea)?.FaceId}");
    }

    private static Game Deal(bool multiplayer, uint seed, bool expert)
    {
        string[] heroes = multiplayer ? ["she_hulk", "groot"] : ["she_hulk"];
        var plan = ScenarioDealer.Prepare(Setup, new(expert ? "nebula_expert" : "nebula", heroes, null), Cards);
        var abilities = AuthoredCards.Runner();
        var world = WorldSetup.DealConfigured(Cards, plan.Blueprints,
            [.. heroes.Select(hero => Setup.Hero(hero).Name)], seed, abilities,
            new(Expert: expert, EncounterGroups: plan.EncounterGroups));
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
