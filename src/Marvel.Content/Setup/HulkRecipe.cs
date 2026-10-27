using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Pins playable composition to the published Hulk divider.</summary>
internal static class HulkRecipe
{
    // Repository admission policy pins datasets/rules-packs/mc09/UPSTREAM.md:
    // 15 signature, 17 Aggression and 8 Basic; obligation and nemesis are separate.
    internal static bool Complete(HeroSetup hero) =>
        hero.Hero.SequenceEqual(["10001a,10001b"])
        && hero.Obligations.SequenceEqual(["10025"])
        && hero.NemesisSet.SequenceEqual(["10026", "10027", "10028", "10028", "10028"])
        && hero.NemesisSetIcon == "hulk_nemesis"
        && hero.AspectRule == DeckAspectRule.Standard
        && Quantities(hero.HeroDeck, 2, [2, 2, 2, 2, 2, 2, 1, 1, 1])
        && Quantities(hero.PlayerDeck, 11, [1, 1, 1, 3, 3, 3, 2, 3, 3, 1, 1, 1, 1, 1])
        && hero.InvocationDeck is null;

    private static bool Quantities(IReadOnlyList<string> actual, int first, int[] counts) =>
        actual.Order(StringComparer.Ordinal).SequenceEqual(counts.SelectMany((count, index) =>
            Enumerable.Repeat($"10{first + index:000}", count)));
}
