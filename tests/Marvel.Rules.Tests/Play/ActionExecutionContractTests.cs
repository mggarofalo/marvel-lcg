using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class ActionExecutionContractTests
{
    [Fact]
    public void EmptyEventsDoNotCompleteAnActionWithOwnedWork()
    {
        // Synthetic execution contract: event count says nothing about completion.
        var world = new World(new Printed(), 1);
        world.Agenda.Add(new PhaseStep(new LifecycleOperation(LifecycleKind.CardPlayed), 1, 1));
        var owner = world.Agenda.Current!.Value;
        var occurrence = world.Agenda.Occurrence!;
        var abilities = new Actions { ScheduleWork = true };
        var action = new PlayerAction(new(7, AbilityType.Action, 0), [], []);

        AbilityExecutionResult result = abilities.ExecuteAction(world, action, occurrence);

        Assert.Empty(result.Events);
        Assert.False(result.IsComplete);
        Assert.Throws<InvalidOperationException>(() => world.Agenda.Advance(owner, occurrence));
        Assert.True(world.Agenda.IsOutstanding(owner, occurrence));
        Assert.Equal(2, world.Agenda.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ARejectedUnstartedActionDoesNotPoisonTheNextInvocation(bool missingWork)
    {
        var world = new World(new Printed(), 1);
        var abilities = new Actions { Reject = true, MissingWork = missingWork };
        var action = new PlayerAction(new(7, AbilityType.Action, 0), [11], [13],
            new Dictionary<string, long> { ["X"] = 2 }, [new(11, 0, "BB")]);
        string digest = world.Digest().Canonical();
        string random = System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState());

        if (missingWork)
            Assert.Throws<InvalidOperationException>(() => abilities.ExecuteAction(world, action));
        else
            Assert.Throws<ArgumentException>(() => abilities.ExecuteAction(world, action));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random, System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState()));
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
        Assert.False(world.Agenda.IsBusy);

        abilities.Reject = false;
        AbilityExecutionResult accepted = abilities.ExecuteAction(world, action);
        Assert.True(accepted.IsComplete);
        Decision answer = Assert.IsType<Decision>(abilities.Accepted);
        Assert.Equal(31, answer.Affordance);
        Assert.Equal(action.Chosen, answer.Targets);
        Assert.Equal(action.Paying, answer.Spent);
        Assert.Equal(action.DefinedValues, answer.DefinedValues);
        Assert.Equal(action.Allocated, answer.Allocated);
    }

    private sealed class Actions : NoCardAbilities
    {
        internal bool ScheduleWork { get; init; }
        internal bool Reject { get; set; }
        internal bool MissingWork { get; init; }
        internal Decision? Accepted { get; private set; }

        public override Affordance Describe(World world, PendingAbility ability) =>
            new(31, "synthetic", ability.Card, ability.Player, "contract");

        public override AbilityExecutionResult Act(World world, AbilityExecution invocation, Decision answer)
        {
            if (Reject)
            {
                if (MissingWork) return invocation.Yield([]);
                throw new ArgumentException("synthetic admission rejection", nameof(answer));
            }
            Accepted = answer;
            if (ScheduleWork)
                world.Agenda.Add(new PhaseStep(new LifecycleOperation(LifecycleKind.CardPlayed), 1, 2));
            return invocation.Finish([]);
        }
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Hero;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
