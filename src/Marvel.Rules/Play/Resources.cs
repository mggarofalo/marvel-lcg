using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// Resources, and whether a handful of them pays a cost — <c>rr:resource</c>.
/// </summary>
/// <remarks>
/// <para>
/// "Resources are used to pay the cost to play cards and to pay certain ability
/// costs", and <c>rr:resource.1</c> says where they come from: "a player can
/// generate resources to pay a cost by <b>discarding cards from their hand</b>
/// to generate the resource or resources indicated at the bottom-left corner of
/// the card, or by using card abilities that generate resources."
/// </para>
/// <para>
/// <b>The letters are the wire format.</b> <c>ResourceSource.Generates</c>
/// carries "resource-type letters — one per resource", and the recorded prompt
/// fixture shows <c>"generates": "B"</c>. So a resource type is a
/// <see cref="char"/> here rather than an enum, and the four letters are the
/// ones the card data prints.
/// </para>
/// </remarks>
public static class Resources
{
    /// <summary>A mental resource. <c>rr:mental-resource</c>.</summary>
    public const char Mental = 'B';

    /// <summary>An energy resource. <c>rr:energy-resource</c>.</summary>
    public const char Energy = 'Y';

    /// <summary>A physical resource. <c>rr:physical-resource</c>.</summary>
    public const char Physical = 'R';

    /// <summary>
    /// A wild resource. <c>rr:wild-resource</c>.
    /// </summary>
    /// <remarks>
    /// <c>rr:resource.2</c>: "wild resources can be used as their type or any of
    /// the other types", which is why <see cref="Pays"/> counts them against a
    /// required type after the exact matches are spent.
    /// </remarks>
    public const char Wild = 'G';

    /// <summary>The four types, in the order the card data lists them.</summary>
    /// <remarks>
    /// <para>
    /// The letters are the engine's wire-format spelling. The generated card
    /// dataset maps MarvelSDB's structured mental, energy, physical and wild
    /// resource flags to <c>B</c>, <c>Y</c>, <c>R</c> and <c>G</c>, respectively.
    /// </para>
    /// <para>
    /// This mapping is chosen by the engine rather than named by the rulebook;
    /// the rulebook names the resource types, not their serialized letters.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<char> Types { get; } = [Mental, Energy, Physical, Wild];

    /// <summary>Whether a card prints a cost value that permits it to be played.</summary>
    public static bool HasPlayableCost(string faceId, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts.Attributes(faceId).TryGetValue("Cost", out string? printed)
            && !string.IsNullOrWhiteSpace(printed)
            && printed is not ("-" or "–");
    }

    /// <summary>
    /// What discarding this card from hand generates.
    /// </summary>
    /// <remarks>
    /// The printed <c>RES</c> field, which is already one letter per resource:
    /// <c>"BB"</c> is two mental. <c>rr:resource-card</c> is the card type whose
    /// "primary function is to be discarded from a player's hand to generate
    /// resources", but every player card has the field and any of them can be
    /// spent this way.
    /// </remarks>
    /// <param name="faceId">A printed card id.</param>
    /// <param name="facts">The printed card data.</param>
    public static string GeneratedBy(string faceId, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts.Attributes(faceId).TryGetValue("RES", out string? printed)
            ? printed
            : string.Empty;
    }

    /// <summary>How many printed icons of one resource type these cards have.</summary>
    /// <remarks>
    /// The printed <c>RES</c> field is one letter per icon, so a double-resource
    /// card contributes two. This deliberately reads printed data rather than
    /// <see cref="IResourceCardAbilities.ResourcesGeneratedBy"/>: an effect that counts
    /// icons on discarded cards is not paying a cost and does not receive a
    /// conditional resource-generator bonus.
    /// </remarks>
    public static long PrintedCount(
        IEnumerable<Card> cards, char resource, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(facts);

        return cards.Sum(card =>
            GeneratedBy(card.FaceId, facts).LongCount(each => each == resource));
    }

    /// <summary>
    /// The resource types a card's cost <b>must</b> include —
    /// <c>rr:requirement-resources</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "A card with the requirement keyword cannot be played unless each
    /// resource of the specified type is spent while paying for that card's
    /// cost", which <c>.1</c> writes as the constant ability "when paying this
    /// card's resource cost, you must spend the following resources:
    /// [resources]".
    /// </para>
    /// <para>
    /// <b>Part of the cost, not additional to it</b> — the same reading
    /// <c>rr:resource.4</c> gets, and <see cref="Pays"/> already had the
    /// parameter for it. What was missing was anybody passing one: thirteen
    /// cards print a <c>Requirement</c> and every one of them was payable with
    /// any three cards in hand.
    /// </para>
    /// <para>
    /// The letters are the same vocabulary <c>RES</c> uses — <c>27049</c>
    /// prints <c>YBR</c>, one of each of energy, mental and physical.
    /// </para>
    /// </remarks>
    /// <param name="world">The board carrying gained and lost characteristics.</param>
    /// <param name="card">The card whose requirement is being enforced.</param>
    /// <param name="facts">The printed card data.</param>
    /// <returns>The required letters, or an empty string.</returns>
    public static string Required(World world, Card card, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(facts);
        return !Characteristics.IsLost(world, card, "requirement")
            && facts.Attributes(card.FaceId).TryGetValue("Requirement", out string? printed)
            ? printed
            : string.Empty;
    }

    /// <summary>
    /// Whether these resources pay a cost.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:resource.3</c>: "a number of resources equal to <i>(or greater
    /// than)</i> the card's cost must be generated. For most cards, <b>any type
    /// (or mix of types)</b> of resources can be used to pay this cost." So a
    /// plain numeric cost is a count and nothing more — which is why
    /// <paramref name="required"/> is usually empty.
    /// </para>
    /// <para>
    /// <c>rr:resource.4</c> is the other case: "many abilities require specific
    /// resource types, and the specified types in the specified quantities must
    /// be generated". Those are spent first, and only then does the remainder
    /// count toward the number.
    /// </para>
    /// <para>
    /// <c>rr:cost.4</c> permits generating beyond the cost and <c>.4.2</c>
    /// throws the excess away, so this asks whether the cost is <i>met</i> and
    /// never whether it is met exactly.
    /// </para>
    /// </remarks>
    /// <param name="generated">Every resource generated, as letters.</param>
    /// <param name="cost">How many are needed in total.</param>
    /// <param name="required">Specific types that must be among them.</param>
    public static bool Pays(string generated, long cost, string? required = null)
    {
        ArgumentNullException.ThrowIfNull(generated);

        // **The required types are part of the cost, not additional to it.**
        // `rr:resource.4` reads "a number of resources equal to or greater than
        // this cost must be generated. Many abilities require specific resource
        // types, and the specified types in the specified quantities must be
        // generated **in order to pay the cost**" -- so a cost of 3 requiring
        // one mental is three resources of which one is mental, not four.
        if (generated.Length < cost || (required?.Length ?? 0) > cost)
        {
            return false;
        }

        var pool = new List<char>(generated);
        foreach (char type in required ?? string.Empty)
        {
            // The exact type first, and a wild only when there is none --
            // spending a wild that did not have to be spent could fail a later
            // requirement the exact one would have met.
            int found = pool.IndexOf(type);
            if (found < 0)
            {
                found = pool.IndexOf(Wild);
            }

            if (found < 0)
            {
                return false;
            }

            pool.RemoveAt(found);
        }

        return true;
    }

    /// <summary>Whether printed icons pay a cost without wild substitution.</summary>
    /// <remarks>
    /// <c>rr:printed.1.1</c>: “Wild resources cannot be spent as other resource
    /// types for such a cost.” A printed wild still pays a required wild or an
    /// unrestricted printed-resource slot; it does not become energy, mental,
    /// or physical here.
    /// </remarks>
    public static bool PaysPrinted(string generated, long cost, string? required = null)
    {
        return PaysExactTypes(generated, cost, required);
    }

    /// <summary>Whether player-declared paid types exactly satisfy a cost.</summary>
    /// <remarks>
    /// <c>rr:wild-resource</c>: a player generating a wild “must specify which
    /// resource type ... it is being used as.” Once declared, the wire letter
    /// is that type; a declaration of wild cannot later substitute for a
    /// physical, energy, or mental requirement.
    /// </remarks>
    public static bool PaysDeclared(string declared, long cost, string? required = null)
    {
        return PaysExactTypes(declared, cost, required);
    }

    private static bool PaysExactTypes(string generated, long cost, string? required)
    {
        ArgumentNullException.ThrowIfNull(generated);
        if (generated.Length < cost || (required?.Length ?? 0) > cost)
        {
            return false;
        }

        var pool = new List<char>(generated);
        foreach (char type in required ?? string.Empty)
        {
            int found = pool.IndexOf(type);
            if (found < 0)
            {
                return false;
            }
            pool.RemoveAt(found);
        }
        return true;
    }

    /// <summary>The exact resources paid for a satisfied cost, excluding overpayment.</summary>
    /// <remarks>
    /// <c>rr:cost.4.1</c> distinguishes resources generated beyond a cost from
    /// resources paid for it. Required types are allocated first, then the
    /// remaining generated icons in generator order. That ordering is the
    /// engine's deterministic allocation when no card effect distinguishes
    /// otherwise; the rulebook does not define an engine representation.
    /// </remarks>
    public static string Paid(string generated, long cost, string? required = null)
    {
        ArgumentNullException.ThrowIfNull(generated);
        if (!Pays(generated, cost, required))
        {
            throw new ArgumentException("The generated resources do not pay the cost.", nameof(generated));
        }

        var used = new bool[generated.Length];
        foreach (char type in required ?? string.Empty)
        {
            int found = -1;
            for (int index = 0; index < used.Length; index++)
            {
                if (!used[index] && generated[index] == type)
                {
                    found = index;
                    break;
                }
            }
            if (found < 0)
            {
                for (int index = 0; index < used.Length; index++)
                {
                    if (!used[index] && generated[index] == Wild)
                    {
                        found = index;
                        break;
                    }
                }
            }

            used[found] = true;
        }

        long selected = used.LongCount(taken => taken);
        for (int index = 0; index < used.Length && selected < cost; index++)
        {
            if (!used[index])
            {
                used[index] = true;
                selected += 1;
            }
        }

        return string.Concat(generated.Where((_, index) => used[index]));
    }

    /// <summary>
    /// The printed cost of playing a card, or null when it has none.
    /// </summary>
    /// <remarks>
    /// <c>rr:cost.2</c>'s per-player icon is multiplied by the stable starting
    /// player count. A cost of <c>X</c> still needs the card ability or player
    /// choice that defines it and is refused by name rather than read as zero.
    /// </remarks>
    /// <param name="faceId">A printed card id.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="players">The number of players who started the scenario.</param>
    /// <exception cref="RulesNotImplementedException">The cost is not a number.</exception>
    public static long? Cost(string faceId, ICardFacts facts, int players = 1)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(players);
        if (!facts.Attributes(faceId).TryGetValue("Cost", out string? printed)
            || printed.Length == 0)
        {
            return null;
        }

        if (long.TryParse(printed, out long cost))
        {
            return cost;
        }

        if (printed.EndsWith('*')
            && long.TryParse(printed[..^1], out long perPlayer))
        {
            // `rr:cost.2`: the multiplier is the number who started the
            // scenario. World.Players is immutable when a seat is eliminated,
            // unlike World.PlayerOrder, so callers pass that stable count.
            return checked(perPlayer * players);
        }

        throw new RulesNotImplementedException(
            $"card '{faceId}' costs '{printed}', which is not a number; "
            + "a cost of X is not implemented");
    }
}
