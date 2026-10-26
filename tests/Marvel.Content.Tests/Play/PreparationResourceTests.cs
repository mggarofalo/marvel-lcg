using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PreparationResourceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:resource-ability.1")]
    public void TwoGauntletsPayPreparationInEitherFormAndQueriesPreserveContinuation(bool hero)
    {
        // 08007: "Exhaust Black Widow's Gauntlet ... generate a wild resource for a Preparation card."
        var board = PreparationResourceFixture.Board(hero);
        var control = PreparationResourceFixture.Board(hero);
        string before = board.World.Digest().Canonical();
        long words = board.World.Random.Generator.WordsConsumed;
        var random = board.World.Random.Generator.GetState();
        var pending = board.Game.Pending;
        var agenda = board.World.Agenda.Outstanding.ToArray();
        var runner = PreparationResourceFixture.Runner();
        for (int repeat = 0; repeat < 2; repeat++)
        {
            var price = Assert.IsType<CostOption>(CardPayment.Price(board.World, board.World.Facts,
                board.World.Seats[0], board.Target));
            Assert.Equal("2", price.Cost);
            Assert.Equal(2, price.Generators.Count(source => source.Restriction == ResourceRestriction.PreparationCard));
            var first = price.Generators.Single(source => source.Effect == board.First.ObjectId);
            Assert.Equal("G", first.Generates);
            Assert.Contains("For a Preparation card's resource or ability cost", first.CostDescription, StringComparison.Ordinal);
            Assert.DoesNotContain(CardPayment.Generators(board.World, board.World.Facts, board.World.Seats[1], runner,
                board.Target), source => source.Effect == board.First.ObjectId);
        }
        Assert.Equal(before, board.World.Digest().Canonical());
        Assert.Equal(words, board.World.Random.Generator.WordsConsumed);
        Assert.Equal(random.Words, board.World.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, board.World.Random.Generator.GetState().Index);
        Assert.Same(pending, board.Game.Pending);
        Assert.Equal(agenda, board.World.Agenda.Outstanding);
        Pay(board.Game, board.World, board.Target, [board.First.ObjectId, board.Second.ObjectId]);
        Pay(control.Game, control.World, control.Target, [control.First.ObjectId, control.Second.ObjectId]);
        Assert.False(board.First.Ready);
        Assert.False(board.Second.Ready);
        Assert.Equal(DeckType.UpgradesArea, board.Target.Area.Type);
        Assert.Equal(control.World.Digest().Canonical(), board.World.Digest().Canonical());
        Assert.Equal(control.World.Random.Generator.WordsConsumed, board.World.Random.Generator.WordsConsumed);
        Assert.Equal(control.World.Random.Generator.GetState().Words, board.World.Random.Generator.GetState().Words);
        Assert.Equal(control.World.Random.Generator.GetState().Index, board.World.Random.Generator.GetState().Index);
        Assert.Equal(control.Game.Pending!.Asking, board.Game.Pending!.Asking);
        Assert.Equal(control.World.Agenda.Outstanding, board.World.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void SameCostNonPreparationAndUnrelatedAbilityCannotSpendGauntlet()
    {
        // Costs must be payable before paying: "abort ... without paying any costs".
        var board = PreparationResourceFixture.Board(face: "01064");
        var runner = PreparationResourceFixture.Runner();
        Assert.Equal("2", board.World.Facts.Attributes(board.Target.FaceId)["Cost"]);
        Assert.Null(CardPayment.Price(board.World, board.World.Facts, board.World.Seats[0], board.Target));
        var preparation = board.World.CreateCard("08008", board.World.Seats[0].Hand);
        Assert.NotNull(CardPayment.Price(board.World, board.World.Facts, board.World.Seats[0], preparation));
        Assert.DoesNotContain(CardPayment.Generators(board.World, board.World.Facts,
            board.World.Seats[0], runner, abilitySource: board.Target), source => source.Effect == board.First.ObjectId);
        string before = board.World.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => AbilityResourcePayment.Spend(board.World, runner,
            new(board.Target, 0, [board.First.ObjectId], 1, ""), []));
        Assert.Equal(before, board.World.Digest().Canonical());
        Assert.True(board.First.Ready);
        // Inference from Finesse and the 1.6 event-cost ruling: the actual
        // Preparation's own ability retains its matching card context.
        var payment = AbilityResourcePayment.Spend(board.World, runner,
            new(preparation, 0, [board.First.ObjectId], 1, "Y"), []);
        Assert.True(payment.IsComplete);
        Assert.False(board.First.Ready);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("exhausted")]
    [InlineData("wrong-seat")]
    [Rule("rr:initiating-abilities.step.5")]
    public void InvalidOrStalePaymentLeavesPromptResourcesAndPendingWorkIntact(string invalid)
    {
        // Step 5 aborts "without paying any costs".
        var board = PreparationResourceFixture.Board();
        var offer = Assert.Single(board.Game.Pending!.Affordances,
            item => item.Verb == "Play" && item.AnchorId == board.Target.ObjectId);
        if (invalid == "exhausted") board.Second.Exhaust();
        int[] paying = invalid == "duplicate" ? [board.First.ObjectId, board.First.ObjectId]
            : [board.First.ObjectId, board.Second.ObjectId];
        var allocation = ResourcePayment.Allocate(Assert.Single(offer.Costs!), paying);
        string before = board.World.Digest().Canonical();
        long words = board.World.Random.Generator.WordsConsumed;
        var random = board.World.Random.Generator.GetState();
        var pending = board.Game.Pending;
        var agenda = board.World.Agenda.Outstanding.ToArray();
        var answer = new Decision(offer.Id, [board.World.Seats[0].IdentityCard.ObjectId], paying, Allocations: allocation ?? []);
        if (invalid == "wrong-seat")
            Assert.Equal(DecisionRejection.UnauthorizedActor, DecisionAssessment.Assess(board.Game.Pending!, answer, 1).Rejection);
        else
            Assert.ThrowsAny<ArgumentException>(() => board.Game.Resolve(answer));
        Assert.Equal(before, board.World.Digest().Canonical());
        Assert.Equal(words, board.World.Random.Generator.WordsConsumed);
        Assert.Equal(random.Words, board.World.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, board.World.Random.Generator.GetState().Index);
        Assert.Equal(agenda, board.World.Agenda.Outstanding);
        Assert.Same(pending, board.Game.Pending);
        Assert.True(board.First.Ready);
    }

    [Fact]
    [Rule("rr:resource-ability.1")]
    public void CommittedGauntletStaysExhaustedAcrossNestedGeneratorPayment()
    {
        // A resource ability may trigger "anytime ... generating resources to pay a cost".
        var board = PreparationResourceFixture.Board(nested: true);
        var control = PreparationResourceFixture.Board(nested: true);
        var generator = Assert.Single(board.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0).Cards);
        Pay(board.Game, board.World, board.Target, [board.First.ObjectId, generator.ObjectId]);
        var controlGenerator = Assert.Single(control.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0).Cards);
        Pay(control.Game, control.World, control.Target, [control.First.ObjectId, controlGenerator.ObjectId]);
        Assert.Equal(Question.Opportunity, board.Game.Pending!.Asking);
        Assert.False(board.First.Ready);
        Assert.True(board.Second.Ready);
        Assert.Equal(DeckType.RevealingArea, board.Target.Area.Type);
        var runner = PreparationResourceFixture.Runner();
        string before = board.World.Digest().Canonical();
        var random = board.World.Random.Generator.GetState();
        long words = board.World.Random.Generator.WordsConsumed;
        var pending = board.Game.Pending;
        var agenda = board.World.Agenda.Outstanding.ToArray();
        Assert.DoesNotContain(runner.ResourceAbilities(board.World, 0), source => source.Effect == board.First.ObjectId);
        Assert.Equal(before, board.World.Digest().Canonical());
        Assert.Equal(random.Words, board.World.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, board.World.Random.Generator.GetState().Index);
        Assert.Equal(words, board.World.Random.Generator.WordsConsumed);
        Assert.Equal(agenda, board.World.Agenda.Outstanding);
        Assert.Same(pending, board.Game.Pending);
        board.Game.Resolve(Decision.Decline);
        control.Game.Resolve(Decision.Decline);
        Assert.Equal(DeckType.UpgradesArea, board.Target.Area.Type);
        Assert.False(board.First.Ready);
        Assert.True(board.Second.Ready);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.False(board.World.Agenda.IsBusy);
        Assert.Equal(control.World.Digest().Canonical(), board.World.Digest().Canonical());
        Assert.Equal(control.World.Random.Generator.GetState().Words, board.World.Random.Generator.GetState().Words);
        Assert.Equal(control.World.Random.Generator.GetState().Index, board.World.Random.Generator.GetState().Index);
        Assert.Equal(control.World.Agenda.Outstanding, board.World.Agenda.Outstanding);
        Assert.Equal(control.Game.Pending!.Asking, board.Game.Pending!.Asking);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void SuspendedPaymentCannotRetargetAnAcceptedGauntletToItsReenteredCopyOrItsTwin()
    {
        // A card returning to play is "a new copy of that card".
        var board = PreparationResourceFixture.Board(nested: true);
        var generator = Assert.Single(board.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0).Cards);
        Pay(board.Game, board.World, board.Target, [generator.ObjectId, board.First.ObjectId]);
        Assert.Equal(Question.Opportunity, board.Game.Pending!.Asking);
        World.MoveToTop(board.First, board.World.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        World.MoveToTop(board.First, board.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var error = Assert.Throws<RulesNotImplementedException>(() => board.Game.Resolve(Decision.Decline));
        Assert.Contains("accepted copy and contribution", error.Message, StringComparison.Ordinal);
        Assert.True(board.First.Ready);
        Assert.True(board.Second.Ready);
        Assert.Equal(DeckType.RevealingArea, board.Target.Area.Type);
    }

    private static void Pay(Game game, World world, Card target, int[] paying)
    {
        var offer = Assert.Single(game.Pending!.Affordances, item => item.Verb == "Play" && item.AnchorId == target.ObjectId);
        ResourceAllocation[] allocation = [new(paying[0], 0, "Y"), new(paying[1], 0, "B")];
        Assert.True(ResourcePayment.Allows(Assert.Single(offer.Costs!), paying, null, allocation));
        game.Resolve(new Decision(offer.Id, [world.Seats[0].IdentityCard.ObjectId], paying, Allocations: allocation));
    }
}
