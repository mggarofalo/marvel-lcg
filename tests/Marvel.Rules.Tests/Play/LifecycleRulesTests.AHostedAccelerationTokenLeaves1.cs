using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class LifecycleRulesAHostedAccelerationTokenLeavesTests : LifecycleRulesTestBase
{
    [Rule("rr:acceleration-token.3")]
    [Fact]
    public void AHostedAccelerationTokenLeavesWithANonMainSchemeCard()
    {
        // "Acceleration tokens placed on cards other than the main scheme are
        // removed from play when the card they are placed on leaves play."
        var facts = new Facts();
        var world = Board(facts);
        var side = world.CreateCard("sideA", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens(EncounterDeck.AccelerationToken, 2);
        var events = new List<GameEvent>();
        Discard.Card(world, side, "test", events);
        Assert.Equal(0, side.Tokens[EncounterDeck.AccelerationToken]);
        Assert.Contains(events.OfType<FieldSet>(), changed => changed.Card == side.ObjectId && changed.Field == EncounterDeck.AccelerationToken && changed.From == 2 && changed.To == 0);
    }

    [Rule("rr:leaves-play")]
    [Rule("rr:leaves-play.1")]
    [Rule("rr:leaves-play.2")]
    [Rule("rr:leaves-play.2.1")]
    [Rule("rr:leaves-play.2.2")]
    [Rule("rr:leaves-play.2.3")]
    [Fact]
    public void ACardReturningToPlayIsANewCopyWithoutHostedState()
    {
        // A non-villain that leaves returns its attached, tucked, boost, token,
        // and status state to the supply; if it returns, it is a new copy.
        var facts = new Facts();
        var world = Board(facts);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        ally.Exhaust();
        ally.PlaceTokens("c_charge", 3);
        var attached = world.CreateCard("upgrade", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId, cardOwner: 0));
        var boost = world.CreateCard("treachery", world.AreaOf(DeckType.BoostCardsDeck, PlayArea.Of(0), ally.ObjectId));
        var status = world.CreateCard("status", world.AreaOf(DeckType.StatusArea, PlayArea.Of(0), ally.ObjectId));
        int incarnation = ally.Incarnation;
        Discard.Card(world, ally, "test", []);
        CardPlay.PutAllyIntoPlay(world, facts, new NoCardAbilities(), ally, 0, "test", []);
        Assert.Equal(incarnation + 1, ally.Incarnation);
        Assert.Equal(0, ally.Damage);
        Assert.True(ally.Ready);
        Assert.Equal(0, ally.Tokens["c_charge"]);
        Assert.Equal(DeckType.DiscardPile, attached.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, boost.Area.Type);
        Assert.Equal(DeckType.RemovedArea, status.Area.Type);
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Rule("rr:in-play-and-out-of-play.8")]
    [Rule("rr:leaves-play.1")]
    [Fact]
    public void CardsCannotEnterHostedAreasAfterTheHostLeavesPlay()
    {
        // The discard pile is out of play, and a card that left is a new copy.
        // Empty areas from the former copy may remain in the world, but neither
        // creating nor moving a card may populate them again.
        var facts = new Facts();
        var world = Board(facts);
        int areasBefore = world.Areas.Count;
        Assert.Throws<ArgumentOutOfRangeException>(() => world.CreateArea(DeckType.StatusArea, playArea: PlayArea.Of(0), host: world.Cards.Count + 1));
        Assert.Equal(areasBefore, world.Areas.Count);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var statusArea = world.AreaOf(DeckType.StatusArea, PlayArea.Of(0), ally.ObjectId);
        var upgradeArea = world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId, cardOwner: 0);
        var upgrade = world.CreateCard("upgrade", world.Seats[0].Hand);
        Discard.Card(world, ally, "test", []);
        int cardsBefore = world.Cards.Count;
        Assert.Throws<InvalidOperationException>(() => world.CreateCard("status", statusArea));
        Assert.Equal(cardsBefore, world.Cards.Count);
        Assert.Throws<InvalidOperationException>(() => World.MoveToTop(upgrade, upgradeArea));
        Assert.Same(world.Seats[0].Hand, upgrade.Area);
        Assert.Empty(statusArea.Cards);
        Assert.Empty(upgradeArea.Cards);
    }

    [Rule("rr:all-purpose-counter.3")]
    [Fact]
    public void AMovedCounterTakesTheDestinationCardsType()
    {
        // A moved counter "loses any previous type" and gains the type defined
        // by the card it now occupies.
        var facts = new Facts();
        facts.Uses["quiver"] = "3,arrow";
        facts.Uses["shooter"] = "3,web";
        var world = Board(facts);
        var quiver = world.CreateCard("quiver", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var shooter = world.CreateCard("shooter", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        quiver.PlaceTokens("c_arrow", 2);
        long moved = Counters.Move(world, facts, quiver, shooter, "arrow", 1, "test", []);
        Assert.Equal(1, moved);
        Assert.Equal(1, quiver.Tokens["c_arrow"]);
        Assert.Equal(1, shooter.Tokens["c_web"]);
        Assert.Equal(0, shooter.Tokens.GetValueOrDefault("c_arrow"));
    }

    [Rule("rr:tuck")]
    [Rule("rr:tuck.1")]
    [Rule("rr:tuck.2")]
    [Fact]
    public void ATuckedCardIsFaceupOutOfPlayAndDiscardsWithItsHost()
    {
        // Tucked cards are faceup and out of play, are not attachments, and
        // are discarded when the card above them leaves play.
        var facts = new Facts();
        var world = Board(facts);
        var host = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var tucked = world.CreateCard("event", world.Seats[0].Hand);
        Tuck.Card(world, tucked, host, "test", []);
        Assert.True(tucked.FaceUp);
        Assert.False(DeckTypes.IsInPlay(tucked.Area.Type));
        Assert.Equal(host.ObjectId, tucked.Area.Host);
        Discard.Card(world, host, "test", []);
        Assert.Equal(DeckType.DiscardPile, tucked.Area.Type);
    }

    [Rule("rr:removed-from-the-game.1")]
    [Rule("rr:removed-from-the-game.2")]
    [Fact]
    public void ARemovedCardCannotReenterAnyGameArea()
    {
        // Removed is an out-of-play state whose cards "cannot reenter the game
        // through any means"; set-aside is the retrievable state instead.
        var facts = new Facts();
        var world = Board(facts);
        var card = world.CreateCard("event", world.Seats[0].Hand);
        World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        Assert.Throws<InvalidOperationException>(() => World.MoveToTop(card, world.Seats[0].Hand));
        Assert.Equal(DeckType.RemovedArea, card.Area.Type);
    }

    [Rule("rr:discard.1")]
    [Rule("rr:discard.2")]
    [Fact]
    public void DiscardsLandFaceupOnTopOfTheirOwnersPile()
    {
        // Player and encounter cards both land faceup on top; the player card
        // uses its owner's pile, while the encounter card uses the scenario's.
        var facts = new Facts();
        var world = Board(facts);
        var playerCard = world.CreateCard("event", world.Seats[0].Hand);
        var encounterCard = world.CreateCard("treachery", world.AreaOf(DeckType.RevealingArea));
        Discard.Card(world, playerCard, "test", []);
        Discard.Card(world, encounterCard, "test", []);
        Assert.True(playerCard.FaceUp);
        Assert.Same(playerCard, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards[^1]);
        Assert.True(encounterCard.FaceUp);
        Assert.Same(encounterCard, world.AreaOf(DeckType.EncounterDiscardPile).Cards[^1]);
    }

    [Rule("rr:discard-pile.4")]
    [Fact]
    public void AnEmptyDiscardPileDoesNotShuffleItsDeck()
    {
        // An instruction to shuffle a zero-card discard pile back into a deck
        // does not shuffle that deck or consume the game's random stream.
        var facts = new Facts();
        var world = Board(facts);
        var empty = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        Assert.False(world.Shuffle(empty));
        Assert.Empty(empty.Cards);
    }

    [Rule("rr:flip.2")]
    [Rule("rr:flip.2.1")]
    [Fact]
    public void AFlipToTheSameCardTypeRetainsHostedState()
    {
        // With the same type, "the card retains all attached cards, tucked
        // cards, status cards, and tokens."
        var facts = new Facts();
        var world = Board(facts);
        var card = world.CreateCard("sideA,sideB", world.AreaOf(DeckType.SideSchemesArea));
        card.PlaceTokens("k_threat", 3);
        var attached = world.CreateCard("upgrade", world.AreaOf(DeckType.UpgradesArea, card.Area.PlayArea, card.ObjectId));
        CardFlip.To(world, facts, card, "sideB", "test", []);
        Assert.Equal("sideB", card.FaceId);
        Assert.Equal(3, card.Tokens["k_threat"]);
        Assert.Equal(DeckType.UpgradesArea, attached.Area.Type);
        Assert.Equal(card.ObjectId, attached.Area.Host);
    }

    [Rule("rr:flip.2")]
    [Rule("rr:flip.2.2")]
    [Fact]
    public void AFlipToADifferentCardTypeDiscardsHostedState()
    {
        // With a different type, "all attached cards, tucked cards, status
        // cards, and tokens are discarded from the card."
        var facts = new Facts();
        var world = Board(facts);
        var card = world.CreateCard("minion,environment", world.AreaOf(DeckType.EngagedEnemiesArea));
        card.PlaceTokens("k_threat", 3);
        var attached = world.CreateCard("upgrade", world.AreaOf(DeckType.UpgradesArea, card.Area.PlayArea, card.ObjectId));
        var status = world.CreateCard("status", world.AreaOf(DeckType.StatusArea, card.Area.PlayArea, card.ObjectId));
        CardFlip.To(world, facts, card, "environment", "test", []);
        Assert.Equal("environment", card.FaceId);
        Assert.Equal(0, card.Tokens["k_threat"]);
        Assert.Equal(DeckType.EncounterDiscardPile, attached.Area.Type);
        Assert.Equal(DeckType.RemovedArea, status.Area.Type);
    }

    [Rule("rr:form-change-form.6.2")]
    [Fact]
    public void AnAdditionalFormChangeTriggersWithoutSpendingTheIdentityFlip()
    {
        // It "does not count against the once-per-turn limit" but "does count
        // as changing form for the purpose of triggering card effects."
        var facts = new Facts();
        facts.Forms["gamma"] = "energy";
        var world = Board(facts);
        var seat = world.Seats[0];
        seat.FormChangedInRound = 4;
        var form = world.CreateCard("gamma", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), seat.IdentityCard.ObjectId, cardOwner: 0));
        form.TurnFaceDown();
        var events = new List<GameEvent>();
        Forms.ChangeAdditional(world, seat, facts, form, faceUp: true, round: 5, "test", events);
        Assert.True(form.FaceUp);
        Assert.Equal(4, seat.FormChangedInRound);
        Assert.Contains(world.Agenda.Outstanding, step => step.What == Steps.FormChanged && step.Subject == form.ObjectId);
        Assert.Contains(events.OfType<CardsFlipped>(), flipped => flipped.Cards.Contains(form.ObjectId) && flipped.FaceUp);
    }

    [Rule("rr:ownership-and-control.2.2")]
    [Fact]
    public void TakingAScenarioPlayerCardAlsoTransfersOwnership()
    {
        // A player taking control of a scenario-specific player card with a
        // player back "becomes the owner of that card."
        var facts = new Facts();
        facts.Classes["ally"] = "Scenario";
        var world = Board(facts, players: 2);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AsideDeck));
        CardPlay.PutAllyIntoPlay(world, facts, new NoCardAbilities(), ally, 0, "test", []);
        Assert.Equal(0, ally.Owner);
        Assert.Equal(PlayArea.Of(0), ally.Area.PlayArea);
        CardControlTransfer.TakeControl(world, facts, ally, 1);
        Assert.Equal(1, ally.Owner);
        Assert.Equal(PlayArea.Of(1), ally.Area.PlayArea);
    }

    [Rule("rr:ownership-and-control.6")]
    [Rule("rr:ownership-and-control.7.1")]
    [Fact]
    public void AttachedUpgradesFollowControlAndReturnWithTheirHost()
    {
        // "Upgrades on a card that changes control also change control"; when
        // the changing ability ends, the card "reverts to its owner's control."
        var facts = new Facts();
        var world = Board(facts, players: 2);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var upgrade = world.CreateCard("upgrade", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId, cardOwner: 0));
        CardControlTransfer.TakeControl(world, facts, ally, 1);
        Assert.Equal(PlayArea.Of(1), ally.Area.PlayArea);
        Assert.Equal(PlayArea.Of(1), upgrade.Area.PlayArea);
        CardControlTransfer.ReturnToOwnerControl(world, facts, ally);
        Assert.Equal(PlayArea.Of(0), ally.Area.PlayArea);
        Assert.Equal(PlayArea.Of(0), upgrade.Area.PlayArea);
    }
}
