using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class SimultaneousAttackDamageTests
{
    [Rule("rr:damage.step.5")]
    [Rule("rr:damage.step.6")]
    [Fact]
    public void EveryPlacementPrecedesTheFirstDefeatInterrupt()
    {
        // faq:03006 says Shield Toss damage is simultaneous. Damage steps 5
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
    }

    [Rule("rr:retaliate-x.1")]
    [Rule("rr:tough.3")]
    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    public void EachSurvivingTargetRetaliatesUnlessTheAttackIsRanged(bool ranged, long expected)
    {
        // Retaliate follows an attack; tough says the character is not
        // considered to have taken damage, not that it was not attacked.
        var (world, first, second) = Board("retaliator");
        Statuses.Give(world, second, Statuses.Tough);
        if (ranged) Grant(world, Keywords.Ranged);
        var result = Resolve(world, [first, second]);
        Sequence.Finish(world, world.Facts, world.Abilities, []);

        Assert.True(result.IsComplete);
        Assert.Equal(expected, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(4, first.Damage);
        Assert.Equal(0, second.Damage);
        Assert.False(Statuses.Has(world, second, Statuses.Tough));
    }

    [Rule("rr:overkill.1")]
    [Fact]
    public void EachDefeatedMinionSpillsItsOwnExcessWithoutRepeatingPlacement()
    {
        // Overkill sends damage in excess of a defeated minion's remaining
        // hit points to the villain.
        var (world, first, second) = Board("fragile");
        var villain = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea, PlayArea.Villains));
        Grant(world, Keywords.Overkill);
        var result = Resolve(world, [first, second]);
        Sequence.Finish(world, world.Facts, world.Abilities, []);

        Assert.Equal(2, villain.Damage);
        Assert.Equal([1L, 1L], result.Recipients.Select(recipient => recipient.Excess));
        Assert.Equal([5L, 5L], result.Recipients.Select(recipient => recipient.Amount));
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

    [Rule("rr:attack-player-ability-type.5.1")]
    [Fact]
    public void RetaliationWaitsUntilEveryAttackedTargetHasSettledItsDefeat()
    {
        // Each surviving enemy retaliates "after the attack resolves."
        var (world, first, _) = Board("retaliator");
        var second = world.CreateCard("fragile", first.Area);
        var observer = new RetaliationObserver(second);
        world.Abilities = observer;
        var result = Resolve(world, [first, second]);
        Sequence.Finish(world, world.Facts, observer, []);

        Assert.True(result.IsComplete);
        Assert.Equal(DeckType.EncounterDiscardPile, observer.OtherAreaWhenRetaliating);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:attack-player-ability-type.5.1")]
    [Fact]
    public void BatchCompletionWaitsForSuspendedRetaliationDamage()
    {
        // The attack's surviving recipient deals retaliation after resolution.
        var (world, first, _) = Board("retaliator");
        var second = world.CreateCard("fragile", first.Area);
        var hero = world.Seats[0].IdentityCard;
        var abilities = new SuspendedReplacement(hero);
        world.Abilities = abilities;
        var result = Resolve(world, [first, second]);
        Assert.False(result.IsComplete);
        Assert.Equal(0, hero.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);

        Sequence.Finish(world, world.Facts, abilities, []);
        Assert.True(result.IsComplete);
        Assert.Equal(2, hero.Damage);
        Assert.Equal([4L, 4L], result.Recipients.Select(recipient => recipient.Taken));
        Assert.False(world.Agenda.IsBusy);
    }

    private static SimultaneousAttackDamageResult Resolve(World world, IReadOnlyList<Card> targets) =>
        SimultaneousAttackDamage.Resolve(world, world.Facts,
            new(world.Seats[0].IdentityCard, world.Seats[0].IdentityCard, targets, 4, "synthetic", "Attack"), []);

    private static void Grant(World world, string keyword)
    {
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: keyword, Amount: 1, Card: hero.ObjectId, Affects: hero.ObjectId));
    }

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

    private sealed class RetaliationObserver(Card other) : NoCardAbilities
    {
        internal DeckType? OtherAreaWhenRetaliating { get; private set; }
        public override AbilityExecutionResult WouldBeDealt(World world, AbilityExecution entry, DamageReplacement receipt)
        {
            if (receipt.Target == world.Seats[0].IdentityCard)
                OtherAreaWhenRetaliating = other.Area.Type;
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
