using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class NativeIndirectRetaliationTests : AttackTestBase
{
    [Rule("rr:indirect-damage.5.1")]
    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public void OnlyTheAttackedCharacterRetaliatesWhenBothDamageRecipientsSurvive(bool allyDefends, int damage)
    {
        // rr:indirect-damage.5.1: "Only the defending character, or the attacked
        // player's identity if the attack was undefended, is considered to have
        // been attacked". Distinct grants distinguish the two surviving recipients.
        var facts = Printed(atk: 2, boost: 0);
        var world = Board(facts);
        var hero = world.Seats[0].IdentityCard;
        var ally = world.CreateCard("ally",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "retaliate", 1, Affects: hero.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "retaliate", 2, Affects: ally.ObjectId));
        var abilities = new NoCardAbilities();
        var events = new List<GameEvent>();
        var defend = Sequence.Work(world, facts, abilities, events)!;
        Assert.Equal(Question.Defender, defend.Asking);
        Attack.MakeIndirect(world);
        Sequence.Answer(world, facts, abilities, defend,
            allyDefends ? Decision.Take(ally.ObjectId) : Decision.Decline, events);
        var assign = Sequence.Work(world, facts, abilities, events)!;
        Assert.Equal(Question.Element, assign.Asking);
        Sequence.Answer(world, facts, abilities, assign,
            Decision.Take(Assert.Single(assign.Affordances).Id, [hero.ObjectId, ally.ObjectId], []), events);
        Sequence.Finish(world, facts, abilities, events);

        Assert.Equal(1, hero.Damage);
        Assert.Equal(1, ally.Damage);
        Assert.True(DeckTypes.IsInPlay(ally.Area.Type));
        Assert.Equal(damage, world.TheCardIn(DeckType.VillainArea)!.Damage);
        var retaliation = Assert.Single(events.OfType<FieldSet>(), item => item.Verb == "Retaliate");
        Assert.Equal(damage, retaliation.From - retaliation.To);
    }
}
