using System.Text.Json.Serialization;

namespace Marvel.Rules.Timing;

/// <summary>Whether rule-defined resolution is still pending, succeeded, or did not occur.</summary>
public enum ResolutionStatus
{
    /// <summary>The ability or card has initiated but has not finished.</summary>
    Pending,

    /// <summary>At least one required child effect or ability resolved.</summary>
    Resolved,

    /// <summary>Completion or cancellation left no resolved child.</summary>
    Unresolved,
}
