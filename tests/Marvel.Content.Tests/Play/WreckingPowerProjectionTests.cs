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
        var (world, groups, scope) = Fixture();
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
        Assert.Equal(new[] { groups[0].Stages[1], groups[1].Villain, groups[2].Villain, groups[3].Villain },
            AbilityPowerTrace.PowerEvery(new AbilityCardSelection.Query(AbilityCardQuery.Enemies), scope, promoted));
        Assert.Same(groups[1].Villain,
            AbilityPowerTrace.PowerFind(new AbilityCardSelection.Query(AbilityCardQuery.Villain), scope, promoted));

        var defeated = Damage(promoted, groups[0].Stages[1], scope);
        Assert.Equal(-1, defeated.EncounterStages![groups[0].Villain.ObjectId]);
        Assert.Equal(groups[1].Villain.ObjectId, defeated.CurrentVillain);
        Assert.False(defeated.Finished);
        Assert.Contains(groups[0].Scheme.ObjectId, defeated.Discarded);
        Assert.Contains(hostage.ObjectId, defeated.Discarded);
        Assert.Equal(groups.Skip(1).Select(group => group.Villain),
            AbilityPowerTrace.PowerEvery(new AbilityCardSelection.Query(AbilityCardQuery.Enemies), scope, defeated));
        Assert.Equal(groups[0].Villain.ObjectId, initial.EncounterStages![groups[0].Villain.ObjectId]);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng, world.Random.Generator.WordsConsumed);
    }

    [Fact]
    public void ActiveFinalDefeatUsesProjectedThreatForUniqueReplacement()
    {
        var (_, groups, scope) = Fixture();
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
        var (_, groups, scope) = Fixture();
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

    private static AbilityPowerState Damage(
        AbilityPowerState state, Card card, AbilityAdmission.AbilityAdmissionScope scope) =>
        AbilityPowerHealthTrace.ApplyPowerDamage(state, [card], 100,
            scope.World.Seats[0].IdentityCard, scope);

    private static (World, EncounterDeckGroup[], AbilityAdmission.AbilityAdmissionScope) Fixture()
    {
        var world = Hero("01001a");
        string[][] faces = [["07002", "07003", "07004"], ["07017", "07018", "07019"],
            ["07032", "07033", "07034"], ["07046", "07047", "07048"]];
        var groups = faces.Select(pair => Group(world, pair)).ToArray();
        world.BindEncounterTopology(new EncounterTopology(world, groups, groups[1].Villain));
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var query = new AbilityQueryContext(world, source, new Occurrence(0, []), 0,
            source.Incarnation, null, null, null, []);
        var expression = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var scope = new AbilityAdmission.AbilityAdmissionScope(new AbilityAdmissionContext(
            AbilityLowering.Book(AuthoredCards.Book), world.ResourceAbilities, expression, new(), null), []);
        return (world, groups, scope);
    }

    private static EncounterDeckGroup Group(World world, string[] faces)
    {
        var first = world.CreateCard(faces[0], world.AreaOf(DeckType.VillainArea));
        var next = world.CreateCard(faces[1], world.AreaOf(DeckType.VillainDeck));
        return new(first, world.CreateCard(faces[2], world.AreaOf(DeckType.SideSchemesArea)),
            world.CreateArea(DeckType.EncounterDeck), world.CreateArea(DeckType.EncounterDiscardPile),
            [], [first, next]);
    }
}
