using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Prepared nonpublic two-seat board; actual authored Invocation programs, no public setup claim.</summary>
internal static class InvocationNativeScene
{
    internal static OpenedGame Create(CardCatalog facts, AbilityRunner abilities)
    {
        var world = new World(facts, 2, 5489);
        for (int index = 0; index < 2; index++)
        {
            var seat = world.CreateSeat($"p{index}");
            seat.IdentityCard = world.CreateCard(index == 1 ? "09001b,09001a" : "01001b,01001a", seat.Hero);
            for (int count = 0; count < 20; count++) world.CreateCard("01088", seat.Deck);
        }
        world.CreateCard("01134", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01137b", world.AreaOf(DeckType.MainSchemesArea));
        var aside = world.CreateArea(DeckType.AsideDeck, 1, PlayArea.Of(1));
        var cards = Enumerable.Range(32, 5).Select(number => world.CreateCard($"090{number}", aside)).ToArray();
        var deck = InvocationDeck.Initialize(world, 1, cards, "prepared native fixture", []);
        World.MoveToTop(cards.Single(card => card.FaceId == "09034"), deck.Deck);
        world.CreateCard("09005", world.Seats[1].Hand);
        var game = Game.Begin(world, facts, abilities);
        while (game.Phase == GamePhase.Mulligan)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ChangeForm).Id));
        while (game.Active != 1) game.Resolve(Decision.Decline);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ChangeForm).Id));
        Assert.Equal(PublicDecisionKind.PlayerAction, game.Pending!.PublicKind);
        Assert.Equal(1, game.Pending.Player);
        Assert.Equal("09034", deck.PublicTop!.FaceId);
        Assert.Equal(0, world.Seats[0].IdentityCard.ObjectId);
        Assert.Equal(21, world.Seats[1].IdentityCard.ObjectId);
        Assert.Equal("01088", world.Cards[41].FaceId);
        Assert.Same(world.Seats[1].Hand, world.Cards[41].Area);
        return new(game, []);
    }
}
