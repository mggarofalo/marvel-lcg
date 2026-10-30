using Marvel.Rules.Timing;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One operation with its occurrence identity and scheduling context.</summary>
/// <remarks>Only the owning operation carries resumable procedure state.</remarks>
public readonly record struct PhaseStep(
    AgendaOperation Operation, int Round, int Number, int Index = 0,
    int Subject = -1, int Seat = -1, bool Plan = false)
{
    // Scheduler identity is an engine choice, independent of occurrence identity
    // and mutable procedure payloads. It is assigned only when work is enqueued.
    internal long FrameId { get; init; }
    internal object? FrameGame { get; init; }
    internal AbilityExecution? ExecutionOwner { get; init; }
    internal AbilityExecution? CompletionPrerequisite { get; init; }
    internal AbilityContinuationPermit? ContinuationPermit { get; init; }
    internal bool SameFrameAs(PhaseStep other) => FrameId != 0 && FrameId == other.FrameId && ReferenceEquals(FrameGame, other.FrameGame);

    /// <summary>The containing enemy activation, or -1 outside an activation.</summary>
    public int ActivationId { get; init; } = -1;
    /// <summary>The nearest causal enemy attack, retained after its completion.</summary>
    public EnemyAttackOrigin? AttackOrigin { get; init; }
    /// <summary>A dynamic occurrence identity, or null for round/number/index identity.</summary>
    public int? OccurrenceId { get; init; }
    /// <summary>The operation's timing vocabulary name.</summary>
    public string What => Operation.What;
    /// <summary>Requires the operation owned by the resolving procedure.</summary>
    public T Require<T>() where T : AgendaOperation => Operation as T
        ?? throw new InvalidOperationException($"{What} is not a {typeof(T).Name}");

    /// <summary>What is happening, as triggering conditions.</summary>
    /// <remarks>
    /// Usually one. The villain phase's ending is two — the phase ends and the
    /// round ends — and <c>rr:triggering-condition.2</c> is why they share one
    /// occurrence rather than getting two windows each.
    /// </remarks>
    public IReadOnlyList<string> Conditions => StepConditions.ConditionsOf(What);

    /// <summary>This step's occurrence, distinct from every other in the game.</summary>
    /// <remarks>
    /// <c>rr:triggering-condition.1</c> is per occurrence, so two threat
    /// placements in the same game must not share an id — the second would find
    /// every interrupt already spent.
    /// </remarks>
    public Occurrence OccurrenceOf(World world, ICardFacts facts)
    {
        int id = OccurrenceId ?? Moment.Id(Round, Number, Index);
        if (Operation is BoostFaceupOperation boost) return boost.Occurrence(id, world, Subject, Seat);
        if (Operation is HeroDefenseOperation defense) return defense.Occurrence(id, world);
        if (Operation is CardReadyOperation ready) return ready.Occurrence(id);

        if (Operation is EncounterEntryOperation entry)
            return new Occurrence(id, EncounterEntry.IsCurrent(world, entry.Entry)
                && EncounterEntry.WillEnter(world, world.Cards[Subject], entry.Entry.Player, entry.Entry.Placement)
                    ? [Steps.CardEntersPlay] : [], Subject, Seat);
        return What switch
        {
            EventPlayTiming.Initiated => new Occurrence(id, Conditions, Subject, Seat)
            { EventRemovesThreat = Require<LifecycleOperation>().EventRemovesThreat },
            Steps.RevealEncounterCard => new Occurrence(id, EncounterEntry.WillEnter(world, world.Cards[Subject],
                EncounterRevealExposure.RevealingPlayer(world, world.Cards[Subject], Seat))
                ? [Steps.CardRevealed, Steps.CardEntersPlay] : Conditions, Subject,
                EncounterRevealExposure.RevealingPlayer(world, world.Cards[Subject], Seat))
            { RevealOrigin = Require<RevealStageOperation>().Origin },
            Steps.PlaceThreat => VillainPhaseThreat(id, world, facts),
            Steps.SchemeThreat => SchemeThreat(id, world, facts),
            Steps.PlaceThreatEffect when Operation is ThreatPlacementOperation { Placement: { } placement } =>
                OccurrenceCapture.Threat(id, Conditions, world, facts, placement),
            Steps.PlaceThreatEffect => throw new RulesNotImplementedException(
                "a scheduled threat placement has no placement payload"),
            Steps.Attack => Occurrence.ForAttack(
                id,
                Conditions,
                world,
                facts,
                Subject,
                Require<EnemyAttackOperation>().Character >= 0
                    ? Require<EnemyAttackOperation>().Character : world.Seats[Seat].IdentityCard.ObjectId,
                Seat),
            Steps.CharacterAttacks when Require<CharacterAttackOperation>().Attack is { } attack =>
                Occurrence.ForAttack(
                    id,
                    BasicUse.AttackConditions(world, facts, attack, Conditions),
                    world,
                    facts,
                    attack.Attacker,
                    attack.Enemy,
                    attack.Player) with { BasicUse = BasicUse.ForAttack(world, facts, attack) },
            Steps.CharacterThwarts when Require<CharacterThwartOperation>().Thwart is { } thwart =>
                Occurrence.ForThwart(
                    id,
                    BasicUse.ThwartConditions(world, facts, thwart, Conditions),
                    world,
                    facts,
                    thwart.Thwarter,
                    thwart.Scheme,
                    thwart.Player) with { BasicUse = BasicUse.ForThwart(world, thwart) },
            Steps.PrepareIndirectAttackDamage => Occurrence.ForAttack(
                id,
                Conditions,
                world,
                facts,
                Require<IndirectDamageOperation>().Source,
                Subject,
                Seat),
            Steps.DealAttackDamage when world.Attack is { Indirect: true } attack =>
                Occurrence.ForAttack(
                    id, [], world, facts, attack.Enemy, attack.Target, attack.Player),
            Steps.DealAttackDamage when world.Attack is { } attack => Occurrence.ForAttack(
                id,
                Conditions,
                world,
                facts,
                attack.Enemy,
                attack.Target,
                attack.Player),
            Steps.EndAttack when world.Attack is { } attack => Occurrence.ForAttack(
                id,
                Conditions,
                world,
                facts,
                attack.Enemy,
                attack.Target,
                attack.Player) with { Retaliation = attack.Retaliation },
            _ => new Occurrence(id, Conditions, Subject, Seat),
        };
    }

    private Occurrence VillainPhaseThreat(int id, World world, ICardFacts facts)
    {
        if (world.TheCardIn(DeckType.MainSchemesArea) is not { } scheme)
        {
            return new Occurrence(id, Conditions);
        }

        long amount = StateFields.Modified(
                world, scheme, "escalation_threat", facts, world.Players)
            + MainScheme.Acceleration(world, facts);
        if (amount <= 0)
        {
            return new Occurrence(id, Conditions, Subject: scheme.ObjectId);
        }

        return OccurrenceCapture.Threat(
            id,
            Conditions,
            world,
            facts,
            new ThreatPlacement(
                scheme.ObjectId, scheme.ObjectId, amount, ThreatCause.VillainPhase,
                "villain phase, place threat"));
    }

    private Occurrence SchemeThreat(int id, World world, ICardFacts facts)
    {
        if (Subject < 0 || Subject >= world.Cards.Count)
        {
            return new Occurrence(id, []);
        }

        var enemy = world.Cards[Subject];
        if (world.ThreatAbilities.SchemeDestination(world, enemy) is not { } scheme)
            // The enemy still schemes when its area has no threat destination.
            // Keep its causal roles for scheme-end responses without a placement.
            return new Occurrence(id, [], Subject: enemy.ObjectId, Player: Seat,
                Actor: enemy.ObjectId, ActorFacts: OccurrenceCard.Capture(enemy, facts));
        long amount = StateFields.Modified(
            world, enemy, "scheme", facts, world.Players);
        if (amount <= 0)
        {
            return OccurrenceCapture.Threat(
                id,
                [Steps.SchemeEnds],
                world,
                facts,
                new ThreatPlacement(
                    scheme.ObjectId, enemy.ObjectId, 0, ThreatCause.EnemyScheme,
                    "scheme", Seat),
                subject: enemy.ObjectId);
        }

        return OccurrenceCapture.Threat(
            id,
            Conditions,
            world,
            facts,
            new ThreatPlacement(
                scheme.ObjectId, enemy.ObjectId, amount, ThreatCause.EnemyScheme,
                "scheme", Seat),
            subject: enemy.ObjectId);
    }

    /// <summary>An occurrence that needs no live attack roles, or null.</summary>
    public Occurrence? ScheduledOccurrence => Operation is DirectDamagePreventionOperation prevention
        ? prevention.Prevention.Occurrence : Operation is CompletedCardEffectOperation { Occurrence: { } response }
        ? response : Operation is EncounterEntryOperation or HeroDefenseOperation or BoostFaceupOperation or CardReadyOperation ? null : What is
        Steps.Attack or Steps.CharacterAttacks or Steps.CharacterThwarts or Steps.EndAttack
            or Steps.PlaceThreat or Steps.SchemeThreat or Steps.PlaceThreatEffect
            or Steps.PrepareIndirectAttackDamage
            or Steps.RevealEncounterCard
            ? null
            : new Occurrence(
                OccurrenceId ?? Moment.Id(Round, Number, Index), Conditions, Subject, Seat)
            {
                EventRemovesThreat = (Operation as LifecycleOperation)?.EventRemovesThreat,
                Entry = new OccurrenceEntry(
                    (Operation as LifecycleOperation)?.Payment ?? (Operation as PlayerAllyEntryOperation)?.PlayedPayment,
                    (Operation as LifecycleOperation)?.EntryPayment ?? (Operation as PlayerAllyEntryOperation)?.Payment,
                    Operation as PlayerAllyEntryOperation),
            };
}
