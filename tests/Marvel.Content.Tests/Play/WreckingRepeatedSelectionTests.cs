using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingRepeatedSelectionTests
{
    [Fact]
    public void GeneratedActiveVillainSelectionCanFollowTheCounterToAnyLivingGroup()
    {
        var (_, groups, scope) = WreckingProjectionFixture.Create();
        var target = groups[3].Villain;
        var effect = new AbilityEffect.Damage(
            new AbilityCardSelection.Query(AbilityCardQuery.Villain), new AbilityNumber.Constant(1), false);
        var trace = Assert.Single(AbilityRepeatedDamageAnalysis.DamageTraces(
            effect, scope, AbilityRepeatedEffectAnalysis.RepeatedChange.None, false));
        var initial = DamageTraceStateFactory.Initial(target, scope);
        initial.Threat[groups[3].Scheme.ObjectId] = 9;
        initial = new DamageTraceMutation(initial, target, scope).Apply(
            [new(-1, groups[1].Villain.ObjectId, 100), new(-1, groups[1].Stages[1].ObjectId, 100)]);
        Assert.Equal(target.ObjectId, initial.CurrentVillain);
        var state = new DamageTraceMutation(initial, target, scope).Apply(trace);
        Assert.Equal(1, state.Damage.GetValueOrDefault(target.ObjectId));
        Assert.Equal(0, state.Damage.GetValueOrDefault(groups[0].Villain.ObjectId));
        Assert.Equal(0, state.Damage.GetValueOrDefault(groups[2].Villain.ObjectId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratedEnemyDamageVisitsEachLivingGroupOnceRatherThanEachPhysicalStage(bool promote)
    {
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var target = groups[0].Villain;
        var effect = new AbilityEffect.Damage(
            new AbilityCardSelection.Query(AbilityCardQuery.Enemies), new AbilityNumber.Constant(1), false);
        var trace = Assert.Single(AbilityRepeatedDamageAnalysis.DamageTraces(
            effect, scope, AbilityRepeatedEffectAnalysis.RepeatedChange.None, false));
        Assert.Equal(4, trace.Count);
        string digest = world.Digest().Canonical();
        var initial = DamageTraceStateFactory.Initial(target, scope);
        if (promote)
            initial = new DamageTraceMutation(initial, target, scope).Apply([new(-1, target.ObjectId, 100)]);
        var state = new DamageTraceMutation(initial, target, scope).Apply(trace);
        foreach (var group in groups)
        {
            int current = state.EncounterStages![group.Villain.ObjectId];
            Assert.Equal(1, state.Damage.GetValueOrDefault(current));
        }
        var repeated = new DamageTraceMutation(state, target, scope).Apply(trace);
        Assert.All(repeated.EncounterStages!.Values,
            current => Assert.Equal(2, repeated.Damage.GetValueOrDefault(current)));
        Assert.Equal(digest, world.Digest().Canonical());
    }
}
