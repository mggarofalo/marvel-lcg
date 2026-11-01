using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Pins playable composition to the published Scarlet Witch starter.</summary>
internal static class ScarletWitchRecipe
{
    // Repository admission policy pins the vendored MarvelSDB starter quantities:
    // 15 signature, 16 Justice and 9 Basic; both obligations and the nemesis are separate.
    internal static bool Complete(HeroSetup hero) =>
        hero.Hero.SequenceEqual(["15001a,15001b"])
        && hero.Obligations.SequenceEqual(["15023", "15023"])
        && hero.NemesisSet.SequenceEqual(["15024", "15025", "15026", "15027"])
        && hero.NemesisSetIcon == "scw_nemesis"
        && hero.AspectRule == DeckAspectRule.Standard
        && Quantities(hero.HeroDeck, 2, [1, 1, 4, 3, 1, 1, 3, 1])
        && Quantities(hero.PlayerDeck, 10, [1, 1, 3, 3, 3, 3, 2, 2, 1, 3, 1, 1, 1])
        && hero.InvocationDeck is null;

    private static bool Quantities(IReadOnlyList<string> actual, int first, int[] counts) =>
        actual.Order(StringComparer.Ordinal).SequenceEqual(counts.SelectMany((count, index) =>
            Enumerable.Repeat($"15{first + index:000}", count)));
}
