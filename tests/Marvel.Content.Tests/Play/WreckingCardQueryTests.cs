using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic topology contracts; these do not admit the Wrecking Crew pack.</summary>
public sealed class WreckingCardQueryTests : CoreCardTestBase
{
    [Fact]
    public void VillainReferenceAndItsAttachmentsFollowCounterRatherThanSourceOrAreaOrder()
    {
        // pack:mc03:new-rules.the-active-villain: "the villain" refers to the
        // active villain. The source's physical encounter ownership does not change it.
        var world = Hero("01001a");
        var groups = Groups(world);
        var source = world.CreateCard("01002", world.Seats[0].Hand);
        var evaluator = Query(world, source);
        var attachments = groups.Select(group => world.CreateCard("07005",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, group.Villain.ObjectId))).ToArray();
        var topology = new EncounterTopology(world, groups, groups[2].Villain);
        world.BindEncounterTopology(topology);

        foreach (int index in new[] { 2, 3, 1, 0 })
        {
            topology.SelectActive(groups[index].Villain, "test", []);
            string before = world.Digest().Canonical();
            long words = world.Random.Generator.WordsConsumed;
            Assert.Same(groups[index].Villain, Assert.Single(evaluator.Evaluate(AbilityCardQuery.Villain)));
            Assert.Same(groups[index].Scheme, Assert.Single(evaluator.Evaluate(AbilityCardQuery.ActiveVillainSideScheme)));
            Assert.Same(attachments[index], Assert.Single(evaluator.Evaluate(AbilityCardQuery.AttachmentsOnVillain)));
            Assert.Same(groups[index].Villain,
                Assert.Single(Query(world, groups[(index + 1) % groups.Length].Villain).Evaluate(AbilityCardQuery.Villain)));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(words, world.Random.Generator.WordsConsumed);
        }
    }

    [Rule("rr:attack-player-ability-type.4")]
    [Rule("rr:guard.1")]
    [Theory]
    [InlineData("07008")]
    [InlineData("07023")]
    [InlineData("07037")]
    [InlineData("07052")]
    public void EnemySelectionKeepsInactiveVillainsAndGuardExcludesEveryVillain(string guardFace)
    {
        // pack:mc03:new-rules.the-active-villain permits attacking any villain.
        // Guard prevents attacking the villain, including inactive villains.
        var world = Hero("01001a");
        var groups = Groups(world);
        world.BindEncounterTopology(new EncounterTopology(world, groups, groups[2].Villain));
        var evaluator = Query(world, world.Seats[0].IdentityCard);
        Assert.Equal(groups.Select(group => group.Villain.ObjectId),
            evaluator.Evaluate(AbilityCardQuery.Enemies).Select(card => card.ObjectId));
        Assert.Equal(groups.Select(group => group.Villain.ObjectId),
            BasicPowers.Attackable(world, Cards, 0).Select(card => card.ObjectId));
        var guard = world.CreateCard(guardFace, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var runner = AuthoredCards.Runner();
        Assert.Empty(runner.ExecuteReveal(world, guard, 0).Events);
        Assert.Same(guard, Assert.Single(BasicPowers.Attackable(world, Cards, 0)));
        Assert.Equal(5, evaluator.Evaluate(AbilityCardQuery.Enemies).Count);
        World.MoveToTop(guard, world.AreaOf(DeckType.RemovedArea));
        Assert.Equal(groups.Select(group => group.Villain.ObjectId),
            BasicPowers.Attackable(world, Cards, 0).Select(card => card.ObjectId));
    }

    [Fact]
    public void OrdinaryVillainAndAttachmentQueriesRetainTheirSingleVillainMeaning()
    {
        var world = Hero("01001a");
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var attachment = world.CreateCard("01098",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
        var evaluator = Query(world, world.Seats[0].IdentityCard);
        Assert.Same(villain, Assert.Single(evaluator.Evaluate(AbilityCardQuery.Villain)));
        Assert.Same(attachment, Assert.Single(evaluator.Evaluate(AbilityCardQuery.AttachmentsOnVillain)));
    }

    private static CardQueryEvaluator Query(World world, Card source) =>
        new(new AbilityQueryContext(world, source, new Occurrence(0, []), 0,
            source.Incarnation, null, null, null, []), null);

    private static EncounterDeckGroup[] Groups(World world)
    {
        string[][] faces = [["07002", "07004"], ["07017", "07019"],
            ["07032", "07034"], ["07046", "07048"]];
        return faces.Select(pair => new EncounterDeckGroup(
            world.CreateCard(pair[0], world.AreaOf(DeckType.VillainArea)),
            world.CreateCard(pair[1], world.AreaOf(DeckType.SideSchemesArea)),
            world.CreateArea(DeckType.EncounterDeck), world.CreateArea(DeckType.EncounterDiscardPile), [])).ToArray();
    }
}
