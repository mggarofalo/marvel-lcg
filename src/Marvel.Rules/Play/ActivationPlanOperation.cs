using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the ActivationPlan procedure.</summary>
public sealed record ActivationPlanOperation(IReadOnlyList<int>? ActivatedEnemies = null, IReadOnlyList<int>? Players = null,
    IReadOnlyList<int>? Chosen = null, IReadOnlyDictionary<int, long>? Incarnations = null) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.EnemiesActivate;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Activation;
}
