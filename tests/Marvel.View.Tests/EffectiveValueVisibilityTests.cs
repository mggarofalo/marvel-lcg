using System.Collections.Immutable;
using System.Text.Json;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.View.Tests;

public sealed class EffectiveValueVisibilityTests
{
    [Fact]
    public void PrivateOriginsHaveNoIdentifiersCountsOrderOrConditionsInAnotherSeatsExplanation()
    {
        World world = VisibilityFixture.Board();
        Card villain = world.AreaOf(DeckType.VillainArea).Cards[0];
        Card source = world.Seats[1].Hand.Cards[0];
        CardEffectiveValue before = Value(world, villain, Scope(world, 0));
        world.Effects.Register(new(EffectSource.LastingEffect, "scheme", 3, source.ObjectId, villain.ObjectId,
            Duration.NextTime("private-condition")));
        world.Effects.Register(new(EffectSource.LastingEffect, "scheme", -3, source.ObjectId, villain.ObjectId));

        CardEffectiveValue hidden = Value(world, villain, Scope(world, 0));
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(hidden));
        CardEffectiveValue authorized = Value(world, villain, Scope(world, 1));
        Assert.Equal(2, authorized.Calculation.Count);
        Assert.All(authorized.Calculation, step => Assert.Equal("one-hand", step.Source!.Title));
        Assert.Equal("private-condition", authorized.Calculation[0].Duration!.OnCondition);
        Assert.Equal(2, Value(world, villain,
            new PermissiveVisibilityPolicy().Authorize(null, world.Players)).Calculation.Count);
    }

    [Fact]
    public void PublicSourcesRemainUsefulWithoutExposingHiddenContributionSubtotals()
    {
        World world = VisibilityFixture.Board();
        Card villain = world.AreaOf(DeckType.VillainArea).Cards[0];
        Card hidden = world.Seats[1].Hand.Cards[0];
        Card shown = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)).Cards[0];
        world.Effects.Register(new(EffectSource.LastingEffect, "scheme", 3, hidden.ObjectId, villain.ObjectId));
        world.Effects.Register(new(EffectSource.LastingEffect, "scheme", 1, shown.ObjectId, villain.ObjectId));

        CardEffectiveValue value = Value(world, villain, Scope(world, 0));
        Assert.Equal(8, value.CurrentValue);
        CardValueCalculation step = Assert.Single(value.Calculation);
        Assert.Equal(1, step.Amount);
        Assert.Equal("player-ally", step.Source!.Title);
        Assert.DoesNotContain("one-hand", JsonSerializer.Serialize(value), StringComparison.Ordinal);
        Assert.DoesNotContain("Result", JsonSerializer.Serialize(step), StringComparison.Ordinal);
    }

    [Fact]
    public void HistoricalPublicOriginsAreInspectableWithoutLinkingToANewCopy()
    {
        World world = VisibilityFixture.Board();
        Card villain = world.AreaOf(DeckType.VillainArea).Cards[0];
        Card source = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)).Cards[0];
        world.Effects.Register(new(EffectSource.LastingEffect, "scheme", 1, source.ObjectId, villain.ObjectId));
        World.MoveToTop(source, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)));
        World.MoveToTop(source, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)));

        CardValueSourceDescriptor origin = Assert.Single(Value(world, villain, Scope(world, 0)).Calculation).Source!;
        Assert.Equal("player-ally", origin.Title);
        Assert.True(origin.Historical);
        Assert.Null(origin.CardId);
    }

    [Fact]
    public void AnOutOfPlaySourceNeverLinksARecycledEventOrADepartedCopy()
    {
        World world = VisibilityFixture.Board();
        Card villain = world.AreaOf(DeckType.VillainArea).Cards[0];
        Card source = world.Seats[1].Hand.Cards[0];
        World.MoveToTop(source, world.AreaOf(DeckType.RevealingArea));
        int incarnation = source.Incarnation;
        world.Effects.Register(new(EffectSource.LastingEffect, "scheme", 1, source.ObjectId, villain.ObjectId));
        World.MoveToTop(source, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)));
        World.MoveToTop(source, world.Seats[1].Hand);
        Assert.Equal(incarnation, source.Incarnation);

        foreach (int seat in new[] { 0, 1 })
        {
            CardValueSourceDescriptor origin = Assert.Single(Value(world, villain, Scope(world, seat)).Calculation).Source!;
            Assert.Equal("one-hand", origin.Title);
            Assert.True(origin.Historical);
            Assert.Null(origin.CardId);
        }

        Card ally = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)).Cards[0];
        world.Effects.Register(new(EffectSource.LastingEffect, "scheme", 1, ally.ObjectId, villain.ObjectId));
        World.MoveToTop(ally, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)));
        Assert.All(Value(world, villain, Scope(world, 1)).Calculation, step =>
        {
            Assert.True(step.Source!.Historical);
            Assert.Null(step.Source.CardId);
        });
    }

    [Fact]
    public void ADroneOriginAndItsHistoricalRecordContainOnlyItsPublicReplacementIdentity()
    {
        World world = VisibilityFixture.Board();
        Card villain = world.AreaOf(DeckType.VillainArea).Cards[0];
        Card source = world.CreateCard("underlying-player-card", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        source.AssignProfile(new("effective-drone", "Drone", CardKind.Minion, ["DRONE"],
            ImmutableDictionary<string, long>.Empty.Add("ATK", 1).Add("HP", 1)));
        source.TurnFaceDown();
        world.Effects.Register(new(EffectSource.LastingEffect, "scheme", 1, source.ObjectId, villain.ObjectId));

        AssertDroneOrigin(Value(world, villain, Scope(world, 0)), historical: false);
        World.MoveToTop(source, world.Seats[1].Hand);
        AssertDroneOrigin(Value(world, villain, Scope(world, 0)), historical: true);
    }

    [Fact]
    public void RepeatedProjectionsDoNotChangeStateRandomnessTimingOrSourceRegistrations()
    {
        World world = VisibilityFixture.Board();
        Card villain = world.AreaOf(DeckType.VillainArea).Cards[0];
        world.Effects.Register(new(EffectSource.LastingEffect, "scheme", 1, villain.ObjectId, villain.ObjectId,
            Duration.UntilEndOf(TimingPoints.EndOfRound)));
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        ContinuousEffect[] effects = world.Effects.Registered.ToArray();
        string before = JsonSerializer.Serialize(Value(world, villain, Scope(world, 0)));

        for (int i = 0; i < 4; i++)
            Assert.Equal(before, JsonSerializer.Serialize(Value(world, villain, Scope(world, 0))));

        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(effects, world.Effects.Registered);
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.Equal(4, Value(world, villain, Scope(world, 0)).CurrentValue);
    }

    private static void AssertDroneOrigin(CardEffectiveValue value, bool historical)
    {
        CardValueSourceDescriptor source = Assert.Single(value.Calculation).Source!;
        Assert.Equal("effective-drone", source.FaceId);
        Assert.Equal("Drone", source.Title);
        Assert.Equal(historical, source.Historical);
        Assert.Empty(source.RulesText);
        Assert.DoesNotContain("underlying-player-card", JsonSerializer.Serialize(value), StringComparison.Ordinal);
        Assert.DoesNotContain("Avengers Mansion", JsonSerializer.Serialize(value), StringComparison.Ordinal);
    }

    private static ViewScope Scope(World world, int seat) =>
        new RestrictedVisibilityPolicy(seat).Authorize(null, world.Players);

    private static CardEffectiveValue Value(World world, Card card, ViewScope scope) =>
        VisibilityFixture.Card(WorldProjection.For(world, null, [], scope).World, card.ObjectId).Face!.EffectiveValues["SCH"];
}
