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
    /// Measured rather than assigned. Across the 1,717 single-type cards that
    /// carry both the engine's <c>RES</c> letter and MarvelSDB's resource stat,
    /// the mapping agrees on all but three: <c>B</c> is mental on 519 cards,
    /// <c>Y</c> energy on 501, <c>R</c> physical on 458 and <c>G</c> wild on
    /// 236.
    /// </para>
    /// <para>
    /// The three exceptions are <c>20028</c> Shake it Off, <c>36012</c> Flash
    /// Freeze and <c>41024</c> Telepathy, where the two sources disagree about
    /// the printed card. Resources are not in the state digest, so no fixture
    /// settles it; the engine reads its own letter, as it reads its own traits.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<char> Types { get; } = [Mental, Energy, Physical, Wild];

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
        if (generated.Length < cost)
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

    /// <summary>
    /// The printed cost of playing a card, or null when it has none.
    /// </summary>
    /// <remarks>
    /// <c>rr:cost.2</c>'s per-player icon and a cost of <c>X</c> are both
    /// printed here and neither is implemented, so both are refused by name
    /// rather than read as a number. Measured over the pool: two cards print
    /// <c>X</c>, four print a <c>*</c>, and one prints a letter.
    /// </remarks>
    /// <param name="faceId">A printed card id.</param>
    /// <param name="facts">The printed card data.</param>
    /// <exception cref="RulesNotImplementedException">The cost is not a number.</exception>
    public static long? Cost(string faceId, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!facts.Attributes(faceId).TryGetValue("Cost", out string? printed)
            || printed.Length == 0)
        {
            return null;
        }

        if (long.TryParse(printed, out long cost))
        {
            return cost;
        }

        throw new RulesNotImplementedException(
            $"card '{faceId}' costs '{printed}', which is not a number; "
            + "rr:cost.2's per-player icon and a cost of X are not implemented");
    }
}
