using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreHiddenChoiceMeaningTests
{
    [Fact]
    public void DroneTargetAndIncomingAttackUseItsPublicIdentity()
    {
        // Ultron Drones 01140 gives facedown player cards their Drone identity.
        // Swinging Web Kick 01005 can choose an enemy, including that minion.
        Card? drone = null;
        Card? kick = null;
        Card? genius = null;
        Card? energy = null;
        var (_, world) = Playing(board =>
        {
            drone = board.CreateCard("01091", board.Seats[0].Deck);
            FacedownDrones.EngageTop(board, 0, "test", "Create_Drone", []);
            kick = board.CreateCard(AuthoredCards.SwingingWebKick, board.Seats[0].Hand);
            genius = board.CreateCard("01089", board.Seats[0].Hand);
            energy = board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), offered => offered.Card == kick!.ObjectId);
        var events = runner.Act(world, action, [genius!.ObjectId, energy!.ObjectId], []).ToList();
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Affordance target = Assert.Single(prompt.Affordances, offered => offered.AnchorId == drone!.ObjectId);

        Assert.Equal(FacedownDrones.EffectiveFaceId, target.Label);
        Assert.Equal("Drone", target.DisplayLabel);
        Assert.Equal("Attack Drone", target.CommitLabel);
        Assert.DoesNotContain("The Power of Protection", target.CommitLabel);
        Assert.StartsWith("Drone", target.Description);
        Assert.DoesNotContain("The Power of Protection", target.Description);
        Assert.DoesNotContain("01091", target.Label);

        Card hero = world.Seats[0].IdentityCard;
        world.Attack = new EnemyAttack(drone!.ObjectId, 0, hero.ObjectId);
        world.Activation = new EnemyActivation(drone.ObjectId, 0, Attacking: true);
        Prompt defense = Assert.IsType<Prompt>(Attack.DeclareDefender(world, world.Facts, new NoCardAbilities()));

        Assert.StartsWith("Drone is attacking Spider-Man", defense.Description);
        Assert.DoesNotContain("The Power of Protection", defense.Description);
    }
}
