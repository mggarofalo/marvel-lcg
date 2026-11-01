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

/// <summary>Plays an unarranged public starter to its ending beside an exact same-seed replay.</summary>
internal static class PublicStarterGames
{
    /// <summary>Plays <paramref name="hero"/>'s published starter, with <paramref name="partner"/> when multiplayer.</summary>
    /// <remarks><paramref name="prefer"/> may answer a prompt before the policy does; the replay receives the same answer.</remarks>
    internal static SeatParticipation Play(string hero, string scenario, bool multiplayer, uint seed, ITestOutputHelper output,
        Func<Game, Prompt, Decision?>? prefer = null, string partner = "captain_marvel")
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        string[] heroes = multiplayer ? [hero, partner] : [hero];
        var specification = new GameSpecification(scenario, heroes, null, seed);
        var starter = factory.DiscoverDecks().Heroes.Single(item => item.Key == hero).Starter;
        Assert.Equal(40, starter.Count);
        Assert.True(factory.AssessDeck(hero, starter).IsValid);
        var game = factory.Create(specification).Game;
        Assert.Equal(starter.Order(StringComparer.Ordinal), game.State.Cards
            .Where(card => card.Owner == 0 && game.State.Facts.Kind(card.FaceId)
                is CardKind.Ally or CardKind.Event or CardKind.Support or CardKind.Upgrade or CardKind.Resource)
            .Select(card => card.FaceId).Order(StringComparer.Ordinal));
        var replay = factory.Create(specification).Game;
        var policy = new CoreGamePolicy(game.State.Facts);
        var seats = new SeatParticipation();
        Assert.Equal(Observe(game), Observe(replay));
        while (game.Pending is { } prompt)
        {
            Assert.True(policy.Answered < 2000, $"{scenario}, seed={seed}: {prompt.Label}");
            string beforePolicy = Observe(game);
            var answer = prefer?.Invoke(game, prompt) ?? policy.Answer(game);
            Assert.Equal(beforePolicy, Observe(game));
            seats.Record(prompt, answer);
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
        Assert.Equal(Enumerable.Range(0, heroes.Length), seats.Acting.Order());
        Assert.Equal(Enumerable.Range(0, heroes.Length), seats.Paid.Order());
        Assert.Equal(Enumerable.Range(0, heroes.Length), seats.Played.Order());
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.Null(replay.Pending);
        Assert.True(policy.CardsPlayed > 0);
        Assert.True(policy.PlayerAttacks > 0);
        output.WriteLine($"{scenario}, heroes={string.Join(',', heroes)}, seed={seed}: {game.State.Result}; "
            + $"round={game.Round}, decisions={policy.Answered}, cards={policy.CardsPlayed}, payments={policy.Payments}, "
            + $"attacks={policy.PlayerAttacks}, divisions={seats.Divisions}, stages={string.Join(',', policy.VillainStages.Order())}, "
            + $"rngWords={game.State.Random.Generator.WordsConsumed}");
        return seats;
    }

    internal static string Observe(Game game, Resolution? result = null) => JsonSerializer.Serialize(new
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
