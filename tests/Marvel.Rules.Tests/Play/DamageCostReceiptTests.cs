using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class DamageCostReceiptTests
{
    [Fact]
    public void OnlyTheExactActiveCostOwnerMaySettleOnce()
    {
        // Engine ownership contract: placement completion is not permission for another invocation to settle it.
        var (world, target) = Board();
        var occurrence = new Occurrence(1, []);
        var owner = world.Agenda.Executions.BeginBatch(occurrence);
        var receipt = DamagePlacement.BeginCost(world, world.Facts,
            new(target, target, 2, "synthetic", "Damage"), owner, []);
        Assert.True(receipt.IsPlaced);
        Assert.True(receipt.PlacementExecution.IsComplete);
        Assert.Equal(2, receipt.Taken);
        Assert.Equal(2, target.Damage);
        Assert.False(receipt.IsComplete);
        var foreign = Board().World;
        foreign.Agenda.Executions.RunBatch(occurrence, other =>
        {
            Assert.Throws<InvalidOperationException>(() => receipt.Settle(other, []));
            return other.Finish([]);
        });
        world.Agenda.Executions.RunBatch(occurrence, sibling =>
        {
            Assert.Throws<InvalidOperationException>(() => receipt.Settle(sibling, []));
            return sibling.Finish([]);
        });
        Assert.False(receipt.IsSettling);
        Assert.Throws<InvalidOperationException>(() =>
            DamageCostSettlement.Settle(world, owner, [receipt, receipt], []));
        Assert.False(receipt.IsSettling);
        receipt.Settle(owner, []);
        Assert.True(receipt.IsComplete);
        Assert.Throws<InvalidOperationException>(() => receipt.Settle(owner, []));
        owner.Finish([]);
        Assert.True(owner.IsComplete);
        Assert.Throws<InvalidOperationException>(() => receipt.Settle(owner, []));
        Assert.Equal(2, target.Damage);
    }

    [Fact]
    public void AnUnreleasedDamageCostPreventsItsOwnerFromCompleting()
    {
        var (world, target) = Board();
        world.Agenda.Add(new PhaseStep(new LifecycleOperation(LifecycleKind.CardPlayed), 1, 1));
        var frame = world.Agenda.Current!.Value;
        var occurrence = world.Agenda.Occurrence!;
        var owner = world.Agenda.Executions.BeginBatch(occurrence);
        var receipt = DamagePlacement.BeginCost(world, world.Facts,
            new(target, target, 2, "synthetic", "Damage"), owner, []);
        owner.Finish([]);
        Assert.True(receipt.IsPlaced);
        Assert.False(receipt.IsComplete);
        Assert.False(owner.IsComplete);
        Assert.Throws<InvalidOperationException>(() => world.Agenda.Advance(frame, occurrence));
        Assert.True(world.Agenda.IsOutstanding(frame, occurrence));
        var failure = Assert.Throws<InvalidOperationException>(() =>
            Sequence.Work(world, world.Facts, world.Abilities, []));
        Assert.Contains("made no progress", failure.Message, StringComparison.Ordinal);
        Assert.True(world.Agenda.IsOutstanding(frame, occurrence));
        Assert.False(owner.IsComplete);
    }

    [Fact]
    public void DistinctCostRecipientsBothReachTheirOwnDefeatSettlement()
    {
        var world = new World(new Printed(CardKind.Minion), 1) { Abilities = new NoCardAbilities() };
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var first = world.CreateCard("first", area);
        var second = world.CreateCard("second", area);
        first.TakeDamage(9);
        second.TakeDamage(9);
        var occurrence = new Occurrence(1, []);
        var owner = world.Agenda.Executions.BeginBatch(occurrence);
        var left = DamagePlacement.BeginCost(world, world.Facts,
            new(first, first, 1, "synthetic", "Damage"), owner, []);
        var right = DamagePlacement.BeginCost(world, world.Facts,
            new(first, second, 1, "synthetic", "Damage"), owner, []);
        Assert.Equal(2, area.Cards.Count);
        DamageCostSettlement.Settle(world, owner, [left, right], []);
        owner.Finish([]);
        Assert.True(owner.IsComplete);
        Assert.True(left.IsComplete);
        Assert.True(right.IsComplete);
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }, occurrence.Defeats.Select(defeat => defeat.Card));
    }

    private static (World World, Card Target) Board()
    {
        var world = new World(new Printed(), 1) { Abilities = new NoCardAbilities() };
        return (world, world.CreateCard("synthetic", world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0)));
    }

    private sealed class Printed(CardKind kind = CardKind.Ally) : ICardFacts
    {
        public CardKind Kind(string faceId) => kind;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            attribute == "HP" ? 10 : fallback;
    }
}
