using System.Collections.Immutable;

namespace Marvel.Rules.Setup;

/// <summary>An immutable assessment of an ordered deck, without allocating game state.</summary>
public sealed record DeckAssessment
{
    /// <summary>Captures diagnostics independently of the caller.</summary>
    public DeckAssessment(int? size, IEnumerable<DeckDiagnostic> diagnostics)
    {
        Size = size;
        Diagnostics = diagnostics.ToImmutableArray();
    }

    /// <summary>Counted cards, or null when unrecognized input prevents rules assessment.</summary>
    public int? Size { get; }

    /// <summary>All independently detectable corrections, in deterministic order.</summary>
    public ImmutableArray<DeckDiagnostic> Diagnostics { get; }

    /// <summary>Whether the assessed construction constraints are satisfied.</summary>
    public bool IsValid => Diagnostics.IsEmpty;
}
