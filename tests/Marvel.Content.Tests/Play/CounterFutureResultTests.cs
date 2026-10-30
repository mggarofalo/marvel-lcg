using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic controls for pending results during counter admission.</summary>
public sealed class CounterFutureResultTests : CoreCardTestBase
{
    [Theory]
    [InlineData("{\"result\":\"healed\"}", true)]
    [InlineData("{\"mul\":[2,{\"result\":\"healed\"}]}", true)]
    [InlineData("{\"min\":[0,{\"result\":\"healed\"}]}", false)]
    [InlineData("{\"mul\":[0,{\"result\":\"healed\"}]}", false)]
    [Rule("rr:target.3")]
    public void PendingHealingCostCanProduceOnlyAnUnboundedPositiveCounterAmount(string count, bool offered)
    {
        // A target is valid if an effect can affect it; an ordered cost supplies its later receipt.
        var (world, runner, source) = Board(count, true);
        string before = world.Digest().Canonical();
        var actions = runner.Actions(world, 0);
        Assert.Equal(offered, actions.Any(action => action.Card == source.ObjectId));
        Assert.Equal(before, world.Digest().Canonical());
        if (!offered) return;
        var action = Assert.Single(actions, item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(count.Contains("mul", StringComparison.Ordinal) ? 4 : 2,
            source.Tokens.GetValueOrDefault("c_test"));
    }

    [Fact]
    [Rule("rr:target.3")]
    public void AnUnwrittenResultHasNoPotentialWithoutAProducer()
    {
        // An unwritten result cannot make zero counters affect the target.
        var (world, runner, source) = Board("{\"result\":\"healed\"}", false);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
    }

    [Fact]
    [Rule("rr:target.3")]
    public void AZeroHealInstructionCannotProducePositiveCounters()
    {
        // A zero heal leaves its later result zero and cannot affect either target.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"seq":[{"heal":{"card":"you","amount":0}},
                {"placeCounters":{"card":"this","counter":"test","count":{"result":"healed"}}}]}
            }]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
    }
    private static (World World, AbilityRunner Runner, Card Source) Board(string count, bool cost)
    {
        string payment = cost ? "\"cost\":{\"seq\":[{\"dealDamage\":{\"cards\":\"you\",\"amount\":2}},{\"heal\":{\"card\":\"you\",\"amount\":2}}]}," : "";
        string rows = "{\"cards\":[{\"card\":\"01006\",\"abilities\":[{\"trigger\":{\"event\":\"WhenActionTriggered\",\"timing\":\"Action\",\"subject\":\"this\"}," + payment
            + "\"effect\":{\"placeCounters\":{\"card\":\"this\",\"counter\":\"test\",\"count\":" + count + "}}}]}]}";
        var runner = new AbilityRunner(AbilityCatalog.Parse(rows));
        var world = Hero("01001a");
        world.Abilities = runner;
        return (world, runner, world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0)));
    }
}
