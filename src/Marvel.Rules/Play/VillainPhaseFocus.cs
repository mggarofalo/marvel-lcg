using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>The villain-phase step resolving now, and the seat whose turn of it this is.</summary>
/// <param name="Step">The <c>rr:villain-phase</c> step: 1 threat, 2 activation, or 4 reveal.</param>
/// <param name="Seat">The seat the step is resolving for.</param>
/// <param name="GameArea">That seat's game area id, or null when it is in none.</param>
/// <remarks>
/// Ruling 97d62cdb9ce436a7 runs each step across the separated game areas in turn
/// order: "each player should place threat on their main scheme in turn order, then
/// each player should resolve the activation of the Kang in their game area in turn
/// order, then each player should be dealt an encounter card in turn order".
/// </remarks>
public sealed record VillainPhaseFocus(int Step, int Seat, int? GameArea)
{
    /// <summary>The seat-specific villain-phase work in resolution now, or null between such work.</summary>
    /// <remarks>
    /// The engine reads its own agenda. The villain phase is under way while its
    /// ending step is outstanding; within it, the first outstanding villain-phase step bound
    /// to a seat names the focus, and reaching a step heading first means no seat's
    /// part is under way. Step 1 is seat-bound only while the table is separated,
    /// where each area's placement belongs to its earliest player
    /// (<see cref="SeparateGameAreas.ScheduleStepOne"/>); step 3 deals without a
    /// seat-bound occurrence, so it names none.
    /// </remarks>
    public static VillainPhaseFocus? Of(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var outstanding = world.Agenda.Outstanding;
        if (!outstanding.Any(step => step.Operation is PhaseTransitionOperation { Kind: PhaseTransitionKind.EndVillainPhase }))
            return null;
        foreach (var step in outstanding)
        {
            int? seat = step.Operation switch
            {
                ThreatPlacementOperation threat when threat.Placement.Cause == ThreatCause.VillainPhase
                    && step.Seat >= 0 => step.Seat,
                EnemyAttackOperation or EnemySchemeOperation when step.Number == 2 && step.Seat >= 0 => step.Seat,
                ActivationPlanOperation when world.Activation is { } activation => activation.Player,
                RevealStageOperation { Kind: RevealStageKind.RevealEncounterCard } when step.Number == 4
                    && step.Seat >= 0 => step.Seat,
                _ => null,
            };
            if (seat is { } found)
                return new(StepOf(step.Operation), found, world.GameAreaOf(PlayArea.Of(found))?.Id);
            if (step.Operation is ThreatStageOperation or ActivationPlanOperation or RevealStageOperation
                or PhaseTransitionOperation)
                return null;
        }
        return null;
    }

    private static int StepOf(AgendaOperation operation) => operation switch
    {
        ThreatPlacementOperation => 1,
        RevealStageOperation => 4,
        _ => 2,
    };
}
