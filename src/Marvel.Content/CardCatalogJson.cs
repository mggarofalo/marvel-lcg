using System.Text.Json;
using Marvel.Rules.State;

namespace Marvel.Content;

/// <summary>Decodes and normalizes the canonical card dataset.</summary>
public static class CardCatalogJson
{

    internal static CardCatalogEntry ReadEntry(JsonElement element)
    {
        var traits = new List<string>();
        var printedTraitLabels = new List<string>();
        ReadTraits(element, traits, printedTraitLabels);
        Dictionary<string, string> attributes = ReadAttributes(element);
        CardKind kind = ReadKind(element);
        string set = ReadString(element, "set");
        IReadOnlyList<string> linkedTo = ReadStrings(element, "linked_to");

        // `rr:identity.2` makes a title name one card, and `rr:villain-defeat.3`
        // turns on whether two stages share one. Rhino's three do, which is the
        // case that rule is written for.
        string title = ReadString(element, "name");

        // `rr:team-up.2` matches "either its title or subtitle", so the small
        // line under the name is printed data the rules turn on too.
        string subtitle = ReadString(element, "subname");

        // `rr:boost-boost-icon.2`'s "Boost" abilities are indicated by a star
        // icon, and the star is in the text box and nowhere else -- the printed
        // `Boost` attribute counts icons and `.1` says a star is not one.
        string printed = ReadString(element, "text_plain");
        string formatted = ReadString(element, "text", printed);

        return new CardCatalogEntry(
            kind, set, linkedTo, traits, printedTraitLabels, attributes, title, subtitle, printed,
            formatted, KeywordsOf(attributes),
            CounterTypesOf(printed, attributes), CounterMaximumsOf(printed));
    }

    private static void ReadTraits(
        JsonElement element, List<string> traits, List<string> labels)
    {
        if (!element.TryGetProperty("traits", out JsonElement values)
            || values.ValueKind != JsonValueKind.Array)
        {
            return;
        }
        foreach (JsonElement value in values.EnumerateArray())
        {
            if (value.GetString() is { Length: > 0 } text)
            {
                labels.Add(text);
                traits.Add(CardCatalog.TraitKey(text));
            }
        }
    }

    private static Dictionary<string, string> ReadAttributes(JsonElement element)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!element.TryGetProperty("attributes", out JsonElement values)
            || values.ValueKind != JsonValueKind.Object)
        {
            return result;
        }
        foreach (JsonProperty attribute in values.EnumerateObject())
        {
            result[attribute.Name] = attribute.Value.GetString() ?? string.Empty;
        }
        return result;
    }

    private static CardKind ReadKind(JsonElement element) =>
        element.TryGetProperty("type", out JsonElement value)
        && value.GetString() is string name
            ? ToKind(name)
            : CardKind.Unknown;

    private static string ReadString(
        JsonElement element, string name, string fallback = "") =>
        element.TryGetProperty(name, out JsonElement value)
            ? value.GetString() ?? fallback
            : fallback;

    private static IReadOnlyList<string> ReadStrings(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value)
        && value.ValueKind == JsonValueKind.Array
            ? [.. value.EnumerateArray().Select(item => item.GetString()!)]
            : [];

    internal static List<string> KeywordsOf(Dictionary<string, string> attributes)
    {
        // The generated dataset records these printed keywords as structured
        // attributes. Keeping the vocabulary here prevents a client from
        // guessing rules meaning by scraping prose from the text box.
        (string Attribute, string Label, bool ShowsValue)[] names =
        [
            ("Acceleration", "Acceleration", false),
            ("Alliance", "Alliance", false),
            ("Assault", "Assault", false),
            ("Crisis", "Crisis", false),
            ("Guard", "Guard", false),
            ("Hazard", "Hazard", false),
            ("Hinder", "Hinder", true),
            ("Incite", "Incite", true),
            ("Patrol", "Patrol", false),
            ("Peril", "Peril", false),
            ("Permanent", "Permanent", false),
            ("Quickstrike", "Quickstrike", false),
            ("Restricted", "Restricted", false),
            ("Retaliate", "Retaliate", true),
            ("Stalwart", "Stalwart", false),
            ("Steady", "Steady", false),
            ("Surge", "Surge", false),
            ("TeamUp", "Team-Up", false),
            ("Teamwork", "Teamwork", false),
            ("Toughness", "Toughness", false),
            ("Victory", "Victory", true),
            ("Villainous", "Villainous", false),
            ("Vulnerable", "Vulnerable", false),
        ];
        return
        [
            .. names
                .Where(keyword => attributes.TryGetValue(
                    keyword.Attribute, out string? value)
                    && value.Length > 0 && value != "0")
                .Select(keyword => keyword.ShowsValue
                    ? $"{keyword.Label} {attributes[keyword.Attribute]}"
                    : keyword.Label),
            .. UsesKeyword(attributes),
        ];
    }

    internal static IEnumerable<string> UsesKeyword(Dictionary<string, string> attributes)
    {
        if (!attributes.TryGetValue("Uses", out string? uses) || uses.Length == 0)
        {
            yield break;
        }

        string[] parts = uses.Split(',');
        yield return parts.Length == 2 && parts[1].Length > 0
            ? $"Uses ({parts[0]} {parts[1]} counters)"
            : $"Uses ({uses})";
    }

    internal static List<string> CounterTypesOf(
        string printed, Dictionary<string, string> attributes)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        if (attributes.TryGetValue("Uses", out string? uses))
        {
            string[] parts = uses.Split(',');
            if (parts.Length == 2 && parts[1].Length > 0)
            {
                found.Add(parts[1].ToLowerInvariant());
            }
        }

        string[] words = printed.Split(
            [' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
        for (int index = 1; index < words.Length; index++)
        {
            string word = words[index].Trim('(', ')', '.', ',', ':', ';').ToLowerInvariant();
            if (word is not ("counter" or "counters"))
            {
                continue;
            }

            string type = words[index - 1]
                .Trim('(', ')', '.', ',', ':', ';')
                .ToLowerInvariant();
            if (type.Length > 0 && type.All(letter => char.IsLetter(letter) || letter == '-'))
            {
                found.Add(type);
            }
        }

        return found.Order(StringComparer.Ordinal).ToList();
    }

    internal static Dictionary<string, long> CounterMaximumsOf(string printed)
    {
        var found = new Dictionary<string, long>(StringComparer.Ordinal);
        string[] words = printed.Split(
            [' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
        for (int index = 6; index < words.Length; index++)
        {
            string counter = words[index].Trim('(', ')', '.', ',', ':', ';');
            if (counter is not ("counter" or "counters")
                || !string.Equals(words[index - 5], "enters", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(words[index - 4], "play", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(words[index - 3], "with", StringComparison.OrdinalIgnoreCase)
                || !long.TryParse(words[index - 2], out long maximum))
            {
                continue;
            }

            string type = words[index - 1]
                .Trim('(', ')', '.', ',', ':', ';')
                .ToLowerInvariant();
            found[type] = maximum;
        }

        return found;
    }

    /// <summary>
    /// The <c>[type]</c> of a "[type] form" keyword in a card's printed text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:form-change-form.6</c>. The keyword is a sentence of its own on the
    /// keyword line — "Energy form. Permanent." on <c>21002</c>, and
    /// "Permanent. Mass form." on <c>57046a</c>, so it is not always first.
    /// Reading it as a whole sentence is what separates the keyword from prose
    /// naming a form: obligation <c>42024</c> says "If you are in Archangel
    /// form, place 2 threat", and Nick Fury's <c>32031a</c> says "After you
    /// attack or defend in Solid mass form" — neither is a sentence, and
    /// neither grants anything.
    /// </para>
    /// <para>
    /// <b>Not what the engine reads.</b> <see cref="CardCatalog.FormKeyword"/> reads the
    /// structured <c>Form</c> attribute instead; this is the same fact taken
    /// from the printed words, and the two are held against each other so that
    /// a face whose attribute was missed when the dataset was built fails a
    /// test rather than silently granting no form.
    /// </para>
    /// </remarks>
    /// <param name="printed">The card's printed text, or null.</param>
    public static string? FormOf(string? printed)
    {
        if (printed is null)
        {
            return null;
        }

        foreach (string line in printed.Split('\n'))
        {
            foreach (string sentence in line.Split('.'))
            {
                if (Granted(sentence.Trim()) is { } form)
                {
                    return form;
                }
            }
        }

        return null;
    }

    // "Energy form" grants; "Hero form only" and "in Archangel form, place"
    // do not, because neither is the whole sentence.
    internal static string? Granted(string sentence)
    {
        const string suffix = " form";
        if (!sentence.EndsWith(suffix, StringComparison.Ordinal))
        {
            return null;
        }

        string type = sentence[..^suffix.Length];
        if (type.Length == 0 || !char.IsUpper(type[0]))
        {
            return null;
        }

        foreach (char letter in type)
        {
            if (!char.IsLetter(letter) && letter != '-')
            {
                return null;
            }
        }

        return type.ToLowerInvariant();
    }

    // The card data's `type` is the engine's printed face kind. `Villain` and
    // `SideScheme` name the encounter variants; PlayerSideScheme is its own
    // printed type. Leader remains distinct even though the expansion rules
    // make it function as a villain (`pack:mc56:leaders`).
    internal static CardKind ToKind(string type) => type switch
    {
        "Insert" => CardKind.Insert,
        "AlterEgo" => CardKind.AlterEgo,
        "Hero" => CardKind.Hero,
        "Ally" => CardKind.Ally,
        "Event" => CardKind.Event,
        "Resource" => CardKind.Resource,
        "Support" => CardKind.Support,
        "Upgrade" => CardKind.Upgrade,
        "Attachment" => CardKind.Attachment,
        "Obligation" => CardKind.Obligation,
        "Treachery" => CardKind.Treachery,
        "Minion" => CardKind.Minion,
        "MainScheme" => CardKind.MainScheme,
        // Engine-only. Tough, stunned and confused are cards, made mid-game
        // and attached to whoever gained the status.
        "Status" => CardKind.Status,
        "Villain" => CardKind.EncounterVillain,
        "SideScheme" => CardKind.EncounterSideScheme,
        "Leader" => CardKind.Leader,
        "Evidence" => CardKind.Evidence,
        "PlayerSideScheme" => CardKind.PlayerSideScheme,
        "Challenge" => CardKind.Challenge,

        // `rr:environment` is a card type of its own -- "an environment card
        // enters play in the villain's play area, and is active so long as it
        // remains in play" -- and `rr:reveal.2` gives it that destination. It
        // was missing here, so all eighty environments in the pool answered
        // `Unknown` and `Reveal.Resolve` discarded them like a treachery.
        "Environment" => CardKind.Environment,
        _ => CardKind.Unknown,
    };

    internal sealed record CardCatalogEntry(
        CardKind Kind,
        string Set,
        IReadOnlyList<string> LinkedTo,
        IReadOnlyList<string> Traits,
        IReadOnlyList<string> PrintedTraits,
        IReadOnlyDictionary<string, string> Attributes,
        string Title,
        string Subtitle,
        string Text,
        string FormattedText,
        IReadOnlyList<string> Keywords,
        IReadOnlyList<string> CounterTypes,
        IReadOnlyDictionary<string, long> CounterMaximums);
}
