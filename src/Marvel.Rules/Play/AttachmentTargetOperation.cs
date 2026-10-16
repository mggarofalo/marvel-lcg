using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the AttachmentTarget procedure.</summary>
public sealed record AttachmentTargetOperation(IReadOnlyList<int> Candidates, Occurrence? Origin = null) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.ChooseAttachmentTarget;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Reveal;
}
