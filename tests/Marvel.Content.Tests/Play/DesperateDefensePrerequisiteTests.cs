using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DesperateDefensePrerequisiteTests
{
    [Theory]
    [InlineData(0, 4, false, 0, true)]
    [InlineData(0, 5, false, 1, false)]
    [InlineData(1, 5, false, 0, true)]
    [InlineData(1, 6, false, 1, false)]
    [InlineData(1, 4, true, 1, true)]
    [Rule("rr:defend-defense.2")]
    [Rule("rr:delayed-effect.1")]
    public void ActualTextCandidatePaysThenReadiesOnlyForZeroAttackDamage(
        int owner, int attack, bool boost, int damage, bool ready)
    {
        // Basic defense requires "exhausting their hero"; delayed effects resolve
        // "immediately after their specified timing point or future condition".
        // Printed 09015 grants +2 DEF and tests damage "from that attack".
        var (world, runner, source, payment, enemy) = DesperateDefensePrerequisiteFixture.Board(owner, attack, boost);
        var hero = world.Seats[owner].IdentityCard;
        var events = new List<GameEvent>();
        Assert.Equal(1, DesperateDefensePrerequisiteFixture.Drain(world, runner, source, payment,
            hero.ObjectId, owner, events));
        Assert.Equal(damage, hero.Damage);
        Assert.Equal(ready, hero.Ready);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == UndamagedDefenseReady.Kind);
        Assert.Equal(owner == 0 ? 2 : 3, StateFields.Modified(world, hero, "defense", world.Facts, world.Players));

        // A later attack receives neither the DEF modifier nor the delayed ready.
        hero.Refresh();
        long before = hero.Damage;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false },
            1, 2, Subject: enemy.ObjectId, Seat: owner));
        Assert.Equal(0, DesperateDefensePrerequisiteFixture.Drain(world, runner, source, payment,
            hero.ObjectId, owner, events));
        Assert.False(hero.Ready);
        Assert.Equal(before + Math.Max(0, attack - (owner == 0 ? 2 : 3)), hero.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:defend-defense.3")]
    public void OtherPlayerAndAllyDefenseDoNotOfferTheOwnersEvent(bool ally)
    {
        // An ally defense exhausts "an ally they control"; printed 09015 requires "your hero".
        var (world, runner, source, payment, _) = DesperateDefensePrerequisiteFixture.Board(0, 1);
        var defender = ally ? world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0))
            : world.Seats[1].IdentityCard;
        Assert.Equal(0, DesperateDefensePrerequisiteFixture.Drain(world, runner, source, payment,
            defender.ObjectId, ally ? 0 : 1, []));
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
        Assert.Equal(DeckType.HandsArea, payment.Area.Type);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.6")]
    public void CostPrecedesAnyEffectAndStaleAnswerCannotRepeatPayment()
    {
        // "Pay all costs" precedes applying the effects in step 7.
        var (world, runner, source, payment, _) = DesperateDefensePrerequisiteFixture.Board(1, 4);
        var hero = world.Seats[1].IdentityCard;
        var events = new List<GameEvent>();
        var defense = Sequence.Work(world, world.Facts, runner, events)!;
        if (defense.Player == 0)
        {
            Sequence.Answer(world, world.Facts, runner, defense, Decision.Decline, events);
            defense = Sequence.Work(world, world.Facts, runner, events)!;
        }
        Assert.Equal(Question.Defender, defense.Asking);
        Sequence.Answer(world, world.Facts, runner, defense, Decision.Take(hero.ObjectId), events);
        var interrupt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.False(hero.Ready);
        Assert.Equal(3, StateFields.Modified(world, hero, "defense", world.Facts, world.Players));
        var offer = interrupt.Affordances.Single(item => item.AnchorId == source.ObjectId);
        string unpaid = world.Digest().Canonical();
        Assert.ThrowsAny<Exception>(() => Sequence.Answer(world, world.Facts, runner, interrupt,
            Decision.Take(offer.Id), events));
        Assert.Equal(unpaid, world.Digest().Canonical());
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == UndamagedDefenseReady.Kind);
        Assert.Equal(3, StateFields.Modified(world, hero, "defense", world.Facts, world.Players));
        Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Take(offer.Id, [], [payment.ObjectId]), events);
        Sequence.Work(world, world.Facts, runner, events);
        string digest = world.Digest().Canonical();
        Assert.ThrowsAny<Exception>(() => Sequence.Answer(world, world.Facts, runner, interrupt,
            Decision.Take(offer.Id, [], [payment.ObjectId]), events));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.True(hero.Ready);
    }

    [Fact]
    public void QueriesPreserveExecutionAndAnUnrelatedAttackCannotConsumeTheReady()
    {
        var probed = DesperateDefensePrerequisiteFixture.Board(1, 4);
        var control = DesperateDefensePrerequisiteFixture.Board(1, 4);
        Prepare(probed.World, probed.Runner);
        Prepare(control.World, control.Runner);
        var world = probed.World;
        var occurrence = world.Agenda.Occurrence!;
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        string random = System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState());
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        for (int query = 0; query < 4; query++)
        {
            Assert.True(UndamagedDefenseReady.CanInstall(world, occurrence, 1));
            Assert.False(UndamagedDefenseReady.CanInstall(world, occurrence, 0));
        }
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(random, System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState()));
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        var activation = world.Activation!;
        world.Activation = activation with { Id = activation.Id + 1 };
        Assert.False(UndamagedDefenseReady.CanInstall(world, occurrence, 1));
        world.Activation = activation;

        UndamagedDefenseReady.Install(world, occurrence, probed.Source, 1);
        var registration = world.Effects.Registered.Single(effect => effect.Kind == UndamagedDefenseReady.Kind);
        DelayedEffects.Occur(world, Steps.AttackEnds, -1, [], world.Activation!.Id + 1);
        Assert.Contains(registration, world.Effects.Registered);
        Assert.False(world.Seats[1].IdentityCard.Ready);
        // Remove the direct probe registration by consuming its matching condition;
        // the real event below creates its own registration and pays its real cost.
        world.Effects.Occur(Steps.AttackEnds, world.Activation.Id);
        DesperateDefensePrerequisiteFixture.Drain(world, probed.Runner, probed.Source, probed.Payment,
            world.Seats[1].IdentityCard.ObjectId, 1, []);
        DesperateDefensePrerequisiteFixture.Drain(control.World, control.Runner, control.Source, control.Payment,
            control.World.Seats[1].IdentityCard.ObjectId, 1, []);
        Assert.Equal(control.World.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.World.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(control.World.Random.Generator.GetState()),
            System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState()));
        Assert.Equal(control.World.Effects.Registered, world.Effects.Registered);
        Assert.Equal(control.World.Agenda.Outstanding, world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void DelayedReadyDoesNotFollowTheHeroIntoANewIncarnation()
    {
        // A returned card "is considered to be a new copy of the card".
        var (world, runner, source, _, _) = DesperateDefensePrerequisiteFixture.Board(1, 4);
        Prepare(world, runner);
        var hero = world.Seats[1].IdentityCard;
        UndamagedDefenseReady.Install(world, world.Agenda.Occurrence!, source, 1);
        var home = hero.Area;
        World.MoveToTop(hero, world.AreaOf(DeckType.AsideDeck));
        World.MoveToTop(hero, home);
        hero.Exhaust();
        DelayedEffects.Occur(world, Steps.AttackEnds, -1, [], world.Activation!.Id);
        Assert.False(hero.Ready);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == UndamagedDefenseReady.Kind);
    }

    private static void Prepare(World world, Marvel.Cards.Run.AbilityRunner runner)
    {
        var helper = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, helper, Decision.Decline, []);
        var defense = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, defense,
            Decision.Take(world.Seats[1].IdentityCard.ObjectId), []);
        Assert.NotNull(Sequence.Work(world, world.Facts, runner, []));
    }
}
