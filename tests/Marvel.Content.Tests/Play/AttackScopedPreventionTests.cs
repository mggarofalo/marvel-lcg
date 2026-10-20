using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AttackScopedPreventionTests
{
    [Fact]
    public void PreventionCannotBeBoughtWithoutAnUnpreventedAttack()
    {
        var runner = Runner("01006", "Action", """{"preventAttackDamage":"attack"}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "01006");
        Assert.DoesNotContain(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Attack = new(villain.ObjectId, 0, world.Seats[0].IdentityCard.ObjectId)
            { PreventAllDamage = true };
        Assert.DoesNotContain(runner.Actions(world, 0), option => option.Card == source.ObjectId);
    }

    [Fact]
    [Rule("rr:prevent.1.3")]
    public void PreventionSurvivesSourceDepartureAndDoesNotAffectOtherDamage()
    {
        // "The attacking character is considered to have dealt damage", but has not attacked and damaged.
        // Synthetic ability isolates the printed return cost from attack-scoped prevention.
        var runner = Runner("01006", "Action", """{"preventAttackDamage":"attack"}""",
            cost: """{"returnToHand":"this"}""");
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var source = InPlay(world, "01006");
        var identity = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Activation = new(villain.ObjectId, 0, Attacking: true);
        world.Attack = new(villain.ObjectId, 0, identity.ObjectId,
            CalculatedDamage: 5);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
        Assert.True(world.Attack.PreventAllDamage);
        var events = new List<GameEvent>();
        DamagePlacement.Resolve(world, world.Facts,
            new(villain, identity, 1, "Boost", "Damage"), events);
        Assert.Equal(1, identity.Damage);
        Attack.DealDamage(world, world.Facts, events);
        Assert.Equal(1, identity.Damage);
        Assert.False(world.Attack.Damaged);
        Assert.Equal(5, world.Activation.DamageDealt);
        world.Attack = new(villain.ObjectId, 0, identity.ObjectId, CalculatedDamage: 2);
        Attack.DealDamage(world, world.Facts, events);
        Assert.Equal(3, identity.Damage);
    }

    [Fact]
    [Rule("rr:damage.step.3")]
    public void PreventionFollowsAnAllyDefenderAndToughStillHasPriority()
    {
        // Step 3 prevention changes damage taken, after Tough, without changing damage dealt.
        var (_, world) = Playing(_ => { }, hero: true);
        var identity = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Activation = new(villain.ObjectId, 0, Attacking: true);
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.Attack = new(villain.ObjectId, 0, identity.ObjectId, CalculatedDamage: 8);
        Attack.PreventDamage(world);
        world.Attack = world.Attack with { Target = ally.ObjectId, Defender = ally.ObjectId };
        var events = new List<GameEvent>();
        Statuses.Give(world, ally, Statuses.Tough);
        Attack.DealDamage(world, world.Facts, events);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(0, identity.Damage);
        Assert.False(Statuses.Has(world, ally, Statuses.Tough));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
    }

    [Fact]
    public void IndirectAttackPreventsEachAssignedShare()
    {
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var identity = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.Agenda.Abandon();
        world.Activation = new(villain.ObjectId, 0, Attacking: true);
        world.Attack = new(villain.ObjectId, 0, identity.ObjectId, CalculatedDamage: 3, Indirect: true)
            { PreventAllDamage = true };
        world.Agenda.Add(new PhaseStep(new AttackStageOperation(AttackStageKind.DealAttackDamage),
            Round: 1, Number: 5, Subject: villain.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var assign = Sequence.Work(world, world.Facts, runner, events)!;
        var option = Assert.Single(assign.Affordances);
        Sequence.Answer(world, world.Facts, runner, assign,
            Decision.Take(option.Id, [identity.ObjectId, identity.ObjectId, ally.ObjectId], []), events);
        while (Sequence.Work(world, world.Facts, runner, events) is { } pending)
        {
            Assert.True(pending.Cancellable);
            Assert.Contains("All damage from this attack is prevented", pending.Description);
            Assert.DoesNotContain("→", pending.Description);
            Sequence.Answer(world, world.Facts, runner, pending, Decision.Decline, events);
        }
        Assert.Equal(0, identity.Damage);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(3, world.Activation.DamageDealt);
    }
}
