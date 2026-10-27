using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.BasicAttackOverkillFixture;

namespace Marvel.Content.Tests.Play;

public sealed class BasicAttackOverkillContinuationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:overkill.1")]
    public void SuspendedDamageRetainsKeywordButRequiresActualDefeat(bool prevent)
    {
        // "If this attack defeats a minion" requires actual defeat after prevention.
        var runner = Runner(extra: """
            ,{"card":"01092","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"minion"},
              "effect":{"choose":{"options":[{"preventDamage":"trigger.target"},{"draw":{"player":"you","count":1}}]}}
            }]}
            """);
        var (world, source, physical, other, minion) = Board(runner);
        InPlay(world, "01092");
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, minion, []);
        Pay(world, runner, source, physical, other);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(0, minion.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(prompt.Affordances[prevent ? 0 : 1].Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(prevent ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(prevent ? 0 : 9, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:lasting-effects.1")]
    public void CanceledBasicAttackDoesNotLeaveItsReceiptOnTheNextAttack()
    {
        // A "specified duration" of one attack ends with that attack's cancellation.
        var runner = Runner(after: """{"cancelOccurrence":"trigger"}""");
        var (world, source, physical, other, minion) = Board(runner);
        var hero = world.Seats[0].IdentityCard;
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, minion, []);
        Pay(world, runner, source, physical, other);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(0, minion.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        hero.Refresh();
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, minion, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(2, minion.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Fact]
    public void QueriesDoNotGrantBonusOrKeywordOrChangeSubsequentExecution()
    {
        var runner = Runner();
        var probed = Board(runner);
        var control = Board(runner);
        BasicPowerInitiation.BasicAttack(probed.World, probed.World.Facts, 0, probed.Minion, []);
        BasicPowerInitiation.BasicAttack(control.World, control.World.Facts, 0, control.Minion, []);
        var prompt = Sequence.Work(probed.World, probed.World.Facts, runner, [])!;
        var otherPrompt = Sequence.Work(control.World, control.World.Facts, runner, [])!;
        var world = probed.World;
        var occurrence = world.Agenda.Occurrence!;
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var work = world.Agenda.Outstanding.ToArray();
        for (int index = 0; index < 3; index++)
        {
            var offer = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));
            Assert.Contains("overkill", runner.Describe(world, offer).Description, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(work, world.Agenda.Outstanding.ToArray());
            Assert.Equal(0, occurrence.BasicUse!.Bonus);
            Assert.False(occurrence.BasicUse.Overkill);
        }
        foreach (var setup in new[] { probed, control })
        {
            var current = setup.World == world ? prompt : otherPrompt;
            Sequence.Answer(setup.World, setup.World.Facts, runner, current,
                new(Assert.Single(current.Affordances).Id, [], [setup.Physical.ObjectId, setup.Other.ObjectId],
                    Allocations: [new(setup.Physical.ObjectId, 0, "RR"), new(setup.Other.ObjectId, 0, "R")]), []);
            Sequence.Finish(setup.World, setup.World.Facts, runner, []);
        }
        Assert.Equal(control.World.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.World.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(9, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Fact]
    public void NonAttackBasicUseCannotReceiveTheKeyword()
    {
        var use = new BasicUse(0, 0, "thwart");
        Assert.Throws<InvalidOperationException>(() => use.GrantOverkill());
        Assert.False(use.Overkill);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void DepartedPaymentSourceIsRejectedBeforeAnyBonusOrKeyword()
    {
        // "Abort this process without paying any costs" if the selected resources cannot pay.
        var runner = Runner();
        var (world, source, physical, other, minion) = Board(runner);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, minion, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        World.MoveToTop(physical, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, world.Facts, runner, prompt,
            new(Assert.Single(prompt.Affordances).Id, [], [physical.ObjectId, other.ObjectId],
                Allocations: [new(physical.ObjectId, 0, "RR"), new(other.ObjectId, 0, "R")]), []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        Assert.Equal(0, world.Agenda.Occurrence!.BasicUse!.Bonus);
        Assert.False(world.Agenda.Occurrence.BasicUse.Overkill);
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
    }
}
