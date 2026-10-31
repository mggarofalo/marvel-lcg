using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Pins playable composition to the published Wasp starter.</summary>
internal static class WaspRecipe
{
    // Repository admission policy pins the vendored MarvelSDB starter quantities:
    // 15 signature, 16 Aggression and 9 Basic; obligation and nemesis are separate.
    internal static bool Complete(HeroSetup hero) =>
        hero.Hero.SequenceEqual(["13001a,13001b,13001c"])
        && hero.Obligations.SequenceEqual(["13026"])
        && hero.NemesisSet.SequenceEqual(["13027", "13028", "13029", "13030", "13030"])
        && hero.NemesisSetIcon == "wsp_nemesis"
        && hero.AspectRule == DeckAspectRule.Standard
        && Quantities(hero.HeroDeck, 2, [1, 2, 3, 2, 2, 2, 1, 1, 1])
        && Quantities(hero.PlayerDeck, 11, [1, 1, 3, 3, 2, 3, 3, 1, 1, 1, 1, 1, 1, 2, 1])
        && hero.InvocationDeck is null;

    private static bool Quantities(IReadOnlyList<string> actual, int first, int[] counts) =>
        actual.Order(StringComparer.Ordinal).SequenceEqual(counts.SelectMany((count, index) =>
            Enumerable.Repeat($"13{first + index:000}", count)));
}
