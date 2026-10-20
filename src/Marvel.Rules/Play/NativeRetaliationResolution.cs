using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Retains native response identity while retaliation damage suspends.</summary>
internal sealed class NativeRetaliationResolution(AbilityExecution execution, DamageResult damage)
{
    internal AbilityExecution Execution => execution;

    internal static AbilityExecutionResult Begin(World world, AbilityExecution invocation)
    {
        var context = invocation.Occurrence.Retaliation
            ?? throw new InvalidOperationException("Retaliate has no completed attack");
        var recipient = context.Recipients.Single(item => item.Attack == invocation.Ability.TriggeringAttack);
        var character = RetaliationEligibility.Character(world, recipient);
        var attacker = RetaliationEligibility.Attacker(world, context);
        if (character is null || attacker is null) return invocation.Finish([]);
        var events = new List<GameEvent>();
        var result = DamagePlacement.Resolve(world, world.Facts,
            new(character, attacker, RetaliationEligibility.Amount(world, character),
                context.Trigger, "Retaliate"), events);
        var resolution = new NativeRetaliationResolution(invocation, result);
        if (result.IsComplete) return resolution.Complete(events);
        world.Agenda.ThenContinuation(new PhaseStep(new NativeRetaliationOperation(resolution),
            world.Agenda.Current?.Round ?? 0, 5, Plan: true), invocation.Occurrence);
        return invocation.Yield(events);
    }

    private AbilityExecutionResult Complete(List<GameEvent> events)
    {
        if (!damage.IsComplete) throw new InvalidOperationException("Retaliation damage remains pending");
        if (damage.Taken > 0) execution.Occurrence.Resolution.Resolve(execution.Ability);
        return execution.Finish(events);
    }

    internal static void Resume(World world, PhaseStep step, List<GameEvent> events)
    {
        var resolution = step.Require<NativeRetaliationOperation>().Resolution;
        world.Agenda.Executions.RequireContinuation(step);
        world.Agenda.Executions.Resume(resolution.Execution);
        _ = resolution.Complete(events);
    }
}
