namespace Marvel.Rules.State;

/// <summary>
/// One card. <b>One card, however many faces it has printed on it.</b>
/// </summary>
/// <remarks>
/// <para>
/// An identity is one card with a hero side and an alter-ego side; a main scheme
/// is one card with an <c>A</c> and a <c>B</c> side. Both get one
/// <see cref="ObjectId"/>. Treating a face as a card would shift every id after
/// it, and ids are on the wire.
/// </para>
/// <para>
/// <see cref="Faces"/> can be <i>replaced</i> and not merely flipped: the Ultron
/// scenario turns a player card into a facedown drone in place, keeping the
/// object id. Nothing at setup does that, but the model has to allow it.
/// </para>
/// </remarks>
public sealed class Card
{
    internal Card(int objectId, IReadOnlyList<string> faces, int owner)
    {
        ObjectId = objectId;
        Faces = faces;
        Owner = owner;
    }

    /// <summary>The card's id. Its position in the deal order.</summary>
    public int ObjectId { get; }

    /// <summary>The printed face ids, in the order the engine created them.</summary>
    public IReadOnlyList<string> Faces { get; private set; }

    /// <summary>Which face is currently showing.</summary>
    public int FaceIndex { get; private set; }

    /// <summary>The printed id of the face currently showing.</summary>
    public string FaceId => Faces[FaceIndex];

    /// <summary>The seat that owns this card, or -1 for the scenario.</summary>
    public int Owner { get; }

    /// <summary>Where the card is.</summary>
    public Area Area { get; private set; } = null!;

    /// <summary>Whether the card is face up.</summary>
    public bool FaceUp { get; private set; } = true;

    /// <summary>Whether the card is ready. <c>is_exhaust</c> is its negation.</summary>
    public bool Ready { get; private set; } = true;

    /// <summary>Turns the card to a named face.</summary>
    /// <param name="faceId">A printed id this card carries.</param>
    /// <exception cref="ArgumentException">The card has no such face.</exception>
    public void TurnTo(string faceId)
    {
        int index = Faces.ToList().IndexOf(faceId);
        FaceIndex = index >= 0
            ? index
            : throw new ArgumentException($"card {ObjectId} has no face '{faceId}'", nameof(faceId));
    }

    internal void MovedTo(Area area)
    {
        Area = area;
        FaceUp = !DeckTypes.FaceDownOnEntry(area.Type);
    }
}
