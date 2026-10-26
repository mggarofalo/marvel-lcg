using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>The committed original price of one physical card entering play.</summary>
/// <remarks>The engine chooses an occurrence-owned receipt, reconstructed by command replay.
/// It describes the card play, never an interrupt or response ability cost.</remarks>
public sealed record PlayedCardPayment(int Card, int Incarnation, int Player, string Paid)
{
    /// <summary>Whether this receipt names the card's current entry and paid type.</summary>
    public bool Includes(Card source, int player, char resource) =>
        source.ObjectId == Card && source.Incarnation == Incarnation
        && player == Player && Paid.Contains(resource);
}
