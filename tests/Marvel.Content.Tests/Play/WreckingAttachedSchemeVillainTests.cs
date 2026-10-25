using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingAttachedSchemeVillainTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void SourceHostSelectsPhysicalPairIndependentlyOfActiveCounter(int groupIndex)
    {
        var (world, groups, initial) = WreckingProjectionFixture.Create();
        var source = world.CreateCard("07005", world.AreaOf(
            DeckType.UpgradesArea, PlayArea.Villains, groups[groupIndex].Scheme.ObjectId));
        var context = initial.Context.WithQuery(initial.Context.Query with
            { Source = source, SourceIncarnation = source.Incarnation });
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        var query = new AbilityCardSelection.Query(AbilityCardQuery.VillainOfAttachedScheme);
        var area = new AreaProjectionState(context);
        var power = AbilityPowerStateFactory.Initial(scope);
        Assert.Same(groups[groupIndex].Villain, Assert.Single(AbilityAttachedSchemeVillainQuery.Live(context.Query)));
        Assert.Same(groups[groupIndex].Villain, Assert.Single(AbilityProjectedStateQueries.ProjectedEvery(query, area, context)));
        Assert.Same(groups[groupIndex].Villain, AbilityPowerSelectionTrace.PowerFind(query, scope, power));

        var next = groups[groupIndex].Stages[1];
        area.Departed.Add(groups[groupIndex].Villain.ObjectId);
        area.Entered.Add(next.ObjectId);
        power.Discarded.Add(groups[groupIndex].Villain.ObjectId);
        power.EncounterStages![groups[groupIndex].Villain.ObjectId] = next.ObjectId;
        Assert.Same(next, Assert.Single(AbilityProjectedStateQueries.ProjectedEvery(query, area, context)));
        Assert.Same(next, AbilityPowerSelectionTrace.PowerFind(query, scope, power));
    }

    [Fact]
    public void ProjectedReattachmentUsesNewHostAndDepartureHasNoTarget()
    {
        var (world, groups, initial) = WreckingProjectionFixture.Create();
        var source = world.CreateCard("07005", world.AreaOf(
            DeckType.UpgradesArea, PlayArea.Villains, groups[0].Scheme.ObjectId));
        var context = initial.Context.WithQuery(initial.Context.Query with
            { Source = source, SourceIncarnation = source.Incarnation });
        var area = new AreaProjectionState(context);
        area.Hosts[source.ObjectId] = groups[3].Scheme.ObjectId;
        Assert.Same(groups[3].Villain, Assert.Single(AbilityAttachedSchemeVillainQuery.InAreas(context, area)));
        area.Departed.Add(source.ObjectId);
        Assert.Empty(AbilityAttachedSchemeVillainQuery.InAreas(context, area));
        Assert.Same(groups[0].Villain, Assert.Single(AbilityAttachedSchemeVillainQuery.Live(context.Query)));
    }
}
