using System.Collections.Immutable;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

// Synthetic blank-minion identities exercise the general engine boundary;
// they do not admit another scenario to the Core runtime.
public sealed class EffectiveCardLifecycleTests
{
    private static readonly EffectiveCardProfile Profile = new(
        "synthetic-scout", "Scout", CardKind.Minion, ["ROBOT"],
        new Dictionary<string, long> { ["SCH"] = 2, ["ATK"] = 3, ["HP"] = 4 }
            .ToImmutableDictionary(StringComparer.Ordinal));

    [Fact]
    public void LocationAndFacingDoNotAssignATemporaryIdentity()
    {
        var world = Board();
        var card = world.CreateCard("held-card", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        card.TurnFaceDown();

        Assert.Null(card.InstanceState.Profile);
        Assert.Equal(CardKind.Ally, EffectiveCards.Kind(card, world.Facts));
        Assert.Equal("Printed ally", EffectiveCards.Title(card, world.Facts));
        Assert.Equal(["AVENGER"], Traits.Of(world, card, world.Facts));
        Assert.DoesNotContain(card, BasicPowers.Attackable(world, world.Facts, 0));
    }

    [Rule("rr:base-value")]
    [Fact]
    public void AnAssignedIdentityHasItsOwnBaseValuesAndNoUnderlyingAbilitiesOrResources()
    {
        // "The value of a quantity before any modifiers are applied."
        var world = Board();
        world.CreateCard("held-card", world.Seats[0].Deck);
        var card = FacedownMinions.EngageTop(world, 0, Profile, "synthetic", "Engage", [])!;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "attack", Amount: 1, Affects: card.ObjectId));

        Assert.Equal("held-card", card.FaceId);
        Assert.Equal("Scout", EffectiveCards.Title(card, world.Facts));
        Assert.Equal(["ROBOT"], Traits.Of(world, card, world.Facts));
        Assert.Equal(3, EffectiveCards.BaseValue(card, world.Facts, "ATK", 2));
        Assert.Equal(4, StateFields.Modified(world, card, "attack", world.Facts, 2));
        Assert.Equal(4, DamagePlacement.Health(world, world.Facts, card));
        Assert.False(Keywords.Has(world, card, "retaliate", world.Facts));
        Assert.Empty(world.Abilities.ResourcesGeneratedBy(world, card, null));
        Assert.Equal(Profile.Id, world.Digest().Cards.Single(record => record.Id == card.ObjectId).Profile!.Id);
    }

    [Rule("rr:leaves-play.1")]
    [Theory]
    [InlineData(DeckType.DiscardPile)]
    [InlineData(DeckType.HandsArea)]
    [InlineData(DeckType.VictoryDisplay)]
    [InlineData(DeckType.RemovedArea)]
    public void DepartureEndsTheCopyBeforeReentry(DeckType destination)
    {
        // "there is no memory of its previous state" when a card returns.
        var world = Board();
        world.CreateCard("held-card", world.Seats[0].Deck);
        var card = FacedownMinions.EngageTop(world, 0, Profile, "synthetic", "Engage", [])!;
        card.TakeDamage(2);
        card.Exhaust();
        card.PlaceTokens("c_test", 3);
        var previous = card.InstanceState;
        int incarnation = card.Incarnation;

        World.MoveToTop(card, world.AreaOf(destination, PlayArea.Of(0), cardOwner: 0));

        Assert.NotSame(previous, card.InstanceState);
        Assert.Null(card.InstanceState.Profile);
        Assert.Equal(0, card.Damage);
        Assert.True(card.Ready);
        Assert.Equal(0, card.Tokens["c_test"]);
        Assert.Equal(2, previous.Damage);
        Assert.Same(Profile, previous.Profile);
        Assert.Equal(CardKind.Ally, EffectiveCards.Kind(card, world.Facts));
        Assert.Null(world.Digest().Cards.Single(record => record.Id == card.ObjectId).Profile);
        if (destination == DeckType.RemovedArea)
        {
            Assert.Throws<InvalidOperationException>(() => World.MoveToTop(card, world.Seats[0].Hero));
            return;
        }
        World.MoveToTop(card, world.Seats[0].Hero);
        Assert.Equal(incarnation + 1, card.Incarnation);
        Assert.Null(card.InstanceState.Profile);
        Assert.Equal(0, card.Damage);
    }

    [Rule("rr:engage.1")]
    [Fact]
    public void AnInPlayMovePreservesTheCopyAndDefeatUsesThePhysicalOwner()
    {
        // An engaged minion remains engaged "until it is defeated or removed
        // by a card ability, or until the player is eliminated".
        var world = Board();
        world.CreateCard("held-card", world.Seats[0].Deck);
        world.CreateCard("held-card", world.Seats[0].Deck);
        var card = FacedownMinions.EngageTop(world, 0, Profile, "synthetic", "Engage", [])!;
        var copy = card.InstanceState;
        card.TakeDamage(1);
        World.MoveToTop(card, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        card.TurnFaceDown();
        Assert.Same(copy, card.InstanceState);
        Assert.Same(Profile, card.InstanceState.Profile);
        Assert.Equal(1, card.Damage);
        Assert.Equal(0, card.Owner);
        Agendas.Happening(world);
        var events = new List<GameEvent>();

        Assert.True(DamagePlacement.Deal(world, world.Facts, card, card, 3, "synthetic", "Damage", events));

        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(PlayArea.Of(0), card.Area.PlayArea);
        Assert.Null(card.InstanceState.Profile);
        Assert.Equal(0, card.Damage);
        Assert.Contains(events.OfType<CardsMoved>(), moved => moved.Subjects?[card.ObjectId] == "Scout");
    }

    private static World Board()
    {
        var world = new World(new Printed(), 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("hero", seat.Hero);
        }
        return world;
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId == "hero" ? CardKind.Hero : CardKind.Ally;
        public string Title(string faceId) => faceId == "hero" ? "Hero" : "Printed ally";
        public IReadOnlyList<string> Traits(string faceId) => ["AVENGER"];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string> { ["HP"] = "9", ["ATK"] = "8", ["Retaliate"] = "3", ["RES"] = "BB" };
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out var value) && long.TryParse(value, out long number) ? number : fallback;
    }
}
