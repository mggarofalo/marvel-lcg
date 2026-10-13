using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>One visibility-safe, human-readable entry in the game chronology.</summary>
public sealed record EventPresentation(
    string Summary,
    string Cause,
    IReadOnlyList<int> Anchors,
    EventMotionKind Motion)
{
    /// <summary>An authorized mandatory ability receipt requiring no engine command.</summary>
    public ForcedAbilityNotification? ForcedAbilityNotification { get; init; }

    /// <summary>Accepted player intention surrounding this result, distinct from its immediate trigger.</summary>
    public string? Intention { get; init; }
    /// <summary>Local typed activity facts; the server wire continues to carry filtered GameEvents.</summary>
    public EventActivityDescriptor? Activity { get; init; }

    /// <summary>Compact occurrence copy for a table cue; the full chronology remains in Summary.</summary>
    public string? CueSummary { get; init; }
    /// <summary>Concise measured result beneath an occurrence headline, when separate from its intention.</summary>
    public string? ResultSummary { get; init; }

    /// <summary>Exact authorized component events folded into this occurrence, retained in engine order.</summary>
    public IReadOnlyList<EventPresentation> Details { get; init; } = [];

    /// <summary>Explicit result subjects; readable copy is never a relationship source.</summary>
    public IReadOnlyList<TableRelationshipDescriptor> Relationships { get; init; } = [];
}
