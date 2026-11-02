using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>
/// Unarranged public starter games with Kree Militants, Menagerie Medley or Space Pirates as
/// the modular set, each reaching an ending with exact decision replay.
/// </summary>
public sealed class GalaxyModularPublicGameTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("kree_militant", "she_hulk", "rhino", false, 1u)]
    [InlineData("kree_militant", "groot", "klaw", true, 2u)]
    [InlineData("menagerie_medley", "spider_man", "ultron", false, 3u)]
    [InlineData("menagerie_medley", "rocket_raccoon", "rhino", true, 4u)]
    [InlineData("space_pirates", "captain_marvel", "klaw", false, 5u)]
    [InlineData("space_pirates", "she_hulk", "absorbing_man", true, 7u)]
    public void ModularGamesReachAnEndingWithExactDecisionReplay(
        string set, string hero, string scenario, bool multiplayer, uint seed)
    {
        // In each of these games a card of the chosen set is in play at some decision.
        var faces = GalaxyModularFaces.Of(set);
        int inPlay = 0;
        PublicStarterGames.Play(hero, scenario, multiplayer, seed, output, (game, _) =>
        {
            if (game.State.Cards.Any(card => faces.Contains(card.FaceId) && DeckTypes.IsInPlay(card.Area.Type))) inPlay++;
            return null;
        }, modulars: [set]);
        output.WriteLine($"Prompts answered with a {set} card in play: {inPlay}");
        Assert.True(inPlay > 0);
    }
}
