using System.Collections.Immutable;
using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual authored rows exercise printed play restrictions without admitting Black Widow.
public sealed class ControlledCharacterPlayRestrictionTests : CoreCardTestBase
{
    [Theory]
    [InlineData("08018")]
    [InlineData("08033")]
    public void CatalogReadsTheActualControlledSpyCharacterRestriction(string face)
    {
        Assert.StartsWith("Play only if you control a Spy character.", Cards.Text(face));
        Assert.Equal("SPY", Cards.RequiredControlledCharacterTrait(face));
        Assert.Null(Cards.RequiredIdentityTrait(face));
        Assert.Null(Cards.RequiredControlledCharacterTrait("01006"));
        Assert.Equal("AVENGER", Cards.RequiredIdentityTrait("03025"));
        Assert.Null(Cards.RequiredControlledCharacterTrait("03025"));
    }

    [Theory]
    [InlineData("08018", "08001a")]
    [InlineData("08018", "08001b")]
    [InlineData("08033", "08001a")]
    [InlineData("08033", "08001b")]
    [Rule("rr:character")]
    public void EitherSpyIdentityFormPermitsPlay(string face, string identity)
    {
        // rr:character: "Identities (heroes and alter-egos), allies, villains, and minions are all characters."
        var world = Hero(identity);
        var card = world.CreateCard(face, world.Seats[0].Hand);
        Pay(world);
        Assert.NotNull(CardPayment.Price(world, Cards, world.Seats[0], card));
    }

    [Theory]
    [InlineData("08018", 0, 0, DeckType.AlliesArea, true)]
    [InlineData("08033", 0, 0, DeckType.AlliesArea, true)]
    [InlineData("08018", 1, 0, DeckType.AlliesArea, true)]
    [InlineData("08018", 0, 1, DeckType.AlliesArea, false)]
    [InlineData("08033", 1, 1, DeckType.AlliesArea, false)]
    [InlineData("08018", 0, 0, DeckType.DiscardPile, false)]
    [InlineData("08033", 0, 0, DeckType.HandsArea, false)]
    [InlineData("08018", 0, 0, DeckType.PlayerDeck, false)]
    [InlineData("08018", 0, 0, DeckType.AsideDeck, false)]
    [Rule("rr:ownership-and-control.8")]
    public void OnlyCurrentControlOfAnInPlaySpyAllyQualifies(string face, int owner, int controller,
        DeckType location, bool permitted)
    {
        // rr:ownership-and-control.8: "only refers to cards in play currently under that player's control."
        var world = Hero("01001b", 2);
        var ally = world.CreateCard("08002", world.AreaOf(location, PlayArea.Of(controller), cardOwner: owner));
        var card = world.CreateCard(face, world.Seats[0].Hand);
        var sameCost = world.CreateCard("01006", world.Seats[0].Hand);
        Pay(world);
        Assert.Equal(owner, ally.Owner);
        Assert.NotNull(CardPayment.Price(world, Cards, world.Seats[0], sameCost));
        Assert.Equal(CardPayment.CostOf(world, Cards, world.Seats[0], sameCost).Amount,
            CardPayment.CostOf(world, Cards, world.Seats[0], card).Amount);
        Assert.Equal(permitted, CardPayment.Price(world, Cards, world.Seats[0], card) is not null);
    }

    [Fact]
    [Rule("rr:character")]
    [Rule("rr:traits.1")]
    public void EffectiveCharacterKindAndCurrentTraitDecideEligibility()
    {
        // rr:traits.1: abilities "reference cards that possess or lack specific traits."
        // rr:character includes identities, allies, villains and minions, rather than support cards.
        var world = Hero("01001b");
        var card = world.CreateCard("08018", world.Seats[0].Hand);
        var support = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        Pay(world);
        var grant = world.Effects.Register(new(EffectSource.LastingEffect, Traits.Granted + "SPY",
            Affects: support.ObjectId));
        Assert.Null(CardPayment.Price(world, Cards, world.Seats[0], card));
        support.AssignProfile(new EffectiveCardProfile("synthetic-spy", "Synthetic Spy", CardKind.Ally,
            [], ImmutableDictionary<string, long>.Empty));
        Assert.NotNull(CardPayment.Price(world, Cards, world.Seats[0], card));
        grant.Dispose();
        Assert.Null(CardPayment.Price(world, Cards, world.Seats[0], card));
        support.AssignProfile(new EffectiveCardProfile("synthetic-spy", "Synthetic Spy", CardKind.Ally,
            ["SPY"], ImmutableDictionary<string, long>.Empty));
        var loss = world.Effects.Register(new(EffectSource.LastingEffect,
            Characteristics.Lost + Traits.Granted + "SPY", Affects: support.ObjectId));
        Assert.Null(CardPayment.Price(world, Cards, world.Seats[0], card));
        loss.Dispose();
        Assert.NotNull(CardPayment.Price(world, Cards, world.Seats[0], card));
        support.AssignProfile(new EffectiveCardProfile("synthetic-spy", "Synthetic Spy", CardKind.Minion,
            ["SPY"], ImmutableDictionary<string, long>.Empty));
        Assert.Equal(World.Scenario, CardControl.ControllerOf(world, support));
        Assert.Null(CardPayment.Price(world, Cards, world.Seats[0], card));
    }

    [Theory]
    [InlineData("08018")]
    [InlineData("08033")]
    [Rule("rr:initiating-abilities.step.2")]
    public void QueriesAndStalePlayPreservePendingExecutionAndMatchAnUnprobedControl(string face)
    {
        // Step 2: "Check play restrictions: can the card be played, or the ability initiated, at this time?"
        var runner = Marvel.Content.Tests.Cards.AuthoredCards.Runner();
        var (game, world) = Playing(_ => { }, abilities: runner);
        var (controlGame, control) = Playing(_ => { }, abilities: runner);
        var card = world.CreateCard(face, world.Seats[0].Hand);
        var expectedCard = control.CreateCard(face, control.Seats[0].Hand);
        var spy = world.CreateCard("08002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        var controlSpy = control.CreateCard("08002", control.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        int payment = Pay(world);
        int expectedPayment = Pay(control);
        var pending = game.Pending;
        string before = Snapshot(world);
        for (int i = 0; i < 3; i++) Assert.NotNull(CardPayment.Price(world, Cards, world.Seats[0], card));
        Assert.Equal(before, Snapshot(world));
        Assert.Same(pending, game.Pending);
        World.MoveToTop(spy, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        before = Snapshot(world);
        var events = new List<GameEvent>();
        Assert.Null(CardPayment.Price(world, Cards, world.Seats[0], card));
        Assert.Throws<RulesNotImplementedException>(() =>
            CardPlay.Play(world, Cards, runner, world.Seats[0], card, [payment], events));
        Assert.Equal(before, Snapshot(world));
        Assert.Empty(events);
        Assert.Same(pending, game.Pending);
        World.MoveToTop(controlSpy, control.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        World.MoveToTop(spy, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        World.MoveToTop(controlSpy, control.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        var expectedEvents = new List<GameEvent>();
        CardPlay.Play(world, Cards, runner, world.Seats[0], card, [payment], events);
        CardPlay.Play(control, Cards, runner, control.Seats[0], expectedCard, [expectedPayment], expectedEvents);
        Assert.Equal(DeckType.UpgradesArea, card.Area.Type);
        Assert.Equal(expectedEvents.Select(Describe), events.Select(Describe));
        Assert.Equal(Snapshot(control), Snapshot(world));
        Assert.Equal(controlGame.Pending?.Label, game.Pending?.Label);
    }

    private static int Pay(World world) => world.CreateCard("01093", world.Seats[0].Hand).ObjectId;

    private static string Describe(GameEvent item) => JsonSerializer.Serialize(item, item.GetType());

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(),
        Random = world.Random.Generator.GetState(), world.Random.Generator.WordsConsumed,
        world.Agenda.Stage, world.Agenda.IsBusy, Effects = world.Effects.Registered,
        Frames = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject }),
    });
}
