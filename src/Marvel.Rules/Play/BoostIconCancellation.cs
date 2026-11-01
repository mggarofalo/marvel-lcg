using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>The remaining numeric icons on one faceup boost card.</summary>
public sealed class BoostIconCancellation
{
    internal BoostIconCancellation(World world, Card card, int activation, ICardFacts facts)
    { Game = world; Card = card; Incarnation = card.Incarnation; Activation = activation; Facts = facts; Count = new(world, card); }
    private World Game { get; }
    private ICardFacts Facts { get; }
    private Card Card { get; }
    private int Incarnation { get; }
    private int Activation { get; }
    internal Occurrence? Occurrence { get; set; }
    /// <summary>The count of this boost card's icons, which count interrupts may change.</summary>
    public BoostIconCount Count { get; }
    // Ruling 937d6ff4f970a1da counts icons changed by earlier interrupts.
    // Ruling 1ea208998735ef4b leaves icons added by the later Boost ability intact.
    // Keep actual cancellation separate from the card's live modified total.
    /// <summary>Current modified icons still available after earlier cancellation.</summary>
    public long Remaining => Contribution(Game, Facts);
    private long Cancelled { get; set; }
    // rr:amplify-icon: "add one additional boost icon to that card for each amplify
    // icon in play." Engine choice: when a count interrupt counts another card's
    // icons instead, that card is not a boost card and gains no amplify icon. A
    // boost card that lost its icons gains none either (rr:loses.2).
    internal long Contribution(World world, ICardFacts facts) => Math.Max(0,
        Count.Value + (Count.Replaced || BoostIconCounting.Lost(world, Card) ? 0
            : MainScheme.Amplify(world, facts, ActivatingEnemy(world))) - Cancelled);
    private static Card? ActivatingEnemy(World world) =>
        world.Activation is { Enemy: >= 0 } activation ? world.Cards[activation.Enemy] : null;
    /// <summary>Whether this exact boost window remains live.</summary>
    public bool IsCurrent(World world) => ReferenceEquals(Game, world)
        && world.Activation?.Id == Activation && Card.Incarnation == Incarnation
        && Card.Area.Type == DeckType.BoostingArea && Occurrence is { } occurrence
        && world.Agenda.IsOutstanding(occurrence);
    /// <summary>Cancel the current numeric icons and return exactly the amount canceled.</summary>
    public long Cancel(World world)
    {
        if (!IsCurrent(world)) throw new InvalidOperationException("the boost cancellation occurrence is stale");
        if (world.Agenda.Executions.Active is not { } active || !ReferenceEquals(active.Occurrence, Occurrence))
            throw new InvalidOperationException("only the active faceup ability may cancel boost icons");
        long amount = Remaining;
        Cancelled += amount;
        return amount;
    }
}
