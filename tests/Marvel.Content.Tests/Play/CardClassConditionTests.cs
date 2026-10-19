using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CardClassConditionTests
{
    [Theory]
    [InlineData("04035", "Aggression", true)]
    [InlineData("04036", "Leadership", true)]
    [InlineData("04037", "Protection", true)]
    [InlineData("04038", "Justice", true)]
    [InlineData("04035", "Justice", false)]
    [InlineData("01091", "Aggression", false)]
    public void AspectSignaturesMatchTheirPrintedClass(string face, string classification, bool matches)
    {
        // Synthetic action distinguishes printed aspect from identity-set membership.
        var runner = Runner("01006", "Action", """
            {"placeCounters":{"card":"this","counter":"observed","count":
              {"if":{"test":{"cardClass":{"card":{"cardsIn":{"area":"yourDiscardPile"}},"class":"CLASS"}},"then":2,"else":1}}}}
            """.Replace("CLASS", classification, StringComparison.Ordinal), cost: """{"exhaust":"this"}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "01006");
        world.CreateCard(face, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(matches ? 2 : 1, source.Tokens["c_observed"]);
    }
}
