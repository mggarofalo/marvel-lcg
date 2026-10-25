using System.Collections.Immutable;
using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Pinned playable composition of the reviewed standalone products.</summary>
internal static class StandaloneProductRecipes
{
    // Repository admission policy pins the FFG divider transcription in
    // datasets/rules-packs/mc04/UPSTREAM.md, including its published starter.
    internal static bool CaptainAmerica(HeroSetup hero) =>
        hero.Hero.SequenceEqual(["03001a,03001b"])
        && hero.Obligations.SequenceEqual(["03026"])
        && hero.NemesisSet.SequenceEqual(["03027", "03028", "03029", "03029", "03030"])
        && hero.NemesisSetIcon == "captain_america_nemesis"
        && hero.AspectRule == DeckAspectRule.Standard
        && Quantities(hero.HeroDeck, 2, [1, 2, 3, 2, 2, 1, 1, 1, 2])
        && Quantities(hero.PlayerDeck, 11, [1, 1, 1, 1, 3, 2, 3, 2, 3, 1, 1, 1, 1, 1, 3]);

    private static bool Quantities(IReadOnlyList<string> actual, int first, int[] counts) =>
        actual.Order(StringComparer.Ordinal).SequenceEqual(counts.SelectMany((count, index) =>
            Enumerable.Repeat($"03{first + index:000}", count)));

    // The mc03 additional-setup and adjustable-difficulty instructions define four
    // separate decks and A/B stages. These pinned compositions also have independent
    // canonical deal tests; generic legal allocation alone does not admit a product.
    private static readonly ImmutableArray<ImmutableArray<string>> Stages =
        [["07002", "07003"], ["07017", "07018"], ["07032", "07033"], ["07046", "07047"]];
    private static readonly ImmutableArray<string> Schemes = ["07004", "07019", "07034", "07048"];
    private static readonly ImmutableArray<ImmutableArray<string>> Decks =
    [
        ["07005", "07006", "07007", "07008", "07009", "07010", "07011", "07012", "07012", "07013", "07014", "07015", "07015", "07016", "07016"],
        ["07020", "07021", "07022", "07023", "07024", "07025", "07026", "07027", "07027", "07028", "07029", "07030", "07030", "07031", "07031"],
        ["07035", "07035", "07036", "07037", "07038", "07039", "07040", "07041", "07042", "07042", "07043", "07043", "07044", "07044", "07045"],
        ["07049", "07050", "07051", "07051", "07052", "07053", "07054", "07055", "07055", "07056", "07057", "07058", "07058", "07059", "07059"],
    ];

    internal static bool WreckingCrew(string key, CampaignSetup recipe)
    {
        string mode = key["wrecking_crew_".Length..];
        string variants = mode == "expert" ? "bbbb" : mode.StartsWith("custom_", StringComparison.Ordinal) ? mode[^4..] : "aaaa";
        string[][] stages = Enumerable.Range(0, 4).Select(index => mode == "extreme"
            ? Stages[index].ToArray() : new[] { Stages[index][variants[index] == 'b' ? 1 : 0] }).ToArray();
        return Header(recipe, mode)
            && recipe.Villain.SequenceEqual(stages.SelectMany(group => group))
            && recipe.EncounterGroups is { ActiveGroup: 0, Groups.Count: 4 } groups
            && Enumerable.Range(0, 4).All(index => Group(groups.Groups[index], index, stages[index]));
    }

    private static bool Header(CampaignSetup recipe, string mode) =>
        recipe.SetupMode == (mode.StartsWith("custom_", StringComparison.Ordinal) ? "custom" : mode)
        && recipe.Expert == (mode is "expert" or "extreme")
        && recipe.Schemes.SequenceEqual(["07001a,07001b"])
        && recipe.Challenges.Count == 0 && recipe.SetAside.Count == 0
        && recipe.Encounters.Count == 0 && recipe.EncounterSets.Count == 0 && recipe.ModularSets.Count == 0;

    private static bool Group(EncounterGroupRecipe group, int index, string[] stages) =>
        group.Stages.SequenceEqual(stages)
        && group.Scheme == Schemes[index]
        && group.Encounters.Order(StringComparer.Ordinal).SequenceEqual(Decks[index])
        && group.Variant == (stages.Length == 2 ? "A → B" : stages[0] == Stages[index][0] ? "A" : "B");
}
