using Marvel.Content;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Timing;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class HostedInvocationPrivacyTests
{
    [Fact]
    [Rule("rr:in-play-and-out-of-play.10")]
    public void FacedownInvocationAndShuffledOrderAreHiddenEvenFromItsOwner()
    {
        // "Facedown cards attached to in-play cards are out of play."
        // Concealed piles are unreadable for every visibility policy (engine choice).
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 2, 5489);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 1 ? "09001b,09001a" : "01001b,01001a", seat.Hero);
            for (int i = 0; i < 10; i++) world.CreateCard("01088", seat.Deck);
        }
        var aside = world.CreateArea(DeckType.AsideDeck, 1, PlayArea.Of(1));
        var cards = Enumerable.Range(32, 5).Select(id => world.CreateCard($"090{id}", aside)).ToArray();
        var deck = InvocationDeck.Initialize(world, 1, cards, "setup", []);
        var runner = new AbilityRunner(AbilityCatalog.Parse(File.ReadAllText(
            RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var game = Game.Begin(world, facts, runner);
        while (game.Phase == GamePhase.Mulligan)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        var scheme = world.CreateCard("09029", world.AreaOf(DeckType.RevealingArea, PlayArea.Of(0)));
        var top = deck.PublicTop!;
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 1, Subject: scheme.ObjectId, Seat: 0));
        Assert.Null(Sequence.Work(world, facts, runner, events));
        Assert.Equal([top], BeneathCards.Of(world, scheme));
        AssertPrivacy(world, deck, top, events);
        Threat.Remove(world, facts, world.ThreatAbilities,
            new(scheme, scheme.Tokens["k_threat"], "test", "Remove_Threat", 0)
                { RecordDefeatOn = new Occurrence(100, [], Player: 0) }, events);
        Assert.Null(Sequence.Work(world, facts, runner, events));
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.Same(deck.Deck, top.Area);
        Assert.False(world.Agenda.IsBusy);
        AssertPrivacy(world, deck, null, events);
    }

    private static void AssertPrivacy(World world, InvocationDeck deck, Card? stored, List<GameEvent> events)
    {
        foreach (var scope in new[] { ViewScope.None,
                     new RestrictedVisibilityPolicy(0).Authorize(null, 2),
                     new RestrictedVisibilityPolicy(1).Authorize(null, 2),
                     new PermissiveVisibilityPolicy().Authorize(null, 2) })
        {
            var visible = WorldProjection.For(world, null, events, scope);
            var hidden = deck.Deck.Cards.Where(card => card != deck.PublicTop)
                .Concat(stored is null ? [] : new[] { stored }).Select(card => card.ObjectId).ToHashSet();
            Assert.DoesNotContain(visible.World.Areas.SelectMany(area => area.Cards),
                card => card.Id is { } id && hidden.Contains(id));
            var publicTop = Assert.Single(visible.World.Areas.SelectMany(area => area.Cards),
                card => card.Id == deck.PublicTop!.ObjectId);
            Assert.Equal(deck.PublicTop!.FaceId, publicTop.Face!.Id);
            if (stored is not null)
            {
                var beneath = Assert.Single(visible.World.Areas, area => area.Id == stored.Area.Id);
                var concealed = Assert.Single(beneath.Cards);
                Assert.Null(concealed.Id);
                Assert.Null(concealed.Face);
                Assert.Null(concealed.Location);
                Assert.Null(concealed.State);
                Assert.Null(concealed.Persistent);
            }
            Assert.DoesNotContain(visible.Events.OfType<AreaReordered>(), changed => changed.Area == Places.Reference(deck.Deck));
            Assert.DoesNotContain(visible.Events.OfType<CardsMoved>().SelectMany(moved => moved.Cards), landing => hidden.Contains(landing.Card));
            var observed = events.OfType<CardsFlipped>().Where(flip => flip.FaceUp)
                .SelectMany(flip => flip.PublicFaces ?? new Dictionary<int, string>()).ToDictionary(face => face.Key, face => face.Value);
            Assert.All(visible.Events.OfType<CardsFlipped>(), flip =>
            {
                foreach (int card in flip.Cards.Where(hidden.Contains))
                {
                    Assert.True(flip.FaceUp);
                    Assert.Equal(observed[card], flip.SubjectFaces![card]);
                }
            });
            Assert.DoesNotContain(visible.Events.OfType<CardAttached>(), attached => hidden.Contains(attached.Card));
            Assert.DoesNotContain(visible.Events.OfType<CardDetached>(), detached => hidden.Contains(detached.Card));
            Assert.DoesNotContain(visible.Events.Where(happened => happened.SubjectFaces is not null)
                .SelectMany(happened => happened.SubjectFaces!.Keys), card => hidden.Contains(card) && !observed.ContainsKey(card));
        }
    }
}
