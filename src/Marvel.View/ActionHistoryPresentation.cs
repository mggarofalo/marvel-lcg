using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>One action headline and its genuine subordinate results.</summary>
public sealed record ActionHistoryPresentation(
    string Summary,
    IReadOnlyList<string> Details);
