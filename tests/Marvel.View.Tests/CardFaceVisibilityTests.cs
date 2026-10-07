using System.Collections.Immutable;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;
using static Marvel.View.Tests.VisibilityFixture;

namespace Marvel.View.Tests;

public sealed class CardFaceVisibilityTests
{
    [Fact]
    public void AFacedownDroneShowsOnlyItsEffectivePublicIdentity()
    {
        var board = Board();
        Area engaged = board.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(1));
        Card drone = board.CreateCard("underlying-player-card", board.Seats[1].Deck);
        World.MoveToTop(drone, engaged);
        drone.AssignProfile(new EffectiveCardProfile("effective-drone", "Drone", CardKind.Minion,
            ["DRONE"], new Dictionary<string, long> { ["SCH"] = 1, ["ATK"] = 1, ["HP"] = 1 }
                .ToImmutableDictionary(StringComparer.Ordinal)));
        drone.TurnFaceDown();
        drone.TakeDamage(1);
        board.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "attack", Amount: 2, Affects: drone.ObjectId));
        board.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "health", Amount: 2, Affects: drone.ObjectId));
        ViewScope[] scopes =
        [
            new RestrictedVisibilityPolicy(0).Authorize(null, board.Players),
            new PermissiveVisibilityPolicy().Authorize(null, board.Players),
        ];

        Assert.All(scopes, scope =>
        {
            CardDescriptor visible = Assert.Single(
                Assert.Single(
                    WorldProjection.For(board, null, [], scope).World.Areas,
                    area => area.Id == engaged.Id).Cards);

            Assert.Equal(drone.ObjectId, visible.Id);
            Assert.Equal(CardBack.Player, visible.Back);
            CardFaceDescriptor face = Assert.IsType<CardFaceDescriptor>(visible.Face);
            Assert.Equal("effective-drone", face.Id);
            Assert.Equal("Drone", face.Title);
            Assert.Equal(CardKind.Minion, face.Kind);
            Assert.Equal(["DRONE"], face.Traits);
            Assert.Equal(1, face.Fields["scheme"]);
            Assert.Equal(3, face.Fields["attack"]);
            Assert.Equal(2, face.Fields["health"]);
            Assert.Equal(1, face.Damage);
            Assert.Equal("1", face.PrintedStats["SCH"]);
            Assert.Equal("1", face.PrintedStats["ATK"]);
            Assert.Equal("1", face.PrintedStats["HP"]);
            Assert.DoesNotContain("Unique", face.PrintedStats.Keys);
            Assert.Equal(new CardPrintedValue("1", false, false, 0), face.PrintedValues["ATK"]);
            Assert.Equal(["ATK", "HP", "SCH"], face.PrintedValues.Keys.Order().ToArray());
            Assert.Null(face.ArtFaceId);
            Assert.Empty(face.RulesText);
        });
    }

    [Fact]
    public void ConcealedPileCardsDoNotExposeMutableState()
    {
        var board = Board();
        Card card = board.AreaOf(DeckType.VillainArea).Cards[0];
        card.Exhaust();
        World.MoveToTop(card, board.AreaOf(DeckType.EncounterDeck));
        ViewScope scope = new RestrictedVisibilityPolicy(0).Authorize(null, board.Players);

        CardDescriptor hidden = Assert.Single(
            WorldProjection.For(board, null, [], scope).World.Areas
                .Single(area => area.Zone == nameof(DeckType.EncounterDeck)).Cards,
            candidate => candidate.Back == CardBack.Encounter);

        Assert.Null(hidden.Id);
        Assert.Null(hidden.Face);
        Assert.True(hidden.Ready);
        Assert.Equal(-1, hidden.Host);
        Assert.False(hidden.FaceUp);
    }

    [Fact]
    public void ReadableFaceFactsTravelTogetherAndAConcealedDeckGetsNoneOfThem()
    {
        var board = Board();
        Card villain = board.AreaOf(DeckType.VillainArea).Cards[0];
        villain.TakeDamage(2);
        villain.PlaceTokens("c_test", 3);
        board.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Traits.Granted + "AERIAL",
            Affects: villain.ObjectId));
        ViewScope scope = new RestrictedVisibilityPolicy(0).Authorize(null, board.Players);

        WorldDescriptor visible = WorldProjection.For(board, null, [], scope).World;
        CardFaceDescriptor face = Assert.IsType<CardFaceDescriptor>(
            Card(visible, villain.ObjectId).Face);
        CardDescriptor hidden = visible.Areas
            .Single(area => area.Zone == nameof(DeckType.EncounterDeck)).Cards[0];

        Assert.Equal(["BRUTE", "AERIAL"], face.Traits);
        Assert.Equal("4", face.PrintedStats["SCH"]);
        Assert.Equal("Encounter", face.PrintedStats["Class"]);
        Assert.Equal("1", face.PrintedStats["Unique"]);
        Assert.Null(face.Cost);
        Assert.Equal(["Guard"], face.Keywords);
        Assert.Equal("Guard.", face.RulesText);
        Assert.Equal("<b>Guard</b>.", face.RulesMarkup);
        Assert.Equal("public-villain", face.ArtFaceId);
        Assert.Equal(2, face.Damage);
        Assert.Equal(3, face.Counters["test"]);
        Assert.Null(hidden.Face);
    }

    [Fact]
    public void AnAllyInPlayProjectsItsRemainingHealth()
    {
        var board = Board();
        Card ally = Assert.Single(board.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(1)).Cards);
        ally.TakeDamage(1);

        CardFaceDescriptor face = Assert.IsType<CardFaceDescriptor>(
            Card(WorldProjection.For(
                board, null, [], new PermissiveVisibilityPolicy().Authorize(null, board.Players)).World,
                ally.ObjectId).Face);

        Assert.Equal(2, face.Fields["health"]);
    }

    [Fact]
    public void EngineRuleInsertIsNotAVisibleGameComponent()
    {
        var board = Board();
        Card insert = board.CreateCard("rule-insert", board.AreaOf(DeckType.RemovedArea));

        WorldDescriptor visible = WorldProjection.For(
            board, null, [], new PermissiveVisibilityPolicy().Authorize(null, board.Players)).World;

        Assert.DoesNotContain(
            visible.Areas.SelectMany(area => area.Cards.Concat(area.Removed)),
            card => card.Id == insert.ObjectId);
    }

    [Fact]
    public void EveryCardFieldIsEitherRedactedOrExplicitlyPublic()
    {
        string[] publicWhileHidden = ["Back", "FaceUp", "Ready", "Host"];
        string[] redacted = ["Id", "Face"];
        string[] readableOnly = ["Location", "State"];
        string[] declared = typeof(CardDescriptor).GetProperties()
            .Where(property => property.GetMethod?.IsPublic == true)
            .Select(property => property.Name)
            .Order()
            .ToArray();

        Assert.Equal(
            publicWhileHidden.Concat(redacted).Concat(readableOnly).Order().ToArray(),
            declared);
    }

}
