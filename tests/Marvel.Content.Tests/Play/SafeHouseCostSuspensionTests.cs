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
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.step.7")]
    public void RecoveryWaitsForPaymentAndRetainsTheSelectedPhysicalIncarnation(bool reentered)
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
        int incarnation = chosen.Incarnation;
        if (reentered)
        {
            World.MoveToTop(chosen, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
            World.MoveToTop(chosen, discard);
            Assert.NotEqual(incarnation, chosen.Incarnation);
        }
        var events = new List<GameEvent>();
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.DoesNotContain(events, entry => entry.Verb == "Exhaust");
        Assert.False(source.Ready);
        Assert.Same(reentered ? discard : world.Seats[1].Hand, chosen.Area);
        Assert.Same(discard, other.Area);
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.Operation is AbilityContinuationAgendaOperation);
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
