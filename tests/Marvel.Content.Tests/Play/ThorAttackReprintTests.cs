using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorAttackReprintTests
{
    [Theory]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void ChaseThemDownRequiresHeroDefeatAndRemovesTwoThreat(bool allyAttack, bool lethal, bool labelled)
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var chase = world.CreateCard("06013", world.Seats[0].Hand);
        var enemy = world.CreateCard("01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        if (lethal) enemy.TakeDamage(DamagePlacement.Health(world, world.Facts, enemy) - 1);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 4);
        if (allyAttack)
        {
            var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            AllyBasicPowers.AllyPower(world, world.Facts, ally, enemy, BasicPowers.AttackVerb, []);
        }
        else if (labelled)
        {
            var eventCard = world.CreateCard("01005", world.Seats[0].Hand);
            var energy = world.CreateCard("06022", world.Seats[0].Hand);
            var physical = world.CreateCard("01003", world.Seats[0].Hand);
            var action = Assert.Single(runner.Actions(world, 0), a => a.Card == eventCard.ObjectId);
            runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId, physical.ObjectId], [enemy.ObjectId]));
        }
        else BasicPowerInitiation.BasicAttack(world, world.Facts, 0, enemy, []);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        if (allyAttack || !lethal)
        {
            Assert.True(prompt is null || prompt.Affordances.All(a => a.AnchorId != chase.ObjectId));
            Assert.Equal(4, scheme.Tokens["k_threat"]);
            return;
        }
        var response = Assert.Single(prompt!.Affordances, a => a.AnchorId == chase.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        var target = Sequence.Work(world, world.Facts, runner, []);
        if (target is not null) Sequence.Answer(world, world.Facts, runner, target, Decision.Take(scheme.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(2, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, chase.Area.Type);
    }
}
