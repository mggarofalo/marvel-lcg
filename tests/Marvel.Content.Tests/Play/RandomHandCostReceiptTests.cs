using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RandomHandCostReceiptTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnlyOutcomeIndependentReceiptCountsAreAdmitted(bool readsPrintedCost)
    {
        string number = readsPrintedCost ? "printedCostDiscarded" : "cardsDiscarded";
        var runner = Runner("""{"discardRandomFromHand":2}""", """
            {"placeCounters":{"card":"you","counter":"paid","count":{"NUMBER":"game"}}}
            """.Replace("NUMBER", number, StringComparison.Ordinal));
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, "01007"), abilities: new NoCardAbilities());
        world.Abilities = runner;
        foreach (var card in world.Seats[0].Hand.Cards.ToArray()) World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        Card[] hand = [world.CreateCard("01005", world.Seats[0].Hand),
            world.CreateCard("01003", world.Seats[0].Hand), world.CreateCard("01007", world.Seats[0].Hand)];
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        if (readsPrintedCost)
        {
            Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(words, world.Random.Generator.WordsConsumed);
            return;
        }
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source!.ObjectId);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(2, world.Seats[0].IdentityCard.Tokens["c_paid"]);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Equal(2, hand.Count(card => card.Area.Type == DeckType.DiscardPile));
    }

    [Theory]
    [InlineData("{\"seq\":[{\"discardRandomFromHand\":1},{\"discardRandomFromHand\":1}]}")]
    [InlineData("{\"seq\":[{\"spend\":\"B\"},{\"discardRandomFromHand\":1}]}")]
    public void UnsupportedCombinedCommitmentsRefuseBeforeMutation(string cost)
    {
        var runner = Runner(cost, """{"discard":"this"}""");
        var (_, world) = Playing(board => InPlay(board, "01007"), abilities: new NoCardAbilities());
        world.Abilities = runner;
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CountMustBePositive(int count) => Assert.Throws<AbilityException>(() =>
        Runner($$"""{"discardRandomFromHand":{{count}}}""", """{"discard":"this"}"""));

    private static AbilityRunner Runner(string cost, string effect) => new(AbilityCatalog.Parse($$"""
        {"cards":[{"card":"01007","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
          "cost":{{cost}},"effect":{{effect}}
        }]}]}
        """));
}
