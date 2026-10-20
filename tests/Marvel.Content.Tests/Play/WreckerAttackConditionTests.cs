using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckerAttackConditionTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("42")]
    [InlineData("\"not-a-selector\"")]
    [InlineData("{\"query\":\"villain\",\"extra\":true}")]
    public void InvalidAttackerSelectorsFailAtLowering(string selector)
    {
        var json = """
            {"cards":[{"card":"07002","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"if":{"test":{"isEnemyAttacker":SELECTOR},
                "then":{"grant":{"card":"this","keyword":"attack","amount":2}}}}
            }]}]}
            """.Replace("SELECTOR", selector, StringComparison.Ordinal);
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(json)));
    }

    [Fact]
    public void ProjectedStageChangeDoesNotReuseTheLiveAttackerRelationship()
    {
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var first = groups[0].Villain;
        world.Attack = new EnemyAttack(first.ObjectId, 0, world.Seats[0].IdentityCard.ObjectId);
        var condition = new AbilityCondition.IsEnemyAttacker(new AbilityCardSelection.Titled("Wrecker"));
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed, world.Attack);
        var changes = new VillainTraceChanges([first.ObjectId], new Dictionary<int, long>(),
            new Dictionary<int, long>(), new Dictionary<(int, string), long>(),
            new Dictionary<int, HashSet<string>>(), new HashSet<(int, string)>(), new Dictionary<int, int>());
        var trace = new VillainConditionTrace(first, groups[0].Stages[1], scope, changes, 0, 0);

        Assert.False(trace.TryTest(condition, out _));
        Assert.True(AbilityConditionProjection.ValueReadsVillain(condition, scope));
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed, world.Attack));
    }

    [Fact]
    public void CompletedAndDepartedAttackersDoNotCountAsCurrentlyAttacking()
    {
        var (world, groups, original) = WreckingProjectionFixture.Create();
        var source = groups[0].Villain;
        var scope = new AbilityAdmission.AbilityAdmissionScope(original.Context.WithChosen(source), []);
        var condition = new AbilityCondition.IsEnemyAttacker(
            new AbilityCardSelection.Bound(AbilityCardBinding.Chosen));
        var attack = new EnemyAttack(source.ObjectId, 0, world.Seats[0].IdentityCard.ObjectId);
        world.FinishedAttack = attack;
        Assert.False(AbilityInitiationPrimitives.Test(condition, scope));
        world.Attack = attack;
        Assert.True(AbilityInitiationPrimitives.Test(condition, scope));
        World.MoveToTop(source, world.AreaOf(DeckType.RemovedArea));
        Assert.False(AbilityInitiationPrimitives.Test(condition, scope));
    }

    [Theory]
    [InlineData(-1, false, 2)]
    [InlineData(0, false, 4)]
    [InlineData(0, true, 2)]
    [InlineData(1, false, 2)]
    [InlineData(1, true, 2)]
    public void ConstantBindsItsOwnAttackerAndReadsLiveDefense(int attackingGroup, bool defended, int expected)
    {
        // 07002: "While Wrecker is attacking, he gets +2 ATK if the attack is undefended."
        // This arranged constant contract distinguishes his physical source from the active counter.
        var (world, groups, _) = WreckingProjectionFixture.Create();
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"07002","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"if":{"test":{"and":[
                {"isEnemyAttacker":"this"},{"undefendedAttack":"you"}]},
                "then":{"grant":{"card":"this","keyword":"attack","amount":2}}}}
            }]}]}
            """));
        var hero = world.Seats[0].IdentityCard;
        if (attackingGroup >= 0)
            world.Attack = new EnemyAttack(groups[attackingGroup].Villain.ObjectId, 0, hero.ObjectId,
                Defender: defended ? hero.ObjectId : -1);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed, world.Attack);

        Assert.Equal(expected, StateFields.Modified(world, groups[0].Villain, "attack", world.Facts, world.Players));

        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed, world.Attack));
        world.Attack = null;
        Assert.Equal(2, StateFields.Modified(world, groups[0].Villain, "attack", world.Facts, world.Players));
    }
}
