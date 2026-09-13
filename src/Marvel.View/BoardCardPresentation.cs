using System.Globalization;
using System.Text;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>One readable card, face-down object, or concealed pile summary.</summary>
public sealed record BoardCardPresentation(
    int? TargetId,
    int Count,
    bool Concealed,
    string Title,
    string Subtitle,
    string Kind,
    string Status,
    IReadOnlyList<BoardFieldPresentation> Fields)
{
    /// <summary>The card's engine-zone-derived role in a progressive stage stack.</summary>
    public BoardStageRole StageRole { get; init; }

    /// <summary>The non-identifying physical back.</summary>
    public string Back { get; init; } = string.Empty;

    /// <summary>The stable visible face id used only for optional local art.</summary>
    public string? FaceId { get; init; }

    /// <summary>Effective traits visible on the current face.</summary>
    public IReadOnlyList<string> Traits { get; init; } = [];

    /// <summary>The printed aspect or Basic classification.</summary>
    public string Classification { get; init; } = string.Empty;

    /// <summary>The printed cost, or null when none is printed.</summary>
    public string? Cost { get; init; }

    /// <summary>Printed stats, kept separate from current live values.</summary>
    public IReadOnlyList<BoardFieldPresentation> PrintedStats { get; init; } = [];

    /// <summary>Printed keyword labels.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];

    /// <summary>Printed rules text.</summary>
    public string RulesText { get; init; } = string.Empty;

    /// <summary>Printed rules text with display-only emphasis and symbol tokens.</summary>
    public string RulesMarkup { get; init; } = string.Empty;

    /// <summary>Damage currently on the card.</summary>
    public long Damage { get; init; }

    /// <summary>Live counters currently on the card.</summary>
    public IReadOnlyList<BoardFieldPresentation> Counters { get; init; } = [];
}
