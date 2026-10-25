using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingStatusProjectionTests
{
    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:villain-defeat.4.2")]
    [Theory]
    [InlineData(Statuses.Stunned, true, false)]
    [InlineData(Statuses.Stunned, true, true)]
    [InlineData(Statuses.Confused, true, false)]
    [InlineData(Statuses.Confused, true, true)]
    [InlineData(Statuses.Stunned, false, false)]
    [InlineData(Statuses.Stunned, false, true)]
    [InlineData(Statuses.Confused, false, false)]
    [InlineData(Statuses.Confused, false, true)]
    public void ProjectedStatusCountsFollowOnlyASameCharacterSuccessor(string status, bool sameTitle, bool traceLocal)
    {
        // Same-title stages: "status cards ... carry over to the new stage."
        // Different-title stages: those cards "do not carry over".
        var (world, groups, scope) = WreckingProjectionFixture.Create(sameTitle ? null : "01113");
        var prior = groups[0].Villain;
        var next = groups[0].Stages[1];
        if (!traceLocal) Statuses.Give(world, prior, status);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
        var initial = AbilityPowerStateFactory.Initial(scope);
        var give = new AbilityEffect.GiveStatus(new AbilityCardSelection.Titled("Wrecker"), status);
        if (traceLocal) initial = AbilityPowerStateMutation.ApplyPowerLeafState(give, scope, false, initial);
        var power = AbilityPowerHealthTrace.ApplyPowerDamage(initial, [prior], 100,
            world.Seats[0].IdentityCard, scope);
        Assert.Equal(sameTitle ? 1 : 0, power.StatusCounts.GetValueOrDefault((next.ObjectId, status)));
        Assert.Equal(sameTitle, power.StatusChanges.Contains((next.ObjectId, status)));
        Assert.Equal(traceLocal ? 1 : 0, initial.StatusCounts.GetValueOrDefault((prior.ObjectId, status)));
        var repeatedInitial = DamageTraceStateFactory.Initial(prior, scope);
        if (traceLocal) repeatedInitial = new DamageTraceMutation(repeatedInitial, prior, scope)
            .Apply(Assert.Single(AbilityRepeatedDamageAnalysis.DamageTraces(
                give, scope, AbilityRepeatedEffectAnalysis.RepeatedChange.None, false)));
        var repeated = new DamageTraceMutation(repeatedInitial, prior, scope)
            .Apply([new(-1, prior.ObjectId, 100)]);
        Assert.Equal(sameTitle ? 1 : 0, repeated.StatusCounts.GetValueOrDefault((next.ObjectId, status)));
        Assert.Equal(sameTitle, repeated.StatusChanges.Contains((next.ObjectId, status)));
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
    }
}
