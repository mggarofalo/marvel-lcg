using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>The remaining numeric icons on one faceup boost card.</summary>
public sealed class BoostIconCancellation
{
    internal BoostIconCancellation(World world, Card card, int activation, ICardFacts facts)
    { Game = world; Card = card; Incarnation = card.Incarnation; Activation = activation; Facts = facts; }
    private World Game { get; }
    private ICardFacts Facts { get; }
    private Card Card { get; }
    private int Incarnation { get; }
    private int Activation { get; }
    internal Occurrence? Occurrence { get; set; }
    // Ruling 937d6ff4f970a1da counts icons changed by earlier interrupts.
    // Ruling 1ea208998735ef4b leaves icons added by the later Boost ability intact.
    // Keep actual cancellation separate from the card's live modified total.
    /// <summary>Current modified icons still available after earlier cancellation.</summary>
    public long Remaining => Contribution(Game, Facts);
    private long Cancelled { get; set; }
    internal long Contribution(World world, ICardFacts facts) => Math.Max(0,
        (Characteristics.IsLost(world, Card, "boost_const") ? 0
            : StateFields.Modified(world, Card, "boost_const", facts, world.Players)
                + MainScheme.Amplify(world, facts)) - Cancelled);
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
