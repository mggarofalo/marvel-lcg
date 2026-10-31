using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class FirstAllyCostTests
{
    [Fact]
    [Rule("rr:constant-ability")]
    public void FirstAllyAcrossPlayersGetsDiscountWhileIdentityFaceIsActive()
    {
        // Printed03001b: "Reduce the cost of the first ally played each round by 1."
        var runner = Discount();
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var first = world.CreateCard("01068", world.Seats[1].Hand);
        var later = world.CreateCard("01068", world.Seats[0].Hand);
        Assert.Equal(3, CardPayment.CostOf(world, world.Facts, world.Seats[1], first).Amount);
        Pay(world, runner, first, 1, 3);
        Assert.Equal(4, CardPayment.CostOf(world, world.Facts, world.Seats[0], later).Amount);
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.Equal(3, CardPayment.CostOf(world, world.Facts, world.Seats[0], later).Amount);
    }

    [Fact]
    [Rule("rr:constant-ability")]
    public void AllyPlayedInHeroFormConsumesFirstPositionBeforeChangingBack()
    {
        // Official FAQ03001a: Living Legend only applies to the very first ally each round,
        // even if an earlier ally was played while Captain America's hero face was active.
        var runner = Discount();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var first = world.CreateCard("01068", world.Seats[0].Hand);
        Assert.Equal(4, CardPayment.CostOf(world, world.Facts, world.Seats[0], first).Amount);
        Pay(world, runner, first, 0, 4);
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.PeterParker);
        var later = world.CreateCard("01067", world.Seats[0].Hand);
        Assert.Equal(2, CardPayment.CostOf(world, world.Facts, world.Seats[0], later).Amount);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void RejectedPaymentLeavesFirstAllyDiscountAvailable()
    {
        // If the cost cannot be paid, rr:initiating-abilities.step.5 says to abort.
        var runner = Discount();
        var (_, world) = Playing(_ => { }, abilities: runner);
        var ally = world.CreateCard("01068", world.Seats[0].Hand);
        var before = world.Effects.Registered.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, world.Facts, runner,
            world.Seats[0], ally, [], new List<GameEvent>()));
        Assert.Same(world.Seats[0].Hand, ally.Area);
        Assert.Equal(before, world.Effects.Registered);
        Assert.Equal(3, CardPayment.CostOf(world, world.Facts, world.Seats[0], ally).Amount);
    }

    [Fact]
    [Rule("rr:play-put-into-play")]
    public void PuttingAnAllyIntoPlayDoesNotConsumeFirstPlay()
    {
        // rr:play-put-into-play: putting a card into play is not playing it.
        var runner = Discount();
        var (_, world) = Playing(_ => { }, abilities: runner);
        var ally = world.CreateCard("01068", world.Seats[0].Hand);
        var source = InPlay(world, AuthoredCards.AuntMay);
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        var first = world.CreateCard("01067", world.Seats[0].Hand);
        Assert.Equal(1, CardPayment.CostOf(world, world.Facts, world.Seats[0], first).Amount);
    }

    [Fact]
    public void PricingPreservesRandomPendingWorkAndRegisteredEffects()
    {
        var runner = Discount();
        var (game, world) = Playing(_ => { }, abilities: runner);
        var ally = world.CreateCard("01068", world.Seats[0].Hand);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var effects = world.Effects.Registered.ToArray();
        var pending = world.Agenda.Outstanding.ToArray();
        var prompt = game.Pending;
        for (int i = 0; i < 3; i++)
            Assert.Equal(3, CardPayment.CostOf(world, world.Facts, world.Seats[0], ally).Amount);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Same(prompt, game.Pending);
    }

    [Theory]
    [InlineData("rhino", "spider_man")]
    [InlineData("crossbones", "hawkeye")]
    public void ExistingProductsDoNotAllocateRoundMarkers(string scenario, string hero)
    {
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(_ => { }, scenario: scenario, heroes: [hero], abilities: runner);
        Assert.False(runner.RequiresAllyPlayHistory(world));
        var ally = world.CreateCard("01068", world.Seats[0].Hand);
        var before = world.Effects.Registered.ToArray();
        Pay(world, runner, ally, 0, 4);
        Assert.Equal(before, world.Effects.Registered);
    }

    [Theory]
    [InlineData(true, "rhino", "spider_man")]
    [InlineData(false, "rhino", "spider_man")]
    [InlineData(false, "crossbones", "hawkeye")]
    public void ExtraPricingQueriesPreserveSubsequentPlayAgainstUnprobedControl(bool discount, string scenario, string hero)
    {
        var runner = discount ? Discount() : AuthoredCards.Runner();
        var (_, control) = Playing(_ => { }, scenario: scenario, heroes: [hero], abilities: runner);
        var (_, probed) = Playing(_ => { }, scenario: scenario, heroes: [hero], abilities: runner);
        var expectedAlly = control.CreateCard("01068", control.Seats[0].Hand);
        var actualAlly = probed.CreateCard("01068", probed.Seats[0].Hand);
        for (int query = 0; query < 3; query++)
            _ = CardPayment.CostOf(probed, probed.Facts, probed.Seats[0], actualAlly);
        var expected = Pay(control, runner, expectedAlly, 0, discount ? 3 : 4);
        var actual = Pay(probed, runner, actualAlly, 0, discount ? 3 : 4);
        Assert.Equal(expected.Select(Describe), actual.Select(Describe));
        Assert.Equal(control.Digest().Canonical(), probed.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(control.Random.Generator.WordsConsumed, probed.Random.Generator.WordsConsumed);
        Assert.Equal(control.Effects.Registered, probed.Effects.Registered);
        Assert.Equal(control.Agenda.Outstanding.Select(step => step.What), probed.Agenda.Outstanding.Select(step => step.What));
    }

    private static string Describe(GameEvent item) => System.Text.Json.JsonSerializer.Serialize(item, item.GetType());

    private static AbilityRunner Discount() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"01001b","abilities":[{
          "trigger":{"timing":"Constant","subject":"this"},"effect":{"reduceFirstAllyCost":1}
        }]},{"card":"01006","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
          "effect":{"putIntoPlay":{"card":{"cardsIn":{"area":"yourHand","title":"Vision"}},"where":"yourPlayArea"}}
        }]}]}
        """));

    private static List<GameEvent> Pay(World world, AbilityRunner runner, Card ally, int player, int amount)
    {
        int[] resources = Enumerable.Range(0, amount).Select(_ => world.CreateCard("01093", world.Seats[player].Hand).ObjectId).ToArray();
        var events = new List<GameEvent>();
        CardPlay.Play(world, world.Facts, runner, world.Seats[player], ally, resources, events);
        return events;
    }
}
