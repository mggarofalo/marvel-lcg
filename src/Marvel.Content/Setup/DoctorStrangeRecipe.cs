using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Pins the playable composition on the published Doctor Strange divider.</summary>
internal static class DoctorStrangeRecipe
{
    // Repository admission policy pins datasets/rules-packs/mc08/UPSTREAM.md:
    // 15 signature, 17 Protection, 8 Basic, and five separate Invocations.
    internal static bool Complete(HeroSetup hero) =>
        hero.Hero.SequenceEqual(["09001a,09001b"])
        && hero.Obligations.SequenceEqual(["09027"])
        && hero.NemesisSet.SequenceEqual(["09028", "09029", "09030", "09030", "09031"])
        && hero.NemesisSetIcon == "doctor_strange_nemesis"
        && hero.AspectRule == DeckAspectRule.Standard
        && Quantities(hero.HeroDeck, 2, [1, 2, 2, 2, 1, 2, 1, 1, 2, 1])
        && Quantities(hero.PlayerDeck, 12, [1, 1, 1, 3, 3, 2, 2, 1, 3, 3, 1, 1, 1, 1, 1])
        && hero.InvocationDeck is not null
        && hero.InvocationDeck.Cards.SequenceEqual(["09032", "09033", "09034", "09035", "09036"]);

    private static bool Quantities(IReadOnlyList<string> actual, int first, int[] counts) =>
        actual.Order(StringComparer.Ordinal).SequenceEqual(counts.SelectMany((count, index) =>
            Enumerable.Repeat($"09{first + index:000}", count)));
}
