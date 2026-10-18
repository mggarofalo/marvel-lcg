namespace Marvel.Protocol;

/// <summary>Exactly one public catalog or draft assessment, according to the request.</summary>
public sealed record DeckResponse(DeckCatalog? Catalog, DeckAssessmentResult? Assessment);
