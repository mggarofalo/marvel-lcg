namespace Marvel.Rules.Play;

/// <summary>The response window after a completed Preparation ability.</summary>
public sealed record PreparationAbilityResolvedOperation(ResolvedPreparationAbility Receipt) : AgendaOperation
{
    /// <summary>Engine spelling of this completed-ability condition.</summary>
    public const string Condition = "WhenPreparationAbilityResolved";
    /// <inheritdoc/>
    public override string What => Condition;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Lifecycle;
}
