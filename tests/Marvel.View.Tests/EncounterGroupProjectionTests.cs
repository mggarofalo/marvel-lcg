using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

public sealed class EncounterGroupProjectionTests
{
    [Fact]
    public void PublicGroupsExplainOwnershipAndActiveCounterWithoutRevealingDeckMembers()
    {
        var (world, groups) = Board();
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        int agenda = world.Agenda.Count;
        var visible = WorldProjection.For(world, null, [], ViewScope.None).World;
        Assert.Equal(4, visible.EncounterGroups.Count);
        Assert.Equal(groups[2].Villain.ObjectId, Assert.Single(visible.EncounterGroups, group => group.Active).Villain);
        for (int index = 0; index < 4; index++)
        {
            var actual = visible.EncounterGroups[index];
            Assert.Equal(groups[index].Villain.ObjectId, actual.Group);
            Assert.Equal(groups[index].Scheme.ObjectId, actual.Scheme);
            Assert.Equal(groups[index].Deck.Id, actual.Deck);
            Assert.Equal(groups[index].Discard.Id, actual.Discard);
            Assert.False(actual.Defeated);
            Assert.Equal([groups[index].Villain.ObjectId], actual.Stages);
            Assert.All(visible.Areas.Single(area => area.Id == actual.Deck).Cards,
                card => { Assert.Null(card.Id); Assert.Null(card.Face); });
        }
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(agenda, world.Agenda.Count);
    }

    [Fact]
    public void PublishedGroupsDetachFromLiveMovementAndConsumerCollectionMutation()
    {
        var (world, groups) = Board();
        var first = WorldProjection.For(world, null, [], ViewScope.None).World;
        var stageIds = Assert.IsType<int[]>(first.EncounterGroups[0].Stages);
        stageIds[0] = 999;
        Assert.Equal(groups[0].Villain.ObjectId,
            WorldProjection.For(world, null, [], ViewScope.None).World.EncounterGroups[0].Stages[0]);
        world.EncounterTopology!.SelectActive(groups[1].Villain, "test", []);
        World.MoveToTop(groups[0].Villain, world.AreaOf(DeckType.RemovedArea));
        var next = WorldProjection.For(world, null, [], ViewScope.None).World;
        Assert.True(first.EncounterGroups[2].Active);
        Assert.False(first.EncounterGroups[0].Defeated);
        Assert.True(next.EncounterGroups[1].Active);
        Assert.True(next.EncounterGroups[0].Defeated);
        Assert.Null(next.EncounterGroups[0].Villain);
    }

    [Fact]
    public void SyntheticConcealedInitialStageCannotLeakIdentityThroughGroupMetadata()
    {
        var (world, groups) = Board();
        // Synthetic visibility boundary: an initial stage in a private hand is not a played scenario.
        World.MoveToTop(groups[0].Villain, world.Seats[0].Hand);
        var visible = WorldProjection.For(world, null, [], ViewScope.None).World;
        Assert.DoesNotContain(visible.EncounterGroups, group => group.Group == groups[0].Villain.ObjectId);
        Assert.DoesNotContain(visible.EncounterGroups.SelectMany(group => group.Stages),
            id => id == groups[0].Villain.ObjectId);
    }

    [Fact]
    public void SyntheticConcealedFutureStageIsAbsentFromPublishedMembership()
    {
        var (world, groups) = Board(future: true);
        var hidden = groups[0].Stages[1];
        World.MoveToTop(hidden, world.Seats[0].Deck);
        var visible = WorldProjection.For(world, null, [], ViewScope.None).World;
        Assert.Equal([groups[0].Villain.ObjectId], visible.EncounterGroups[0].Stages);
        Assert.DoesNotContain(visible.EncounterGroups.SelectMany(group => group.Stages), id => id == hidden.ObjectId);
    }

    [Fact]
    public void BoardAndActivityDescribeActiveCounterWithoutNumericStorageFields()
    {
        var (world, groups) = Board();
        World.MoveToTop(groups[0].Members[0], world.AreaOf(DeckType.RevealingArea));
        var visible = WorldProjection.For(world, null, [], ViewScope.None).World;
        var board = BoardPresentation.From(visible);
        Assert.DoesNotContain(board.Areas.SelectMany(area => area.Cards).SelectMany(card => card.Fields),
            field => field.Name is "ACTIVE VILLAIN" or "ENCOUNTER VILLAIN");
        var happened = new Marvel.Rules.Events.FieldSet(groups[0].Villain.ObjectId, "active_villain", 0, 1);
        Assert.Equal("villain0 became the active villain.", EventPresenter.Present(happened, visible).Summary);
        var ids = Assert.IsType<int[]>(board.EncounterGroups[0].Stages);
        ids[0] = 999;
        Assert.Equal(groups[0].Villain.ObjectId, visible.EncounterGroups[0].Stages[0]);
    }

    private static (World World, EncounterDeckGroup[] Groups) Board(bool future = false)
    {
        var world = new World(new Facts(), 1);
        world.CreateSeat("Player");
        var groups = Enumerable.Range(0, 4).Select(index =>
        {
            var villain = world.CreateCard($"villain{index}", world.AreaOf(DeckType.VillainArea));
            var scheme = world.CreateCard("side", world.AreaOf(DeckType.SideSchemesArea));
            var deck = world.CreateArea(DeckType.EncounterDeck);
            var discard = world.CreateArea(DeckType.EncounterDiscardPile);
            Card[] stages = future
                ? [villain, world.CreateCard($"villain{index}B", world.AreaOf(DeckType.VillainDeck))]
                : [villain];
            return new EncounterDeckGroup(villain, scheme, deck, discard,
                [world.CreateCard("secret", deck)], stages);
        }).ToArray();
        world.BindEncounterTopology(new EncounterTopology(world, groups, groups[2].Villain));
        return (world, groups);
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId.StartsWith("villain", StringComparison.Ordinal)
            ? CardKind.EncounterVillain : faceId == "side" ? CardKind.EncounterSideScheme : CardKind.Minion;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
