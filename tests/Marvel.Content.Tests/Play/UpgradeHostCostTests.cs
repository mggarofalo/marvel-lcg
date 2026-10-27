using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic test-local 09039 row; prepared copies test physical host isolation.
public sealed class UpgradeHostCostTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.4")]
    public void PromptPricesEachExactHostAndExecutionUsesTheSelectedPrice()
    {
        // Step 4: "Apply any modifiers to the cost(s)." Printed 09039 reduces each upgrade on Iron Man by 1.
        Card? iron = null, other = null, upgrade = null;
        var runner = Synthetic();
        var (game, world) = Playing(board =>
        {
            iron = Ally(board, "09039", 1);
            other = Ally(board, "01067", 0);
            upgrade = board.CreateCard("01074", board.Seats[0].Hand);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var offer = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == upgrade!.ObjectId);
        Assert.Equal("0", Assert.Single(offer.CostOptions, cost => cost.Target == iron!.ObjectId).Cost);
        Assert.Equal("1", Assert.Single(offer.CostOptions, cost => cost.Target == other!.ObjectId).Cost);
        Assert.False(DecisionAssessment.Assess(game.Pending, new Decision(offer.Id, [other!.ObjectId], [])).IsAccepted);
        game.Resolve(new Decision(offer.Id, [iron!.ObjectId], []));
        Assert.Equal(iron.ObjectId, upgrade!.Area.Host);
        Assert.Equal(1, upgrade.Area.PlayArea.Player);
        Assert.Equal(0, upgrade.Owner);
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
    }

    [Fact]
    public void DirectPaymentUsesOnlyTheSelectedHostAndEachCopyDiscountsIndependently()
    {
        var runner = Synthetic();
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var first = Ally(world, "09039", 0);
        var second = Ally(world, "09039", 1);
        var other = Ally(world, "01067", 0);
        var upgrade = world.CreateCard("01074", world.Seats[0].Hand);
        var ordinary = world.CreateCard("01008", world.Seats[0].Hand);
        Assert.Equal(1, Cost(world, upgrade));
        Assert.Equal(0, Cost(world, upgrade, first.ObjectId));
        Assert.Equal(0, Cost(world, upgrade, second.ObjectId));
        Assert.Equal(1, Cost(world, upgrade, other.ObjectId));
        Assert.Equal(1, Cost(world, ordinary, first.ObjectId));
        Assert.Equal(4, Cost(world, second, first.ObjectId));
        var events = new List<GameEvent>();
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, world.Facts, runner,
            world.Seats[0], upgrade, [], events, [other.ObjectId]));
        Assert.Empty(events);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], upgrade, [], events, [second.ObjectId]);
        Assert.Equal(second.ObjectId, upgrade.Area.Host);
        Assert.NotEmpty(events);
        var another = world.CreateCard("03031", world.Seats[0].Hand);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], another, [], events, [first.ObjectId]);
        Assert.Equal(first.ObjectId, another.Area.Host);
    }

    [Fact]
    public void FreeUpgradeRemainsZeroAndOtherReductionsStackWithoutConsumingTheHostAbility()
    {
        var runner = Synthetic();
        var (_, world) = Playing(_ => { }, abilities: runner);
        var iron = Ally(world, "09039", 0);
        var upgrade = world.CreateCard("01074", world.Seats[0].Hand);
        CardPayment.ReduceNextCardCost(world, world.Seats[0].IdentityCard, 0, 2);
        Assert.Equal(0, Cost(world, upgrade, iron.ObjectId));
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], upgrade, [], [], [iron.ObjectId]);
        var next = world.CreateCard("03031", world.Seats[0].Hand);
        Assert.Equal(0, Cost(world, next, iron.ObjectId));
        World.MoveToTop(iron, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        Assert.Equal(1, Cost(world, next, iron.ObjectId));
        Assert.Null(CardPayment.Price(world, world.Facts, world.Seats[0], next, iron.ObjectId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.step.5")]
    public void IllegalOrStaleHostRejectsWithoutChangingPromptRngOrPendingWork(bool stale)
    {
        // Step 5: "abort this process without paying any costs."
        Card? iron = null, upgrade = null;
        var (game, world) = Playing(board =>
        {
            iron = Ally(board, "09039", 0);
            upgrade = board.CreateCard("01074", board.Seats[0].Hand);
        }, abilities: Synthetic());
        var prompt = game.Pending!;
        var offer = Assert.Single(prompt.Affordances, option => option.AnchorId == upgrade!.ObjectId);
        if (stale) World.MoveToTop(iron!, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        var digest = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        int target = stale ? iron!.ObjectId : world.Seats[0].IdentityCard.ObjectId;
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(offer.Id, [target], [])));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Same(world.Seats[0].Hand, upgrade!.Area);
    }

    [Fact]
    public void PricingQueriesPreserveExecutionAndMatchAnUnprobedControl()
    {
        var runner = Synthetic();
        var (game, world) = Playing(_ => { }, abilities: runner);
        var (controlGame, control) = Playing(_ => { }, abilities: runner);
        var host = Ally(world, "09039", 0);
        var expectedHost = Ally(control, "09039", 0);
        var upgrade = world.CreateCard("01074", world.Seats[0].Hand);
        var expectedUpgrade = control.CreateCard("01074", control.Seats[0].Hand);
        string digest = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        var prompt = game.Pending;
        for (int query = 0; query < 3; query++)
        {
            Assert.Equal(0, Cost(world, upgrade, host.ObjectId));
            Assert.Equal("0", CardPayment.Price(world, world.Facts, world.Seats[0], upgrade, host.ObjectId)!.Cost);
        }
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Same(prompt, game.Pending);
        var events = new List<GameEvent>();
        var expected = new List<GameEvent>();
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], upgrade, [], events, [host.ObjectId]);
        CardPlay.Play(control, control.Facts, runner, control.Seats[0], expectedUpgrade, [], expected, [expectedHost.ObjectId]);
        Assert.Equal(expected.Select(Describe), events.Select(Describe));
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(control.Agenda.Outstanding.Select(step => step.What), world.Agenda.Outstanding.Select(step => step.What));
        Assert.Equal(controlGame.Pending?.Label, game.Pending?.Label);
    }

    private static string Describe(GameEvent item) => System.Text.Json.JsonSerializer.Serialize(item, item.GetType());

    [Theory]
    [InlineData("Action", "", "{\"reduceUpgradeCostOnThis\":1}")]
    [InlineData("Constant", "", "{\"seq\":[{\"reduceUpgradeCostOnThis\":1}]}")]
    [InlineData("Constant", "", "{\"reduceUpgradeCostOnThis\":0}")]
    [InlineData("Constant", "\"anyPlayer\":true,", "{\"reduceUpgradeCostOnThis\":1}")]
    public void UnsupportedProgramsFailDuringLowering(string timing, string qualifier, string effect)
    {
        string json = "{\"cards\":[{\"card\":\"09039\",\"abilities\":[{\"trigger\":{\"timing\":\""
            + timing + "\",\"subject\":\"this\"}," + qualifier + "\"effect\":" + effect + "}]}]}";
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(json)));
    }

    private static long Cost(World world, Card card, int? host = null) =>
        CardPayment.CostOf(world, world.Facts, world.Seats[0], card, host).Amount;
    private static Card Ally(World world, string face, int player) =>
        world.CreateCard(face, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));
    private static AbilityRunner Synthetic() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"09039","abilities":[{
          "trigger":{"timing":"Constant","subject":"this"},"effect":{"reduceUpgradeCostOnThis":1}
        }]},{"card":"01074","attachTo":{"query":"allies"},"abilities":[]},
        {"card":"03031","attachTo":{"query":"allies"},"abilities":[]},
        {"card":"01008","abilities":[]},{"card":"01067","abilities":[]}]}
        """));
}
