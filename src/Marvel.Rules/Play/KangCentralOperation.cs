using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>One ordered part of Kang's central stage reveal.</summary>
public sealed record KangCentralOperation(KangCentralPart Part, int Index = 0, IReadOnlyList<int>? Players = null,
    IReadOnlyList<OccurrenceCard>? Stored = null) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Part switch
    {
        KangCentralPart.RevealVillain => "RevealKangStageThree",
        KangCentralPart.Dominions => "RevealKangDominions",
        KangCentralPart.Nemeses => "PutKangNemesesIntoPlay",
        _ => throw new ArgumentOutOfRangeException(nameof(Part)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.PhaseTransition;
}
