using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>One visibility-safe, human-readable entry in the game chronology.</summary>

/// <summary>Persistent entries and replaceable transient cues for one response.</summary>
public sealed record EventBatchPresentation(
    IReadOnlyList<EventPresentation> History,
    IReadOnlyList<EventPresentation> Cues,
    IReadOnlyList<EventPresentation> Highlights);
