namespace Marvel.Rules.Play;

/// <summary>The committed amounts of one scheduled effect, in assignment order.</summary>
public sealed class EffectCompletion
{
    private readonly long?[] applied;

    /// <summary>Create a receipt before any assignment begins.</summary>
    public EffectCompletion(long amount, int parts)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentOutOfRangeException.ThrowIfNegative(parts);
        Requested = checked(amount * parts);
        applied = new long?[parts];
    }

    /// <summary>The total amount instructed by the effect.</summary>
    public long Requested { get; }
    /// <summary>Whether every scheduled assignment has finished.</summary>
    public bool IsComplete => applied.All(value => value.HasValue);
    /// <summary>The actual total, available only after all assignments finish.</summary>
    public long Applied => IsComplete ? applied.Sum(value => value!.Value)
        : throw new InvalidOperationException("the effect has unfinished assignments");

    /// <summary>Commit one assignment exactly once, including a prevented amount of zero.</summary>
    internal void Complete(int part, long amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (applied[part].HasValue)
            throw new InvalidOperationException("the assignment already completed");
        applied[part] = amount;
    }
}
