using System.Text.Json;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>
/// Reads authored card abilities out of their canonical JSON.
/// </summary>
/// <remarks>
/// <para>
/// Takes a string, never a path: <c>Marvel.Cards</c> does no file or network
/// I/O (<c>docs/presentation-layer.md</c>'s dependency rules), and where the
/// bytes come from is a packaging decision nobody has made yet.
/// </para>
/// <para>
/// <b>Strict, and deliberately so.</b> Every unknown key is refused rather than
/// ignored. A card is data a player may one day author or download, and the
/// failure mode of a lenient reader is a card that looks accepted and does
/// three quarters of what it says — which nothing downstream can detect.
/// </para>
/// </remarks>
public static class AbilityCatalog
{
    // `note` carries the reasoning that JSON has nowhere else to put: why a card
    // was read the way it was, and what its data deliberately does not say.
    // Nothing reads it, and that is the point -- it is for the next person.
    private static readonly HashSet<string> CardKeys =
        new(StringComparer.Ordinal) { "card", "name", "note", "abilities" };

    private static readonly HashSet<string> AbilityKeys =
        new(StringComparer.Ordinal) { "name", "note", "trigger", "effect", "cost" };

    private static readonly HashSet<string> TriggerKeys =
        new(StringComparer.Ordinal) { "event", "timing", "subject", "form" };

    /// <summary>Parses the canonical ability dataset.</summary>
    /// <param name="json">The dataset text.</param>
    /// <exception cref="AbilityException">The text is not an ability dataset.</exception>
    public static AbilityBook Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var document = Read(json);
        if (!document.RootElement.TryGetProperty("cards", out var cards)
            || cards.ValueKind != JsonValueKind.Array)
        {
            throw new AbilityException("the ability dataset has no 'cards' array");
        }

        var abilities = new List<CardAbility>();
        var authored = new HashSet<string>(StringComparer.Ordinal);

        foreach (var element in cards.EnumerateArray())
        {
            Refuse(element, CardKeys, "a card");
            string card = Text(element, "card") ?? throw new AbilityException("a card has no 'card'");
            string name = Text(element, "name") ?? card;

            if (!authored.Add(card))
            {
                // Two entries for one card would make which one wins depend on
                // file order, and the loser would vanish silently.
                throw new AbilityException($"card '{card}' is authored twice");
            }

            if (!element.TryGetProperty("abilities", out var list))
            {
                continue;
            }

            foreach (var ability in list.EnumerateArray())
            {
                abilities.Add(Ability(card, name, ability));
            }
        }

        return new AbilityBook(abilities, authored);
    }

    private static JsonDocument Read(string json)
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

    private static CardAbility Ability(string card, string cardName, JsonElement element)
    {
        Refuse(element, AbilityKeys, $"an ability on '{card}'");

        if (!element.TryGetProperty("trigger", out var trigger))
        {
            throw new AbilityException($"an ability on '{card}' has no 'trigger'");
        }

        Refuse(trigger, TriggerKeys, $"a trigger on '{card}'");

        string when = Text(trigger, "event")
            ?? throw new AbilityException($"a trigger on '{card}' has no 'event'");
        string subject = Text(trigger, "subject") ?? AbilitySubjects.This;

        if (!AbilitySubjects.All.Contains(subject))
        {
            throw new AbilityException(
                $"'{card}' triggers on subject '{subject}', which is not one of "
                + string.Join(", ", AbilitySubjects.All.Order(StringComparer.Ordinal)));
        }

        string timing = Text(trigger, "timing")
            ?? throw new AbilityException($"a trigger on '{card}' has no 'timing'");
        if (!Enum.TryParse(timing, ignoreCase: false, out AbilityType type))
        {
            throw new AbilityException(
                $"'{card}' has timing '{timing}', which is not an ability type");
        }

        if (!element.TryGetProperty("effect", out var effect))
        {
            throw new AbilityException($"an ability on '{card}' has no 'effect'");
        }

        return new CardAbility(
            card,
            Text(element, "name") ?? cardName,
            new AbilityTrigger(when, type, subject, Form(trigger, card)),
            Node(effect, card),
            element.TryGetProperty("cost", out var cost) ? Node(cost, card) : null);
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
    private static string? Form(JsonElement trigger, string card)
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

    /// <summary>One node: a value with exactly one named operation in it.</summary>
    private static AbilityNode Node(JsonElement element, string card)
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
    private static AbilityValue Value(JsonElement element, string card) => element.ValueKind switch
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

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static void Refuse(JsonElement element, HashSet<string> known, string what)
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
