using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreAttackCompletionTests
{
    [Rule("rr:attack-enemy-activation.step.6")]
    [Rule("rr:defend-defense.2")]
    [Rule("rr:defend-defense.6")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MinionAttackCompletionNamesItsEstablishedDefenderEvenWhenNoHealthChanges(bool defend)
    {
        // "The attack ends." A hero's DEF reduces the damage, while choosing
        // no defending character makes the attack "considered undefended".
        // Canonical Core state: Shocker is engaged and Spider-Man is ready.
        Card? minion = null;
        var (_, world) = Playing(board =>
        {
            minion = Assert.Single(board.Cards, card => card.FaceId == AuthoredCards.Shocker);
            World.MoveToTop(minion, board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true);
        var events = new List<GameEvent>();
        var abilities = new NoCardAbilities();
        Card hero = world.Seats[0].IdentityCard;
        world.Agenda.Add(new PhaseStep(Steps.Attack, 1, 2,
            Subject: minion!.ObjectId, Seat: 0));
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, abilities, events));
        Sequence.Answer(world, world.Facts, abilities, prompt,
            defend ? Decision.Take(hero.ObjectId) : Decision.Decline, events);
        Sequence.Finish(world, world.Facts, abilities, events);

        AttackCompleted completed = Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(minion.ObjectId, completed.Enemy);
        Assert.Equal(hero.ObjectId, completed.Target);
        Assert.Equal(defend ? hero.ObjectId : -1, completed.Defender);
        Assert.Equal(defend ? 0 : hero.Damage, completed.DamageDealt);
        // Event verbs are engine-chosen wire metadata. Completion does not
        // add a second attack effect to authority-derived attack transcripts.
        Assert.Equal("Attack_Completed", completed.Verb);
        Assert.Equal("Shocker", completed.Subjects![completed.Enemy]);
        Assert.Equal("Spider-Man", completed.Subjects[completed.Target]);
        Assert.Equal(!defend, hero.Ready);
        if (defend)
        {
            Assert.Equal(0, hero.Damage);
            Assert.DoesNotContain(events.OfType<FieldSet>(), happened => happened.Field == "health");
        }
        else
        {
            Assert.True(hero.Damage > 0);
        }
        Attack.End(world, events);
        Assert.Single(events.OfType<AttackCompleted>());
    }

    [Rule("rr:retaliate-x")]
    [Rule("rr:leaves-play.1")]
    [Fact]
    public void RetaliateDefeatedDroneCompletionNamesTheAttackerAtItsOccurrence()
    {
        // "After a character with the retaliate X keyword is attacked, deal
        // X damage to the attacker." Leaving play makes the player card a
        // "new copy"; it does not rewrite the Drone that attacked Black Panther.
        Card? drone = null;
        var (_, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01040a");
            drone = FacedownMinions.EngageTop(board, 0, Marvel.Content.Tests.Cards.AuthoredCards.DroneProfile, "test", "test", []);
        }, heroes: ["black_panther"], scenario: "ultron");
        var events = new List<GameEvent>();
        var abilities = new NoCardAbilities();
        world.Agenda.Add(new PhaseStep(Steps.Attack, 1, 2,
            Subject: drone!.ObjectId, Seat: 0));
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, abilities, events));

        Occurrence owner = Assert.IsType<Occurrence>(Assert.Single(world.Agenda.Outstanding,
            step => step.What == Steps.EndAttack).ProcedureOwnerOccurrence);
        Assert.Equal(CardKind.Minion, owner.ActorFacts!.Kind);
        Assert.Equal(0, owner.ActorFacts.Owner);
        Assert.Empty(owner.Conditions);
        string preview = Damage.PreviewAttack(world, world.Facts, drone, drone,
            world.Seats[0].IdentityCard, 1);
        Assert.Contains("Retaliate 1 will hit Drone", preview);

        Sequence.Answer(world, world.Facts, abilities, prompt, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, abilities, events);

        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, drone.Area.Type);
        Assert.True(drone.FaceUp);
        Assert.Equal(CardKind.Minion, owner.ActorFacts.Kind);
        AttackCompleted completed = Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(drone.ObjectId, completed.Enemy);
        Assert.Equal("Drone", completed.Subjects![drone.ObjectId]);
        Assert.Equal("Black Panther", completed.Subjects[completed.Target]);
        Assert.Equal(-1, completed.Defender);
        Assert.Equal(1, completed.DamageDealt);
    }

    [Rule("rr:damage.3.2")]
    [Rule("rr:player-elimination")]
    [Fact]
    public void LethalCoreAttackRecordsFullDamageAfterEliminationEndsTheAttack()
    {
        // "The amount of damage dealt is not modified" when damage taken is
        // modified. Core Shocker deals two even with Spider-Man at one HP.
        Card? minion = null;
        var (_, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TakeDamage(9);
            minion = Assert.Single(board.Cards, card => card.FaceId == AuthoredCards.Shocker);
            World.MoveToTop(minion, board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true);
        var events = new List<GameEvent>();
        var abilities = new NoCardAbilities();
        world.Agenda.Add(new PhaseStep(Steps.Attack, 1, 2, Subject: minion!.ObjectId, Seat: 0));
        Prompt defense = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, abilities, events));
        Sequence.Answer(world, world.Facts, abilities, defense, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, abilities, events);

        Assert.True(world.Seats[0].Eliminated);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        Assert.Contains(events.OfType<FieldSet>(), change => change.Field == "health" && change.From == 1 && change.To == 0);
    }

    [Rule("rr:activation.6")]
    [Fact]
    public void SyntheticDepartureBeforeDamageRetainsTheCapturedDroneActor()
    {
        // "If an activating minion leaves play, that minion's activation
        // ends immediately and no further steps of that activation resolve."
        // Synthetic departure during a real Core Drone attack: this exercises
        // the continuation contract, not a claimed printed Core card ability.
        Card? drone = null;
        var (_, world) = Playing(board =>
            drone = FacedownMinions.EngageTop(board, 0, Marvel.Content.Tests.Cards.AuthoredCards.DroneProfile, "test", "test", []),
            hero: true, scenario: "ultron");
        var events = new List<GameEvent>();
        var abilities = new NoCardAbilities();
        world.Agenda.Add(new PhaseStep(Steps.Attack, 1, 2,
            Subject: drone!.ObjectId, Seat: 0));
        Prompt defense = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, abilities, events));
        Sequence.Answer(world, world.Facts, abilities, defense, Decision.Decline, events);

        Discard.Card(world, drone, "synthetic departure", events);
        Sequence.Finish(world, world.Facts, abilities, events);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        AttackCompleted completed = Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal("Drone", completed.Subjects![drone.ObjectId]);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, completed.Target);
        Assert.Equal(-1, completed.Defender);
        Assert.Equal(0, completed.DamageDealt);
    }
}
