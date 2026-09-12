using Marvel.Cards.Dsl;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class ActionAbilityCoreARedundantSourceRemainsAdvertisedTests
{
    [Fact]
    public void ARedundantSourceRemainsAdvertisedWhenItCannotChangeThePaidOutcome()
    {
        const string relentlessAssault = "01053";
        Card? card = null;
        Card? triplePhysical = null;
        Card? mental = null;
        var runner = AuthoredCards.Runner();
        var(_, world) = Playing(board =>
        {
            Hand(board, Physicals, 0);
            card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
            triplePhysical = board.CreateCard("10007", board.Seats[0].Hand);
            mental = board.CreateCard(Mentals, board.Seats[0].Hand);
            board.CreateCard(AuthoredCards.Shocker, board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        foreach (var extra in world.Seats[0].Hand.Cards.Where(candidate => candidate != card && candidate != triplePhysical && candidate != mental).ToList())
        {
            World.MoveToTop(extra, world.Seats[0].Deck);
        }

        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Contains(price.Generators, source => source.Effect == triplePhysical!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == mental!.ObjectId);
        runner.Act(world, action, [triplePhysical!.ObjectId, mental!.ObjectId], [world.Cards.First(candidate => candidate.FaceId == AuthoredCards.Shocker).ObjectId]);
        Assert.Equal(DeckType.DiscardPile, triplePhysical.Area.Type);
        Assert.Equal(DeckType.DiscardPile, mental.Area.Type);
    }

    [Rule("rr:wild-resource")]
    [Fact]
    public void ARemainingWildDeclarationChoiceIsOfferedButNotInferred()
    {
        const string requiredEvent = "27016"; // one of two wilds must be physical.
        Card? card = null;
        Card? doubleWild = null;
        var runner = Runner(requiredEvent, "Action", """{ "if": { "test": { "paidWithResource": "G" }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        var(_, world) = Playing(board =>
        {
            Hand(board, Physicals, 0);
            card = board.CreateCard(requiredEvent, board.Seats[0].Hand);
            doubleWild = board.CreateCard("01044", board.Seats[0].Hand);
        }, heroes: ["captain_marvel"], abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.True(price.DeclarationSensitive);
        Assert.Contains(price.Generators, source => source.Effect == doubleWild!.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, action, [doubleWild!.ObjectId], []));
        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, doubleWild!.Area);
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, action, [doubleWild.ObjectId], [], allocations: [new ResourceAllocation(doubleWild.ObjectId, Cost: 0, PaidAs: "GG"), ]));
        Assert.Same(world.Seats[0].Hand, card.Area);
        Assert.Same(world.Seats[0].Hand, doubleWild.Area);
        runner.Act(world, action, [doubleWild.ObjectId], [], allocations: [new ResourceAllocation(doubleWild.ObjectId, Cost: 0, PaidAs: "RG"), ]);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleWild.Area.Type);
    }

    [Rule("rr:cost.5")]
    [Rule("rr:cost.5.1")]
    [Fact]
    public void OneDoubleResourceCanBeDividedBetweenSimultaneousCosts()
    {
        // Two energy costs on one ability are paid simultaneously. A single
        // card generating two energy icons supplies one to each cost and is
        // discarded once.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "draw": { "player": "you", "count": 1 } }""", cost: """{ "seq": [ { "spend": "Y" }, { "spend": "Y" } ] }""");
        Card? source = null;
        Card? doubleEnergy = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            Hand(board, AuthoredCards.Backflip, 0);
            doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        var price = Assert.Single(action.CostOptions);
        Assert.Equal("2", price.Cost);
        Assert.Equal(["YY"], price.Rule);
        game.Resolve(Decision.Take(action.Id, [], [doubleEnergy!.ObjectId]));
        Assert.Equal(DeckType.DiscardPile, doubleEnergy.Area.Type);
        Assert.NotNull(game.Pending);
        Assert.Equal(Question.TurnOption, game.Pending.Asking);
    }

    [Rule("rr:cost.5")]
    [Rule("rr:cost.5.1")]
    [Fact]
    public void OneDoubleResourceCanBeAllocatedAcrossAnEventsTwoCosts()
    {
        // An event's printed cost and its cost before the arrow are paid
        // simultaneously, and the player "chooses how to divide" a generated
        // double resource between them. One command therefore assigns one icon
        // to each component while naming the generator only once.
        const string eventCard = "01004";
        var runner = Runner(eventCard, "Action", """{ "draw": { "player": "you", "count": 1 } }""", cost: """{ "spend": "Y" }""");
        Card? card = null;
        Card? doubleEnergy = null;
        var(game, _) = Playing(board =>
        {
            card = board.CreateCard(eventCard, board.Seats[0].Hand);
            doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == card!.ObjectId);
        var price = Assert.Single(action.CostOptions);
        Assert.Equal("2", price.Cost);
        Assert.Equal(2, price.ResourceCosts.Count);
        Assert.Equal(["1", "1"], price.ResourceCosts.Select(cost => cost.Cost));
        game.Resolve(Decision.Take(action.Id, [], [doubleEnergy!.ObjectId], new Dictionary<string, long>(StringComparer.Ordinal), [new ResourceAllocation(doubleEnergy.ObjectId, Cost: 0, PaidAs: "Y"), new ResourceAllocation(doubleEnergy.ObjectId, Cost: 1, PaidAs: "Y"), ]));
        Assert.Equal(DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleEnergy!.Area.Type);
    }

    [Rule("rr:cost.5")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ACombinedEventCostWithoutAnAllocationChangesNoState()
    {
        const string eventCard = "01004";
        var runner = Runner(eventCard, "Action", """{ "draw": { "player": "you", "count": 1 } }""", cost: """{ "spend": "Y" }""");
        Card? card = null;
        Card? doubleEnergy = null;
        var(_, world) = Playing(board =>
        {
            card = board.CreateCard(eventCard, board.Seats[0].Hand);
            doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
        }, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var thrown = Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, action, [doubleEnergy!.ObjectId], []));
        Assert.Contains("allocation was not supplied", thrown.Message, StringComparison.Ordinal);
        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, doubleEnergy!.Area);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AnInvalidCombinedEventAllocationChangesNoState()
    {
        const string eventCard = "01004";
        var runner = Runner(eventCard, "Action", """{ "draw": { "player": "you", "count": 1 } }""", cost: """{ "spend": "Y" }""");
        Card? card = null;
        Card? doubleEnergy = null;
        var(_, world) = Playing(board =>
        {
            card = board.CreateCard(eventCard, board.Seats[0].Hand);
            doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
        }, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, action, [doubleEnergy!.ObjectId], [], allocations: [new ResourceAllocation(doubleEnergy.ObjectId, Cost: 0, PaidAs: "Y"), ]));
        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, doubleEnergy!.Area);
    }
}
