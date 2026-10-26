namespace Marvel.Rules.Play;

/// <summary>Closed stages of one rules procedure.</summary>
public enum LifecycleKind
{
    /// <summary>CardPlayed procedure stage.</summary>
    CardPlayed,
    /// <summary>EventPlayed procedure stage.</summary>
    EventPlayed,
    /// <summary>CardEntersPlay procedure stage.</summary>
    CardEntersPlay,
    /// <summary>FormChanged procedure stage.</summary>
    FormChanged,
    /// <summary>Completed basic recovery.</summary>
    BasicRecoveryUsed,
    /// <summary>A paid event is about to resolve its effects.</summary>
    EventPlayInitiated,
}
