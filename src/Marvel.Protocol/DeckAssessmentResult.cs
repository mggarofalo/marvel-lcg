using Marvel.Rules.Setup;

namespace Marvel.Protocol;

/// <summary>The host's assessment snapshot; clients do not reconstruct these rules.</summary>
public sealed record DeckAssessmentResult(int? Size, IReadOnlyList<DeckDiagnostic> Diagnostics);
