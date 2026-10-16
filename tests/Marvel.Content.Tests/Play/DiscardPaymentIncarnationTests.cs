using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DiscardPaymentIncarnationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:leaves-play.1")]
    public void RequiredHandDiscardsValidateEveryAcceptedCopyBeforeCommitting(bool reenter)
    {
        // "If ... the cost(s) cannot be paid, abort this process without paying any costs."
        // Synthetic preparation makes an intervening departure observable at commitment.
        var (program, runner) = PaymentRunner(AuthoredCards.AuntMay,
            """{"draw":{"player":"you","count":1}}""", cost: """{"discardFromHand":2}""");
        var (world, source) = OrderedPaymentBoard(runner);
        var hand = world.Seats[0].Hand;
        var first = world.CreateCard("01088", hand);
        var second = world.CreateCard("01089", hand);
        var payment = AbilityCostPayment.Prepare(world, source, 0, Assert.Single(program.On(source.FaceId)).Cost,
            program, runner, AbilityPaymentSelection.Capture([], [first.ObjectId, second.ObjectId]));
        if (reenter)
        {
            World.MoveToTop(second, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            Discard.Card(world, second, CardPlay.Verb, []);
            World.MoveToTop(second, hand);
        }
        var before = world.Digest().Canonical();
        var events = new List<GameEvent>();
        if (reenter)
        {
            Assert.Throws<RulesNotImplementedException>(() => PreparedCostPayment.Commit(world, payment, runner, events));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Empty(events);
            return;
        }
        PreparedCostPayment.Commit(world, payment, runner, events);
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
    }
}
