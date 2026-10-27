using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Production printed 09001b/09002; no public pack admission.</summary>
internal static class InvocationTopDiscardFixture
{
    internal static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    internal static AbilityRunner Runner() => Marvel.Content.Tests.Cards.AuthoredCards.Runner();

    internal static (World World, Game Game, InvocationDeck Deck, Card Wong, AbilityRunner Runner) Board(
        int controller = 1, bool last = false, bool damaged = false, bool start = true)
    {
        var runner = Runner();
        var world = new World(Cards, 2, 5489);
        for (int index = 0; index < 2; index++)
        {
            var seat = world.CreateSeat($"p{index}");
            seat.IdentityCard = world.CreateCard(index == 1 ? "09001b,09001a" : "01001b,01001a", seat.Hero);
            for (int i = 0; i < 20; i++) world.CreateCard("01088", seat.Deck);
            if (damaged) seat.IdentityCard.TakeDamage(2);
        }
        world.CreateCard("01134", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01137b", world.AreaOf(DeckType.MainSchemesArea));
        var aside = world.CreateArea(DeckType.AsideDeck, 1, PlayArea.Of(1));
        var cards = Enumerable.Range(32, 5).Select(id => world.CreateCard($"090{id}", aside)).ToArray();
        var deck = InvocationDeck.Initialize(world, 1, cards, "setup", []);
        if (last)
            foreach (var card in deck.Deck.Cards.SkipLast(1).ToArray()) World.MoveToTop(card, deck.Discard);
        // A foreign controller keeps Wong's printed ownership, as with Make the Call.
        var wong = world.CreateCard("09002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        World.MoveToTop(wong, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(controller), cardOwner: controller));
        var game = Game.Begin(world, Cards, runner);
        if (!start) return (world, game, deck, wong, runner);
        while (game.Phase == GamePhase.Mulligan)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        while (game.Active != controller) game.Resolve(Decision.Decline);
        return (world, game, deck, wong, runner);
    }

    internal static Affordance Action(Game game, Card card) => Assert.Single(game.Pending!.Affordances,
        offer => offer.AnchorId == card.ObjectId && offer.Verb == Game.ActionVerb);
}
