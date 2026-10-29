using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic ability book: this proves the pricing mechanism without admitting Thor content.
public sealed class SelfPlayCostTests
{
    [Theory]
    [InlineData(0, 6)]
    [InlineData(2, 4)]
    [InlineData(7, 0)]
    [Rule("rr:initiating-abilities.step.4")]
    public void EachCandidateUsesOnlyItsFutureControllersCurrentMinions(int count, long cost)
    {
        // Printed 06011: "Reduce the cost to play Hercules by 1 for each minion engaged with you."
        var runner = Synthetic();
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var candidate = world.CreateCard("06011", world.Seats[1].Hand);
        var copy = world.CreateCard("06011", world.Seats[1].Hand);
        var foreign = world.CreateCard("06011", world.Seats[0].Hand);
        AddMinions(world, 1, count);
        AddMinions(world, 0, 1);
        Assert.Equal(cost, CardPayment.CostOf(world, world.Facts, world.Seats[1], candidate).Amount);
        Assert.Equal(cost, CardPayment.CostOf(world, world.Facts, world.Seats[1], copy).Amount);
        Assert.Equal(5, CardPayment.CostOf(world, world.Facts, world.Seats[0], foreign).Amount);
        Assert.Equal(6, CardPayment.CostOf(world, world.Facts, world.Seats[0], candidate).Amount);
        Assert.Empty(runner.Constant(world, candidate));
        var moved = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards[0];
        World.MoveToTop(moved, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Assert.Equal(Math.Max(0, 5 - count), CardPayment.CostOf(world, world.Facts, world.Seats[1], candidate).Amount);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void ChangedEngagementRepricesAndRejectsUnderpaymentWithoutCommittingAnything()
    {
        // Step 5: "abort this process without paying any costs" when costs cannot be paid.
        var runner = Synthetic();
        var (game, world) = Playing(_ => { }, abilities: runner);
        var ally = world.CreateCard("06011", world.Seats[0].Hand);
        AddMinions(world, 0, 2);
        int[] paying = Resources(world, 0, 4);
        Assert.Equal("4", CardPayment.Price(world, world.Facts, world.Seats[0], ally)!.Cost);
        World.MoveToTop(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards[0],
            world.AreaOf(DeckType.EncounterDiscardPile, PlayArea.Villains));
        string digest = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var effects = world.Effects.Registered.ToArray();
        var pending = world.Agenda.Outstanding.ToArray();
        var prompt = game.Pending;
        var events = new List<GameEvent>();
        Assert.Throws<RulesNotImplementedException>(() =>
            CardPlay.Play(world, world.Facts, runner, world.Seats[0], ally, paying, events));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Same(prompt, game.Pending);
        Assert.Empty(events);
        Assert.Same(world.Seats[0].Hand, ally.Area);
        Assert.Equal("5", CardPayment.Price(world, world.Facts, world.Seats[0], ally)!.Cost);
    }

    [Fact]
    public void QueriesPreserveRngPendingWorkAndSubsequentCommittedPlayAgainstControl()
    {
        var runner = Synthetic();
        var (controlGame, control) = Playing(_ => { }, abilities: runner);
        var (game, world) = Playing(_ => { }, abilities: runner);
        var expected = control.CreateCard("06011", control.Seats[0].Hand);
        var ally = world.CreateCard("06011", world.Seats[0].Hand);
        AddMinions(control, 0, 2);
        AddMinions(world, 0, 2);
        int[] expectedPayment = Resources(control, 0, 4);
        int[] payment = Resources(world, 0, 4);
        string digest = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var effects = world.Effects.Registered.ToArray();
        var pending = world.Agenda.Outstanding.ToArray();
        var prompt = game.Pending;
        for (int query = 0; query < 3; query++)
        {
            Assert.Equal(4, CardPayment.CostOf(world, world.Facts, world.Seats[0], ally).Amount);
            Assert.Equal("4", CardPayment.Price(world, world.Facts, world.Seats[0], ally)!.Cost);
        }
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Same(prompt, game.Pending);
        var expectedEvents = new List<GameEvent>();
        var actualEvents = new List<GameEvent>();
        CardPlay.Play(control, control.Facts, runner, control.Seats[0], expected, expectedPayment, expectedEvents);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], ally, payment, actualEvents);
        Assert.Equal(expectedEvents.Select(Describe), actualEvents.Select(Describe));
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(control.Effects.Registered, world.Effects.Registered);
        Assert.Equal(control.Agenda.Outstanding.Select(step => step.What), world.Agenda.Outstanding.Select(step => step.What));
        Assert.Equal(controlGame.Pending?.Label, game.Pending?.Label);
    }

    [Fact]
    public void DiscountsStackAtZeroAndRemainSubjectToUniquenessAndAllyLimit()
    {
        var runner = Synthetic();
        var (_, world) = Playing(_ => { }, abilities: runner);
        var ally = world.CreateCard("06011", world.Seats[0].Hand);
        AddMinions(world, 0, 5);
        CardPayment.ReduceNextCardCost(world, world.Seats[0].IdentityCard, 0, 2);
        Assert.Equal("0", CardPayment.Price(world, world.Facts, world.Seats[0], ally)!.Cost);
        var events = new List<GameEvent>();
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], ally, [], events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        var duplicate = world.CreateCard("06011", world.Seats[0].Hand);
        Assert.Null(CardPayment.Price(world, world.Facts, world.Seats[0], duplicate));
        Assert.Equal(1, CardPayment.CostOf(world, world.Facts, world.Seats[0], duplicate).Amount);
        World.MoveToTop(ally, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        foreach (string face in new[] { "01002", "01067", "01068" })
            world.CreateCard(face, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        Assert.Equal("1", CardPayment.Price(world, world.Facts, world.Seats[0], duplicate)!.Cost);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], duplicate, Resources(world, 0, 1), events);
        Assert.NotNull(Sequence.Work(world, world.Facts, runner, events));
        Assert.Contains(world.Agenda.Outstanding, step => step.What == Steps.ChooseAllyForLimit);
    }

    [Fact]
    public void AcceptedPriceSurvivesResourceGeneratorSuspensionAndCandidateExposure()
    {
        var runner = Synthetic();
        Card? ally = null;
        Card? generator = null;
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("06011", board.Seats[0].Hand);
            AddMinions(board, 0, 5);
            generator = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        var offer = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == ally!.ObjectId);
        Assert.Equal("1", Assert.Single(offer.CostOptions).Cost);
        var allocation = ResourcePayment.Allocate(Assert.Single(offer.CostOptions), [generator!.ObjectId]);
        Assert.NotNull(allocation);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, Assert.Single(offer.Targets!.Legal));
        game.Resolve(new Decision(offer.Id, [world.Seats[0].IdentityCard.ObjectId], [generator.ObjectId], Allocations: allocation));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(DeckType.RevealingArea, ally!.Area.Type);
        Assert.Equal(6, CardPayment.CostOf(world, world.Facts, world.Seats[0], ally).Amount);
        game.Resolve(Decision.Decline);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }

    [Theory]
    [InlineData("Action", "{\"reduceSelfCostPerEngagedMinion\":1}")]
    [InlineData("Constant", "{\"seq\":[{\"reduceSelfCostPerEngagedMinion\":1}]}")]
    [InlineData("Constant", "{\"reduceSelfCostPerEngagedMinion\":0}")]
    public void UnsupportedPricingProgramsAreRejectedBeforeGameplay(string timing, string effect)
    {
        string book = "{\"cards\":[{\"card\":\"06011\",\"abilities\":[{\"trigger\":{\"timing\":\""
            + timing + "\",\"subject\":\"this\"},\"effect\":" + effect + "}]}]}";
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(book)));
    }

    private static string Describe(GameEvent item) => System.Text.Json.JsonSerializer.Serialize(item, item.GetType());
    private static AbilityRunner Synthetic() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"06011","abilities":[{
          "trigger":{"timing":"Constant","subject":"this"},"effect":{"reduceSelfCostPerEngagedMinion":1}
        }]},{"card":"01030","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
          "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"Y"}
        }]},{"card":"01092","abilities":[{
          "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
          "effect":{"draw":{"player":"you","count":1}}
        }]}]}
        """));
    private static void AddMinions(World world, int player, int count)
    {
        for (int i = 0; i < count; i++)
            world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));
    }
    private static int[] Resources(World world, int player, int count) => Enumerable.Range(0, count)
        .Select(_ => world.CreateCard("01093", world.Seats[player].Hand).ObjectId).ToArray();
}
