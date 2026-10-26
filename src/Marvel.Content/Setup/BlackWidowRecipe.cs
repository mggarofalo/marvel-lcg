using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Pins the playable composition on the published Black Widow divider.</summary>
internal static class BlackWidowRecipe
{
    // Repository admission policy pins the FFG divider transcription in
    // datasets/rules-packs/mc07/UPSTREAM.md: 15 signature, 17 Justice and 8 Basic.
    internal static bool Complete(HeroSetup hero) =>
        hero.Hero.SequenceEqual(["08001a,08001b"])
        && hero.Obligations.SequenceEqual(["08025"])
        && hero.NemesisSet.SequenceEqual(["08026", "08027", "08028", "08028", "08029"])
        && hero.NemesisSetIcon == "black_widow_nemesis"
        && hero.AspectRule == DeckAspectRule.Standard
        && Quantities(hero.HeroDeck, 2, [1, 2, 2, 1, 2, 2, 2, 1, 2])
        && Quantities(hero.PlayerDeck, 11, [1, 1, 3, 2, 2, 2, 3, 3, 1, 1, 1, 1, 1, 3]);

    private static bool Quantities(IReadOnlyList<string> actual, int first, int[] counts) =>
        actual.Order(StringComparer.Ordinal).SequenceEqual(counts.SelectMany((count, index) =>
            Enumerable.Repeat($"08{first + index:000}", count)));
}
