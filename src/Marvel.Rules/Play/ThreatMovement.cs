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
        // rr:move.1-.2: "cannot move to its same (current) placement";
        // "If there is no valid source or destination ... the move cannot be made."
        if (request.From is not { } from || request.To is not { } to
            || ReferenceEquals(from, to) || !LiveScheme(world, from) || !LiveScheme(world, to)) return;
        // rr:move.6-.7: moved threat is "removed from that scheme" and
        // "placed on that scheme", respectively.
        var removal = Threat.RemoveWithOutcome(world, facts, world.ThreatAbilities,
            new(from, request.Amount, request.Trigger, "Move_Threat")
            { ResponsiblePlayer = request.Player }, events);
        operation.Receipt.Removed = removal.Removed;
        if (removal.Removed == 0) return;
        var completion = Threat.Schedule(world, new([to], request.Source,
            removal.Removed, ThreatCause.CardAbility, request.Trigger, request.Player)
            { Movement = operation.Receipt });
        operation.Receipt.Assignment(completion);
    }

    private static bool LiveScheme(World world, Card card) =>
        card.ObjectId >= 0 && card.ObjectId < world.Cards.Count
        && ReferenceEquals(world.Cards[card.ObjectId], card)
        && card.Area.Type is DeckType.MainSchemesArea or DeckType.SideSchemesArea;
}
