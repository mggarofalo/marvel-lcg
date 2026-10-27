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
            Place(world, operation, events);
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

    private static void Place(World world, ThreatMovementOperation operation, List<GameEvent> events)
    {
        var request = operation.Request;
        if (operation.Receipt.Removed == 0 || request.To is not { } to
            || !LiveDestination(world, request)) return;
        if (to.Area.Type == DeckType.SupportsArea)
        {
            StoreOnSupport(to, operation, events);
            return;
        }
        var completion = Threat.Schedule(world, new([to], request.Source,
            operation.Receipt.Removed, ThreatCause.CardAbility, request.Trigger, request.Player)
            { Movement = operation.Receipt });
        operation.Receipt.Assignment(completion);
    }

    private static bool CanMove(World world, ThreatMoveRequest request) =>
        request.From is { } from && request.To is { } to
        && !ReferenceEquals(from, to)
        && LiveScheme(world, from, request.FromIncarnation)
        && LiveDestination(world, request);

    /// <summary>The supported destination shapes: a scheme, or the resolving controller's own source support.</summary>
    /// <remarks>Engine boundary: arbitrary non-scheme destinations and support-to-scheme moves are unsupported.</remarks>
    public static bool IsSupportedDestination(World world, Card destination, Card? source, int player) =>
        destination.Area.Type is DeckType.MainSchemesArea or DeckType.SideSchemesArea
        || ReferenceEquals(destination, source) && destination.Area.Type == DeckType.SupportsArea
            && destination.Area.PlayArea.Player == player
            && world.Facts.Kind(destination.FaceId) == CardKind.Support;

    private static bool LiveDestination(World world, ThreatMoveRequest request) =>
        request.To is { } to && Live(world, to, request.ToIncarnation)
        && IsSupportedDestination(world, to, request.Source, request.Player);

    private static void StoreOnSupport(Card support, ThreatMovementOperation operation, List<GameEvent> events)
    {
        // rr:move.7: "If threat is moved to a scheme" it is considered placed there.
        // A support stores the moved tokens without a scheme placement occurrence.
        long before = support.Tokens.GetValueOrDefault("k_threat");
        long amount = operation.Receipt.Removed;
        support.PlaceTokens("k_threat", amount);
        events.Add(new FieldSet(support.ObjectId, "k_threat", before, before + amount)
        { Trigger = operation.Request.Trigger, Verb = "Move_Threat" });
        operation.Receipt.Store(amount);
    }

    private static bool LiveScheme(World world, Card card, int incarnation) =>
        Live(world, card, incarnation)
        && card.Area.Type is DeckType.MainSchemesArea or DeckType.SideSchemesArea;

    private static bool Live(World world, Card card, int incarnation) =>
        card.Incarnation == incarnation &&
        card.ObjectId >= 0 && card.ObjectId < world.Cards.Count
        && ReferenceEquals(world.Cards[card.ObjectId], card);
}
