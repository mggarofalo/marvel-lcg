using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingActiveCounterProjectionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProjectedCompletionDistinguishesARealMoveFromItsCurrentPlacement(bool move)
    {
        var (_, _, scope) = WreckingProjectionFixture.Create();
        var effect = new AbilityEffect.CardAction(AbilityCardInstruction.MoveActiveCounter,
            move ? new AbilityCardSelection.Titled("Bulldozer")
                : new AbilityCardSelection.Query(AbilityCardQuery.Villain));
        var power = AbilityPowerStateFactory.Initial(scope);
        var area = new AreaProjectionState(scope.Context);

        Assert.Equal(move ? AbilityAdmission.AdmissionResolution.Full : AbilityAdmission.AdmissionResolution.None,
            Assert.Single(AbilityPowerOutcomeTrace.PowerOutcomes(effect, scope, true, false, power)));
        Assert.Equal(move ? AbilityProjectionResolution.Full : AbilityProjectionResolution.None,
            AbilityProjectedStateQueries.ProjectedResolution(effect, area, scope.Context));
        Assert.Equal(move ? AbilityAdmission.AdmissionResolution.Full : AbilityAdmission.AdmissionResolution.None,
            AbilityResolutionAdmission.ResolutionOf(effect, scope));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LaterActiveVillainReadsFollowTheMovedCounterOnEveryProjectedBoard(bool promoted)
    {
        // Synthetic composition checks a projected counter move followed by an
        // active-villain read. Physical group ownership and live state stay fixed.
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var target = groups[0].Villain;
        var power = AbilityPowerStateFactory.Initial(scope);
        var area = new AreaProjectionState(scope.Context);
        var repeated = DamageTraceStateFactory.Initial(target, scope);
        if (promoted)
        {
            target = groups[0].Stages[1];
            power.EncounterStages![groups[0].Villain.ObjectId] = target.ObjectId;
            repeated.EncounterStages![groups[0].Villain.ObjectId] = target.ObjectId;
            power.Discarded.Add(groups[0].Villain.ObjectId);
            repeated.Discarded.Add(groups[0].Villain.ObjectId);
            area.Departed.Add(groups[0].Villain.ObjectId);
            area.Entered.Add(target.ObjectId);
        }
        var select = new AbilityCardSelection.Last(
            new AbilityCardSelection.Query(AbilityCardQuery.VillainsWithMostSchemeThreat));
        groups[0].Scheme.PlaceTokens("k_threat", 9);
        var effect = new AbilityEffect.CardAction(AbilityCardInstruction.MoveActiveCounter, select);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            world.Agenda.Outstanding.ToArray());

        var changed = AbilityPowerStateMutation.ApplyPowerLeafState(effect, scope, false, power);
        var projection = new AbilityAreaProjection([effect], null, new HashSet<DeckType>(), scope.Context, 1);
        var changedArea = Assert.Single(projection.Trace(effect, [area]));
        var transfers = AbilityRepeatedDamageAnalysis.DamageTransfers(effect, scope);
        var changedRepeated = new DamageTraceMutation(repeated, target, scope).Apply(transfers);

        Assert.Equal(target.ObjectId, changed.CurrentVillain);
        Assert.Equal(target.ObjectId, changedArea.ActiveVillain);
        Assert.Equal(target.ObjectId, changedRepeated.CurrentVillain);
        Assert.Equal(power.EncounterStages, changed.EncounterStages);
        Assert.Equal(repeated.EncounterStages, changedRepeated.EncounterStages);
        var active = new AbilityCardSelection.Query(AbilityCardQuery.Villain);
        Assert.Same(target, AbilityPowerSelectionTrace.PowerFind(active, scope, changed));
        Assert.Same(target, AbilityProjectedStateQueries.ProjectedFind(active, changedArea, scope.Context));
        Assert.True(new AbilityAreaProjection([effect], null,
            new HashSet<DeckType> { DeckType.VillainArea }, scope.Context, 1).MayChange());
        Assert.Equal(before.Item1, world.Digest().Canonical());
        Assert.Equal(before.Item2, world.Random.Generator.WordsConsumed);
        Assert.Equal(before.Item3, world.Agenda.Outstanding);
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    [Fact]
    public void CurrentHolderProducesNoCompletedMoveAndMissingTopologyRaises()
    {
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var effect = new AbilityEffect.CardAction(AbilityCardInstruction.MoveActiveCounter,
            new AbilityCardSelection.Query(AbilityCardQuery.Villain));
        var power = AbilityPowerStateFactory.Initial(scope);
        var area = new AreaProjectionState(scope.Context);

        Assert.False(AbilityActiveCounterProjection.CanMovePower(effect, scope, power));
        Assert.False(AbilityActiveCounterProjection.CanMoveArea(effect, area, scope.Context));
        Assert.Equal(power, AbilityActiveCounterProjection.MovePower(effect, scope, power));
        Assert.Throws<Marvel.Rules.Play.RulesNotImplementedException>(() =>
            AbilityActiveCounterProjection.CanMove(world, power.CurrentVillain, groups[0].Villain, null, new HashSet<int>()));
        Assert.Throws<Marvel.Rules.Play.RulesNotImplementedException>(() =>
            AbilityActiveCounterProjection.CanMove(world, power.CurrentVillain, groups[0].Villain,
                new Dictionary<int, int>(), new HashSet<int>()));
    }
}
