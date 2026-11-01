using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

// Kang stays outside public admission; KangRecipeFactory deals the declared recipe
// through the production dealer. Seeds were chosen from the recorded scan to cover
// both modes, each player count, each modular set, the split and the reunion.
public sealed class KangRecipeGameTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("kang", "anachronauts", 1, 1u, true)]
    [InlineData("kang", "mot", 2, 1u, true)]
    [InlineData("kang", "mot", 3, 15u, true)]
    [InlineData("kang", "temporal", 4, 4u, true)]
    [InlineData("kang_expert", "anachronauts", 1, 36u, true)]
    [InlineData("kang_expert", "mot", 2, 11u, true)]
    [InlineData("kang_expert", "temporal", 3, 1u, false)]
    [InlineData("kang_expert", "temporal", 4, 1u, false)]
    [Rule("rr:winning-the-game.1")]
    public void DeclaredRecipesPlayToAnEndingWithExactReplay(string scenario, string modular, int players, uint seed,
        bool reunites)
    {
        // pack:mc11:areas: at 2A "each player reveals their own random stage 3A"; 11008b: "When
        // all the players have joined this game area, advance to stage 4A." rr:winning-the-game.1:
        // the players lose when every player is eliminated or a final-stage scheme completes.
        var record = KangRecipeGames.Play(scenario, modular, players, seed);
        output.WriteLine($"{scenario}/{modular}/{players}/{seed}: {record}");
        Assert.Equal(reunites, record.Split);
        Assert.Equal(reunites, record.Reunited);
        Assert.Equal(reunites, record.FinalKang);
        Assert.NotEqual(Outcome.Unfinished, record.Outcome);
        if (reunites && players > 1) Assert.True(record.Joins > 0);
    }
}
