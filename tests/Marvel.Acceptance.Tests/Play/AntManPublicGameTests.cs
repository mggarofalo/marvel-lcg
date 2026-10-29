using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Unarranged public starter games; deterministic policy losses are complete games.</summary>
public sealed class AntManPublicGameTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("rhino", false, 1u)]
    [InlineData("klaw", false, 2u)]
    [InlineData("ultron", false, 3u)]
    [InlineData("crossbones", false, 4u)]
    [InlineData("absorbing_man", false, 5u)]
    [InlineData("rhino", true, 6u)]
    [InlineData("absorbing_man", true, 7u)]
    public void PublicStartersReachAnEndingWithExactDecisionReplay(string scenario, bool multiplayer, uint seed)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        string[] heroes = multiplayer ? ["ant_man", "captain_marvel"] : ["ant_man"];
        var specification = new GameSpecification(scenario, heroes, null, seed);
        var starter = factory.DiscoverDecks().Heroes.Single(hero => hero.Key == "ant_man").Starter;
        Assert.Equal(40, starter.Count);
        Assert.True(factory.AssessDeck("ant_man", starter).IsValid);
        var game = factory.Create(specification).Game;
        Assert.Equal(starter.Order(StringComparer.Ordinal), game.State.Cards
            .Where(card => card.Owner == 0 && game.State.Facts.Kind(card.FaceId)
                is CardKind.Ally or CardKind.Event or CardKind.Support or CardKind.Upgrade or CardKind.Resource)
            .Select(card => card.FaceId).Order(StringComparer.Ordinal));
        var replay = factory.Create(specification).Game;
        var policy = new CoreGamePolicy(game.State.Facts);
        var acting = new HashSet<int>();
        var paid = new HashSet<int>();
        var played = new HashSet<int>();
        Assert.Equal(Observe(game), Observe(replay));
        while (game.Pending is { } prompt)
        {
            Assert.True(policy.Answered < 2000, $"{scenario}, seed={seed}: {prompt.Label}");
            if (prompt.Asking == Question.TurnOption) acting.Add(prompt.Player);
            string beforePolicy = Observe(game);
            var answer = policy.Answer(game);
            Assert.Equal(beforePolicy, Observe(game));
            if (answer.Spent.Count > 0) paid.Add(prompt.Player);
            var chosen = prompt.Affordances.FirstOrDefault(offer => offer.Id == answer.Affordance);
            if (chosen?.PlaysCard == true) played.Add(prompt.Player);
            Resolution result;
            try { result = game.Resolve(answer); }
            catch (Exception failure)
            {
                throw new InvalidOperationException($"{scenario}, multiplayer={multiplayer}, seed={seed}, "
                    + $"decision={policy.Answered}; prompt={JsonSerializer.Serialize(PromptRecord.From(prompt))}; "
                    + $"answer={JsonSerializer.Serialize(answer)}", failure);
            }
            var replayed = replay.Resolve(answer);
            Assert.Equal(Observe(game, result), Observe(replay, replayed));
        }
        Assert.Equal(Enumerable.Range(0, heroes.Length), acting.Order());
        Assert.Equal(Enumerable.Range(0, heroes.Length), paid.Order());
        Assert.Equal(Enumerable.Range(0, heroes.Length), played.Order());
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.Null(replay.Pending);
        Assert.True(policy.CardsPlayed > 0);
        Assert.True(policy.PlayerAttacks > 0);
        output.WriteLine($"{scenario}, heroes={string.Join(',', heroes)}, seed={seed}: {game.State.Result}; "
            + $"round={game.Round}, decisions={policy.Answered}, cards={policy.CardsPlayed}, payments={policy.Payments}, "
            + $"attacks={policy.PlayerAttacks}, stages={string.Join(',', policy.VillainStages.Order())}, "
            + $"rngWords={game.State.Random.Generator.WordsConsumed}");
    }

    private static string Observe(Game game, Resolution? result = null) => JsonSerializer.Serialize(new
    {
        State = game.State.Digest().Canonical(),
        RngWords = game.State.Random.Generator.WordsConsumed,
        Rng = game.State.Random.Generator.GetState(),
        game.State.Result,
        game.Phase,
        game.Round,
        Prompt = game.Pending is { } prompt ? PromptRecord.From(prompt) : null,
        result?.Events,
        result?.Information,
    });
}
