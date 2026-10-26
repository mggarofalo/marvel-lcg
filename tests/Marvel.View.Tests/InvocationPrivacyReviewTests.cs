using Marvel.Content;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

/// <summary>Existing projection privacy at the nonpublic Invocation lifecycle boundary.</summary>
public sealed class InvocationPrivacyReviewTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void MulliganRevealDiscardAndRecycleNeverPublishDeeperIdentitiesOrShuffleOrder(int owner)
    {
        // pack:mc08:the-invocation-deck: "top card ... faceup at all times".
        // A synthetic research board exercises existing View filtering, not public admission.
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 2, 5489);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == owner ? "09001b,09001a" : "01001b,01001a", seat.Hero);
            for (int i = 0; i < 10; i++) world.CreateCard("01088", seat.Deck);
        }
        var source = world.CreateArea(DeckType.AsideDeck, owner, PlayArea.Of(owner));
        var cards = Enumerable.Range(32, 5)
            .Select(number => world.CreateCard($"090{number}", source)).ToArray();
        var events = new List<GameEvent>();
        var invocation = InvocationDeck.Initialize(world, owner, cards, "setup", events);
        var game = Game.Begin(world, facts, new NoCardAbilities());
        AssertPrivacy(world, invocation, events, publicCount: 0);
        var first = game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        Assert.Equal(GamePhase.Mulligan, game.Phase);
        AssertPrivacy(world, invocation, first.Events, publicCount: 0);
        var second = game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        AssertPrivacy(world, invocation, second.Events, publicCount: 1);

        for (int i = 0; i < 5; i++)
        {
            events.Clear();
            invocation.DiscardTop(invocation.PublicTop!, "discard", events);
            AssertPrivacy(world, invocation, events, publicCount: i == 4 ? 1 : i + 2);
        }
    }

    private static void AssertPrivacy(
        World world, InvocationDeck invocation, IReadOnlyList<GameEvent> events, int publicCount)
    {
        foreach (var scope in new[] { ViewScope.None,
                     new RestrictedVisibilityPolicy(0).Authorize(null, 2),
                     new RestrictedVisibilityPolicy(1).Authorize(null, 2),
                     new PermissiveVisibilityPolicy().Authorize(null, 2) })
        {
            var before = world.Digest().Canonical();
            long words = world.Random.Generator.WordsConsumed;
            var visible = WorldProjection.For(world, null, events, scope);
            var relationship = Assert.Single(visible.World.InvocationDecks);
            Assert.Equal(new InvocationDeckDescriptor(invocation.Player,
                invocation.Deck.Id, invocation.Discard.Id), relationship);
            var board = BoardPresentation.From(visible.World);
            Assert.Equal("INVOCATION DECK", Assert.Single(board.Areas,
                area => area.Id == relationship.Deck).Title);
            Assert.Equal("INVOCATION DISCARD", Assert.Single(board.Areas,
                area => area.Id == relationship.Discard).Title);
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(words, world.Random.Generator.WordsConsumed);
            var deck = Assert.Single(visible.World.Areas, area => area.Id == invocation.Deck.Id);
            var discard = Assert.Single(visible.World.Areas, area => area.Id == invocation.Discard.Id);
            Assert.Equal(publicCount, deck.Cards.Count(card => card.Face is not null) + discard.Cards.Count);
            var top = invocation.PublicTop;
            Assert.All(deck.Cards, card =>
            {
                if (top is not null && card.Id == top.ObjectId)
                    Assert.Equal(top.FaceId, card.Face!.Id);
                else
                {
                    Assert.Null(card.Id);
                    Assert.Null(card.Face);
                }
            });
            Assert.DoesNotContain(visible.Events, happened => happened is AreaReordered);
            var hidden = invocation.Deck.Cards.Where(card => !ReferenceEquals(card, top))
                .Select(card => card.ObjectId).ToHashSet();
            Assert.DoesNotContain(visible.Events.OfType<CardsMoved>().SelectMany(moved => moved.Cards),
                landing => hidden.Contains(landing.Card));
            Assert.DoesNotContain(visible.Events.OfType<CardsFlipped>().SelectMany(flipped => flipped.Cards),
                hidden.Contains);
            Assert.DoesNotContain(visible.Events.Where(happened => happened.SubjectFaces is not null)
                .SelectMany(happened => happened.SubjectFaces!.Keys), hidden.Contains);
        }
    }
}
