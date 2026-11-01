using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Unarranged public starter games; deterministic policy losses are complete games.</summary>
public sealed class RocketPublicGameTests(ITestOutputHelper output)
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
        PublicStarterGames.Play("rocket_raccoon", scenario, multiplayer, seed, output);

    [Theory]
    [InlineData("rhino", 9u)]
    [InlineData("klaw", 10u)]
    [InlineData("crossbones", 11u)]
    public void RocketAbilitiesResolveThroughACompleteReplayedGame(string scenario, uint seed)
    {
        // The research policy declines optional abilities. This game takes every free
        // optional ability Rocket Raccoon's own cards offer outside the turn menu, such
        // as "Murdered You!" draws, Follow Through and the responses of his allies.
        int taken = 0;
        PublicStarterGames.Play("rocket_raccoon", scenario, false, seed, output, (game, prompt) =>
        {
            if (!prompt.Cancellable || prompt.Player != 0) return null;
            var own = prompt.Affordances.FirstOrDefault(offer => offer.CostOptions.Count == 0 && offer.Targets is null
                && offer.Verb != Game.ChangeForm
                && offer.AnchorId >= 0 && offer.AnchorId < game.State.Cards.Count
                && IsRocketFace(game.State.Cards[offer.AnchorId].FaceId)
                && game.State.Cards[offer.AnchorId].Area.Type != Marvel.Rules.State.DeckType.HandsArea);
            if (own is null) return null;
            taken++;
            return Decision.Take(own.Id);
        });
        output.WriteLine($"Rocket Raccoon optional abilities taken: {taken}");
        Assert.True(taken > 0);
    }

    [Theory]
    [InlineData("rhino", 12u)]
    [InlineData("absorbing_man", 21u)]
    public void FloraAndFaunaChargesARocketUpgradeAtAGrootAndRocketTable(string scenario, uint seed)
    {
        // Flora and Fauna (16020, 16048), a Team-Up for Groot and Rocket Raccoon, offers
        // its Rocket option while a Rocket Raccoon upgrade is in play. This game takes it.
        int taken = 0;
        PublicStarterGames.Play("rocket_raccoon", scenario, true, seed, output, (game, prompt) =>
        {
            if (prompt.Asking != Question.Option) return null;
            var rocket = prompt.Affordances.FirstOrDefault(offer => offer.Id == 1
                && prompt.ContextCardIds.Any(id => game.State.Cards[id].FaceId is "16020" or "16048"));
            if (rocket is null) return null;
            taken++;
            return Decision.Take(rocket.Id);
        }, partner: "groot");
        output.WriteLine($"Flora and Fauna Rocket options taken: {taken}");
        Assert.True(taken > 0);
    }

    internal static bool IsRocketFace(string face) =>
        string.CompareOrdinal(face, "16029") >= 0 && string.CompareOrdinal(face, "16058") < 0;
}
