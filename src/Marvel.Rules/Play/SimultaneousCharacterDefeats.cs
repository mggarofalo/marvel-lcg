using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Holds simultaneous character damage at each defeat stage until all its work completes.</summary>
internal sealed class SimultaneousCharacterDefeats(World world, ICardFacts facts,
    IReadOnlyList<ISimultaneousDefeatRecipient> recipients)
{
    private AbilityExecutionResult? wouldDefeat;
    private AbilityExecutionResult? whenDefeated;
    private bool admitted;
    private int departure;

    internal bool Continue(List<GameEvent> events)
    {
        // rr:and: effects "resolve simultaneously"; rr:damage.step.6-.8 order
        // "would be defeated", "is defeated", then "Discarding". Applying those
        // stages to one simultaneous effect keeps every recipient at the same stage.
        wouldDefeat ??= BeginWouldDefeat(events);
        if (world.IsOver) return true;
        if (!wouldDefeat.IsComplete) return false;
        if (!admitted) AdmitDefeats(events);
        if (world.IsOver) return true;
        if (whenDefeated is { IsComplete: false }) return false;
        while (!world.IsOver && departure < recipients.Count)
        {
            // Depart may yield to mandatory villain-selection work. Retain the
            // index before yielding so the same defeated incarnation cannot depart twice.
            if (recipients[departure++].Depart(world, facts, events)) return false;
        }
        return true;
    }

    private void AdmitDefeats(List<GameEvent> events)
    {
        foreach (var recipient in recipients)
            if (recipient.IsLethal(world, facts)) recipient.RecordDefeat(world);
        var defeats = recipients.Where(recipient => recipient.Defeated is not null)
            .Select(recipient => recipient.Defeated!).ToArray();
        if (defeats.Length > 0)
        {
            string trigger = recipients.First(recipient => recipient.Defeated is not null).Request.Trigger;
            whenDefeated = defeats.Length == 1
                ? world.DamageAbilities.ExecuteDefeat(world, world.Cards[defeats[0].Card], defeats[0], trigger)
                : world.DamageAbilities.ExecuteDefeats(world, defeats, trigger);
            ActivityBatches.Append(events, whenDefeated.Events);
        }
        admitted = true;
    }

    private AbilityExecutionResult BeginWouldDefeat(List<GameEvent> events)
    {
        var sources = recipients.Where(recipient => recipient.IsLethal(world, facts))
            .Select(recipient => new DefeatWindowSource(new Occurrence(0, [Steps.CardWouldBeDefeated],
                Subject: recipient.Request.Target.ObjectId, Player: recipient.Request.Target.Owner),
                recipient.Request.Target.Incarnation)).ToArray();
        var origin = new Occurrence(0, [Steps.CardWouldBeDefeated]);
        var window = new DefeatWindow(origin) { Sources = sources };
        return world.Agenda.Executions.RunBatch(origin, entry =>
        {
            foreach (var source in sources)
            {
                source.Origin.SharedDefeatWindow = window;
                var request = recipients.Single(recipient => recipient.Request.Target.ObjectId == source.Origin.Subject).Request;
                var result = world.DamageAbilities.ExecuteWouldDefeat(world, request.Target,
                    new DefeatCause(request.Source.ObjectId, request.Trigger, request.Verb, request.By, request.RecordDefeatOn),
                    source.Origin);
                ActivityBatches.Append(events, result.Events);
            }
            if (sources.Any(source => AbilityWindow.Tiers(world.WindowAbilities.Waiting(world,
                source.Origin, WindowKind.Interrupt), WindowKind.Interrupt, source.Origin).Count > 0))
            {
                var first = recipients.Single(recipient => recipient.Request.Target.ObjectId == sources[0].Origin.Subject).Request;
                var resume = new PhaseStep(new DefeatAbilityOperation(DefeatAbilityKind.ResumeWouldBeDefeated,
                    new DefeatCause(first.Source.ObjectId, first.Trigger, first.Verb, first.By, first.RecordDefeatOn),
                    window), world.Agenda.Current?.Round ?? 0,
                    6, Subject: sources[0].Origin.Subject, Plan: true);
                DefeatProcedure.ScheduleProcedureChoice(world, resume);
            }
            return entry.Finish([]);
        });
    }
}
