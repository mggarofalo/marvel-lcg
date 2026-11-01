using System.Collections.Immutable;
using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Pins playable composition to the published Rocket Raccoon starter.</summary>
internal static class RocketRaccoonRecipe
{
    /// <summary>Rocket Raccoon's faces within The Galaxy's Most Wanted: identity, starter, obligation and nemesis.</summary>
    internal static readonly ImmutableArray<string> Faces =
        ["16029a", "16029b", .. Enumerable.Range(30, 28).Select(number => $"16{number:000}")];

    // Repository admission policy pins the vendored MarvelSDB starter quantities:
    // 15 signature, 17 Aggression and 8 Basic; obligation and nemesis are separate.
    internal static bool Complete(HeroSetup hero) =>
        hero.Hero.SequenceEqual(["16029a,16029b"])
        && hero.Obligations.SequenceEqual(["16053"])
        && hero.NemesisSet.SequenceEqual(["16054", "16055", "16056", "16057", "16057"])
        && hero.NemesisSetIcon == "rocket_nemesis"
        && hero.AspectRule == DeckAspectRule.Standard
        && Quantities(hero.HeroDeck, 30, [2, 2, 1, 2, 2, 1, 1, 1, 2, 1])
        && Quantities(hero.PlayerDeck, 40, [1, 2, 3, 3, 2, 3, 3, 1, 1, 1, 1, 1, 3])
        && hero.InvocationDeck is null;

    private static bool Quantities(IReadOnlyList<string> actual, int first, int[] counts) =>
        actual.Order(StringComparer.Ordinal).SequenceEqual(counts.SelectMany((count, index) =>
            Enumerable.Repeat($"16{first + index:000}", count)));
}
