using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class LabelledPowerActivationTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [Rule("rr:stun-stunned.6")]
    [Rule("rr:labeled-ability.6")]
    public void SyntheticEnemyAttackAndDamageRetainTheWholeLabel(bool enemyStunned, bool heroStunned)
    {
        // rr:stun-stunned.6: "discard the stunned status card instead."
        // rr:labeled-ability.6: "the entire ability (except for its costs) is canceled."
        // Synthetic authored event carrying the printed Toe to Toe composition (10015).
        var runner = Runner("04043", "Action", """
            {"chooseCard":{"from":{"query":"attackableEnemies"},"effect":{
              "attack":{"target":"chosen","effect":{"seq":[
                {"enemyAttacks":{"enemies":"chosen"}},
                {"dealAttackDamage":{"cards":"chosen","amount":5}}
              ]}}
            }}}
            """, includeAuthored: true);
        var world = Hero("01001a");
        world.Abilities = runner;
        var hero = world.Seats[0].IdentityCard;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var enemy = world.CreateCard("01103", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new(EffectSource.LastingEffect, "health", 10, Affects: enemy.ObjectId));
        if (enemyStunned) Statuses.Give(world, enemy, Statuses.Stunned);
        if (heroStunned) Statuses.Give(world, hero, Statuses.Stunned);
        var source = world.CreateCard("04043", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        for (int index = 0; index < 10; index++)
        {
            world.CreateCard("01089", world.Seats[0].Deck);
            world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        }
        var events = Resolve(runner, world, source, payment, enemy);
        Assert.Equal(heroStunned ? 0 : 5, enemy.Damage);
        Assert.Equal(enemyStunned || heroStunned ? 0 : 2, hero.Damage);
        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
        Assert.False(Statuses.Has(world, enemy, Statuses.Stunned));
        Assert.Equal(enemyStunned || heroStunned ? 0 : 1, events.OfType<AttackCompleted>().Count());
        Assert.Equal(0, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    private static List<GameEvent> Resolve(AbilityRunner runner, World world,
        Card source, Card payment, Card enemy)
    {
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId && option.Ordinal == 1);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])).Events.ToList();
        var prompt = Sequence.Work(world, Cards, runner, events);
        if (prompt?.Asking == Question.Element)
            Sequence.Answer(world, Cards, runner, prompt, Decision.Take(enemy.ObjectId), events);
        while ((prompt = Sequence.Work(world, Cards, runner, events)) is not null)
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        return events;
    }
}
