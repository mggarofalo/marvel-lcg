using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// A synthetic capability deliberately advertises a different type from its committed
// receipt; this isolates the payment contract, not a reachable authored card ability.
public sealed class EventAllocationCommitTests
{
    [Theory]
    [InlineData("Y", true)]
    [InlineData("R", false)]
    [Rule("rr:cost.5")]
    public void CommittedGeneratorMustSatisfyItsAcceptedAllocation(string actual, bool valid)
    {
        // rr:cost.5.1: the player "chooses how to divide" resources between costs.
        var (program, runner) = PaymentRunner(AuthoredCards.Backflip,
            """{"draw":{"player":"you","count":1}}""", """{"spend":"Y"}""");
        var (world, _) = OrderedPaymentBoard(runner);
        var seat = world.Seats[0];
        var card = world.CreateCard(AuthoredCards.Backflip, seat.Hand);
        var generator = InPlay(world, "01091");
        var spare = world.CreateCard("01088", seat.Hand);
        var probe = new DivergingGenerator(generator, actual);
        var ability = Assert.Single(program.On(card.FaceId));
        int[] selected = [generator.ObjectId, spare.ObjectId];
        ResourceAllocation[] allocations = [new(generator.ObjectId, 1, "Y")];
        var selection = AbilityPaymentSelection.Capture(selected, [], allocations: allocations);
        var payment = Assert.IsType<AbilityEventPayment>(
            AbilityEventPayment.Prepare(world, card, 0, ability, probe, selection));
        var occurrence = new Occurrence(0, [Steps.TurnAction], Subject: card.ObjectId, Player: 0);

        if (!valid)
        {
            var error = Assert.Throws<RulesNotImplementedException>(() => payment.Commit(occurrence, []));
            Assert.Contains("accepted allocation", error.Message, StringComparison.Ordinal);
            Assert.False(occurrence.Is(Steps.CardPlayed));
            Assert.DoesNotContain(world.Effects.Active(), effect => effect.Card == card.ObjectId
                && effect.Kind.StartsWith("paid:", StringComparison.Ordinal));
            return;
        }

        Assert.True(payment.Commit(occurrence, []));
        Assert.Equal("Y", payment.Paid);
        Assert.True(occurrence.Is(Steps.CardPlayed));
        Assert.Equal(DeckType.DiscardPile, spare.Area.Type);
    }

    [Fact]
    [Rule("rr:cost.5")]
    public void VariableArrowPaymentRetainsItsAcceptedValueAndSourceAllocation()
    {
        // The zero printed price and declared energy arrow price are simultaneous costs.
        var (program, runner) = PaymentRunner(AuthoredCards.Backflip,
            """{"draw":{"player":"you","count":1}}""", """{"spendEnergyX":"Y"}""");
        var (world, _) = OrderedPaymentBoard(runner);
        var card = world.CreateCard(AuthoredCards.Backflip, world.Seats[0].Hand);
        var generator = InPlay(world, "01091");
        var probe = new DivergingGenerator(generator, "Y");
        var values = new Dictionary<string, long> { ["X"] = 1 };
        var allocations = new List<ResourceAllocation> { new(generator.ObjectId, 1, "Y") };
        var selection = AbilityPaymentSelection.Capture([generator.ObjectId], [], values, allocations: allocations);
        var payment = Assert.IsType<AbilityEventPayment>(
            AbilityEventPayment.Prepare(world, card, 0, Assert.Single(program.On(card.FaceId)), probe, selection));
        values["X"] = 2;
        allocations.Clear();

        Assert.True(payment.Commit(new Occurrence(0, [Steps.TurnAction], Subject: card.ObjectId, Player: 0), []));
        Assert.Equal("Y", payment.Paid);
        Assert.Equal(1, payment.Energy);
    }

    private sealed class DivergingGenerator(Card generator, string actual) : NoCardAbilities
    {
        public override IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player) =>
            [new(generator.ObjectId, "Y")];

        public override ResourceExecutionResult UseResource(World world, AbilityExecution entry, int player, int card)
        {
            var receipt = new ResourceGenerationReceipt(world, entry);
            receipt.Complete(world, entry, actual);
            return new(entry.Finish([]), receipt);
        }
    }
}
