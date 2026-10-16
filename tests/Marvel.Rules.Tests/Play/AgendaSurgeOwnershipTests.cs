using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class AgendaSurgeOwnershipTests
{
    [Fact]
    public void SurgeGrantBelongsToOneRevealEvenWhenThePhysicalCardIsShared()
    {
        // Engine ownership choice: nested reveals can use the same physical
        // source and numeric occurrence id. Only the exact reveal owns its grant.
        var agenda = new Agenda();
        var first = new Occurrence(0, [Steps.CardRevealed], Subject: 4, Player: 0);
        var second = new Occurrence(0, [Steps.CardRevealed], Subject: 4, Player: 0);
        agenda.ThenContinuation(new PhaseStep(
            new AbilityContinuationAgendaOperation(AbilityContinuationStage.Choose)
            { Origin = first }, 1, 4, Subject: 4, Seat: 0), first);
        agenda.ThenContinuation(new PhaseStep(
            new AbilityContinuationAgendaOperation(AbilityContinuationStage.Choose)
            { Origin = second }, 1, 4, Subject: 4, Seat: 0), second);

        agenda.MarkSurgeGained(4, first);

        var frames = agenda.Outstanding.Select(step => step.Require<AbilityContinuationAgendaOperation>()).ToList();
        Assert.True(Assert.Single(frames, frame => ReferenceEquals(frame.Origin, first)).SurgeGained);
        Assert.False(Assert.Single(frames, frame => ReferenceEquals(frame.Origin, second)).SurgeGained);
        agenda.MarkSurgeGained(4, second);
        Assert.All(agenda.Outstanding, step => Assert.True(step.Require<AbilityContinuationAgendaOperation>().SurgeGained));
    }
}
