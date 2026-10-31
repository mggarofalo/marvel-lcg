using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Places every selected share before any recipient's defeat or attack aftermath.</summary>
internal sealed class SimultaneousAttackDamageResolution
{
    private readonly World world;
    private readonly ICardFacts facts;
    private readonly SimultaneousAttackRecipient[] recipients;
    private int replacementIndex;
    private int settlementIndex;
    private bool placed;
    private SimultaneousCharacterDefeats? defeats;

    internal SimultaneousAttackDamageResolution(World world, ICardFacts facts,
        SimultaneousAttackDamageRequest request, AbilityExecution execution, AbilityExecution? completion)
    {
        this.world = world;
        this.facts = facts;
        Execution = execution;
        Result = new(completion ?? execution);
        recipients = [.. request.Targets.Select((target, index) => new SimultaneousAttackRecipient(
            new(request.Attacker, request.Source, target, request.AmountFor(index), request.Trigger, request.Verb, request.Retaliate)
            {
                Basic = request.Basic,
                GrantedOverkill = request.GrantedOverkill,
                BasicRecipient = request.Basic ? Attacked.Capture(target, facts) : null,
            },
            new DamageRequest(request.Source, target, request.AmountFor(index), request.Trigger, request.Verb,
                LabeledAbilities.IdentityPlayer(world, request.Attacker)) { RecordDefeatOn = execution.Occurrence, FromAttack = true },
            OccurrenceCard.Capture(request.Attacker, facts)))];
    }

    internal AbilityExecution Execution { get; }
    internal SimultaneousAttackDamageResult Result { get; }

    internal void Continue(List<GameEvent> events)
    {
        world.Agenda.Executions.RequireActive(Execution);
        if (!PrepareAndPlace(events)) return;
        defeats ??= new(world, facts, recipients);
        if (!defeats.Continue(events)) { Suspend(events); return; }
        if (world.IsOver) { Suspend(events); return; }
        while (settlementIndex < recipients.Length)
        {
            if (!recipients[settlementIndex].Settle(world, facts, Execution, events)) { Suspend(events); return; }
            if (world.IsOver) { Suspend(events); return; }
            settlementIndex++;
        }
        // rr:attack-player-ability-type.5.1: each surviving attacked enemy
        // retaliates after the attack resolves, not between recipient defeats.
        foreach (var recipient in recipients)
        {
            recipient.Retaliate(world, facts, events);
            if (world.IsOver) { Suspend(events); return; }
        }
        Result.Publish(recipients.Select(recipient => recipient.Result!));
        Execution.Finish(events);
    }

    private bool PrepareAndPlace(List<GameEvent> events)
    {
        while (replacementIndex < recipients.Length)
        {
            if (!recipients[replacementIndex].Replace(world, Execution, events)) { Suspend(events); return false; }
            replacementIndex++;
        }
        if (!placed)
        {
            // faq:03006: Shield Toss damage is dealt simultaneously. No defeat
            // or attack aftermath may change another recipient's placement.
            foreach (var recipient in recipients) recipient.Prepare(world, facts, events);
            foreach (var recipient in recipients) recipient.Place(world, facts, events);
            foreach (var recipient in recipients) recipient.RecordPlacement(world, Execution, events);
            placed = true;
        }
        return true;
    }

    private void Suspend(List<GameEvent> events)
    {
        world.Agenda.ThenContinuation(new PhaseStep(new SimultaneousAttackDamageOperation(this),
            world.Agenda.Current?.Round ?? 0, 5, Plan: true), Execution.Occurrence);
        Execution.Yield(events);
    }

    internal static void Resume(World world, PhaseStep step, List<GameEvent> events)
    {
        var resolution = step.Require<SimultaneousAttackDamageOperation>().Resolution;
        world.Agenda.Executions.RequireContinuation(step);
        world.Agenda.Executions.Resume(resolution.Execution);
        resolution.Continue(events);
    }
}
