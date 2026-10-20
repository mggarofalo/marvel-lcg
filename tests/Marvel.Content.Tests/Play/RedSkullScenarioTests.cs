using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RedSkullScenarioTests
{
    [Fact]
    public void SetupPlacesRedHouseAndSeparatesEveryOtherSideScheme()
    {
        var f = RedSkullFixture.Create();
        Assert.Equal("04125", f.Villain.FaceId);
        Assert.Equal("04128b", f.Main.FaceId);
        Assert.Equal("04139", Assert.Single(f.World.AreaOf(DeckType.SideSchemesArea).Cards).FaceId);
        Assert.NotEmpty(f.World.AreaOf(DeckType.SideSchemeDeck).Cards);
        Assert.All(f.World.AreaOf(DeckType.SideSchemeDeck).Cards,
            card => Assert.Equal(CardKind.EncounterSideScheme, f.World.Facts.Kind(card.FaceId)));
        Assert.DoesNotContain(f.World.AreaOf(DeckType.EncounterDeck).Cards,
            card => f.World.Facts.Kind(card.FaceId) == CardKind.EncounterSideScheme);
        Assert.Contains(f.World.Cards, card => card.FaceId == "04130" && card.Area.Type == DeckType.AsideDeck);
    }

    [Fact]
    public void VillainPhaseStepOneRevealsExactlyOneSideScheme()
    {
        var f = RedSkullFixture.Create();
        var deck = f.World.AreaOf(DeckType.SideSchemeDeck);
        var target = deck.Cards.Single(card => card.FaceId == "04143");
        World.MoveToTop(target, deck);
        int before = deck.Cards.Count;
        f.World.Agenda.Add(new PhaseStep(new ThreatStageOperation(ThreatStageKind.PlaceThreat), 1, 1));
        f.Finish();
        Assert.Equal(before - 1, deck.Cards.Count);
        Assert.Equal(DeckType.SideSchemesArea, target.Area.Type);
        Assert.Equal(2, target.Tokens["k_threat"]);
        Assert.Equal(2, StateFields.Modified(f.World, f.Villain, "attack", f.World.Facts, 1));
    }

    [Fact]
    public void NewWorldHydraFrontRevealsBeforeItsBackStartingThreat()
    {
        var f = RedSkullFixture.Create();
        var deck = f.World.AreaOf(DeckType.SideSchemeDeck);
        var target = deck.Cards.Single(card => card.FaceId == "04143");
        World.MoveToTop(target, deck);
        MainScheme.Advance(f.World, f.World.Facts, f.Runner, f.Main, "test", []);
        f.Finish();
        Assert.Equal("04129b", f.Main.FaceId);
        Assert.Equal(1, f.Main.Tokens["k_threat"]);
        Assert.Equal(DeckType.SideSchemesArea, target.Area.Type);
    }

    [Fact]
    public void RedHouseProtectsOnlyRedSkullWhileItsSourceRemainsInPlay()
    {
        var f = RedSkullFixture.Create();
        var house = f.World.AreaOf(DeckType.SideSchemesArea).Cards.Single(card => card.FaceId == "04139");
        var hero = f.World.Seats[0].IdentityCard;
        var minion = f.World.CreateCard("04131", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.False(f.Runner.CanTakeDamage(f.World, f.Villain, hero));
        Assert.True(f.Runner.CanTakeDamage(f.World, minion, hero));
        DamagePlacement.Resolve(f.World, f.World.Facts, new DamageRequest(hero, f.Villain, 3, "test", "Damage"), []);
        f.Finish();
        Assert.Equal(0, f.Villain.Damage);
        World.MoveToTop(house, f.World.AreaOf(DeckType.SideSchemeDiscardPile));
        Assert.True(f.Runner.CanTakeDamage(f.World, f.Villain, hero));
        DamagePlacement.Resolve(f.World, f.World.Facts, new DamageRequest(hero, f.Villain, 3, "test", "Damage"), []);
        f.Finish();
        Assert.Equal(3, f.Villain.Damage);
    }

    [Fact]
    public void MassChaosCountsDistinctResourcesForEachPlayersOwnFiveCards()
    {
        var f = RedSkullFixture.Create(players: 2);
        for (int player = 0; player < 2; player++)
        {
            var deck = f.World.Seats[player].Deck;
            foreach (var card in deck.Cards.ToArray()) World.MoveToTop(card, f.World.AreaOf(DeckType.RemovedArea));
            for (int copy = 0; copy < 6; copy++) f.World.CreateCard(player == 0 ? "01088" : "01089", deck);
        }
        var chaos = f.Reveal("04144");
        f.Finish();
        Assert.Equal(2, chaos.Tokens["k_threat"]);
        Assert.All(f.World.Seats, seat => Assert.Single(seat.Deck.Cards));
    }
}
