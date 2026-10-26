using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class SimultaneousDirectDamageTests
{
    [Rule("rr:damage.step.5")]
    [Rule("rr:damage.step.6")]
    [Fact]
    public void EveryPlacementPrecedesTheFirstDefeatInterrupt()
    {
        // ruling:3eb442853617fe14: "all damage is dealt simultaneously". Steps 5
        // ("place ... damage") and 6 ("would be defeated") therefore cannot
        // run to completion for one recipient before placing the other share.
        var (world, first, second) = Board("fragile");
        var observer = new DefeatObserver(second);
        world.Abilities = observer;
        var result = Resolve(world, [first, second]);
        Sequence.Finish(world, world.Facts, observer, []);

        Assert.Equal(4, observer.OtherDamageAtFirstDefeat);
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
        Assert.True(result.IsComplete);
        Assert.Equal([4L, 4L], result.Recipients.Select(recipient => recipient.Taken));
    }

    [Rule("rr:damage.step.1")]
    [Fact]
    public void AReplacementSuspendsTheWholeBatchBeforeAnyPlacement()
    {
        // Damage step 1 resolves "would be dealt" abilities before placement.
        var (world, first, second) = Board();
        var abilities = new SuspendedReplacement(second);
        world.Abilities = abilities;
        var result = Resolve(world, [first, second]);

        Assert.False(result.IsComplete);
        Assert.Throws<InvalidOperationException>(() => result.Recipients);
        Assert.Equal(0, first.Damage);
        Assert.Equal(0, second.Damage);
        Sequence.Finish(world, world.Facts, abilities, []);
        Assert.True(result.IsComplete);
        Assert.Equal(4, first.Damage);
        Assert.Equal(2, second.Damage);
        Assert.Equal([4L, 2L], result.Recipients.Select(recipient => recipient.Taken));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void ADepartedAndReturnedTargetIsNotDamagedAsANewIncarnation()
    {
        // Engine identity contract: a declared target binds its incarnation.
        var (world, first, second) = Board();
        var abilities = new SuspendedReplacement(first);
        world.Abilities = abilities;
        var result = Resolve(world, [first, second]);
        int original = second.Incarnation;
        World.MoveToTop(second, world.AreaOf(DeckType.DiscardPile, PlayArea.Villains));
        World.MoveToTop(second, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.NotEqual(original, second.Incarnation);
        Sequence.Finish(world, world.Facts, abilities, []);

        Assert.True(result.IsComplete);
        Assert.Equal(2, first.Damage);
        Assert.Equal(0, second.Damage);
        Assert.Equal([2L, 0L], result.Recipients.Select(recipient => recipient.Taken));
        Assert.Equal(original, result.Recipients[1].TargetIncarnation);
    }

    [Fact]
    public void RepeatedTargetsAreRejectedWithoutChangingState()
    {
        // Engine API contract: simultaneous recipients are distinct.
        var (world, first, _) = Board();
        var before = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => Resolve(world, [first, first]));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:damage.step.6")]
    [Fact]
    public void ASuspendedDefeatResumesWithoutPlacingTheOtherShareAgain()
    {
        // "Would be defeated" interrupts occur after damage is placed.
        var (world, first, second) = Board("fragile");
        var abilities = new SuspendedDefeat(first);
        world.Abilities = abilities;
        var result = Resolve(world, [first, second]);

        Assert.False(result.IsComplete);
        Assert.Equal(4, first.Damage);
        Assert.Equal(4, second.Damage);
        Sequence.Finish(world, world.Facts, abilities, []);
        Assert.True(result.IsComplete);
        Assert.Equal([4L, 4L], result.Recipients.Select(recipient => recipient.Taken));
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:damage.step.3")]
    [Fact]
    public void EveryPreventionIsEvaluatedBeforeAnyDamageIsPlaced()
    {
        // Step 3 modifies "the amount of damage" before step 5 places damage.
        var (world, first, second) = Board();
        var observer = new PreventionObserver(first);
        world.Abilities = observer;
        var result = Resolve(world, [first, second]);
        Sequence.Finish(world, world.Facts, observer, []);
        Assert.Equal([0L, 0L], observer.DamageDuringPrevention);
        Assert.Equal([4L, 4L], result.Recipients.Select(recipient => recipient.Taken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DirectDamageDoesNotAttackOrRetaliateAndRetainsIgnoredTough(bool ignores)
    {
        var (world, first, second) = Board("retaliator");
        var tough = Statuses.Give(world, second, Statuses.Tough);
        var result = SimultaneousDirectDamage.Resolve(world, world.Facts,
            new[] { first, second }.Select(target => new DamageRequest(world.Seats[0].IdentityCard,
                target, 4, "synthetic", "Deal_Damage") { IgnoresTough = ignores }).ToArray(), []);
        Sequence.Finish(world, world.Facts, world.Abilities, []);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Empty(result.Execution.Occurrence.Attacks);
        Assert.Equal([4L, ignores ? 4L : 0L], result.Recipients.Select(recipient => recipient.Taken));
        if (ignores) Assert.Same(tough, Assert.Single(Statuses.On(world, second, Statuses.Tough)));
        else Assert.False(Statuses.Has(world, second, Statuses.Tough));
    }

    private sealed class PreventionObserver(Card first) : NoCardAbilities
    {
        internal List<long> DamageDuringPrevention { get; } = [];
        public override long WouldTake(World world, Card target, Card source, long amount, List<GameEvent> events)
        {
            DamageDuringPrevention.Add(first.Damage);
            return amount;
        }
    }

    private static SimultaneousDirectDamageResult Resolve(World world, IReadOnlyList<Card> targets) =>
        SimultaneousDirectDamage.Resolve(world, world.Facts,
            targets.Select(target => new DamageRequest(world.Seats[0].IdentityCard, target, 4, "synthetic", "Deal_Damage")).ToArray(), []);

    private static (World World, Card First, Card Second) Board(string face = "target")
    {
        var world = new World(new Printed(), 1);
        world.CreateSeat("player");
        world.Seats[0].IdentityCard = world.CreateCard("hero", world.Seats[0].Hero);
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        return (world, world.CreateCard(face, area), world.CreateCard(face, area));
    }

    private sealed class DefeatObserver(Card other) : NoCardAbilities
    {
        internal long? OtherDamageAtFirstDefeat { get; private set; }
        public override AbilityExecutionResult WouldBeDefeated(World world, AbilityExecution entry,
            Card target, DefeatCause cause)
        {
            OtherDamageAtFirstDefeat ??= other.Damage;
            return entry.Finish([]);
        }
    }

    private sealed class SuspendedReplacement(Card target) : NoCardAbilities
    {
        private DamageReplacement? replacement;
        private AbilityExecution? child;
        public override AbilityExecutionResult WouldBeDealt(World world, AbilityExecution entry, DamageReplacement receipt)
        {
            if (receipt.Target != target) return entry.Finish([]);
            replacement = receipt;
            child = world.Agenda.Executions.Begin(entry.Occurrence, new(target.ObjectId, AbilityType.ForcedInterrupt, 0));
            world.Agenda.ThenContinuation(new PhaseStep(
                new AbilityContinuationAgendaOperation(AbilityContinuationStage.Resume)
                { Continuation = new Continuation { Execution = child } }, 1, 1, Plan: true), entry.Occurrence);
            child.Yield([]);
            return entry.Finish([]);
        }

        public override IReadOnlyList<GameEvent> ResumeAbility(World world, PhaseStep continuation)
        {
            world.Agenda.Executions.Resume(child!);
            replacement!.Replace(child!, 2);
            child!.Finish([]);
            return [];
        }
    }

    private sealed record Continuation : CardAbilityContinuation;

    private sealed class SuspendedDefeat(Card target) : NoCardAbilities
    {
        private AbilityExecution? child;
        public override AbilityExecutionResult WouldBeDefeated(World world, AbilityExecution entry,
            Card character, DefeatCause cause)
        {
            if (character != target) return entry.Finish([]);
            child = world.Agenda.Executions.Begin(entry.Occurrence, new(target.ObjectId, AbilityType.Interrupt, 0));
            world.Agenda.ThenContinuation(new PhaseStep(
                new AbilityContinuationAgendaOperation(AbilityContinuationStage.Resume)
                { Continuation = new Continuation { Execution = child } }, 1, 1, Plan: true), entry.Occurrence);
            child.Yield([]);
            return entry.Finish([]);
        }

        public override IReadOnlyList<GameEvent> ResumeAbility(World world, PhaseStep continuation)
        {
            world.Agenda.Executions.Resume(child!);
            child!.Finish([]);
            return [];
        }
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero" => CardKind.Hero,
            "villain" => CardKind.EncounterVillain,
            "tough" => CardKind.Status,
            _ => CardKind.Minion,
        };
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            attribute switch
            {
                "HP" => faceId switch { "hero" => 10, "villain" => 100, "fragile" => 3, _ => 5 },
                "Retaliate" when faceId == "retaliator" => 1,
                _ => fallback,
            };
        public long ConsequentialDamage(string faceId, string attribute) => 0;
    }
}
