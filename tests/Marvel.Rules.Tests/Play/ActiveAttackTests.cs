using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class ActiveAttackTests : AttackTestBase
{
    [Theory]
    [InlineData(Outcome.PlayersWin)]
    [InlineData(Outcome.VillainWins)]
    public void TerminalGamesHaveNoActiveAttackEvenWithRetainedCombatState(Outcome result)
    {
        var world = Board(Printed(atk: 3, boost: 0));
        world.Attack = new EnemyAttack(world.TheCardIn(DeckType.VillainArea)!.ObjectId,
            0, world.Seats[0].IdentityCard.ObjectId);
        Assert.NotNull(ActiveAttack.From(world));
        world.Finish(result);
        Assert.Null(ActiveAttack.From(world));
    }

    [Fact]
    public void AttackOwnershipSpansAllWindowsAndEndsWithItsOwner()
    {
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        var observer = new ActiveObserver();
        Assert.Null(ActiveAttack.From(world));
        var events = new List<GameEvent>();
        var defense = Assert.IsType<Prompt>(Sequence.Work(world, facts, observer, events));
        Assert.Equal(Question.Defender, defense.Asking);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, ActiveAttack.From(world)!.Target);
        Sequence.Answer(world, facts, observer, defense, Decision.Decline, events);
        Sequence.Finish(world, facts, observer, events);

        Assert.Contains(Steps.AttackInitiated, observer.Conditions);
        Assert.Contains("WhenBoostCardsFlipped", observer.Conditions);
        Assert.Contains(Steps.DamageWouldBeDealt, observer.Conditions);
        Assert.Contains(Steps.AttackEnds, observer.Conditions);
        Assert.NotNull(world.FinishedAttack);
        Assert.Null(ActiveAttack.From(world));
    }

    [Fact]
    public void DefenderRetargetsTheLiveAttackBeforeConsequences()
    {
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        var events = new List<GameEvent>();
        var abilities = new NoCardAbilities();
        var defense = Assert.IsType<Prompt>(Sequence.Work(world, facts, abilities, events));
        Sequence.Answer(world, facts, abilities, defense, Decision.Take(ally.ObjectId), events);
        Assert.Equal(ally.ObjectId, ActiveAttack.From(world)!.Target);
    }

    [Fact]
    public void EndResponseKeepsItsAttackAcrossNestedChoicesButNotAfterRemoval()
    {
        // Synthetic suspension of the engine's final attack occurrence.
        var world = Board(Printed(atk: 3, boost: 0));
        world.Agenda.Abandon();
        int hero = world.Seats[0].IdentityCard.ObjectId;
        int villain = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        world.Attack = new EnemyAttack(villain, 0, hero);
        world.Agenda.Add(new PhaseStep(Steps.EndAttack, 1, 6));
        world.Agenda.Begin(world, world.Facts);
        world.Agenda.Advance();
        Attack.End(world, []);
        world.Agenda.Advance();
        world.Agenda.Now(new PhaseStep(Steps.ChooseOption, 1, 7));
        Assert.Equal(new ActiveAttack(villain, hero), ActiveAttack.From(world));
        world.Agenda.Abandon();
        Assert.NotNull(world.FinishedAttack);
        Assert.Null(ActiveAttack.From(world));
    }

    [Fact]
    public void NestedPlayerAttackOwnsItsWindowThenReturnsToOuterAttack()
    {
        // Synthetic nested agenda ownership: no claimed printed card ability.
        var world = Board(Printed(atk: 3, boost: 0));
        int hero = world.Seats[0].IdentityCard.ObjectId;
        int villain = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        world.Attack = new EnemyAttack(villain, 0, hero);
        world.CharacterAttack = new CharacterAttack(hero, villain, 0);
        var step = new PhaseStep(Steps.CharacterAttacks, 1, 3,
            CharacterAttack: world.CharacterAttack);
        world.Agenda.Now(step);
        world.Agenda.Begin(world, world.Facts);
        Assert.Equal(new ActiveAttack(hero, villain) { Identity = new(Moment.Id(step.Round, step.Number, step.Index), false) }, ActiveAttack.From(world));
        world.Agenda.Advance();
        world.Agenda.Advance();
        Assert.Equal(new ActiveAttack(hero, villain) { Identity = new(Moment.Id(step.Round, step.Number, step.Index), false) }, ActiveAttack.From(world));
        world.Agenda.Advance();
        Assert.Equal(new ActiveAttack(villain, hero), ActiveAttack.From(world));
        world.Attack = null;
        world.Agenda.Abandon();
        Assert.Null(ActiveAttack.From(world));
        Assert.NotNull(world.CharacterAttack);
    }

    [Fact]
    public void AQueuedResponsePowerRetainsItsEnclosingFinishedAttack()
    {
        // Synthetic response ownership: the pending nested power retains the
        // enemy attack that caused it, even after that enemy attack has ended.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        world.Agenda.Abandon();
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Attack = null;
        world.FinishedAttack = new EnemyAttack(villain.ObjectId, 0, hero.ObjectId);
        var origin = Occurrence.ForAttack(
            123, [Steps.AttackEnds], world, facts, villain.ObjectId, hero.ObjectId, 0);

        Assert.True(BasicPowerInitiation.CardAttack(world, facts,
            new(0, hero, villain, 1, "response") { Performer = hero, Origin = origin }, []));

        Assert.Equal(new ActiveAttack(villain.ObjectId, hero.ObjectId), ActiveAttack.From(world));
        world.Agenda.Abandon();
        Assert.Null(ActiveAttack.From(world));
    }

    private sealed class ActiveObserver : NoCardAbilities
    {
        public HashSet<string> Conditions { get; } = [];

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window)
        {
            if (occurrence.Is("WhenBoostCardsFlipped"))
            {
                Assert.NotNull(ActiveAttack.From(world));
                Conditions.UnionWith(occurrence.Conditions);
            }
            if (occurrence.Actor < 0) return [];
            var active = Assert.IsType<ActiveAttack>(ActiveAttack.From(world));
            Assert.Equal(occurrence.Actor, active.Source);
            Assert.Equal(occurrence.Target, active.Target);
            Conditions.UnionWith(occurrence.Conditions);
            return [];
        }
    }
}
