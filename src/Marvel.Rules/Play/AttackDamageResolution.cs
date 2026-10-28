using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Retains attack roles while each damage replacement and departure finishes.</summary>
internal sealed class AttackDamageResolution
{
    private readonly World world;
    private readonly ICardFacts facts;
    private readonly AttackDamageRequest request;
    private readonly bool spillOnly;
    private readonly int incarnation;
    private readonly Timing.OccurrenceCard attacker;
    private long remaining;
    private CardKind kind;
    private int controller = -1;
    private readonly AttackDamagePolicy policy;
    private readonly int priorDefeats;
    private DamageReplacement? replacement;
    private Damage.PlacedDamage? placed;
    private DirectDamagePrevention? spillPrevention;
    private Damage.Outcome outcome;
    private AttackDamageResult? spill;
    private bool aftermath;
    private bool pierced;
    private EffectCompletion? excessThreat;

    internal AttackDamageResolution(World world, ICardFacts facts, AttackDamageRequest request,
        AbilityExecution execution, bool spillOnly, AbilityExecution? completion = null)
    {
        this.world = world;
        this.facts = facts;
        this.request = request;
        this.spillOnly = spillOnly;
        attacker = Timing.OccurrenceCard.Capture(request.Attacker, facts);
        replacement = request.Replacement;
        Execution = execution;
        Result = new(completion ?? execution);
        priorDefeats = execution.Occurrence.Defeats.Count;
        incarnation = request.Prepared?.Incarnation ?? request.Replacement?.TargetIncarnation ?? request.Target.Incarnation;
        policy = request.Prepared?.Policy ?? AttackDamagePolicy.Capture(world, request, spillOnly);
    }

    internal AttackDamageResult Result { get; }
    internal AbilityExecution Execution { get; }

    internal void Continue(List<GameEvent> events)
    {
        world.Agenda.Executions.RequireActive(Execution);
        if (placed is null && !Place(events)) { Suspend(events); return; }
        if (world.IsOver) { Suspend(events); return; }
        CompleteDefeatAndBeginSpill(events);
        if (world.IsOver) { Suspend(events); return; }
        if (spill is { IsComplete: false }) { Suspend(events); return; }
        if (!CompleteExcessThreat(events)) return;
        Finish(events);
    }

    private void Finish(List<GameEvent> events)
    {
        if (policy.Retaliate && request.Target.Incarnation == incarnation)
            DamageAttacks.Retaliate(world, facts, request.Target, request.Attacker, request.Trigger, events);
        Result.Publish(placed!, spill?.Amount ?? 0, outcome == Damage.Outcome.Defeated ? Excess : 0);
        Execution.Finish(events);
    }

    private bool CompleteExcessThreat(List<GameEvent> events)
    {
        if (excessThreat is null && policy.ExcessThreat is { } route
            && outcome == Damage.Outcome.Defeated && Excess > 0)
        {
            excessThreat = route.Schedule(world, Excess, request.Trigger);
            Suspend(events);
            return false;
        }
        if (excessThreat is { IsComplete: false }) { Suspend(events); return false; }
        return true;
    }

    private void CompleteDefeatAndBeginSpill(List<GameEvent> events)
    {
        if (outcome == Damage.Outcome.Suspended)
            outcome = Execution.Occurrence.Defeats.Skip(priorDefeats).Any(defeat =>
                defeat.Card == request.Target.ObjectId && defeat.Incarnation == incarnation)
                ? Damage.Outcome.Defeated : Damage.Outcome.NotDefeated;
        if (aftermath) return;
        aftermath = true;
        BeginSpill(events);
    }

    private bool Place(List<GameEvent> events)
    {
        if (request.Prepared is { } prepared) return PlacePrepared(prepared, events);
        PrepareReplacement(events);
        if (!replacement!.IsComplete) return false;
        if (request.Target.Incarnation != incarnation || !DeckTypes.IsInPlay(request.Target.Area.Type))
        {
            placed = new(request.Target, 0, 0);
            return true;
        }
        // Replacement abilities may change health or control before placement.
        remaining = Math.Max(0, DamagePlacement.Health(world, facts, request.Target) - request.Target.Damage);
        kind = EffectiveCards.Kind(request.Target, facts);
        controller = kind == CardKind.Ally ? request.Target.Area.PlayArea.Player : -1;
        var assignment = new DamageRequest(request.Source, request.Target, replacement.ResolvedAmount,
            request.Trigger, request.Verb)
        { PreventAllDamage = request.EnemyAttack?.PreventAllDamage == true, FromAttack = true };
        if (spillOnly)
        {
            // rr:overkill.2: excess is "damage from an attack" and has its own recipient.
            spillPrevention ??= DirectDamagePrevention.Begin(world, assignment, replacement,
                DamagePlacement.PrepareAfterReplacement(world, facts, assignment, events));
            if (!spillPrevention.IsComplete) return false;
            placed = spillPrevention.Placement;
        }
        else placed = DamagePlacement.PrepareAfterReplacement(world, facts, assignment, events);
        DamagePlacement.ApplyPlaced(world, facts, placed, request.Trigger, request.Verb, events);
        return Settle(events);
    }

    private bool PlacePrepared(PreparedAttackDamage prepared, List<GameEvent> events)
    {
        remaining = prepared.RemainingHealth;
        kind = prepared.Kind;
        controller = prepared.Controller;
        placed = prepared.Placement;
        if (prepared.SettledOutcome is { } settled)
        {
            outcome = settled;
            return true;
        }
        return Settle(events);
    }

    private bool Settle(List<GameEvent> events)
    {
        if (request.Target.Incarnation != incarnation || !DeckTypes.IsInPlay(request.Target.Area.Type))
            return true;
        AttackDamageAccounting.RecordDamage(world, request, Execution, attacker, placed!, spillOnly, events);
        outcome = DamagePlacement.FinishPlaced(world, facts, request.Source, placed!, request.Trigger,
            request.Verb, events, LabeledAbilities.IdentityPlayer(world, request.Attacker), Execution.Occurrence);
        return outcome != Damage.Outcome.Suspended;
    }

    private void PrepareReplacement(List<GameEvent> events)
    {
        if (!pierced)
        {
            pierced = true;
            if (!spillOnly && AttackKeywords.Has(world, request.Attacker, request.Source, Keywords.Piercing, request.Basic))
                DamageAttacks.DiscardPiercedTough(world, request.Target, request.Amount, request.Trigger, events);
        }
        replacement ??= DamagePlacement.Replace(world,
            new(request.Source, request.Target, request.Amount, request.Trigger, request.Verb,
                LabeledAbilities.IdentityPlayer(world, request.Attacker)) { RecordDefeatOn = Execution.Occurrence, FromAttack = true }, events);
    }

    private long Excess => Math.Max(0, placed!.Taken - remaining);

    private void BeginSpill(List<GameEvent> events)
    {
        // rr:overkill.1: excess goes to the ally controller's identity or the villain.
        if (policy.ExcessThreat is not null || !policy.Overkill
            || outcome != Damage.Outcome.Defeated || Excess <= 0) return;
        Card? onto = kind switch
        {
            CardKind.Ally when controller >= 0 => world.Seats[controller].IdentityCard,
            CardKind.Minion => EncounterRouting.Villain(world),
            _ => null,
        };
        if (onto is not null)
            spill = DamageAttacks.Begin(world, facts,
                request with { Target = onto, Amount = Excess, Verb = Keywords.Overkill, Retaliate = false,
                    Replacement = null, EnemyAttack = null, Prepared = null },
                events, spillOnly: true);
    }

    private void Suspend(List<GameEvent> events)
    {
        world.Agenda.ThenContinuation(new PhaseStep(new AttackDamageResolutionOperation(this),
            world.Agenda.Current?.Round ?? 0, 5, Plan: true), Execution.Occurrence);
        Execution.Yield(events);
    }

    internal static void Resume(World world, PhaseStep step, List<GameEvent> events)
    {
        var resolution = step.Require<AttackDamageResolutionOperation>().Resolution;
        world.Agenda.Executions.RequireContinuation(step);
        world.Agenda.Executions.Resume(resolution.Execution);
        resolution.Continue(events);
    }
}
