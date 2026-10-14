using Marvel.Godot;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class AttackActivityPresentationTests
{
    [Fact]
    public void PendingAttackHasSourceAndTargetWithoutAnyPlayback()
    {
        var attack = Attack(1, "Rhino", 2, "Spider-Man");
        EventPresentation cue = Assert.IsType<EventPresentation>(AttackActivityPresentation.From(attack));
        Assert.Equal("Rhino → Spider-Man", cue.Summary);
        Assert.Equal(new[] { attack.Source, attack.Target }, cue.Activity!.Subjects);
        Assert.Empty(cue.Activity.Quantities);
        Assert.Null(AttackActivityPresentation.From(null));
    }

    [Fact]
    public void InterruptedAttackSurvivesAResponseWithNoAttackResult()
    {
        var attack = Attack(1, "Rhino", 2, "Spider-Man");
        var draw = new EventPresentation("Spider-Man drew 1 card", "", [2], EventMotionKind.HandGain);
        var cue = AttackActivityPresentation.DuringTransition(attack, attack, [draw]);
        Assert.Equal("Rhino → Spider-Man", cue!.Summary);
        Assert.Equal("Attack_Active", cue.Activity!.Operation);
    }

    [Fact]
    public void AcceptedDefenderWaitsForItsCausalBeat()
    {
        var previous = Attack(1, "Rhino", 2, "Spider-Man");
        var defended = Attack(1, "Rhino", 3, "Mockingbird");
        var completed = Introduction(defended);
        var cue = AttackActivityPresentation.DuringTransition(previous, null, [completed]);
        Assert.NotSame(completed, cue);
        Assert.Equal(2, cue!.Activity!.Subjects.Single(subject => subject.Role == ActivitySubjectRole.Target).Card);
        Assert.Null(AttackActivityPresentation.From(null));
    }

    [Fact]
    public void NextAttackDoesNotReplaceTheAttackWhoseConsequencesAreStillPresented()
    {
        var previous = Attack(1, "Rhino", 2, "Spider-Man");
        var next = Attack(4, "Hydra Mercenary", 2, "Spider-Man");
        var cue = AttackActivityPresentation.DuringTransition(previous, next, [Introduction(next)]);
        Assert.Equal("Rhino → Spider-Man", cue!.Summary);
        Assert.Equal("Hydra Mercenary → Spider-Man", AttackActivityPresentation.From(next)!.Summary);
    }

    [Fact]
    public void NewAttacksWaitForTheirOwnInitiationBeat()
    {
        var attack = Attack(1, "Rhino", 2, "Spider-Man");
        Assert.Null(AttackActivityPresentation.DuringTransition(null, attack, []));
        EventPresentation instant = Introduction(Attack(2, "Spider-Man", 1, "Rhino"));
        Assert.Null(AttackActivityPresentation.DuringTransition(null, null, [instant]));
        Assert.Null(AttackActivityPresentation.DuringTransition(null, null, []));
    }

    private static ActiveAttackDescriptor Attack(int source, string sourceName, int target, string targetName) =>
        new(new(source, sourceName, ActivitySubjectRole.Source), new(target, targetName, ActivitySubjectRole.Target));

    private static EventPresentation Introduction(ActiveAttackDescriptor attack)
    {
        EventPresentation cue = AttackActivityPresentation.From(attack)!;
        return cue with { Activity = cue.Activity! with { Operation = "Attack_Presentation" } };
    }
}
