using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class BoostResolvedTests : AttackTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [Rule("rr:attack-enemy-activation.step.3.c")]
    public void ReportsAppliedIconsBeforeDiscardAndBeforeTheNextBoost(int icons)
    {
        // "Increase the attacking enemy's ATK value by one for each boost icon on the card."
        // The event's position and fields are our reporting contract.
        var facts = Printed(atk: 3, boost: icons);
        var world = Board(facts);
        Card enemy = world.TheCardIn(DeckType.VillainArea)!;
        world.Agenda.Abandon();
        Attack.Initiate(world, facts, new PhaseStep(Steps.Attack, 1, 2, Subject: enemy.ObjectId, Seat: 0), []);
        Attack.GiveAdditionalBoostCard(world, enemy, "test", []);
        Attack.GiveAdditionalBoostCard(world, enemy, "test", []);
        var events = new List<GameEvent>();
        Attack.FlipBoostCards(world, facts, new NoCardAbilities(), events);

        BoostResolved[] contributions = events.OfType<BoostResolved>().ToArray();
        Assert.Equal(2, contributions.Length);
        Assert.Equal(icons, contributions[0].Icons);
        Assert.Equal(0, contributions[1].Icons);
        Assert.All(contributions, value =>
        {
            Assert.True(value.Attacking);
            Assert.Equal(enemy.ObjectId, value.Enemy);
            Assert.Equal(3 + icons, value.Strength);
            Assert.Equal(2, value.Subjects!.Count);
        });
        int first = events.IndexOf(contributions[0]);
        var discarded = Assert.IsType<CardsMoved>(events[first + 1]);
        Assert.Equal(contributions[0].Card, Assert.Single(discarded.Cards).Card);
        Assert.IsType<CardsFlipped>(events[first + 2]);
    }
    [Theory]
    [InlineData(false, 4, 7)]
    [InlineData(true, 0, 3)]
    [Rule("rr:attack-enemy-activation.step.3.c")]
    [Rule("rr:loses")]
    public void ReportsEffectiveIconsIncludingAmplifyAndLostCharacteristics(bool lost, int icons, int strength)
    {
        // Lost characteristics do not function. The report must name applied icons,
        // not the printed three, including the amplify contribution when applicable.
        var facts = Printed(atk: 3, boost: 3);
        var world = Board(facts);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        var boost = world.AreaOf(DeckType.EncounterDeck).Cards[^1];
        world.CreateCard("amplify", world.AreaOf(DeckType.SideSchemesArea));
        if (lost) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Characteristics.LossOf("boost_const"), Affects: boost.ObjectId));
        world.Agenda.Abandon();
        Attack.Initiate(world, facts, new PhaseStep(Steps.Attack, 1, 2, Subject: enemy.ObjectId, Seat: 0), []);
        Attack.GiveAdditionalBoostCard(world, enemy, "test", []);
        var events = new List<GameEvent>();
        Attack.FlipBoostCards(world, facts, new NoCardAbilities(), events);
        var resolved = Assert.Single(events.OfType<BoostResolved>());
        Assert.Equal(icons, resolved.Icons);
        Assert.Equal(strength, resolved.Strength);
    }
}
