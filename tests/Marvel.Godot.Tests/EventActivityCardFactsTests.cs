using Marvel.Godot;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class EventActivityCardFactsTests
{
    // Synthetic display contracts isolate receipt consumption; these are not played-game fixtures.
    [Fact]
    public void ImpactUsesExactAuthorizedValueWithoutPredictingOtherConsequences()
    {
        BoardCardPresentation card = Card(2);
        var activity = new EventActivityDescriptor([new(2, "Rhino", ActivitySubjectRole.Subject)], [new("health", 14, 12)]);
        BoardCardPresentation shown = EventActivityCardFacts.Apply(card, activity);
        Assert.Equal("12/14", shown.Fields.Single(field => field.Name == "HEALTH").Value);
        Assert.Equal("READY", shown.Status);
        Assert.Equal("14/14", card.Fields.Single().Value);
        Assert.Same(card, EventActivityCardFacts.Apply(card, activity with { Subjects = [new(3, "Other", ActivitySubjectRole.Subject)] }));
        var hidden = card with { Concealed = true };
        Assert.Same(hidden, EventActivityCardFacts.Apply(hidden, activity));
    }

    [Fact]
    public void PhaseBatchRetainsOneBeatAndAllItsExactReadinessFacts()
    {
        EventPresentation[] details = [Ready(1), Ready(2)];
        var batch = new EventPresentation("Readied simultaneously", "", [1, 2], EventMotionKind.State)
        { Activity = new([], []) { Operation = "Ready", PhaseStep = true, Batch = 8 }, Details = details };
        Assert.Equal(details, EventActivityCardFacts.Facts(batch));
        var sequence = new EventActivitySequence();
        sequence.Start([batch], true);
        Assert.Single(sequence.Entries);
        Assert.InRange(sequence.Onset, .14, .18);
        Assert.False(sequence.Advance(.16));
        foreach (var detail in EventActivityCardFacts.Facts(batch))
        {
            var card = Card(detail.Anchors.Single()) with { Status = "EXHAUSTED" };
            Assert.Equal("READY", EventActivityCardFacts.Apply(card, detail.Activity!).Status);
        }
    }

    [Fact]
    public void ActiveAttackCompletionDoesNotIntroduceTheSameAttackAgain()
    {
        var attack = new ActiveAttackDescriptor(new(1, "Rhino", ActivitySubjectRole.Source), new(2, "Hero", ActivitySubjectRole.Target));
        var completed = new EventPresentation("Finished", "", [1, 2], EventMotionKind.Attack)
        { Activity = new([attack.Source, attack.Target], []) { Operation = "Attack_Completed" } };
        var world = new WorldDescriptor([], [], [], Marvel.Rules.Play.Outcome.Unfinished) { ActiveAttack = attack };
        Assert.Equal([completed], EventPlaybackPlan.From([completed], prior: world));
        Assert.Same(completed, AttackActivityPresentation.DuringTransition(attack, null, [completed]));
    }

    [Fact]
    public void StatusReceiptUpdatesOnlyItsExplicitHostAndRetainsItsFoldedMeaning()
    {
        var status = new EventPresentation("Stunned", "", [8, 2], EventMotionKind.Status)
        {
            Activity = new([new(8, "Stunned", ActivitySubjectRole.Subject), new(2, "Card", ActivitySubjectRole.Target)], [])
                { StatusChange = ActivityStatusChange.Applied },
            Details = [new("Attached", "", [8, 2], EventMotionKind.State)],
        };
        Assert.Same(status, EventActivityCardFacts.Facts(status).First());
        BoardCardPresentation card = EventActivityCardFacts.Apply(Card(2), status.Activity);
        Assert.Equal(["Stunned"], card.Statuses);
        Assert.Empty(EventActivityCardFacts.Apply(card, status.Activity with { StatusChange = ActivityStatusChange.Removed }).Statuses);
        Assert.Empty(EventActivityCardFacts.Apply(Card(3), status.Activity).Statuses);
    }

    [Fact]
    public void HealthOnlyReceiptOmitsUnknownDamageUntilAnExactDamageFactArrives()
    {
        BoardCardPresentation card = Card(2) with { Damage = 3 };
        var health = new EventActivityDescriptor([new(2, "Card", ActivitySubjectRole.Subject)], [new("health", 11, 0)]);
        BoardCardPresentation shown = EventActivityCardFacts.Apply(card, health);
        Assert.Equal("0/14", CardProgressValue.From(shown)!.Value);
        Assert.False(shown.DamageKnown);
        Assert.DoesNotContain("damage", CardLiveStateRendering.Description(shown));
        Assert.True(card.DamageKnown);
        var damage = health with { Quantities = [new("k_damage", 3, 17)] };
        shown = EventActivityCardFacts.Apply(shown, damage);
        Assert.True(shown.DamageKnown);
        Assert.Equal(17, shown.Damage);
        Assert.Contains("17 damage", CardLiveStateRendering.Description(shown));
    }

    [Fact]
    public void HealthWithoutTypedMaximumDoesNotInventOrParseAMaximum()
    {
        var card = Card(2) with { EffectiveValues = new Dictionary<string, CardEffectiveValue>() };
        var health = new EventActivityDescriptor([new(2, "Card", ActivitySubjectRole.Subject)], [new("health", 14, 12)]);
        Assert.Equal("12", EventActivityCardFacts.Apply(card, health).Fields.Single().Value);
    }

    private static BoardCardPresentation Card(int id) => new(id, 1, false, "Card", "", "HERO", "READY", [new("HEALTH", "14/14")])
        { EffectiveValues = new Dictionary<string, CardEffectiveValue> { ["HP"] = new(14, 14, "Printed", false, []) } };
    private static EventPresentation Ready(int id) => new("Ready", "", [id], EventMotionKind.State)
    { Activity = new([new(id, "Card", ActivitySubjectRole.Subject)], [new("is_exhaust", 1, 0)]) };
}
