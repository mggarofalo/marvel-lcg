using System.Text.Json.Nodes;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;
using static Marvel.Content.Tests.Play.SpellMasteryFixture;

namespace Marvel.Content.Tests.Play;

/// <summary>Exact proposed Crimson row with an explicitly synthetic damage prohibition.</summary>
internal static class CrimsonIndependentDiscardFixture
{
    internal const string EnemySentence = """
        {"chooseCard":{"from":{"query":"enemies"},"effect":{"seq":[
          {"giveStatus":{"card":"chosen","status":"stunned"}},
          {"dealDamage":{"cards":"chosen","amount":7}}]}}}
        """;

    internal static AbilityRunner Runner(bool blocked, string? special = null)
    {
        string effect = special ?? $$"""{"seq":[{{EnemySentence}},{"discardInvocation":"this"}]}""";
        var replacements = new JsonArray(JsonNode.Parse($$"""
            {"card":"09032","abilities":[{"trigger":{"event":"WhenActionTriggered",
             "timing":"Special","subject":"this"},"effect":{{effect}}}]}
            """));
        if (blocked) replacements.Add(JsonNode.Parse("""
            {"card":"01134","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
             "effect":{"preventDamageWhile":{"card":"this","condition":{"exists":{"query":"villain"}}}}}]}
            """));
        return DoctorStrangeAbilityFixture.WithReplacements(replacements);
    }

    internal static List<GameEvent> Initiate(World world, Game game, InvocationDeck deck, bool master)
    {
        var hero = world.Seats[1].IdentityCard;
        var offer = master ? Assert.Single(game.Pending!.Affordances, item =>
            item.AnchorId == world.Cards.Single(card => card.FaceId == "09005").ObjectId)
            : Mastery(game, hero);
        Assert.Equal(master ? "3" : "2", Assert.Single(offer.CostOptions).Cost);
        int[] sources = world.Seats[1].Hand.Cards.Where(card => card.FaceId == "01088").Take(master ? 2 : 1)
            .Select(card => card.ObjectId).ToArray();
        var allocations = ResourcePayment.Allocate(Assert.Single(offer.CostOptions), sources)!;
        var result = game.Resolve(new Decision(offer.Id, [deck.PublicTop!.ObjectId], sources, Allocations: allocations));
        Assert.All(sources, id => Assert.Equal(DeckType.DiscardPile,
            world.Cards.Single(card => card.ObjectId == id).Area.Type));
        Assert.Equal(master, hero.Ready);
        return result.Events.ToList();
    }

    internal static void AssertLifecycle(World world, Game game, InvocationDeck deck, Card original,
        List<GameEvent> events, bool master, bool last)
    {
        int discarded = events.FindIndex(item => item is CardsMoved move && move.Verb == "Discard"
            && move.Cards.Any(card => card.Card == original.ObjectId));
        Assert.True(discarded >= 0);
        int reveal = events.FindIndex(discarded + 1, item => item is CardsFlipped flip && flip.FaceUp);
        Assert.True(reveal > discarded);
        Assert.Equal(last ? 1 : 0, events.OfType<AreaReordered>().Count());
        if (master)
        {
            int returned = events.FindIndex(item => item is CardsMoved move && move.Verb == "Return"
                && move.Cards.Any(card => card.Card == original.ObjectId));
            Assert.True(returned > reveal);
            Assert.Same(original, deck.PublicTop);
        }
        else if (!last) Assert.Same(deck.Discard, original.Area);
        Assert.Equal(5, deck.Deck.Cards.Count + deck.Discard.Cards.Count);
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
