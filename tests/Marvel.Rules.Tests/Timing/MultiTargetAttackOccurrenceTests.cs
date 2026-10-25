using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Timing;

public sealed class MultiTargetAttackOccurrenceTests
{
    [Rule("rr:attack-player-ability-type.2.1")]
    [Rule("rr:attack-player-ability-type.5")]
    [Rule("rr:triggering-condition.1")]
    [Fact]
    public void OneWindowRetainsDistinctRecipientResponseInstances()
    {
        // "A single attack" still considers the character to have "attacked
        // each of those enemies". Each matching trigger may answer once.
        var occurrence = new Occurrence(7, Steps.AttackEnds);
        var villain = Target(10, 1, CardKind.EncounterVillain);
        var minion = Target(11, 1, CardKind.Minion);
        occurrence.Also(villain);
        occurrence.Also(minion);
        var first = new PendingAbility(20, AbilityType.Response, 0) { TriggeringAttack = villain };
        var second = first with { TriggeringAttack = minion };

        Assert.True(occurrence.Trigger(WindowKind.Response, second));
        occurrence.Resolution.Begin(second);
        occurrence.Resolution.Resolve(second);
        Assert.False(occurrence.MayTrigger(WindowKind.Response, second));
        Assert.True(occurrence.MayTrigger(WindowKind.Response, first));
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.Resolution.StatusOf(first));
        Assert.True(occurrence.Trigger(WindowKind.Response, first));
        occurrence.Resolution.Begin(first);
        Assert.Equal(ResolutionStatus.Pending, occurrence.Resolution.StatusOf(first));
        Assert.Equal(2, occurrence.Resolutions.Count);
        Assert.Equal(new[] { villain, minion }, occurrence.Attacks);
        Assert.Equal(new[] { Steps.AttackEnds }, occurrence.Conditions);
        Assert.Equal(7, occurrence.Id);
    }

    [Rule("rr:attack-player-ability-type.2.1")]
    [Fact]
    public void WholeAttackResponseRemainsOneInstance()
    {
        // "An ability labeled as an attack is considered a single attack."
        var occurrence = new Occurrence(7, Steps.AttackEnds);
        occurrence.Also(Target(10, 1, CardKind.EncounterVillain));
        occurrence.Also(Target(11, 1, CardKind.Minion));
        var response = new PendingAbility(20, AbilityType.Response, 0);
        Assert.True(occurrence.Trigger(WindowKind.Response, response));
        Assert.False(occurrence.Trigger(WindowKind.Response, response));
        Assert.Empty(new Occurrence(8, Steps.AttackEnds).Attacks);
    }

    [Fact]
    public void ARecipientCannotBeRecordedTwiceButIncarnationsRemainDistinct()
    {
        var occurrence = new Occurrence(7, Steps.AttackEnds);
        var target = Target(10, 1, CardKind.Minion);
        occurrence.Also(target);
        Assert.Throws<InvalidOperationException>(() => occurrence.Also(target));
        var first = new PendingAbility(20, AbilityType.Response, 0) { TriggeringAttack = target };
        Assert.True(occurrence.Trigger(WindowKind.Response, first));
        Assert.True(occurrence.MayTrigger(WindowKind.Response,
            first with { TriggeringAttack = target with { Incarnation = 2 } }));
        Assert.Single(occurrence.Attacks);
    }

    private static Attacked Target(int card, int incarnation, CardKind kind) =>
        new(card, incarnation, new OccurrenceCard(card, kind, World.Scenario, World.Scenario), 0);
}
