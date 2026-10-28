using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AntManReprintIndependentTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:character")]
    [Rule("rr:target.3")]
    public void FirstAidCanHealAnAllyOrAnEngagedMinion(bool minionTarget)
    {
        // "Identities ... allies, villains, and minions are all characters."
        // A target is valid if any part of the ability can affect it.
        var world = ThorActualFixture.Board("06001a", 2);
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        ally.TakeDamage(1);
        minion.TakeDamage(1);
        var source = world.CreateCard("12019", world.Seats[0].Hand);
        var payment = world.CreateCard("12021", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Contains(prompt.Affordances, item => item.AnchorId == ally.ObjectId);
        Assert.Contains(prompt.Affordances, item => item.AnchorId == minion.ObjectId);
        Assert.DoesNotContain(prompt.Affordances, item => item.AnchorId == world.TheCardIn(DeckType.MainSchemesArea)!.ObjectId);
        var target = minionTarget ? minion : ally;
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(target.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(0, target.Damage);
        Assert.Equal(1, (minionTarget ? ally : minion).Damage);
    }
}
