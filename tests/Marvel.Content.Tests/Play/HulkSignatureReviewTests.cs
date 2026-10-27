using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkSignatureReviewTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.4")]
    public void CrushingBlowRequiresPhysicalForTheIncreasedPriceAndRejectsMixedBeforeCommit()
    {
        // "Apply any modifiers to the cost(s)." ruling:8bde2daf20730c27 explicitly
        // requires physical resources for an increase as well as the printed price.
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        HulkFirstSliceFixture.EmptyHand(world, 0);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("10002", world.Seats[0].Hand);
        var physical = world.CreateCard("01090", world.Seats[0].Hand);
        var wild = world.CreateCard("01011", world.Seats[0].Hand);
        // The prepared source uses the existing next-event surcharge mechanism;
        // the actual Crushing Blow row and all other ability rows remain intact.
        var surcharge = world.CreateCard("09027", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        NextEventCost.Register(world, surcharge, 0, 2);
        var target = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        Assert.Equal("3", Assert.Single(runner.Describe(world, action).CostOptions).Cost);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [physical.ObjectId, wild.ObjectId], [target.ObjectId],
                Allocations: [new(physical.ObjectId, 0, "RR"), new(wild.ObjectId, 0, "B")])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        runner.ExecuteAction(world, new PlayerAction(action, [physical.ObjectId, wild.ObjectId], [target.ObjectId],
            Allocations: [new(physical.ObjectId, 0, "RR"), new(wild.ObjectId, 0, "R")]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(3, target.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, surcharge.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("R", 1, true)]
    [InlineData("Y", 1, false)]
    [InlineData("RY", 0, false)]
    [InlineData("RB", 0, false)]
    [Rule("rr:cost.4.1")]
    public void ForceUsesSettledDoubledWildDeclarationsAndIgnoresUnspentIcons(string paidAs, int reduction, bool draw)
    {
        // Resources generated beyond the specified cost "were not paid for that cost".
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        HulkFirstSliceFixture.EmptyHand(world, 0);
        var hero = world.Seats[0].IdentityCard;
        hero.Exhaust();
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("10006", world.Seats[0].Hand);
        var doubledWild = world.CreateCard("01044", world.Seats[0].Hand);
        if (reduction > 0) CardPayment.ReduceNextCardCost(world, hero, 0, reduction);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        int deck = world.Seats[0].Deck.Cards.Count;
        runner.ExecuteAction(world, new PlayerAction(action, [doubledWild.ObjectId], [],
            Allocations: [new(doubledWild.ObjectId, 0, paidAs)]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(hero.Ready);
        Assert.Equal(deck - (draw ? 1 : 0), world.Seats[0].Deck.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, doubledWild.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:overkill.1")]
    public void ToughConsumesActualSmashWithoutDefeatSpillOrALaterBonus()
    {
        // "If this attack defeats a minion" excludes the Tough-protected first attack.
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        HulkFirstSliceFixture.EmptyHand(world, 0);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var source = world.CreateCard("10003", world.Seats[0].Hand);
        var physical = world.CreateCard("10007", world.Seats[0].Hand);
        var hero = world.Seats[0].IdentityCard;
        var minion = world.CreateCard("01121", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, minion, Statuses.Tough);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, minion, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt,
            new(offer.Id, [], [physical.ObjectId], Allocations: [new(physical.ObjectId, 0, "RRR")]), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(0, minion.Damage);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(0, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        hero.Refresh();
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, minion, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(0, villain.Damage);
        hero.Refresh();
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, villain, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(3, villain.Damage);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }
}
