using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class LifecycleRulesAHostedMaximumBlocksTheWholeControlTransferAtomiTests : LifecycleRulesTestBase
{
    [Rule("rr:max-maximum.3")]
    [Rule("rr:max-maximum.3.1")]
    [Fact]
    public void AHostedMaximumBlocksTheWholeControlTransferAtomically()
    {
        // A maximum applies to every card the destination player would
        // control, including an upgrade that follows its host. The root and
        // its hosted tree remain together when that destination is illegal.
        var facts = new Facts();
        facts.Maxima["limited"] = 1;
        var world = Board(facts, players: 2);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var moving = world.CreateCard("limited", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId, cardOwner: 0));
        world.CreateCard("limited", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        Assert.Throws<RulesNotImplementedException>(() => CardControlTransfer.TakeControl(world, facts, ally, 1));
        Assert.Equal(PlayArea.Of(0), ally.Area.PlayArea);
        Assert.Equal(PlayArea.Of(0), moving.Area.PlayArea);
        Assert.Equal(ally.ObjectId, moving.Area.Host);
    }

    [Rule("rr:ownership-and-control.7.3")]
    [Fact]
    public void APlayedEventGoesToItsOwnersDiscardPile()
    {
        // An event controlled from another player's hand is still placed "in
        // its owner's discard pile" after it resolves.
        var facts = new Facts();
        var world = Board(facts, players: 2);
        var card = world.CreateCard("event", world.Seats[0].Hand);
        World.MoveToTop(card, world.Seats[1].Hand);
        CardPlay.Play(world, facts, new NoCardAbilities(), world.Seats[1], card, [], []);
        Assert.Equal(0, card.Owner);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(PlayArea.Of(0), card.Area.PlayArea);
    }

    [Rule("rr:ownership-and-control.7.4")]
    [Fact]
    public void ACardDiscardedFromAnotherPlayersHandGoesToItsOwner()
    {
        // A card discarded from a player's hand is placed "in its owner's
        // discard pile," not the discard pile beside that hand.
        var facts = new Facts();
        var world = Board(facts, players: 2);
        var card = world.CreateCard("event", world.Seats[0].Hand);
        World.MoveToTop(card, world.Seats[1].Hand);
        Discard.Card(world, card, "test", []);
        Assert.Equal(0, card.Owner);
        Assert.Equal(PlayArea.Of(0), card.Area.PlayArea);
    }

    [Rule("rr:permanent.1")]
    [Fact]
    public void OnlyAnEffectFromTheSameSetCanRemoveAPermanent()
    {
        // Permanent forbids removal by other sets and expressly permits it for
        // "card abilities in the same set."
        var facts = new Facts();
        facts.Sets["same"] = "hero-set";
        facts.Sets["permanent"] = "hero-set";
        facts.Sets["other"] = "scenario-set";
        var allowed = Board(facts);
        var target = allowed.CreateCard("permanent", allowed.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var source = allowed.CreateCard("same", allowed.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Discard.CardFromEffect(allowed, facts, source, target, "test", []);
        Assert.Equal(DeckType.DiscardPile, target.Area.Type);
        var refused = Board(facts);
        target = refused.CreateCard("permanent", refused.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        source = refused.CreateCard("other", refused.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Assert.Throws<RulesNotImplementedException>(() => Discard.CardFromEffect(refused, facts, source, target, "test", []));
        Assert.Equal(DeckType.SupportsArea, target.Area.Type);
    }

    [Rule("rr:player-deck.3")]
    [Fact]
    public void ADiscardEffectStopsAtThePlayerDeckReshuffle()
    {
        // If the deck empties while cards are being discarded, "no further
        // cards are discarded from the newly shuffled deck."
        var facts = new Facts();
        var world = Board(facts);
        var deck = world.Seats[0].Deck;
        World.MoveToTop(deck.Cards[0], world.AreaOf(DeckType.RemovedArea));
        var pile = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        var first = world.CreateCard("resource", deck);
        var second = world.CreateCard("resource", deck);
        world.CreateCard("resource", pile);
        world.CreateCard("resource", pile);
        world.CreateCard("treachery", world.AreaOf(DeckType.EncounterDeck));
        var discarded = PlayerDeck.DiscardTop(world, 0, 4, "test", []);
        Assert.Equal([second.ObjectId, first.ObjectId], discarded.Select(card => card.ObjectId));
        Assert.Equal(4, deck.Cards.Count);
    }
}
