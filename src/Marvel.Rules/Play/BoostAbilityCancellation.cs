using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Cancellation of one physical boost card's ability, independently of icons.</summary>
public sealed class BoostAbilityCancellation
{
    internal BoostAbilityCancellation(World world, Card card, int activation)
    { Game = world; Card = card; Incarnation = card.Incarnation; Activation = activation; }
    private World Game { get; }
    private Card Card { get; }
    private int Incarnation { get; }
    private int Activation { get; }
    internal Occurrence? Occurrence { get; set; }
    internal bool Cancelled { get; private set; }
    internal bool Started { get; set; }
    /// <summary>Whether the exact unresolved star remains cancellable.</summary>
    public bool CanCancel(World world) => ReferenceEquals(Game, world) && !Cancelled && !Started
        && world.Activation?.Id == Activation && Card.Incarnation == Incarnation
        && Card.Area.Type == DeckType.BoostingArea && world.Facts.HasBoostAbility(Card.FaceId)
        && Occurrence is { } occurrence && world.Agenda.IsOutstanding(occurrence);
    /// <summary>Cancel only from an executing ability in this exact faceup window.</summary>
    public bool Cancel(World world)
    {
        if (!CanCancel(world)) throw new InvalidOperationException("the boost ability cancellation occurrence is stale");
        if (world.Agenda.Executions.Active is not { } active || !ReferenceEquals(active.Occurrence, Occurrence))
            throw new InvalidOperationException("only the active faceup ability may cancel this boost ability");
        Cancelled = true;
        return true;
    }
}
