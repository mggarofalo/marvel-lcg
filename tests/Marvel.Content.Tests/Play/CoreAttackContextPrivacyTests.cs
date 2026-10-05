using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreAttackContextPrivacyTests
{
    [Rule("rr:damage.step.3")]
    [Fact]
    public void BackflipDuringADroneAttackNamesThePublicAttacker()
    {
        // Step 3 lets effects that "prevent damage" modify the damage taken.
        // Backflip (01003) prevents damage from an attack. Core Spider-Man's
        // actual starter card is in hand when the Ultron Drone attacks him.
        Card? drone = null;
        Card? backflip = null;
        var (_, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
            backflip = board.Cards.First(card => card.FaceId == AuthoredCards.Backflip);
            World.MoveToTop(backflip, board.Seats[0].Hand);
            drone = FacedownMinions.EngageTop(board, 0, Marvel.Content.Tests.Cards.AuthoredCards.DroneProfile, "test", "test", []);
        }, scenario: "ultron");
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(Steps.Attack, 1, 2, Subject: drone!.ObjectId, Seat: 0));
        Prompt defense = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Defender, defense.Asking);
        Sequence.Answer(world, world.Facts, runner, defense, Decision.Decline, events);

        Prompt prevent = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));

        Assert.Contains(prevent.Affordances, option => option.AnchorId == backflip!.ObjectId);
        Assert.Contains("Drone is attacking Spider-Man", prevent.Description);
        Sequence.Answer(world, world.Facts, runner, prevent,
            Decision.Take(Assert.Single(prevent.Affordances,
                option => option.AnchorId == backflip!.ObjectId).Id), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, backflip!.Area.Type);
        Assert.Contains(events.OfType<AttackCompleted>(), completed => completed.Enemy == drone.ObjectId);
    }
}
