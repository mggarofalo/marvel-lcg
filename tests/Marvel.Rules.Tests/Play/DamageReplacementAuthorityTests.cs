using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class DamageReplacementAuthorityTests
{
    [Fact]
    public void OnlyTheActiveReplacementInvocationCanWriteItsAmount()
    {
        // Engine ownership contract: a receipt is not a transferable write capability.
        var (world, target) = Board();
        var abilities = new SuspendedReplacement();
        world.Abilities = abilities;
        var events = new List<GameEvent>();
        var result = DamagePlacement.Resolve(world, world.Facts,
            new(target, target, 4, "synthetic", "Damage"), events);
        var receipt = Assert.IsType<DamageReplacement>(abilities.Replacement);
        Assert.False(result.IsComplete);
        Assert.Throws<InvalidOperationException>(() => receipt.ResolvedAmount);
        Assert.Throws<InvalidOperationException>(() => result.Taken);
        Assert.Equal(0, target.Damage);

        var foreign = Board().World;
        foreign.Agenda.Executions.RunBatch(new Occurrence(1, []), entry =>
        {
            Assert.Throws<InvalidOperationException>(() => receipt.Replace(entry, 99));
            Assert.Throws<InvalidOperationException>(() => new NoCardAbilities().WouldBeDealt(foreign, entry, receipt));
            return entry.Finish([]);
        });
        world.Agenda.Executions.RunBatch(new Occurrence(2, []), sibling =>
        {
            Assert.Throws<InvalidOperationException>(() => receipt.Replace(sibling, 99));
            Assert.Throws<InvalidOperationException>(() => new NoCardAbilities().WouldBeDealt(world, sibling, receipt));
            return sibling.Finish([]);
        });
        Assert.Equal(4, receipt.CurrentAmount);
        Assert.Equal(0, target.Damage);

        Sequence.Finish(world, world.Facts, abilities, events);
        Assert.True(result.IsComplete);
        Assert.Equal(2, receipt.ResolvedAmount);
        Assert.Equal(2, result.Dealt);
        Assert.Equal(2, result.Taken);
        Assert.Equal(2, target.Damage);
        Assert.Throws<InvalidOperationException>(() => receipt.Replace(abilities.Child!, 99));
        Assert.Equal(2, receipt.ResolvedAmount);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void AReplacementDoesNotDamageANewIncarnationOfItsRecipient()
    {
        var (world, target) = Board();
        var abilities = new SuspendedReplacement();
        world.Abilities = abilities;
        var events = new List<GameEvent>();
        var result = DamagePlacement.Resolve(world, world.Facts,
            new(target, target, 4, "synthetic", "Damage"), events);
        var original = target.Incarnation;
        World.MoveToTop(target, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        World.MoveToTop(target, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Assert.NotEqual(original, target.Incarnation);
        Sequence.Finish(world, world.Facts, abilities, events);
        Assert.True(result.IsComplete);
        Assert.Equal(0, result.Taken);
        Assert.Equal(Damage.Outcome.NotDefeated, result.Outcome);
        Assert.Equal(0, target.Damage);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(4, true)]
    public void StandalonePreventedDamageDoesNotInventADamageResponse(long amount, bool tough)
    {
        var (world, target) = Board();
        if (tough) Statuses.Give(world, target, Statuses.Tough);
        var result = DamagePlacement.Resolve(world, world.Facts,
            new(target, target, amount, "synthetic", "Damage"), []);
        Assert.False(result.IsComplete);
        var cause = Assert.IsType<Occurrence>(world.Agenda.Occurrence);
        Assert.DoesNotContain(Steps.DamageDealt, cause.Conditions);
        Assert.Empty(cause.Defeats);
        Sequence.Finish(world, world.Facts, world.Abilities, []);
        Assert.True(result.IsComplete);
        Assert.Equal(0, result.Taken);
        Assert.Equal(Damage.Outcome.NotDefeated, result.Outcome);
        Assert.False(world.Agenda.IsBusy);
    }

    private static (World World, Card Target) Board()
    {
        var world = new World(new Printed(), 1);
        world.CreateSeat("synthetic");
        world.CreateCard("deck", world.Seats[0].Deck);
        return (world, world.CreateCard("synthetic", world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0)));
    }

    private sealed class SuspendedReplacement : NoCardAbilities
    {
        internal DamageReplacement? Replacement { get; private set; }
        internal AbilityExecution? Child { get; private set; }

        public override AbilityExecutionResult WouldBeDealt(World world, AbilityExecution entry, DamageReplacement replacement)
        {
            world.Agenda.Executions.RequireBatch(entry);
            Replacement = replacement;
            Child = world.Agenda.Executions.Begin(entry.Occurrence, new(replacement.Target.ObjectId, AbilityType.ForcedInterrupt, 0));
            world.Agenda.ThenContinuation(new PhaseStep(
                new AbilityContinuationAgendaOperation(AbilityContinuationStage.Resume)
                { Continuation = new Continuation { Execution = Child } }, 1, 1, Plan: true), entry.Occurrence);
            Child.Yield([]);
            return entry.Finish([]);
        }

        public override IReadOnlyList<GameEvent> ResumeAbility(World world, PhaseStep continuation)
        {
            world.Agenda.Executions.Resume(Child!);
            Replacement!.Replace(Child!, 2);
            Child!.Finish([]);
            return [];
        }
    }

    private sealed record Continuation : CardAbilityContinuation;

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Ally;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            attribute == "HP" ? 10 : fallback;
    }
}
