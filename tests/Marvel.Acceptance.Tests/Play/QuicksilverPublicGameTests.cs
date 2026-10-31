using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Unarranged public starter games; deterministic policy losses are complete games.</summary>
public sealed class QuicksilverPublicGameTests(ITestOutputHelper output)
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
        PublicStarterGames.Play("quicksilver", scenario, multiplayer, seed, output);

    [Theory]
    [InlineData("rhino", 9u)]
    [InlineData("klaw", 10u)]
    [InlineData("crossbones", 11u)]
    public void QuicksilverResponsesResolveThroughACompleteReplayedGame(string scenario, uint seed)
    {
        // The research policy declines optional responses. This game takes every
        // free optional ability Quicksilver's own cards offer: Super Speed after a
        // basic power and Friction Resistance after he readies.
        int taken = 0;
        PublicStarterGames.Play("quicksilver", scenario, false, seed, output, (game, prompt) =>
        {
            if (!prompt.Cancellable || prompt.Player != 0 || prompt.Asking == Question.TurnOption) return null;
            var own = prompt.Affordances.FirstOrDefault(offer => offer.CostOptions.Count == 0 && offer.Targets is null
                && offer.AnchorId >= 0 && offer.AnchorId < game.State.Cards.Count
                && game.State.Cards[offer.AnchorId].FaceId.StartsWith("14", StringComparison.Ordinal));
            if (own is null) return null;
            taken++;
            return Decision.Take(own.Id);
        });
        output.WriteLine($"Quicksilver optional abilities taken: {taken}");
        Assert.True(taken > 0);
    }
}
