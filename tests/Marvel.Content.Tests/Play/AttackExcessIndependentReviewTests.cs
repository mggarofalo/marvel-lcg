using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic local responses and attacks; this does not admit expansion content.
public sealed class AttackExcessIndependentReviewTests
{
    [Theory]
    [InlineData(2, 1)]
    [InlineData(4, -1)]
    [Rule("rr:overkill.1")]
    [Rule("rr:overkill.3")]
    [Rule("rr:prevent.1.1")]
    public void PartialPreventionChangesLethalityAndHealing(int prevention, int healing)
    {
        // rr:prevent.1.1: prevention reduces damage taken. rr:overkill.3 counts
        // the same excess as overkill, which requires defeat under rr:overkill.1.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"12030","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"you","target":"enemy","form":"hero"},
              "when":{"wasDefeated":"trigger.target"},
              "effect":{"heal":{"card":"you","amount":{"attackExcessDamage":"trigger.target"}}}
            }]},{"card":"01003","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"you","target":"enemy","form":"hero"},
              "effect":{"placeCounters":{"card":"you","counter":"control","count":1}}
            }]}]}
            """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(4);
        long attack = StateFields.Modified(world, hero, "attack", world.Facts, world.Players);
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 5 - attack, Affects: hero.ObjectId));
        var enemy = world.CreateCard("01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new(EffectSource.LastingEffect, "health", 10, Affects: enemy.ObjectId));
        enemy.TakeDamage(DamagePlacement.Health(world, world.Facts, enemy) - 2);
        world.Effects.Register(new(EffectSource.LastingEffect, "preventDamage", prevention,
            Affects: enemy.ObjectId, Lasts: Duration.NextUses(1)));
        var moment = world.CreateCard("12030", world.Seats[0].Hand);
        var control = world.CreateCard("01003", world.Seats[0].Hand);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, enemy, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Contains(prompt.Affordances, option => option.AnchorId == control.ObjectId);
        if (healing < 0)
        {
            Assert.DoesNotContain(prompt.Affordances, option => option.AnchorId == moment.ObjectId);
            Assert.Equal(DeckType.EngagedEnemiesArea, enemy.Area.Type);
            Assert.Equal(4, hero.Damage);
            return;
        }
        Assert.Equal(DeckType.EncounterDiscardPile, enemy.Area.Type);
        var response = Assert.Single(prompt.Affordances, option => option.AnchorId == moment.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        Assert.Equal(4 - healing, hero.Damage);
        Assert.Equal(DeckType.DiscardPile, moment.Area.Type);
    }

    [Fact]
    [Rule("rr:overkill.3")]
    public void RepeatedAttackDamageHealsOnlyTheLastPlacementsExcess()
    {
        // rr:overkill.3 counts "the same value of excess damage" as overkill.
        // The first placement is nonlethal; only three from the second is excess.
        string body = LabelledPowerActivationFixture.Body.Replace(
            "{\"enemyAttacks\":{\"enemies\":\"chosen\"}}",
            "{\"dealAttackDamage\":{\"cards\":\"chosen\",\"amount\":5}}", StringComparison.Ordinal);
        var f = new LabelledPowerActivationFixture(villain: false, body: body, includeMoment: true);
        var hero = f.World.Seats[1].IdentityCard;
        hero.TakeDamage(4);
        f.Enemy.TakeDamage(DamagePlacement.Health(f.World, f.World.Facts, f.Enemy) - 7);
        var moment = f.World.CreateCard("12030", f.World.Seats[1].Hand);
        f.Start();
        Assert.Equal(DeckType.EncounterDiscardPile, f.Enemy.Area.Type);
        var response = Assert.Single(f.Pending!.Affordances, option => option.AnchorId == moment.ObjectId);
        f.Answer(Decision.Take(response.Id));
        Assert.Equal(1, hero.Damage);
        Assert.Equal(DeckType.DiscardPile, moment.Area.Type);
        f.Finish();
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:damage.step.6")]
    [Rule("rr:overkill.1")]
    public void WouldDefeatHealingChangesResponseEligibility(bool saveEnemy)
    {
        // rr:damage.step.6 opens would-be-defeated interrupts before defeat;
        // rr:overkill.1 requires that the enemy actually be defeated.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01092","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt","subject":"game"},
              "effect":{"choose":{"options":[{"heal":{"card":"trigger.subject","amount":20}},{"draw":{"player":"you","count":1}}]}}
            }]},{"card":"12030","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"you","target":"enemy","form":"hero"},
              "when":{"wasDefeated":"trigger.target"},
              "effect":{"heal":{"card":"you","amount":{"attackExcessDamage":"trigger.target"}}}
            }]},{"card":"01003","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"you","target":"enemy","form":"hero"},
              "effect":{"placeCounters":{"card":"you","counter":"control","count":1}}
            }]}]}
            """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(4);
        long attack = StateFields.Modified(world, hero, "attack", world.Facts, world.Players);
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 5 - attack, Affects: hero.ObjectId));
        var enemy = world.CreateCard("01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new(EffectSource.LastingEffect, "health", 10, Affects: enemy.ObjectId));
        enemy.TakeDamage(DamagePlacement.Health(world, world.Facts, enemy) - 2);
        world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var moment = world.CreateCard("12030", world.Seats[0].Hand);
        var control = world.CreateCard("01003", world.Seats[0].Hand);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, enemy, []);
        var choice = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(Question.Option, choice.Asking);
        Sequence.Answer(world, world.Facts, runner, choice,
            Decision.Take(choice.Affordances[saveEnemy ? 0 : 1].Id), []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Contains(prompt.Affordances, option => option.AnchorId == control.ObjectId);
        if (saveEnemy)
        {
            Assert.Equal(DeckType.EngagedEnemiesArea, enemy.Area.Type);
            Assert.DoesNotContain(prompt.Affordances, option => option.AnchorId == moment.ObjectId);
            Assert.Equal(4, hero.Damage);
            return;
        }
        Assert.Equal(DeckType.EncounterDiscardPile, enemy.Area.Type);
        var response = Assert.Single(prompt.Affordances, option => option.AnchorId == moment.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        Assert.Equal(1, hero.Damage);
        Assert.Equal(DeckType.DiscardPile, moment.Area.Type);
    }
}


