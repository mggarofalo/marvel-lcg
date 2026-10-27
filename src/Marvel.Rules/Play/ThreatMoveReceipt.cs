using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>The committed removal and exact destination placement of one threat move.</summary>
public sealed class ThreatMoveReceipt
{
    private Occurrence? placement;
    private EffectCompletion? completion;
    private long stored;

    /// <summary>The amount actually removed from the source.</summary>
    public long Removed { get; internal set; }
    /// <summary>The amount received by the exact destination, including tokens stored on a support.</summary>
    public long Placed => completion?.Applied ?? stored;
    /// <summary>Whether a destination forced response has begun for this exact placement.</summary>
    /// <remarks>This observes triggering, including an ability whose effects do not apply.
    /// Callers wait for the containing procedure before using this to choose a subsequent effect.</remarks>
    public bool DestinationForcedResponseTriggered => placement is not null
        && placement.Resolutions.Any(entry => entry.Card == placement.Target
            && entry.Ability == AbilityType.ForcedResponse
            && (entry.TriggeringThreatId is null || entry.TriggeringThreatId == placement.Id));

    internal void Store(long amount) => stored = amount;
    internal void Assignment(EffectCompletion receipt) => completion = receipt;
    internal void Observe(Occurrence occurrence) => placement = occurrence;
}
