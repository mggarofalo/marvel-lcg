using System.Globalization;
using System.Text.Json;
using Marvel.Rules.State;

namespace Marvel.Content;

/// <summary>
/// The printed cards, as <c>datasets/cards/cards.json</c> records them.
/// </summary>
/// <remarks>
/// <para>
/// Satisfies <see cref="ICardFacts"/>, which is declared in <c>Marvel.Rules</c>.
/// The arrow points that way on purpose: the rules say what they need and the
/// content assembly supplies it, so nothing below the content layer has to know
/// this file exists.
/// </para>
/// <para>
/// <b>Traits are read from the engine's list, not derived from the printed
/// one.</b> They are two different lists and the digest is built from the
/// engine's — see <see cref="TraitKey"/> and <c>docs/card-dataset.md</c>.
/// </para>
/// </remarks>
public sealed class CardCatalog : ICardFacts
{
    private readonly IReadOnlyDictionary<string, Entry> cards;

    private CardCatalog(IReadOnlyDictionary<string, Entry> cards) => this.cards = cards;

    /// <summary>How many cards the catalog holds.</summary>
    public int Count => cards.Count;

    /// <summary>Parses the canonical <c>cards.json</c> text.</summary>
    /// <param name="json">The dataset.</param>
    /// <exception cref="JsonException">The text is not a card dataset.</exception>
    public static CardCatalog Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("cards", out var array)
            || array.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("the card dataset has no 'cards' array");
        }

        var parsed = new Dictionary<string, Entry>(StringComparer.Ordinal);
        foreach (var element in array.EnumerateArray())
        {
            string id = element.GetProperty("card_id").GetString()
                        ?? throw new JsonException("a card has no 'card_id'");
            parsed[id] = ReadEntry(element);
        }

        return new CardCatalog(parsed);
    }

    /// <inheritdoc />
    public CardKind Kind(string faceId) => Find(faceId).Kind;

    /// <inheritdoc />
    /// <remarks>
    /// Read from the printed <c>Form</c> attribute, which the card data carries
    /// as a structured field on exactly the nine faces that print the keyword.
    /// <see cref="FormOf"/> reads the same fact out of the printed <i>text</i>,
    /// and <c>FormDataTests</c> holds the two against each other — the
    /// attribute drives the engine, and the text is what would catch a face
    /// whose attribute was missed when the dataset was built.
    /// </remarks>
    public string? FormKeyword(string faceId)
    {
        var printed = Find(faceId).Attributes;
        return printed.TryGetValue("Form", out string? form) && form.Length > 0
            ? form.ToLowerInvariant()
            : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Traits(string faceId) => Find(faceId).Traits;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Attributes(string faceId) => Find(faceId).Attributes;

    /// <inheritdoc />
    public long PrintedValue(string faceId, string attribute, int players, long fallback = 0)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        return Find(faceId).Attributes.TryGetValue(attribute, out var printed)
            ? Evaluate(printed, players, fallback)
            : fallback;
    }

    /// <summary>
    /// The digest's spelling of an engine trait, without the <c>t_</c> prefix.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CardFace.GetInfoTraits</c> builds every key as
    /// <c>f"t_{trait.replace(' ', '_').replace('!', '')}"</c> over the engine's
    /// own trait list, so this is those two substitutions and nothing else. The
    /// engine's traits are already upper-case and already carry no trailing
    /// stop — <c>A.I.M</c> and <c>S.H.I.E.L.D</c> are stored that way.
    /// </para>
    /// <para>
    /// <b>Two traits carry the <c>!</c>:</b> <c>CHASE!</c> and <c>TRAP!</c>, on
    /// five cards between them (<c>27102a</c>, <c>27102b</c>, <c>47031</c>,
    /// <c>47032</c>, <c>47033</c>). Dropping it is not cosmetic — the digest key
    /// is <c>t_TRAP</c>, and a port that kept the <c>!</c> would emit
    /// <c>t_TRAP!</c> and fail the byte comparison on every step those cards are
    /// in play.
    /// </para>
    /// <para>
    /// <b>This is not the printed spelling, and the difference is not only
    /// spelling.</b> MARVEL-177 measured the two lists across 3,999 cards:
    /// they disagree about the card itself on <b>twelve</b> — the engine gives
    /// <c>01172</c> the <c>CRIMINAL</c> trait and the printed card has none;
    /// <c>42016</c> is the other way round; <c>39029</c> is an engine typo,
    /// <c>THESPYAN</c> for <c>THESPIAN</c>. They are reported as
    /// <c>engine_traits_diverge</c> in <c>datasets/cards/anomalies.json</c>.
    /// Reading the engine's list is what makes the twelve visible data rather
    /// than a silent divergence.
    /// </para>
    /// </remarks>
    /// <param name="engineTrait">The trait as the engine's data stores it.</param>
    public static string TraitKey(string engineTrait)
    {
        ArgumentNullException.ThrowIfNull(engineTrait);
        return engineTrait.Replace(" ", "_", StringComparison.Ordinal)
                          .Replace("!", string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// A printed value, with <c>*</c> read as "per player".
    /// </summary>
    /// <remarks>
    /// The engine substitutes the player count for every <c>*</c> and evaluates
    /// the result as arithmetic, so <c>14*</c> at three players is 42 and
    /// <c>1**</c> is 9. Reproduced rather than reinterpreted. A value that is not
    /// a number at all — <c>RES: Y</c>, <c>Class: Hero</c>, <c>Stage: 1A</c> —
    /// answers <paramref name="fallback"/>.
    /// </remarks>
    /// <param name="printed">The printed string.</param>
    /// <param name="players">How many players are in the game.</param>
    /// <param name="fallback">What to answer when it is not a number.</param>
    public static long Evaluate(string printed, int players, long fallback = 0)
    {
        ArgumentNullException.ThrowIfNull(printed);

        int stars = printed.Count(character => character == '*');
        string digits = printed.TrimEnd('*');
        if (!long.TryParse(digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture,
                           out long value))
        {
            return fallback;
        }

        for (int multiplied = 0; multiplied < stars; multiplied++)
        {
            value *= players;
        }

        return value;
    }

    private Entry Find(string faceId)
    {
        ArgumentNullException.ThrowIfNull(faceId);
        return cards.TryGetValue(faceId, out var entry)
            ? entry
            : throw new KeyNotFoundException($"no card '{faceId}' in the card dataset");
    }

    private static Entry ReadEntry(JsonElement element)
    {
        // Deliberately not the top-level `traits`, which is MarvelSDB's
        // printed list. A card the engine does not have gets none, which is
        // right: the engine cannot put a card it has never heard of into a
        // digest.
        var traits = new List<string>();
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        var kind = CardKind.Unknown;
        if (element.TryGetProperty("engine", out var engine)
            && engine.ValueKind == JsonValueKind.Object)
        {
            if (engine.TryGetProperty("traits", out var engineTraits)
                && engineTraits.ValueKind == JsonValueKind.Array)
            {
                foreach (var trait in engineTraits.EnumerateArray())
                {
                    if (trait.GetString() is { Length: > 0 } text)
                    {
                        traits.Add(TraitKey(text));
                    }
                }
            }

            if (engine.TryGetProperty("type", out var type) && type.GetString() is string name)
            {
                kind = ToKind(name);
            }

            if (engine.TryGetProperty("attributes", out var printedAttributes)
                && printedAttributes.ValueKind == JsonValueKind.Object)
            {
                foreach (var attribute in printedAttributes.EnumerateObject())
                {
                    attributes[attribute.Name] = attribute.Value.GetString() ?? string.Empty;
                }
            }
        }

        return new Entry(kind, traits, attributes);
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
    /// <b>Not what the engine reads.</b> <see cref="FormKeyword"/> reads the
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
    private static string? Granted(string sentence)
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

    // The card data's `type` is the engine's face class on every kind but two:
    // `Villain` and `SideScheme` each have a player and an encounter variant,
    // and a scenario's own cards are always the encounter one. A player side
    // scheme is a different class and it does not appear on an opening board.
    private static CardKind ToKind(string type) => type switch
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
        _ => CardKind.Unknown,
    };

    private sealed record Entry(
        CardKind Kind,
        IReadOnlyList<string> Traits,
        IReadOnlyDictionary<string, string> Attributes);
}
