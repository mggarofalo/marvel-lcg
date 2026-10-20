using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HydraFlameSoldierTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void PrintedAttackAndBoostClausesDiscardSupportOnlyWhenUndefended(bool boost, bool defended)
    {
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var source = world.CreateCard("04145", world.AreaOf(boost ? DeckType.BoostingArea : DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01088", world.Seats[0].Deck);
        var support = world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner:0));
        var retained = world.CreateCard("01092", support.Area);
        var hero = world.Seats[0].IdentityCard;
        var attack = new EnemyAttack(source.ObjectId, 0, hero.ObjectId, defended ? hero.ObjectId : -1);
        if (boost)
        {
            world.Attack = attack;
            runner.ExecuteBoost(world, source, 0);
        }
        else
        {
            world.FinishedAttack = attack;
            var occurrence = new Occurrence(1, [Steps.AttackEnds], Subject: source.ObjectId, Actor: source.ObjectId, Target: hero.ObjectId, Player:0, ActorFacts: new OccurrenceCard(source.ObjectId, CardKind.Minion, -1, -1));
            var responses = runner.Waiting(world, occurrence, WindowKind.Response);
            if (responses.Count > 0)
            {
                var response = Assert.Single(responses);
                runner.ResolveWindow(world, occurrence, response, new Decision(response.Card, [], []));
            }
        }
        var choice = Sequence.Work(world, Cards, runner, []);
        if (!defended)
        {
            Assert.NotNull(choice);
            var option = Assert.Single(choice.Affordances, offered => offered.AnchorId == support.ObjectId);
            Sequence.Answer(world, Cards, runner, choice, Decision.Take(option.Id), []);
            Sequence.Finish(world, Cards, runner, []);
        }
        else Assert.Null(choice);
        Assert.Equal(defended ? DeckType.SupportsArea : DeckType.DiscardPile, support.Area.Type);
        Assert.Equal(DeckType.SupportsArea, retained.Area.Type);
    }
}


