using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaFalconTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 2)]
    [Rule("rr:look-looked-at.1.1")]
    [Rule("rr:ability.11")]
    public void EntryInspectsWithoutMovingCardsAndEachTreacheryChoosesItsOwnScheme(bool decline, int treacheries)
    {
        // rr:look-looked-at.1.1: looked-at cards are returned "in the same order".
        // rr:ability.11: responses without "Forced" are optional.
        Card? falcon = null;
        Card? side = null;
        int[] payment = [];
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            falcon = board.CreateCard("03011", board.Seats[0].Hand);
            payment = [board.CreateCard("03022", board.Seats[0].Hand).ObjectId,
                board.CreateCard("03023", board.Seats[0].Hand).ObjectId];
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
            side = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
            side.PlaceTokens("k_threat", 2);
            var deck = board.AreaOf(DeckType.EncounterDeck);
            for (int index = 0; index < 3; index++)
                board.CreateCard(index < treacheries ? "01104" : "01121", deck);
        });
        var deck = world.AreaOf(DeckType.EncounterDeck);
        int[] order = [.. deck.Cards.Select(card => card.ObjectId)];
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long mainBefore = main.Tokens.GetValueOrDefault("k_threat");
        var play = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == falcon!.ObjectId && offer.PlaysCard);
        game.Resolve(new Decision(play.Id, [world.Seats[0].IdentityCard.ObjectId], payment,
            Allocations: ResourcePayment.Allocate(play.CostOptions[0], payment)));
        if (decline) game.Resolve(Decision.Decline);
        else
        {
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
                offer => offer.AnchorId == falcon!.ObjectId).Id));
            Assert.Equal(PublicDecisionKind.CardLook, game.Pending!.PublicKind);
            Assert.Equal(order.AsEnumerable().Reverse().Take(3), game.Pending.ContextCardIds);
            Assert.Equal(mainBefore, main.Tokens.GetValueOrDefault("k_threat"));
            game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));
            ResolveThreatChoices(game, main, side!, treacheries);
        }
        Assert.Equal(mainBefore - (decline ? 0 : (treacheries + 1) / 2), main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(2 - (decline ? 0 : treacheries / 2), side!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(order, deck.Cards.Select(card => card.ObjectId));
        Assert.All(deck.Cards, card => Assert.False(card.FaceUp));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    private static void ResolveThreatChoices(Game game, Card main, Card side, int count)
    {
        for (int index = 0; index < count; index++)
        {
            int target = index % 2 == 0 ? main.ObjectId : side.ObjectId;
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
                offer => offer.AnchorId == target).Id));
        }
    }
}
