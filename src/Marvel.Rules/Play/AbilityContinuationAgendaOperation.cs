using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Routes suspended interpreter work without inspecting its frames.</summary>
public sealed record AbilityContinuationAgendaOperation(AbilityContinuationStage Stage)
    : AgendaOperation
{
    /// <summary>The interpreter-owned execution frame.</summary>
    public CardAbilityContinuation? Continuation { get; init; }
    /// <summary>The surrounding occurrence, retained for attack attribution.</summary>
    public Occurrence? Origin { get; init; }
    /// <summary>Whether a scheduled Special is the final Special in its sequence.</summary>
    public bool FinalSpecial { get; init; }
    /// <summary>Whether a scheduled each-player frame is the last selected player.</summary>
    public bool FinalPlayer { get; init; }
    /// <summary>The reveal's one effective gained Surge instance.</summary>
    public bool SurgeGained { get; init; }
    /// <summary>Enemy activation completions that must precede resumption.</summary>
    public IReadOnlyList<int> AwaitedActivations { get; init; } = [];
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.AbilityContinuation;
    /// <inheritdoc/>
    public override string What => Stage switch
    {
        AbilityContinuationStage.Resume => Steps.ResumeAbility,
        AbilityContinuationStage.Choose => Steps.ChooseOption,
        AbilityContinuationStage.OrderPlayers => Steps.OrderEachPlayer,
        AbilityContinuationStage.ResolvePlayer => Steps.ResolveEachPlayer,
        AbilityContinuationStage.Special => Steps.ResolveSpecial,
        _ => throw new InvalidOperationException("unknown ability continuation stage"),
    };
}
