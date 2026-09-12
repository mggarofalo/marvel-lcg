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
/// The dataset is generated from the vendored MarvelSDB snapshot by
/// <c>tools/Marvel.Cards.Extract</c>, so everything here is a reading of a
/// printed card. <see cref="TraitKey"/> is the one place a printed word is
/// reshaped, and it reshapes the spelling rather than the fact.
/// </para>
/// </remarks>
public sealed class CardCatalog : ICardFacts
{
    private readonly IReadOnlyDictionary<string, CardCatalogJson.CardCatalogEntry> cards;

    private CardCatalog(IReadOnlyDictionary<string, CardCatalogJson.CardCatalogEntry> cards) => this.cards = cards;

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

        var parsed = new Dictionary<string, CardCatalogJson.CardCatalogEntry>(StringComparer.Ordinal);
        foreach (var element in array.EnumerateArray())
        {
            string id = element.GetProperty("card_id").GetString()
                        ?? throw new JsonException("a card has no 'card_id'");
            parsed[id] = CardCatalogJson.ReadEntry(element);
        }

        return new CardCatalog(parsed);
    }

    /// <summary>Whether the dataset carries a face.</summary>
    /// <remarks>
    /// For a caller walking a <i>different</i> list — the vendored snapshot the
    /// dataset is generated from carries one record for a double-sided upgrade
    /// where this carries its two faces, so the two lists are not the same set
    /// of ids.
    /// </remarks>
    /// <param name="faceId">A face id.</param>
    public bool Has(string faceId)
    {
        ArgumentNullException.ThrowIfNull(faceId);
        return cards.ContainsKey(faceId);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> LinkedCards(string bringingFaceId)
    {
        ArgumentNullException.ThrowIfNull(bringingFaceId);
        return
        [
            .. cards
                .Where(entry => entry.Value.LinkedTo.Contains(
                    bringingFaceId, StringComparer.Ordinal))
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => entry.Key),
        ];
    }

    /// <inheritdoc />
    public CardKind Kind(string faceId) => Find(faceId).Kind;

    /// <inheritdoc />
    public string EncounterSet(string faceId) => Find(faceId).Set;

    /// <inheritdoc />
    /// <remarks>
    /// Read from the printed <c>Form</c> attribute, which the card data carries
    /// as a structured field on exactly the nine faces that print the keyword.
    /// <see cref="CardCatalogJson.FormOf"/> reads the same fact out of the printed <i>text</i>,
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
    public string? RequiredForm(string faceId) => RequiredFormOf(Find(faceId).Text);

    /// <summary>Reads a printed "[type] form only" play restriction.</summary>
    /// <param name="printed">The card's printed text, or null.</param>
    public static string? RequiredFormOf(string? printed)
    {
        if (printed is null)
        {
            return null;
        }

        const string suffix = " form only";
        foreach (string line in printed.Split('\n'))
        {
            foreach (string sentence in line.Split('.'))
            {
                string trimmed = sentence.Trim();
                if (!trimmed.EndsWith(suffix, StringComparison.Ordinal))
                {
                    continue;
                }

                string form = trimmed[..^suffix.Length];
                if (form.Length > 0 && char.IsUpper(form[0]))
                {
                    return form.ToLowerInvariant();
                }
            }
        }

        return null;
    }

    /// <inheritdoc />
    public string Title(string faceId) => Find(faceId).Title;

    /// <inheritdoc/>
    public string Subtitle(string faceId) => Find(faceId).Subtitle;

    /// <inheritdoc/>
    public string Text(string faceId) => Find(faceId).Text;

    /// <inheritdoc/>
    public string FormattedText(string faceId) => Find(faceId).FormattedText;

    /// <inheritdoc/>
    public IReadOnlyList<string> Keywords(string faceId) => Find(faceId).Keywords;

    /// <inheritdoc/>
    /// <remarks>
    /// Read out of the text box, because the star icon survives nowhere else:
    /// 418 of the 419 cards that have one print "[star] Boost:" and `39029`
    /// Supporting Actor prints "Boost:" with the marker missing from the
    /// extraction. Both are the same ability, so both count.
    /// </remarks>
    public bool HasBoostAbility(string faceId) =>
        Find(faceId).Text.Contains("Boost:", StringComparison.Ordinal);

    /// <inheritdoc/>
    /// <remarks>
    /// Out of the text box, like <see cref="HasBoostAbility"/>: the printed
    /// attributes say nothing about it, so an unwritten ability and a card
    /// that has none would otherwise look identical.
    /// </remarks>
    public bool HasWhenDefeated(string faceId) =>
        Find(faceId).Text.Contains("When Defeated:", StringComparison.Ordinal);

    /// <inheritdoc />
    public IReadOnlyList<string> Traits(string faceId) => Find(faceId).Traits;

    /// <inheritdoc />
    public IReadOnlyList<string> PrintedTraits(string faceId) => Find(faceId).PrintedTraits;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Attributes(string faceId) => Find(faceId).Attributes;

    /// <inheritdoc />
    public IReadOnlyList<string> CounterTypes(string faceId) => Find(faceId).CounterTypes;

    /// <inheritdoc />
    public long? CounterMaximum(string faceId, string type)
    {
        var maxima = Find(faceId).CounterMaximums;
        return maxima.TryGetValue(type.ToLowerInvariant(), out long maximum)
            ? maximum
            : null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>The <c>*</c> means two different things and the card kind is what
    /// tells them apart.</b> On a villain's <c>HP</c>, a scheme's threat values
    /// and a handful of costs it is the per-player icon and
    /// <see cref="Evaluate"/> multiplies. <b>On an ally's <c>ATK</c> or
    /// <c>THW</c> it is a consequential damage icon</b>, and multiplying by the
    /// player count is simply wrong: Black Cat's <c>THW</c> of <c>"1*"</c> is
    /// thwart 1 with one consequential damage, not thwart 3 at three players.
    /// </para>
    /// <para>
    /// Measured over the pool: 660 of the 664 ally <c>ATK</c>/<c>THW</c> values
    /// have the number before the star equal to MarvelSDB's printed value and
    /// the star count equal to its <c>attack_cost</c>/<c>thwart_cost</c>. The
    /// four that do not are two cards MarvelSDB records with a base of
    /// <c>-1</c>, where the star count still agrees.
    /// </para>
    /// <para>
    /// <b>No recording could catch this.</b> Every recorded game has one
    /// player, and <c>1*</c> at one player is 1 either way. 642 attribute
    /// values on allies were being read wrong at every other table size.
    /// </para>
    /// </remarks>
    public long PrintedValue(string faceId, string attribute, int players, long fallback = 0)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        var entry = Find(faceId);
        if (!entry.Attributes.TryGetValue(attribute, out var printed))
        {
            return fallback;
        }

        return IsConsequential(entry.Kind, attribute)
            ? Evaluate(printed.Replace("*", string.Empty, StringComparison.Ordinal),
                       players, fallback)
            : Evaluate(printed, players, fallback);
    }

    /// <summary>
    /// How many consequential damage icons sit under one of an ally's powers —
    /// <c>rr:consequential-damage</c>.
    /// </summary>
    /// <remarks>
    /// "After an ally attacks, it takes consequential damage equal to the
    /// number of consequential damage icons <b>beneath its ATK field</b>." The
    /// icons are printed in the same attribute as the value, as stars after it.
    /// </remarks>
    /// <param name="faceId">A printed card id.</param>
    /// <param name="attribute">The power, <c>ATK</c> or <c>THW</c>.</param>
    public long ConsequentialDamage(string faceId, string attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        var entry = Find(faceId);
        return IsConsequential(entry.Kind, attribute)
               && entry.Attributes.TryGetValue(attribute, out string? printed)
            ? printed.Count(letter => letter == '*')
            : 0;
    }

    // `rr:consequential-damage` is an ally rule -- "after an **ally** attacks"
    // -- and only these two fields have icons beneath them. Everything else
    // keeps the per-player reading.
    //
    // **The kind check cannot be observed on today's pool** and is kept anyway:
    // no non-ally card prints a starred `ATK` or `THW`, so deleting it changes
    // nothing a test could see. It is the difference between "allies have
    // consequential damage" and "a star in ATK is consequential damage", and
    // the first is what the rule says. A minion printing a starred ATK in some
    // later pack would find the second reading already wrong.
    private static bool IsConsequential(CardKind kind, string attribute) =>
        kind == CardKind.Ally
        && (string.Equals(attribute, "ATK", StringComparison.Ordinal)
            || string.Equals(attribute, "THW", StringComparison.Ordinal));

    /// <summary>
    /// The digest's spelling of a trait, without the <c>t_</c> prefix.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A trait is a printed word and a digest key is a wire format, and the two
    /// cannot be the same thing: "Hero for Hire" has spaces in it. So this is
    /// two substitutions — a space becomes an underscore and a <c>!</c> goes —
    /// and everything else is left as printed. <c>A.I.M</c> and
    /// <c>S.H.I.E.L.D</c> keep their stops. <b>Which two substitutions is our
    /// choice</b>; the only property they have to keep is holding still.
    /// </para>
    /// <para>
    /// <b>Two traits carry the <c>!</c>:</b> <c>CHASE!</c> and <c>TRAP!</c>, on
    /// five cards between them (<c>27102a</c>, <c>27102b</c>, <c>47031</c>,
    /// <c>47032</c>, <c>47033</c>). Dropping it is not cosmetic — the digest key
    /// is <c>t_TRAP</c>, and a reader that kept the <c>!</c> would emit
    /// <c>t_TRAP!</c> for every step those cards are in play.
    /// </para>
    /// </remarks>
    /// <param name="printedTrait">The trait as the printed card carries it.</param>
    public static string TraitKey(string printedTrait)
    {
        ArgumentNullException.ThrowIfNull(printedTrait);
        return printedTrait.Replace(" ", "_", StringComparison.Ordinal)
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

    private CardCatalogJson.CardCatalogEntry Find(string faceId)
    {
        ArgumentNullException.ThrowIfNull(faceId);
        return cards.TryGetValue(faceId, out var entry)
            ? entry
            : throw new KeyNotFoundException($"no card '{faceId}' in the card dataset");
    }
}
