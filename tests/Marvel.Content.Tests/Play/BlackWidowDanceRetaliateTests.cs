using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.BlackWidowEventCompositionFixture;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowDanceRetaliateTests
{
    [Fact]
    [Rule("rr:attack-player-ability-type.3")]
    public void ActualDanceRetargetsAfterTheFirstAttackDefeatsAMinion()
    {
        // "Each of those instances is considered a separate attack."
        var world = Board();
        var minion = world.CreateCard("01121", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        minion.TakeDamage(1);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        PlayEvent(world, "08004");
        var first = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, world.Abilities!, []));
        Sequence.Answer(world, world.Facts, world.Abilities!, first, Decision.Take(minion.ObjectId), []);
        for (int attack = 1; attack < 3; attack++)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, world.Abilities!, []));
            Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
            Assert.DoesNotContain(prompt.Affordances, offer => offer.AnchorId == minion.ObjectId);
            Sequence.Answer(world, world.Facts, world.Abilities!, prompt, Decision.Take(villain.ObjectId), []);
        }
        Sequence.Finish(world, world.Facts, world.Abilities!, []);
        Assert.Equal(5, villain.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:retaliate-x.1")]
    public void ActualDanceResolvesRetaliationBetweenItsThreeAttacks()
    {
        // "After this character is attacked, deal X damage to the attacker."
        var world = Board();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "retaliate", 1, Affects: villain.ObjectId));
        PlayEvent(world, "08004");
        for (int attack = 0; attack < 3; attack++)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, world.Abilities!, []));
            Assert.Equal(attack, world.Seats[1].IdentityCard.Damage);
            Sequence.Answer(world, world.Facts, world.Abilities!, prompt, Decision.Take(villain.ObjectId), []);
        }
        Sequence.Finish(world, world.Facts, world.Abilities!, []);
        Assert.Equal(3, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(6, villain.Damage);
        Assert.False(world.Agenda.IsBusy);
    }
}
