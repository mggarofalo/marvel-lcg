using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkCustomizationArmorTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [Rule("rr:defend-defense.7")]
    public void CompletedDefenseDamagesCausalMinionWithOrWithoutAttackDamage(bool defend, bool damage)
    {
        // "Abilities that trigger after a character defends an attack resolve after that attack ends."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var hero = world.Seats[1].IdentityCard;
        var source = world.CreateCard("10031", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 0));
        var enemy = world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", damage ? 3 : -10, Affects: enemy.ObjectId));
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false }, 1, 2,
            Subject: enemy.ObjectId, Seat: 1));
        bool offered = false;
        for (int i = 0; i < 30; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            var response = prompt.Affordances.SingleOrDefault(a => a.AnchorId == source.ObjectId);
            Decision answer = Decision.Decline;
            if (response is not null)
            {
                Assert.True(defend);
                Assert.Equal(1, prompt.Player);
                Assert.Single(events.OfType<AttackCompleted>());
                offered = true;
                answer = Decision.Take(response.Id);
            }
            else if (defend && prompt.Asking == Question.Defender && prompt.Player == 1)
                answer = Decision.Take(hero.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt, answer, events);
        }
        Assert.Equal(defend, offered);
        Assert.Equal(defend ? 1 : 0, enemy.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(source.Ready);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.IsOver);
    }
}
