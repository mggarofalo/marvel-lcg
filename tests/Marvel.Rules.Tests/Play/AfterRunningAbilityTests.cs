using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class AfterRunningAbilityTests
{
    [Fact]
    [Rule("rr:player-elimination.5")]
    public void BoardWorkWaitsBehindEveryFrameTheOutermostRunningAbilityOwns()
    {
        // rr:player-elimination.5: "If a player is eliminated partway through the resolution
        // of an ability, resolve the entire ability." Work the elimination requires follows
        // the ability's frame and everything its outermost invocation still owns.
        var agenda = new Agenda();
        agenda.Add(Step(1));
        agenda.Add(Step(9));
        var occurrence = agenda.Occurrence!;
        _ = agenda.Executions.BeginBatch(occurrence);
        agenda.Then(Step(2));
        agenda.Then(Step(3));
        _ = agenda.Executions.Begin(occurrence, new(7, AbilityType.ForcedResponse, 0));

        agenda.AfterRunningAbility(Step(4));

        Assert.Equal([1, 2, 3, 4, 9], agenda.Outstanding.Select(step => step.Number));
    }

    [Fact]
    [Rule("rr:player-elimination.5")]
    public void BoardWorkOutsideAnyAbilityRunsAtOnce()
    {
        var agenda = new Agenda();
        agenda.Add(Step(1));
        agenda.Add(Step(9));

        agenda.AfterRunningAbility(Step(4));

        Assert.Equal([4, 1, 9], agenda.Outstanding.Select(step => step.Number));
    }

    private static PhaseStep Step(int number) =>
        new(new LifecycleOperation(LifecycleKind.CardPlayed), 1, number);
}
