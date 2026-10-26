using Godot;
using Marvel.Decisions;

namespace Marvel.Godot;

/// <summary>Selects the card-play composition surface without deciding payment legality.</summary>
internal static class CardPaymentPresentation
{
    internal static bool UsesModal(DecisionComposer? composer, bool submitting = false) =>
        !submitting && composer?.Selected?.PlaysCard == true;

    internal static string Progress(PaymentProgress payment) =>
        $"{payment.GeneratedIcons} resource{(payment.GeneratedIcons == 1 ? "" : "s")} selected"
        + (payment.IsSatisfied ? " - Payment ready"
            : payment.RemainingRequired is > 0 and var remaining
                ? $" - {remaining} more resource{(remaining == 1 ? "" : "s")} needed"
                : payment.CanCoverCost ? " - Assign selected resources to the required costs"
                : " - Choose resources to cover the cost");
    internal static IEnumerable<Control> Components(PaymentProgress payment)
    {
        if (payment.Components is not { } components || payment.RemainingRequired is not null) yield break;
        foreach (var component in components)
        {
            string status = component.Remaining == 0 ? "covered"
                : $"{component.Remaining} more resource{(component.Remaining == 1 ? "" : "s")} needed";
            string qualifier = component.DistinctTypes ? " - different types" : "";
            string text = $"Possible coverage - Cost {component.Component + 1} - {status}{qualifier}";
            var row = ResourceIconRendering.Row(text, component.RemainingResources, GodotThemeVariations.Body);
            row.Name = $"PaymentComponentProgress{component.Component}";
            row.AccessibilityName = $"{text} {CardRulesMarkup.ResourceNames(component.RemainingResources)}";
            row.TooltipText = "Suggested remaining costs under one possible allocation; assignments remain editable.";
            yield return row;
        }
    }
}

