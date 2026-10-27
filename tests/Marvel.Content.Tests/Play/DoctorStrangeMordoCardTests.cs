using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeMordoCardTests
{
    [Theory]
    [InlineData("01090", 2, true, false)]
    [InlineData("01088", 4, false, false)]
    [InlineData("01089", 2, false, true)]
    [InlineData("01055", 4, true, true)]
    [InlineData("37010", 4, true, false)]
    [InlineData("50007", 2, true, true)]
    [InlineData("47010a", 4, false, true)]
    [InlineData("16033", 4, true, true)]
    [Rule("rr:printed")]
    [Rule("rr:attack-enemy-activation.5")]
    public void AttackUsesAttackedPlayersExactPrintedResource(string face, int damage, bool stun, bool confuse)
    {
        // "Printed" refers to information physically printed on the card.
        // "When [enemy name] attacks" has the same timing as attack initiation.
        var world = DoctorStrangeNemesisFixture.Board();
        var hero = world.Seats[1].IdentityCard;
        foreach (var seat in world.Seats) seat.IdentityCard.Exhaust();
        var top = world.CreateCard(face, world.Seats[1].Deck);
        var other = world.CreateCard("01055", world.Seats[0].Deck);
        world.CreateCard("01055", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        var mordo = world.CreateCard("09028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1, Subject: mordo.ObjectId, Seat: 1));
        Sequence.Finish(world, world.Facts, world.Abilities, []);
        Assert.Equal(damage, hero.Damage);
        Assert.Equal(stun, Statuses.Has(world, hero, Statuses.Stunned));
        Assert.Equal(confuse, Statuses.Has(world, hero, Statuses.Confused));
        Assert.Same(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1), top.Area);
        Assert.Same(world.Seats[0].Deck, other.Area);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:player-deck.4")]
    public void EmptyDeckWithoutDiscardHasNoResourceEffectOrReset()
    {
        // The deck "does not reset until there is at least one card" in its discard pile.
        var world = DoctorStrangeNemesisFixture.Board();
        foreach (var card in world.Seats[1].Deck.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        foreach (var seat in world.Seats) seat.IdentityCard.Exhaust();
        var mordo = world.CreateCard("09028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1, Subject: mordo.ObjectId, Seat: 1));
        Sequence.Finish(world, world.Facts, world.Abilities, events);
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Stunned));
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Confused));
        Assert.Empty(world.Seats[1].Deck.Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.DoesNotContain(events.OfType<CardsMoved>(), move => move.Verb == "Reset");
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:status-cards.2")]
    public void StunnedMordoDoesNotDiscardOrApplyInterrupt()
    {
        // "Status card abilities have timing priority over all conflicting triggered abilities."
        var world = DoctorStrangeNemesisFixture.Board();
        var top = world.CreateCard("01055", world.Seats[1].Deck);
        var mordo = world.CreateCard("09028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Statuses.Give(world, mordo, Statuses.Stunned);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1, Subject: mordo.ObjectId, Seat: 1));
        Sequence.Finish(world, world.Facts, world.Abilities, []);
        Assert.Same(world.Seats[1].Deck, top.Area);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.False(Statuses.Has(world, mordo, Statuses.Stunned));
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Confused));
    }

    [Fact]
    [Rule("rr:player-deck.1")]
    public void FinalDeckCardResetsAndStillSuppliesItsWildReceipt()
    {
        // An empty deck "immediately" resets from its discard pile and receives an encounter card.
        var world = DoctorStrangeNemesisFixture.Board();
        foreach (var card in world.Seats[1].Deck.Cards.ToArray()) World.MoveToTop(card, world.Seats[1].Hand);
        var top = world.CreateCard("01055", world.Seats[1].Deck);
        var mordo = world.CreateCard("09028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        foreach (var seat in world.Seats) seat.IdentityCard.Exhaust();
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1, Subject: mordo.ObjectId, Seat: 1));
        Sequence.Finish(world, world.Facts, world.Abilities, events);
        Assert.Same(top, Assert.Single(world.Seats[1].Deck.Cards));
        Assert.Equal(4, world.Seats[1].IdentityCard.Damage);
        Assert.True(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Stunned));
        Assert.True(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Confused));
        Assert.Single(events.OfType<CardsMoved>(), move => move.Verb == "Reset");
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }
}



