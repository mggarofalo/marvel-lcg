using Marvel.Content;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class SemanticActivityBatchTests
{
    [Rule("rr:draw-drawing-cards")]
    [Fact]
    public void DrawThreeIsOneActivityButThreeInstructionsRemainThree()
    {
        // "Those cards are drawn from the top of their deck one at a time."
        World one = Board(1, 8);
        World separate = Board(1, 8);
        var events = new List<GameEvent>();
        var adjacent = new List<GameEvent>();
        Draw.Cards(one, 0, 3, "same cause", events);
        for (int index = 0; index < 3; index++) Draw.Cards(separate, 0, 1, "same cause", adjacent);
        Assert.Equal(3, events.OfType<CardsMoved>().Count());
        Assert.Single(events.OfType<ActivityCompleted>());
        Assert.Single(Present(one, events).Cues);
        Assert.Equal(3, Present(separate, adjacent).History.Count);
        Assert.Equal(events.OfType<CardsMoved>().Select(move => move.Cards[0].Card),
            adjacent.OfType<CardsMoved>().Select(move => move.Cards[0].Card));
    }

    [Rule("rr:player-deck.2")]
    [Fact]
    public void AResetWithinDrawThreeKeepsItsPositionAndRandomConsumption()
    {
        // "The player continues to draw cards up to the specified number."
        World one = Board(1, 1);
        World separate = Board(1, 1);
        foreach (World world in new[] { one, separate })
            for (int index = 0; index < 4; index++)
                world.CreateCard("01002", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var events = new List<GameEvent>();
        var adjacent = new List<GameEvent>();
        Draw.Cards(one, 0, 3, "same cause", events);
        for (int index = 0; index < 3; index++) Draw.Cards(separate, 0, 1, "same cause", adjacent);
        Assert.Equal(one.Random.Generator.WordsConsumed, separate.Random.Generator.WordsConsumed);
        Assert.Equal(one.Digest().Canonical(), separate.Digest().Canonical());
        Assert.Equal(Raw(events), Raw(adjacent));
        EventBatchPresentation presented = Present(one, events);
        Assert.Single(presented.History, item => item.Activity?.Operation == "Draw");
        Assert.Contains(presented.History, item => item.Activity?.Operation == "Reset");
        Assert.Contains(events.OfType<CardsMoved>(), item => item.To.Zone == "DealtEncounterCardsDeck");
    }

    [Rule("rr:end-of-player-phase.step.2")]
    [Rule("rr:end-of-player-phase.step.3")]
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void PhaseDrawAndReadyAreEachOneSimultaneousTableActivity(int players)
    {
        // "Each player simultaneously draws up to their hand size" and "readies all of their cards."
        World world = Board(players, 8);
        foreach (Seat seat in world.Seats) seat.IdentityCard.Exhaust();
        var events = new List<GameEvent>();
        PhaseEnd.DrawToHandSize(world, world.Facts, events);
        PhaseEnd.ReadyCards(world, events);
        ActivityCompleted[] completed = events.OfType<ActivityCompleted>().ToArray();
        Assert.Equal(2, completed.Length);
        Assert.Equal(players, completed[0].Participants.Count);
        Assert.Equal(ActivityPhaseStep.DrawToHandSize, completed[0].PhaseStep);
        Assert.Equal(ActivityPhaseStep.ReadyCards, completed[1].PhaseStep);
        EventBatchPresentation presented = Present(world, events);
        Assert.Equal(2, presented.Cues.Count);
        Assert.All(presented.Cues, cue => Assert.Contains("simultaneously", cue.Summary));
        Assert.Equal(players, presented.Cues[1].Anchors.Count);
    }

    [Rule("rr:end-of-player-phase.step.1")]
    [Fact]
    public void SelectedHandDiscardsAreOneInstruction()
    {
        // "Each player may discard any number of cards from their hand."
        World world = Board(1, 8);
        Draw.Cards(world, 0, 3, "setup", []);
        int[] selected = world.Seats[0].Hand.Cards.Take(2).Select(card => card.ObjectId).ToArray();
        var events = new List<GameEvent>();
        PhaseEnd.DiscardToHandSize(world, world.Facts, 0, selected, events);
        Assert.Equal(2, events.OfType<CardsMoved>().Count());
        Assert.Equal(2, Assert.Single(events.OfType<ActivityCompleted>()).Participants[0].Count);
        Assert.Single(Present(world, events).Cues);
    }

    [Fact]
    public void RestrictedDrawRetainsPublicQuantityWithoutConcealedIdsOrNames()
    {
        World world = Board(2, 8);
        var events = new List<GameEvent>();
        Draw.Cards(world, 1, 3, "ability", events);
        ViewScope scope = new RestrictedVisibilityPolicy(0).Authorize(null, world.Players);
        VisibleResult visible = WorldProjection.For(world, null, events, scope);
        ActivityCompleted retained = Assert.Single(visible.Events.OfType<ActivityCompleted>());
        Assert.Empty(retained.Participants[0].Cards);
        Assert.Null(retained.Subjects);
        EventPresentation presentation = Assert.Single(EventPresenter.PresentNarrative(visible.Events, visible.World));
        Assert.Equal("Captain Marvel drew 3 cards.", presentation.Summary);
        Assert.Empty(presentation.Anchors);
        Assert.Equal(3, presentation.Activity!.Participants[0].Count);
        Assert.DoesNotContain("Spider-Tracer", EventJson.Write(retained));
    }

    [Rule("rr:cost.3")]
    [Fact]
    public void OnePaymentDiscardsItsCardsInOneActivityWhileSeparatePaymentsStaySeparate()
    {
        // Resources are spent "by discarding cards from their hand".
        World together = Board(1, 8);
        World separate = Board(1, 8);
        foreach (World world in new[] { together, separate }) Draw.Cards(world, 0, 3, "setup", []);
        int[] chosen = together.Seats[0].Hand.Cards.Select(card => card.ObjectId).ToArray();
        var events = new List<GameEvent>();
        var adjacent = new List<GameEvent>();
        CardPlay.Spend(together, together.Facts, [together.Seats[0].Hand], chosen, 3, "", -1, 0, events);
        foreach (int card in chosen)
            CardPlay.Spend(separate, separate.Facts, [separate.Seats[0].Hand], [card], 1, "", -1, 0, adjacent);
        Assert.Single(Present(together, events).History);
        Assert.Equal(3, Present(separate, adjacent).History.Count);
        Assert.Equal(Raw(events), Raw(adjacent));
        Assert.Equal(together.Digest().Canonical(), separate.Digest().Canonical());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OuterInstructionKeepsAnInnerReceiptAndCapturesOnlyItsOwnCards(bool outerFirst)
    {
        // Engine-choice nested boundaries retain ownership even with identical verbs and causes.
        World world = Board(1, 8);
        Draw.Cards(world, 0, 3, "setup", []);
        Card[] cards = world.Seats[0].Hand.Cards.ToArray();
        var events = new List<GameEvent>();
        if (outerFirst) Discard.Card(world, cards[0], "same cause", events);
        int innerStart = events.Count;
        Discard.Card(world, cards[1], "same cause", events);
        ActivityBatches.Complete(events, innerStart, "Discard", 0);
        if (!outerFirst) Discard.Card(world, cards[0], "same cause", events);
        Discard.Card(world, cards[2], "same cause", events);
        ActivityBatches.Complete(events, 0, "Discard", 0);
        ActivityCompleted[] receipts = events.OfType<ActivityCompleted>().ToArray();
        Assert.Equal(2, receipts.Length);
        Assert.Equal([cards[1].ObjectId], receipts[0].Participants[0].Cards);
        Assert.Equal([cards[0].ObjectId, cards[2].ObjectId], receipts[1].Participants[0].Cards);
        Assert.NotEqual(receipts[0].Batch, receipts[1].Batch);
        ActivityBatches.Complete(events, 0, "Discard", 0);
        Assert.Equal(2, events.OfType<ActivityCompleted>().Count());
        EventBatchPresentation presented = Present(world, events);
        Assert.Equal(2, presented.History.Count);
        Assert.Equal(2, presented.Cues.Count);
        Assert.Equal(1, presented.History[0].Activity!.Participants[0].Count);
        Assert.Equal(2, presented.History[1].Activity!.Participants[0].Count);
    }

    [Rule("rr:encounter-deck.2")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EncounterSearchHasOneDiscardActivityAndAnEncounterParticipant(bool emptiesDeck)
    {
        // "Until" stops when its condition is met or "the encounter deck is empty".
        World world = Board(1, 0);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        foreach (Card card in deck.Cards.ToArray()) World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        if (!emptiesDeck) world.CreateCard("01187", deck);
        Card found = world.CreateCard("01103", deck);
        Card above = world.CreateCard("01187", deck);
        var events = new List<GameEvent>();
        Assert.Same(found, EncounterDeck.DiscardUntil(world, world.Facts, CardKind.Minion, "search", events));
        ActivityCompleted completed = Assert.Single(events.OfType<ActivityCompleted>());
        Assert.Equal([above.ObjectId, found.ObjectId], completed.Participants[0].Cards);
        EventBatchPresentation presented = Present(world, events);
        EventPresentation discard = Assert.Single(presented.History, item => item.Activity?.Operation == "Discard");
        Assert.StartsWith("The encounter deck discarded ", discard.Summary);
        Assert.Equal("The encounter deck", discard.Activity!.Participants[0].Name);
        if (emptiesDeck)
        {
            CardsMoved reset = Assert.Single(events.OfType<CardsMoved>(), item => item.Verb == "Reset");
            Assert.Null(reset.ActivityBatch);
            Assert.True(events.IndexOf(reset) < events.IndexOf(completed));
            Assert.Equal(2, discard.Activity.Participants[0].Count);
            Assert.Empty(discard.Activity.Participants[0].Subjects);
        }
        else Assert.Contains("Shocker", discard.Summary);
        Assert.DoesNotContain(presented.History, item => item.Summary.Contains("Player -1"));
    }

    private static string[] Raw(IEnumerable<GameEvent> events) => events
        .Where(item => item is not ActivityCompleted)
        .Select(item => EventJson.Write(item with { ActivityBatch = null })).ToArray();

    private static EventBatchPresentation Present(World world, IReadOnlyList<GameEvent> events)
    {
        ViewScope scope = new PermissiveVisibilityPolicy().Authorize(null, world.Players);
        VisibleResult visible = WorldProjection.For(world, null, events, scope);
        return EventCuePlanner.Plan(visible.Events, visible.World, Outcome.Unfinished);
    }

    private static World Board(int players, int deck)
    {
        // Synthetic component board uses Core printed data to isolate instruction boundaries.
        CardCatalog facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, players, seed: 7);
        for (int player = 0; player < players; player++)
        {
            Seat seat = world.CreateSeat(player == 0 ? "Spider-Man" : "Captain Marvel");
            seat.IdentityCard = world.CreateCard(player == 0 ? "01001a" : "01010a", seat.Hero);
            for (int card = 0; card < deck; card++) world.CreateCard("01002", seat.Deck);
        }
        for (int card = 0; card < 8; card++) world.CreateCard("01187", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }
}
