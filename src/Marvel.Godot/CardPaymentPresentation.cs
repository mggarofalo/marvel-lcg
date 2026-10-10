using Marvel.Decisions;

namespace Marvel.Godot;

/// <summary>Selects the card-play composition surface without deciding payment legality.</summary>
internal static class CardPaymentPresentation
{
    internal static bool UsesModal(DecisionComposer? composer, bool submitting = false) =>
        !submitting && composer?.Selected?.PlaysCard == true;

    internal static string Progress(PaymentProgress payment) =>
        $"{payment.GeneratedIcons} resource{(payment.GeneratedIcons == 1 ? "" : "s")} selected"
        + (payment.IsSatisfied ? " · Payment ready"
            : payment.RemainingRequired is > 0 and var remaining
                ? $" · {remaining} more resource{(remaining == 1 ? "" : "s")} needed"
                : payment.CanCoverCost ? " · Assign selected resources to the required costs"
                : " · Choose resources to cover the cost");
}
