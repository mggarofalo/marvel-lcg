using System.Text.Json;
using Marvel.Protocol;

namespace Marvel.Client;

/// <summary>Checks that discovery can be represented by independent mode and component controls.</summary>
internal static class ScenarioChoiceContract
{
    internal static bool Complete(IReadOnlyList<ScenarioSetupChoice> choices)
    {
        if (choices.Any(choice => !Valid(choice))) return false;
        if (choices.Select(choice => choice.Key).Distinct(StringComparer.Ordinal).Count() != choices.Count) return false;
        return choices.GroupBy(choice => (choice.Name, choice.Mode)).All(CompleteMode);
    }

    private static bool Valid(ScenarioSetupChoice choice) => choice is not null
        && ValidIdentity(choice) && ValidModularSets(choice) && ValidVariants(choice.Variants);

    private static bool ValidIdentity(ScenarioSetupChoice choice) =>
        !string.IsNullOrWhiteSpace(choice.Key) && !string.IsNullOrWhiteSpace(choice.Name)
        && choice.Mode is "standard" or "expert" or "custom" or "extreme";

    private static bool ValidModularSets(ScenarioSetupChoice choice) => choice.RecommendedModularSets is not null
        && (choice.AllowsModularSets || choice.RecommendedModularSets.Count == 0);

    private static bool ValidVariants(IReadOnlyList<ScenarioVariantChoice>? variants) => variants is not null
        && variants.All(item => item is not null && !string.IsNullOrWhiteSpace(item.Name)
            && !string.IsNullOrWhiteSpace(item.Value))
        && variants.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == variants.Count;

    private static bool CompleteMode(IGrouping<(string Name, string Mode), ScenarioSetupChoice> family)
    {
        var choices = family.ToArray();
        if (family.Key.Mode != "custom") return choices.Length == 1;
        if (!ConsistentModularSets(choices)) return false;
        var names = choices[0].Variants.Select(item => item.Name).ToArray();
        if (names.Length == 0 || choices.Any(choice => !choice.Variants.Select(item => item.Name)
            .SequenceEqual(names, StringComparer.Ordinal))) return false;
        long combinations = 1;
        for (int index = 0; index < names.Length; index++)
        {
            combinations *= choices.Select(choice => choice.Variants[index].Value).Distinct(StringComparer.Ordinal).Count();
            if (combinations > choices.Length) return false;
        }
        return combinations == choices.Length && choices.Select(choice => JsonSerializer.Serialize(choice.Variants))
            .Distinct(StringComparer.Ordinal).Count() == choices.Length;
    }

    private static bool ConsistentModularSets(ScenarioSetupChoice[] choices) => choices.All(choice =>
        choice.AllowsModularSets == choices[0].AllowsModularSets
        && choice.RecommendedModularSets.SequenceEqual(choices[0].RecommendedModularSets, StringComparer.Ordinal));
}
