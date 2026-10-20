using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingPotentialSelectorTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PotentialVillainSelectionIncludesEachGroupsPhysicalStages(bool separateLivingArea)
    {
        // Engine contract: topology owns physical stage membership. Potential-selector
        // analysis must not depend on which group happens to occupy the default area.
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var futureArea = world.CreateArea(DeckType.VillainDeck);
        foreach (var group in groups)
            World.MoveToTop(group.Stages[1], futureArea);
        if (separateLivingArea)
        {
            var livingArea = world.CreateArea(DeckType.VillainArea);
            foreach (var group in groups)
                World.MoveToTop(group.Villain, livingArea);
        }
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);

        foreach (var group in groups)
            Assert.True(AbilityProjection.PotentialVillainSelector(
                new AbilityCardSelection.Titled(world.Facts.Title(group.Villain.FaceId)), scope));
        Assert.True(AbilityProjection.PotentialVillainSelector(
            new AbilityCardSelection.Query(AbilityCardQuery.Villain), scope));
        Assert.False(AbilityProjection.PotentialVillainSelector(
            new AbilityCardSelection.Titled("Spider-Man"), scope));

        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
    }
}
