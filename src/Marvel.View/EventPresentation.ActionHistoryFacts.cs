using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>One visibility-safe, human-readable entry in the game chronology.</summary>

/// <summary>Authorized engine facts for one completed player action.</summary>
public sealed record ActionHistoryFacts(
    int Cursor,
    string Actor,
    string Role,
    string Phase,
    string? Verb,
    string Action,
    int? Subject,
    IReadOnlyList<int> ResourceGeneratorIds,
    IReadOnlyList<string> ResourceGenerators,
    Outcome? Outcome = null);
