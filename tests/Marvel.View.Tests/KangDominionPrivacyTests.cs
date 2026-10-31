using Marvel.Content;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class KangDominionPrivacyTests
{
    [Fact]
    public void ActualDominionStorageHidesPhysicalIdentityAndEventsFromEveryViewerUntilReveal()
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 2, 42);
        for (int seat = 0; seat < 2; seat++)
        {
            var player = world.CreateSeat($"p{seat}");
            player.IdentityCard = world.CreateCard(seat == 0 ? "01001a" : "01019a", player.Hero);
        }
        var host = world.CreateCard("11013a,11013b", world.AreaOf(DeckType.MainSchemesDeck));
        var card = world.CreateCard("11023", world.AreaOf(DeckType.AsideDeck));
        var from = card.Area;
        var storage = world.AreaOf(DeckType.BeneathCardsPile, PlayArea.Villains, host.ObjectId);
        World.MoveToTop(card, storage);
        List<GameEvent> events = [
            new CardsMoved(Places.Reference(from), Places.Reference(storage), [new Landing(card.ObjectId, 0)]),
            new CardAttached(card.ObjectId, host.ObjectId),
        ];
        ViewScope[] scopes = [ViewScope.None, new PermissiveVisibilityPolicy().Authorize(null, 2),
            new RestrictedVisibilityPolicy(0).Authorize(null, 2), new RestrictedVisibilityPolicy(1).Authorize(null, 2)];
        foreach (var scope in scopes)
        {
            var result = WorldProjection.For(world, null, events, scope);
            var hidden = Assert.Single(result.World.Areas.Single(area => area.Id == storage.Id).Cards);
            Assert.Null(hidden.Id);
            Assert.Null(hidden.Face);
            Assert.Equal(-1, hidden.Host);
            Assert.Equal(CardBack.Encounter, hidden.Back);
            Assert.DoesNotContain(result.Events.OfType<CardAttached>(), item => item.Card == card.ObjectId);
            Assert.DoesNotContain(result.Events.OfType<CardsMoved>().SelectMany(item => item.Cards), landing => landing.Card == card.ObjectId);
        }
        events.Clear();
        EncounterRevealExposure.Expose(world, card, "11013a", events);
        World.MoveToTop(card, world.AreaOf(DeckType.SideSchemesArea));
        foreach (var scope in scopes)
        {
            var result = WorldProjection.For(world, null, events, scope);
            var visible = Assert.Single(result.World.Areas.SelectMany(area => area.Cards), item => item.Id == card.ObjectId);
            Assert.Equal("11023", visible.Face!.Id);
            Assert.Single(result.Events.OfType<CardsFlipped>(), item => item.Cards.Contains(card.ObjectId));
        }
    }
}
