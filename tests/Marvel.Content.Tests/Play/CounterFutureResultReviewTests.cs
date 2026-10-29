using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CounterFutureResultReviewTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:target.3")]
    public void HealingCostCanConsumeTheDamageBeforeTheLaterHeal()
    {
        // The later heal replaces the earlier cost result with zero.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"heal":{"card":"you","amount":1}},
              "effect":{"seq":[{"heal":{"card":"you","amount":2}},
                {"placeCounters":{"card":"this","counter":"test","count":{"result":"healed"}}}]}
            }]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        world.Seats[0].IdentityCard.TakeDamage(1);
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
    }
    [Fact]
    [Rule("rr:target.3")]
    public void EarlierDamageEffectMakesTheHealEffective()
    {
        // The damage sentence can affect the identity before the heal resolves.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"seq":[{"dealDamage":{"cards":"you","amount":2}},
                {"heal":{"card":"you","amount":2}},
                {"placeCounters":{"card":"this","counter":"test","count":{"result":"healed"}}}]}
            }]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var offer = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(offer, [], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(2, source.Tokens.GetValueOrDefault("c_test"));
    }
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [Rule("rr:target.3")]
    public void ExhaustionResultRequiresAReadyTarget(bool exhausted, bool offered)
    {
        // Exhausting an already exhausted card cannot produce an exhausted result.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"seq":[{"exhaust":"you"},
                {"placeCounters":{"card":"this","counter":"test","count":{"result":"exhausted"}}}]}
            }]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        if (exhausted) world.Seats[0].IdentityCard.Exhaust();
        var actions = runner.Actions(world, 0);
        Assert.Equal(offered, actions.Any(action => action.Card == source.ObjectId));
        if (!offered) return;
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(actions, action => action.Card == source.ObjectId), [], []));
        Assert.Equal(1, source.Tokens.GetValueOrDefault("c_test"));
    }
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [Rule("rr:target.3")]
    public void EarlierCostDamageIsProjectedBeforeTheHeal(bool removeDamage, bool offered)
    {
        // A payable cost can add damage before the heal; a later cost can remove it again.
        string later = removeDamage ? ",{\"heal\":{\"card\":\"you\",\"amount\":2}}" : "";
        string rows = "{\"cards\":[{\"card\":\"01006\",\"abilities\":[{\"trigger\":{\"event\":\"WhenActionTriggered\",\"timing\":\"Action\",\"subject\":\"this\"},\"cost\":{\"seq\":[{\"dealDamage\":{\"cards\":\"you\",\"amount\":2}}" + later
            + "]},\"effect\":{\"seq\":[{\"heal\":{\"card\":\"you\",\"amount\":2}},{\"placeCounters\":{\"card\":\"this\",\"counter\":\"test\",\"count\":{\"result\":\"healed\"}}}]}}]}]}";
        var runner = new AbilityRunner(AbilityCatalog.Parse(rows));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var actions = runner.Actions(world, 0);
        Assert.Equal(offered, actions.Any(action => action.Card == source.ObjectId));
        if (!offered) return;
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(actions, item => item.Card == source.ObjectId), [], []));
        Assert.Equal(2, source.Tokens.GetValueOrDefault("c_test"));
    }
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [Rule("rr:target.3")]
    public void HealingResultRequiresAnEffectThatCanActuallyHeal(int damage, bool offered)
    {
        // A target is valid if any part of the ability can affect it.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"seq":[{"heal":{"card":"you","amount":2}},
                {"placeCounters":{"card":"this","counter":"test","count":{"result":"healed"}}}]}
            }]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Seats[0].IdentityCard.TakeDamage(damage);
        var actions = runner.Actions(world, 0);
        Assert.Equal(offered, actions.Any(action => action.Card == source.ObjectId));
        if (!offered) return;
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(actions, action => action.Card == source.ObjectId), [], []));
        Assert.Equal(1, source.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }
}
