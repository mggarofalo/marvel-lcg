namespace Marvel.Rules.Timing;

/// <summary>Freezes public participant facts when a combat or threat occurrence begins.</summary>
internal static class OccurrenceCapture
{
    internal static Occurrence Attack(
        int id,
        IReadOnlyList<string> conditions,
        State.World world,
        State.ICardFacts facts,
        int actor,
        int target,
        int player = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        return new Occurrence(
            id,
            conditions,
            Player: player,
            Actor: actor,
            Target: target,
            ActorFacts: OccurrenceCard.Capture(world.Cards[actor], facts),
            TargetFacts: target >= 0 ? OccurrenceCard.Capture(world.Cards[target], facts) : null)
        { DamageFromAttack = conditions.Contains(Play.Steps.DamageWouldBeDealt, StringComparer.Ordinal) };
    }

    internal static Occurrence Thwart(
        int id,
        IReadOnlyList<string> conditions,
        State.World world,
        State.ICardFacts facts,
        int actor,
        int scheme,
        int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        return new Occurrence(
            id,
            conditions,
            Subject: scheme,
            Player: player,
            Actor: actor,
            Target: scheme,
            ActorFacts: OccurrenceCard.Capture(world.Cards[actor], facts),
            TargetFacts: scheme >= 0 ? OccurrenceCard.Capture(world.Cards[scheme], facts) : null);
    }

    internal static Occurrence Threat(
        int id, IReadOnlyList<string> conditions, State.World world,
        State.ICardFacts facts, State.ThreatPlacement placement, int? subject = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(placement);

        OccurrenceCard? source = placement.Source >= 0
            ? OccurrenceCard.Capture(world.Cards[placement.Source], facts)
            : null;
        OccurrenceCard? target = placement.Scheme >= 0
            ? OccurrenceCard.Capture(world.Cards[placement.Scheme], facts)
            : null;

        return new Occurrence(
            id,
            conditions,
            Subject: subject ?? placement.Scheme,
            Player: placement.Player,
            Actor: placement.Source,
            Target: placement.Scheme,
            ActorFacts: source,
            TargetFacts: target,
            Threat: placement);
    }
}
