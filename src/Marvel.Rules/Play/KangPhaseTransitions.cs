using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Game-owned captures for Kang's delayed, phase-ending departures.</summary>
/// <remarks>Nonpublic capability; complete scenario admission remains closed.</remarks>
public sealed class KangPhaseTransitions
{
    private readonly List<KangDeparture> pending = [];

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
        if (ready.Length != 1)
            throw new RulesNotImplementedException("simultaneous Kang phase-end departures require ordering");
        RequireGroup(ready[0]);
        string[] conditions = owner.Require<PhaseTransitionOperation>().Kind == PhaseTransitionKind.EndPlayerPhase
            ? [PhaseEnd.PlayerPhaseEnds] : [PhaseEnd.VillainPhaseEnds, PhaseEnd.RoundEnds];
        // The prepared controller has no accepted ordering policy for co-due
        // delayed work. Refuse before PhaseEnd expires or consumes any effect.
        if (world.Effects.HasDue(conditions))
            throw new RulesNotImplementedException("Kang phase-end departure and competing delayed effects require ordering");
    }

    internal static void RequireGroup(KangDeparture departure)
    {
        if (departure.Group.Count != departure.Source.PlayAreas.Count
            || departure.Group.Any(area => !departure.Source.Contains(area)))
            throw new RulesNotImplementedException("the captured Kang group changed before its departure resolved");
    }

    private KangDeparture[] Ready(PhaseStep owner) => owner.Operation is PhaseTransitionOperation phase
        && phase.Kind is PhaseTransitionKind.EndPlayerPhase or PhaseTransitionKind.EndVillainPhase
        ? pending.Where(item => item.NextPhase || item.Round == owner.Round && item.Boundary == phase.Kind).ToArray() : [];

    internal void Begin(World world, PhaseStep owner)
    {
        var ready = Ready(owner);
        if (ready.Length == 0) return;
        if (ready.Length != 1)
            throw new RulesNotImplementedException("simultaneous Kang phase-end departures require ordering");
        var origin = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("the Kang phase boundary has no occurrence");
        world.Agenda.ContinueBeforeOwner(origin, owner, owner with
        {
            Operation = new KangJoinOperation(ready[0], owner, origin),
            Subject = ready[0].Defeated.Card,
        });
        pending.Remove(ready[0]);
    }
}
