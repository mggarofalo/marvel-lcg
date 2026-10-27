using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Game-owned captures for Kang's delayed, phase-ending departures.</summary>
/// <remarks>Prepared nonpublic capability; printed defeat admission is a separate connection.</remarks>
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
        if (defeated.FaceId is not ("11002" or "11003" or "11004" or "11005"))
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

    internal void Preflight(World world, PhaseStep owner)
    {
        var ready = Ready(owner);
        if (ready.Length == 0) return;
        if (ready.Length != 1)
            throw new RulesNotImplementedException("simultaneous Kang phase-end departures require ordering");
        RequireGroup(ready[0]);
        string[] conditions = ready[0].Boundary == PhaseTransitionKind.EndPlayerPhase
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
        ? pending.Where(item => item.Round == owner.Round && item.Boundary == phase.Kind).ToArray() : [];

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
