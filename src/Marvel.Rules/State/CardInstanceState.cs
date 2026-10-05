namespace Marvel.Rules.State;

/// <summary>Mutable quantities belonging to one copy of a physical card.</summary>
public sealed class CardInstanceState
{
    private readonly Dictionary<string, long> tokens = new(StringComparer.Ordinal);

    /// <summary>Sustained damage on this copy.</summary>
    public long Damage { get; private set; }
    /// <summary>Whether this copy is ready.</summary>
    public bool Ready { get; private set; } = true;
    /// <summary>Token quantities on this copy.</summary>
    public IReadOnlyDictionary<string, long> Tokens => tokens;

    /// <summary>Temporary characteristics assigned to this copy by an effect.</summary>
    public EffectiveCardProfile? Profile { get; internal set; }

    internal CardInstanceState() { }

    /// <summary>Restores explicitly captured last-known quantities.</summary>
    public static CardInstanceState FromFacts(
        long damage, bool ready, IReadOnlyDictionary<string, long> quantities)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(damage);
        var state = new CardInstanceState { Damage = damage, Ready = ready };
        foreach (var (kind, count) in quantities)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            state.tokens.Add(kind, count);
        }
        return state;
    }

    internal void PlaceTokens(string kind, long count)
    {
        ArgumentNullException.ThrowIfNull(kind);
        tokens[kind] = Math.Max(0, tokens.GetValueOrDefault(kind) + count);
    }

    internal void TakeDamage(long amount) => Damage = Math.Max(0, Damage + amount);
    internal void Exhaust() => Ready = false;
    internal void Refresh() => Ready = true;

    internal CardInstanceState NewCopy()
    {
        var copy = new CardInstanceState();
        // Registered zero-valued pools are part of the digest contract. Their
        // registration survives, but none of the old copy's quantities do.
        foreach (string kind in tokens.Keys) copy.tokens.Add(kind, 0);
        return copy;
    }
}
