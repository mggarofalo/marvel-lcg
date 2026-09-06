namespace Marvel.Rules.Play;

/// <summary>
/// Damage amounts after replacement and tough, before live timing and placement.
/// </summary>
/// <remarks>
/// This value uses only explicit amounts and status presence. Live procedures
/// own callbacks, status removal, damage placement and defeat; projected readers
/// supply their overlay's status instead of mutating a board.
/// </remarks>
public readonly record struct DamageAssignment
{
    private DamageAssignment(long dealt, long taken, bool spendsTough)
    {
        Dealt = dealt;
        Taken = taken;
        SpendsTough = spendsTough;
    }

    /// <summary>The amount fixed by damage step 1, before prevention.</summary>
    public long Dealt { get; }

    /// <summary>The amount remaining for placement after prevention.</summary>
    public long Taken { get; }

    /// <summary>Whether this instance discards exactly one tough status card.</summary>
    public bool SpendsTough { get; }

    /// <summary>Resolve the numeric and status decision at damage step 2.</summary>
    public static DamageAssignment AfterReplacement(long amount, bool hasTough)
    {
        // rr:tough.2.2: "If the damage is reduced to 0, the hero does not lose
        // their tough status card." Replaced-away damage likewise never takes
        // a status; step 1 precedes step 2 in rr:damage.
        if (amount <= 0)
        {
            return default;
        }
        // rr:tough.2: "prevent all of that damage and discard a tough status
        // card"; .2.1: "only one tough status card each time it would take damage".
        return new DamageAssignment(amount, hasTough ? 0 : amount, hasTough);
    }

    /// <summary>Apply the amount returned by step-3 damage-taking abilities.</summary>
    public DamageAssignment AfterPrevention(long amount)
    {
        // rr:damage.3.2: "the amount of damage dealt is not modified" when the
        // amount taken is modified. Fully replaced or tough-prevented damage
        // does not reach step 3 and cannot be revived by a later amount.
        return Taken <= 0 ? this : new DamageAssignment(Dealt, Math.Max(0, amount), false);
    }
}
