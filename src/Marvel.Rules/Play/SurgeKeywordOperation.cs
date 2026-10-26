using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>One actual surge resolution, after reveal cancellation and ordering.</summary>
public sealed record SurgeKeywordOperation(Occurrence? Origin = null,
    PendingAbility? Ability = null, string DealTrigger = "surge") : AgendaOperation
{
    /// <summary>Engine spelling of the interrupt immediately before surge deals its card.</summary>
    public const string Condition = "WhenSurgeWouldResolve";
    /// <inheritdoc/>
    public override string What => Condition;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Reveal;
}
