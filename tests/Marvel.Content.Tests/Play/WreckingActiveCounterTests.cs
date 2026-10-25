using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingActiveCounterTests
{
    [Rule("rr:move.1")]
    [Rule("rr:move.2")]
    [Fact]
    public void MovementRequiresADifferentLivingPhysicalDestination()
    {
        // "it cannot move to its same (current) placement"; without a valid
        // source or destination "the move cannot be made."
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var foreign = world.CreateCard(groups[0].Villain.FaceId, world.AreaOf(DeckType.VillainArea));
        var before = Snapshot(world);
        List<GameEvent> events = [];
        Card[] invalid = [groups[1].Villain, groups[0].Stages[1], groups[0].Scheme, foreign];

        foreach (var card in invalid)
        {
            Assert.False(EncounterActiveCounter.CanMove(world, card));
            Assert.False(EncounterActiveCounter.Move(world, card, "test", events));
        }

        Assert.Empty(events);
        Assert.Equal(before.Digest, world.Digest().Canonical());
        Assert.Equal(before.Random, world.Random.Generator.WordsConsumed);
        Assert.Equal(before.Pending, world.Agenda.Outstanding);
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    [Fact]
    public void MovementPublishesExactCounterChangesAndPreservesPhysicalOwnership()
    {
        // pack:mc03:new-rules: "When the villain is dealt a boost card,
        // it is dealt from the active villain's deck."
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var prior = groups[1];
        var next = groups[2];
        List<GameEvent> events = [];
        var before = Snapshot(world);

        Assert.True(EncounterActiveCounter.CanMove(world, next.Villain));
        Assert.Equal(before.Digest, world.Digest().Canonical());
        Assert.Equal(before.Random, world.Random.Generator.WordsConsumed);
        Assert.Equal(before.Pending, world.Agenda.Outstanding);
        Assert.True(EncounterActiveCounter.Move(world, next.Villain, "printed boost", events));

        Assert.Same(next, world.EncounterTopology!.Active);
        Assert.Same(next.Deck, EncounterRouting.Deck(world));
        Assert.Collection(events,
            item => Assert.Equal(new FieldSet(prior.Villain.ObjectId, "active_villain", 1, 0)
                { Trigger = "printed boost", Verb = "Remove" }, item),
            item => Assert.Equal(new FieldSet(next.Villain.ObjectId, "active_villain", 0, 1)
                { Trigger = "printed boost", Verb = "Place" }, item));
        Assert.All(groups, group =>
            Assert.All(group.Members, card => Assert.Same(group, world.EncounterTopology.ForCard(card))));
        Assert.Equal(before.Random, world.Random.Generator.WordsConsumed);
        Assert.Equal(before.Pending, world.Agenda.Outstanding.ToArray());
    }

    [Rule("rr:move.2")]
    [Fact]
    public void PromotionReplacesThePhysicalDestinationAndAnAbsentSourceCannotMove()
    {
        // "If there is no valid source or destination for a move, the move cannot be made."
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var old = groups[0].Villain;
        var next = groups[0].Stages[1];
        World.MoveToTop(old, world.AreaOf(DeckType.RemovedArea));
        World.MoveToTop(next, world.AreaOf(DeckType.VillainArea));
        Assert.False(EncounterActiveCounter.CanMove(world, old));
        Assert.True(EncounterActiveCounter.CanMove(world, next));
        World.MoveToTop(groups[1].Villain, world.AreaOf(DeckType.RemovedArea));
        var before = Snapshot(world);
        List<GameEvent> events = [];

        Assert.False(EncounterActiveCounter.CanMove(world, next));
        Assert.False(EncounterActiveCounter.Move(world, next, "test", events));

        Assert.Empty(events);
        Assert.Equal(before.Digest, world.Digest().Canonical());
        Assert.Equal(before.Random, world.Random.Generator.WordsConsumed);
        Assert.Equal(before.Pending, world.Agenda.Outstanding);
    }

    private static (string Digest, long Random, PhaseStep[] Pending) Snapshot(World world) =>
        (world.Digest().Canonical(), world.Random.Generator.WordsConsumed, world.Agenda.Outstanding.ToArray());
}
