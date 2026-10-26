using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Holds all direct damage recipients behind replacement and placement barriers.</summary>
internal sealed class SimultaneousDirectDamageResolution
{
    private readonly World world;
    private readonly ICardFacts facts;
    private readonly SimultaneousDirectRecipient[] recipients;
    private int replacementIndex;
    private int settlementIndex;
    private bool placed;
    private SimultaneousCharacterDefeats? defeats;
    internal SimultaneousDirectDamageResolution(World world, ICardFacts facts,
        IReadOnlyList<DamageRequest> requests, AbilityExecution execution, AbilityExecution? completion)
    {
        this.world = world;
        this.facts = facts;
        Execution = execution;
        Result = new(completion ?? execution);
        recipients = [.. requests.Select(request => new SimultaneousDirectRecipient(
            request with { RecordDefeatOn = request.RecordDefeatOn ?? execution.Occurrence }))];
    }
    internal AbilityExecution Execution { get; }
    internal SimultaneousDirectDamageResult Result { get; }
    internal void Continue(List<GameEvent> events)
    {
        world.Agenda.Executions.RequireActive(Execution);
        while (replacementIndex < recipients.Length)
        {
            if (!recipients[replacementIndex].Replace(world, events)) { Suspend(events); return; }
            replacementIndex++;
        }
        if (!placed)
        {
            // ruling:3eb442853617fe14: "all damage is dealt simultaneously";
            // drones remain in play when Ultron III's damage eligibility is fixed.
            // All step-three prevention is also evaluated before any step-five placement.
            foreach (var recipient in recipients) recipient.Prepare(world, facts, events);
            foreach (var recipient in recipients) recipient.Place(world, facts, events);
            placed = true;
        }
        defeats ??= new(world, facts, recipients);
        if (!defeats.Continue(events)) { Suspend(events); return; }
        while (settlementIndex < recipients.Length)
        {
            if (!recipients[settlementIndex].Settle(world, facts, Execution, events)) { Suspend(events); return; }
            settlementIndex++;
        }
        Result.Publish(recipients.Select(recipient => recipient.Result!));
        Execution.Finish(events);
    }
    private void Suspend(List<GameEvent> events)
    {
        world.Agenda.ThenContinuation(new PhaseStep(new SimultaneousDirectDamageOperation(this),
            world.Agenda.Current?.Round ?? 0, 5, Plan: true), Execution.Occurrence);
        Execution.Yield(events);
    }
    internal static void Resume(World world, PhaseStep step, List<GameEvent> events)
    {
        var resolution = step.Require<SimultaneousDirectDamageOperation>().Resolution;
        world.Agenda.Executions.RequireContinuation(step);
        world.Agenda.Executions.Resume(resolution.Execution);
        resolution.Continue(events);
    }
}
