using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Authored Depowered on a nonpublic research board: a requested Action event is
// played by its owner, so only the owner's prohibition applies.
public sealed class KangDepoweredRequestedActionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [Rule("rr:player-turn.6")]
    [Rule("rr:cannot")]
    public void TheOwnersProhibitionDecidesARequestedHeroSpecificEvent(int depowered, bool offered)
    {
        // rr:player-turn.6: the active player may "Ask another player to trigger any
        // 'Action' ability that player could trigger on their own turn. The other player
        // then decides whether or not to trigger the ability." 11020: "You cannot play
        // hero-specific cards." Player 1 plays Swinging Web Kick during player 0's turn,
        // so Depowered beside player 0 does not stop it and Depowered beside player 1 does.
        var runner = AuthoredCards.Runner();
        var world = Board(runner);
        world.CreateCard("11020", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(depowered)));
        var kick = world.Seats[1].Hand.Cards.Single(card => card.FaceId == "01005");
        var payment = world.Seats[1].Hand.Cards.Where(card => card.FaceId == "01088").Select(card => card.ObjectId).ToArray();
        var villain = Assert.Single(world.AreaOf(DeckType.VillainArea).Cards);
        var game = Game.Begin(world, Cards, runner);
        for (int i = 0; i < 2; i++)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ResolveMulligans).Id));
        Assert.Equal(0, game.Pending!.Player);

        var offer = game.Pending.Affordances.SingleOrDefault(a => a.AnchorId == kick.ObjectId);
        Assert.Equal(offered, offer is not null);
        if (offer is null) return;
        Assert.Equal(1, DecisionAssessment.ActingSeat(game.Pending, offer));
        var allocation = ResourcePayment.Allocate(Assert.Single(offer.CostOptions), payment);
        Assert.NotNull(allocation);
        game.Resolve(Decision.Take(offer.Id, [villain.ObjectId], payment, new Dictionary<string, long>(), allocation));
        Assert.Equal(DeckType.DiscardPile, kick.Area.Type);
        Assert.Equal(8, villain.Damage);
    }

    private static World Board(Marvel.Cards.Run.AbilityRunner runner)
    {
        var world = new World(Cards, 2, 241);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
            for (int i = 0; i < 20; i++) world.CreateCard("01087", seat.Deck);
        }
        for (int i = 0; i < 20; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        world.CreateCard("01005", world.Seats[1].Hand);
        for (int i = 0; i < 3; i++) world.CreateCard("01088", world.Seats[1].Hand);
        world.Abilities = runner;
        return world;
    }
}
