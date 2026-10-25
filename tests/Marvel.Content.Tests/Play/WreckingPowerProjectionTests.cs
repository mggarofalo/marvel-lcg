using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Arranged projection contracts, not full scenario admission.</summary>
public sealed class WreckingPowerProjectionTests : CoreCardTestBase
{
    [Rule("rr:villain-defeat.3.1")]
    [Fact]
    public void InactiveExtremePromotionAndFinalDefeatDoNotReplaceTheActiveVillain()
    {
        // The pack's Extreme mode promotes each villain to its own B stage.
        // The active counter remains on another living group.
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var hostage = world.CreateCard("07005",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, groups[0].Scheme.ObjectId));
        var initial = AbilityPowerStateFactory.Initial(scope);
        string digest = world.Digest().Canonical();
        long rng = world.Random.Generator.WordsConsumed;

        var promoted = Damage(initial, groups[0].Villain, scope);
        Assert.Equal(groups[1].Villain.ObjectId, promoted.CurrentVillain);
        Assert.Equal(groups[0].Stages[1].ObjectId, promoted.EncounterStages![groups[0].Villain.ObjectId]);
        Assert.False(promoted.Finished);
        Assert.Contains(groups[0].Villain.ObjectId, promoted.Discarded);
        Assert.DoesNotContain(groups[0].Scheme.ObjectId, promoted.Discarded);
        Assert.Equal(new[] { groups[1].Villain, groups[2].Villain, groups[3].Villain, groups[0].Stages[1] },
            AbilityPowerSelectionTrace.PowerEvery(new AbilityCardSelection.Query(AbilityCardQuery.Enemies), scope, promoted));
        Assert.Same(groups[0].Stages[1], AbilityPowerSelectionTrace.PowerFind(
            new AbilityCardSelection.Last(new AbilityCardSelection.Query(AbilityCardQuery.Enemies)), scope, promoted));
        Assert.Same(groups[1].Villain,
            AbilityPowerSelectionTrace.PowerFind(new AbilityCardSelection.Query(AbilityCardQuery.Villain), scope, promoted));

        var defeated = Damage(promoted, groups[0].Stages[1], scope);
        Assert.Equal(-1, defeated.EncounterStages![groups[0].Villain.ObjectId]);
        Assert.Equal(groups[1].Villain.ObjectId, defeated.CurrentVillain);
        Assert.False(defeated.Finished);
        Assert.Contains(groups[0].Scheme.ObjectId, defeated.Discarded);
        Assert.Contains(hostage.ObjectId, defeated.Discarded);
        Assert.Equal(groups.Skip(1).Select(group => group.Villain),
            AbilityPowerSelectionTrace.PowerEvery(new AbilityCardSelection.Query(AbilityCardQuery.Enemies), scope, defeated));
        Assert.Equal(groups[0].Villain.ObjectId, initial.EncounterStages![groups[0].Villain.ObjectId]);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng, world.Random.Generator.WordsConsumed);
    }

    [Fact]
    public void SuccessivePromotionsAppendInResolutionOrderWhileExplicitObjectOrderRemainsExplicit()
    {
        var (_, groups, scope) = WreckingProjectionFixture.Create();
        var state = Damage(AbilityPowerStateFactory.Initial(scope), groups[0].Villain, scope);
        state = Damage(state, groups[2].Villain, scope);
        var enemies = new AbilityCardSelection.Query(AbilityCardQuery.Enemies);
        Card[] expected = [groups[1].Villain, groups[3].Villain, groups[0].Stages[1], groups[2].Stages[1]];
        Assert.Equal(expected, AbilityPowerSelectionTrace.PowerEvery(enemies, scope, state));
        Assert.Same(expected[^1], AbilityPowerSelectionTrace.PowerFind(new AbilityCardSelection.Last(enemies), scope, state));
        Assert.Equal(expected.OrderBy(card => card.ObjectId), AbilityPowerSelectionTrace.PowerEvery(
            new AbilityCardSelection.InObjectIdOrder(enemies), scope, state));
    }

    [Fact]
    public void ActiveFinalDefeatUsesProjectedThreatForUniqueReplacement()
    {
        var (_, groups, scope) = WreckingProjectionFixture.Create();
        var state = AbilityPowerStateFactory.Initial(scope);
        state = state with { SchemeThreat = new Dictionary<int, long> { [groups[3].Scheme.ObjectId] = 9 } };
        state = Damage(state, groups[1].Villain, scope);
        state = Damage(state, groups[1].Stages[1], scope);
        Assert.Equal(groups[3].Villain.ObjectId, state.CurrentVillain);
        Assert.Null(state.UnprojectedStageEntry);
        Assert.False(state.Finished);
    }

    [Fact]
    public void ActiveFinalDefeatPreservesAnUnansweredTieInsteadOfChoosingByOrder()
    {
        var (_, groups, scope) = WreckingProjectionFixture.Create();
        var state = AbilityPowerStateFactory.Initial(scope);
        state = Damage(state, groups[1].Villain, scope);
        state = Damage(state, groups[1].Stages[1], scope);
        Assert.False(state.Finished);
        Assert.Equal(-1, state.CurrentVillain);
        Assert.Contains("tied-villain choice", state.UnprojectedStageEntry);
        Assert.Throws<RulesNotImplementedException>(() => Damage(state, groups[0].Villain, scope));
        Assert.False(AbilityPowerSuspension.SuspendsPowerEffect(
            new AbilityEffect.Draw(new AbilityPlayerSelection.OnePlayer(AbilityPlayer.You), 1), scope,
            reachability: state));
    }

    [Fact]
    public void AreaProjectionKeepsDamageAndAttachmentsInTheirOwnGroup()
    {
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var attachment = world.CreateCard("07006",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, groups[0].Villain.ObjectId));
        var hostage = world.CreateCard("07005",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, groups[0].Scheme.ObjectId));
        var state = new AreaProjectionState(scope.Context);
        var projection = new AbilityAreaProjection([], null,
            new HashSet<DeckType> { DeckType.EncounterDiscardPile }, scope.Context, 1);
        string digest = world.Digest().Canonical();
        Assert.Equal(groups[1].Villain.ObjectId, state.ActiveVillain);

        projection.DealProjected(state, groups[0].Villain, 100, 1);
        Assert.Contains(groups[0].Villain.ObjectId, state.Departed);
        Assert.Contains(groups[0].Stages[1].ObjectId, state.Entered);
        Assert.DoesNotContain(groups[1].Villain.ObjectId, state.Departed);
        Assert.Equal(groups[1].Villain.ObjectId, state.ActiveVillain);
        Assert.Equal(groups[0].Stages[1].ObjectId, state.Hosts[attachment.ObjectId]);
        Assert.DoesNotContain(attachment.ObjectId, state.Departed);
        Assert.DoesNotContain(groups[0].Scheme.ObjectId, state.Departed);

        projection.DealProjected(state, groups[0].Villain, 100, 1);
        Assert.Contains(groups[0].Stages[1].ObjectId, state.Departed);
        Assert.Contains(attachment.ObjectId, state.Departed);
        Assert.Contains(hostage.ObjectId, state.Departed);
        Assert.Contains(groups[0].Scheme.ObjectId, state.Departed);
        Assert.Equal(groups[1].Villain.ObjectId, state.ActiveVillain);
        Assert.Equal(digest, world.Digest().Canonical());
    }

    [Fact]
    public void AreaChosenBindingFollowsItsOwnPromotedCharacterAndDisappearsAfterFinalDefeat()
    {
        var (_, groups, scope) = WreckingProjectionFixture.Create();
        var context = scope.Context.WithChosen(groups[0].Villain);
        var state = new AreaProjectionState(context);
        var projection = new AbilityAreaProjection([], null,
            new HashSet<DeckType> { DeckType.EncounterDiscardPile }, context, 1);
        var chosen = new AbilityCardSelection.Bound(AbilityCardBinding.Chosen);
        projection.DealProjected(state, groups[0].Villain, 100, 1);
        Assert.Same(groups[0].Stages[1], AbilityProjectedStateQueries.ProjectedFind(chosen, state, context));
        Assert.Same(groups[1].Villain, AbilityProjectedStateQueries.ProjectedFind(
            new AbilityCardSelection.Query(AbilityCardQuery.Villain), state, context));
        projection.DealProjected(state, groups[0].Villain, 100, 1);
        Assert.Null(AbilityProjectedStateQueries.ProjectedFind(chosen, state, context));
    }

    [Fact]
    public void AreaProjectionUsesProjectedThreatAndRefusesAnUnansweredActiveTie()
    {
        var (_, groups, scope) = WreckingProjectionFixture.Create();
        var projection = new AbilityAreaProjection([], null,
            new HashSet<DeckType> { DeckType.EncounterDiscardPile }, scope.Context, 1);
        var tied = new AreaProjectionState(scope.Context);
        projection.DealProjected(tied, groups[1].Villain, 100, 1);
        Assert.Throws<RulesNotImplementedException>(() =>
            projection.DealProjected(tied, groups[1].Stages[1], 100, 1));
        var unique = new AreaProjectionState(scope.Context);
        unique.Threat[groups[3].Scheme.ObjectId] = 9;
        projection.DealProjected(unique, groups[1].Villain, 100, 1);
        projection.DealProjected(unique, groups[1].Stages[1], 100, 1);
        Assert.Equal(groups[3].Villain.ObjectId, unique.ActiveVillain);
    }

    [Fact]
    public void RepeatedDamageTracksInactivePromotionAndFinalDefeatWithoutFinishingOtherGroups()
    {
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var target = groups[0].Villain;
        var initial = DamageTraceStateFactory.Initial(target, scope);
        var enemies = new AbilityCardSelection.Query(AbilityCardQuery.Enemies);
        string digest = world.Digest().Canonical();
        var promoted = new DamageTraceMutation(initial, target, scope).Apply(
            [new(-1, target.ObjectId, 100, ToVillain: enemies)]);
        Assert.Equal(groups[1].Villain.ObjectId, promoted.CurrentVillain);
        Assert.Equal(groups[0].Stages[1].ObjectId, promoted.EncounterStages![target.ObjectId]);
        Assert.False(promoted.Finished);
        Assert.DoesNotContain(groups[0].Scheme.ObjectId, promoted.Discarded);
        var defeated = new DamageTraceMutation(promoted, target, scope).Apply(
            [new(-1, target.ObjectId, 100, ToVillain: enemies)]);
        Assert.Equal(-1, defeated.EncounterStages![target.ObjectId]);
        Assert.Equal(groups[1].Villain.ObjectId, defeated.CurrentVillain);
        Assert.Contains(groups[0].Scheme.ObjectId, defeated.Discarded);
        Assert.False(defeated.Finished);
        Assert.Equal(digest, world.Digest().Canonical());
    }

    [Fact]
    public void RepeatedDamageRejectsActiveTieAndUsesUniqueProjectedThreat()
    {
        var (_, groups, scope) = WreckingProjectionFixture.Create();
        var target = groups[1].Villain;
        var initial = DamageTraceStateFactory.Initial(target, scope);
        var promoted = new DamageTraceMutation(initial, target, scope).Apply([new(-1, target.ObjectId, 100)]);
        Assert.Throws<RulesNotImplementedException>(() => new DamageTraceMutation(promoted, target, scope)
            .Apply([new(-1, groups[1].Stages[1].ObjectId, 100)]));
        promoted.Threat[groups[3].Scheme.ObjectId] = 9;
        var defeated = new DamageTraceMutation(promoted, target, scope)
            .Apply([new(-1, groups[1].Stages[1].ObjectId, 100)]);
        Assert.Equal(groups[3].Villain.ObjectId, defeated.CurrentVillain);
        Assert.False(defeated.Finished);
    }

    [Fact]
    public void DifferentTitleSuccessorIsRediscoveredByQueryButDoesNotInheritChosenCharacter()
    {
        var (_, groups, original) = WreckingProjectionFixture.Create(firstSuccessor: "01113");
        var scope = new AbilityAdmission.AbilityAdmissionScope(original.Context.WithChosen(groups[0].Villain), []);
        var chosen = new AbilityCardSelection.Bound(AbilityCardBinding.Chosen);
        var enemies = new AbilityCardSelection.Query(AbilityCardQuery.Enemies);
        var power = Damage(AbilityPowerStateFactory.Initial(scope), groups[0].Villain, scope);
        Assert.Contains(groups[0].Stages[1], AbilityPowerSelectionTrace.PowerEvery(enemies, scope, power));
        Assert.Null(AbilityPowerSelectionTrace.PowerFind(chosen, scope, power));
        Assert.Empty(AbilityPowerSelectionTrace.PowerEvery(chosen, scope, power));
        Assert.Empty(AbilityPowerSelectionTrace.PowerEvery(
            new AbilityCardSelection.WithTrait(chosen, "SYNTHETIC") { Exclude = true }, scope, power));
        var area = new AreaProjectionState(scope.Context);
        var projection = new AbilityAreaProjection([], null,
            new HashSet<DeckType> { DeckType.EncounterDiscardPile }, scope.Context, 1);
        projection.DealProjected(area, groups[0].Villain, 100, 1);
        Assert.Contains(groups[0].Stages[1], AbilityProjectedStateQueries.ProjectedEvery(enemies, area, scope.Context));
        Assert.Null(AbilityProjectedStateQueries.ProjectedFind(chosen, area, scope.Context));
    }

    [Fact]
    public void ProjectedCandidateOrderKeepsEachGroupInItsCapturedVillainArea()
    {
        var (world, groups, original) = WreckingProjectionFixture.Create();
        var area = world.CreateArea(DeckType.VillainArea);
        World.MoveToTop(groups[2].Villain, area);
        var scope = new AbilityAdmission.AbilityAdmissionScope(original.Context.WithChosen(groups[2].Villain), []);
        var power = Damage(AbilityPowerStateFactory.Initial(scope), groups[2].Villain, scope);
        var enemies = new AbilityCardSelection.Query(AbilityCardQuery.Enemies);
        Assert.Equal(new[] { groups[0].Villain, groups[1].Villain, groups[3].Villain, groups[2].Stages[1] },
            AbilityPowerSelectionTrace.PowerEvery(enemies, scope, power));
        Assert.Equal(new[] { groups[2].Stages[1] }, AbilityPowerSelectionTrace.PowerEvery(
            new AbilityCardSelection.WithTrait(new AbilityCardSelection.Bound(AbilityCardBinding.Chosen),
                "SYNTHETIC") { Exclude = true }, scope, power));
    }

    [Rule("rr:when-revealed-abilities")]
    [Fact]
    public void RepeatedProjectionRefusesUnknownStageEntryBeforeReadingItsResult()
    {
        // Synthetic composed topology: revealing Klaw II must resolve its
        // When Revealed search for The Immortal Klaw before subsequent reads.
        // No currently admitted each-player damage body can promote a villain.
        var (_, groups, scope) = WreckingProjectionFixture.Create(firstSuccessor: "01114");
        var target = groups[0].Villain;
        var initial = DamageTraceStateFactory.Initial(target, scope);
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            new DamageTraceMutation(initial, target, scope).Apply([new(-1, target.ObjectId, 100)]));
        Assert.Contains("When Revealed", error.Message);
    }

    private static AbilityPowerState Damage(
        AbilityPowerState state, Card card, AbilityAdmission.AbilityAdmissionScope scope) =>
        AbilityPowerHealthTrace.ApplyPowerDamage(state, [card], 100,
            scope.World.Seats[0].IdentityCard, scope);

}
