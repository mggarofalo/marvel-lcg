using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Theory]
    [InlineData("addToHand")]
    [InlineData("returnToHand")]
    [InlineData("returnOwnedToHand")]
    [InlineData("discard")]
    [InlineData("removeFromGame")]
    public void MovementKeepsCompiledTargetAndDistinctOwnerDestination(string operation)
    {
        var (runner, fields) = MutableEffectRunner(operation,
            """{"titled":"Avengers Mansion"}""", false);
        Card? source = null;
        Card? target = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            target = board.CreateCard("01091", board.Seats[1].Hand);
            World.MoveToTop(target, board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var from = target!.Area;
        Assert.Equal(1, target.Owner);
        Assert.Equal(PlayArea.Of(0), from.PlayArea);
        var destination = operation switch
        {
            "removeFromGame" => world.AreaOf(DeckType.RemovedArea),
            "discard" => world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1),
            "addToHand" => world.Seats[0].Hand,
            _ => world.Seats[1].Hand,
        };
        int position = destination.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        // Engine choice: compilation fixes the selector; ownership, control
        // and the resolving player remain distinct inputs to movement.
        fields["titled"] = new AbilityValue.Word("Aunt May");
        var result = game.Resolve(Decision.Take(action.Id));

        Assert.Same(destination, target.Area);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
        Assert.False(source.Ready);
        var moved = Assert.Single(result.Events.OfType<CardsMoved>(), change =>
            change.Cards.Any(card => card.Card == target.ObjectId));
        Assert.Equal(Places.Reference(from), moved.From);
        Assert.Equal(Places.Reference(destination), moved.To);
        Assert.Equal(new Landing(target.ObjectId, position), Assert.Single(moved.Cards));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Theory]
    [InlineData("discardAtRandom")]
    [InlineData("placeAtRandom")]
    public void RandomMovementKeepsCompiledCountAndPlayerOrder(string operation)
    {
        string host = operation == "placeAtRandom" ? ",\"on\":\"this\"" : string.Empty;
        var (runner, fields) = MutableEffectRunner(operation,
            $$"""{"player":"each","count":2{{host}}}""", false);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        foreach (var seat in world.Seats)
        {
            while (seat.Hand.Cards.Count > 2) World.MoveToTop(seat.Hand.Cards[0], seat.Deck);
            Assert.Equal(2, seat.Hand.Cards.Count);
        }
        world.FirstPlayer = 1;
        var originals = world.Seats.SelectMany(seat => seat.Hand.Cards).ToList();
        long before = world.Random.Generator.WordsConsumed;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        fields["count"] = new AbilityValue.Number(1);

        var result = game.Resolve(Decision.Take(action.Id));

        // Two selections per player, from pools of two then one: each bound
        // consumes exactly one word, including the final one-card pool.
        Assert.Equal(before + 4, world.Random.Generator.WordsConsumed);
        Assert.All(world.Seats, seat => Assert.Empty(seat.Hand.Cards));
        var moved = result.Events.OfType<CardsMoved>()
            .SelectMany(change => change.Cards).Select(card => world.Cards[card.Card]).ToList();
        Assert.Equal([1, 1, 0, 0], moved.Select(card => card.Owner));
        Assert.Equal(originals.OrderBy(card => card.ObjectId), moved.OrderBy(card => card.ObjectId));
        Assert.All(moved, card =>
        {
            Assert.Equal(operation == "placeAtRandom" ? DeckType.UpgradesArea : DeckType.DiscardPile, card.Area.Type);
            Assert.Equal(operation != "placeAtRandom", card.FaceUp);
            if (operation == "placeAtRandom") Assert.Equal(source!.ObjectId, card.Area.Host);
        });
    }

    [Fact]
    public void EncounterCardsUseCompiledCountAndRoundRobinDealOrder()
    {
        var (runner, fields) = MutableEffectRunner("dealEncounterCards", """{"count":2}""", false);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        world.FirstPlayer = 1;
        var top = world.AreaOf(DeckType.EncounterDeck).Cards.Reverse().Take(4).ToList();
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        fields["count"] = new AbilityValue.Number(1);

        var result = game.Resolve(Decision.Take(action.Id));

        Assert.Equal([top[0], top[2]], world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Equal([top[1], top[3]], world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.All(top, card => Assert.False(card.FaceUp));
        var moved = result.Events.OfType<CardsMoved>().SelectMany(change => change.Cards);
        Assert.Equal(top.Select(card => card.ObjectId), moved.Select(card => card.Card));
    }

    [Theory]
    [InlineData("yourDeck")]
    [InlineData("encounterDeck")]
    public void TopDiscardKeepsCompiledCountAndPileOrder(string from)
    {
        var (runner, fields) = MutableEffectRunner("discardTop", $$"""{"from":"{{from}}","count":2}""", false);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var deck = from == "yourDeck" ? world.Seats[0].Deck : world.AreaOf(DeckType.EncounterDeck);
        var discard = from == "yourDeck"
            ? world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0)
            : world.AreaOf(DeckType.EncounterDiscardPile);
        var top = deck.Cards.Reverse().Take(2).ToList();
        int remaining = deck.Cards.Count - 2;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        fields["count"] = new AbilityValue.Number(1);

        var result = game.Resolve(Decision.Take(action.Id));

        Assert.Equal(remaining, deck.Cards.Count);
        Assert.Equal(top, discard.Cards.TakeLast(2));
        var moved = result.Events.OfType<CardsMoved>().SelectMany(change => change.Cards);
        Assert.Equal(top.Select(card => card.ObjectId), moved.Select(card => card.Card));
        Assert.All(top, card => Assert.True(card.FaceUp));
    }

    [Fact]
    public void HandResourceDiscardKeepsEarlierDiscardEvidenceForTheSequence()
    {
        // `result.discarded` spans the resolution. The hand operation has no
        // matching cards here, but must not erase the card its preceding
        // discardTop operation recorded.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "discardTop": { "from": "yourDeck", "count": 1 } },
              { "discardHandWithResource": "Y" },
              { "if": { "test": { "atLeast": {
                "value": { "result": "discarded" }, "count": 1
              } }, "then": { "placeCounters": {
                "card": "this", "counter": "test", "count": 1
              } } } }
            ] }
            """);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.ToList())
            World.MoveToTop(card, world.Seats[0].Deck);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(0, source!.Tokens.GetValueOrDefault("c_test"));

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(1, source!.Tokens.GetValueOrDefault("c_test"));
    }
}
