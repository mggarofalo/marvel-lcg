using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

/// <summary>Reads the frozen prompt shape written by schema 2 journals.</summary>
public static class SchemaTwoPromptJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    /// <summary>
    /// Parses every schema 2 member and verifies that its computed aliases still
    /// agree with the independent values before producing a canonical record.
    /// </summary>
    public static PromptRecord Read(JsonElement element)
    {
        var legacy = element.Deserialize<LegacyPromptRecord>(Options)
            ?? throw new JsonException("schema 2 prompt is null");
        if (legacy.Affordances is null)
        {
            throw new JsonException("schema 2 prompt affordances are null");
        }

        return new PromptRecord(
            legacy.Player,
            legacy.Asking,
            legacy.When,
            legacy.Trigger,
            legacy.Label,
            legacy.Cancellable,
            [.. legacy.Affordances.Select(affordance =>
                Convert(affordance
                    ?? throw new JsonException("schema 2 affordance is null")))]);
    }

    private static AffordanceRecord Convert(LegacyAffordanceRecord affordance)
    {
        if (affordance.Costs is null)
        {
            throw new JsonException("schema 2 affordance costs are null");
        }

        return new(
            affordance.Verb,
            affordance.AnchorId,
            affordance.AnchorPlayer,
            affordance.Label,
            affordance.Targets is null ? null : Convert(affordance.Targets),
            [.. affordance.Costs.Select(cost =>
                Convert(cost ?? throw new JsonException("schema 2 cost is null")))],
            affordance.Illegal);
    }

    private static TargetRequestRecord Convert(LegacyTargetRequest request)
    {
        bool grouped = request.Groups is { Count: > 0 };
        if (request.IsGrouped != grouped)
        {
            throw new JsonException("schema 2 target is_grouped does not match groups");
        }

        bool allowRepeatedRecorded = Recorded(request.AllowRepeated);
        bool maximumOccurrencesRecorded = Recorded(request.MaximumOccurrences);
        bool detailsRecorded = Recorded(request.Details);
        return new TargetRequestRecord(
            request.Legal,
            request.Min,
            request.Max,
            request.Groups,
            request.MustIncludeTraits,
            request.Rule,
            request.IsSearch,
            allowRepeatedRecorded ? request.AllowRepeated.GetBoolean() : false,
            ReadOptional<IReadOnlyDictionary<int, int>>(request.MaximumOccurrences),
            ReadOptional<IReadOnlyDictionary<int, string>>(request.Details))
        {
            LegacyAllowRepeatedRecorded = allowRepeatedRecorded,
            LegacyMaximumOccurrencesRecorded = maximumOccurrencesRecorded,
            LegacyDetailsRecorded = detailsRecorded,
        };
    }

    private static CostOptionRecord Convert(LegacyCostOption option)
    {
        if (option.OrCost is null
            || option.Generators is null
            || option.VariableRequests is null
            || option.ResourceCosts is null)
        {
            throw new JsonException("schema 2 cost has a null computed alias");
        }

        IReadOnlyList<LegacyResourceSource> generators = option.Sources ?? [];
        IReadOnlyList<LegacyVariableRequest> variables = option.Variables ?? [];
        IReadOnlyList<LegacyResourceCost> resourceCosts = option.Components
            ?? [new LegacyResourceCost(option.Cost, option.Rule, Printed: false)];
        if (!AliasesMatch(option, generators, variables, resourceCosts))
        {
            throw new JsonException("schema 2 cost computed aliases do not match source fields");
        }

        bool declarationSensitiveRecorded = Recorded(option.DeclarationSensitive);
        return new CostOptionRecord(
            option.Target,
            option.Cost,
            option.Rule,
            option.OrCost,
            option.OrRule,
            ConvertSources(option.Sources),
            ConvertVariables(option.Variables),
            ConvertComponents(option.Components),
            declarationSensitiveRecorded ? option.DeclarationSensitive.GetBoolean() : false)
        {
            LegacyDeclarationSensitiveRecorded = declarationSensitiveRecorded,
        };
    }

    private static bool AliasesMatch(
        LegacyCostOption option,
        IReadOnlyList<LegacyResourceSource> generators,
        IReadOnlyList<LegacyVariableRequest> variables,
        IReadOnlyList<LegacyResourceCost> resourceCosts) =>
        option.HasAlternative == (option.OrCost!.Length > 0)
        && SequenceEqual(option.Generators!, generators)
        && SequenceEqual(option.VariableRequests!, variables)
        && ResourceCostsEqual(option.ResourceCosts!, resourceCosts);

    private static ResourceSourceRecord[]? ConvertSources(
        IReadOnlyList<LegacyResourceSource>? sources) =>
        sources is null
            ? null
            : [.. sources.Select(source =>
                new ResourceSourceRecord(source.Effect, source.Generates))];

    private static VariableRequestRecord[]? ConvertVariables(
        IReadOnlyList<LegacyVariableRequest>? variables) =>
        variables is null
            ? null
            : [.. variables.Select(variable =>
                new VariableRequestRecord(variable.Name, variable.Min, variable.Max))];

    private static ResourceCostComponentRecord[]? ConvertComponents(
        IReadOnlyList<LegacyResourceCost>? components) =>
        components is null
            ? null
            : [.. components.Select(component => new ResourceCostComponentRecord(
                component.Cost, component.Rule, component.Printed))];

    private static bool SequenceEqual<T>(
        IReadOnlyList<T> left, IReadOnlyList<T> right) where T : notnull =>
        left.Count == right.Count && left.SequenceEqual(right);

    private static bool ResourceCostsEqual(
        IReadOnlyList<LegacyResourceCost> left,
        IReadOnlyList<LegacyResourceCost> right) =>
        left.Count == right.Count
        && left.Zip(right).All(pair =>
            pair.First is not null
            && pair.Second is not null
            && string.Equals(pair.First.Cost, pair.Second.Cost, StringComparison.Ordinal)
            && pair.First.Printed == pair.Second.Printed
            && (pair.First.Rule is null
                ? pair.Second.Rule is null
                : pair.Second.Rule is not null
                    && pair.First.Rule.SequenceEqual(pair.Second.Rule)));

    private static bool Recorded(JsonElement value) =>
        value.ValueKind != JsonValueKind.Undefined;

    private static T? ReadOptional<T>(JsonElement value) where T : class =>
        !Recorded(value) || value.ValueKind == JsonValueKind.Null
            ? null
            : value.Deserialize<T>(Options);

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    private sealed record LegacyPromptRecord(
        [property: JsonRequired] int Player,
        [property: JsonRequired] string Asking,
        [property: JsonRequired] string When,
        [property: JsonRequired] string Trigger,
        [property: JsonRequired] string Label,
        [property: JsonRequired] bool Cancellable,
        [property: JsonRequired] IReadOnlyList<LegacyAffordanceRecord> Affordances);

    private sealed record LegacyAffordanceRecord(
        [property: JsonRequired] string Verb,
        [property: JsonRequired] int AnchorId,
        [property: JsonRequired] int AnchorPlayer,
        [property: JsonRequired] string Label,
        [property: JsonRequired] LegacyTargetRequest? Targets,
        [property: JsonRequired] IReadOnlyList<LegacyCostOption> Costs,
        [property: JsonRequired] string? Illegal);

    private sealed record LegacyTargetRequest(
        [property: JsonRequired] IReadOnlyList<int> Legal,
        [property: JsonRequired] int Min,
        [property: JsonRequired] int Max,
        [property: JsonRequired] IReadOnlyList<IReadOnlyList<int>>? Groups,
        [property: JsonRequired] IReadOnlyList<string>? MustIncludeTraits,
        [property: JsonRequired] string Rule,
        [property: JsonRequired] bool IsSearch,
        [property: JsonRequired] bool IsGrouped,
        JsonElement AllowRepeated = default,
        JsonElement MaximumOccurrences = default,
        JsonElement Details = default);

    private sealed record LegacyCostOption(
        [property: JsonRequired] int Target,
        [property: JsonRequired] string Cost,
        [property: JsonRequired] IReadOnlyList<string>? Rule,
        [property: JsonRequired] string OrCost,
        [property: JsonRequired] IReadOnlyList<string>? OrRule,
        [property: JsonRequired] IReadOnlyList<LegacyResourceSource>? Sources,
        [property: JsonRequired] IReadOnlyList<LegacyVariableRequest>? Variables,
        [property: JsonRequired] IReadOnlyList<LegacyResourceCost>? Components,
        [property: JsonRequired] bool HasAlternative,
        [property: JsonRequired] IReadOnlyList<LegacyResourceSource> Generators,
        [property: JsonRequired] IReadOnlyList<LegacyVariableRequest> VariableRequests,
        [property: JsonRequired] IReadOnlyList<LegacyResourceCost> ResourceCosts,
        JsonElement DeclarationSensitive = default);

    private readonly record struct LegacyResourceSource(
        [property: JsonRequired] int Effect,
        [property: JsonRequired] string Generates);

    private readonly record struct LegacyVariableRequest(
        [property: JsonRequired] string Name,
        [property: JsonRequired] long Min,
        [property: JsonRequired] long Max);

    private sealed record LegacyResourceCost(
        [property: JsonRequired] string Cost,
        [property: JsonRequired] IReadOnlyList<string>? Rule,
        [property: JsonRequired] bool Printed);
}
