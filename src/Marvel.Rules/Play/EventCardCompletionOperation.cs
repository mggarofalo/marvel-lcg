using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Retains the exact played event until its resolving execution finishes.</summary>
internal sealed record EventCardCompletionOperation(
    World World, Card Card, int Incarnation, AbilityExecutionResult Resolution, AbilityExecution Execution) : AgendaOperation
{
    internal bool Consumed { get; set; }
    public override string What => "CompletePlayedEvent";
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.CardEntry;
}
