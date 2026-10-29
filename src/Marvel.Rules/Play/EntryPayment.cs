using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>The completed resource cost of one physical card's entry.</summary>
/// <remarks>
/// The engine retains this occurrence-scoped snapshot for entry abilities.
/// It is distinct from the paid-only card-play receipt and is reconstructed by replay.
/// </remarks>
public sealed class EntryPayment
{
    private readonly Card card;

    private EntryPayment(Card card, int player, ResourcePaymentReceipt payment)
    {
        this.card = card;
        Card = card.ObjectId;
        OriginIncarnation = card.Incarnation;
        OriginArea = card.Area.Id;
        Player = player;
        Paid = payment.Paid;
        Generated = payment.Generated;
    }

    /// <summary>Capture a completed payment for its exact prospective entrant.</summary>
    public static EntryPayment Capture(Card card, int player, ResourcePaymentReceipt payment)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(payment);
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        if (!payment.IsFor(card, player))
            throw new InvalidOperationException("payment does not belong to this card and player");
        if (DeckTypes.IsInPlay(card.Area.Type))
            throw new InvalidOperationException("entry payment requires an out-of-play card");
        return new EntryPayment(card, player, payment);
    }

    /// <summary>The physical card whose cost was paid.</summary>
    public int Card { get; }
    /// <summary>The incarnation before entry.</summary>
    public int OriginIncarnation { get; }
    /// <summary>The area from which entry was initiated.</summary>
    public int OriginArea { get; }
    /// <summary>The player paying the card's entry cost.</summary>
    public int Player { get; }
    /// <summary>Resources paid for the card's cost.</summary>
    public string Paid { get; }
    /// <summary>Resources generated for the card's cost, including excess.</summary>
    public string Generated { get; }
    /// <summary>Generated resources beyond those paid for the cost.</summary>
    public int Excess => Generated.Length - Paid.Length;

    /// <summary>Whether the prospective entrant is still the captured card in its original area.</summary>
    public bool MatchesPending(Card card, int player) =>
        ReferenceEquals(this.card, card) && card.ObjectId == Card && card.Incarnation == OriginIncarnation
        && card.Area.Id == OriginArea && player == Player;

    /// <summary>Whether this is the exact resulting in-play incarnation.</summary>
    public bool MatchesEntered(Card card, int player) =>
        ReferenceEquals(this.card, card) && card.ObjectId == Card && card.Incarnation == OriginIncarnation + 1
        && DeckTypes.IsInPlay(card.Area.Type) && player == Player;
}
