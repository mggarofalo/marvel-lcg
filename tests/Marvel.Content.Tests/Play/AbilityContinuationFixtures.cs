using System.Collections.Immutable;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Content.Tests.Play;

internal static class AbilityContinuationFixtures
{
    internal static PhaseStep Pending(World world, Card source) =>
        world.Agenda.Outstanding.First(step => step.Subject == source.ObjectId
            && step.Operation is AbilityContinuationAgendaOperation);

    internal static PhaseStep Step(Card source, params AbilityStructuralFrame[] frames) => new(
        new AbilityContinuationAgendaOperation(AbilityContinuationStage.Resume)
        {
            Continuation = new AbilityContinuationState(
                new(new(source.FaceId, AbilityType.Action, 0),
                    new(source.ObjectId, source.Area.Id, source.Incarnation),
                    new Occurrence(1, [Steps.TurnAction]), Steps.TurnAction, 0, -1),
                new([.. frames], 0, 0, false, false, true, false),
                new(ImmutableDictionary<string, long>.Empty, [], null, [], source.InstanceState, false))
            { Reason = AbilityResumeReason.EffectProcedure },
        }, 1, 2, Subject: source.ObjectId, Seat: 0);
}
