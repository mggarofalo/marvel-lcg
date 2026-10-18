using Marvel.Rules.Setup;

namespace Marvel.Protocol;

/// <summary>The host's assessment snapshot; clients do not reconstruct these rules.</summary>
public sealed record DeckAssessmentResult(int? Size, IReadOnlyList<DeckDiagnostic> Diagnostics)
{
    /// <summary>Engine acceptance of a partial deck, allowing only the unfinished minimum size.</summary>
    public bool CanExtend { get; init; }
}
