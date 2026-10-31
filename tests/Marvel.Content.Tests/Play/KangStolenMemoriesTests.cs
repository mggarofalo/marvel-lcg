using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.KangObligationFixture;

namespace Marvel.Content.Tests.Play;

// Stolen Memories (11019) and the full authored book on a nonpublic Rhino
// board; Kang's public admission stays closed.
public sealed class KangStolenMemoriesTests : CoreCardTestBase
{
    private const string StolenMemories = "11019";
    private const string Genius = "01089";
    private const string Energy = "01088";

    [Fact]
    [Rule("rr:obligation.5")]
    [Rule("rr:obligation.1")]
    public void RevealStoresTheTopEightCardsOfTheRevealingPlayersDeckFacedown()
    {
        // 11019: "When Revealed: Place the top 8 cards of your deck facedown under
        // this card." rr:obligation.5: the revealing player "places that obligation
        // into their play area"; rr:obligation.1 makes "your deck" that player's.
        // Player 1, not the first player, reveals; player 0's deck is untouched.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var top = world.Seats[1].Deck.Cards.TakeLast(8).ToList();

        var memories = f.Reveal(StolenMemories, player: 1);
        f.Finish();

        Assert.Equal(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)), memories.Area);
        Assert.Equal(top.Select(card => card.ObjectId).Order(), Beneath(world, memories).Select(card => card.ObjectId).Order());
        Assert.All(Beneath(world, memories), card => Assert.False(card.FaceUp));
        Assert.Equal(4, world.Seats[1].Deck.Cards.Count);
        Assert.Equal(12, world.Seats[0].Deck.Cards.Count);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:player-deck.1")]
    public void AShortDeckReshufflesDealsAnEncounterCardAndKeepsPlacing()
    {
        // Ruling a31d988626f0879e: with fewer than 8 cards "you would place all you
        // can, reshuffle, get an encounter card, and then place the rest from the new
        // deck ... you would continue placing cards from the newly shuffled deck."
        // rr:player-deck.1: the reset deals "one facedown encounter card".
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var deck = world.Seats[1].Deck;
        foreach (var card in deck.Cards.Take(7).ToList())
            World.MoveToTop(card, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        Assert.Equal(5, deck.Cards.Count);

        var memories = f.Reveal(StolenMemories, player: 1);
        f.Finish();

        Assert.Equal(8, Beneath(world, memories).Count);
        Assert.Equal(4, deck.Cards.Count);
        Assert.Empty(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1).Cards);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:player-deck.1")]
    public void PlacingTheLastCardResetsTheDeckAtOnce()
    {
        // rr:player-deck.1: "If a player deck empties, the player shuffles their
        // discard pile to make a new deck. That player immediately deals themself
        // one facedown encounter card." Exactly 8 cards remain; placing the eighth
        // empties the deck, which resets before the ability ends.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var deck = world.Seats[0].Deck;
        foreach (var card in deck.Cards.Take(4).ToList())
            World.MoveToTop(card, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));

        var memories = f.Reveal(StolenMemories);
        f.Finish();

        Assert.Equal(8, Beneath(world, memories).Count);
        Assert.Equal(4, deck.Cards.Count);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Fact]
    [Rule("rr:player-deck.4")]
    public void AnEmptyDiscardPileLeavesNoNewDeckToPlaceFrom()
    {
        // rr:player-deck.4: "If a player deck empties and the player has no cards in
        // their discard pile, the deck does not reset until there is at least one
        // card in the player's discard pile, then the player deals themself one
        // facedown encounter card." Three cards are placed. The Genius paid as the
        // action's cost is the first card to reach the discard pile, so it alone
        // becomes the new deck; the stored cards follow the obligation afterwards.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var deck = world.Seats[0].Deck;
        foreach (var card in deck.Cards.Take(9).ToList())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        AlterEgo(world, 0);
        var genius = InHand(world, Genius, 0);

        var memories = f.Reveal(StolenMemories);
        f.Finish();

        Assert.Equal(3, Beneath(world, memories).Count);
        Assert.Empty(deck.Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);

        var stored = Beneath(world, memories).ToList();
        Act(f, memories, 0, genius);

        Assert.Equal([genius], deck.Cards);
        Assert.Equal(stored.Select(card => card.ObjectId).Order(), world.AreaOf(
            DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards.Select(card => card.ObjectId).Order());
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:obligation.6")]
    [Rule("rr:leaves-play")]
    public void TheAlterEgoActionDiscardsAPrintedMentalResourceTheObligationAndItsStoredCards(int player)
    {
        // 11019: "Alter-Ego Action: Discard a [mental] resource from your hand ->
        // discard this obligation. (Discard each facedown card under this
        // obligation.)" Only the energy card is left in hand.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var memories = f.Reveal(StolenMemories, player);
        f.Finish();
        var stored = Beneath(world, memories).ToList();
        AlterEgo(world, player);
        var energy = InHand(world, Energy, player);
        var genius = InHand(world, Genius, player);

        Act(f, memories, player, genius);

        var pile = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player);
        Assert.Equal(DeckType.EncounterDiscardPile, memories.Area.Type);
        Assert.Equal(stored.Append(genius).Select(card => card.ObjectId).Order(),
            pile.Cards.Select(card => card.ObjectId).Order());
        Assert.Equal([energy], world.Seats[player].Hand.Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("01088", true, false)]
    [InlineData("01062", true, false)]
    [InlineData("01089", false, false)]
    [InlineData("01089", true, true)]
    [Rule("rr:obligation.6")]
    [Rule("rr:action.1.1")]
    [Rule("rr:wild-resource")]
    public void TheActionNeedsAlterEgoAPrintedMentalResourceAndTheObligationsOwner(
        string inHand, bool alterEgo, bool otherPlayer)
    {
        // Ruling 437680ffd6d92fb8: "For a wild resource to be treated a different type
        // of resource, it must be spent. Stolen Memories does not specify that the
        // discarded mental resource is spent, so a wild resource cannot be used",
        // so The Power of Justice (wild) does not pay. A hero cannot use an Alter-Ego
        // Action, and rr:action.1.1: "Players cannot trigger action abilities on
        // obligations in other players' play areas."
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var memories = Obligation(world, StolenMemories, 1);
        int actor = otherPlayer ? 0 : 1;
        if (alterEgo) AlterEgo(world, actor);
        InHand(world, inHand, actor);

        Assert.False(Offers(f, memories, actor));
    }
}
