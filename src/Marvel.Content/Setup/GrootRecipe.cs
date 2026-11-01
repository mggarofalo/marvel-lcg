using System.Collections.Immutable;
using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Pins playable composition to the published Groot starter.</summary>
internal static class GrootRecipe
{
    /// <summary>Groot's faces within The Galaxy's Most Wanted: identity, starter, obligation and nemesis.</summary>
    internal static readonly ImmutableArray<string> Faces =
        ["16001a", "16001b", .. Enumerable.Range(2, 27).Select(number => $"16{number:000}")];

    // Repository admission policy pins the vendored MarvelSDB starter quantities:
    // 15 signature, 17 Protection and 8 Basic; obligation and nemesis are separate.
    internal static bool Complete(HeroSetup hero) =>
        hero.Hero.SequenceEqual(["16001a,16001b"])
        && hero.Obligations.SequenceEqual(["16025"])
        && hero.NemesisSet.SequenceEqual(["16026", "16027", "16028", "16028", "16028"])
        && hero.NemesisSetIcon == "groot_nemesis"
        && hero.AspectRule == DeckAspectRule.Standard
        && Quantities(hero.HeroDeck, 2, [2, 2, 2, 3, 1, 1, 1, 1, 1, 1])
        && Quantities(hero.PlayerDeck, 12, [1, 3, 3, 2, 3, 3, 2, 1, 1, 1, 1, 1, 3])
        && hero.InvocationDeck is null;

    private static bool Quantities(IReadOnlyList<string> actual, int first, int[] counts) =>
        actual.Order(StringComparer.Ordinal).SequenceEqual(counts.SelectMany((count, index) =>
            Enumerable.Repeat($"16{first + index:000}", count)));
}
