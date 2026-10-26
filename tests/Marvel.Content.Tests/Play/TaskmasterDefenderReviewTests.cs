using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class TaskmasterDefenderReviewTests
{
    [Fact]
    [Rule("rr:attack-enemy-activation.step.2")]
    [Rule("rr:lasting-effects.1")]
    [Rule("rr:lasting-effects.3")]
    public void ActualBoostCapturesTheDefendingSeatAndOutlivesItsDiscardedSource()
    {
        // "the defending player becomes the target player for the attack."
        // A lasting effect continues "whether or not the card that created" it is in play.
        var world = WidowEncounterFixture.Board();
        var runner = WidowEncounterFixture.Runner();
        var f = new RedSkullFixture(world, runner);
        WidowEncounterFixture.Upgrade(world, "01057");
        WidowEncounterFixture.Upgrade(world, "01057");
        WidowEncounterFixture.Upgrade(world, "01057", 0);
        world.CreateCard("08026", world.AreaOf(DeckType.BoostCardsDeck,
            f.Villain.Area.PlayArea, host: f.Villain.ObjectId));
        world.CreateCard("01173", world.AreaOf(DeckType.EncounterDeck));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: f.Villain.ObjectId, Seat: 1));
        var events = new List<GameEvent>();
        bool defended = false;
        for (int decisions = 0; decisions < 30; decisions++)
        {
            Prompt? prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            Decision answer = Decision.Decline;
            if (prompt.Asking == Question.Defender)
            {
                answer = Decision.Take(world.Seats[0].IdentityCard.ObjectId);
                defended = true;
            }
            Sequence.Answer(world, world.Facts, runner, prompt, answer, events);
        }
        Assert.True(defended);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
    }
}
