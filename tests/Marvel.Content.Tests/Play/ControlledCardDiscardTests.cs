using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ControlledCardDiscardTests
{
    [Fact]
    [Rule("rr:identity.3")]
    public void ChoiceIncludesControlledAlliesAndSupportsButNotIdentitiesOrOtherPlayersCards()
    {
        // "Identity cards cannot be discarded from play."
        var runner = Runner("01007", "Action", """
            {"chooseCard":{"from":{"discardable":{"query":"cardsYouControl"}},"effect":{"discard":"chosen"}}}
            """);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, "01007"),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var ally = world.CreateCard("01011", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var other = world.CreateCard("01011", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId).Id));
        var options = game.Pending!.Affordances;
        Assert.Contains(options, item => item.AnchorId == ally.ObjectId);
        Assert.Contains(options, item => item.AnchorId == source!.ObjectId);
        Assert.DoesNotContain(options, item => item.AnchorId == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(options, item => item.AnchorId == other.ObjectId);
        game.Resolve(Decision.Take(Assert.Single(options, item => item.AnchorId == ally.ObjectId).Id));
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(DeckType.AlliesArea, other.Area.Type);
    }

    [Fact]
    [Rule("rr:identity.3")]
    public void DiscardCannotMoveAnInPlayIdentity()
    {
        // "Identity cards cannot be discarded from play."
        var (_, world) = Playing(_ => { });
        var identity = world.Seats[0].IdentityCard;
        var area = identity.Area;
        var events = new List<GameEvent>();
        Discard.Card(world, identity, "test", events);
        Assert.Same(area, identity.Area);
        Assert.Empty(events);
    }
}
