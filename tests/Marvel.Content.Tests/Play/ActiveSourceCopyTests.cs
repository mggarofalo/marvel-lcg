using Marvel.Rules.Play;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ActiveSourceCopyTests
{
    [Rule("rr:leaves-play.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResumingALiveSourceKeepsItsChangesUntilItsCopyEnds(bool suspendAfterDeparture)
    {
        // A departed card has "no memory of its previous state". The resolving
        // effect retains its own last-known quantities from that ended copy.
        string secondChoice = suspendAfterDeparture
            ? """{"choose":{"options":[{"draw":{"player":"you","count":1}},{"draw":{"player":"you","count":2}}]}},""" : "";
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"seq":[
              {"choose":{"options":[{"draw":{"player":"you","count":1}},{"draw":{"player":"you","count":2}}]}},
              {"placeCounters":{"card":"this","counter":"test","count":2}},
              {"discard":"this"},
              __SECOND_CHOICE__
              {"placeCounters":{"card":"you","counter":"result","count":{"countersOn":{"card":"this","counter":"test"}}}}
            ]}
            """.Replace("__SECOND_CHOICE__", secondChoice, StringComparison.Ordinal));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            source.PlaceTokens("c_test", 3);
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Option, game.Pending!.Asking);
        game.Resolve(Decision.Take(0));
        if (suspendAfterDeparture)
        {
            Assert.Equal(Question.Option, game.Pending!.Asking);
            Assert.Equal(0, source!.Tokens["c_test"]);
            game.Resolve(Decision.Take(0));
        }
        Assert.Equal(5, world.Seats[0].IdentityCard.Tokens["c_result"]);
        Assert.Equal(0, source!.Tokens["c_test"]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }
}
