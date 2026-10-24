using System.Collections.Immutable;
using System.Text.Json;

namespace Marvel.Content.Setup;

/// <summary>Reads the optional independent-encounter recipe without inferring ownership from names.</summary>
internal static class EncounterRecipeReader
{
    internal static EncounterSetupRecipe? Read(JsonElement campaign)
    {
        if (!campaign.TryGetProperty("encounter_groups", out var recipe)) return null;
        RequireObject(recipe, ["groups", "active_group"]);
        var activeValue = recipe.GetProperty("active_group");
        if (activeValue.ValueKind != JsonValueKind.Number || !activeValue.TryGetInt32(out int active))
            throw new JsonException("active_group must be an integer");
        var groups = recipe.GetProperty("groups");
        if (groups.ValueKind != JsonValueKind.Array)
            throw new JsonException("encounter groups must be an array");
        var result = ImmutableArray.CreateBuilder<EncounterGroupRecipe>();
        foreach (var group in groups.EnumerateArray())
        {
            RequireObject(group, ["stages", "scheme", "encounters"]);
            result.Add(new(Strings(group.GetProperty("stages")),
                Text(group.GetProperty("scheme")), Strings(group.GetProperty("encounters"))));
        }
        if (result.Count < 2 || active < 0 || active >= result.Count)
            throw new JsonException("encounter groups require at least two groups and a valid active_group");
        return new(result.ToImmutable(), active);
    }

    private static void RequireObject(JsonElement value, string[] names)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new JsonException("encounter recipe must be an object");
        var actual = value.EnumerateObject().Select(property => property.Name).ToArray();
        if (actual.Length != names.Length || actual.Distinct(StringComparer.Ordinal).Count() != names.Length
            || actual.Any(name => !names.Contains(name, StringComparer.Ordinal)))
            throw new JsonException("encounter recipe has missing, duplicate or unknown fields");
    }

    private static ImmutableArray<string> Strings(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
            throw new JsonException("encounter recipe cards must be an array");
        return value.EnumerateArray().Select(Text).ToImmutableArray();
    }

    private static string Text(JsonElement value) => value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString()!
        : throw new JsonException("encounter recipe card must be a nonempty string");
}
