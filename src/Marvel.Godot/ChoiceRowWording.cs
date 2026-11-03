using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>
/// The words of a row in a decision's choices: a short label with its state mark, the per-target detail that shows as
/// a chip only when it tells rows apart, and the progress as a tracker. Every sentence stays on the row's hover.
/// </summary>
/// <remarks>The words and the rule for which detail shows are our choices; the details are the engine's.</remarks>
internal static class ChoiceRowWording
{
    /// <summary>A target row's label: its state mark, the action and the target's name.</summary>
    internal static string Target(string action, string name, bool selected) =>
        $"{(selected ? "✓" : "◇")} {action} · {name}";

    /// <summary>A grouped target row's label: its state mark, its number and the group's members in order.</summary>
    internal static string Group(int index, IEnumerable<string> members, bool selected) =>
        $"{(selected ? "✓" : "◇")} Group {index + 1} · {string.Join(" → ", members)}";

    /// <summary>
    /// The detail a target row shows as a chip, without the target's name: null when the target has none, when it
    /// repeats <paramref name="shownElsewhere"/>, or when every legal target carries the same detail, which then
    /// tells no row apart and stays on hover.
    /// </summary>
    internal static string? Distinguishing(TargetRequest request, int target, string name, string? shownElsewhere = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        string? detail = Detail(name, request.Details?.GetValueOrDefault(target), shownElsewhere);
        if (detail is null) return null;
        int[] legal = [.. request.Legal.Distinct()];
        bool shared = legal.Length > 1 && legal.All(id => request.Details?.GetValueOrDefault(id) == request.Details![target]);
        return shared ? null : detail;
    }

    /// <summary>A row's hover: the target's name and the engine's whole detail.</summary>
    internal static string Tooltip(string name, string? detail) =>
        string.IsNullOrWhiteSpace(detail) ? name : detail.StartsWith(name + ":", StringComparison.Ordinal) ? detail : $"{name}: {detail}";

    /// <summary>
    /// The selection's progress as a tracker part: what is staged or selected against the most that can be, with the
    /// sentence on hover. Null when the decision selects no targets.
    /// </summary>
    internal static DraftPart? Progress(DecisionComposer? composer, TargetSelectionProgress progress)
    {
        if (progress.Mode == TargetSelectionMode.None) return null;
        string text = DecisionPanelCopy.TargetProgress(composer, progress);
        return composer?.Selected?.Verb switch
        {
            Game.ResolveMulligans => new(text, DecisionWording.DraftRedraw, progress.Selected, progress.Maximum),
            Game.EndPhaseVerb => new(text, DecisionWording.DraftDiscard, progress.Selected, progress.Maximum),
            _ when progress.Mode == TargetSelectionMode.Grouped => new(text, DecisionWording.DraftTargets, progress.Selected, 1),
            _ => new(text, DecisionWording.DraftTargets, progress.Selected, progress.Maximum),
        };
    }

    /// <summary>A repeatable target's count against its limit, as a tracker part named by the target.</summary>
    internal static DraftPart Repeated(string name, string? detail, int count, int limit) =>
        new($"{Tooltip(name, detail)} · {count} of at most {limit}", name, count, limit);

    // The engine's detail without the target's name, or null when it has none or repeats what is shown elsewhere.
    private static string? Detail(string name, string? detail, string? shownElsewhere)
    {
        if (string.IsNullOrWhiteSpace(detail) || detail == shownElsewhere || detail == $"{name}: {shownElsewhere}") return null;
        return detail.StartsWith(name + ":", StringComparison.Ordinal) ? detail[(name.Length + 1)..].Trim() : detail;
    }
}
