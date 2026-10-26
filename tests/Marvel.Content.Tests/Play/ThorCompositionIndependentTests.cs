using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic boards use the actual printed catalog and complete authored book.
public sealed class ThorCompositionIndependentTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:cost.7")]
    public void ForeignMjolnirCannotPayHammerThrowDespiteItsMatchingTitle()
    {
        // A player "must pay costs with cards and/or game elements they control."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(0), cardOwner: 0));
        var attack = world.CreateCard("06005", world.Seats[0].Hand);
        var payment = Enumerable.Range(0, 3)
            .Select(_ => world.CreateCard("01003", world.Seats[0].Hand).ObjectId).ToArray();
        var previouslyOffered = Assert.Single(runner.Actions(world, 0), a => a.Card == attack.ObjectId);
        World.MoveToTop(hammer, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 0));
        var before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == attack.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(previouslyOffered, payment, [])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.True(hammer.Ready);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void ActualHerculesRepricesAfterEngagementChangesAndPreservesDiscountOnRejection()
    {
        // If costs cannot be paid, "abort this process without paying any costs."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[1];
        var ally = world.CreateCard("06011", seat.Hand);
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var payment = Enumerable.Range(0, 5).Select(_ => world.CreateCard("01003", seat.Hand).ObjectId).ToArray();
        CardPayment.ReduceNextCardCost(world, seat.IdentityCard, 1, 1);
        Assert.Equal("4", CardPayment.Price(world, world.Facts, seat, ally)!.Cost);
        World.MoveToTop(minion, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var effects = world.Effects.Registered.ToArray();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Equal("5", CardPayment.Price(world, world.Facts, seat, ally)!.Cost);
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, world.Facts,
            runner, seat, ally, payment[..4], []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Equal(pending, world.Agenda.Outstanding);
        CardPlay.Play(world, world.Facts, runner, seat, ally, payment, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(PlayArea.Of(1), ally.Area.PlayArea);
        Assert.All(payment, id => Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:ally-limit")]
    [Rule("rr:unique-icon.4.1")]
    public void ActualHerculesAtZeroPriceStillHonorsAllyLimitAndUniqueness()
    {
        // At more than three allies, "immediately choose and discard" one.
        // A matching unique player card "cannot be played or put into play."
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var ally = world.CreateCard("06011", seat.Hand);
        for (int i = 0; i < 5; i++)
            world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var allies = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var departing = world.CreateCard("01067", allies);
        world.CreateCard("01068", allies);
        world.CreateCard("01070", allies);
        CardPayment.ReduceNextCardCost(world, seat.IdentityCard, 0, 2);
        Assert.Equal("0", CardPayment.Price(world, world.Facts, seat, ally)!.Cost);
        CardPlay.Play(world, world.Facts, runner, seat, ally, [], []);
        var limit = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.False(limit.Cancellable);
        Assert.Equal(4, ally.Area.Cards.Count);
        var discard = Assert.Single(limit.Affordances, a => a.AnchorId == departing.ObjectId);
        Sequence.Answer(world, world.Facts, runner, limit, Decision.Take(discard.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.DiscardPile, departing.Area.Type);
        Assert.Equal(3, ally.Area.Cards.Count);
        var copy = world.CreateCard("06011", seat.Hand);
        Assert.Null(CardPayment.Price(world, world.Facts, seat, copy));
        Assert.Equal(1, CardPayment.CostOf(world, world.Facts, seat, copy).Amount);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MeanSwingPaysSelectedActualWeaponAndDecliningPreservesBothWeapons(bool accept)
    {
        // Printed 06015: "exhaust a Weapon upgrade on your hero" for "this attack."
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var hero = world.Seats[0].IdentityCard;
        var attached = world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), hero.ObjectId, cardOwner: 0);
        var axe = world.CreateCard("06019", attached);
        var hammer = world.CreateCard("06009", attached);
        var swing = world.CreateCard("06015", world.Seats[0].Hand);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, villain, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == swing.ObjectId);
        Assert.Equal(new[] { axe.ObjectId, hammer.ObjectId }.Order(), offer.Targets!.Legal.Order());
        Assert.Contains("Current attack bonus: 0", prompt.Description);
        Assert.Equal(new[] { hero.ObjectId, villain.ObjectId }, prompt.ContextCardIds);
        Sequence.Answer(world, world.Facts, runner, prompt,
            accept ? new Decision(offer.Id, [hammer.ObjectId]) : Decision.Decline, []);
        var aftermath = Sequence.Work(world, world.Facts, runner, []);
        if (!accept)
        {
            // The held Mean Swing can pay Jarnbjorn's physical response; decline it explicitly.
            Assert.NotNull(aftermath);
            Assert.Single(aftermath.Affordances, response => response.AnchorId == axe.ObjectId);
            Sequence.Answer(world, world.Facts, runner, aftermath, Decision.Decline, []);
        }
        else Assert.Null(aftermath);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(axe.Ready);
        Assert.Equal(!accept, hammer.Ready);
        Assert.Equal(accept ? DeckType.DiscardPile : DeckType.HandsArea, swing.Area.Type);
        Assert.Equal(accept ? 6 : 3, villain.Damage);
        Assert.Equal(3, StateFields.Modified(world, hero, "attack", world.Facts, world.Players));
        Assert.Empty(world.Agenda.Outstanding);
    }
}
