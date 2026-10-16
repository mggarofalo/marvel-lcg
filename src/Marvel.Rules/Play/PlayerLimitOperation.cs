using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the PlayerLimit procedure.</summary>
public sealed record PlayerLimitOperation(PlayerLimitKind Kind,
    IReadOnlyList<int>? Candidates = null) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Kind switch
    {
        PlayerLimitKind.ChooseAllyForLimit => Steps.ChooseAllyForLimit,
        PlayerLimitKind.ChooseRestrictedCard => Steps.ChooseRestrictedCard,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.PlayerAction;
}
