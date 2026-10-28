using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AntManFormResponseCardTests
{
    [Theory]
    [InlineData("12001c", true)]
    [InlineData("12001a", false)]
    [InlineData("12001b", false)]
    [Rule("rr:response.2.1")]
    [Rule("rr:lasting-effects.5")]
    public void GiantStrengthCopiesFollowTheirControllerAndExpireTogether(string destination, bool offered)
    {
        // "Multiple copies of a card with a response can each be triggered";
        // each lasting effect expires at its specified timing point.
        var (game, world, source, _, _) = AntManFormResponseFixture.Board("12016", destination);
        if (destination == "12001a") game.Resolve(Decision.Decline);
        var strengths = new[]
        {
            world.CreateCard("12009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1)),
            world.CreateCard("12009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1)),
        };
        var other = world.CreateCard("12009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        AntManFormResponseFixture.Change(game, source);
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.AnchorId == other.ObjectId);
        if (!offered)
        {
            Assert.DoesNotContain(game.Pending.Affordances, a => strengths.Any(card => card.ObjectId == a.AnchorId));
            return;
        }
        foreach (var card in strengths)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == card.ObjectId).Id));
        Assert.Equal(5, AntManFormResponseFixture.Value(world, 1, "attack"));
        game.Resolve(Decision.Decline);
        Assert.Equal(1, game.Active);
        Assert.Equal(3, AntManFormResponseFixture.Value(world, 1, "attack"));
    }

    [Theory]
    [InlineData("12001c", 1, true)]
    [InlineData("12001a", 1, true)]
    [InlineData("12001b", 1, false)]
    [InlineData("12001c", 0, false)]
    [Rule("rr:response.1")]
    public void MoxieRequiresItsControllersChangeAndHeroDestination(string destination, int controller, bool offered)
    {
        // rr:response.1: "Players can only trigger response abilities on cards they control".
        // Printed 12016 is a Hero Response and gives the controller's hero the three bonuses.
        var (game, world, source, moxie, _) = AntManFormResponseFixture.Board("12016", destination, controller);
        if (destination == "12001a")
        {
            game.Resolve(Decision.Decline);
        }
        long firstAttack = AntManFormResponseFixture.Value(world, 0, "attack");
        AntManFormResponseFixture.Change(game, source);
        Assert.Equal(destination, world.Seats[1].IdentityCard.FaceId);
        if (offered)
        {
            Assert.Equal(1, game.Pending!.Player);
            var response = Assert.Single(game.Pending.Affordances, a => a.AnchorId == moxie.ObjectId);
            game.Resolve(Decision.Take(response.Id));
            Assert.Equal(destination == "12001a" ? 3 : 4, AntManFormResponseFixture.Value(world, 1, "attack"));
            Assert.Equal(destination == "12001a" ? 3 : 2, AntManFormResponseFixture.Value(world, 1, "thwart"));
            Assert.Equal(destination == "12001a" ? 3 : 4, AntManFormResponseFixture.Value(world, 1, "defense"));
            Assert.Equal(DeckType.DiscardPile, moxie.Area.Type);
        }
        else
        {
            Assert.DoesNotContain(game.Pending!.Affordances, a => a.AnchorId == moxie.ObjectId);
            Assert.Equal(DeckType.HandsArea, moxie.Area.Type);
        }
        Assert.Equal(firstAttack, AntManFormResponseFixture.Value(world, 0, "attack"));
    }

    [Theory]
    [InlineData("12001c", 1, true)]
    [InlineData("12001a", 1, true)]
    [InlineData("12001b", 1, false)]
    [InlineData("12001c", 0, false)]
    [Rule("rr:response.1")]
    public void LawRequiresControllersChangeAndHeroDestination(string destination, int controller, bool offered)
    {
        // Only controlled cards may respond; printed 12031 is specifically a Hero Response.
        var (game, world, source, law, _) = AntManFormResponseFixture.Board("12031", destination, controller);
        world.CreateCard("01089", world.Seats[controller].Hand);
        if (destination == "12001a")
        {
            game.Resolve(Decision.Decline);
        }
        AntManFormResponseFixture.Change(game, source);
        Assert.Equal(destination, world.Seats[1].IdentityCard.FaceId);
        Assert.Equal(offered, game.Pending!.Affordances.Any(a => a.AnchorId == law.ObjectId));
        Assert.Equal(DeckType.HandsArea, law.Area.Type);
    }

    [Fact]
    [Rule("rr:confuse-confused.5")]
    public void ConfusionConsumesLawsPaidThwartWithoutRemovingThreat()
    {
        // Confused replaces the next thwart: "remove the confused status card instead".
        var (game, world, source, law, scheme) = AntManFormResponseFixture.Board("12031", controller: 1);
        var payment = world.CreateCard("01089", world.Seats[1].Hand);
        Statuses.Give(world, world.Seats[1].IdentityCard, Statuses.Confused);
        AntManFormResponseFixture.Change(game, source);
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == law.ObjectId);
        game.Resolve(new Decision(offer.Id, [], [payment.ObjectId],
            Allocations: [new ResourceAllocation(payment.ObjectId, 0, "B")]));
        Assert.Equal(6, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Confused));
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, law.Area.Type);
    }

    [Fact]
    [Rule("rr:lasting-effects.1")]
    [Rule("rr:lasting-effects.5")]
    [Rule("rr:response.2.1")]
    public void TwoMoxiesStackAndPersistPastTurnEndUntilRoundEnd()
    {
        // Lasting effects continue "whether or not the card that created" them is in play.
        // They expire "as soon as the timing point specified by its duration is reached".
        // "Multiple copies of a card with a response can each be triggered".
        var (game, world, source, first, _) = AntManFormResponseFixture.Board("12016", controller: 1);
        var second = world.CreateCard("12016", world.Seats[1].Hand);
        AntManFormResponseFixture.Change(game, source);
        foreach (var card in new[] { first, second })
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == card.ObjectId).Id));
        Assert.Equal(5, AntManFormResponseFixture.Value(world, 1, "attack"));
        Assert.Equal(3, AntManFormResponseFixture.Value(world, 1, "thwart"));
        Assert.Equal(5, AntManFormResponseFixture.Value(world, 1, "defense"));
        game.Resolve(Decision.Decline);
        Assert.Equal(1, game.Active);
        Assert.Equal(5, AntManFormResponseFixture.Value(world, 1, "attack"));
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.Equal(3, AntManFormResponseFixture.Value(world, 1, "attack"));
        Assert.Equal(1, AntManFormResponseFixture.Value(world, 1, "thwart"));
        Assert.Equal(3, AntManFormResponseFixture.Value(world, 1, "defense"));
    }

    [Theory]
    [InlineData("01089", "B", 4)]
    [InlineData("01090", "R", 3)]
    [InlineData("01055", "B", 4)]
    [InlineData("01055", "R", 3)]
    [Rule("rr:resource.5")]
    [Rule("rr:wild-resource.1")]
    public void LawUsesAllocatedMentalAndRejectsInvalidPaymentBeforeRetry(string paymentFace, string paidAs, int removed)
    {
        // "Excess resources generated toward any cost are lost"; a wild's player
        // "may specify which resource type" it pays as. Printed 12031 checks payment.
        var (game, world, source, law, scheme) = AntManFormResponseFixture.Board("12031", controller: 1);
        var payment = world.CreateCard(paymentFace, world.Seats[1].Hand);
        var excessMental = world.CreateCard("01089", world.Seats[1].Hand);
        AntManFormResponseFixture.Change(game, source);
        var response = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == law.ObjectId);
        var prompt = game.Pending;
        string digest = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(response.Id, [], [])));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        game.Resolve(new Decision(response.Id, [], [payment.ObjectId, excessMental.ObjectId],
            Allocations: [new ResourceAllocation(payment.ObjectId, 0, paidAs)]));
        var target = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == scheme.ObjectId);
        var targetPrompt = game.Pending;
        string afterPayment = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(int.MaxValue)));
        Assert.Same(targetPrompt, game.Pending);
        Assert.Equal(afterPayment, world.Digest().Canonical());
        game.Resolve(Decision.Take(target.Id));
        Assert.Equal(6 - removed, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.DiscardPile, law.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, excessMental.Area.Type);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }
}
