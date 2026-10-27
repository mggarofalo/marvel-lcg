using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Required Kang choices retained ahead of their exact phase-ending frame.</summary>
public sealed record KangJoinOperation(
    KangDeparture Departure, PhaseStep Owner, Occurrence Origin,
    GameArea? Destination = null) : AgendaOperation
{
    /// <summary>Engine-chosen name of this scenario procedure.</summary>
    public const string Join = "ResolveKangPhaseJoin";
    /// <inheritdoc/>
    public override string What => Join;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.PhaseTransition;
}
