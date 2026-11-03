using System.Collections.Immutable;

namespace Marvel.Rules.Play;

/// <summary>State owned by the batched minion-entry procedure.</summary>
/// <param name="Batch">The minions this instruction has put into play.</param>
/// <param name="Stage">What the step does.</param>
/// <param name="Remaining">Cards still to discard.</param>
/// <param name="Completion">The containing instruction's receipt.</param>
public sealed record MinionEntryBatchOperation(MinionEntryBatch Batch, MinionEntryBatchStage Stage,
    long Remaining, EffectCompletion Completion) : AgendaOperation
{
    /// <summary>The discarded minion awaiting the first player's choice of engaged player.</summary>
    public int? Minion { get; init; }
    /// <summary>The players tied for the fewest engaged minions.</summary>
    public ImmutableArray<int> Tied { get; init; } = [];
    /// <summary>Keyword activations that have not resolved, in entry order.</summary>
    public ImmutableArray<MinionEntryKeyword> Pending { get; init; } = [];

    /// <summary>Discard one encounter card for an instruction that engages each discarded minion.</summary>
    public const string DiscardEngagingMinions = "DiscardEngagingMinions";
    /// <summary>The first player chooses which tied player a discarded minion engages.</summary>
    public const string ChooseEngagedPlayer = "ChooseEngagedPlayer";
    /// <summary>Resolve the Quickstrike and Teamwork of minions put into play together.</summary>
    public const string ResolveBatchedEntryKeywords = "ResolveBatchedEntryKeywords";
    /// <summary>Open the response windows of minions put into play together.</summary>
    public const string OpenBatchedEntryResponses = "OpenBatchedEntryResponses";

    /// <inheritdoc/>
    public override string What => Stage switch
    {
        MinionEntryBatchStage.Discard => DiscardEngagingMinions,
        MinionEntryBatchStage.ChooseEngagedPlayer => ChooseEngagedPlayer,
        MinionEntryBatchStage.Keywords => ResolveBatchedEntryKeywords,
        MinionEntryBatchStage.Responses => OpenBatchedEntryResponses,
        _ => throw new ArgumentOutOfRangeException(nameof(Stage)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.EncounterEntry;
}
