using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Xunit;

namespace Marvel.Rules.Tests.Events;

public sealed class ActivityCompositionTests
{
    [Fact]
    public void IndependentInstructionsWithTheSameLocalIdKeepTheirExactMembers()
    {
        // Batch identity is an engine contract, independent of matching verbs or labels.
        List<GameEvent> first = Discard(7);
        List<GameEvent> second = Discard(8);
        string[] original = second.Select(EventJson.Write).ToArray();
        ActivityBatches.Append(first, second);
        ActivityBatches.Append(first, second);
        Assert.Equal(original, second.Select(EventJson.Write));
        Assert.Equal([7, 8, 8], first.OfType<ActivityCompleted>()
            .Select(receipt => Assert.Single(receipt.Participants[0].Cards)));
        AssertMembership(first);
        AssertMembership(first.Select(item => EventJson.Read(EventJson.Write(item))).ToList());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedInstructionsKeepTheirOwnershipThroughFurtherComposition(bool childFirst)
    {
        List<GameEvent> inner = Discard(8);
        var outer = new List<GameEvent>();
        if (!childFirst) outer.Add(Move(7));
        ActivityBatches.Append(outer, inner);
        if (childFirst) outer.Add(Move(7));
        outer.Add(Move(9));
        ActivityBatches.Complete(outer, 0, "Discard", 0);
        List<GameEvent> response = Discard(6);
        ActivityBatches.Append(response, outer);
        AssertMembership(response);
        Assert.Equal([8], response.OfType<ActivityCompleted>().ElementAt(1).Participants[0].Cards);
        Assert.Equal([7, 9], response.OfType<ActivityCompleted>().ElementAt(2).Participants[0].Cards);
        ActivityBatches.Complete(response, 0, "Discard", 0);
        Assert.Equal(3, response.OfType<ActivityCompleted>().Count());
    }

    [Fact]
    public void AStreamCanBeAppendedToItselfWithoutRewritingItsExistingOwnership()
    {
        List<GameEvent> events = Discard(7);
        string[] original = events.Select(EventJson.Write).ToArray();
        ActivityBatches.Append(events, events);
        Assert.Equal(original, events.Take(original.Length).Select(EventJson.Write));
        Assert.Equal(4, events.Count);
        AssertMembership(events);
    }

    [Fact]
    public void UnbatchedFactsKeepTheirPositionAndMeaning()
    {
        List<GameEvent> events = [new PhaseStarted("Player")];
        List<GameEvent> child = Discard(7);
        child.Add(new PhaseStarted("Villain"));
        ActivityBatches.Append(events, child);
        Assert.IsType<PhaseStarted>(events[0]);
        Assert.IsType<PhaseStarted>(events[3]);
        Assert.Null(events[0].ActivityBatch);
        Assert.Null(events[3].ActivityBatch);
        AssertMembership(events);
    }

    private static List<GameEvent> Discard(int card)
    {
        List<GameEvent> events = [Move(card)];
        ActivityBatches.Complete(events, 0, "Discard", 0);
        return events;
    }

    private static CardsMoved Move(int card) => new(
        AreaRef.Player("HandsArea", 0), AreaRef.Player("DiscardPile", 0), [new Landing(card, 0)])
    { Verb = "Discard", Trigger = "same instruction" };

    private static void AssertMembership(IReadOnlyList<GameEvent> events)
    {
        ActivityCompleted[] receipts = events.OfType<ActivityCompleted>().ToArray();
        Assert.Equal(receipts.Length, receipts.Select(item => item.Batch).Distinct().Count());
        foreach (ActivityCompleted receipt in receipts)
        {
            CardsMoved[] members = events.OfType<CardsMoved>()
                .Where(item => item.ActivityBatch == receipt.Batch).ToArray();
            Assert.Equal(receipt.Participants[0].Cards, members.SelectMany(item => item.Cards.Select(card => card.Card)));
            Assert.Equal(receipt.Batch, events.ToList().IndexOf(members[0]));
        }
        Assert.All(events.Where(item => item.ActivityBatch.HasValue), member =>
            Assert.Single(receipts, receipt => receipt.Batch == member.ActivityBatch));
    }
}
