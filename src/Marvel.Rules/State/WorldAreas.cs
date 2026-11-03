namespace Marvel.Rules.State;

/// <summary>
/// Every area a world has made, the one allocator of area ids, and the reads
/// open on that world.
/// </summary>
/// <remarks>
/// <para>
/// An area id is its position in creation order, and <c>AreaRef.Id</c> carries
/// it on the event wire, so making an area earlier or later renumbers every area
/// made after it. Only a step of the game may make one. A read -- the state
/// digest, a client's projection -- answers from the board as it stands.
/// </para>
/// <para>
/// A lookup inside a read that names a place the world has not made yet answers
/// an empty place with id -1 that is never added: a place nothing has reached
/// holds nothing. The same place is answered for the rest of the read, so a
/// card a read moves there and back is found where it was put. A read that
/// leaves a card in such a place, or that would make an area or a card outright,
/// raises. That the read answers rather than allocates is the engine's choice;
/// the rules are silent on bookkeeping identities.
/// </para>
/// </remarks>
internal sealed class WorldAreas
{
    private readonly List<Area> made = [];
    private readonly List<Area> unmade = [];
    private int reads;

    /// <summary>Every area made, in creation order.</summary>
    internal IReadOnlyList<Area> All => made;

    /// <summary>Whether a read is open on the world.</summary>
    internal bool Reading => reads > 0;

    /// <summary>The made area matching a place, or <c>null</c>.</summary>
    internal Area? Find(DeckType type, PlayArea where, int host)
    {
        foreach (var area in made)
        {
            if (area.Type == type && area.PlayArea == where && area.Host == host)
            {
                return area;
            }
        }

        return null;
    }

    /// <summary>Makes an area with the next id.</summary>
    internal Area Make(DeckType type, int cardOwner, PlayArea where, int host, Card? hostCard)
    {
        Refuse($"a {type} area");
        var area = new Area(made.Count, type, cardOwner, where, host, hostCard);
        made.Add(area);
        return area;
    }

    /// <summary>The empty, never-added place a read answers for an area not yet made.</summary>
    internal Area Unmade(DeckType type, int cardOwner, PlayArea where, int host, Card? hostCard)
    {
        foreach (var area in unmade)
        {
            if (area.Type == type && area.PlayArea == where && area.Host == host)
            {
                return area;
            }
        }

        var place = new Area(-1, type, cardOwner, where, host, hostCard);
        unmade.Add(place);
        return place;
    }

    /// <summary>Raises when a read is open, naming what it tried to allocate.</summary>
    internal void Refuse(string allocation)
    {
        if (Reading)
        {
            throw new InvalidOperationException($"a read of the world tried to make {allocation}");
        }
    }

    /// <summary>Opens a read; reads may nest.</summary>
    internal WorldRead Begin()
    {
        reads++;
        return new WorldRead(this);
    }

    /// <summary>Closes a read, and forgets its unmade places once the outermost closes.</summary>
    internal void End()
    {
        if (--reads > 0)
        {
            return;
        }

        var occupied = unmade.Find(area => area.Cards.Count > 0);
        unmade.Clear();
        if (occupied is not null)
        {
            throw new InvalidOperationException(
                $"a read of the world left a card in the {occupied.Type} area it never made");
        }
    }
}
