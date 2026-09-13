namespace Marvel.View;

/// <summary>Live public values of a readable card, separated from printed face facts.</summary>
public sealed record CardStateDescriptor(
    bool Ready,
    long Damage,
    long? Threat,
    IReadOnlyDictionary<string, long> Counters,
    IReadOnlyDictionary<string, long> Values,
    IReadOnlyList<string> Statuses);
