using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingAttackConstantProjectionTests
{
    [Theory]
    [InlineData("""{"isEnemyAttacker":"this"}""", false, false, true)]
    [InlineData("""{"and":[{"isEnemyAttacker":"this"},{"undefendedAttack":"you"}]}""", false, false, true)]
    [InlineData("""{"isEnemyAttacker":"this"}""", true, false, false)]
    [InlineData("""{"isEnemyAttacker":"this"}""", false, true, false)]
    [InlineData("""{"isEnemyAttacker":"chosen"}""", false, false, false)]
    [InlineData("""{"not":{"isEnemyAttacker":"this"}}""", false, false, false)]
    [InlineData("""{"or":[{"isEnemyAttacker":"this"},{"undefendedAttack":"you"}]}""", false, false, false)]
    [InlineData("""{"and":[{"undefendedAttack":"you"},{"isEnemyAttacker":"this"}]}""", false, false, false)]
    public void OnlyProvablyInactiveSourceAttackConstantsPermitStageContinuation(
        string condition, bool activeAttack, bool hasElse, bool exact)
    {
        // Synthetic successor grants health, not ATK: the proof must establish
        // that the whole constant is inactive, rather than ignore a field.
        var (world, groups, original) = WreckingProjectionFixture.Create();
        var current = groups[0].Villain;
        var next = groups[0].Stages[1];
        var json = """
            {"cards":[{"card":"07003","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"if":{"test":CONDITION,
                "then":{"grant":{"card":"this","keyword":"health","amount":100}}ELSE}}
            }]}]}
            """.Replace("CONDITION", condition, StringComparison.Ordinal)
            .Replace("ELSE", hasElse
                ? ""","else":{"grant":{"card":"this","keyword":"health","amount":100}}"""
                : "", StringComparison.Ordinal);
        var program = AbilityLowering.Book(AbilityCatalog.Parse(json));
        var compiled = Assert.Single(program.On("07003"));
        Assert.False(AbilityInactiveAttackConstant.IsInactive(compiled with
        {
            When = new AbilityCondition.IsEnemyAttacker(new AbilityCardSelection.Bound(AbilityCardBinding.This)),
        }, world));
        var scope = new AbilityAdmission.AbilityAdmissionScope(
            original.Context.WithChosen(current) with { Program = program }, []);
        if (activeAttack)
            world.Attack = new EnemyAttack(current.ObjectId, 0, world.Seats[0].IdentityCard.ObjectId);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed, world.Attack);
        var state = AbilityPowerHealthTrace.ApplyPowerDamage(AbilityPowerStateFactory.Initial(scope),
            [current], 100, world.Seats[0].IdentityCard, scope);

        Assert.Equal(exact, state.UnprojectedStageEntry is null);
        AssertRepeatedEntry(scope, current, next, exact);
        if (exact)
        {
            var subsequent = AbilityPowerHealthTrace.ApplyPowerDamage(state, [next], 1,
                world.Seats[0].IdentityCard, scope);
            Assert.Equal(1, subsequent.CardDamage[next.ObjectId]);
        }
        else
        {
            Assert.Throws<RulesNotImplementedException>(() => AbilityPowerHealthTrace.ApplyPowerDamage(
                state, [next], 1, world.Seats[0].IdentityCard, scope));
        }
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed, world.Attack));
    }

    private static void AssertRepeatedEntry(
        AbilityAdmission.AbilityAdmissionScope scope, Card current, Card next, bool exact)
    {
        var mutation = new DamageTraceMutation(DamageTraceStateFactory.Initial(current, scope), current, scope);
        if (exact)
        {
            var promoted = mutation.Apply([new(-1, current.ObjectId, 100)]);
            Assert.Equal(next.ObjectId, promoted.EncounterStages![current.ObjectId]);
        }
        else
        {
            Assert.Throws<RulesNotImplementedException>(() => mutation.Apply([new(-1, current.ObjectId, 100)]));
        }
    }
}
