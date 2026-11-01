using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class VillainPhaseFocusTests : CoreCardTestBase
{
    [Theory]
    [InlineData(ThreatCause.VillainPhase, 1, true)]
    [InlineData(ThreatCause.CardAbility, 1, false)]
    [InlineData(ThreatCause.VillainPhase, -1, false)]
    [Rule("rr:villain-phase.step.1")]
    public void AnAreasStepOnePlacementNamesItsEarliestSeat(ThreatCause cause, int seat, bool named)
    {
        // Ruling 97d62cdb9ce436a7: "each player should place threat on their main scheme in turn order".
        var (world, scheme) = Separated();
        world.Agenda.Add(new PhaseStep(new ThreatPlacementOperation(
            new ThreatPlacement(scheme.ObjectId, -1, 1, cause, "villain phase, place threat", seat),
            new EffectCompletion(1, 1), 0), 1, 1, Seat: seat));
        EndOfPhase(world);

        var focus = VillainPhaseFocus.Of(world);

        Assert.Equal(named ? new VillainPhaseFocus(1, seat, world.GameAreaOf(PlayArea.Of(seat))!.Id) : null, focus);
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(2, false)]
    [Rule("rr:villain-phase.step.4")]
    public void ADealtRevealNamesItsSeatOnlyInStepFour(int number, bool named)
    {
        var (world, _) = Separated();
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard), 1, number,
            Subject: 0, Seat: 0));
        EndOfPhase(world);

        Assert.Equal(named ? new VillainPhaseFocus(4, 0, world.GameAreaOf(PlayArea.Of(0))!.Id) : null,
            VillainPhaseFocus.Of(world));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [Rule("rr:villain-phase.step.2")]
    public void AnActivationNamesItsSeatUntilAHeadingOrThePhaseEndComesFirst(bool headingFirst, bool phaseEnds)
    {
        var (world, _) = Separated();
        if (headingFirst) world.Agenda.Add(new PhaseStep(new ActivationPlanOperation(), 1, 2, Plan: true));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Index: 1, Subject: 0, Seat: 1));
        if (phaseEnds) EndOfPhase(world);

        Assert.Null(VillainPhaseFocus.Of(world));
    }

    [Fact]
    [Rule("rr:villain-phase.step.2")]
    public void AnAttackOutsideStepTwoNamesNoActivatingSeat()
    {
        // An attack a revealed card causes is not that seat's step-2 activation.
        var (world, _) = Separated();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 4, Index: 1, Subject: 0, Seat: 1));
        EndOfPhase(world);

        Assert.Null(VillainPhaseFocus.Of(world));
    }

    [Fact]
    [Rule("rr:villain-phase.step.2")]
    public void AnActivationUnderWayNamesTheActivatedSeat()
    {
        var (world, _) = Separated();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Index: 1, Subject: 0, Seat: 1));
        EndOfPhase(world);
        Assert.Equal(new VillainPhaseFocus(2, 1, world.GameAreaOf(PlayArea.Of(1))!.Id), VillainPhaseFocus.Of(world));

        var (other, _) = Separated();
        other.Agenda.Add(new PhaseStep(new ActivationPlanOperation(), 1, 2, Plan: true));
        EndOfPhase(other);
        Assert.Null(VillainPhaseFocus.Of(other));
        other.Activation = new EnemyActivation(0, 0, true);
        Assert.Equal(new VillainPhaseFocus(2, 0, other.GameAreaOf(PlayArea.Of(0))!.Id), VillainPhaseFocus.Of(other));
    }

    private static void EndOfPhase(World world) =>
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndVillainPhase), 1, 6));

    private static (World World, Card Scheme) Separated()
    {
        var world = new World(Cards, 2, 42);
        Card scheme = null!;
        for (int seat = 0; seat < 2; seat++)
        {
            world.CreateSeat($"p{seat}").IdentityCard = world.CreateCard("01001a", world.Seats[seat].Hero);
            world.Join(PlayArea.Of(seat), world.CreateGameArea(), "prepared split", []);
            var local = world.CreateCard("11009a,11009b", world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(seat)));
            if (seat == 1) scheme = local;
        }
        world.Detach(PlayArea.Villains, "prepared split", []);
        return (world, scheme);
    }
}
