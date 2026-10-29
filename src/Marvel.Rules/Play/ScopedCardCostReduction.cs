using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>A reduction supplied to the price of one identified card play.</summary>
/// <remarks>
/// Engine choice: carry this value with an invocation rather than installing a
/// next-card effect. Its identity check prevents using the quote for another game,
/// seat or physical card. It is not a resource and does not create a modifier
/// receipt to consume. The caller remains responsible for play legality and for
/// revalidating the current prompt and its source-specific conditions.
/// </remarks>
public sealed class ScopedCardCostReduction
{
    private readonly World world;
    private readonly Seat seat;
    private readonly Card card;
    private readonly int incarnation;
    private readonly string face;

    /// <summary>Bind a positive reduction to the specified physical card and player.</summary>
    public ScopedCardCostReduction(World world, Seat seat, Card card, long amount)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (seat.Index < 0 || seat.Index >= world.Seats.Count
            || !ReferenceEquals(world.Seats[seat.Index], seat)
            || card.ObjectId < 0 || card.ObjectId >= world.Cards.Count
            || !ReferenceEquals(world.Cards[card.ObjectId], card))
            throw new ArgumentException("The card and seat must belong to the game.");
        this.world = world;
        this.seat = seat;
        this.card = card;
        incarnation = card.Incarnation;
        face = card.FaceId;
        Amount = amount;
    }

    /// <summary>The reduction of this play's resource cost.</summary>
    public long Amount { get; }

    internal void Validate(World candidateWorld, Seat candidateSeat, Card candidateCard)
    {
        if (!ReferenceEquals(world, candidateWorld) || !ReferenceEquals(seat, candidateSeat)
            || !ReferenceEquals(card, candidateCard) || card.Incarnation != incarnation
            || !string.Equals(card.FaceId, face, StringComparison.Ordinal))
            throw new ArgumentException("The scoped reduction does not belong to this card play.");
    }
}
