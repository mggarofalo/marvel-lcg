using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.BasicAttackOverkillFixture;

namespace Marvel.Content.Tests.Play;

public sealed class BasicAttackOverkillReviewTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:overkill.1")]
    public void ReflectedAttackNeverSpillsFromTheUndefeatedMinion(bool reflect, bool prevent)
    {
        // "If this attack defeats a minion" excludes a minion whose damage is reflected.
        // Synthetic attachment hosts a minion to exercise the existing reflection mechanism.
        var runner = Runner(extra: """
            ,{"card":"04104","abilities":[{
              "trigger":{"event":"WhenAttackInitiated","timing":"ForcedInterrupt","target":"attachedTo","player":"trigger.player"},
              "effect":{"seq":[{"reflectAttackDamage":"attack"},{"discard":"this"}]}
            }]},
            {"card":"01092","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"you"},
              "effect":{"choose":{"options":[{"preventDamage":"trigger.target"},{"draw":{"player":"you","count":1}}]}}
            }]}
            """);
        var (world, source, physical, other, minion) = Board(runner);
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "health", 20, Affects: hero.ObjectId));
        if (reflect)
            world.CreateCard("04104", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, minion.ObjectId));
        InPlay(world, "01092");
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, minion, []);
        Pay(world, runner, source, physical, other);
        if (reflect)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, [])!;
            Assert.Equal(0, minion.Damage);
            Assert.Equal(0, hero.Damage);
            Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
            Sequence.Answer(world, world.Facts, runner, prompt,
                Decision.Take(prompt.Affordances[prevent ? 0 : 1].Id), []);
        }
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(reflect ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(reflect && !prevent ? 12 : 0, hero.Damage);
        Assert.Equal(reflect ? 0 : 9, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Null(world.Agenda.PlayerAttackScope());
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:overkill.1")]
    [Rule("rr:damage.step.6")]
    public void WouldDefeatHealingSuppressesSpillAfterLethalDamage(bool heal)
    {
        // "If this attack defeats a minion" is checked after "would be defeated" interrupts.
        // Synthetic forced choice separates lethal damage from actual defeat.
        var runner = Runner(extra: """
            ,{"card":"01092","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt","subject":"game"},
              "effect":{"choose":{"options":[{"heal":{"card":"trigger.subject","amount":20}},{"draw":{"player":"you","count":1}}]}}
            }]}
            """);
        var (world, source, physical, other, minion) = Board(runner);
        InPlay(world, "01092");
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, minion, []);
        Pay(world, runner, source, physical, other);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(12, minion.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(prompt.Affordances[heal ? 0 : 1].Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(heal ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(0, minion.Damage);
        Assert.Equal(heal ? 0 : 9, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }
}
