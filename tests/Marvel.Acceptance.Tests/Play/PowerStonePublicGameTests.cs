using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Unarranged public starter games with the Power Stone (16149) as the modular set.</summary>
public sealed class PowerStonePublicGameTests(ITestOutputHelper output)
{
    private const string PowerStone = "16149";

    [Theory]
    [InlineData("she_hulk", "rhino", false, 1u)]
    [InlineData("she_hulk", "klaw", false, 2u)]
    [InlineData("she_hulk", "ultron", false, 3u)]
    [InlineData("she_hulk", "absorbing_man", false, 14u)]
    [InlineData("she_hulk", "rhino", true, 5u)]
    [InlineData("rocket_raccoon", "klaw", true, 6u)]
    public void PowerStoneGamesReachAnEndingWithExactDecisionReplay(string hero, string scenario, bool multiplayer, uint seed)
    {
        // "Setup. Attach to the villain." The stone starts on the villain and, after a hero
        // or villain deals 3 or more damage to its host with a single attack, moves to that
        // attacker. In each of these games a hero takes it.
        bool started = false;
        int heroTurns = 0;
        PublicStarterGames.Play(hero, scenario, multiplayer, seed, output, (game, _) =>
        {
            var stone = game.State.Cards.Single(card => card.FaceId == PowerStone);
            var kind = game.State.Facts.Kind(game.State.Cards[stone.Area.Host].FaceId);
            if (!started) Assert.True(CardKinds.IsVillain(kind));
            started = true;
            if (DeckTypes.IsInPlay(stone.Area.Type) && kind is CardKind.Hero or CardKind.AlterEgo) heroTurns++;
            return null;
        }, modulars: ["power_stone"]);
        output.WriteLine($"Prompts answered while a hero held the Power Stone: {heroTurns}");
        Assert.True(heroTurns > 0);
    }
}
