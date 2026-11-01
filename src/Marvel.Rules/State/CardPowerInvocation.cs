using System.Collections.Immutable;
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
    int Player, Card Source, Card? Target, long Amount, string Trigger)
{
    /// <summary>Explicit recipients validated together before a multi-target interpreter continuation starts.</summary>
    /// <remarks>The default retains the ordinary single-target contract. The continuation owns damage resolution.</remarks>
    public ImmutableArray<Card> Targets { get; init; }

    /// <summary>Whether <see cref="Targets"/> is a divided allocation that may name one recipient once per share.</summary>
    public bool Allocation { get; init; }

    /// <summary>A performer already established by the containing labelled ability.</summary>
    public Card? Performer { get; init; }

    /// <summary>The interpreter's suspended work, or null for a fixed-amount power.</summary>
    public CardPowerContinuation? Continuation { get; init; }

    /// <summary>The containing occurrence whose response may own this nested power.</summary>
    public Occurrence? Origin { get; init; }

    /// <summary>Whether the containing reveal has already gained Surge.</summary>
    public bool SurgeGained { get; init; }

    /// <summary>Interpreter-owned evidence that the power reads the performer's THW.</summary>
    public bool UsesThwartStat { get; init; }

    /// <summary>
    /// Whether the containing labelled ability initiated this power before earlier
    /// steps of its effect resolved, so its target was legal then and is not checked again.
    /// </summary>
    /// <remarks>
    /// Ruling 6fd4652a06512d03: "Guard prevents you from initiating attacks, but does
    /// not prevent you from dealing damage", and a stunned status placed after the
    /// attack was initiated does not stop it; ruling ca713a04a3ff5491 applies the same
    /// to a thwart, confused and patrol.
    /// </remarks>
    public bool Initiated { get; init; }
}
