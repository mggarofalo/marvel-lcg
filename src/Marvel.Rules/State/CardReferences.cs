namespace Marvel.Rules.State;

/// <summary>Which card a reference such as "the main scheme" or "the villain" means.</summary>
/// <remarks>
/// These answer the same question as <see cref="Places"/> — what a card's own
/// text reaches by where it is — for references that name one card rather
/// than a set.
/// </remarks>
public static class CardReferences
{
    /// <summary>The one card of this type that a card's text refers to, or <c>null</c>.</summary>
    /// <remarks>
    /// <para>
    /// "The main scheme" and "the villain" name one card, and in an ordinary
    /// game there is one of each, so the first such card in play is the answer.
    /// While the table is split into game areas there is one per game area, and
    /// <c>pack:mc11:game-areas</c> settles which: "Cards and components in one
    /// game area cannot affect another game area". So the card in the source's
    /// own game area is preferred, and a card in another game area is never
    /// returned.
    /// </para>
    /// <para>
    /// A central card in no game area — Kang's 2B, "active for all players"
    /// (<c>pack:mc11:areas</c>) — is reachable from everywhere, so it is the
    /// answer only when the source's game area has no card of this type, or
    /// when the source is itself in no game area. In the one-area case every
    /// card shares the source's game area, so the answer is the first card of
    /// the type in play, as it is without game areas.
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="type">What kind of place to look in.</param>
    /// <param name="source">The card whose text refers to the card.</param>
    public static Card? TheCardIn(World world, DeckType type, Card source)
    {
        ArgumentNullException.ThrowIfNull(world);
        var home = Places.GameAreaOf(world, source);
        Card? reachable = null;
        foreach (var area in world.Areas)
        {
            if (area.Type != type || area.Cards.Count == 0)
            {
                continue;
            }

            var card = area.Cards[0];
            var there = Places.GameAreaOf(world, card);
            if (home is not null && ReferenceEquals(there, home))
            {
                return card;
            }

            if (reachable is null && (home is null || there is null))
            {
                reachable = card;
            }
        }

        return reachable;
    }
}
