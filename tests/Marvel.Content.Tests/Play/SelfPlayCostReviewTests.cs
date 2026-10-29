using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Independent synthetic pricing probes; no actual Hercules ability row is admitted.
public sealed class SelfPlayCostReviewTests
{
    [Fact]
    public void ScalarCountsOnlyMinionsInThePlayingSeatsEngagementArea()
    {
        // Printed 06011 reduces its play cost "for each minion engaged with you."
        // Scalar two is a synthetic contract probe of the chosen DSL instruction.
        var runner = Runner(2);
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var candidate = world.CreateCard("06011", world.Seats[1].Hand);
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1));
        world.CreateCard("01101", engaged);
        world.CreateCard("01093", engaged);
        world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck, PlayArea.Villains));
        Assert.Equal(4, CardPayment.CostOf(world, world.Facts, world.Seats[1], candidate).Amount);
        Assert.Equal(6, CardPayment.CostOf(world, world.Facts, world.Seats[0], candidate).Amount);
        Assert.Empty(runner.Constant(world, candidate));
        World.MoveToTop(candidate, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)));
        Assert.Empty(runner.Constant(world, candidate));
        Assert.Equal(0, runner.SelfPlayCostReduction(world, candidate, 1));
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void StaleUnderpaymentCanRetryAndConsumesDiscountOnlyOnSuccessfulPlay()
    {
        // "abort this process without paying any costs" when the costs cannot be paid.
        var runner = Runner(1);
        var (_, world) = Playing(_ => { }, abilities: runner);
        var candidate = world.CreateCard("06011", world.Seats[0].Hand);
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var payment = Enumerable.Range(0, 5)
            .Select(_ => world.CreateCard("01093", world.Seats[0].Hand).ObjectId).ToArray();
        CardPayment.ReduceNextCardCost(world, world.Seats[0].IdentityCard, 0, 1);
        Assert.Equal("4", CardPayment.Price(world, world.Facts, world.Seats[0], candidate)!.Cost);
        World.MoveToTop(minion, world.AreaOf(DeckType.EncounterDiscardPile, PlayArea.Villains));
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var effects = world.Effects.Registered.ToArray();
        var pending = world.Agenda.Outstanding.ToArray();
        var events = new List<GameEvent>();
        for (int attempt = 0; attempt < 2; attempt++)
        {
            Assert.Throws<RulesNotImplementedException>(() =>
                CardPlay.Play(world, world.Facts, runner, world.Seats[0], candidate, payment[..4], events));
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(words, world.Random.Generator.WordsConsumed);
            Assert.Equal(effects, world.Effects.Registered);
            Assert.Equal(pending, world.Agenda.Outstanding);
            Assert.Empty(events);
        }
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], candidate, payment, events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.AlliesArea, candidate.Area.Type);
        Assert.All(payment, id => Assert.Equal(DeckType.DiscardPile, world.Cards.Single(c => c.ObjectId == id).Area.Type));
        Assert.Equal(6, CardPayment.CostOf(world, world.Facts, world.Seats[0], candidate).Amount);
        Assert.NotEmpty(events);
    }

    [Fact]
    public void DedicatedPricingDoesNotSuppressUnsupportedConstantSibling()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"06011","abilities":[
              {"trigger":{"timing":"Constant","subject":"this"},"effect":{"reduceSelfCostPerEngagedMinion":1}},
              {"trigger":{"timing":"Constant","subject":"this"},"effect":{"draw":{"player":"you","count":1}}}
            ]}]}
            """));
        var (_, world) = Playing(_ => { }, abilities: Runner(1));
        var candidate = world.CreateCard("06011", world.Seats[0].Hand);
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Constant(world, candidate));
        Assert.Contains("cannot resolve", error.Message);
        Assert.Contains("Draw", error.Message);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.4")]
    [Rule("rr:initiating-abilities.step.5")]
    public void EngagementDepartureDuringCommittedGeneratorCostDoesNotRepriceTheReceipt()
    {
        // "Apply any modifiers to the cost(s)" precedes "Pay the cost(s)".
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
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
        Card? candidate = null;
        Card? generator = null;
        var (game, world) = Playing(board =>
        {
            candidate = board.CreateCard("06011", board.Seats[0].Hand);
            for (int i = 0; i < 5; i++)
                board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            generator = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        var offer = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == candidate!.ObjectId);
        var price = Assert.Single(offer.CostOptions);
        Assert.Equal("1", price.Cost);
        var allocation = ResourcePayment.Allocate(price, [generator!.ObjectId]);
        Assert.NotNull(allocation);
        game.Resolve(new Decision(offer.Id, [world.Seats[0].IdentityCard.ObjectId], [generator.ObjectId], Allocations: allocation));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(DeckType.RevealingArea, candidate!.Area.Type);
        foreach (var minion in world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards.ToArray())
            World.MoveToTop(minion, world.AreaOf(DeckType.EncounterDiscardPile, PlayArea.Villains));
        Assert.Equal(6, CardPayment.CostOf(world, world.Facts, world.Seats[0], candidate).Amount);
        game.Resolve(Decision.Decline);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.AlliesArea, candidate.Area.Type);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }

    [Theory]
    [InlineData("\"when\":{\"titleInPlay\":\"Spider-Man\"},")]
    [InlineData("\"cost\":{\"exhaust\":\"this\"},")]
    [InlineData("\"anyPlayer\":true,")]
    [InlineData("\"limitPerRound\":1,")]
    [InlineData("\"maxPerRound\":1,")]
    public void CandidatePricingRejectsConditionalOrCostedPrograms(string qualifier)
    {
        var book = """
            {"cards":[{"card":"06011","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},QUALIFIER
              "effect":{"reduceSelfCostPerEngagedMinion":1}
            }]}]}
            """.Replace("QUALIFIER", qualifier, StringComparison.Ordinal);
        var error = Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(book)));
        Assert.Contains("direct unconditional Constant", error.Message);
    }

    private static AbilityRunner Runner(int amount) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"06011","abilities":[{
          "trigger":{"timing":"Constant","subject":"this"},"effect":{"reduceSelfCostPerEngagedMinion":AMOUNT}
        }]}]}
        """.Replace("AMOUNT", amount.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)));
}
