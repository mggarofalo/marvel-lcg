using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SafeHouseCostSuspensionTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData("unchanged", false)]
    [InlineData("hand", false)]
    [InlineData("hand-and-back", false)]
    [InlineData("play-and-back", false)]
    [InlineData("unchanged", true)]
    [InlineData("source-hand", false)]
    [InlineData("source-reentry", false)]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.step.7")]
    public void RecoveryWaitsForPaymentAndRetainsTheSelectedPhysicalIncarnation(string movement, bool nested)
    {
        // "Pay the cost(s)" precedes the ability resolving. Printed 08005's
        // "that card" retains the exact copy chosen on the cost side of the arrow.
        var world = Deal("spider_man", "captain_marvel");
        var runner = SafeHouseCostChoiceTests.Runner();
        var source = world.CreateCard("08005", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1);
        var other = world.CreateCard("08006", discard);
        var chosen = world.CreateCard("08017", discard);
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        ally.TakeDamage(2);
        var health = new ExhaustHealthInterruption(source, ally);
        world.Abilities = health;
        var action = Assert.Single(runner.Actions(world, 1));
        var result = runner.ExecuteAction(world, new PlayerAction(action, [], [chosen.ObjectId]));
        Assert.True(health.Reached);
        Assert.False(result.IsComplete);
        Assert.False(source.Ready);
        Assert.Same(discard, chosen.Area);
        Assert.Same(discard, other.Area);
        ally.TakeDamage(-2);
        MoveDuringPayment(world, source, chosen, discard, movement);
        if (nested)
        {
            // A distinct accepted action must not overwrite the suspended cost receipt.
            var nestedSource = world.CreateCard("08005", source.Area);
            var nestedResult = runner.ExecuteAction(world, new PlayerAction(
                new PendingAbility(nestedSource.ObjectId, AbilityType.Action, 1), [], [other.ObjectId]));
            Assert.True(nestedResult.IsComplete);
            Assert.False(nestedSource.Ready);
            Assert.Same(world.Seats[1].Hand, other.Area);
        }
        var events = new List<GameEvent>();
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.DoesNotContain(events, entry => entry.Verb == "Exhaust");
        Assert.Equal(movement.StartsWith("source", StringComparison.Ordinal), source.Ready);
        Assert.Same(movement == "play-and-back" ? discard : world.Seats[1].Hand, chosen.Area);
        Assert.Same(nested ? world.Seats[1].Hand : discard, other.Area);
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.Operation is AbilityContinuationAgendaOperation);
    }

    private static void MoveDuringPayment(World world, Card source, Card chosen, Area discard, string movement)
    {
        if (movement.StartsWith("source", StringComparison.Ordinal))
        {
            int sourceIncarnation = source.Incarnation;
            var area = source.Area;
            World.MoveToTop(source, world.Seats[1].Hand);
            if (movement == "source-reentry") World.MoveToTop(source, area);
            Assert.Equal(movement == "source-reentry", sourceIncarnation != source.Incarnation);
            return;
        }
        if (movement == "unchanged") return;
        int incarnation = chosen.Incarnation;
        World.MoveToTop(chosen, movement == "play-and-back"
            ? world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1)
            : world.Seats[1].Hand);
        if (movement != "hand") World.MoveToTop(chosen, discard);
        Assert.Equal(movement == "play-and-back", incarnation != chosen.Incarnation);
    }

    private sealed class ExhaustHealthInterruption(Card source, Card ally) : NoCardAbilities
    {
        internal bool Reached { get; private set; }
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card) =>
            card == source && source.Ready
                ? [new(EffectSource.ConstantAbility, "health", 1, source.ObjectId, ally.ObjectId)] : [];

        public override AbilityExecutionResult WouldBeDefeated(World world, AbilityExecution entry,
            Card target, DefeatCause cause)
        {
            Assert.Same(ally, target);
            Assert.False(source.Ready);
            Reached = true;
            world.Agenda.Add(new PhaseStep(new LifecycleOperation(LifecycleKind.CardPlayed), 0, 1, Plan: true));
            return world.Agenda.Executions.FinishBatch(entry, []);
        }
    }
}
