using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorFrostGiantCardTests
{
    [Fact]
    [Rule("rr:toughness.1")]
    public void ActualFrostGiantEntersWithOneToughStatus()
    {
        // "When a character with the toughness keyword enters play, place a tough status card on it."
        var world = ThorActualFixture.Board("06001a");
        var fixture = new RedSkullFixture(world, AuthoredCards.Runner());
        var giant = fixture.Reveal("06029");
        fixture.Finish();
        Assert.Equal(DeckType.EngagedEnemiesArea, giant.Area.Type);
        Assert.Single(Statuses.On(world, giant, Statuses.Tough));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [Rule("rr:tough.3")]
    [Rule("rr:boost-boost-icon.2")]
    public void ActualBoostStunsOnlyCharacterDamagedDuringItsVillainAttack(bool scheme, bool tough)
    {
        // A character whose tough prevents the damage "is not considered to have taken damage."
        var world = ThorActualFixture.Board(scheme ? "06001b" : "06001a");
        var runner = AuthoredCards.Runner();
        var hero = world.Seats[0].IdentityCard;
        var boost = world.CreateCard("06029", world.AreaOf(DeckType.EncounterDeck));
        if (tough) Statuses.Give(world, hero, Statuses.Tough);
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(scheme ? new EnemySchemeOperation() : new EnemyAttackOperation(),
            1, 2, Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0));
        for (int step = 0; step < 20; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            Assert.True(prompt.Cancellable);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        }
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
        Assert.Equal(DeckType.EncounterDiscardPile, boost.Area.Type);
        Assert.Equal(!scheme && !tough, Statuses.Has(world, hero, Statuses.Stunned));
        Assert.Equal(scheme ? 0 : 1, events.OfType<AttackCompleted>().Count());
        if (!scheme && !tough) World.MoveToTop(Assert.Single(Statuses.On(world, hero, Statuses.Stunned)), world.AreaOf(DeckType.RemovedArea));
        DamagePlacement.Resolve(world, world.Facts, new(hero, hero, 1, "later", "later"), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
    }
    [Fact]
    public void BoostAbilityDamageToAnAllyDoesNotBecomeAttackDamageForFrostGiant()
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        // Physical queued boosts are prepared in order; the normal attack consumes both.
        world.CreateCard("01154", world.AreaOf(DeckType.BoostCardsDeck, villain.Area.PlayArea, host: villain.ObjectId));
        world.CreateCard("06029", world.AreaOf(DeckType.BoostCardsDeck, villain.Area.PlayArea, host: villain.ObjectId));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false },
            1, 2, Subject: villain.ObjectId, Seat: 0));
        for (int step = 0; step < 20; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.True(prompt.Cancellable);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
        Assert.Equal(1, ally.Damage);
        Assert.False(Statuses.Has(world, ally, Statuses.Stunned));
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
    }
    [Fact]
    public void FrostGiantBoostOnAMinionAttackDoesNotStun()
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var boost = world.CreateCard("06029", world.AreaOf(DeckType.BoostCardsDeck, minion.Area.PlayArea, host: minion.ObjectId));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false }, 1, 2, Subject: minion.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        for (int step = 0; step < 20; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            Assert.True(prompt.Cancellable);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        }
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(DeckType.EncounterDiscardPile, boost.Area.Type);
        Assert.True(world.Seats[0].IdentityCard.Damage > 0);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.False(world.Agenda.IsBusy);
    }
}
