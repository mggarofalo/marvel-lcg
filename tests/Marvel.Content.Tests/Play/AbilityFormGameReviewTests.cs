using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AbilityFormGameReviewTests
{
    private static readonly string[] FirstEvents = ["CardsMoved", "CardsMoved", "CardsMoved", "CardsMoved", "ActivityCompleted", "CardsFlipped", "CardsMoved", "ActivityCompleted", "FieldSet", "AbilityWindowResult"];
    private static readonly string[] AcceptedEvents = ["FieldSet", "CardsMoved", "CardsMoved", "ActivityCompleted", "CardsMoved"];
    private static readonly string[] DeclinedEvents = ["CardsMoved", "CardsMoved", "ActivityCompleted", "CardsMoved"];
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.2")]
    [Rule("rr:then.2")]
    public void ActualSplitPersonalityContinuesThroughGameResponse(bool accept)
    {
        // 01025 changes form, then draws; 04093 responds before 01019a's optional response.
        // rr:initiating-abilities.2: responses initiate "immediately after" their condition.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var order = Dealer.DealOrder(setup, "rhino", ["she_hulk"]);
        var world = WorldSetup.DealWithoutCardAbilities(cards, Blueprints.From(order, cards), ["She-Hulk"], 241);
        var seat = world.Seats[0];
        foreach (var card in seat.Hand.Cards.ToArray()) World.MoveToTop(card, seat.Deck);
        World.MoveToTop(world.TheCardIn(DeckType.VillainArea)!, world.AreaOf(DeckType.RemovedArea));
        var villain = world.CreateCard("04093", world.AreaOf(DeckType.VillainArea));
        var boost = world.CreateCard("04064", world.AreaOf(DeckType.EncounterDeck));
        var split = world.CreateCard("01025", seat.Hand);
        var payments = Enumerable.Range(0, 3).Select(_ => world.CreateCard("01087", seat.Hand)).ToArray();
        var game = Game.Begin(world, cards, AuthoredCards.Runner());
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ResolveMulligans).Id));
        Assert.Equal("01019b", seat.IdentityCard.FaceId);
        int deckCount = seat.Deck.Cards.Count;
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var play = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == split.ObjectId && a.Verb == Game.ActionVerb);
        var first = game.Resolve(PaidDecision.Take(play, payments.Select(c => c.ObjectId).ToArray()));
        Assert.All(payments, c => Assert.Equal(DeckType.DiscardPile, c.Area.Type));
        Assert.Equal("01019a", seat.IdentityCard.FaceId);
        Assert.Equal(3, seat.IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, boost.Area.Type);
        Assert.Equal(deckCount, seat.Deck.Cards.Count);
        Assert.Single(first.Events.OfType<CardsFlipped>());
        Assert.DoesNotContain(first.Events.OfType<CardsMoved>(), e => e.From.Zone == nameof(DeckType.PlayerDeck) && e.To.Zone == nameof(DeckType.HandsArea));
        var response = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == seat.IdentityCard.ObjectId);
        var second = game.Resolve(accept ? Decision.Take(response.Id, [villain.ObjectId], []) : Decision.Decline);
        Assert.Equal(accept ? 2 : 0, villain.Damage);
        Assert.Equal(4, seat.Hand.Cards.Count);
        Assert.Equal(deckCount - 2, seat.Deck.Cards.Count);
        Assert.Equal(FirstEvents,
            first.Events.Select(e => e.GetType().Name));
        var flip = Assert.IsType<CardsFlipped>(first.Events[5]);
        Assert.Equal([seat.IdentityCard.ObjectId], flip.Cards);
        Assert.Equal("WhenActionTriggered", flip.Trigger);
        var discardedBoost = Assert.IsType<CardsMoved>(first.Events[6]);
        Assert.Equal(boost.ObjectId, Assert.Single(discardedBoost.Cards).Card);
        var damage = Assert.IsType<FieldSet>(first.Events[8]);
        Assert.Equal(seat.IdentityCard.ObjectId, damage.Card);
        Assert.Equal((15, 12), (damage.From, damage.To));
        Assert.Equal(villain.ObjectId, discardedBoost.AbilitySource);
        Assert.Equal(villain.ObjectId, damage.AbilitySource);
        Assert.NotNull(damage.AbilityOccurrence);
        Assert.Equal(discardedBoost.AbilityOccurrence, damage.AbilityOccurrence);
        Assert.Equal("WhenFormChanged", damage.Trigger);
        Assert.Equal(accept ? AcceptedEvents : DeclinedEvents,
            second.Events.Select(e => e.GetType().Name));
        int drawAt = accept ? 1 : 0;
        if (accept)
        {
            var responseDamage = Assert.IsType<FieldSet>(second.Events[0]);
            Assert.Equal(villain.ObjectId, responseDamage.Card);
            Assert.Equal((13, 11), (responseDamage.From, responseDamage.To));
            Assert.Equal("WhenFormChanged", responseDamage.Trigger);
        }
        foreach (var item in second.Events.Skip(drawAt).Take(2))
        {
            var draw = Assert.IsType<CardsMoved>(item);
            Assert.Equal(nameof(DeckType.PlayerDeck), draw.From.Zone);
            Assert.Equal(nameof(DeckType.HandsArea), draw.To.Zone);
            Assert.Equal("WhenActionTriggered", draw.Trigger);
            Assert.Null(draw.AbilitySource);
        }
        var cleanup = Assert.IsType<CardsMoved>(second.Events[^1]);
        Assert.Equal(split.ObjectId, Assert.Single(cleanup.Cards).Card);
        Assert.Equal(nameof(DeckType.DiscardPile), cleanup.To.Zone);
        Assert.Equal(DeckType.DiscardPile, split.Area.Type);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.True(game.IsRootPrompt);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
    }
}
