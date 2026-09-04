using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Player-facing action and payment language for the decision rail.</summary>
internal static class DecisionCopy
{
    public static string Choice(AffordancePresentation view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (!string.IsNullOrWhiteSpace(view.Description))
        {
            return view.Description;
        }

        string label = PromptPresentation.Words(view.Label);
        if (string.Equals(label, view.Anchor, StringComparison.OrdinalIgnoreCase))
        {
            return label;
        }

        if (!string.Equals(view.Verb, label, StringComparison.OrdinalIgnoreCase))
        {
            return $"{label}  ·  {view.Anchor}";
        }

        return view.Verb switch
        {
            "Play" => $"Play {view.Anchor}",
            "Attack" or "Thwart" or "Recover" => $"{view.Verb} with {view.Anchor}",
            "Resolve Mulligans" => "Choose cards to discard and redraw",
            _ => $"{view.Verb}  ·  {view.Anchor}",
        };
    }

    public static string ActionSummary(AffordancePresentation view)
    {
        ArgumentNullException.ThrowIfNull(view);
        string action = Choice(view);
        return string.IsNullOrWhiteSpace(view.Consequence)
            ? action
            : $"{action}\n{view.Consequence}";
    }

    public static string GenericCommit(string verb, string label, string anchor)
    {
        string readableVerb = PromptPresentation.Words(verb);
        string readableLabel = PromptPresentation.Words(label);
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
