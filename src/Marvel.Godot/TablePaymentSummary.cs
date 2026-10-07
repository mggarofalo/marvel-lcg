using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Describes payment progress without replacing the surrounding game situation.</summary>
internal static class TablePaymentSummary
{
    internal static bool ExplainsOutstandingChoice(DecisionProgressPresentation progress) =>
        progress.Targets.IsSatisfied
        && progress.Payment is { CostState: CostSelectionState.Selected, IsSatisfied: false, CanCoverCost: false } payment
        && payment.DefinedVariables == payment.RequestedVariables;

    internal static void Add(List<string> state, PaymentProgress payment, bool compact = false)
    {
        switch (payment.CostState)
        {
            case CostSelectionState.Required:
                state.Add($"{payment.CostOptions} payment options.");
                break;
            case CostSelectionState.Selected:
                state.Add($"{payment.GeneratedIcons} resource{(payment.GeneratedIcons == 1 ? string.Empty : "s")} selected.");
                if (!compact && !payment.IsSatisfied && payment.DefinedVariables == payment.RequestedVariables)
                    state.Add(payment.CanCoverCost
                        ? "Assign the selected icons to the required costs."
                        : "Select resources that cover the displayed cost and required types.");
                if (payment.ExcessIcons > 0)
                {
                    state.Add(DecisionCopy.OverpaymentWarning(payment));
                }
                break;
        }
    }
}
