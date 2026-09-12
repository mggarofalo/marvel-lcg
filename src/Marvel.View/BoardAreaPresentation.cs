using System.Globalization;
using System.Text;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>One engine-provided area and its two card containers.</summary>
public sealed record BoardAreaPresentation(
    int Id,
    string Title,
    string Context,
    IReadOnlyList<BoardCardPresentation> Cards,
    IReadOnlyList<BoardCardPresentation> Removed)
{
    /// <summary>The descriptor zone name, retained for diagnostics and generic rendering.</summary>
    public string Zone { get; init; } = string.Empty;

    /// <summary>The scenario or player table coordinate, not card ownership.</summary>
    public int Seat { get; init; } = -1;

    /// <summary>The visible host card id, or -1.</summary>
    public int Host { get; init; } = -1;

    /// <summary>Nesting depth beneath a visible host area.</summary>
    public int Depth { get; init; }

    /// <summary>The visible host title, or empty when unhosted.</summary>
    public string HostedBy { get; init; } = string.Empty;

    /// <summary>How prominently the desktop table should present this area.</summary>
    public BoardAreaProminence Prominence { get; init; } = BoardAreaProminence.Empty;
}
