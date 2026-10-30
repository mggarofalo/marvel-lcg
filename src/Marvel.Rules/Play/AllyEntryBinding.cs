using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Exact physical incarnation produced by a pending ally entry.</summary>
public sealed class AllyEntryBinding
{
    /// <summary>The card and its entry area and incarnation, once entry applies.</summary>
    public (int Card, int Area, int Incarnation)? Entered { get; private set; }

    internal void Capture(Card card)
    {
        if (Entered is not null) throw new InvalidOperationException("ally entry binding was already captured");
        Entered = (card.ObjectId, card.Area.Id, card.Incarnation);
    }
}
