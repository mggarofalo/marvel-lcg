namespace Marvel.Rules.State;

/// <summary>Reads a world without allocating anything in it.</summary>
public static class WorldReads
{
    /// <summary>The area matching a place, or <c>null</c> when the world has not made it.</summary>
    /// <remarks>
    /// Never makes one, so a caller that reports an area's id reports only an
    /// area the world holds.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="type">What kind of place it is.</param>
    /// <param name="playArea">Which play area it sits in. Defaults to the villain's.</param>
    /// <param name="host">The card it hangs off, or -1.</param>
    public static Area? FindArea(this World world, DeckType type, PlayArea? playArea = null, int host = -1) =>
        world._places.Find(type, playArea ?? PlayArea.Villains, host);

    /// <summary>Opens a read of the world that allocates nothing until it is disposed.</summary>
    /// <remarks>
    /// Inside it <see cref="WorldTopology.AreaOf"/> answers a place the world has not made
    /// with an empty place that is never added, and making an area, a card or
    /// a game area raises. See <see cref="WorldAreas"/>.
    /// </remarks>
    /// <param name="world">The world.</param>
    public static WorldRead BeginRead(this World world) => world._places.Begin();
}
