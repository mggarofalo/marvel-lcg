using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RecipientCostSuspensionTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [Rule("rr:initiating-abilities.step.5")]
    public void SettlingTheExhaustCostRetainsTheExplicitRecipientAcrossContinuation(int recipient, bool eliminated)
    {
        // "Pay the cost(s)." A health-loss procedure caused by exhausting must
        // settle before the draw; its continuation retains the committed recipient.
        var world = Deal("spider_man", "captain_marvel");
        var runner = AuthoredCards.Runner();
        Card source = world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Card ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        var health = new ExhaustHealthInterruption(source, ally);
        world.Abilities = health;
        int[] before = [.. world.Seats.Select(seat => seat.Hand.Cards.Count)];
        var resolution = runner.ExecuteAction(world, new PlayerAction(new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], [world.Seats[recipient].IdentityCard.ObjectId]));
        var cost = resolution.Events;
        Assert.True(health.Reached);
        Assert.False(source.Ready);
        Assert.DoesNotContain(cost, entry => entry.Verb == "Draw");
        Assert.Equal(before, world.Seats.Select(seat => seat.Hand.Cards.Count));
        Assert.False(resolution.IsComplete);
        ally.TakeDamage(-2);
        if (eliminated) world.Seats[recipient].Eliminated = true;
        var effect = new List<GameEvent>();
        var prompt = Sequence.Work(world, Cards, runner, effect);
        Assert.DoesNotContain(effect, entry => entry.Verb == "Exhaust");
        if (eliminated)
        {
            Assert.NotNull(prompt);
            Assert.DoesNotContain(effect, entry => entry.Verb == "Draw");
            Assert.False(source.Ready);
            Assert.Equal(world.Seats[0].IdentityCard.ObjectId, Assert.Single(prompt.Affordances).Id);
            Assert.DoesNotContain(prompt.Affordances, option => option.Id == world.Seats[recipient].IdentityCard.ObjectId);
            Assert.Equal(before, world.Seats.Select(seat => seat.Hand.Cards.Count));
            return;
        }
        Assert.Null(prompt);
        Assert.Contains(effect, entry => entry.Verb == "Draw");
        for (int seat = 0; seat < 2; seat++)
            Assert.Equal(before[seat] + (seat == recipient ? 1 : 0), world.Seats[seat].Hand.Cards.Count);
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
