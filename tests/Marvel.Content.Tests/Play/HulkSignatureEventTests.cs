using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkSignatureEventTests
{
    [Theory]
    [InlineData(0, "R")]
    [InlineData(1, "")]
    [Rule("rr:initiating-abilities.step.4")]
    [Rule("rr:wild-resource.1")]
    public void CrushingBlowPaysAdjustedPhysicalPriceAndReadsModifiedAttack(int reduction, string declaration)
    {
        // "Apply any modifiers to the cost(s)." Wilds may specify "which resource type" is used.
        // ruling:5bbba476a198a90c directly permits zero-price Crushing Blow without physical payment.
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        HulkFirstSliceFixture.EmptyHand(world, 0);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("10009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var source = world.CreateCard("10002", world.Seats[0].Hand);
        var wild = world.CreateCard("01011", world.Seats[0].Hand);
        var target = world.TheCardIn(DeckType.VillainArea)!;
        if (reduction > 0) CardPayment.ReduceNextCardCost(world, world.Seats[0].IdentityCard, 0, reduction);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        var cost = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal((1 - reduction).ToString(), cost.Cost);
        runner.ExecuteAction(world, new PlayerAction(action, reduction == 0 ? [wild.ObjectId] : [], [target.ObjectId],
            Allocations: reduction == 0 ? [new(wild.ObjectId, 0, declaration)] : []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(4, target.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(reduction == 0 ? DeckType.DiscardPile : DeckType.HandsArea, wild.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("B")]
    [InlineData("G")]
    [Rule("rr:wild-resource.1")]
    public void CrushingBlowRejectsWildDeclaredAsNonphysicalWithoutCommit(string declaration)
    {
        // The generated wild's specified resource type governs payment.
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        HulkFirstSliceFixture.EmptyHand(world, 0);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("10002", world.Seats[0].Hand);
        var wild = world.CreateCard("01011", world.Seats[0].Hand);
        var target = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [wild.ObjectId], [target.ObjectId], Allocations: [new(wild.ObjectId, 0, declaration)])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:identity.2")]
    public void ForceReadiesNamedHulkAcrossControllersAndDrawsForThePlayingHero()
    {
        // A named hero refers "only to the identity with that title".
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { }, owner: 1);
        HulkFirstSliceFixture.EmptyHand(world, 0);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var controller = world.Seats[0].IdentityCard;
        var hulk = world.Seats[1].IdentityCard;
        controller.Exhaust();
        hulk.Exhaust();
        var source = world.CreateCard("10006", world.Seats[0].Hand);
        var payment = world.CreateCard("01090", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        int controllerDeck = world.Seats[0].Deck.Cards.Count;
        int hulkDeck = world.Seats[1].Deck.Cards.Count;
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [],
            Allocations: [new(payment.ObjectId, 0, "RR")]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(hulk.Ready);
        Assert.False(controller.Ready);
        Assert.Equal(controllerDeck - 1, world.Seats[0].Deck.Cards.Count);
        Assert.Equal(hulkDeck, world.Seats[1].Deck.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    [Theory]
    [InlineData(false, "RR", 0, true)]
    [InlineData(false, "RG", 0, false)]
    [InlineData(true, "RR", 0, true)]
    [InlineData(true, "RG", 0, false)]
    [InlineData(false, "", 2, false)]
    [Rule("rr:target.3.4")]
    public void ForceReadiesAndIndependentlyDrawsOnlyForSettledPhysicalPayment(bool alreadyReady, string paidAs, int reduction, bool draw)
    {
        // A multiple-effect target is valid "if at least one of those effects can affect the target".
        // No printed "then": ready failure does not prevent the independent draw.
        // ruling:5bbba476a198a90c directly says zero-price Force does not draw.
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        HulkFirstSliceFixture.EmptyHand(world, 0);
        var hero = world.Seats[0].IdentityCard;
        if (!alreadyReady) hero.Exhaust();
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("10006", world.Seats[0].Hand);
        var payment = world.CreateCard("01044", world.Seats[0].Hand);
        if (reduction > 0) CardPayment.ReduceNextCardCost(world, hero, 0, reduction);
        var offers = runner.Actions(world, 0).Where(a => a.Card == source.ObjectId).ToArray();
        var action = Assert.Single(offers);
        if (alreadyReady && !draw)
        {
            string before = world.Digest().Canonical();
            Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
                new PlayerAction(action, [payment.ObjectId], [], Allocations: [new(payment.ObjectId, 0, paidAs)])));
            Assert.Equal(before, world.Digest().Canonical());
            return;
        }
        int deck = world.Seats[0].Deck.Cards.Count;
        runner.ExecuteAction(world, new PlayerAction(action, reduction == 0 ? [payment.ObjectId] : [], [],
            Allocations: reduction == 0 ? [new(payment.ObjectId, 0, paidAs)] : []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(hero.Ready);
        Assert.Equal(deck - (draw ? 1 : 0), world.Seats[0].Deck.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(reduction == 0 ? DeckType.DiscardPile : DeckType.HandsArea, payment.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
