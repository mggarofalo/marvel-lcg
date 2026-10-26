using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowEncounterCompositionTests
{
    [Theory]
    [Rule("rr:you-your.10")]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void DeadlyShotChoosesOnlyOwnUpgradeAndContinuesWithoutOne(bool hero, bool absent)
    {
        // A function against a card "you control" uses a card "in their play area".
        var world = WidowEncounterFixture.Board(hero);
        var runner = world.Abilities;
        var foreign = WidowEncounterFixture.Upgrade(world, seat: 0);
        var own = absent ? null : WidowEncounterFixture.Upgrade(world);
        var f = new RedSkullFixture(world, WidowEncounterFixture.Runner());
        var shot = f.Reveal("08029", 1);
        if (own is not null)
        {
            var choice = Sequence.Work(world, world.Facts, runner, [])!;
            Assert.Equal(1, choice.Player);
            Assert.Equal(own.ObjectId, Assert.Single(choice.Affordances).Id);
            string before = world.Digest().Canonical();
            Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner,
                choice, Decision.Take(foreign.ObjectId), []));
            Assert.Equal(before, world.Digest().Canonical());
            Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(own.ObjectId), []);
        }
        f.Finish();
        Assert.Equal(DeckType.UpgradesArea, foreign.Area.Type);
        if (own is not null) Assert.Equal(DeckType.DiscardPile, own.Area.Type);
        Assert.Equal(hero ? 1 : 0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(hero ? 0 : 1, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.EncounterDiscardPile, shot.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TaskmasterUsesEngagedPlayersLiveUpgradeCount(bool otherSeat)
    {
        var world = WidowEncounterFixture.Board();
        WidowEncounterFixture.Upgrade(world, seat: 0);
        WidowEncounterFixture.Upgrade(world);
        var second = WidowEncounterFixture.Upgrade(world, "08007");
        var f = new RedSkullFixture(world, WidowEncounterFixture.Runner());
        var taskmaster = f.Reveal("08026", otherSeat ? 0 : 1);
        f.Finish();
        Assert.Equal(DeckType.EngagedEnemiesArea, taskmaster.Area.Type);
        Assert.Equal(otherSeat ? 1 : 2, StateFields.Modified(world, taskmaster, "attack", world.Facts, 2));
        Assert.Equal(otherSeat ? 1 : 2, StateFields.Modified(world, taskmaster, "scheme", world.Facts, 2));
        Discard.Card(world, second, "fixture", []);
        Assert.Equal(1, StateFields.Modified(world, taskmaster, "attack", world.Facts, 2));
        Assert.Equal(1, StateFields.Modified(world, taskmaster, "scheme", world.Facts, 2));
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TaskmasterBoostUsesActivationSeatAndExpires(bool attack)
    {
        var world = WidowEncounterFixture.Board(attack);
        var runner = WidowEncounterFixture.Runner();
        world.Abilities = runner;
        WidowEncounterFixture.Upgrade(world);
        WidowEncounterFixture.Upgrade(world, "08007");
        var boost = world.CreateCard("08026", world.AreaOf(DeckType.EncounterDeck));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Agenda.Add(new PhaseStep(attack ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            1, 2, Subject: villain.ObjectId, Seat: 1));
        var f = new RedSkullFixture(world, runner);
        f.Finish();
        Assert.Equal(attack ? 4 : 0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(attack ? 0 : 3, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(2, StateFields.Modified(world, villain, "attack", world.Facts, 2));
        Assert.Equal(1, StateFields.Modified(world, villain, "scheme", world.Facts, 2));
        Assert.Equal(DeckType.EncounterDiscardPile, boost.Area.Type);
        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
    }

}
