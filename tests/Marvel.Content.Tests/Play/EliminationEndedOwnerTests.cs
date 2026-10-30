using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EliminationEndedOwnerTests : CoreCardTestBase
{
    private const int Activation = 4;

    [Theory]
    [InlineData("eliminated", true)]
    [InlineData("seat not eliminated", false)]
    [InlineData("activation still active", false)]
    [InlineData("another effect is current", false)]
    [InlineData("owner has no activation", false)]
    [InlineData("owner has no seat", false)]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:player-elimination.5.1")]
    public void OnlyAnAttackEndedByEliminatingItsSeatResumesAfterTheEffectWindows(string board, bool released)
    {
        // rr:player-elimination.5.1: "If a player is eliminated in the middle of an attack,
        // that attack ends." Only that ending explains a missing owner; any other absence
        // stays an engine fault, and the ability resumes behind its own effect windows.
        var world = new World(Cards, players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01010a,01010b", seat.Hero);
        }
        var crossbones = world.CreateCard("04058", world.AreaOf(DeckType.VillainArea));
        var gun = world.CreateCard("04064", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, crossbones.ObjectId));
        var owner = new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Index: 0, Subject: crossbones.ObjectId, Seat: board == "owner has no seat" ? -1 : 0)
        {
            ActivationId = board == "owner has no activation" ? -1 : Activation,
        };
        world.Agenda.Add(new PhaseStep(new CompletedCardEffectOperation(), 1, 2));
        CardEffectWindows.Begin(world.Agenda,
            board == "another effect is current" ? crossbones.ObjectId : gun.ObjectId, 0);
        world.Seats[0].Eliminated = board != "seat not eliminated";
        if (board == "activation still active")
            world.Activation = new EnemyActivation(crossbones.ObjectId, 0, true, Activation);

        var continuation = new PhaseStep(new CompletedCardEffectOperation(), 1, 3, Subject: gun.ObjectId);
        int before = world.Agenda.Count;
        void Continue() => AbilityResolutionEffectChoices.ContinueAfterEliminationEndedOwner(
            world, owner, gun.ObjectId, continuation);

        if (!released)
        {
            var fault = Assert.Throws<InvalidOperationException>(Continue);
            Assert.Equal("the continuation owner is not on the agenda", fault.Message);
            Assert.Equal(before, world.Agenda.Count);
            return;
        }
        Continue();
        // The continuation follows the ability's own effect windows, ahead of other work.
        Assert.Equal(before + 1, world.Agenda.Count);
        Assert.Equal([Steps.CardEffectResolved, Steps.CardEffectResolved, Steps.CardEffectResolved],
            world.Agenda.Outstanding.Select(step => step.What));
        Assert.Equal(3, world.Agenda.Outstanding[1].Number);
    }
}
