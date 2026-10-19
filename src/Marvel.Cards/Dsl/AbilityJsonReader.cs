using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

internal static class AbilityJsonReader
{
    internal static CardCounterPool StartingCounters(JsonElement element, string card)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new AbilityException(
                $"card '{card}' gives 'startingCounters' a non-object value");
        }
        Refuse(element, AbilityCatalog.StartingCounterKeys, $"starting counters on '{card}'");

        string type = Text(element, "type")
            ?? throw new AbilityException(
                $"starting counters on '{card}' have no string 'type'");
        if (type.Length == 0
            || type.Any(letter => !char.IsLower(letter) && letter != '-'))
        {
            throw new AbilityException(
                $"starting counters on '{card}' need a lower-case counter 'type'");
        }
        if (!element.TryGetProperty("count", out var countElement)
            || !countElement.TryGetInt32(out int count)
            || count <= 0)
        {
            throw new AbilityException(
                $"starting counters on '{card}' need a positive integer 'count'");
        }
        if (!element.TryGetProperty("uses", out _))
        {
            throw new AbilityException(
                $"starting counters on '{card}' need a boolean 'uses'");
        }

        return new CardCounterPool(type, count, Flag(element, "uses", card))
        { PerPlayer = Flag(element, "perPlayer", card) };
    }

    internal static JsonDocument Read(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException failure)
        {
            throw new AbilityException("the ability dataset is not JSON", failure);
        }
    }

    internal static CardAbility Ability(string card, string cardName, JsonElement element)
    {
        Refuse(element, AbilityCatalog.AbilityKeys, $"an ability on '{card}'");
        var trigger = AbilityTriggerReader.Read(element, card);
        var effect = Effect(element, card);
        string printedResources = PrintedResources(element, card, trigger.Timing, effect);

        return new CardAbility(
            card,
            Text(element, "name") ?? cardName,
            trigger,
            effect,
            element.TryGetProperty("cost", out var cost) ? Node(cost, card) : null,
            AbilityLimit.ReadCount(element),
            element.TryGetProperty("when", out var condition) ? Node(condition, card) : null,
            Flag(element, "anyPlayer", card),
            Labels(element, card),
            printedResources,
            Maximum(element, card))
        {
            LimitPeriod = element.TryGetProperty("limitPerPhase", out _) ? AbilityLimitPeriod.Phase : AbilityLimitPeriod.Round,
            ResourceRestriction = AbilityResourceRestriction.Read(element, trigger.Timing),
        };
    }

    private static AbilityNode Effect(JsonElement element, string card)
    {
        if (!element.TryGetProperty("effect", out var effect))
        {
            throw new AbilityException($"an ability on '{card}' has no 'effect'");
        }
        return Node(effect, card);
    }

    private static string PrintedResources(
        JsonElement element, string card, AbilityType type, AbilityNode effect)
    {
        string printedResources = Text(element, "printedResources") ?? string.Empty;
        if (printedResources.Any(resource => !Resources.Types.Contains(resource)))
        {
            throw new AbilityException(
                $"'{card}' declares an unknown printed text-box resource");
        }
        if (printedResources.Length > 0
            && (type != AbilityType.Resource
                || effect.Kind != "generate"
                || effect.Argument is not AbilityValue.Word generated
                || !string.Equals(
                    generated.Value, printedResources, StringComparison.Ordinal)))
        {
            throw new AbilityException(
                $"'{card}' may declare printed text-box resources only on a matching "
                + "fixed resource ability");
        }
        return printedResources;
    }

    internal static AbilityMaximum? Maximum(JsonElement ability, string card)
    {
        var written = new[]
        {
            (Key: "maxPerRound", Period: MaximumPeriod.Round),
            (Key: "maxPerPhase", Period: MaximumPeriod.Phase),
            (Key: "maxPerAttack", Period: MaximumPeriod.Attack),
            (Key: "maxPerActivation", Period: MaximumPeriod.Activation),
            (Key: "maxPerGame", Period: MaximumPeriod.Game),
            (Key: "maxPerInstance", Period: MaximumPeriod.Instance),
        }
            .Where(candidate => ability.TryGetProperty(candidate.Key, out _))
            .ToList();
        if (written.Count > 1)
        {
            throw new AbilityException($"'{card}' gives one ability several maxima");
        }
        if (written.Count == 0)
        {
            return null;
        }

        var maximum = written[0];
        long amount = ability.GetProperty(maximum.Key).GetInt64();
        if (amount <= 0)
        {
            throw new AbilityException($"'{card}' gives an ability a non-positive maximum");
        }
        return new AbilityMaximum(amount, maximum.Period);
    }

    internal static List<string> Labels(JsonElement ability, string card)
    {
        if (!ability.TryGetProperty("labels", out var labels))
        {
            return [];
        }
        if (labels.ValueKind != JsonValueKind.Array)
        {
            throw new AbilityException($"'{card}' gives 'labels' a non-array value");
        }

        var parsed = new List<string>();
        foreach (var label in labels.EnumerateArray())
        {
            if (label.ValueKind != JsonValueKind.String)
            {
                throw new AbilityException($"'{card}' gives 'labels' a non-string value");
            }
            string normalized = label.GetString()!.ToLowerInvariant() switch
            {
                "attack" => BasicPowers.AttackVerb,
                "defense" => Attack.DefenseVerb,
                "thwart" => BasicPowers.ThwartVerb,
                var unknown => throw new AbilityException(
                    $"'{card}' has unknown labeled-ability type '{unknown}'"),
            };
            if (!parsed.Contains(normalized, StringComparer.Ordinal))
            {
                parsed.Add(normalized);
            }
        }
        if (parsed.Count == 0)
        {
            throw new AbilityException($"'{card}' gives 'labels' an empty array");
        }
        return parsed;
    }

    /// <summary>One node: a value with exactly one named operation in it.</summary>
    internal static AbilityNode Node(JsonElement element, string card)
    {
        try
        {
            return AbilityNode.Of(Value(element, card));
        }
        catch (AbilityException failure)
        {
            throw new AbilityException($"'{card}': {failure.Message}", failure);
        }
    }

    /// <summary>
    /// One value, structurally. Nothing here knows the vocabulary.
    /// </summary>
    /// <remarks>
    /// An object stays an object. Whether a given object is a node or a map of
    /// fields is a question only the interpreter can answer — see
    /// <see cref="AbilityValue.Map"/>.
    /// </remarks>
    internal static AbilityValue Value(JsonElement element, string card) => element.ValueKind switch
    {
        JsonValueKind.Number => new AbilityValue.Number(element.GetInt64()),
        JsonValueKind.String => new AbilityValue.Word(element.GetString()!),
        JsonValueKind.Array => new AbilityValue.List(
            [.. element.EnumerateArray().Select(item => Value(item, card))]),
        JsonValueKind.Object => new AbilityValue.Map(
            element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => Value(property.Value, card),
                StringComparer.Ordinal)),
        _ => throw new AbilityException(
            $"'{card}' has a {element.ValueKind} where a value was expected"),
    };

    internal static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    internal static bool Flag(JsonElement element, string name, string card)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return false;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        throw new AbilityException($"'{card}' gives '{name}' a non-boolean value");
    }

    internal static void Refuse(JsonElement element, IReadOnlySet<string> known, string what)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!known.Contains(property.Name))
            {
                throw new AbilityException(
                    $"{what} carries '{property.Name}', which nothing reads. Known: "
                    + string.Join(", ", known.Order(StringComparer.Ordinal)));
            }
        }
    }
}
