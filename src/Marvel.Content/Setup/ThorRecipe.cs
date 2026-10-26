using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Pins the playable composition on the published Thor divider.</summary>
internal static class ThorRecipe
{
    // Repository admission policy pins the FFG divider transcription in
    // datasets/rules-packs/mc06/UPSTREAM.md, including the 40-card starter.
    // Numbered nemesis components resolve the divider: two Frost Giants, one Trickster.
    internal static bool Complete(HeroSetup hero) =>
        hero.Hero.SequenceEqual(["06001a,06001b"])
        && hero.Obligations.SequenceEqual(["06026"])
        && hero.NemesisSet.SequenceEqual(["06027", "06028", "06029", "06029", "06030"])
        && hero.NemesisSetIcon == "thor_nemesis"
        && hero.AspectRule == DeckAspectRule.Standard
        && Quantities(hero.HeroDeck, 2, [1, 3, 1, 3, 2, 1, 2, 1, 1])
        && Quantities(hero.PlayerDeck, 11, [1, 1, 2, 3, 3, 2, 1, 3, 1, 1, 3, 1, 1, 1, 1]);

    private static bool Quantities(IReadOnlyList<string> actual, int first, int[] counts) =>
        actual.Order(StringComparer.Ordinal).SequenceEqual(counts.SelectMany((count, index) =>
            Enumerable.Repeat($"06{first + index:000}", count)));
}
