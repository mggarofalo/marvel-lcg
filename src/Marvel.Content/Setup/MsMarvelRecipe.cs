using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Pins the playable composition on the published Ms. Marvel divider.</summary>
internal static class MsMarvelRecipe
{
    // Repository admission policy pins the FFG divider transcription in
    // datasets/rules-packs/mc05/UPSTREAM.md, including the 40-card starter.
    internal static bool Complete(HeroSetup hero) =>
        hero.Hero.SequenceEqual(["05001a,05001b"])
        && hero.Obligations.SequenceEqual(["05025"])
        && hero.NemesisSet.SequenceEqual(["05026", "05027", "05028", "05029", "05029"])
        && hero.NemesisSetIcon == "ms_marvel_nemesis"
        && hero.AspectRule == DeckAspectRule.Standard
        && Quantities(hero.HeroDeck, 2, [1, 3, 3, 2, 1, 1, 1, 1, 1, 1])
        && Quantities(hero.PlayerDeck, 12, [1, 2, 3, 3, 2, 3, 1, 1, 1, 1, 1, 3, 3]);

    private static bool Quantities(IReadOnlyList<string> actual, int first, int[] counts) =>
        actual.Order(StringComparer.Ordinal).SequenceEqual(counts.SelectMany((count, index) =>
            Enumerable.Repeat($"05{first + index:000}", count)));
}
