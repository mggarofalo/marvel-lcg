using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Moves threat using removal permissions, scheme defeat, and placement windows.</summary>
public static class ThreatMovement
{
    /// <summary>Schedule an exact move; the request's identities are never retargeted.</summary>
    public static ThreatMoveReceipt Schedule(World world, ThreatMoveRequest request,
        PhaseStep? continuation = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegative(request.Amount);
        var receipt = new ThreatMoveReceipt();
        var current = world.Agenda.Current;
        var steps = new List<PhaseStep>
        {
            new(new ThreatMovementOperation(request, receipt), current?.Round ?? 0,
                current?.Number ?? 0, Seat: request.Player),
        };
        if (continuation is { } resume) steps.Add(resume);
        if (current is not null) world.Agenda.Now(steps);
        else foreach (var step in steps) world.Agenda.Add(step);
        return receipt;
    }

    internal static void Apply(World world, ICardFacts facts,
        ThreatMovementOperation operation, List<GameEvent> events)
    {
        var request = operation.Request;
        if (operation.PlacementPending)
        {
            Place(world, operation);
            return;
        }
        // rr:move.1-.2: "cannot move to its same (current) placement";
        // "If there is no valid source or destination ... the move cannot be made."
        if (!CanMove(world, request)) return;
        var from = request.From!;
        // rr:move.6-.7: moved threat is "removed from that scheme" and
        // "placed on that scheme", respectively.
        // Engine choice: resolve the removal half first. rr:move.6 calls it
        // "removed"; rr:side-scheme.2 makes zero threat cause defeat, whose
        // interrupt must finish before leaving play (rr:when-defeated-abilities.2.1).
        // Schedule after this owner before removal can insert nested defeat work.
        var current = world.Agenda.Current;
        world.Agenda.Then(new(new ThreatMovementOperation(request, operation.Receipt)
            { PlacementPending = true }, current?.Round ?? 0, current?.Number ?? 0,
            Seat: request.Player));
        var removal = Threat.RemoveWithOutcome(world, facts, world.ThreatAbilities,
            new(from, request.Amount, request.Trigger, "Move_Threat")
            { ResponsiblePlayer = request.Player }, events);
        operation.Receipt.Removed = removal.Removed;
    }

    private static void Place(World world, ThreatMovementOperation operation)
    {
        var request = operation.Request;
        if (operation.Receipt.Removed == 0 || request.To is not { } to
            || !LiveScheme(world, to, request.ToIncarnation)) return;
        var completion = Threat.Schedule(world, new([to], request.Source,
            operation.Receipt.Removed, ThreatCause.CardAbility, request.Trigger, request.Player)
            { Movement = operation.Receipt });
        operation.Receipt.Assignment(completion);
    }

    private static bool CanMove(World world, ThreatMoveRequest request) =>
        request.From is { } from && request.To is { } to
        && !ReferenceEquals(from, to)
        && LiveScheme(world, from, request.FromIncarnation)
        && LiveScheme(world, to, request.ToIncarnation);

    private static bool LiveScheme(World world, Card card, int incarnation) =>
        card.Incarnation == incarnation &&
        card.ObjectId >= 0 && card.ObjectId < world.Cards.Count
        && ReferenceEquals(world.Cards[card.ObjectId], card)
        && card.Area.Type is DeckType.MainSchemesArea or DeckType.SideSchemesArea;
}
