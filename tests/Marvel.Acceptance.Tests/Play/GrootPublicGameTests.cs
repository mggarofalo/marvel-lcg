using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Unarranged public starter games; deterministic policy losses are complete games.</summary>
public sealed class GrootPublicGameTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("rhino", false, 1u)]
    [InlineData("klaw", false, 2u)]
    [InlineData("ultron", false, 3u)]
    [InlineData("crossbones", false, 4u)]
    [InlineData("absorbing_man", false, 5u)]
    [InlineData("wrecking_crew_standard", false, 6u)]
    [InlineData("rhino", true, 7u)]
    [InlineData("absorbing_man", true, 8u)]
    public void PublicStartersReachAnEndingWithExactDecisionReplay(string scenario, bool multiplayer, uint seed) =>
        PublicStarterGames.Play("groot", scenario, multiplayer, seed, output);

    [Theory]
    [InlineData("rhino", 9u)]
    [InlineData("klaw", 10u)]
    [InlineData("crossbones", 11u)]
    public void GrootAbilitiesResolveThroughACompleteReplayedGame(string scenario, uint seed)
    {
        // The research policy declines optional abilities. This game takes every free
        // optional ability Groot's own cards offer outside the turn menu, such as Growth
        // Spurt's counters reaching Flora Colossus and the responses of his upgrades.
        int taken = 0;
        PublicStarterGames.Play("groot", scenario, false, seed, output, (game, prompt) =>
        {
            if (!prompt.Cancellable || prompt.Player != 0) return null;
            var own = prompt.Affordances.FirstOrDefault(offer => offer.CostOptions.Count == 0 && offer.Targets is null
                && offer.Verb != Game.ChangeForm
                && offer.AnchorId >= 0 && offer.AnchorId < game.State.Cards.Count
                && game.State.Cards[offer.AnchorId].FaceId.StartsWith("16", StringComparison.Ordinal)
                && game.State.Cards[offer.AnchorId].Area.Type != Marvel.Rules.State.DeckType.HandsArea);
            if (own is null) return null;
            taken++;
            return Decision.Take(own.Id);
        });
        output.WriteLine($"Groot optional abilities taken: {taken}");
        Assert.True(taken > 0);
    }
}
