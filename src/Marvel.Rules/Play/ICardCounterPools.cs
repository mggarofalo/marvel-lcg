using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Reads the card-defined counter pool without requiring ability execution.</summary>
public interface ICardCounterPools
{
    /// <summary>The counter pool this card enters play with, or null.</summary>
    /// <remarks>
    /// The default derives the Uses keyword for rules-only callers. The card
    /// interpreter overrides it with authored DSL data, which also represents
    /// ordinary "enters play with" text that does not discard at zero.
    /// </remarks>
    CardCounterPool? CounterPool(World world, Card card)
    {
        var (count, type) = Reveal.Uses(world.Facts.Attributes(card.FaceId));
        return count > 0
            ? new CardCounterPool(type, checked((int)count), Uses: true)
            : null;
    }
}
