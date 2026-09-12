using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class AttackAHeroReducedToZeroTests : AttackTestBase
{
    [Rule("rr:damage.1")]
    [Rule("rr:hit-points.2.1")]
    [Rule("rr:player-elimination")]
    [Fact]
    public void AHeroReducedToZeroIsDefeatedAndTheirPlayerEliminated()
    {
        // "When a character has damage on it equal to or in excess of its hit
        // points, it is defeated", and `rr:hit-points.2.1`: "if a player's hit
        // point dial is reduced to zero, that player is defeated and eliminated
        // from the game."
        //
        // A hero left standing on 0 hit points is the dangerous board:
        // everything else about it is right.
        var printed = Printed(atk: 20, boost: 0);
        var world = Board(printed);
        var identity = world.Seats[0].IdentityCard;
        Finish(world, printed);
        Assert.True(world.Seats[0].Eliminated);
        // `rr:defeat.2` -- an identity is removed from the game, not discarded.
        Assert.Equal(DeckType.RemovedArea, identity.Area.Type);
        // `rr:player-elimination.4` -- "if all players are eliminated, the game
        // ends and the players lose." One player, so this is that.
        Assert.Equal(Outcome.PlayersLose, world.Result);
    }

    [Rule("rr:player-elimination.5.1")]
    [Rule("rr:damage.3.2")]
    [Fact]
    public void EliminatingTheAttackedPlayerEndsTheAttackImmediately()
    {
        // A second player keeps the game alive, making the attack cleanup
        // observable instead of having game-over abandon the entire agenda.
        var printed = Printed(atk: 20, boost: 0);
        var world = Board(printed, players: 2);
        var observer = new CombatWindowObserver();
        Finish(world, printed, observer);
        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.Seats[1].Eliminated);
        Assert.Null(world.Attack);
        Assert.Null(world.Activation);
        Assert.NotNull(world.FinishedAttack);
        Assert.True(world.FinishedAttack!.Damaged);
        // All twenty was dealt even though the identity could take only its
        // ten remaining hit points; modifying damage taken does not modify
        // damage dealt.
        Assert.Equal(20, Assert.Single(observer.CompletedActivations).DamageDealt);
        Assert.True(observer.SawDamageDealt);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:damage.1")]
    [Rule("rr:hit-points")]
    [Rule("rr:remaining-hit-points.1")]
    [Rule("rr:sustained-damage.1")]
    [Fact]
    public void HealthIsWhatIsLeftAfterDamage()
    {
        // "An identity's or villain's hit point dial represents their remaining
        // hit points." Its sustained damage is maximum minus that remaining
        // value: four damage changes nine remaining hit points to five. The
        // digest records `health` and no damage key, so it exposes the dial.
        var printed = Printed(atk: 1, boost: 0);
        var world = new World(printed, players: 1);
        world.CreateSeat("p0");
        var alterEgo = world.CreateCard("alter", world.Seats[0].Hero);
        long Health() => StateFields.For(alterEgo, printed, 1, inPlay: true, hasHeldPools: true, hasFirstPlayerToken: false, world: world)["health"];
        Assert.Equal(9, Health());
        alterEgo.TakeDamage(4);
        Assert.Equal(5, Health());
    }

    [Rule("rr:attack-enemy-activation.step.6")]
    [Fact]
    public void TheAttackIsOverWhenItsLastStepIsTaken()
    {
        var printed = Printed(atk: 1, boost: 0);
        var world = Board(printed);
        Finish(world, printed);
        Assert.Null(world.Attack);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:attack-enemy-activation.step.6.a")]
    [Rule("rr:attack-enemy-activation.7")]
    [Rule("rr:tough.3")]
    [Fact]
    public void AnAttackRecordsWhetherItLandedAndOnlyItsOwnWindowSeesIt()
    {
        // `.step.6.a` lists "after [character] attacks **and damages** ... you"
        // as a trigger of its own, so "it attacked" and "it landed" are two
        // facts -- and by the time those abilities run, the attack is over and
        // the damage is on a dial that had damage on it before. The attack
        // carries what it did.
        //
        // `rr:tough.3` is what pulls the two apart in an ordinary game, and it
        // is the second half of this test: a tough card absorbs the attack, and
        // a character who "is not considered to have taken damage" was not
        // damaged by it.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        Finish(world, facts);
        Assert.NotNull(world.FinishedAttack);
        Assert.True(world.FinishedAttack!.Damaged);
        // A second attack, absorbed. The record is the new attack's own -- the
        // first one's `true` must not still be standing when this window opens.
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Tough);
        world.Agenda.Add(new PhaseStep(Steps.Attack, Round: 2, Number: 2, Index: 0, Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0));
        Finish(world, facts);
        Assert.False(world.FinishedAttack!.Damaged);
    }

    [Rule("rr:status-cards.2")]
    [Fact]
    public void StunCancelsBeforeAttackInitiationAbilitiesCanTrigger()
    {
        // "Status card abilities have timing priority over all conflicting
        // triggered abilities." The stun cancels this attack, so an authored
        // "when the villain attacks" interrupt must never observe it.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, villain, Statuses.Stunned);
        var observer = new CombatWindowObserver();
        Finish(world, facts, observer);
        Assert.False(Statuses.Has(world, villain, Statuses.Stunned));
        Assert.False(observer.SawAttackInitiation);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Null(world.Attack);
        Assert.Null(world.Activation);
    }

    [Rule("rr:initiating-abilities.1")]
    [Rule("rr:initiating-abilities.2")]
    [Fact]
    public void AttackInitiationOpensOneInterruptAndOneResponseWindow()
    {
        var facts = Printed(atk: 1, boost: 0);
        var world = Board(facts);
        var observer = new CombatWindowObserver();
        Finish(world, facts, observer);
        Assert.Equal(1, observer.AttackInitiationInterrupts);
        Assert.Equal(1, observer.AttackInitiationResponses);
    }

    [Rule("rr:damage.step.4")]
    [Fact]
    public void DamageThatLandsCreatesTheDamageResponseCondition()
    {
        var facts = Printed(atk: 1, boost: 0);
        var world = Board(facts);
        var observer = new CombatWindowObserver();
        Finish(world, facts, observer);
        Assert.True(observer.SawDamageWouldBeDealt);
        Assert.True(observer.SawDamageDealt);
    }

    [Rule("rr:status-cards.2")]
    [Rule("rr:damage.step.2")]
    [Fact]
    public void ToughPreventionCreatesNoDamageDealtResponse()
    {
        // Tough sits after "would be dealt" effects and before the later
        // damage triggers. The interrupt therefore sees imminent damage, but
        // the response cannot say damage was dealt when Tough prevented it.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        var hero = world.Seats[0].IdentityCard;
        Statuses.Give(world, hero, Statuses.Tough);
        var observer = new CombatWindowObserver();
        Finish(world, facts, observer);
        Assert.True(observer.SawDamageWouldBeDealt);
        Assert.False(observer.SawDamageDealt);
        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
        Assert.Equal(0, hero.Damage);
    }

    [Rule("rr:attack-enemy-activation")]
    [Fact]
    public void AnAttackInProgressHasNoFinishedAttackToRead()
    {
        // The record belongs to one attack. While the next one is resolving
        // there is no finished attack, rather than the previous one's facts
        // sitting there looking current -- an interrupt on the second attack
        // that asked what the attack did would otherwise be answered about the
        // first.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        Finish(world, facts);
        Assert.NotNull(world.FinishedAttack);
        Attack.Initiate(world, facts, new PhaseStep(Steps.Attack, 2, 2, Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0), []);
        Assert.Null(world.FinishedAttack);
    }

    [Rule("rr:activation.6")]
    [Fact]
    public void AnAttackerThatLeavesPlayMidAttackStopsThere()
    {
        // "If an activating minion **leaves play**, that minion's activation
        // ends immediately and **no further steps of that activation
        // resolve**." An attack is six steps and any of them can be the one
        // that takes the attacker off the table -- an interrupt answering the
        // attack by defeating the thing making it is the ordinary case.
        //
        // Here the attack is begun and the attacker removed between its steps,
        // which is what such an interrupt would do. Nothing after it may
        // happen: no boost card off the encounter deck, and no damage.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        int deck = world.AreaOf(DeckType.EncounterDeck).Cards.Count;
        Attack.Initiate(world, facts, new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        World.MoveToTop(villain, world.AreaOf(DeckType.RemovedArea));
        Finish(world, facts);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(deck, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
    }

    [Rule("rr:defend-defense.7")]
    [Rule("rr:defend-defense.7.1")]
    [Rule("rr:attack-enemy-activation.6")]
    [Fact]
    public void AnAttackThatEndsEarlyRemainsDefendedAtItsAfterAttackWindow()
    {
        // "If an effect causes a defended attack to end before fully
        // resolving, the attack is still considered to have been defended."
        // Removing an activating minion after the defender is declared skips
        // its remaining activation steps, but the final attack occurrence must
        // retain the defense for after-defense abilities.
        var facts = Printed(atk: 3, boost: 0, def: 1);
        var world = Board(facts);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard("villain", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Agenda.Abandon();
        world.Agenda.Add(new PhaseStep(Steps.Attack, 1, 2, Subject: minion.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var abilities = new CombatWindowObserver();
        var defend = Sequence.Work(world, facts, abilities, events)!;
        Sequence.Answer(world, facts, abilities, defend, Decision.Take(world.Seats[0].IdentityCard.ObjectId), events);
        World.MoveToTop(minion, world.AreaOf(DeckType.EncounterDiscardPile));
        Sequence.Finish(world, facts, abilities, events);
        Assert.NotNull(world.FinishedAttack);
        Assert.True(world.FinishedAttack!.IsDefended);
        Assert.True(world.FinishedAttack.BasicDefense);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.VillainArea, villain.Area.Type);
        Assert.False(abilities.SawBoostCardsFlipped);
        Assert.False(abilities.SawDamageWouldBeDealt);
        Assert.True(abilities.SawAttackEnds);
    }

    [Rule("rr:attack-enemy-activation")]
    [Rule("rr:attack-enemy-activation.step.1")]
    [Fact]
    public void OneAttackCanResolveAgainstBothHeroesWithOneBoostAndOneCompletion()
    {
        // Whirlwind 01130: "also resolve his attack against each other hero."
        // "His attack" is the attack already initiating, not another attack:
        // its one boost card is reused and its activation completes once.
        var facts = Printed(atk: 2, boost: 1);
        var world = Board(facts, players: 2);
        var abilities = new CompletionRecorder();
        Attack.AlsoResolveAgainstEachOtherHero(world);
        Assert.Equal(2, world.Agenda.Count); // one attack root and its sentinel
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, facts, abilities, events);
        while (asked is not null)
        {
            Sequence.Answer(world, facts, abilities, asked, Decision.Decline, events);
            asked = Sequence.Work(world, facts, abilities, events);
        }

        Assert.Equal([3L, 3L], world.Seats.Select(seat => seat.IdentityCard.Damage));
        Assert.Single(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        var result = Assert.Single(abilities.Results);
        Assert.True(result.Made);
        Assert.Equal(6, result.DamageDealt);
    }

    [Rule("rr:defend-defense.2")]
    [Rule("rr:attack-enemy-activation.step.4")]
    [Fact]
    public void AdditionalHeroesResolveInSeatOrderWithTheirOwnDefenderWindows()
    {
        // The engine chooses seat order as its deterministic player order.
        // Each hero gets step 2 and a fresh damage calculation, while the
        // already-flipped boost remains bounded to the one attack.
        var facts = Printed(atk: 2, boost: 1, def: 1);
        var world = Board(facts, players: 3);
        var abilities = new CompletionRecorder();
        Attack.AlsoResolveAgainstEachOtherHero(world);
        var events = new List<GameEvent>();
        var first = Sequence.Work(world, facts, abilities, events)!;
        Assert.Equal(0, first.Player);
        Sequence.Answer(world, facts, abilities, first, Decision.Decline, events);
        var second = Sequence.Work(world, facts, abilities, events)!;
        Assert.Equal(1, second.Player);
        Sequence.Answer(world, facts, abilities, second, Decision.Take(world.Seats[1].IdentityCard.ObjectId), events);
        var third = Sequence.Work(world, facts, abilities, events)!;
        Assert.Equal(2, third.Player);
        Sequence.Answer(world, facts, abilities, third, Decision.Decline, events);
        Sequence.Finish(world, facts, abilities, events);
        Assert.Equal([3L, 2L, 3L], world.Seats.Select(seat => seat.IdentityCard.Damage));
        Assert.False(world.Seats[1].IdentityCard.Ready);
        Assert.Equal(world.Seats.Select(seat => seat.IdentityCard.ObjectId), events.OfType<FieldSet>().Where(change => change.Field == "health").Select(change => change.Card));
        Assert.Equal(8, Assert.Single(abilities.Results).DamageDealt);
    }
}
