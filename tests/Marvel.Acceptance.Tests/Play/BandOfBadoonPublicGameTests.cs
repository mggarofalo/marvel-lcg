using Marvel.Rules.State;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Unarranged public starter games with Band of Badoon (16117–16121) as the modular set.</summary>
public sealed class BandOfBadoonPublicGameTests(ITestOutputHelper output)
{
    private static readonly string[] Band = ["16117", "16118", "16119", "16120", "16121"];

    [Theory]
    [InlineData("she_hulk", "rhino", false, 1u)]
    [InlineData("she_hulk", "klaw", false, 2u)]
    [InlineData("she_hulk", "ultron", false, 3u)]
    [InlineData("she_hulk", "absorbing_man", false, 14u)]
    [InlineData("she_hulk", "rhino", true, 5u)]
    [InlineData("groot", "klaw", true, 6u)]
    public void BandOfBadoonGamesReachAnEndingWithExactDecisionReplay(string hero, string scenario, bool multiplayer, uint seed)
    {
        // The ten Band of Badoon cards replace the scenario's recommended modular set. In each
        // of these games at least one Badoon minion enters play engaged with a player.
        var engaged = new HashSet<int>();
        PublicStarterGames.Play(hero, scenario, multiplayer, seed, output, (game, _) =>
        {
            foreach (var card in game.State.Cards.Where(card => Band.Contains(card.FaceId)
                && card.Area.Type == DeckType.EngagedEnemiesArea))
                engaged.Add(card.ObjectId);
            return null;
        }, modulars: ["band_of_badoon"]);
        output.WriteLine($"Badoon minions engaged: {engaged.Count}");
        Assert.NotEmpty(engaged);
    }
}
