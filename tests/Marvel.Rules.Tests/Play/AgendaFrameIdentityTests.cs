using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class AgendaFrameIdentityTests
{
    [Fact]
    public void CancellingAnOccurrencePreservesItsSuspendedAbilityCleanup()
    {
        // Cancellation removes the occurrence's windows, not the accepted
        // ability's remaining work. Frame identity is an engine choice.
        var agenda = new Agenda();
        agenda.Add(new PhaseStep(new LifecycleOperation(LifecycleKind.CardPlayed), 1, 1));
        var owner = agenda.Current!.Value;
        var occurrence = agenda.Occurrence!;
        agenda.ContinueBeforeOwner(occurrence, owner, new PhaseStep(
            new AbilityContinuationAgendaOperation(AbilityContinuationStage.Resume), 1, 1));
        var cleanup = agenda.Current!.Value;

        agenda.Cancel(occurrence);

        Assert.False(agenda.IsOutstanding(owner, occurrence));
        Assert.True(agenda.IsOutstanding(cleanup, occurrence));
        Assert.Equal(1, agenda.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PayloadChangesDoNotConfuseAnOwnerWithItsIdenticalNestedContinuation(bool reorderChildren)
    {
        // Frame identity is an engine scheduling choice. Two continuations can
        // share an occurrence and payload while requiring independent progress.
        var agenda = new Agenda();
        var reveal = new Occurrence(19, [Steps.RevealEncounterCard], Subject: 4, Player: 0);
        var template = new PhaseStep(
            new AbilityContinuationAgendaOperation(AbilityContinuationStage.Choose)
            { Origin = reveal }, 1, 4, Subject: 4, Seat: 0);
        agenda.ThenContinuation(template, reveal);
        PhaseStep owner = agenda.Current!.Value;
        if (reorderChildren) agenda.BeforeOwnerAfterContinuations(reveal, owner, owner);
        else agenda.ContinueBeforeOwner(reveal, owner, owner);
        PhaseStep child = agenda.Current!.Value;

        agenda.MarkSurgeGained(4, reveal);
        Assert.True(agenda.Current.Value.Require<AbilityContinuationAgendaOperation>().SurgeGained);
        Assert.True(agenda.IsOutstanding(owner, reveal));
        Assert.True(agenda.IsOutstanding(child, reveal));
        Assert.True(agenda.Advance(owner, reveal));
        Assert.Equal(Stage.Apply, agenda.Stage);
        Assert.False(agenda.Advance(owner, reveal));
        Assert.False(agenda.IsOutstanding(owner, reveal));
        Assert.True(agenda.IsOutstanding(child, reveal));
        Assert.Equal(1, agenda.Count);

        Assert.True(agenda.Advance(child, reveal));
        Assert.False(agenda.Advance(child, reveal));
        Assert.False(agenda.IsBusy);
    }

    [Fact]
    public void RevealTailStaysBehindAnAlreadySuspendedChild()
    {
        var agenda = new Agenda();
        var reveal = new Occurrence(21, [Steps.RevealEncounterCard], Subject: 4, Player: 0);
        agenda.ThenContinuation(new PhaseStep(
            new AbilityContinuationAgendaOperation(AbilityContinuationStage.Choose)
            { Origin = reveal }, 1, 4, Subject: 4), reveal);
        PhaseStep owner = agenda.Current!.Value;
        agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndVillainPhase),
            1, 6, Subject: 99));
        agenda.ContinueBeforeOwner(reveal, owner, owner with { Subject = 5 });
        PhaseStep child = agenda.Current!.Value;
        agenda.Then(new PhaseStep(
            new RevealStageOperation(RevealStageKind.DiscardRevealedTreachery),
            1, 4, Subject: 4, Plan: true));

        agenda.BeforeResponses(owner, reveal);

        Assert.Equal([5, 4, 4, 99], agenda.Outstanding.Select(step => step.Subject));
        Assert.Equal(5, agenda.Current!.Value.Subject);
        Assert.True(agenda.Advance(child, reveal));
        Assert.False(agenda.Advance(child, reveal));
        Assert.Equal(Steps.DiscardRevealedTreachery, agenda.Current!.Value.What);
        Assert.True(agenda.IsOutstanding(owner, reveal));
    }

}
