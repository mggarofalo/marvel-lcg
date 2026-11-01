using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One count of the boost icons on one encounter card.</summary>
/// <remarks>
/// <para>
/// Ruling 6962a015e8597900 (Scarlet Witch rulebook): "Boost icons are counted
/// whenever a game step or card ability determines the exact number of boost
/// icons on an encounter card." This receipt is that count. Interrupts to it can
/// replace the card whose icons are counted and adjust the number for this count.
/// </para>
/// <para>
/// The count reads the live board until <see cref="Freeze"/> records the final
/// number, so a constant that leaves play before the count completes no longer
/// adds to it (ruling 805b3f9b199ca68b).
/// </para>
/// </remarks>
public sealed class BoostIconCount
{
    /// <summary>Begin a count of the boost icons on one card.</summary>
    public BoostIconCount(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        Game = world;
        Original = card;
        Counted = card;
    }

    private World Game { get; }

    /// <summary>The card whose icons this count was asked for.</summary>
    public Card Original { get; }

    /// <summary>The card whose icons are counted instead, or the original card.</summary>
    public Card Counted { get; private set; }

    /// <summary>Whether an interrupt replaced the counted card.</summary>
    public bool Replaced => !ReferenceEquals(Counted, Original);

    /// <summary>The increase or decrease applied for this count.</summary>
    public long Adjustment { get; private set; }

    /// <summary>The final number, once the count has completed.</summary>
    public long? Final { get; private set; }

    /// <summary>The number this count currently reads.</summary>
    /// <remarks>
    /// Engine choice: an adjustment never makes a count negative. A card cannot
    /// carry fewer than zero icons, and no printed decrease says otherwise. A card
    /// that lost its boost icons cannot regain them (rr:loses.2), so no adjustment applies.
    /// </remarks>
    public long Value => Final ?? (BoostIconCounting.Lost(Game, Counted) ? 0
        : Math.Max(0, BoostIconCounting.Icons(Game, Counted) + Adjustment));

    /// <summary>Whether this count belongs to the game and has not completed.</summary>
    public bool IsCurrent(World world) => ReferenceEquals(Game, world) && Final is null;

    /// <summary>Count the icons on another card instead.</summary>
    public void Replace(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(card);
        RequireCurrent(world);
        Counted = card;
    }

    /// <summary>Increase or decrease the number for this count.</summary>
    public void Adjust(World world, long amount)
    {
        RequireCurrent(world);
        Adjustment = checked(Adjustment + amount);
    }

    /// <summary>Record the number the completed count reached.</summary>
    public void Freeze() => Final ??= Value;

    private void RequireCurrent(World world)
    {
        if (!IsCurrent(world)) throw new InvalidOperationException("the boost icon count is no longer open");
    }
}
