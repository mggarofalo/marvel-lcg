using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Nonpublic Invocation target admission with actual Images and synthetic controls.</summary>
public sealed class InvocationEmptyChoiceTests
{
    private const string Images = """
        {"seq":[
          {"giveStatus":{"card":{"query":"villain"},"status":"confused"}},
          {"chooseCard":{"from":{"query":"schemes"},"effect":{"removeThreat":{"scheme":"chosen","amount":4}}}},
          {"discardInvocation":"this"}
        ]}
        """;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("ruling:8c9c9d30f4c01189")]
    public void ImagesChoiceSelfDiscardsThroughBothArrowsWhenOtherEffectsCannotChangeState(bool master, bool last)
    {
        // "Each Invocation card always has a valid target" includes its own discard.
        // Test-local 09033 follows its printed confuse, remove four, discard instructions.
        // The genuine scheme choice has no eligible targets and must not ask for an answer.
        var (world, game, deck, _) = BoardWithSpecial(Images, last, "09033", master ? 1 : 0);
        var hero = world.Seats[1].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard(Statuses.Confused, world.AreaOf(DeckType.StatusArea, host: villain.ObjectId));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
        world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        Assert.All(world.Cards.Where(card => card.Area.Type is DeckType.MainSchemesArea or DeckType.SideSchemesArea),
            card => Assert.Equal(0, card.Tokens.GetValueOrDefault("k_threat")));
        var original = deck.PublicTop!;
        var next = last ? null : deck.Deck.Cards[^2];
        var physical = deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order().ToArray();
        long random = world.Random.Generator.WordsConsumed;
        var offer = master ? Assert.Single(game.Pending!.Affordances, item =>
            item.AnchorId == world.Cards.Single(card => card.FaceId == "09005").ObjectId) : Mastery(game, hero);
        int[] sources = [world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088").ObjectId];
        var allocations = ResourcePayment.Allocate(Assert.Single(offer.CostOptions), sources)!;

        var result = game.Resolve(new Decision(offer.Id, [original.ObjectId], sources, Allocations: allocations));

        Assert.Equal(1, Statuses.Count(world, villain, Statuses.Confused));
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
        var events = result.Events.ToList();
        AssertInvocationLifecycle(events, original, deck, next, master, last);
        Assert.Equal(last, world.Random.Generator.WordsConsumed > random);
        Assert.Equal(last ? 5 : master ? 5 : 4, deck.Deck.Cards.Count);
        Assert.Equal(master, hero.Ready);
        Assert.Single(events.OfType<CardsMoved>(), move => move.Verb == "Discard"
            && move.Cards.Any(card => sources.Contains(card.Card)));
        Assert.Equal(DeckType.DiscardPile, world.Cards.Single(card => card.ObjectId == sources[0]).Area.Type);
        Assert.Equal(master ? 1 : 0, events.OfType<CardsMoved>().Count(move => move.Verb == CardPlay.Verb));
        Assert.True(game.IsRootPrompt);
        Assert.Equal(physical, deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order());
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
        Assert.Empty(world.Agenda.Outstanding);
    }


    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ImagesChoosesAnEligibleSchemeBeforeSelfDiscardAndExactReturn(bool master, bool last)
    {
        var (world, game, deck, _) = BoardWithSpecial(Images, last, "09033", master ? 1 : 0);
        var hero = world.Seats[1].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        var side = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 6);
        var original = deck.PublicTop!;
        var next = deck.Deck.Cards.Count > 1 ? deck.Deck.Cards[^2] : null;
        var physical = deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order().ToArray();
        var offer = master ? Assert.Single(game.Pending!.Affordances, item =>
            item.AnchorId == world.Cards.Single(card => card.FaceId == "09005").ObjectId) : Mastery(game, hero);
        int[] sources = [world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088").ObjectId];
        var allocations = ResourcePayment.Allocate(Assert.Single(offer.CostOptions), sources)!;
        var first = game.Resolve(new Decision(offer.Id, [original.ObjectId], sources, Allocations: allocations));

        Assert.False(game.IsRootPrompt);
        Assert.Equal(1, game.Pending!.Player);
        Assert.Single(game.Pending.Affordances, item => item.Id == side.ObjectId);
        Assert.DoesNotContain(game.Pending.Affordances, item => item.Id == main.ObjectId);
        Assert.Same(original, deck.PublicTop);
        Assert.Equal(6, side.Tokens["k_threat"]);
        Assert.Equal(1, Statuses.Count(world, villain, Statuses.Confused));
        var second = game.Resolve(Decision.Take(side.ObjectId));
        var events = first.Events.Concat(second.Events).ToList();

        Assert.Equal(2, side.Tokens["k_threat"]);
        Assert.Equal(0, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(1, Statuses.Count(world, villain, Statuses.Confused));
        Assert.Single(events.OfType<CardsMoved>(), move => move.Verb == "Discard"
            && move.Cards.Any(card => sources.Contains(card.Card)));
        Assert.Single(events.OfType<CardsMoved>(), move => move.Verb == "Discard"
            && move.Cards.Any(card => card.Card == original.ObjectId));
        Assert.Equal(last ? 1 : 0, events.OfType<AreaReordered>().Count());
        if (master) Assert.Same(original, deck.PublicTop);
        else if (!last)
        {
            Assert.Same(next, deck.PublicTop);
            Assert.Same(deck.Discard, original.Area);
        }
        Assert.Equal(master, hero.Ready);
        Assert.Equal(master ? 1 : 0, events.OfType<CardsMoved>().Count(move => move.Verb == CardPlay.Verb));
        Assert.Equal(physical, deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order());
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }


    private static void AssertInvocationLifecycle(
        List<GameEvent> events, Card original, InvocationDeck deck, Card? next, bool master, bool last)
    {
        int discarded = events.FindIndex(happened => happened is CardsMoved move && move.Verb == "Discard"
            && move.Cards.Any(card => card.Card == original.ObjectId));
        int revealed = events.FindIndex(discarded + 1, happened => happened is CardsFlipped flip && flip.Verb == "Reveal");
        Assert.True(discarded >= 0);
        Assert.True(revealed > discarded);
        Assert.Equal(last ? 1 : 0, events.OfType<AreaReordered>().Count());
        if (master)
        {
            int returned = events.FindIndex(happened => happened is CardsMoved move && move.Verb == "Return"
                && move.Cards.Any(card => card.Card == original.ObjectId));
            Assert.True(returned > revealed);
            Assert.Same(original, deck.PublicTop);
        }
        else if (!last)
        {
            Assert.Same(deck.Discard, original.Area);
            Assert.Same(next, deck.PublicTop);
        }
    }

    [Fact]
    [Rule("rr:target.2")]
    public void IneffectiveOrdinarySequenceCannotInitiateFromAnEmptyChoice()
    {
        // "can only be initiated if it has at least one valid target."
        const string body = """
            {"seq":[{"giveStatus":{"card":{"query":"villain"},"status":"confused"}},
              {"chooseCard":{"from":{"query":"schemes"},"effect":{"removeThreat":{"scheme":"chosen","amount":4}}}}]}
            """;
        var runner = ActionAbilitySupportFixtures.Runner("01007", "Action", body, cost: """{"exhaust":"this"}""");
        Card? source = null;
        var (game, world) = ActionAbilitySupportFixtures6.Playing(board =>
        {
            source = ActionAbilitySupportFixtures.InPlay(board, "01007");
            var villain = board.TheCardIn(DeckType.VillainArea)!;
            board.CreateCard(Statuses.Confused, board.AreaOf(DeckType.StatusArea, host: villain.ObjectId));
            var main = board.TheCardIn(DeckType.MainSchemesArea)!;
            main.PlaceTokens("k_threat", -main.Tokens.GetValueOrDefault("k_threat"));
        }, hero: true, abilities: runner);
        string before = Snapshot(world);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(before, Snapshot(world));
    }

    [Fact]
    [Rule("rr:then.1")]
    public void EmptyIndependentChoiceDoesNotFabricateFullCompletionForThen()
    {
        // "must successfully resolve in full before" the dependent effect resolves.
        const string special = """
            {"then":{"effect":{"seq":[
              {"giveStatus":{"card":{"query":"villain"},"status":"confused"}},
              {"chooseCard":{"from":{"query":"schemes"},"effect":{"removeThreat":{"scheme":"chosen","amount":4}}}}
            ]},"then":{"draw":{"player":"you","count":1}}}}
            """;
        var (world, _, deck, runner) = BoardWithSpecial(special, false);
        int hand = world.Seats[1].Hand.Cards.Count;
        var result = runner.ExecuteSpecial(world, deck.PublicTop!, 1, finalStep: false);
        Assert.Equal(1, Statuses.Count(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Confused));
        Assert.Equal(hand, world.Seats[1].Hand.Cards.Count);
        Assert.DoesNotContain(result.Events.OfType<CardsMoved>(), move => move.Verb == "Draw");
        Assert.Empty(world.Agenda.Outstanding);
    }
}
