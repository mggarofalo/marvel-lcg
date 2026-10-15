using Marvel.Rules.Timing;

namespace Marvel.Rules.State;

/// <summary>A card-labelled power awaiting status replacement and target validation.</summary>
/// <remarks>
/// The source owns the ability; the performer is the character making the power.
/// A missing performer asks initiation to establish that character and apply status
/// replacement. The continuation belongs to the card interpreter, which receives
/// it intact when the scheduled power resolves.
/// </remarks>
public sealed record CardPowerInvocation(
    int Player, Card Source, Card Target, long Amount, string Trigger)
{
    /// <summary>A performer already established by the containing labelled ability.</summary>
    public Card? Performer { get; init; }

    /// <summary>The interpreter's suspended work, or null for a fixed-amount power.</summary>
    public CardPowerContinuation? Continuation { get; init; }

    /// <summary>The containing occurrence whose response may own this nested power.</summary>
    public Occurrence? Origin { get; init; }

    /// <summary>Whether the containing reveal has already gained Surge.</summary>
    public bool SurgeGained { get; init; }
}
