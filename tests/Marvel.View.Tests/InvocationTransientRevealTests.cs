using System.Text.Json;
using Marvel.Content;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class InvocationTransientRevealTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("ruling:b07d53e12625ab23")]
    public void PublicRevealRetainsExactOccurrenceFaceWithoutExposingConcealedPileOrLaterOrder(int owner)
    {
        // "At that point in time, you'd reveal the new top card of the Invocation deck."
        // Synthetic lifecycle isolates projection; the public seed-12 host test proves reachability.
        var (world, game, deck) = Create(owner);
        var (control, controlGame, controlDeck) = Create(owner);
        var top = deck.PublicTop!;
        var next = deck.Deck.Cards[^2];
        var events = new List<GameEvent>();
        deck.DiscardTop(top, "test", events);
        deck.ReturnToTop(top, "test", events);
        controlDeck.DiscardTop(controlDeck.PublicTop!, "test", []);
        controlDeck.ReturnToTop(control.Cards[top.ObjectId], "test", []);
        string digest = world.Digest().Canonical();
        string random = JsonSerializer.Serialize(world.Random.Generator.GetState());
        long words = world.Random.Generator.WordsConsumed;
        var outstanding = world.Agenda.Outstanding.ToArray();
        var current = world.Agenda.Current;
        string prompt = JsonSerializer.Serialize(game.Pending);
        foreach (var scope in new[] { ViewScope.None,
                     new RestrictedVisibilityPolicy(owner).Authorize(null, 2),
                     new RestrictedVisibilityPolicy(1 - owner).Authorize(null, 2),
                     new PermissiveVisibilityPolicy().Authorize(null, 2) })
        {
            var visible = WorldProjection.For(world, game.Pending, events, scope);
            var flip = Assert.Single(visible.Events.OfType<CardsFlipped>(),
                flip => flip.FaceUp && flip.Cards.Contains(next.ObjectId));
            Assert.Equal([next.ObjectId], flip.Cards);
            Assert.Equal(next.FaceId, Assert.Single(flip.SubjectFaces!).Value);
            Assert.Null(flip.PublicFaces);
            var reveal = Assert.Single(EventPresenter.Present([flip], visible.World));
            Assert.Equal(next.FaceId, Assert.Single(reveal.Activity!.Subjects).Reference!.FaceId);
            Assert.Contains(world.Facts.Title(next.FaceId), reveal.Summary);
            Assert.True(visible.World.EventFaces.ContainsKey(next.FaceId));
            var concealed = deck.Deck.Cards.Where(card => card != top).Select(card => card.ObjectId).ToHashSet();
            Assert.DoesNotContain(visible.World.Areas.SelectMany(area => area.Cards),
                card => card.Id is { } id && concealed.Contains(id));
            Assert.DoesNotContain(visible.Events.OfType<CardsMoved>().SelectMany(move => move.Cards),
                card => concealed.Contains(card.Card));
            Assert.Empty(visible.Events.OfType<AreaReordered>());
            Assert.All(visible.World.EventFaces.Keys, face => Assert.Contains(face, new[] { top.FaceId, next.FaceId }));
            Assert.DoesNotContain(visible.Events.OfType<CardsFlipped>(), flip => !flip.FaceUp && flip.Cards.Contains(next.ObjectId));
            var history = ActionHistoryPresenter.PresentEntry(Action(), events, visible.World);
            Assert.Contains(world.Facts.Title(top.FaceId), history.Details[0]);
            Assert.Contains(world.Facts.Title(next.FaceId), history.Details[1]);
            Assert.Contains("face up", history.Details[1]);
        }
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random, JsonSerializer.Serialize(world.Random.Generator.GetState()));
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(outstanding, world.Agenda.Outstanding);
        Assert.Equal(current, world.Agenda.Current);
        Assert.Equal(prompt, JsonSerializer.Serialize(game.Pending));
        var decision = Decision.Take(Assert.Single(game.Pending!.Affordances, offer => offer.Verb == Game.ChangeForm).Id);
        Assert.Equal(JsonSerializer.Serialize(controlGame.Resolve(decision).Events, EventJson.Options),
            JsonSerializer.Serialize(game.Resolve(decision).Events, EventJson.Options));
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(JsonSerializer.Serialize(control.Random.Generator.GetState()),
            JsonSerializer.Serialize(world.Random.Generator.GetState()));
    }

    [Fact]
    public void PrivateFaceMetadataAndUnrelatedOrFacedownGrantsCannotAuthorizeAReveal()
    {
        var (world, _, deck) = Create(1);
        Card hidden = deck.Deck.Cards[0];
        Card hand = world.Seats[1].Hand.Cards[0];
        var privateFlip = new CardsFlipped([hand.ObjectId], true) { SubjectFaces = OccurrenceFaces.For(hand) };
        var unrelated = new CardsFlipped([hidden.ObjectId], true)
        {
            SubjectFaces = new Dictionary<int, string> { [hidden.ObjectId] = hidden.FaceId },
            PublicFaces = new Dictionary<int, string> { [hand.ObjectId] = hand.FaceId },
        };
        var facedown = new CardsFlipped([hidden.ObjectId], false)
        { PublicFaces = new Dictionary<int, string> { [hidden.ObjectId] = hidden.FaceId } };
        foreach (var scope in new[] { ViewScope.None, new RestrictedVisibilityPolicy(0).Authorize(null, 2) })
        {
            var visible = WorldProjection.For(world, null, [privateFlip, unrelated, facedown], scope);
            Assert.Empty(visible.Events);
            Assert.Empty(visible.World.EventFaces);
            Assert.Empty(ActionHistoryPresenter.PresentEntry(Action(), [privateFlip, unrelated, facedown], visible.World).Details);
        }
        Assert.Single(WorldProjection.For(world, null, [privateFlip],
            new RestrictedVisibilityPolicy(1).Authorize(null, 2)).Events);
    }

    [Fact]
    public void PublicAuthorityIsEngineOnlyAndDoesNotChangeEventJson()
    {
        var (_, _, deck) = Create(1);
        var events = new List<GameEvent>();
        deck.DiscardTop(deck.PublicTop!, "test", events);
        var revealed = Assert.Single(events.OfType<CardsFlipped>(), flip => flip.FaceUp);
        Assert.Equal(Assert.Single(revealed.SubjectFaces!), Assert.Single(revealed.PublicFaces!));
        string json = JsonSerializer.Serialize<GameEvent>(revealed, EventJson.Options);
        Assert.Equal(JsonSerializer.Serialize<GameEvent>(revealed with { PublicFaces = null }, EventJson.Options), json);
        Assert.Null(Assert.IsType<CardsFlipped>(JsonSerializer.Deserialize<GameEvent>(json, EventJson.Options)).PublicFaces);
    }

    private static (World World, Game Game, InvocationDeck Deck) Create(int owner)
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 2, 5489);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == owner ? "09001b,09001a" : "01001b,01001a", seat.Hero);
            for (int i = 0; i < 10; i++) world.CreateCard("01088", seat.Deck);
        }
        var source = world.CreateArea(DeckType.AsideDeck, owner, PlayArea.Of(owner));
        var cards = Enumerable.Range(32, 5).Select(number => world.CreateCard($"090{number}", source)).ToArray();
        var deck = InvocationDeck.Initialize(world, owner, cards, "setup", []);
        var game = Game.Begin(world, facts, new NoCardAbilities());
        while (game.Phase == GamePhase.Mulligan)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        return (world, game, deck);
    }

    private static ActionHistoryFacts Action() => new(0, "Player", "action", "PlayerTurn",
        Game.ActionVerb, "Invocation", null, [], []);
}
