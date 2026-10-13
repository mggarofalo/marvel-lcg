using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Session.Tests;

public sealed class ReplayHistoryActivityTests
{
    [Fact]
    public void SeparateDecisionStreamsKeepDistinctInstructionOwnershipInOneHistoryUnit()
    {
        // Synthetic history contract: ids are response-local and may repeat across decisions.
        // No card behavior or tabletop timing is asserted by this fixture.
        var facts = new EmptyFacts();
        var world = new World(facts, 1);
        Seat seat = world.CreateSeat("Player");
        seat.IdentityCard = world.CreateCard("synthetic-identity", seat.Hero);
        Game game = Game.BeginWithoutCardAbilities(world, facts);
        var prompt = game.Pending!;
        JournalStep step = JournalStep.From(0, prompt, Decision.Decline, [], 0, "synthetic state");
        var unit = new JournalUnit("phase_step", "complete", 0, 0, 0, "Mulligan", [step, step], []);
        var history = new ReplayHistory();
        history.ObserveRoot(unit, step, game, prompt, Decision.Decline);
        GameEvent[] first = Discard(7);
        GameEvent[] second = Discard(8);
        history.ObserveEvents(first);
        history.ObserveEvents(second);
        var units = new List<HistoryUnitInspection>();
        history.Append(units, unit, 0);
        IReadOnlyList<GameEvent> events = Assert.Single(units).Events;
        ActivityCompleted[] receipts = events.OfType<ActivityCompleted>().ToArray();
        Assert.Equal(2, receipts.Length);
        Assert.NotEqual(receipts[0].Batch, receipts[1].Batch);
        foreach (ActivityCompleted receipt in receipts)
            Assert.Equal(receipt.Participants[0].Cards,
                events.OfType<CardsMoved>().Where(item => item.ActivityBatch == receipt.Batch)
                    .SelectMany(item => item.Cards.Select(card => card.Card)));
        Assert.Equal(0, Assert.IsType<ActivityCompleted>(second[1]).Batch);
    }

    private static GameEvent[] Discard(int card) =>
    [
        new CardsMoved(AreaRef.Player("HandsArea", 0), AreaRef.Player("DiscardPile", 0), [new Landing(card, 0)])
            { ActivityBatch = 0, Verb = "Discard" },
        new ActivityCompleted(0, [new ActivityParticipant(0, 1, [card])]) { Verb = "Discard" },
    ];

    private sealed class EmptyFacts : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Hero;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
