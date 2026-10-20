using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingSingularTraceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PowerProjectionRefusesNewlyAmbiguousSingularTitles(bool fallback)
    {
        // Synthetic duplicate titles distinguish a single-card query from choosing
        // the first candidate; a projected state preserves live selector cardinality.
        var (world, groups, scope) = WreckingProjectionFixture.Create(firstSuccessor: "07017");
        AbilityCardSelection title = new AbilityCardSelection.Titled("Thunderball");
        var selection = fallback
            ? new AbilityCardSelection.FirstAvailable([title, new AbilityCardSelection.Bound(AbilityCardBinding.YourHero)])
            : title;
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
        var state = AbilityPowerHealthTrace.ApplyPowerDamage(AbilityPowerStateFactory.Initial(scope),
            [groups[0].Villain], 100, world.Seats[0].IdentityCard, scope);

        Assert.Throws<RulesNotImplementedException>(() => AbilityPowerTrace.PowerFind(selection, scope, state));

        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
    }

    [Fact]
    public void APluralOnlySelectorDoesNotBecomeSingularMerelyBecauseOneCardMatches()
    {
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
        var state = DamageTraceStateFactory.Initial(groups[0].Villain, scope);
        var trace = new AbilitySelectorTraceContext(state.CurrentVillain, scope, state.Discarded,
            state.Traits, state.Modifiers, state.Engagement)
        { EncounterStages = state.EncounterStages, EncounterOrder = state.EncounterOrder };
        Assert.Throws<RulesNotImplementedException>(() =>
            AbilitySingularTraceSelection.Find(new AbilityCardSelection.Query(AbilityCardQuery.Minions), trace));
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NewlyAmbiguousTitleRefusesASingularChoiceIncludingInsideFallback(bool fallback)
    {
        var (world, groups, scope) = WreckingProjectionFixture.Create(firstSuccessor: "07017");
        AbilityCardSelection title = new AbilityCardSelection.Titled("Thunderball");
        var selected = fallback
            ? new AbilityCardSelection.FirstAvailable([title, new AbilityCardSelection.Bound(AbilityCardBinding.YourHero)])
            : title;
        var effect = new AbilityEffect.Heal(selected, new AbilityNumber.Constant(1));
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
        var trace = Assert.Single(AbilityRepeatedDamageAnalysis.DamageTraces(
            effect, scope, AbilityRepeatedEffectAnalysis.RepeatedChange.None, false));
        var target = groups[0].Villain;
        var initial = new DamageTraceMutation(DamageTraceStateFactory.Initial(target, scope), target, scope)
            .Apply([new(-1, target.ObjectId, 100)]);
        Assert.Throws<RulesNotImplementedException>(() => new DamageTraceMutation(initial, target, scope).Apply(trace));
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GeneratedMovementFollowsChosenSameCharacterButNotADifferentTitleSuccessor(bool sameTitle)
    {
        var (world, groups, original) = WreckingProjectionFixture.Create(sameTitle ? null : "01113");
        var target = groups[0].Villain;
        var scope = new AbilityAdmission.AbilityAdmissionScope(original.Context.WithChosen(target), []);
        var effect = new AbilityEffect.MoveDamage(
            new AbilityCardSelection.Bound(AbilityCardBinding.YourHero),
            new AbilityCardSelection.Bound(AbilityCardBinding.Chosen), new AbilityNumber.Constant(1), false);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
        var trace = Assert.Single(AbilityRepeatedDamageAnalysis.DamageTraces(
            effect, scope, AbilityRepeatedEffectAnalysis.RepeatedChange.None, false));
        var hero = world.Seats[0].IdentityCard;
        var initial = DamageTraceStateFactory.Initial(target, scope);
        initial.Damage[hero.ObjectId] = 2;
        initial = new DamageTraceMutation(initial, target, scope).Apply([new(-1, target.ObjectId, 100)]);
        var moved = new DamageTraceMutation(initial, target, scope).Apply(trace);
        Assert.Equal(sameTitle ? 1 : 2, moved.Damage[hero.ObjectId]);
        Assert.Equal(sameTitle ? 1 : 0, moved.Damage[groups[0].Stages[1].ObjectId]);
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratedHealingOrHealthGrantUsesTheNewActiveVillainAfterThePriorGroupIsDefeated(bool grantHealth)
    {
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var target = groups[3].Villain;
        var selected = new AbilityCardSelection.Query(AbilityCardQuery.Villain);
        AbilityEffect effect = grantHealth
            ? new AbilityEffect.GrantField(selected, "health", new AbilityNumber.Constant(1), false, "EndOfPlayerPhase")
            : new AbilityEffect.Heal(selected, new AbilityNumber.Constant(1));
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
        var trace = Assert.Single(AbilityRepeatedDamageAnalysis.DamageTraces(
            effect, scope, AbilityRepeatedEffectAnalysis.RepeatedChange.None, false));
        var initial = DamageTraceStateFactory.Initial(target, scope);
        initial.Threat[groups[3].Scheme.ObjectId] = 9;
        initial.Damage[target.ObjectId] = 5;
        initial = new DamageTraceMutation(initial, target, scope).Apply(
            [new(-1, groups[1].Villain.ObjectId, 100), new(-1, groups[1].Stages[1].ObjectId, 100)]);
        Assert.Equal(target.ObjectId, initial.CurrentVillain);
        var healed = new DamageTraceMutation(initial, target, scope).Apply(trace);
        Assert.Equal(grantHealth ? 5 : 4, healed.Damage[target.ObjectId]);
        Assert.Equal(grantHealth ? 1 : 0, healed.Health.GetValueOrDefault(target.ObjectId));
        Assert.Equal(initial.Damage[groups[1].Villain.ObjectId], healed.Damage[groups[1].Villain.ObjectId]);
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
    }
}
