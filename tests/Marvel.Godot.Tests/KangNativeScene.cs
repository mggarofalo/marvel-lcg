using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>
/// Deals The Once and Future Kang for a native journey through <see cref="KangRecipeFactory"/>,
/// which bypasses public admission alone; Kang stays outside the public catalog.
/// </summary>
/// <remarks>
/// The native client asks for an admitted scenario and the named heroes; this test-only scene
/// keeps those heroes and replaces the scenario, modular set and seed with a recipe that the
/// insert-advised policy carries through the split (<c>kang-recipe-evidence.md</c>). The game opens
/// once every player has their own game area: the policy's prelude is a prepared component, not a
/// transcript the client watches.
/// </remarks>
internal static class KangNativeScene
{
    internal static readonly string[] Heroes = ["spider_man", "captain_marvel", "she_hulk", "iron_man"];

    internal static bool Handles(string contract) => contract.StartsWith("kang-", StringComparison.Ordinal);

    internal static OpenedGame Create(string contract, GameSpecification specification, KangRecipeFactory factory)
    {
        (string modular, uint seed, int players) = contract switch
        {
            "kang-2" => ("temporal", 1u, 2),
            "kang-3" => ("mot", 15u, 3),
            "kang-4" => ("temporal", 4u, 4),
            _ => throw new ArgumentException("no Kang recipe for this native contract", nameof(contract)),
        };
        Assert.Equal(Heroes.Take(players), specification.Heroes);
        OpenedGame opened = factory.Create(new GameSpecification("kang", [.. specification.Heroes], [modular], seed));
        // A recorded prelude, replayed identically each time the host reopens the game.
        var policy = new CoreGamePolicy(opened.Game.State.Facts);
        Func<Game, bool> opening = KangNativeCheckpoints.Opening(contract);
        while (!opening(opened.Game))
        {
            Assert.True(policy.Answered < 400, $"{contract} never separated the table");
            opened.Game.Resolve(KangInsertAdvice.Answer(policy, opened.Game));
        }
        return opened;
    }
}
