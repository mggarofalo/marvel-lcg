using Marvel.Godot;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ActivityVisualMeaningTests
{
    private static readonly WorldDescriptor World = new([], [], [], Outcome.Unfinished);

    [Fact]
    public void StaticSeamRetainsDistinctObjectsAndLocalNumbersRemainExact()
    {
        EventPresentation[] entries = [
            EventPresenter.Present(new FieldSet(1, "k_threat", 0, 1), World),
            EventPresenter.Present(new FieldSet(2, "k_threat", 0, 2), World),
            EventPresenter.Present(new FieldSet(2, "k_threat", 2, 3), World)];
        var settled = ActivityVisualMeaning.Settled(entries);
        Assert.Equal([entries[0], entries[2]], settled);
        Assert.Equal("threat 2 → 3", ActivityVisualMeaning.Result(settled[1]));
    }

    [Fact]
    public void AttackNamesRolesWithoutInventingDefenderDamage()
    {
        var entry = EventPresenter.Present(new AttackCompleted(1, 2, 3) { DamageDealt = 0,
            Subjects = new Dictionary<int, string> { [1] = "Rhino", [2] = "Spider-Man", [3] = "Mockingbird" } }, World);
        Assert.Equal("Rhino → Spider-Man · DEF Mockingbird", ActivityVisualMeaning.Participants(entry));
        Assert.Equal("damage dealt 0", ActivityVisualMeaning.Result(entry));
        Assert.DoesNotContain("Mockingbird", ActivityVisualMeaning.Result(entry));
    }

    [Fact]
    public void AbsentValueIsNeverShownAsZero()
    {
        Assert.Equal("HP 10 → absent", ActivityVisualMeaning.Quantity(new ActivityQuantity("health", 10, null)));
        Assert.Equal("HP 0", ActivityVisualMeaning.Quantity(new ActivityQuantity("health", null, 0)));
    }

    [Fact]
    public void BoostIconsAndStrengthAreAttributedToDifferentSubjects()
    {
        var entry = EventPresenter.Present(new BoostResolved(1, 2, 2, true, 5), World);
        Assert.Equal("boost 2", ActivityVisualMeaning.LocalResult(entry, entry.Activity!.Subjects[0]));
        Assert.Equal("ATK 5", ActivityVisualMeaning.LocalResult(entry, entry.Activity.Subjects[1]));
    }

    [Fact]
    public void CancellationDoesNotLabelItsSourceAsAnAttacker()
    {
        var entry = EventPresenter.Present(new WhenRevealedCanceled(2, 1), World);
        Assert.Equal("SOURCE", ActivityVisualMeaning.Role(entry, entry.Activity!.Subjects[0]));
        Assert.Equal("AFFECTED", ActivityVisualMeaning.Role(entry, entry.Activity.Subjects[1]));
    }

    [Fact]
    public void RemovedStatusHasDifferentMeaningFromAnAppliedStatus()
    {
        var subject = new ActivitySubject(1, "Tough", ActivitySubjectRole.Subject);
        var applied = new EventPresentation("", "", [1], EventMotionKind.Status)
        { Activity = new EventActivityDescriptor([subject], []) { StatusChange = ActivityStatusChange.Applied } };
        var removed = applied with { Activity = applied.Activity with { StatusChange = ActivityStatusChange.Removed } };
        Assert.Equal("+ Tough", ActivityVisualMeaning.LocalResult(applied, subject));
        Assert.Equal("− Tough", ActivityVisualMeaning.LocalResult(removed, subject));
        Assert.Equal("STATUS REMOVED", ActivityVisualMeaning.Title(removed));
    }
}
