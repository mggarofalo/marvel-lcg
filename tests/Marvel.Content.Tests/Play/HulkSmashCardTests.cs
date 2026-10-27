using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkSmashCardTests
{
    [Theory]
    [InlineData("R", 0, 10)]
    [InlineData("B", 0, 0)]
    [InlineData("G", 0, 0)]
    [InlineData("", 3, 0)]
    [Rule("rr:overkill.1")]
    [Rule("rr:wild-resource.1")]
    public void ActualSmashBoostAndConditionalOverkillEndWithExactlyOneAttack(string wildAs, int reduction, int spill)
    {
        // "If this attack defeats a minion" deal excess to the villain; declared wild type is used.
        // Nonempty payment is inferred from ruling:5bbba476a198a90c's identical Force condition.
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        HulkFirstSliceFixture.EmptyHand(world, 0);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var source = world.CreateCard("10003", world.Seats[0].Hand);
        var two = world.CreateCard("01090", world.Seats[0].Hand);
        var wild = world.CreateCard("01011", world.Seats[0].Hand);
        var hero = world.Seats[0].IdentityCard;
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        if (reduction > 0) CardPayment.ReduceNextCardCost(world, hero, 0, reduction);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, minion, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt,
            new(offer.Id, [], reduction == 0 ? [two.ObjectId, wild.ObjectId] : [],
                Allocations: reduction == 0 ? [new(two.ObjectId, 0, "RR"), new(wild.ObjectId, 0, wildAs)] : []), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(spill, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(3, StateFields.Modified(world, hero, "attack", world.Facts, world.Players));
        Assert.False(AttackKeywords.Has(world, hero, hero, Keywords.Overkill, basic: true));
        var next = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        next.TakeDamage(2);
        hero.Refresh();
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, next, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.EncounterDiscardPile, next.Area.Type);
        Assert.Equal(spill, villain.Damage);
        hero.Refresh();
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, villain, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(spill + 3, villain.Damage);
        Assert.False(world.Agenda.IsBusy);
    }
}
