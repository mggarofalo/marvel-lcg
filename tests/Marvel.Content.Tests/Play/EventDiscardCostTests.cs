using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EventDiscardCostTests
{
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AnEventIsNotOfferedAsItsOwnHandDiscardCost()
    {
        // Synthetic event interpretation for the unadmitted Shield Toss cost
        // shape. Costs must be payable "without paying any costs" on failure.
        var (game, _, _, source, _) = Board("01003", 1);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source.ObjectId);
        Assert.DoesNotContain(source.ObjectId, action.Targets!.Legal);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void SelfDiscardRejectsBeforeTheEventLeavesHand()
    {
        // "If ... the cost(s) cannot be paid, abort this process without paying any costs."
        var (_, world, runner, source, _) = Board("01003", 1);
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        AssertRejected(world, () =>
            runner.ExecuteAction(world, new PlayerAction(action, [], [source.ObjectId])));
        Assert.Same(world.Seats[0].Hand, source.Area);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:cost.5")]
    [Fact]
    public void OneHandCardCannotPayPrintedResourcesAndArrowDiscard()
    {
        // "Those costs must be paid simultaneously." Discarding one physical
        // card as a resource cannot also pay a separate hand-discard cost.
        var (game, world, runner, source, held) = Board("01004", 2);
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        var offered = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source.ObjectId);
        var allocation = ResourcePayment.Allocate(Assert.Single(offered.Costs!), [held[0].ObjectId]);
        AssertRejected(world, () =>
            runner.ExecuteAction(world, new PlayerAction(action, [held[0].ObjectId], [held[0].ObjectId], Allocations: allocation)));
        Assert.Same(world.Seats[0].Hand, source.Area);
        Assert.Same(world.Seats[0].Hand, held[0].Area);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void DistinctCardsPayPrintedResourcesAndArrowDiscardOnce()
    {
        // Step5 pays costs; the event effect resolves only after both departures.
        var (game, world, _, source, held) = Board("01004", 2);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source.ObjectId);
        var allocation = ResourcePayment.Allocate(Assert.Single(action.Costs!), [held[0].ObjectId]);
        game.Resolve(new Decision(action.Id, [held[1].ObjectId], [held[0].ObjectId], Allocations: allocation));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, held[0].Area.Type);
        Assert.Equal(DeckType.DiscardPile, held[1].Area.Type);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_test"]);
    }

    [Fact]
    public void AnEventAloneInHandCannotPayItsDiscardCost()
    {
        var (_, world, runner, source, _) = Board("01003", 0);
        foreach (var card in world.Seats[0].Hand.Cards.Where(card => card != source).ToArray())
            World.MoveToTop(card, world.Seats[0].Deck);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
    }

    [Rule("rr:cost.5")]
    [Theory]
    [InlineData(1, 1, false)]
    [InlineData(1, 2, true)]
    [InlineData(2, 2, false)]
    [InlineData(2, 3, true)]
    public void OffersRequireAJointlyPayableResourceAndDiscardSelection(int discarded, int heldCount, bool offered)
    {
        // Multiple costs "must be paid simultaneously"; independently affordable
        // resource and discard costs do not establish a joint payment.
        var (_, world, runner, source, held) = Board("01004", heldCount, discarded);
        foreach (var card in world.Seats[0].Hand.Cards.Where(card => card != source && !held.Contains(card)).ToArray())
            World.MoveToTop(card, world.Seats[0].Deck);
        Assert.Equal(offered, runner.Actions(world, 0).Any(action => action.Card == source.ObjectId));
    }

    [Rule("rr:cost.5")]
    [Rule("rr:resource.4")]
    [Theory]
    [InlineData("01088", true)]
    [InlineData("01089", false)]
    public void JointPaymentPreservesTheRequiredResourceInsteadOfDiscardingTheFirstCard(string resource, bool offered)
    {
        // Specific resource types must be generated "in order to pay the cost."
        // The first candidate generates two icons and must remain available.
        var runner = Runner("01004", "Action",
            """{"placeCounters":{"card":"you","counter":"test","count":1}}""",
            cost: """{"seq":[{"discardFromHand":1},{"spend":"Y"}]}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        Hand(world, resource, 1);
        world.CreateCard("01003", world.Seats[0].Hand);
        var source = world.CreateCard("01004", world.Seats[0].Hand);
        Assert.Equal(offered, runner.Actions(world, 0).Any(action => action.Card == source.ObjectId));
    }

    [Fact]
    public void RepeatedCostQueriesPreserveStateAndSubsequentLegalPlay()
    {
        var (game, world, runner, source, held) = Board("01004", 2);
        var (controlGame, control, _, controlSource, controlHeld) = Board("01004", 2);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var effects = world.Effects.Registered.ToArray();
        var pending = world.Agenda.Outstanding.ToArray();
        var prompt = game.Pending;
        for (int query = 0; query < 3; query++)
        {
            var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
            Assert.DoesNotContain(source.ObjectId, runner.Describe(world, action).Targets!.Legal);
        }
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Same(prompt, game.Pending);

        Assert.Equal(Play(controlGame, controlSource, controlHeld), Play(game, source, held));
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(control.Effects.Registered, world.Effects.Registered);
        Assert.Equal(control.Agenda.Outstanding.Select(step => step.What), world.Agenda.Outstanding.Select(step => step.What));
    }

    private static string[] Play(Game game, Card source, Card[] held)
    {
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source.ObjectId);
        var allocation = ResourcePayment.Allocate(Assert.Single(action.Costs!), [held[0].ObjectId]);
        return [.. game.Resolve(new Decision(action.Id, [held[1].ObjectId], [held[0].ObjectId], Allocations: allocation))
            .Events.Select(item => System.Text.Json.JsonSerializer.Serialize(item, item.GetType()))];
    }

    private static void AssertRejected(World world, Action attempt)
    {
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var effects = world.Effects.Registered.ToArray();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(attempt);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Equal(pending, world.Agenda.Outstanding);
    }

    private static (Game Game, World World, AbilityRunner Runner, Card Source, Card[] Held) Board(
        string face, int cards, int discard = 1)
    {
        var runner = Runner(face, "Action",
            """{"placeCounters":{"card":"you","counter":"test","count":1}}""",
            cost: $$"""{"discardFromHand":{{discard}}}""");
        Card? source = null;
        Card[] held = [];
        var (game, world) = Playing(board =>
        {
            Hand(board, "01093", 0);
            source = board.CreateCard(face, board.Seats[0].Hand);
            held = [.. Enumerable.Range(0, cards).Select(_ => board.CreateCard("01093", board.Seats[0].Hand))];
        }, abilities: runner);
        return (game, world, runner, source!, held);
    }
}
