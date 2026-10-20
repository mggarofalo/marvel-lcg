using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DiscardCountAdmissionTests
{
    [Rule("rr:initiating-abilities.step.3")]
    [Rule("rr:cost.5")]
    [Theory]
    [InlineData("any", 2, 2, false)]
    [InlineData("any", 3, 2, true)]
    [InlineData("upTo", 2, 2, false)]
    [InlineData("upTo", 3, 2, true)]
    [InlineData("exact", 2, 2, false)]
    [InlineData("exact", 3, 2, true)]
    [InlineData("any", 4, 3, true)]
    [InlineData("upTo", 4, 3, false)]
    [InlineData("exact", 4, 3, false)]
    public void AnAdmissibleDiscardReceiptMustLeaveIndependentEventResources(string range, int held, int minimum, bool expected)
    {
        // Costs "must be paid simultaneously". The synthetic effect requires
        // two physical discards to deal positive damage, plus the event's resource.
        var runner = Runner("01004", "Action",
            """{"dealDamage":{"cards":{"query":"villain"},"amount":{"add":[{"cardsDiscarded":"game"},OFFSET]}}}"""
                .Replace("OFFSET", (1 - minimum).ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal),
            cost: Cost(range));
        Card? source = null;
        var retained = new List<Card>();
        var (_, world) = Playing(board =>
        {
            foreach (var card in board.Seats[0].Hand.Cards.ToArray())
                World.MoveToTop(card, board.Seats[0].Deck);
            source = board.CreateCard("01004", board.Seats[0].Hand);
            for (int index = 0; index < held; index++)
                retained.Add(board.CreateCard("01088", board.Seats[0].Hand));
        }, hero: true, abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.Where(card => card != source && !retained.Contains(card)).ToArray())
            World.MoveToTop(card, world.Seats[0].Deck);
        Assert.Equal(expected, runner.Actions(world, 0).Any(action => action.Card == source!.ObjectId));
    }

    [Rule("rr:initiating-abilities.step.3")]
    [Fact]
    public void SpecificReceiptRejectsZeroEffectBeforeCostsAndTwoCardReceiptResolves()
    {
        var runner = Runner("01004", "Action",
            """{"dealDamage":{"cards":{"query":"villain"},"amount":{"add":[{"cardsDiscarded":"game"},-1]}}}""",
            cost: Cost("any"));
        Card? source = null;
        Card[] held = [];
        var (game, world) = Playing(board =>
        {
            Hand(board, "01088", 0);
            source = board.CreateCard("01004", board.Seats[0].Hand);
            held = [.. Enumerable.Range(0, 5).Select(_ => board.CreateCard("01088", board.Seats[0].Hand))];
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        int[] paying = [held[0].ObjectId];
        var allocation = ResourcePayment.Allocate(Assert.Single(action.Costs!), paying);
        string before = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        Assert.Throws<RulesNotImplementedException>(() => game.Resolve(
            new Decision(action.Id, [held[1].ObjectId], paying, Allocations: allocation)));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        game.Resolve(new Decision(action.Id, [held[1].ObjectId, held[2].ObjectId], paying, Allocations: allocation));
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.All(held.Take(3), card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.All(held.Skip(3), card => Assert.Same(world.Seats[0].Hand, card.Area));
    }

    private static string Cost(string range) => range switch
    {
        "any" => """{"discardAnyFromHand":"yourHand"}""",
        "upTo" => """{"discardUpToFromHand":2}""",
        "exact" => """{"discardFromHand":2}""",
        _ => throw new ArgumentException("Unknown fixture range", nameof(range)),
    };

    [Fact]
    public void VariablePhysicalReceiptQueriesRaiseWithoutMutatingInsteadOfApproximating()
    {
        var runner = Runner("04011", "Action",
            """{"placeCounters":{"card":"you","counter":"seen","count":{"printedResourceIconsDiscarded":"game"}}}""",
            cost: Cost("any"));
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "04011");
        Hand(world, "01088", 3);
        string before = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Contains("physical receipt", error.Message);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        Assert.True(source.Ready);
    }
}
