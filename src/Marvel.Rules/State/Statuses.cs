namespace Marvel.Rules.State;

/// <summary>
/// Status cards: tough, stunned, confused.
/// </summary>
/// <remarks>
/// <para>
/// <b>A status is a card, not a flag.</b> The recorded milestone board is
/// unambiguous: when Rhino gains Tough, a new card appears with its own object
/// id, in <c>StatusArea</c>, attached to Rhino — and Rhino's own
/// <c>toughness</c> field stays at zero. Modelling it as a counter on the
/// villain would produce a board with the right behaviour and the wrong digest.
/// </para>
/// <para>
/// It is also the first card the engine makes after setup, so it is where the
/// append-only id contract is first tested by something other than dealing:
/// the milestone board deals 81 cards, ids 0..80, and the Tough is 81.
/// </para>
/// </remarks>
public static class Statuses
{
    /// <summary>The Tough status card's printed id.</summary>
    public const string Tough = "tough";

    /// <summary>Whether a card already carries a status.</summary>
    /// <param name="world">The world.</param>
    /// <param name="host">The card that might carry it.</param>
    /// <param name="status">The status's printed id.</param>
    public static bool Has(World world, Card host, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(host);

        foreach (var area in world.Areas)
        {
            if (area.Type != DeckType.StatusArea || area.Host != host.ObjectId)
            {
                continue;
            }

            foreach (var card in area.Cards)
            {
                if (card.FaceId == status)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Gives a card a status.</summary>
    /// <remarks>
    /// The status area belongs to the scenario however it was caused, which is
    /// why the recorded Tough on Rhino has owner -1: a card takes the owner of
    /// the place it is made in, and this place is the villain's.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="host">The card gaining it.</param>
    /// <param name="status">The status's printed id.</param>
    /// <returns>The new card.</returns>
    public static Card Give(World world, Card host, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(host);

        var area = world.AreaOf(
            DeckType.StatusArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);
        return world.CreateCard(status, area);
    }
}
