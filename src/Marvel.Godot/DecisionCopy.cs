using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Player-facing action and payment language for the decision rail.</summary>
internal static class DecisionCopy
{
    public static string Choice(AffordancePresentation view)
    {
        ArgumentNullException.ThrowIfNull(view);
        string anchor = view.SourceName ?? view.Anchor;
        if (view.PlaysCard) return $"Play {anchor}";
        string label = PromptPresentation.Words(view.DisplayLabel ?? view.Label);
        if (string.Equals(label, view.Anchor, StringComparison.OrdinalIgnoreCase))
        {
            return view.CostDescription is { Length: > 0 } cost ? $"{anchor} · {cost}" : anchor;
        }

        if (!string.Equals(view.Verb, label, StringComparison.OrdinalIgnoreCase))
        {
            return $"{label}  ·  {anchor}";
        }

        return view.Verb switch
        {
            "Play" => $"Play {anchor}",
            "Attack" or "Thwart" or "Recover" => $"{view.Verb} with {anchor}",
            "Choose" => $"Choose {anchor}",
            "Action" => $"Use {anchor}",
            "Resolve Mulligans" => "Choose cards to discard and redraw",
            _ => $"{view.Verb}  ·  {anchor}",
        };
    }

    public static string ActionSummary(AffordancePresentation view) => string.Join("\n", ActionParts(view, compact: false));

    /// <summary>
    /// An action's parts in reading order: its choice, context, source state, costs, effect and consequence. The
    /// compact form leaves out the choice and the source state, which its visible card already shows.
    /// </summary>
    internal static IReadOnlyList<string> ActionParts(AffordancePresentation view, bool compact)
    {
        ArgumentNullException.ThrowIfNull(view);
        string?[] parts = compact
            ? [view.ContextDescription, view.CostDescription, view.Description, view.Consequence]
            : [Choice(view), view.ContextDescription, view.SourceState,
                view.CostDescription is { Length: > 0 } cost ? $"Costs: {cost}." : null,
                view.Description, view.Consequence];
        return [.. parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!).Distinct()];
    }

    public static string GenericCommit(string verb, string label, string anchor)
    {
        string readableVerb = PromptPresentation.Words(verb);
        string readableLabel = PromptPresentation.Words(label);
        if ((string.Equals(readableVerb, "Choose option", StringComparison.OrdinalIgnoreCase)
            || string.Equals(readableVerb, "Choose", StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(readableLabel))
            return readableLabel.StartsWith("Choose ", StringComparison.OrdinalIgnoreCase)
                ? readableLabel : $"Choose {readableLabel}";
        return string.Equals(readableVerb, "Action", StringComparison.OrdinalIgnoreCase)
            ? $"Use {(string.IsNullOrWhiteSpace(readableLabel) ? anchor : readableLabel)}"
            : readableVerb;
    }

    // Source state stays on its visible card; the dock retains commitments and effects.
    internal static string CompactActionSummary(AffordancePresentation view) => string.Join(" · ", ActionParts(view, compact: true));

    internal static string AbilityLabel(AffordancePresentation view, string label, string? choice = null)
    {
        string? effect = string.IsNullOrWhiteSpace(choice) ? view.Description : choice;
        string?[] parts = [view.ContextDescription, view.CostDescription, effect, view.Consequence];
        string detail = string.Join("\n", parts.Where(part => !string.IsNullOrWhiteSpace(part)
            && !TooltipPolicy.Restates(label, part)).Distinct());
        return detail.Length == 0 ? label : $"{label}\n{detail}";
    }

    /// <summary>
    /// A complete window choice: its label, which states its effect, then its costs, its own description and the
    /// consequence. The offer's general description stays on hover, so the choice never repeats its effect.
    /// </summary>
    internal static string WindowChoiceLabel(AffordancePresentation view, Marvel.Rules.Prompts.WindowChoice choice) =>
        AbilityLabel(view with { Description = null }, choice.Label, choice.Description);

    internal static string AbilityDetail(AffordancePresentation view, string label, string? choice)
    {
        string?[] parts = [view.ContextDescription, choice, view.CostDescription, view.Description, view.Consequence];
        return string.Join("\n", parts.Where(part => !string.IsNullOrWhiteSpace(part)
            && !TooltipPolicy.Restates(label, part)).Distinct());
    }

    public static string WithPaymentConsequence(string action, PaymentProgress payment) =>
        payment.ExcessIcons == 0
            ? action
            : $"{action}  ·  {ResourceCount(payment.ExcessIcons)} beyond the cost";

    public static string OverpaymentWarning(PaymentProgress payment) =>
        $"Overpayment: {ResourceCount(payment.ExcessIcons)} not applied to the cost.";

    public static string ResourceCount(int count) =>
        $"{count} excess resource{(count == 1 ? string.Empty : "s")}";

    internal static string ResourceChoice(PaymentSourcePresentation source)
    {
        if (source.DiscardsCard) return $"Discard {source.Name}";
        if (string.IsNullOrWhiteSpace(source.AbilityName)) return $"Use {source.Name}";
        return source.AbilityName == source.Name ? source.Name : $"{source.AbilityName} · {source.Name}";
    }
}
