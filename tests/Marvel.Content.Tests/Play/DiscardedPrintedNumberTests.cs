using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic effects exercise numeric receipts using canonical printed cards.</summary>
public sealed class DiscardedPrintedNumberTests
{
    [Theory]
    [InlineData("cardsDiscarded", "yourDeck", "01088,01088,01089", 3)]
    [InlineData("cardsDiscarded", "yourDeck", "", 0)]
    [InlineData("printedResourceIconsDiscarded", "yourDeck", "01088,01088,01089,01090,01055", 9)]
    [InlineData("distinctPrintedResourceTypesDiscarded", "yourDeck", "01088,01088,01089,01090,01055", 4)]
    [InlineData("distinctPrintedResourceTypesDiscarded", "yourDeck", "01088,01088", 1)]
    [InlineData("boostIconsDiscarded", "encounterDeck", "01100,01101,01121", 3)]
    [InlineData("printedResourceIconsDiscarded", "yourDeck", "", 0)]
    [InlineData("distinctPrintedResourceTypesDiscarded", "yourDeck", "", 0)]
    [InlineData("boostIconsDiscarded", "encounterDeck", "", 0)]
    public void CountsPrintedIconsWithoutConditionalMultipliersOrBoostStars(string expression, string area, string faces, int expected)
    {
        string[] discarded = faces.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var runner = Runner("01006", "Action", $$$$"""
            {"seq":[
              {"discardTop":{"from":"{{{{area}}}}","count":{{{{discarded.Length}}}}}},
              {"placeCounters":{"card":"this","counter":"observed","count":{"add":[1,{"{{{{expression}}}}":"game"}]}}}
            ]}
            """, cost: """{"exhaust":"this"}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "01006");
        var deck = area == "yourDeck" ? world.Seats[0].Deck : world.AreaOf(DeckType.EncounterDeck);
        foreach (string face in discarded) world.CreateCard(face, deck);
        string before = world.Digest().Canonical();
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(expected + 1, source.Tokens["c_observed"]);
        Assert.False(source.Ready);
    }
}
