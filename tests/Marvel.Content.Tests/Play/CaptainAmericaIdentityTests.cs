using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaIdentityTests
{
    [Fact]
    [Rule("rr:limit")]
    public void ActualReadyAbilityPaysOneHeldCardAndRetainsRoundLimitAcrossForms()
    {
        // rr:limit: "per instance of that ability." Printed03001a: "Discard 1 card from your hand".
        Card? payment = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            board.Seats[0].IdentityCard.Exhaust();
            payment = board.CreateCard("01088", board.Seats[0].Hand);
        });
        var identity = world.Seats[0].IdentityCard;
        var action = Assert.Single(game.Pending!.Affordances,
            item => item.AnchorId == identity.ObjectId && item.Verb == Game.ActionVerb);
        game.Resolve(new Decision(action.Id, [payment!.ObjectId]));
        Assert.True(identity.Ready);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        identity.Exhaust();
        identity.TurnTo("03001b");
        identity.TurnTo("03001a");
        Assert.DoesNotContain(AuthoredCards.Runner().Actions(world, 0), item => item.Card == identity.ObjectId);
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.Contains(AuthoredCards.Runner().Actions(world, 0), item => item.Card == identity.ObjectId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:ready.1")]
    public void ActualReadyAbilityRequiresAnExhaustedIdentityAndHeldPayment(bool noHand)
    {
        // rr:ready.1: "If a player is instructed to ready an exhausted card".
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            if (!noHand) return;
            board.Seats[0].IdentityCard.Exhaust();
        });
        if (noHand)
        {
            foreach (var card in world.Seats[0].Hand.Cards.ToArray())
                World.MoveToTop(card, world.Seats[0].Deck);
            Assert.Empty(world.Seats[0].Hand.Cards);
        }
        Assert.DoesNotContain(AuthoredCards.Runner().Actions(world, 0),
            item => item.Card == world.Seats[0].IdentityCard.ObjectId);
    }

    [Theory]
    [InlineData("03001a", 0)]
    [InlineData("03001b", 1)]
    public void ActualLivingLegendRequiresSteveRogersAndRegistersBothPrintedFaces(string face, int discount)
    {
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(_ => { });
        world.Seats[0].IdentityCard.TurnTo(face);
        var runner = AuthoredCards.Runner();
        Assert.True(runner.RequiresAllyPlayHistory(world));
        var ally = world.CreateCard("01068", world.Seats[0].Hand);
        Assert.Equal(4 - discount, CardPayment.CostOf(world, world.Facts, world.Seats[0], ally).Amount);
    }

    [Fact]
    public void AnActualHeroFormAllyPlayConsumesLivingLegendBeforeChangingToSteveRogers()
    {
        // Official FAQ03001a: "the very first ally played each round".
        Card? first = null;
        Card? later = null;
        int[] payment = [];
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            first = board.CreateCard("01068", board.Seats[0].Hand);
            later = board.CreateCard("01083", board.Seats[0].Hand);
            payment = [board.CreateCard("01088", board.Seats[0].Hand).ObjectId,
                board.CreateCard("01090", board.Seats[0].Hand).ObjectId];
        });
        var play = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == first!.ObjectId && item.PlaysCard);
        game.Resolve(new Decision(play.Id, [world.Seats[0].IdentityCard.ObjectId], payment,
            Allocations: ResourcePayment.Allocate(play.CostOptions[0], payment)));
        Assert.Equal(DeckType.AlliesArea, first!.Area.Type);
        var change = Assert.Single(game.Pending!.Affordances, item => item.Verb == Game.ChangeForm);
        game.Resolve(Decision.Take(change.Id));
        Assert.Equal("03001b", world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(3, CardPayment.CostOf(world, world.Facts, world.Seats[0], later!).Amount);
    }

    [Theory]
    [InlineData("other-hand")]
    [InlineData("in-play")]
    [InlineData("duplicate")]
    [Rule("rr:cost.7")]
    public void InvalidReadyPaymentDoesNotMutateAndTheSamePromptStillAcceptsALegalCard(string invalid)
    {
        // rr:cost.7: "that player must pay costs with cards and/or game elements they control."
        Card? payment = null;
        Card? wrong = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            board.Seats[0].IdentityCard.Exhaust();
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            wrong = board.CreateCard("01093", invalid == "other-hand" ? board.Seats[1].Hand
                : board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        }, twoPlayers: true);
        var prompt = game.Pending!;
        var action = Assert.Single(prompt.Affordances,
            item => item.AnchorId == world.Seats[0].IdentityCard.ObjectId && item.Verb == Game.ActionVerb);
        string digest = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        var agenda = world.Agenda.Outstanding.ToArray();
        int[] selection = invalid == "duplicate" ? [payment!.ObjectId, payment.ObjectId] : [wrong!.ObjectId];
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(action.Id, selection)));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Assert.Same(prompt, game.Pending);
        game.Resolve(new Decision(action.Id, [payment!.ObjectId]));
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }
}
