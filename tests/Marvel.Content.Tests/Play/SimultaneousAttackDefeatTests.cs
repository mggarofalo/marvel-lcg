using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SimultaneousAttackDefeatTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:damage.step.7")]
    [Rule("rr:damage.step.8")]
    [Rule("rr:forced.5")]
    public void ShieldTossLetsFirstPlayerOrderBothSoldiers(int chosen)
    {
        // rr:forced.5: "the first player determines the order" of simultaneous forced abilities.
        // rr:damage.step.7-.8: When Defeated abilities precede discarding.
        Card? toss = null;
        Card[] held = [];
        Card[] soldiers = [];
        Card? top = null;
        Card? next = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            toss = board.CreateCard("03006", board.Seats[0].Hand);
            board.CreateCard("03009", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), board.Seats[0].IdentityCard.ObjectId, 0));
            held = [board.CreateCard("03003", board.Seats[0].Hand), board.CreateCard("03003", board.Seats[0].Hand)];
            soldiers = [.. Enumerable.Range(0, 2).Select(player => board.CreateCard("01182",
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player))))];
            var deck = board.AreaOf(DeckType.EncounterDeck);
            top = deck.Cards[0];
            next = deck.Cards.First(card => card.FaceId != top.FaceId);
            World.MoveToTop(next, deck);
            World.MoveToTop(top, deck);
        }, twoPlayers: true);
        world.FirstPlayer = 1;
        var play = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == toss!.ObjectId && offer.PlaysCard);
        game.Resolve(new Decision(play.Id, [.. held.Select(card => card.ObjectId)]));
        var attack = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(attack.Id, [.. soldiers.Select(card => card.ObjectId)]));
        Assert.Equal(Question.Order, game.Pending!.Asking);
        Assert.Equal(1, game.Pending.Player);
        Assert.All(soldiers, card => Assert.Equal(DeckType.EngagedEnemiesArea, card.Area.Type));
        game.Resolve(Decision.Take(soldiers[chosen].ObjectId));
        Assert.Same(top, Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(chosen)).Cards));
        Assert.Same(next, Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1 - chosen)).Cards));
        Assert.All(soldiers, card => Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type));
        Assert.Equal(DeckType.DiscardPile, toss!.Area.Type);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }
}


