using System.Text.Json;
using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual Invocation rows on nonpublic boards, never ordinary event play.</summary>
public sealed class DoctorStrangeInvocationCardTests
{
    private static readonly string[] InvocationFaces = ["09033", "09035"];

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("ruling:8883bc6b0af1c9b8")]
    public void ActualVaporsWithoutStatusesStillDiscardsRevealsAndReturnsExactCopy(bool master, bool last)
    {
        // "even when there isn't a status card in play" permits independent self-discard.
        var (world, game, deck, _) = BoardActual("09035", last, master);
        var hero = world.Seats[1].IdentityCard;
        long random = world.Random.Generator.WordsConsumed;
        var original = deck.PublicTop!;
        var physical = deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order().ToArray();
        var offer = master ? Assert.Single(game.Pending!.Affordances, item =>
            item.AnchorId == world.Cards.Single(card => card.FaceId == "09005").ObjectId) : Mastery(game, hero);
        Assert.Equal(master ? "1" : "0", Assert.Single(offer.CostOptions).Cost);
        int[] sources = master ? [world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088").ObjectId] : [];
        var allocations = ResourcePayment.Allocate(Assert.Single(offer.CostOptions), sources)!;
        var result = game.Resolve(new Decision(offer.Id, [original.ObjectId], sources, Allocations: allocations));
        AssertLifecycle(game, deck, original, result.Events.ToList(), master, last, physical);
        Assert.DoesNotContain(result.Events, item => item.Verb == "Replace_Status");
        Assert.Equal(master, hero.Ready);
        Assert.Equal(last, world.Random.Generator.WordsConsumed > random);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void InvocationInventoryAddsExactlyTwoPrintedFacesWithoutCounters()
    {
        using var json = JsonDocument.Parse(AuthoredCards.Text);
        var rows = json.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(AuthoredCards.ExpectedFaceCount, rows.Length);
        Assert.Equal(InvocationFaces, rows.Skip(665).Take(2).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(665).Take(2), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.DoesNotContain("11001", AuthoredCards.Book.Authored);
    }

    private static void AssertLifecycle(Game game, InvocationDeck deck, Card original,
        List<GameEvent> events, bool master, bool last, int[] physical)
    {
        int discarded = events.FindIndex(item => item is CardsMoved move && move.Verb == "Discard"
            && move.Cards.Any(card => card.Card == original.ObjectId));
        int reveal = events.FindIndex(discarded + 1, item => item is CardsFlipped flip && flip.FaceUp);
        Assert.True(discarded >= 0);
        Assert.True(reveal > discarded);
        Assert.Equal(last ? 1 : 0, events.OfType<AreaReordered>().Count());
        AssertRevealedTop(events, deck, reveal, master, last);
        if (master)
        {
            int returned = events.FindIndex(item => item is CardsMoved move && move.Verb == "Return"
                && move.Cards.Any(card => card.Card == original.ObjectId));
            Assert.True(returned > reveal);
            Assert.Same(original, deck.PublicTop);
        }
        else if (!last) Assert.Same(deck.Discard, original.Area);
        Assert.Equal(physical, deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order());
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
        Assert.True(game.IsRootPrompt);
    }

    private static void AssertRevealedTop(List<GameEvent> events, InvocationDeck deck,
        int reveal, bool master, bool last)
    {
        int expectedReveal = last ? Assert.Single(events.OfType<AreaReordered>()).Order[^1]
            : master ? deck.Deck.Cards[^2].ObjectId : deck.PublicTop!.ObjectId;
        Assert.Equal(expectedReveal, Assert.Single(Assert.IsType<CardsFlipped>(events[reveal]).Cards));
    }
}
