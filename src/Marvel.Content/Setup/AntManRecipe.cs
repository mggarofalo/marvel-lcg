using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Pins playable composition to the published Ant-Man starter.</summary>
internal static class AntManRecipe
{
    // Repository admission policy pins the independently reviewed printed starter:
    // 15 signature, 16 Leadership and 9 Basic; obligation and nemesis are separate.
    internal static bool Complete(HeroSetup hero) =>
        hero.Hero.SequenceEqual(["12001a,12001b,12001c"])
        && hero.Obligations.SequenceEqual(["12025"])
        && hero.NemesisSet.SequenceEqual(["12026", "12027", "12028", "12028", "12029"])
        && hero.NemesisSetIcon == "ant_nemesis"
        && hero.AspectRule == DeckAspectRule.Standard
        && Quantities(hero.HeroDeck, 2, [1, 2, 1, 2, 2, 3, 1, 2, 1])
        && Quantities(hero.PlayerDeck, 11, [1, 1, 1, 1, 3, 3, 3, 3, 2, 1, 1, 1, 1, 3])
        && hero.InvocationDeck is null;

    private static bool Quantities(IReadOnlyList<string> actual, int first, int[] counts) =>
        actual.Order(StringComparer.Ordinal).SequenceEqual(counts.SelectMany((count, index) =>
            Enumerable.Repeat($"12{first + index:000}", count)));
}
