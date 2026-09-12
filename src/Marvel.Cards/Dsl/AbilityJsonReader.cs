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

        return new CardCounterPool(type, count, Flag(element, "uses", card));
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
        var trigger = Trigger(element, card);
        var effect = Effect(element, card);
        string printedResources = PrintedResources(element, card, trigger.Timing, effect);

        return new CardAbility(
            card,
            Text(element, "name") ?? cardName,
            trigger,
            effect,
            element.TryGetProperty("cost", out var cost) ? Node(cost, card) : null,
            element.TryGetProperty("limitPerRound", out var limit) ? limit.GetInt64() : null,
            element.TryGetProperty("when", out var condition) ? Node(condition, card) : null,
            Flag(element, "anyPlayer", card),
            Labels(element, card),
            printedResources,
            Maximum(element, card));
    }

    private static AbilityTrigger Trigger(JsonElement element, string card)
    {
        if (!element.TryGetProperty("trigger", out var trigger))
        {
            throw new AbilityException($"an ability on '{card}' has no 'trigger'");
        }

        Refuse(trigger, AbilityCatalog.TriggerKeys, $"a trigger on '{card}'");

        string? actor = Role(trigger, "actor", card);
        string? target = Role(trigger, "target", card);
        string? subject = TriggerSubject(trigger, actor, target, card);
        AbilityType type = TriggerType(trigger, card);

        return new AbilityTrigger(
            Event(trigger, card, type), type, subject, actor, target,
            Form(trigger, card), Also(trigger, card), Whose(trigger, card));
    }

    private static string? TriggerSubject(
        JsonElement trigger, string? actor, string? target, string card)
    {
        string? subject = Text(trigger, "subject")
            ?? (actor is null && target is null ? AbilitySubjects.This : null);
        if (subject is not null && !AbilitySubjects.All.Contains(subject))
        {
            throw new AbilityException(
                $"'{card}' triggers on subject '{subject}', which is not one of "
                + string.Join(", ", AbilitySubjects.All.Order(StringComparer.Ordinal)));
        }
        return subject;
    }

    private static AbilityType TriggerType(JsonElement trigger, string card)
    {
        string timing = Text(trigger, "timing")
            ?? throw new AbilityException($"a trigger on '{card}' has no 'timing'");
        if (!Enum.TryParse(timing, ignoreCase: false, out AbilityType type))
        {
            throw new AbilityException(
                $"'{card}' has timing '{timing}', which is not an ability type");
        }
        return type;
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

    /// <summary>One explicit occurrence role matcher, or null.</summary>
    internal static string? Role(JsonElement trigger, string name, string card)
    {
        string? role = Text(trigger, name);
        if (role is null || AbilityRoles.All.Contains(role))
        {
            return role;
        }

        throw new AbilityException(
            $"'{card}' matches {name} '{role}', which is not one of "
            + string.Join(", ", AbilityRoles.All.Order(StringComparer.Ordinal)));
    }

    /// <summary>
    /// The triggering condition, which two ability types do not have.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:ability.5</c>: "An ability prefaced by a bold timing trigger
    /// followed by a colon is referred to as a triggered ability. An ability
    /// without a bold timing trigger is referred to as a constant ability."
    /// A constant is the half that is not, so there is no occurrence to name.
    /// </para>
    /// <para>
    /// A "Setup" ability is the other, and for a different reason: it <i>is</i>
    /// a triggered ability, but <c>rr:setup-triggered-ability.2</c> times it to
    /// a step of setup rather than to something happening in the game. Setup is
    /// not on the agenda, so no condition in <c>Steps.EveryCondition</c> names
    /// it — and inventing one would put a triggering condition nothing produces
    /// into the data, which is the failure the whole dataset is held against
    /// the engine to avoid.
    /// </para>
    /// <para>
    /// <b>Refused rather than ignored</b>, for the reason the class remarks
    /// give. An <c>event</c> on one of these is not a harmless extra key — it
    /// is the author having believed the card triggers on something, and the
    /// whole point of reading it back is to say so.
    /// </para>
    /// </remarks>
    internal static string? Event(JsonElement trigger, string card, AbilityType type)
    {
        string? when = Text(trigger, "event");

        if (type is AbilityType.Constant or AbilityType.Setup)
        {
            return when is null
                ? null
                : throw new AbilityException(
                    $"'{card}' is '{type}' and triggers on '{when}'. That ability type is "
                    + "not timed to an occurrence and so names no triggering condition");
        }

        return when ?? throw new AbilityException($"a trigger on '{card}' has no 'event'");
    }

    /// <summary>
    /// The form a triggered ability requires, or null —
    /// <c>rr:player-turn.5.1</c>.
    /// </summary>
    /// <remarks>
    /// "If the action ability is preceded by <b>Hero</b> or <b>Alter-Ego</b>,
    /// the player must be in the specified form in order to trigger the
    /// ability." A closed set, because it is a form and not a predicate: the
    /// only two the parenthesis ever names are the two an identity has.
    /// </remarks>
    internal static string? Form(JsonElement trigger, string card)
    {
        string? form = Text(trigger, "form");
        if (form is null || form == Forms.Hero || form == Forms.AlterEgo)
        {
            return form;
        }

        throw new AbilityException(
            $"'{card}' requires the form '{form}', and an ability may require "
            + $"'{Forms.Hero}' or '{Forms.AlterEgo}'");
    }

    /// <summary>
    /// A second triggering condition of the same occurrence, or null.
    /// </summary>
    /// <remarks>
    /// Held against <c>Steps.EveryCondition</c> exactly as
    /// a trigger's event is, and for the same reason: the vocabulary is the
    /// engine's spelling of a triggering condition rather than a DSL word, so a
    /// name nothing produces is a card that would never fire and this is where
    /// that is caught.
    /// </remarks>
    internal static string? Also(JsonElement trigger, string card)
    {
        string? also = Text(trigger, "alsoHappened");
        if (also is null || Steps.EveryCondition.Contains(also))
        {
            return also;
        }

        throw new AbilityException(
            $"'{card}' asks whether '{also}' also happened, and no step in this engine "
            + "produces that triggering condition");
    }

    /// <summary>
    /// Whose opportunity a triggered ability is, or null for its controller's.
    /// </summary>
    /// <remarks>
    /// A closed set of one, and closed for the reason
    /// <see cref="AbilitySubjects"/> is: "the seat this happened to" is a
    /// relation between a card and an occurrence, and naming the relations is
    /// what stops the field becoming a general predicate. The word is the one
    /// the effect tree already binds a player with, so a card reads the same
    /// phrase the same way in both halves.
    /// </remarks>
    internal static string? Whose(JsonElement trigger, string card)
    {
        string? whose = Text(trigger, "player");
        if (whose is null
            || whose == AbilityPlayers.TriggerPlayer
            || whose == AbilityPlayers.You)
        {
            return whose;
        }

        throw new AbilityException(
            $"'{card}' offers its ability to '{whose}', and an ability may be offered to "
            + $"'{AbilityPlayers.TriggerPlayer}', '{AbilityPlayers.You}', "
            + "or to whoever controls its card");
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
