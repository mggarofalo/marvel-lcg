using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Game-owned captures for Kang's delayed, phase-ending departures.</summary>
/// <remarks>Nonpublic capability; complete scenario admission remains closed.</remarks>
public sealed class KangPhaseTransitions
{
    private readonly List<KangDeparture> pending = [];

    internal KangElimination Elimination { get; } = new();

    internal void PlayerDeparted(World world, GameArea source, int player)
    {
        for (int index = 0; index < pending.Count; index++)
            if (ReferenceEquals(pending[index].Source, source))
                pending[index] = pending[index] with
                {
                    // Only the engine-eliminated member leaves the validated capture.
                    Group = Array.AsReadOnly(pending[index].Group.Where(area => area != PlayArea.Of(player)).ToArray()),
                };
        var scheme = world.Cards.SingleOrDefault(card => KangLocalCompletion.IsLocal(card)
            && card.Area.Type == DeckType.MainSchemesArea
            && ReferenceEquals(Places.GameAreaOf(world, card), source));
        if (scheme is not null && !source.PlayAreas.Any(area => area.IsPlayers))
            MainSchemeCompletion.Schedule(world, scheme);
    }

    /// <summary>A detached snapshot of departures which have not reached their boundary.</summary>
    public IReadOnlyList<KangDeparture> Pending => pending.ToArray();

    /// <summary>Retains an actual stage-II Kang's incarnation and deterministic whole group.</summary>
    public KangDeparture CaptureJoin(World world, Card defeated, int round, PhaseTransitionKind boundary)
    {
        if (!ReferenceEquals(world.Agenda.KangTransitions, this)
            || !world.Cards.Contains(defeated))
            throw new ArgumentException("the departure must belong to this game");
        if (boundary is not (PhaseTransitionKind.EndPlayerPhase or PhaseTransitionKind.EndVillainPhase))
            throw new ArgumentException("a Kang departure requires a phase-ending boundary");
        if (!IsSecondStage(defeated))
            throw new ArgumentException("this departure requires a stage-II Kang");
        GameArea source = Places.GameAreaOf(world, defeated)
            ?? throw new ArgumentException("the departing Kang has no local game area");
        if (pending.Any(item => item.Defeated.Card == defeated.ObjectId
            && item.Defeated.Incarnation == defeated.Incarnation))
            throw new InvalidOperationException("this Kang departure is already pending");
        // pack:mc11:game-areas moves the combined group. Ordering the captured
        // play areas by seat is an engine choice, independent of HashSet order.
        var departure = new KangDeparture(OccurrenceCard.Capture(defeated, world.Facts), source,
            Array.AsReadOnly(source.PlayAreas.OrderBy(area => area.Player).ToArray()), round, boundary);
        pending.Add(departure);
        return departure;
    }

    /// <summary>Whether a face uses Kang II's scenario departure policy.</summary>
    public static bool IsSecondStage(Card card) => card.FaceId is
        "11002" or "11003" or "11004" or "11005" or "11035" or "11036" or "11037" or "11038";

    /// <summary>Resolves the actual own When Defeated removal and retains its next phase ending.</summary>
    public void CaptureDefeat(World world, Card card, Defeated? defeated, List<Marvel.Rules.Events.GameEvent> events)
    {
        if (defeated is null || defeated.Card != card.ObjectId || defeated.Incarnation != card.Incarnation
            || !IsSecondStage(card) || !DeckTypes.IsInPlay(card.Area.Type))
            throw new RulesNotImplementedException("Kang II capture requires its exact own defeat");
        int index = card.FaceId switch
        {
            "11002" or "11035" => 0, "11003" or "11036" => 1,
            "11004" or "11037" => 2, _ => 3,
        };
        var source = Places.GameAreaOf(world, card);
        var scheme = world.Cards.SingleOrDefault(candidate => candidate.FaceId == $"{11009 + index}b"
            && candidate.Area.Type == DeckType.MainSchemesArea
            && ReferenceEquals(Places.GameAreaOf(world, candidate), source))
            ?? throw new RulesNotImplementedException("Kang II defeat requires its matching local stage 3B");
        // 11002-11005/11035-11038: remove the named scheme, then join at phase end.
        // Engine choice: bind to the next actual ending, including defeats during
        // phase-end interrupts, rather than inferring phase from another villain.
        var capture = CaptureJoin(world, card, -1, PhaseTransitionKind.EndPlayerPhase);
        pending[pending.IndexOf(capture)] = capture with { NextPhase = true };
        KangFirstSplit.Remove(world, scheme, card.FaceId, events);
    }

    internal void FinalizeDefeat(World world, Card card, string trigger, List<Marvel.Rules.Events.GameEvent> events)
    {
        if (!pending.Any(item => item.Defeated.Card == card.ObjectId && item.Defeated.Incarnation == card.Incarnation))
            throw new RulesNotImplementedException("Kang II defeat requires its authored delayed join ability");
        // pack:mc11:rules-clarifications: attachments/statuses do not transfer to Kang III.
        KangFirstSplit.Remove(world, card, trigger, events);
    }

    internal void Preflight(World world, PhaseStep owner)
    {
        var ready = Ready(owner);
        if (ready.Length == 0) return;
        if (ready.Select(item => item.Source).Distinct().Count() != ready.Length)
            throw new RulesNotImplementedException("Kang departures require distinct local source areas");
        foreach (var departure in ready) PreflightDeparture(world, departure);
    }

    /// <summary>Whether a captured departure resolves at this phase ending.</summary>
    internal bool IsBoundary(PhaseStep owner) => Ready(owner).Length > 0;

    internal static void PreflightDeparture(World world, KangDeparture departure)
    {
        RequireParticipatingGroup(world, departure);
        if (departure.CompletedScheme is { } completed)
        {
            var scheme = world.Cards[completed.Card];
            var villain = world.Cards[departure.Defeated.Card];
            if (scheme.Incarnation != completed.Incarnation || scheme.Area.Type != DeckType.MainSchemesArea
                || villain.Incarnation != departure.Defeated.Incarnation || villain.Area.Type != DeckType.VillainArea)
                throw new RulesNotImplementedException("the completed Kang stage or villain departed before phase end");
        }
        KangCentral.Preflight(world, departure);
    }

    private static void RequireParticipatingGroup(World world, KangDeparture departure)
    {
        if (world.Seats.Any(seat => seat.Eliminated && world.GameAreaOf(PlayArea.Of(seat.Index)) is not null))
            throw new RulesNotImplementedException("Kang eliminated players must leave through player elimination");
        RequireGroup(departure);
    }

    internal static void RequireGroup(KangDeparture departure)
    {
        if (departure.Group.Count != departure.Source.PlayAreas.Count
            || departure.Group.Any(area => !departure.Source.Contains(area)))
            throw new RulesNotImplementedException("the captured Kang group changed before its departure resolved");
    }

    internal static void RequireGroups(IEnumerable<KangDeparture> departures)
    {
        foreach (var departure in departures) RequireGroup(departure);
    }

    private KangDeparture[] Ready(PhaseStep owner) => owner.Operation is PhaseTransitionOperation phase
        && phase.Kind is PhaseTransitionKind.EndPlayerPhase or PhaseTransitionKind.EndVillainPhase
        ? pending.Where(item => item.NextPhase || item.Round == owner.Round && item.Boundary == phase.Kind).ToArray() : [];

    internal void Begin(World world, PhaseStep owner, List<Marvel.Rules.Events.GameEvent> events)
    {
        var ready = Ready(owner);
        if (ready.Length == 0) return;
        var origin = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("the Kang phase boundary has no occurrence");
        if (ready.Length == 1) KangDepartureOrderProcedure.BeginJoin(world, ready[0], [], owner, origin, events);
        else KangDepartureOrderProcedure.Schedule(world, ready, owner, origin);
        pending.RemoveAll(ready.Contains);
    }

    internal void CaptureCompletion(World world, Card villain, Card scheme)
    {
        var captured = CaptureJoin(world, villain, -1, PhaseTransitionKind.EndPlayerPhase);
        pending[pending.IndexOf(captured)] = captured with
        {
            NextPhase = true, CompletedScheme = OccurrenceCard.Capture(scheme, world.Facts),
        };
    }
}
