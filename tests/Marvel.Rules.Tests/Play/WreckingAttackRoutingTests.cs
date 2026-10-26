using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class WreckingAttackRoutingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BoostDiscardReturnsToOriginalOwnerAfterCounterMoves(bool activationContinues)
    {
        // pack:mc03:new-rules: each encounter card returns to its corresponding deck's discard.
        var fixture = new WreckingRuntimeFixture();
        var world = fixture.World;
        var owner = fixture.Groups[2];
        var boost = owner.Members[0];
        var events = new List<GameEvent>();
        if (activationContinues)
        {
            world.Activation = new EnemyActivation(owner.Villain.ObjectId, 0, true);
            World.MoveToTop(boost, world.AreaOf(DeckType.BoostCardsDeck,
                owner.Villain.Area.PlayArea, host: owner.Villain.ObjectId));
            Attack.FlipBoostCards(world, world.Facts, world.AttackAbilities, events);
            Sequence.Finish(world, world.Facts, world.Abilities, events);
        }
        else
        {
            World.MoveToTop(boost, world.AreaOf(DeckType.BoostingArea));
            Attack.FinishBoostCard(world, world.Facts, world.AttackAbilities,
                new PhaseStep(new BoostCompletionOperation(owner.Villain.ObjectId, true),
                    1, 3, Subject: boost.ObjectId, Seat: 0), events);
        }
        Assert.Same(owner.Discard, boost.Area);
        Assert.Empty(fixture.Groups[0].Discard.Cards);
        Assert.Equal(Places.Reference(owner.Discard),
            Assert.Single(events.OfType<CardsMoved>()).To);
    }

    [Fact]
    public void MinionOverkillHitsActiveVillainRatherThanFirstOrOwningVillain()
    {
        // rr:overkill: excess damage from a defeated minion is dealt to the villain.
        // pack:mc03:new-rules: references to the villain mean the active villain.
        var fixture = new WreckingRuntimeFixture();
        var world = fixture.World;
        world.EncounterTopology!.SelectActive(fixture.Groups[2].Villain, "test", []);
        var hero = world.Seats[0].IdentityCard;
        var minion = fixture.Groups[1].Members[0];
        World.MoveToTop(minion, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Keywords.Overkill, Card: hero.ObjectId, Affects: hero.ObjectId));
        var result = DamageAttacks.Resolve(world, world.Facts,
            new(hero, hero, minion, 7, "test", "Attack"), []);
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities, []));
        Assert.True(result.IsComplete);
        Assert.Equal(2, fixture.Groups[2].Villain.Damage);
        Assert.Equal(0, fixture.Groups[0].Villain.Damage);
        Assert.Equal(0, fixture.Groups[1].Villain.Damage);
        Assert.Same(fixture.Groups[1].Discard, minion.Area);
    }

    [Fact]
    public void RetaliationFollowsOnlyTheAttackedVillainsPromotedStage()
    {
        // rr:villain-defeat.3.1: the next stage is the same character.
        // rr:retaliate-x.2: that character must remain in play after the attack.
        var fixture = new WreckingRuntimeFixture(extreme: true);
        var world = fixture.World;
        var group = fixture.Groups[2];
        var prior = group.Villain;
        var promoted = group.Stages[1];
        Assert.False(Defeat.FinalizeCharacter(world, world.Facts, prior, "test", []));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "retaliate", Amount: 2, Card: promoted.ObjectId, Affects: promoted.ObjectId));
        DamageAttacks.Retaliate(world, world.Facts, prior, world.Seats[0].IdentityCard, "test", []);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Same(promoted, group.CurrentVillain);
        Assert.Same(fixture.Groups[0].Villain, EncounterRouting.Villain(world));
    }
}
