using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic Core-card probes of the distinct-arrow engine boundary.</summary>
public sealed class IndependentDistinctArrowReviewTests
{
    private static readonly string[] ResourceFaces = ["01088", "01089", "01090", "01055"];
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.step.5")]
    public void InvalidComponentAllocationPreservesPhysicalCostsAndAllowsRetry(bool reverse)
    {
        // If a cost cannot be paid, "abort the process without paying any costs."
        string costs = reverse
            ? """{"seq":[{"exhaust":"this"},{"spend":"Y"},{"spendDistinct":2}]}"""
            : """{"seq":[{"exhaust":"this"},{"spendDistinct":2},{"spend":"Y"}]}""";
        var runner = Runner("01006", "Action", """{"draw":{"player":"you","count":1}}""", cost: costs);
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, "01006"), abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.ToArray()) Discard.Card(world, card, "test", []);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var mental = world.CreateCard("01089", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        int distinct = reverse ? 1 : 0;
        int ordinary = 1 - distinct;
        int[] paying = [energy.ObjectId, mental.ObjectId];
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, paying, [], Allocations:
            [new(energy.ObjectId, distinct, "YY"), new(mental.ObjectId, ordinary, "Y")])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.True(source!.Ready);
        runner.ExecuteAction(world, new PlayerAction(action, paying, [], Allocations:
            [new(energy.ObjectId, ordinary, "Y"), new(energy.ObjectId, distinct, "Y"),
             new(mental.ObjectId, distinct, "B")]));
        Assert.False(source.Ready);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(DeckType.DiscardPile, mental.Area.Type);
        Assert.Single(world.Seats[0].Hand.Cards);
    }

    [Fact]
    [Rule("rr:wild-resource.1")]
    public void PreparedFourTypePaymentSnapshotsCallerSelections()
    {
        // A wild may be used as "energy, mental, physical, or wild".
        var book = AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"spendDistinct":4},"effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """);
        var program = AbilityLowering.Book(book);
        var runner = new AbilityRunner(program);
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, "01006"), abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.ToArray()) Discard.Card(world, card, "test", []);
        var payment = ResourceFaces
            .Select(face => world.CreateCard(face, world.Seats[0].Hand)).ToArray();
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        var paying = payment.Select(card => card.ObjectId).ToList();
        var allocation = ResourcePayment.Allocate(price, paying)!.ToList();
        Assert.Equal("BGRY", string.Concat(allocation.SelectMany(part => part.PaidAs).Order()));
        var prepared = AbilityCostPayment.Prepare(world, source!, 0, program.Abilities[0].Cost,
            program, runner, AbilityPaymentSelection.Capture(paying, [], allocations: allocation));
        paying.Clear();
        allocation.Clear();
        var result = PreparedCostPayment.Commit(world, prepared, runner, []);
        Assert.False(result.Suspended);
        Assert.All(payment, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EventDistinctArrowCombinationRaisesBeforeAnyPayment(bool sequence)
    {
        string cost = sequence ? """{"seq":[{"spendAny":1},{"spendDistinct":2}]}""" : """{"spendDistinct":2}""";
        var runner = Runner("01005", "Action", """{"draw":{"player":"you","count":1}}""", cost: cost);
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = world.CreateCard("01005", world.Seats[0].Hand);
        foreach (string face in ResourceFaces)
            world.CreateCard(face, world.Seats[0].Hand);
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Contains("distinct-type arrow cost", error.Message, StringComparison.Ordinal);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
        var control = Runner("01005", "Action", """{"draw":{"player":"you","count":1}}""", cost: """{"spendAny":2}""");
        Assert.Contains(control.Actions(world, 0), offer => offer.Card == source.ObjectId);
    }
}
