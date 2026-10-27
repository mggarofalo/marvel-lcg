using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkNemesisReviewTests
{
    [Fact]
    [Rule("rr:attack-enemy-activation.1.3")]
    public void AbominationResponseFollowsTheHelpingDefendersPlayer()
    {
        // "That player becomes the new target of that attack."
        var (world, runner) = HulkNemesisFixture.Create();
        Forms.Change(world.Seats[1], world.Facts);
        var enemy = world.CreateCard("10026", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var originalTop = world.CreateCard("01044", world.Seats[1].Deck);
        var helperTop = world.CreateCard("01090", world.Seats[0].Deck);
        var helper = world.CreateCard("01050", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), Round: 1, Number: 2,
            Subject: enemy.ObjectId, Seat: 1));
        HulkNemesisFixture.Defend(world, runner, 0, helper);
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(3, helper.Damage);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, helperTop.Area.Type);
        Assert.Same(world.Seats[1].Deck, originalTop.Area);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:player-deck.1")]
    [Rule("rr:player-deck.4")]
    public void AbominationHandlesEmptyAndLastCardDecksWithoutLosingTheDiscardReceipt(bool lastCard)
    {
        // An empty deck with no discard "does not reset until there is at least one card".
        // On reset the player "immediately deals themself one facedown encounter card".
        var (world, runner) = HulkNemesisFixture.Create();
        var seat = world.Seats[1];
        Forms.Change(seat, world.Facts);
        foreach (var card in seat.Deck.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        var pile = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1);
        foreach (var card in pile.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        var top = lastCard ? world.CreateCard("01090", seat.Deck) : null;
        var enemy = world.CreateCard("10026", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), Round: 1, Number: 2,
            Subject: enemy.ObjectId, Seat: 1));
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(lastCard ? 5 : 3, seat.IdentityCard.Damage);
        Assert.Equal(lastCard ? 1 : 0, seat.Deck.Cards.Count);
        if (lastCard) Assert.Same(seat.Deck, top!.Area);
        Assert.Empty(pile.Cards);
        Assert.Equal(lastCard ? 1 : 0,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void LockedTotalDestructionLeavesAnotherSchemeLegalForPrintedThwartEvent()
    {
        var (world, runner) = HulkNemesisFixture.Create();
        Forms.Change(world.Seats[0], world.Facts);
        var locked = HulkNemesisFixture.Dealt(world, "10027");
        WreckingRemainingSchemeFixture.Finish(world, runner);
        world.CreateCard("10026", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 3);
        var source = world.CreateCard("10019", world.Seats[0].Hand);
        var payment = world.CreateCard("01090", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        Assert.DoesNotContain(locked, BasicPowers.Thwartable(world, world.Facts, 0));
        Assert.Contains(main, BasicPowers.Thwartable(world, world.Facts, 0));
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [main.ObjectId]));
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(4, locked.Tokens["k_threat"]);
        Assert.Equal(1, main.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:surge.1")]
    [Rule("rr:form-change-form.5")]
    public void ClashWithoutAnyHeroOrAllyDealsSurgeAndDiscardsTheTreachery()
    {
        // Surge: "Deal yourself 1 facedown encounter card."
        // In alter-ego form, "card abilities that interact with their hero do not interact with their identity."
        var (world, runner) = HulkNemesisFixture.Create();
        Assert.All(world.Seats, seat => Assert.Equal(CardKind.AlterEgo, world.Facts.Kind(seat.IdentityCard.FaceId)));
        var source = HulkNemesisFixture.Dealt(world, "10028", 1);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:form-change-form.5")]
    public void ClashExcludesAlterEgosFromZeroAttackTieWithLegalCharacter(bool hero)
    {
        // In alter-ego form, "card abilities that interact with their hero do not interact with their identity."
        var (world, runner) = HulkNemesisFixture.Create();
        var alterEgo = world.Seats[0].IdentityCard;
        Assert.Equal(CardKind.AlterEgo, world.Facts.Kind(alterEgo.FaceId));
        if (hero) Forms.Change(world.Seats[1], world.Facts);
        var recipient = hero ? world.Seats[1].IdentityCard
            : world.CreateCard("01050", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "attack", Amount: hero ? -2 : -3, Card: recipient.ObjectId, Affects: recipient.ObjectId));
        world.CreateCard("01167", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var source = HulkNemesisFixture.Dealt(world, "10028");
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(3, recipient.Damage);
        Assert.Equal(0, alterEgo.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
