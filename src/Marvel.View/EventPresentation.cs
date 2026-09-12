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
    EventMotionKind Motion);
