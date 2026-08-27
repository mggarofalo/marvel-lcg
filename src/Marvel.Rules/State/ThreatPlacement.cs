namespace Marvel.Rules.State;

/// <summary>Why threat is about to be placed on a scheme.</summary>
public enum ThreatCause
{
    /// <summary>Step one of the villain phase.</summary>
    VillainPhase,

    /// <summary>Step three of an enemy's scheme activation.</summary>
    EnemyScheme,

    /// <summary>The incite keyword.</summary>
    Incite,

    /// <summary>A card ability.</summary>
    CardAbility,
}

/// <summary>
/// One imminent assignment of threat, preserved across its interrupt window.
/// </summary>
/// <remarks>
/// <c>rr:prevent.2</c>: "When threat is prevented, reduce the amount of threat
/// being assigned before it is placed on the scheme." Assigned and remaining
/// are therefore separate facts. The scheme, source and cause are frozen with
/// the amount so an answer cannot resume against a different board reading.
/// </remarks>
public sealed class ThreatPlacement
{
    /// <summary>Create one imminent placement.</summary>
    public ThreatPlacement(
        int scheme, int source, long amount, ThreatCause cause, string trigger, int player = -1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(trigger);

        Scheme = scheme;
        Source = source;
        Assigned = amount;
        Remaining = amount;
        Cause = cause;
        Trigger = trigger;
        Player = player;
    }

    /// <summary>The scheme receiving the threat, by object id.</summary>
    public int Scheme { get; }

    /// <summary>The card that caused the placement, or -1 for a game step.</summary>
    public int Source { get; }

    /// <summary>The amount assigned when the occurrence became imminent.</summary>
    public long Assigned { get; }

    /// <summary>The amount left after prevention.</summary>
    public long Remaining { get; private set; }

    /// <summary>What kind of game effect caused the placement.</summary>
    public ThreatCause Cause { get; }

    /// <summary>The event-stream trigger.</summary>
    public string Trigger { get; }

    /// <summary>The player the placement concerns, or -1.</summary>
    public int Player { get; }

    /// <summary>Whether another effect replaced the placement.</summary>
    public bool Replaced { get; private set; }

    /// <summary>Prevent up to <paramref name="amount"/> of the assigned threat.</summary>
    public void Prevent(long amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Remaining = Math.Max(0, Remaining - amount);
    }

    /// <summary>Replace the imminent placement with another effect.</summary>
    public void Replace()
    {
        Remaining = 0;
        Replaced = true;
    }
}
