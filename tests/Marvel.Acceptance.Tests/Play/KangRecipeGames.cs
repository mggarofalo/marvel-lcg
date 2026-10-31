using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Plays a declared Kang recipe to its ending beside an exact same-seed replay.</summary>
internal static class KangRecipeGames
{
    internal static readonly string[] Heroes = ["spider_man", "captain_marvel", "she_hulk", "iron_man"];

    /// <summary>The policy's answer, after the insert's advice while the table is separated.</summary>
    internal static Decision Answer(CoreGamePolicy policy, Game game) =>
        KangInsertAdvice.Prefer(game, game.Pending!) ?? policy.Answer(game);

    internal static KangGameRecord Play(string scenario, string modular, int players, uint seed)
    {
        var specification = new GameSpecification(scenario, Heroes.Take(players).ToArray(), [modular], seed);
        var game = new KangRecipeFactory().Create(specification).Game;
        var replay = new KangRecipeFactory().Create(specification).Game;
        var policy = new CoreGamePolicy(game.State.Facts);
        bool split = false, reunited = false, final = false;
        int joins = 0;
        Assert.Equal(PublicStarterGames.Observe(game), PublicStarterGames.Observe(replay));
        while (game.Pending is { } prompt)
        {
            Assert.True(policy.Answered < 3000, $"{scenario}/{modular}/{players}/{seed}: {prompt.Label}");
            var answer = Answer(policy, game);
            joins += prompt.Trigger == KangJoinOperation.Join ? 1 : 0;
            Resolution result;
            try { result = game.Resolve(answer); }
            catch (Exception failure)
            {
                throw new InvalidOperationException($"{scenario}/{modular}/{players}/{seed}, decision={policy.Answered}; "
                    + $"prompt={JsonSerializer.Serialize(PromptRecord.From(prompt))}", failure);
            }
            Assert.Equal(PublicStarterGames.Observe(game, result), PublicStarterGames.Observe(replay, replay.Resolve(answer)));
            var world = game.State;
            split |= Places.Separated(world);
            reunited |= split && !Places.Separated(world)
                && world.AreaOf(DeckType.MainSchemesArea).Cards.Any(card => card.FaceId is "11013a" or "11013b");
            final |= world.AreaOf(DeckType.VillainArea).Cards.Any(card => card.FaceId is "11006" or "11039");
        }
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.Null(replay.Pending);
        return new(game.State.Result, game.Round, policy.Answered, split, joins, reunited, final,
            game.State.Random.Generator.WordsConsumed);
    }
}
