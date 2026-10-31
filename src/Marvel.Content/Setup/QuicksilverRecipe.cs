using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Pins playable composition to the published Quicksilver starter.</summary>
internal static class QuicksilverRecipe
{
    // Repository admission policy pins the vendored MarvelSDB starter quantities:
    // 15 signature, 15 Protection and 10 Basic; obligation and nemesis are separate.
    internal static bool Complete(HeroSetup hero) =>
        hero.Hero.SequenceEqual(["14001a,14001b"])
        && hero.Obligations.SequenceEqual(["14024"])
        && hero.NemesisSet.SequenceEqual(["14025", "14026", "14027", "14028", "14028"])
        && hero.NemesisSetIcon == "qsv_nemesis"
        && hero.AspectRule == DeckAspectRule.Standard
        && Quantities(hero.HeroDeck, 2, [1, 4, 2, 2, 1, 1, 1, 1, 1, 1])
        && Quantities(hero.PlayerDeck, 12, [3, 1, 3, 3, 2, 3, 1, 1, 1, 1, 3, 3])
        && hero.InvocationDeck is null;

    private static bool Quantities(IReadOnlyList<string> actual, int first, int[] counts) =>
        actual.Order(StringComparer.Ordinal).SequenceEqual(counts.SelectMany((count, index) =>
            Enumerable.Repeat($"14{first + index:000}", count)));
}
