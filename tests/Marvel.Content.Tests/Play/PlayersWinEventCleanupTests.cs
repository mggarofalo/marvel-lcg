using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic event tests cleanup after a terminal effect, not printed card text.
public sealed class PlayersWinEventCleanupTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:winning-the-game.1")]
    public void EventRemainsResolvingWhenItsEffectEndsTheGame(bool win)
    {
        // "If these conditions are met, the players win or lose accordingly."
        string effect = win ? """{"playersWin":"game"}""" : """{"draw":{"player":"you","count":1}}""";
        var runner = Runner("01004", "Action", effect);
        Card? card = null;
        Card? resource = null;
        var (_, world) = Playing(board =>
        {
            card = board.CreateCard("01004", board.Seats[0].Hand);
            resource = board.CreateCard("01088", board.Seats[0].Hand);
        }, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [resource!.ObjectId], [])).Events;
        Assert.Equal(win ? Outcome.PlayersWin : Outcome.Unfinished, world.Result);
        Assert.Equal(win ? DeckType.RevealingArea : DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(win ? 0 : 1, events.OfType<CardsMoved>().Count(move =>
            move.Cards.Any(landing => landing.Card == card.ObjectId)
            && move.To.Zone == nameof(DeckType.DiscardPile)));
    }
}
