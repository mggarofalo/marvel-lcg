using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeNemesisIndependentTests
{
    [Fact]
    [Rule("rr:player-deck.1")]
    [Rule("rr:printed")]
    public void FinalPhysicalCardResetDoesNotBorrowAnOlderWildDiscard()
    {
        // An empty player deck resets immediately; "printed" refers to the value physically printed on the card.
        var world = DoctorStrangeNemesisFixture.Board();
        foreach (var card in world.Seats[1].Deck.Cards.ToArray())
            World.MoveToTop(card, world.Seats[1].Hand);
        foreach (var seat in world.Seats) seat.IdentityCard.Exhaust();
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1);
        var olderWild = world.CreateCard("01055", discard);
        var top = world.CreateCard("01090", world.Seats[1].Deck);
        var foreign = world.CreateCard("01055", world.Seats[0].Deck);
        var encounterTop = world.AreaOf(DeckType.EncounterDeck).Cards[^1];
        var mordo = world.CreateCard("09028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1, Subject: mordo.ObjectId, Seat: 1));

        Sequence.Finish(world, world.Facts, world.Abilities, events);

        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
        Assert.True(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Stunned));
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Confused));
        Assert.Equal(new[] { top.ObjectId, olderWild.ObjectId }.Order(), world.Seats[1].Deck.Cards.Select(card => card.ObjectId).Order());
        Assert.Empty(discard.Cards);
        Assert.Same(encounterTop, Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards));
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Same(world.Seats[0].Deck, foreign.Area);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Single(events.OfType<CardsMoved>(), move => move.Verb == "Reset");
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.IsOver);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:choose-game-element.1")]
    [Rule("rr:printed")]
    public void UniqueHighestCardSuppliesPenaltyAfterLeavingHand(bool alterEgo)
    {
        // The resolving player chooses; "printed" refers to the value physically printed on that card.
        var world = DoctorStrangeNemesisFixture.Board();
        if (alterEgo) world.Seats[1].IdentityCard.TurnTo("09001b");
        world.FirstPlayer = 0;
        var highest = world.CreateCard("01091", world.Seats[1].Hand);
        var lower = world.CreateCard("01093", world.Seats[1].Hand);
        var foreign = world.CreateCard("01091", world.Seats[0].Hand);
        var older = world.CreateCard("01005", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        CardPayment.ReduceNextCardCost(world, world.Seats[1].IdentityCard, 1, 4);
        Assert.Equal(0, CardPayment.CostOf(world, world.Facts, world.Seats[1], highest).Amount);
        var card = world.CreateCard("09031", world.AreaOf(DeckType.RevealingArea));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard), 1, 1, Subject: card.ObjectId, Seat: 1));

        var prompt = Sequence.Work(world, world.Facts, world.Abilities, [])!;
        Assert.Equal(1, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.Equal(highest.ObjectId, Assert.Single(prompt.Affordances).Id);
        Sequence.Answer(world, world.Facts, world.Abilities, prompt, Decision.Take(highest.ObjectId), []);
        Sequence.Finish(world, world.Facts, world.Abilities, []);

        Assert.Same(world.Seats[1].Hand, lower.Area);
        Assert.Same(world.Seats[0].Hand, foreign.Area);
        Assert.Equal(DeckType.DiscardPile, highest.Area.Type);
        Assert.Same(highest.Area, older.Area);
        Assert.Equal(alterEgo ? 0 : 4, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(alterEgo ? 4 : 0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
