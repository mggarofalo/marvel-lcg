using System.Text.Json.Nodes;
using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual authored Call for Aid with explicitly synthetic composition probes; no pack admission.</summary>
public sealed class CallForAidMatchingTests
{
    private const string Discard = """
        {"discardUntil":{"from":"yourDeck","kind":"ally","trait":"AVENGER","then":"addToHand"}}
        """;

    private static AbilityRunner Runner(bool choice = false, bool kindOnly = false)
    {
        if (!choice && !kindOnly) return AuthoredCards.Runner();
        // Synthetic variants replace only this row inside the complete authored book.
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var cards = book["cards"]!.AsArray();
        int index = cards.Select((row, position) => (row, position))
            .Single(item => item.row!["card"]!.GetValue<string>() == "12015").position;
        string effect = choice
            ? "{\"seq\":[" + Discard + ", {\"choose\":{\"options\":["
                + "{\"draw\":{\"player\":\"you\",\"count\":1}},"
                + "{\"draw\":{\"player\":\"you\",\"count\":2}}]}}]}"
            : Discard.Replace(",\"trait\":\"AVENGER\"", "", StringComparison.Ordinal);
        cards[index]!["abilities"]![0]!["effect"] = JsonNode.Parse(effect);
        return new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:action.1")]
    public void ActualZeroCostHeroActionBelongsToItsOwnerAndLeavesNoDuration(bool hero)
    {
        // "Players can only trigger action abilities on cards they control".
        // Printed 12015 additionally requires hero form.
        var (game, world) = Prepared(hero: hero);
        var source = world.Cards.Single(card => card.FaceId == "12015");
        var runner = (AbilityRunner)world.Abilities;
        Assert.Equal("0", world.Facts.Attributes(source.FaceId)["Cost"]);
        Assert.DoesNotContain(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        if (!hero)
        {
            Assert.DoesNotContain(runner.Actions(world, 1), offer => offer.Card == source.ObjectId);
            return;
        }
        var hand = world.Seats[1].Hand;
        int handCount = hand.Cards.Count;
        long handSize = PhaseEnd.HandSize(world, world.Seats[1], world.Facts);
        game.Resolve(Decision.Take(Action(game, world)));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(1, source.Owner);
        Assert.Equal(handSize, PhaseEnd.HandSize(world, world.Seats[1], world.Facts));
        Assert.Equal(handCount, hand.Cards.Count);
        Assert.Single(hand.Cards, card => card.FaceId == "01011");
    }

    [Fact]
    public void SyntheticKindOnlyMatcherAcceptsANonAvengerAllyAndStopsBeforeTheNextAlly()
    {
        // Synthetic kind-only composition exercises the optional trait vocabulary.
        var (game, world) = Prepared(kindOnly: true);
        var seat = world.Seats[1];
        var expectedDeck = seat.Deck.Cards.Take(2).ToArray();
        var match = seat.Deck.Cards[^2];
        Assert.Equal("01002", match.FaceId);
        Assert.DoesNotContain("AVENGER", world.Facts.Traits(match.FaceId));
        var resolution = game.Resolve(Decision.Take(Action(game, world)));
        Assert.Same(seat.Hand, match.Area);
        Assert.Equal(expectedDeck, seat.Deck.Cards);
        Assert.DoesNotContain(resolution.Events.OfType<AreaReordered>(), item => item.Verb == "Shuffle");
        var batch = Assert.Single(resolution.Events.OfType<ActivityCompleted>(), item => item.Verb == "Discard");
        Assert.Equal(2, batch.Participants[0].Count);
    }

    [Fact]
    [Rule("rr:discard.4")]
    [Rule("rr:discard.4.1")]
    [Rule("rr:discard.4.2")]
    public void ResolvingSeatDiscardsBothNearMissesAndRecoversOnlyTheExactAvengerAlly()
    {
        // "These discards are considered simultaneous"; responses resolve
        // "after all discards are made". The batch closes before the next instruction.
        var (game, world) = Prepared();
        var seat = world.Seats[1];
        var below = seat.Deck.Cards[0];
        var match = seat.Deck.Cards[1];
        var expected = seat.Deck.Cards.Skip(1).Reverse().Select(card => card.ObjectId).ToArray();
        var duplicate = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1).Cards[0];
        int[] otherDeck = world.Seats[0].Deck.Cards.Select(card => card.ObjectId).ToArray();
        var resolution = game.Resolve(Decision.Take(Action(game, world)));
        var discards = resolution.Events.OfType<CardsMoved>()
            .Where(move => move.From.Zone == "PlayerDeck" && move.To.Zone == "DiscardPile").ToArray();
        Assert.Equal(expected, discards.SelectMany(move => move.Cards).Select(card => card.Card));
        Assert.Same(seat.Hand, match.Area);
        Assert.Equal(1, match.Owner);
        Assert.Equal(DeckType.DiscardPile, duplicate.Area.Type);
        Assert.Equal([below], seat.Deck.Cards);
        Assert.Equal(otherDeck, world.Seats[0].Deck.Cards.Select(card => card.ObjectId));
        var batch = Assert.Single(resolution.Events.OfType<ActivityCompleted>(),
            activity => activity.Verb == "Discard" && activity.Participants[0].Count == 3);
        Assert.Equal(expected, batch.Participants[0].Cards);
        Assert.All(discards, move => Assert.Equal(batch.Batch, move.ActivityBatch));
        var ordered = resolution.Events.ToList();
        Assert.True(ordered.IndexOf(batch) < ordered.FindIndex(item => item.Verb == "Add_To_Hand"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:player-deck.1")]
    [Rule("rr:player-deck.3")]
    public void ExhaustionShufflesOnceAndNeverSearchesTheReplacementDeck(bool matchLast)
    {
        // "no further cards are discarded from the newly shuffled deck";
        // "immediately deals themself one facedown encounter card".
        var (game, world) = Prepared(exhaust: true, matchLast: matchLast);
        var (_, control) = Prepared(exhaust: true, matchLast: matchLast);
        var expected = world.Seats[1].Deck.Cards.Reverse().ToArray();
        int dealt = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count;
        var controlDiscarded = PlayerDeck.DiscardTop(control, 1, long.MaxValue, "control", []);
        if (matchLast) World.MoveToTop(controlDiscarded[^1], control.Seats[1].Hand);
        var resolution = game.Resolve(Decision.Take(Action(game, world)));
        Assert.Equal(expected.Select(card => card.ObjectId), resolution.Events.OfType<CardsMoved>()
            .Where(move => move.From.Zone == "PlayerDeck" && move.To.Zone == "DiscardPile")
            .SelectMany(move => move.Cards).Select(card => card.Card));
        Assert.Single(resolution.Events.OfType<AreaReordered>(), item => item.Verb == "Shuffle");
        Assert.Equal(dealt + 1, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count);
        Assert.Equal(control.Seats[1].Deck.Cards.Select(card => card.ObjectId),
            world.Seats[1].Deck.Cards.Select(card => card.ObjectId));
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal(matchLast ? DeckType.HandsArea : DeckType.PlayerDeck, expected[^1].Area.Type);
        Assert.Equal(DeckType.PlayerDeck, expected[0].Area.Type);
    }

    [Fact]
    public void SyntheticFollowingChoiceResumesWithoutRepeatingTheDiscardOrChangingItsOwner()
    {
        // The extra choice is a synthetic composition, not printed Call for Aid text.
        var (game, world) = Prepared(choice: true);
        var (controlGame, control) = Prepared(choice: true);
        int action = Action(game, world);
        int controlAction = Action(controlGame, control);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
        var random = world.Random.Generator.GetState();
        var pending = game.Pending;
        var agenda = world.Agenda.Outstanding.ToArray();
        for (int index = 0; index < 3; index++)
            Assert.Single(Runner(true).Actions(world, 1), offer => offer.Card ==
                world.Cards.Single(card => card.FaceId == "12015").ObjectId);
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Same(pending, game.Pending);
        Assert.Equal(agenda, world.Agenda.Outstanding);
        var first = game.Resolve(Decision.Take(action));
        var controlFirst = controlGame.Resolve(Decision.Take(controlAction));
        Assert.Equal(1, game.Pending!.Player);
        Assert.Equal(2, game.Pending.Affordances.Count);
        var suspendedDigest = world.Digest().Canonical();
        var suspendedRandom = world.Random.Generator.GetState();
        long suspendedWords = world.Random.Generator.WordsConsumed;
        var suspendedPrompt = game.Pending;
        var suspendedAgenda = world.Agenda.Outstanding.ToArray();
        for (int query = 0; query < 3; query++)
        {
            var preview = ((AbilityRunner)world.Abilities).Choosing(world, world.Agenda.Current!.Value);
            Assert.Equal(suspendedPrompt.Affordances, preview!.Affordances);
            Assert.Equal(1, preview.Player);
        }
        Assert.Equal(suspendedDigest, world.Digest().Canonical());
        Assert.Equal(suspendedRandom.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(suspendedRandom.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(suspendedWords, world.Random.Generator.WordsConsumed);
        Assert.Same(suspendedPrompt, game.Pending);
        Assert.Equal(suspendedAgenda, world.Agenda.Outstanding);
        Assert.Equal(DeckType.HandsArea, world.Cards.Single(card => card.FaceId == "01011"
            && card.Owner == 1 && card.Area.Type == DeckType.HandsArea).Area.Type);
        int handBefore = world.Seats[1].Hand.Cards.Count;
        var second = game.Resolve(Decision.Take(game.Pending.Affordances[1].Id));
        var controlSecond = controlGame.Resolve(Decision.Take(controlGame.Pending!.Affordances[1].Id));
        Assert.DoesNotContain(second.Events.OfType<CardsMoved>(), move => move.From.Zone == "PlayerDeck" && move.To.Zone == "DiscardPile");
        Assert.Equal(handBefore + 2, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(controlGame.Pending!.Asking, game.Pending!.Asking);
        Assert.Equal(controlGame.Pending.Player, game.Pending.Player);
        Assert.Equal(controlGame.Pending.Affordances.Select(offer => System.Text.Json.JsonSerializer.Serialize(offer)),
            game.Pending.Affordances.Select(offer => System.Text.Json.JsonSerializer.Serialize(offer)));
        Assert.Equal(control.Agenda.Outstanding, world.Agenda.Outstanding);
        Assert.Equal(controlFirst.Events.Select(EventJson.Write), first.Events.Select(EventJson.Write));
        Assert.Equal(controlSecond.Events.Select(EventJson.Write), second.Events.Select(EventJson.Write));
    }

    [Theory]
    [InlineData("\"set\":\"ms_marvel\",\"kind\":\"ally\"")]
    [InlineData("\"set\":\"ms_marvel\",\"trait\":\"AVENGER\"")]
    [InlineData("\"trait\":\"AVENGER\"")]
    public void AmbiguousOrUnboundedPlayerMatchersAreRejected(string fields)
    {
        string json = "{\"discardUntil\":{\"from\":\"yourDeck\",\"then\":\"addToHand\"," + fields + "}}";
        string program = "{\"cards\":[{\"card\":\"12015\",\"abilities\":[{"
            + "\"trigger\":{\"event\":\"WhenActionTriggered\",\"timing\":\"Action\",\"subject\":\"this\"},"
            + "\"effect\":" + json + "}]}]}";
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(program)));
    }

    private static int Action(Game game, World world) => Assert.Single(game.Pending!.Affordances,
        offer => offer.AnchorId == world.Cards.Single(card => card.FaceId == "12015").ObjectId).Id;

    private static (Game Game, World World) Prepared(
        bool exhaust = false, bool matchLast = false, bool choice = false, bool kindOnly = false, bool hero = true) =>
        Playing(board =>
        {
            var seat = board.Seats[1];
            seat.IdentityCard.TurnTo(hero ? "01010a" : "01010b");
            var removed = board.AreaOf(DeckType.RemovedArea, PlayArea.Of(1));
            foreach (var card in seat.Deck.Cards.ToArray()) World.MoveToTop(card, removed);
            // A matching copy in discard distinguishes a fresh search after the shuffle.
            board.CreateCard("01011", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
            if (!exhaust) board.CreateCard("01088", seat.Deck);
            if (!exhaust || matchLast) board.CreateCard("01011", seat.Deck);
            board.CreateCard("01002", seat.Deck); // Ally, without Avenger.
            board.CreateCard("01091", seat.Deck); // Avenger, without ally.
            board.CreateCard("12015", seat.Hand);
        }, heroes: ["spider_man", "captain_marvel"], abilities: Runner(choice, kindOnly));
}
