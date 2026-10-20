using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingSelectedDeckProjectionTests
{
    [Fact]
    public void UnresolvedRevealNeverPretendsToPreserveAProjectedBoard()
    {
        var (world, _, scope) = WreckingProjectionFixture.Create();
        var effect = new AbilityEffect.CardAction(AbilityCardInstruction.RevealFromVillainDeck,
            new AbilityCardSelection.Query(AbilityCardQuery.Villain));
        var power = AbilityPowerStateFactory.Initial(scope);
        var area = new AreaProjectionState(scope.Context);
        var projection = new AbilityAreaProjection([effect], null,
            new HashSet<DeckType> { DeckType.VillainArea }, scope.Context, 1);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            world.Agenda.Outstanding.ToArray());

        Assert.True(AbilityPowerSuspension.SuspendsPowerEffect(effect, scope));
        Assert.True(new AbilityAreaMutationQuery(new HashSet<DeckType> { DeckType.HandsArea },
            scope.Context, 1).MayChange(effect));
        Assert.Throws<RulesNotImplementedException>(() => projection.MayChange());
        Assert.Throws<RulesNotImplementedException>(() =>
            AbilityPowerStateMutation.ApplyPowerLeafState(effect, scope, false, power));
        Assert.Throws<RulesNotImplementedException>(() => projection.Trace(effect, [area]));
        Assert.Throws<RulesNotImplementedException>(() =>
            AbilityRepeatedDamageAnalysis.DamageTransfers(effect, scope));

        Assert.Equal(before.Item1, world.Digest().Canonical());
        Assert.Equal(before.Item2, world.Random.Generator.WordsConsumed);
        Assert.Equal(before.Item3, world.Agenda.Outstanding);
    }

    [Fact]
    public void AdmissionCannotUseAnUnregisteredSameTitleVillainAsADeckOwner()
    {
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var foreign = world.CreateCard(groups[0].Villain.FaceId, world.AreaOf(DeckType.VillainArea));
        var effect = new AbilityEffect.CardAction(AbilityCardInstruction.RevealFromVillainDeck,
            new AbilityCardSelection.Bound(AbilityCardBinding.Chosen));
        var valid = new AbilityAdmission.AbilityAdmissionScope(scope.Context.WithChosen(groups[0].Villain), []);
        var invalid = new AbilityAdmission.AbilityAdmissionScope(scope.Context.WithChosen(foreign), []);
        var absent = new AbilityAdmission.AbilityAdmissionScope(scope.Context.WithChosen(null), []);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);

        Assert.True(AbilitySelectedDeckReveal.HasTarget(effect, valid));
        Assert.False(AbilitySelectedDeckReveal.HasTarget(effect, absent));
        Assert.Throws<RulesNotImplementedException>(() => AbilitySelectedDeckReveal.HasTarget(effect, invalid));
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
    }

    [Fact]
    public void LivingVillainQueryCountsGroupsAfterPromotionAndDefeat()
    {
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var select = new AbilityCardSelection.Query(AbilityCardQuery.Villains);
        var power = AbilityPowerStateFactory.Initial(scope);
        var area = new AreaProjectionState(scope.Context);
        var promoted = groups[0].Stages[1];
        power.EncounterStages![groups[0].Villain.ObjectId] = promoted.ObjectId;
        power.Discarded.Add(groups[0].Villain.ObjectId);
        power.EncounterStages[groups[3].Villain.ObjectId] = -1;
        power.Discarded.Add(groups[3].Villain.ObjectId);
        area.Departed.Add(groups[0].Villain.ObjectId);
        area.Departed.Add(groups[3].Villain.ObjectId);
        area.Entered.Add(promoted.ObjectId);
        int[] expected = [promoted.ObjectId, groups[1].Villain.ObjectId, groups[2].Villain.ObjectId];
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);

        Assert.Equal(expected.Order(), AbilityPowerSelectionTrace.PowerEvery(select, scope, power)
            .Select(card => card.ObjectId).Order());
        Assert.Equal(expected.Order(), AbilityProjectedStateQueries.ProjectedEvery(select, area, scope.Context)
            .Select(card => card.ObjectId).Order());
        Assert.Equal(4, AbilityCardQueries.Cards(AbilityCardQuery.Villains, scope.Context.Query).Count);
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
    }
}
