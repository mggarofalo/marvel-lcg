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
/// <b>Traits are derived, not read.</b> The dataset carries MarvelSDB's printed
/// spelling — <c>Hero for Hire</c>, <c>S.H.I.E.L.D.</c> — while the digest keys
/// them as <c>t_HERO_FOR_HIRE</c> and <c>t_S.H.I.E.L.D</c>. See
/// <see cref="TraitKey"/> for the transformation and for what it cannot fix.
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
    /// The digest's spelling of a printed trait, without the <c>t_</c> prefix.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Upper-case, trailing full stop dropped, spaces to underscores:
    /// <c>Hero for Hire</c> becomes <c>HERO_FOR_HIRE</c> and
    /// <c>S.H.I.E.L.D.</c> becomes <c>S.H.I.E.L.D</c>. Reproduces every
    /// <c>t_</c> key on the milestone board.
    /// </para>
    /// <para>
    /// <b>What it cannot fix.</b> Derivation is not the same as having the
    /// engine's list. Compared across 3,999 cards the printed traits and the
    /// Python engine's own trait lists disagree outright on <b>142</b> — the
    /// engine gives <c>01172</c> the <c>CRIMINAL</c> trait and the printed card
    /// has none; <c>02033</c> is the other way round. None of the 142 is on the
    /// milestone board, so this is a gap and not yet a failure, and it will
    /// surface the moment a corpus replay reaches one of them. The fix is for
    /// the card dataset to carry the engine's trait list beside the printed one,
    /// which is a change to <c>datasets/cards/</c> and belongs in its own issue.
    /// </para>
    /// </remarks>
    /// <param name="printedTrait">The trait as MarvelSDB prints it.</param>
    public static string TraitKey(string printedTrait)
    {
        ArgumentNullException.ThrowIfNull(printedTrait);
        return printedTrait.ToUpperInvariant().TrimEnd('.').Replace(' ', '_');
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
        var traits = new List<string>();
        if (element.TryGetProperty("traits", out var printed)
            && printed.ValueKind == JsonValueKind.Array)
        {
            foreach (var trait in printed.EnumerateArray())
            {
                string? text = trait.GetString();
                if (!string.IsNullOrEmpty(text))
                {
                    traits.Add(TraitKey(text));
                }
            }
        }

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        var kind = CardKind.Unknown;
        if (element.TryGetProperty("engine", out var engine)
            && engine.ValueKind == JsonValueKind.Object)
        {
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
        "Villain" => CardKind.EncounterVillain,
        "SideScheme" => CardKind.EncounterSideScheme,
        _ => CardKind.Unknown,
    };

    private sealed record Entry(
        CardKind Kind,
        IReadOnlyList<string> Traits,
        IReadOnlyDictionary<string, string> Attributes);
}
