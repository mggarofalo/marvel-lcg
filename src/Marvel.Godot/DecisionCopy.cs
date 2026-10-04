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
            return anchor;
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

    public static string ActionSummary(AffordancePresentation view)
    {
        ArgumentNullException.ThrowIfNull(view);
        string?[] parts = [Choice(view), view.SourceState,
            view.CostDescription is { Length: > 0 } cost ? $"Costs: {cost}." : null,
            view.Description, view.Consequence];
        return string.Join("\n", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Distinct());
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

    public static string WithPaymentConsequence(string action, PaymentProgress payment) =>
        payment.ExcessIcons == 0
            ? action
            : $"{action}  ·  Lose {ResourceCount(payment.ExcessIcons)}";

    public static string OverpaymentWarning(PaymentProgress payment) =>
        $"Overpayment: {ResourceCount(payment.ExcessIcons)} will be lost.";

    public static string ResourceCount(int count) =>
        $"{count} excess resource{(count == 1 ? string.Empty : "s")}";
}
