using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AttackReplacementContinuationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:damage.step.1")]
    public void AttackWaitsForReplacementBeforeDamageAndActivationAccounting(bool indirect, bool prevent)
    {
        // "Abilities that trigger when [character] would be dealt any amount of damage."
        // Synthetic replacement text makes this first damage step await a real choice.
        var runner = ReplacementRunner();
        var (_, world) = Playing(board => InPlay(board, "01092"), hero: true, abilities: runner);
        if (indirect)
            world.CreateCard("01020", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<GameEvent>();
        long before = hero.Damage;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: villain.ObjectId, Seat: 0));
        var defender = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Defender, defender.Asking);
        Sequence.Answer(world, world.Facts, runner, defender, Decision.Decline, events);
        if (indirect) Attack.MakeIndirect(world);
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        if (indirect)
        {
            var assignment = Assert.Single(prompt.Affordances);
            Sequence.Answer(world, world.Facts, runner, prompt,
                Decision.Take(assignment.Id, [.. Enumerable.Repeat(hero.ObjectId, assignment.Targets!.Min)], []), events);
            prompt = Sequence.Work(world, world.Facts, runner, events)!;
        }
        long amount = world.Attack!.CalculatedDamage!.Value;
        Assert.Equal(Question.Option, prompt.Asking);
        Assert.Equal(before, hero.Damage);
        Assert.DoesNotContain(events, item => item is AttackCompleted);
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(prompt.Affordances[prevent ? 0 : 1].Id), events);
        Sequence.Finish(world, world.Facts, runner, events);

        Assert.Equal(before + (prevent ? 0 : amount), hero.Damage);
        // Prevention changes damage taken; it does not replace the amount dealt.
        Assert.Equal(amount, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }
    [Fact]
    [Rule("rr:damage.step.1")]
    public void DirectAttackReceiptWithholdsFactsUntilReplacementCompletes()
    {
        // Step one replacement resolves before the later steps can place damage.
        var runner = ReplacementRunner();
        var (_, world) = Playing(board => InPlay(board, "01092"), hero: true, abilities: runner);
        var target = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<GameEvent>();
        long before = target.Damage;
        var result = DamageAttacks.Resolve(world, world.Facts,
            new(villain, villain, target, 3, "test", "Attack", Retaliate: false), events);
        Assert.False(result.IsComplete);
        Assert.Throws<InvalidOperationException>(() => result.Dealt);
        Assert.Equal(before, target.Damage);
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Option, prompt.Asking);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(prompt.Affordances[1].Id), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.True(result.IsComplete);
        Assert.Equal(3, result.Dealt);
        Assert.Equal(3, result.Taken);
        Assert.Equal(before + 3, target.Damage);
        Assert.Same(target, Assert.Single(result.Characters));
    }
    [Fact]
    public void PendingAttackDoesNotDamageAReenteredRecipient()
    {
        // Engineering contract: queued work names the admitted in-play copy, not only its card ID.
        var runner = ReplacementRunner(anyTarget: true);
        var (_, world) = Playing(board => InPlay(board, "01092"), hero: true, abilities: runner);
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var target = world.CreateCard("01020", area);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<GameEvent>();
        int incarnation = target.Incarnation;
        var result = DamageAttacks.Resolve(world, world.Facts,
            new(villain, villain, target, 1, "test", "Attack"), events);
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;

        World.MoveToTop(target, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        World.MoveToTop(target, area);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "retaliate", Amount: 1, Affects: target.ObjectId));
        Assert.NotEqual(incarnation, target.Incarnation);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(prompt.Affordances[1].Id), events);
        Sequence.Finish(world, world.Facts, runner, events);

        Assert.True(result.IsComplete);
        Assert.Equal(0, target.Damage);
        Assert.Equal(0, result.Dealt);
        Assert.Empty(result.Characters);
        Assert.Equal(0, villain.Damage);
    }

    private static AbilityRunner ReplacementRunner(bool anyTarget = false) => new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01092","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"you"},
              "effect":{"choose":{"options":[
                {"preventDamage":"trigger.target"},
                {"draw":{"player":"you","count":1}}
              ]}}
            }]}]}
            """.Replace("\"target\":\"you\"", anyTarget ? "\"subject\":\"game\"" : "\"target\":\"you\"", StringComparison.Ordinal)));
}
