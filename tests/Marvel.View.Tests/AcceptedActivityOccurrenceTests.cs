using Xunit;

namespace Marvel.View.Tests;

public sealed class AcceptedActivityOccurrenceTests
{
    [Fact]
    public void AttackCostAndDamageAreOneIntentionWithOrderedInspectableChildren()
    {
        var exhausted = new EventPresentation("Spider-Man became exhausted.", "Basic attack", [1], EventMotionKind.State);
        var damage = new EventPresentation("Rhino took 2 damage.", "Basic attack", [9], EventMotionKind.Damage);
        var accepted = new DecisionReceiptContext("Spider-Man: Attack Rhino.", [1, 9])
        { ActionVerb = "Attack", OccurrenceTitle = "Spider-Man attacked Rhino" };
        EventPresentation occurrence = Assert.Single(AcceptedActivityOccurrence.From([exhausted, damage], accepted));
        Assert.Equal("Spider-Man: Attack Rhino · Rhino took 2 damage.", occurrence.Summary);
        Assert.Equal([exhausted, damage], occurrence.Details);
        Assert.Equal("Spider-Man attacked Rhino", occurrence.CueSummary);
        Assert.Equal("Rhino took 2 damage.", occurrence.ResultSummary);
    }

    [Fact]
    public void AttackDamageNeverAppliesItsQuantityToTheActorWhoPaidExhaustion()
    {
        var actor = new ActivitySubject(1, "Spider-Man", ActivitySubjectRole.Subject);
        var target = new ActivitySubject(9, "Rhino", ActivitySubjectRole.Subject);
        var cost = new EventPresentation("Spider-Man exhausted.", "", [1], EventMotionKind.State)
        { Activity = new EventActivityDescriptor([actor], [new("exhausted", 0, 1)]) };
        var damage = new EventPresentation("Rhino HP 14 → 12", "", [9], EventMotionKind.Damage)
        { Activity = new EventActivityDescriptor([target], [new("health", 14, 12)]) };
        var accepted = new DecisionReceiptContext("Spider-Man attacked Rhino", [1, 9]) { ActionVerb = "Attack" };
        EventPresentation result = Assert.Single(AcceptedActivityOccurrence.From([cost, damage], accepted));
        Assert.Same(damage.Activity, result.Activity);
        Assert.Equal(target, Assert.Single(result.Activity!.Subjects));
        Assert.Equal(actor, Assert.Single(result.Details[0].Activity!.Subjects));
    }

    [Theory]
    [InlineData("phase")]
    [InlineData("forced")]
    [InlineData("suspended")]
    [InlineData("draw")]
    public void SemanticBoundariesRemainSeparateFromThePrecedingCommitment(string boundary)
    {
        var change = new EventPresentation("State changed.", "", [], EventMotionKind.State);
        var separate = change with { Activity = new EventActivityDescriptor([], [])
        {
            PhaseBoundary = boundary == "phase", Batch = boundary == "draw" ? 3 : null,
            Operation = boundary switch
            { "forced" => "Ability_Resolved", "suspended" => "Ability_Initiated", _ => "" },
        } };
        var accepted = new DecisionReceiptContext("Play a card.", []) { ActionVerb = "Play" };
        Assert.Equal([change, separate], AcceptedActivityOccurrence.From([change, separate], accepted));
    }
}
