using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class ContinuationExtensionTests
{
    [Fact]
    public void AChildCanExtendItsWorkAfterItsParentHasAlreadyYielded()
    {
        // Each child segment is inserted before its executing frame; the parent's
        // original dependency therefore remains ordered behind every extension.
        var world = new World(new Printed(), 1);
        var abilities = new ExtendedChild();
        var occurrence = new Occurrence(1, []);
        var parent = world.Agenda.Executions.BeginBatch(occurrence);
        var child = world.Agenda.Executions.Begin(occurrence, new(0, AbilityType.Interrupt, 0));
        Schedule(world, child, parent: false);
        child.Yield([]);
        Schedule(world, parent, parent: true);
        parent.Yield([]);
        Sequence.Finish(world, world.Facts, abilities, []);
        Assert.Collection(abilities.Order, item => Assert.Equal("child1", item),
            item => Assert.Equal("child2", item), item => Assert.Equal("parent", item));
        Assert.True(parent.IsComplete);
        Assert.True(child.IsComplete);
        Assert.False(world.Agenda.IsBusy);
    }

    private static void Schedule(World world, AbilityExecution execution, bool parent) =>
        world.Agenda.ThenContinuation(new PhaseStep(
            new AbilityContinuationAgendaOperation(AbilityContinuationStage.Resume)
            { Continuation = new Continuation(parent) { Execution = execution } }, 1, 1, Plan: true), execution.Occurrence);

    private sealed record Continuation(bool Parent) : CardAbilityContinuation;

    private sealed class ExtendedChild : NoCardAbilities
    {
        internal List<string> Order { get; } = [];
        private int childSegments;

        public override IReadOnlyList<GameEvent> ResumeAbility(World world, PhaseStep continuation)
        {
            var saved = (Continuation)continuation.Require<AbilityContinuationAgendaOperation>().Continuation!;
            var execution = saved.Execution!;
            world.Agenda.Executions.Resume(execution);
            if (saved.Parent)
            {
                Order.Add("parent");
                execution.Finish([]);
            }
            else
            {
                Order.Add(++childSegments == 1 ? "child1" : "child2");
                if (childSegments == 1)
                {
                    Schedule(world, execution, parent: false);
                    execution.Yield([]);
                }
                else execution.Finish([]);
            }
            return [];
        }
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Ally;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
